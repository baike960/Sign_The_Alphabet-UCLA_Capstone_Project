using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class WayPoints : MonoBehaviour {

	// put the points from unity interface
	public Transform[] wayPointList;

	public int currentWayPoint = 0; 
	Transform targetWayPoint;

	public float speed = 4f;

	public GameObject signal;

	public bool isMoving=true;

	public GameObject loadBox;



	// Use this for initialization
	void Start () {

	}

	// Update is called once per frame
	void Update () {
			targetWayPoint = wayPointList[currentWayPoint];
		    //first move the character to position 0
			transform.position = Vector3.MoveTowards(transform.position, targetWayPoint.position,   speed*Time.deltaTime);
			walk();
	}

	void walk(){
		// rotate towards the target
		//transform.forward = Vector3.RotateTowards(transform.forward, targetWayPoint.position - transform.position, speed*Time.deltaTime, 0.0f);

		// move towards the target
		MQTT_menu obj=signal.GetComponent<MQTT_menu>();
		if (obj.letter_received == "m") {
			if (currentWayPoint == 2)
				currentWayPoint = 0;
			else
				currentWayPoint++;
			targetWayPoint = wayPointList [currentWayPoint];
			obj.letter_received = "";

		} 


		/*
		if(obj.letter_received=="down"&& currentWayPoint==0)
		{
			targetWayPoint=wayPointList[1];
			currentWayPoint=1;
		}
		else if(obj.letter_received=="right"&& currentWayPoint==1)
		{
			targetWayPoint=wayPointList[2];
			currentWayPoint=2;
		}
		else if(obj.letter_received=="up"&& currentWayPoint==2)
		{
			targetWayPoint=wayPointList[0];
			currentWayPoint=0;
		}
		*/

		transform.position = Vector3.MoveTowards(transform.position, targetWayPoint.position,   speed*Time.deltaTime);

		//Enter scenes when space is pressed
		if (obj.letter_received=="e" && currentWayPoint == 0) 
		{
			//Debug.Log ("entering scene1");
			//SceneManager.LoadScene ("Scene1");
		}
		else if (obj.letter_received=="e" && currentWayPoint == 1) 
		{
			Debug.Log ("entering scene2");
			loadBox.SetActive(true);
			SceneManager.LoadScene ("Scene2");
		}
		else if (obj.letter_received=="e" && currentWayPoint == 2) 
		{
			Debug.Log ("entering scene3");
			loadBox.SetActive(true);
			SceneManager.LoadScene ("tutorialRoom");
		}
		/*
		if(transform.position == targetWayPoint.position)
		{
			currentWayPoint ++ ;
			if (currentWayPoint >= this.wayPointList.Length)
				currentWayPoint = 0;
			targetWayPoint = wayPointList[currentWayPoint];
		}
		*/
	} 
}
