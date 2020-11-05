using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Profiling;
using UnityEngine.Rendering;


// This uses 2 Unity packages:
// Unity Burst compiler, Window -> Package Manager -> Burst (also installs Mathematics).
//    NOTE: On Burst 1.2.3, I had a compile error that seems to be a bug on Unity's end.  1.3.4 works.
// Unity Mathematics, Window -> Package Manager -> Mathematics.
// `NativeList` is mentioned in comments, but not used.  It is available in the Collections package:
//    Collections, Window -> Package Manager -> Collections (currently in preview; Advanced -> Show Preview Packages). 

namespace RopeJobs {
    public class RopeJob : MonoBehaviour {
        // These are constants because they require a restart to change.
        // Maximum number of colliders hitting the rope at once.
        private const int MAX_ROPE_COLLISIONS = 32;
        // Size of the collider buffer, also the maximum number of colliders that a single node can touch at once.
        private const int COLLIDER_BUFFER_SIZE = 8;
        // IF YOU CHANGE THIS, REMEMBER TO CHANGE IT IN THE SHADER AS WELL.
        private const int MAX_RENDER_POINTS = 256;
        // Number of triangles present in each line segment between nodes.
        // 4 = 2 per quad, one quad for the line and one for the start cap.
        private const int TRIANGLES_PER_NODE = 4;
        // Number of vertices present in each line segment between nodes.
        // 8 = 2 quads.
        private const int VERTICES_PER_NODE = 8;

        [Min(2)]
        public int totalNodes = 200;
        public int iterations = 80;
        public float nodeDistance = 0.1f;
        [Min(0.001f)]
        public float stepTime = 0.01f;
        public float maxStep = 0.1f;

        public float drawWidth = 0.025f;
        public Vector2 gravity = new Vector2(0, -20f);
        public float maxSimMove = 1f;
        public float collisionRadius = 0.5f;  // Collision radius around each node.  Set high to avoid tunneling.
        public float friction = 0.5f;
        
        // Job arrays and stuffs.
        private NativeArray<VerletNode> nodes;
        private NativeArray<Constraint> constraints;
        private NativeArray<CollisionInfo> collisionInfos;
        //private NativeArray<float> timeAccumulator;
        private NativeArray<int> collidingNodes;
        private Job job;
        private JobHandle jobHandle;

        private float timeAccum;

        private int numCollisions;
        private bool shouldSnapshotCollision;

        private Material material;
        private Collider2D[] colliderBuffer;
        private Vector4[] renderPositions;
        
        private void Awake() {
            if (totalNodes > MAX_RENDER_POINTS) {
                Debug.LogError("Total nodes is more than MAX_RENDER_POINTS, so won't be able to render the entire rope.");
            }
            
            // Buffer for OverlapCircleNonAlloc.
            colliderBuffer = new Collider2D[COLLIDER_BUFFER_SIZE];
            
            // Jobs setup.
            // You could use `NativeList` for some of these instead, and not worry about all the constants.
            nodes = new NativeArray<VerletNode>(totalNodes, Allocator.Persistent);
            constraints = new NativeArray<Constraint>(totalNodes, Allocator.Persistent);
            collisionInfos = new NativeArray<CollisionInfo>(MAX_ROPE_COLLISIONS, Allocator.Persistent);
            collidingNodes = new NativeArray<int>(totalNodes * MAX_ROPE_COLLISIONS, Allocator.Persistent);

            renderPositions = new Vector4[totalNodes];
            
            // Spawn nodes starting from the transform position and working down.
            Vector2 pos = transform.position;
            for (int i = 0; i < nodes.Length; i++) {
                nodes[i] = new VerletNode(pos);
                renderPositions[i] = new Vector4(pos.x, pos.y, 1, 1);
                pos.y -= nodeDistance;
            }

            // Mesh setup.
            Mesh mesh = new Mesh();
            {
                Vector3[] vertices = new Vector3[totalNodes * VERTICES_PER_NODE];
                int[] triangles = new int[totalNodes * TRIANGLES_PER_NODE * 3];
            
                for (int i = 0; i < totalNodes; i++) {
                    // 4 triangles per node, 3 indices per triangle.
                    int idx = i * TRIANGLES_PER_NODE * 3;
                    // 8 vertices per node.
                    int vIdx = i * VERTICES_PER_NODE;
                
                    // Unity uses a CLOCKWISE WINDING ORDER -- clockwise tri indices are facing the camera.
                    triangles[idx + 0] = vIdx;        // v1 top
                    triangles[idx + 1] = vIdx+1;      // v2 bottom
                    triangles[idx + 2] = vIdx+2;      // v1 bottom
                    triangles[idx + 3] = vIdx;        // v1 top
                    triangles[idx + 4] = vIdx+3;      // v2 top
                    triangles[idx + 5] = vIdx+1;      // v2 bottom
                
                    triangles[idx + 6] = vIdx+4;      // tl
                    triangles[idx + 7] = vIdx+7;      // br
                    triangles[idx + 8] = vIdx+6;      // bl
                    triangles[idx + 9] = vIdx+4;      // tl
                    triangles[idx + 10] = vIdx+5;     // tr
                    triangles[idx + 11] = vIdx+7;     // br
                }
            
                // We only really care about the number of vertices, not what they actually are -- the positions aren't used.
                mesh.vertices = vertices;
                mesh.triangles = triangles;
                // Since we pretty much want the rope to always render (it's always going to be on screen if it's active), we
                // just set the bounds super large to avoid recalculating the bounds when the rope changes.
                mesh.bounds = new Bounds(Vector3.zero, Vector3.one * 100f);
            }
            GetComponent<MeshFilter>().mesh = mesh;
            material = GetComponent<MeshRenderer>().material;
            material.SetFloat("_Width", drawWidth);
        }

