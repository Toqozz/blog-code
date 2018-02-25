using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class UIController : MonoBehaviour {
    public float crosshairLineWidth = 2.0f;
    public bool crosshairFollowsCursor = true;
    public Image crosshairObject;
    public Transform crosshairLine;
    public Transform player;

    private RectTransform crosshairRect;
    private CanvasScaler scaler;

	void Start () {
        scaler = GetComponent<CanvasScaler>();
        crosshairRect = crosshairLine.GetComponent<RectTransform>();
	}
	
	void Update () {
        // Get position of clicked pixel in world space, and adjust for the camera position.

        // Instantiate the prefab where the player is.

        //Debug.DrawRay(transform.position, posWorld, Color.red, 10.0f);

        if (crosshairFollowsCursor) {
            // Get mouse position vector.
            Vector3 mousePos = Input.mousePosition;

            // Simply move the crosshair to a mouse position.
            crosshairObject.transform.position = mousePos;

            Vector3 playerPosScreen = Camera.main.WorldToScreenPoint(player.transform.position);

            Vector2 difference = (mousePos - playerPosScreen);
            float length = difference.magnitude;


            // Needed if we're using a canvasscaler to scale the UI.
            Vector2 scaleMultiplier = new Vector2(scaler.referenceResolution.x / Screen.width, scaler.referenceResolution.y / Screen.height);

            // Adjust canvas accordingly...
            crosshairRect.sizeDelta = new Vector2(length * scaleMultiplier.x, crosshairLineWidth * scaleMultiplier.y);
            crosshairRect.pivot = new Vector2(0f, 0.5f);
            crosshairRect.position = playerPosScreen;

            float angle =  Mathf.Atan2(difference.y, difference.x) * Mathf.Rad2Deg;
            crosshairRect.rotation = Quaternion.Euler(0, 0, angle);

            //Debug.DrawLine(playerPosScreen, mousePos, Color.red, 10.0f);
        }
	}
}
