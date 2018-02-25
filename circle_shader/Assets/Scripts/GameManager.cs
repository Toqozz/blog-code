using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GameManager : MonoBehaviour {
    [HideInInspector]
    public static GameManager instance;
    [HideInInspector]
    public Vector2 lookDirection;
    [HideInInspector]
    public Vector2 lookPosition;
    [HideInInspector]
    public float lookDistance;
    [HideInInspector]
    public float lookAngle;

    private GameObject player;

    private void CalculateMouseDistanceAndDirection() {
        Vector3 posWorld = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        posWorld.z = 0;
        this.lookPosition = posWorld;

        // Direction from player to mouse position.
        Vector2 direction = (posWorld - this.player.transform.position);
        this.lookDistance = direction.magnitude;
        direction.Normalize();  // .normalized does not normalize up, only down.

        // Save this.
        this.lookDirection = direction;
    }

    private void CalculateMouseRotationAngleZ(Vector2 lookDirection) {
        // Like a point on a circle, figure out the angle based on a vector.
        float angle = Mathf.Atan2(lookDirection.y, lookDirection.x) * Mathf.Rad2Deg;

        // Save this.
        this.lookAngle = angle;
    }

	// Use this for initialization
	void Awake () {
		if (instance == null) {
            instance = this;
        } else if (instance != this) {
            Destroy(this.gameObject);
        }

        DontDestroyOnLoad(this.gameObject);

        this.player = GameObject.FindGameObjectWithTag("Player");
	}
	
	// Update is called once per frame
	void Update () {
        CalculateMouseDistanceAndDirection();
        CalculateMouseRotationAngleZ(this.lookDirection);
	}
}
