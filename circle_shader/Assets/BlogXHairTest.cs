using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BlogXHairTest : MonoBehaviour {
    //public AnimationCurve accuracyCurve;

    private Transform player;
    private Renderer rend;

	// Use this for initialization
	void Start () {
        this.player = GameObject.FindGameObjectWithTag("Player").transform;
	}
	
	// Update is called once per frame
	void Update () {
        //Vector3 tempPos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        //tempPos.z = 0.0f;
        Vector3 mousePos = GameManager.instance.lookPosition;
        this.transform.position = Camera.main.WorldToScreenPoint(mousePos);
        //this.transform.position = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        //Debug.Log(Camera.main.ScreenToWorldPoint(Input.mousePosition));
        //float dist = (this.transform.position - this.player.transform.position).magnitude;
        float val = Mathf.Min(GameManager.instance.lookDistance / 10.0f, 2.0f);
        
        //Debug.Log(val);
		this.transform.localScale = new Vector3(val,val,1);
	}
}