        private void Update() {
            if (shouldSnapshotCollision) {
                SnapshotCollision();
            }

            if (Input.GetKeyDown(KeyCode.A)) {
                Time.timeScale = 0.1f;
            } else if (Input.GetKeyUp(KeyCode.A)) {
                Time.timeScale = 1f;
            }

            // Fixed timestep.  We calculate ahead of time to avoid transporting data to the job.
            timeAccum += Time.deltaTime;
            timeAccum = Mathf.Min(timeAccum, maxStep);
            int executions = (int)(timeAccum / stepTime);
            timeAccum = timeAccum % stepTime;
            
            job = new Job {
                executions = executions,
                iterations = iterations,
                nodeDistance = nodeDistance,
                gravity = gravity,
                maxSimMove = maxSimMove,
                friction = friction,
                constraints = constraints,
                collisionInfos = collisionInfos,
                numCollisions = numCollisions,
                collidingNodes = collidingNodes,
                nodes = new NativeArray<VerletNode>(nodes, Allocator.TempJob),

                stepTime = stepTime,
            };

            jobHandle = job.Schedule();

        }

        private void LateUpdate() {
            // Wait for Job to complete.  Hopefully it's done or almost done by now.
            jobHandle.Complete();
            
            // At this point, we could implement some interpolation by have 2 separate node buffers, one for the previous
            // step and one for the latest one, and then one interpolate between them depending on frame time.
            job.nodes.CopyTo(nodes);
            job.nodes.Dispose();
            
            // Send positions to the GPU for rendering this frame.
            // Maybe we could do this in the job, but performance should be fine.
            for (int i = 0; i < nodes.Length; i++) {
                // w of 1 == we should render this node.
                renderPositions[i].w = 1;
                
                Vector2 pos = nodes[i].position;
                renderPositions[i].x = pos.x;
                renderPositions[i].y = pos.y;
                // z indicates how stretched this node is, with <1 being stretched, 1 being normal, and >1 having slack. 
                // See: https://gist.github.com/Toqozz/52fc00f22ae02bba7c48f2062d19bec9 for example implementation.
                renderPositions[i].z = 1;
            }

            material.SetVectorArray("_Points", renderPositions);
            
            // We need to manually update transform position or we might get culled after traveling a long distance.
            transform.position = (Vector2)nodes[0].position;
            if (Input.GetMouseButton(0)) {
                SetConstraint(0, Camera.main.ScreenToWorldPoint(Input.mousePosition));
            } else if (Input.GetMouseButton(1)) {
                UnsetConstraint(0);
            }
        }

