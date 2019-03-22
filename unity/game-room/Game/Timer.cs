using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class Timer : MonoBehaviour {

    public Text timerText;
    private float startTime;

	public bool resetlevel_var;

	void Start () {
        startTime = Time.time;
	}
	
	// Update is called once per frame
	void Update () {

		//resetlevel_var = GameObject.Find ("LevelResetController").GetComponent<ResetLevel> ().reset_var; //Grab Variable from "Reset Level" Script

		if (resetlevel_var == true) {
			startTime = Time.time; //Reset Player Timer if necessary
		}

		float t = Time.time - startTime; //Update timer
   
        string minutes= ((int) t/ 60).ToString();
        string seconds = (t % 60).ToString("f2");

		timerText.text = minutes + ":" + seconds;

			
	}
}
