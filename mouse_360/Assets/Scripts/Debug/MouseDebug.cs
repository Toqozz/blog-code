using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MouseDebug : MonoBehaviour {
    public float mouseDPI = 800f;
    public float sensitivityX = 15F;
    public float sensitivityY = 15F;
    public float minimumX = -360F;
    public float maximumX = 360F;
    public float minimumY = -60F;
    public float maximumY = 60F;
    float rotationX = 0F;
    float rotationY = 0F;
    Quaternion originalRotation;
    private float previousRotationY;
    private float previousRotationX;
	public Material material;

	private float mouseXPos;
	private Vector2 mousePos;
	private Vector3 lastMousePos;
	private float distanceInPixelsTraveled;
	private float distanceInInchesTraveled;
	private float physicalDistanceInInchesTraveled;

	private bool stop = false;

	// Use this for initialization
	void Start () {
        originalRotation = transform.localRotation;
	}
	
	// Update is called once per frame
	void Update () {
		if (!this.stop) {
			MouseTestUpdate();
			MouseLookUpdate();
		}
	}

	private void MouseTestUpdate() {
		// Mouse position on the screen (used for crosshair thing).
		mousePos = Input.mousePosition;
		// Get the mouse delta -- 0.05 = 1 pixel worth of movement, so multiply by 20.
		mouseXPos = Input.GetAxisRaw("Mouse X");
		if (mouseXPos < 0f) {
			mouseXPos = 0f;
		}
		this.distanceInPixelsTraveled += Mathf.Abs(mouseXPos * 20f);

		// Figure out how many physical inches the mouse cursor has traveled.
		this.distanceInInchesTraveled = this.distanceInPixelsTraveled / Screen.dpi;
		// Figure out how many physical inches the mouse has traveled.
		//this.physicalDistanceInInchesTraveled = this.distanceInInchesTraveled / (800 / Screen.dpi);
		this.physicalDistanceInInchesTraveled = this.distanceInPixelsTraveled / mouseDPI;

		// Reset...
		if (Input.GetKey(KeyCode.Space)) {
			this.distanceInPixelsTraveled = 0f;
			this.rotationX = 0f;
			this.stop = false;
		}
	}

	private void MouseLookUpdate() {
        //Gets rotational input from the mouse
        //rotationY += Input.GetAxisRaw("Mouse Y") * sensitivityY;
		float mousemove = Input.GetAxisRaw("Mouse X") * sensitivityX;
        //rotationX += Input.GetAxisRaw("Mouse X") * sensitivityX;
        rotationX += mousemove < 0 ? 0 : mousemove;

        //Clamp the rotation average to be within a specific value range
        //rotationY = ClampAngle(rotationY, minimumY, maximumY);
		if (rotationX >= 360f) {
        	rotationX = ClampAngle(rotationX, minimumX, maximumX);
			this.stop = true;
		}
           
        //Get the rotation you will be at next as a Quaternion
        //Quaternion yQuaternion = Quaternion.AngleAxis(rotationY, Vector3.left);
        Quaternion xQuaternion = Quaternion.AngleAxis(rotationX, Vector3.up);
           
        //Rotate
        Camera.main.transform.localRotation = originalRotation * xQuaternion;// * yQuaternion;
	}

	public float GetLength(string which) {
		switch(which) {
			case "pixels": return this.distanceInPixelsTraveled;
			case "screeninches": return this.distanceInInchesTraveled;
			case "padinches": return this.physicalDistanceInInchesTraveled;
			default: return 0f;
		}
	}

	public float GetAngleX() {
		return rotationX;
	}

	public void SetSensitivity(string sensitivity) {
		//this.sensitivityX = float.Parse(sensitivity) / 2.2727f;
		//this.sensitivityY = float.Parse(sensitivity) / 2.2727f;
		this.sensitivityX = 1f;
		this.sensitivityY = 1f;

	}

    public static float ClampAngle (float angle, float min, float max) {
        angle = angle % 360;
        if ((angle >= -360F) && (angle <= 360F)) {
            if (angle < -360F) {
                angle += 360F;
            }
            if (angle > 360F) {
                angle -= 360F;
            }        
        }
        return Mathf.Clamp (angle, min, max);
    }


	void OnPostRender() {
		DrawLine(new Vector3(0, .5f, 0), new Vector3(1, .5f, 0));
		DrawLine(new Vector3(.5f, 0, 0), new Vector3(.5f, 1, 0));

		DrawLine(new Vector3(0, mousePos.y / Screen.height, 0), new Vector3(1, mousePos.y / Screen.height, 0));
		DrawLine(new Vector3(mousePos.x / Screen.width, 0, 0), new Vector3(mousePos.x / Screen.width, 1, 0));
	}

	private void DrawLine(Vector3 start, Vector3 end) {
		GL.PushMatrix();
		this.material.SetPass(0);
		GL.LoadOrtho();
		GL.Begin(GL.LINES);
		GL.Vertex(start);
		GL.Vertex(end);
		GL.End();
		GL.PopMatrix();
	}
}