        private void FixedUpdate() {
            shouldSnapshotCollision = true;
        }

        private void SnapshotCollision() {
            Profiler.BeginSample("Collision Snapshot");
            
            // Update the colliders in range of each node.
            numCollisions = 0;
            for (int i = 0; i < nodes.Length; i++) {
                // `OverlapCircle` has a `LayerMask` argument you can use to only detect collisions on a certain layer.
                // This is good if you want to separate rope collision from player collision.
                int collisions =
                    Physics2D.OverlapCircleNonAlloc(nodes[i].position, collisionRadius, colliderBuffer);

                for (int j = 0; j < collisions; j++) {
                    Collider2D col = colliderBuffer[j];
                    int id = col.GetInstanceID();

                    int idx = -1;
                    // We only check up to `numCollisions`, which is reset at the start of this function, so we only get
                    // fresh data here.
                    for (int k = 0; k < numCollisions; k++) {
                        if (collisionInfos[k].id == id) {
                            idx = k;
                            break;
                        }
                    }

                    // If we couldn't find the collider in the array, then it's new, and we need to set stuff up.
                    if (idx < 0) {
                        CollisionInfo ci = collisionInfos[numCollisions];
                        ci.id = id;
                        ci.wtl = col.transform.worldToLocalMatrix;
                        ci.ltw = col.transform.localToWorldMatrix;
                        ci.scale.x = math.length(ci.ltw.c0.xyz);
                        ci.scale.y = math.length(ci.ltw.c1.xyz);
                        ci.position = (Vector2) col.transform.position;
                        ci.numCollisions = 1; // 1 collision, this one.
                        /*
                        // Workaround for tiled and sliced sprite colliders.
                        // See blog for a slightly more optimal implementation of this.
                        var sr = col.GetComponent<SpriteRenderer>();
                        if (sr && sr.drawMode != SpriteDrawMode.Simple) {
                            Vector2 size = col.GetComponent<SpriteRenderer>().size;
                            ci.scale.x *= size.x;
                            ci.scale.y *= size.y;
                            ci.ltw = float4x4.TRS(col.transform.position, col.transform.rotation,
                                math.float3(ci.scale.x, ci.scale.y, 1f));
                            ci.wtl = math.inverse(ci.ltw);
                        }
                        */
                        collidingNodes[totalNodes * numCollisions] = i;

                        switch (col) {
                            case CircleCollider2D c:
                                ci.colliderType = ColliderType.Circle;
                                ci.colliderSize.x = ci.colliderSize.y = c.radius;
                                break;
                            case BoxCollider2D b:
                                ci.colliderType = ColliderType.Box;
                                ci.colliderSize = b.size;
                                break;
                            default:
                                ci.colliderType = ColliderType.None;
                                break;
                        }

                        collisionInfos[numCollisions] = ci;

                        // Increment and check to make sure we don't exceed max colliders.
                        numCollisions++;
                        if (numCollisions >= MAX_ROPE_COLLISIONS) {
                            Profiler.EndSample();
                            return;
                        }
                    } else {
                        CollisionInfo nc = collisionInfos[idx];
                        // This collider has reached the maximum number of nodes.
                        if (nc.numCollisions >= totalNodes) {
                            continue;
                        }

                        collidingNodes[idx * totalNodes + nc.numCollisions++] = i;
                        collisionInfos[idx] = nc;
                    }
                }
            }

            shouldSnapshotCollision = false;

            Profiler.EndSample();
        }

        private void OnDestroy() {
            // Clean up all our buffers.
            // This method also executes when stopping play mode.
            nodes.Dispose();
            constraints.Dispose();
            collisionInfos.Dispose();
            collidingNodes.Dispose();
        }
        
        
        
        // ---------------------------------
        // API to change node properties. 
        // These methods must be called in `LateUpdate()`, or the script calling them must be scheduled to run before
        // this script.
        // ---------------------------------


        public void SetConstraint(int index, Vector2 position) {
            Constraint c = constraints[index];
            c.enabled = true;
            c.position = position;
            constraints[index] = c;
        }
        
        public void UnsetConstraint(int index) {
            Constraint c = constraints[index];
            c.enabled = false;
            constraints[index] = c;
        }
    }
}
