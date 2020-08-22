using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Projectile : MonoBehaviour {
    public float speed;

    private PooledObject pooledObject;

    private void Awake() {
        pooledObject = GetComponent<PooledObject>();
    }

    private void Update() {
        transform.Translate(Vector2.up * speed * Time.deltaTime);
    }

    private void OnTriggerEnter2D(Collider2D other) {
        if (!other.CompareTag("Boundary")) {
            return;
        }

        GetComponent<PooledObject>().Finish();
        //Destroy(gameObject);
    }
}