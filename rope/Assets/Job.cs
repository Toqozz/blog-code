using System.Collections;
using System.Collections.Generic;
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;

namespace RopeJobs {
    public struct VerletNode {
        public float2 position;
        public float2 oldPosition;
        public float2 acceleration;
        public float friction;

        public VerletNode(Vector2 position, float friction = 1f) {
            this.position = position;
            this.oldPosition = position;
            this.acceleration = float2.zero;
            this.friction = friction;
        }
    }

    public struct Constraint {
        public bool enabled;
        public float2 position;
    }

    public enum ColliderType {
        Circle,
        Box,
        None,
    }

    public struct CollisionInfo {
        public int id;

        public ColliderType colliderType;
        public float2 colliderSize;
        public float2 position;
        public float2 scale;

        public float4x4 wtl;
        public float4x4 ltw;

        public int numCollisions;
    }

    // CompileSynchronously makes no real difference to performance in builds, it's something related to the editor.
    // Builds always compile synchronously.
    [BurstCompile(CompileSynchronously = true)]
    public struct Job : IJob {
        public int executions;
        
        public int iterations;
        public float nodeDistance;
        public float2 gravity;
        public float maxSimMove;
        public float friction;

        public float stepTime;
        public float maxStep;
        public int numCollisions;

        [ReadOnly] public NativeArray<Constraint> constraints;
        [ReadOnly] public NativeArray<CollisionInfo> collisionInfos;
        [ReadOnly] public NativeArray<int> collidingNodes;

        //public NativeArray<float> timeAccumulator;
        public NativeArray<VerletNode> nodes;

        public void Execute() {
            // Fixed step time assures that behaviour doesn't change with framerate.
            // When performance is bad, it's possible that we accrue steps infinitely, so we also implement a max step to
            // avoid this.  If we need to do this, we're probably pretty fucked anyway though (10fps~ at 0.1 max step).
            //float acc = timeAccumulator[0];
            //acc = math.min(acc, maxStep);
            //while (acc >= stepTime) {
            for (int i = 0; i < executions; i++) {
                Simulate();

                for (int j = 0; j < iterations; j++) {
                    ApplyConstraints();
                    AdjustCollisions();
                }

                //acc -= stepTime;
            }

            //timeAccumulator[0] = acc;
        }

        private void Simulate() {
            // For each node in rope.
            for (int i = 0; i < nodes.Length; i++) {
                VerletNode node = nodes[i];

                // Acceleration is useful to have around if we want to apply external forces to rope nodes, just add the
                // force to `node.acceleration`.
                node.acceleration += gravity;

                float2 move = node.position - node.oldPosition;
                // Limiting maximum move gives the simulation more stability.
                if (math.length(move) > maxSimMove) {
                    move = math.normalize(move) * maxSimMove;
                }

                // Calculate new position.
                float2 temp = node.position;
                node.position += move * (1f - node.friction) + stepTime * stepTime * node.acceleration;
                node.oldPosition = temp;

                node.acceleration = float2.zero;
                node.friction = 0;

                nodes[i] = node;
            }
        }

        private void ApplyConstraints() {
            for (int i = 0; i < nodes.Length - 1; i++) {
                VerletNode node1 = nodes[i];
                VerletNode node2 = nodes[i + 1];
                Constraint constraint1 = constraints[i];
                Constraint constraint2 = constraints[i + 1];

                // If the node is constrained, we need to set its position early so that the connected node adjusts
                // accordingly, but we don't want it to move, so we multiply movement by 0 later if we're constrained.
                // For the most part, we don't actually need to check `constraint2` because it's overwritten in the
                // Next iteration of the loop.  However, the last node never gets to be `node1`, so we have to check both.
                float mult1 = 1f, mult2 = 1f;
                if (constraint1.enabled) {
                    node1.position = constraint1.position;
                    mult1 = 0f;
                }
                if (constraint2.enabled) {
                    node2.position = constraint2.position;
                    mult2 = 0f;
                }
                

                // Get the current distance between rope nodes.
                float2 diff = math.float2(node1.position.x - node2.position.x, node1.position.y - node2.position.y);
                float dist = math.length(node1.position - node2.position);
                float difference = 0;
                // Guard against divide by 0.
                if (dist > 0) {
                    difference = (dist - nodeDistance) / dist;
                }

                diff *= .5f * difference;

                // Apply correction.
                node1.position -= diff * mult1;
                node2.position += diff * mult2;

                nodes[i] = node1;
                nodes[i + 1] = node2;
            }
        }

        private void AdjustCollisions() {
            // Loop through each collider.
            for (int i = 0; i < numCollisions; i++) {
                CollisionInfo nc = collisionInfos[i];

                // Looping inside the switch statement is marginally faster than the other way around.
                switch (nc.colliderType) {
                    case ColliderType.Circle: {
                        float radius = nc.colliderSize.x * math.max(nc.scale.x, nc.scale.y);

                        // Correct each node which is colliding with this collider.
                        for (int j = 0; j < nc.numCollisions; j++) {
                            VerletNode node = nodes[collidingNodes[i * nodes.Length + j]];
                            float distance = math.distance(nc.position, node.position);
                            
                            // Leave if we're not actually colliding.
                            if (distance - radius > 0) {
                                continue;
                            }

                            float2 dir = math.normalize(node.position - nc.position);
                            float2 hitPos = nc.position + dir * radius;
                            node.position = hitPos;

                            // Colliding nodes should have some friction on them.
                            node.friction = friction;

                            nodes[collidingNodes[nodes.Length * i + j]] = node;
                        }
                    }
                        break;

                    case ColliderType.Box: {
                        for (int j = 0; j < nc.numCollisions; j++) {
                            VerletNode node = nodes[collidingNodes[i * nodes.Length + j]];
                            float4 localPoint = math.mul(nc.wtl, math.float4(node.position, 0, 1));
                            
                            // If distance from center is more than box "radius", then we can't be colliding.
                            Vector2 half = nc.colliderSize * .5f;
                            Vector2 scalar = nc.scale;
                            float dx = localPoint.x;
                            float px = half.x - Mathf.Abs(dx);
                            if (px <= 0) {
                                continue;
                            }

                            float dy = localPoint.y;
                            float py = half.x - Mathf.Abs(dy);
                            if (py <= 0) {
                                continue;
                            }

                            // Need to multiply distance by scale or we'll mess up on scaled box corners.
                            if (px * scalar.x < py * scalar.y) {
                                float sx = Mathf.Sign(dx);
                                localPoint.x = half.x * sx;
                            } else {
                                float sy = Mathf.Sign(dy);
                                localPoint.y = half.y * sy;
                            }

                            // Finally get the world space hit position.
                            float4 hitPos = math.mul(nc.ltw, localPoint);

                            node.position = hitPos.xy;
                            node.friction = friction;

                            nodes[collidingNodes[nodes.Length * i + j]] = node;
                        }
                    }
                        break;
                }
            }
        }

    }
}

