using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BenchmarkProjectile : MonoBehaviour {
    public float speed = 1f;

    //private PooledObject pooledObject;

    private float timer = 0f;
    private Vector2 up = Vector2.up;

/*
    private void Awake() {
        pooledObject = GetComponent<PooledObject>();
    }
    */

    private void Update() {
        transform.Translate(up * speed * Time.deltaTime);

        timer += Time.deltaTime;
        if (timer > 2f) {
            timer = 0f;
            //pooledObject.Finish();
            Destroy(gameObject);
        }
    }
}