using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BenchmarkProjectileSpawner : MonoBehaviour {
    public float delay = 5f;
    public int amount = 5000;

    // Pool variant.
    //public PooledObject projectile;
    // Instantiate variant.
    public GameObject projectile;

    // These probably allocate, so cache them for benchmarking.
    private Vector3 position = Vector3.zero;
    private Quaternion rotation = Quaternion.identity;

    private float timer = 0f;

    private void Update() {
        timer += Time.deltaTime;
        if (timer > delay) {
            // Only fire once.
            timer = -Mathf.Infinity;
            for (int i = 0; i < amount; i++) {
                // Pool variant.
                //Pool.Instance.Spawn(projectile, position, rotation);
                // Instantiate variant.
                Instantiate(projectile, position, rotation);
            }
        }
    }
}
