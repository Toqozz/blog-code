using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ProjectileSpawner : MonoBehaviour {
    public float spawnRate = 0.1f;
    public PooledObject projectile;

    private float timer = 0f;

    // Update is called once per frame
    private void Update() {
        timer += Time.deltaTime;
        if (timer > spawnRate) {
            timer -= spawnRate;

            // Spawn object with random 2D rotation.
            PooledObject instance =
                Pool.Instance.Spawn(projectile, transform.position, Quaternion.Euler(0f, 0f, Random.Range(0f, 360f)));
            instance.As<Projectile>().speed = Random.value;
        }
    }
}
