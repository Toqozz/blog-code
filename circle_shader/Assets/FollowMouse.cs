using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FollowMouse : MonoBehaviour {

	// Use this for initialization
	void Start () {
		
	}
	
	// Update is called once per frame
	void Update () {
		if (Input.GetMouseButton(0)) {
			Vector3 mouse = Camera.main.ScreenToWorldPoint(Input.mousePosition);
			mouse.z = 0f;
			this.transform.position = mouse; 
		}
	}
}
