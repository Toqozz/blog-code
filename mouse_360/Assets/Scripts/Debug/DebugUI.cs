using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class DebugUI : MonoBehaviour {
	public MouseDebug mouseDebug;

	public Text textCrosshair,
				textAngle,
				textLenPixels,
				textLenScreenInches,
				textLenPadInches;

	// Use this for initialization
	void Start () {
	}
	
	// Update is called once per frame
	void Update () {
		textCrosshair.text = "+";
		textAngle.text = "Angle: " + mouseDebug.GetAngleX() + " degrees.";
		textLenPixels.text = "Length (pixels): " + mouseDebug.GetLength("pixels") + " pixels";
		textLenScreenInches.text = "Length (screen): " + mouseDebug.GetLength("screeninches") + " inches";
		textLenPadInches.text = "Length (mousepad): " + mouseDebug.GetLength("padinches") + " inches";
	}
}
