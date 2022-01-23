using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using UnityEngine;
using Tobii.Gaming;

public class UI : MonoBehaviour {

    public float x;
    public float y;

    Text text;
    
    void Start () {
        x = 0.0f;
        y = 0.0f;
        text = GetComponent<Text>();
    }
	
	void Update () {
        GazePoint gazePoint = TobiiAPI.GetGazePoint();

        if (gazePoint.IsRecent())
        {
            //text.text = "x: " + (gazePoint.Screen.x).ToString("0.00") + System.Environment.NewLine + "y: " + (gazePoint.Screen.y).ToString("0.00");
            text.text = "";
        }
        else
        {
            text.text = "No gaze";
        }
    }
}
