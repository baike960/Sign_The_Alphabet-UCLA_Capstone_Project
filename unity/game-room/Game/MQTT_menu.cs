using UnityEngine;
using System.Collections;
using System.Net;
using uPLibrary.Networking.M2Mqtt;
using uPLibrary.Networking.M2Mqtt.Messages;
using uPLibrary.Networking.M2Mqtt.Utility;
using uPLibrary.Networking.M2Mqtt.Exceptions;

using System;

public class MQTT_menu : MonoBehaviour {

	public string letter_received;
	private MqttClient client;
	// Use this for initialization
	void Start () {
		// create client instance 
		client = new MqttClient(IPAddress.Parse("127.0.0.1"),1883 , false , null ); 

		// register to message received 
		client.MqttMsgPublishReceived += client_MqttMsgPublishReceived; 

		string clientId = Guid.NewGuid().ToString(); 
		client.Connect(clientId); 

		// subscribe to the topic "/home/temperature" with QoS 2 
		client.Subscribe(new string[] { "IMU" }, new byte[] { MqttMsgBase.QOS_LEVEL_EXACTLY_ONCE }); 

	}

	// Update is called once per frame
	void FixedUpdate () {
		if (Input.GetKeyDown (KeyCode.Space)) {
			Debug.Log("sending...");
			client.Publish("command", System.Text.Encoding.UTF8.GetBytes("1"), MqttMsgBase.QOS_LEVEL_EXACTLY_ONCE, true);
			Debug.Log("sent");
		}

	}

	void client_MqttMsgPublishReceived(object sender, MqttMsgPublishEventArgs e) 
	{ 

		Debug.Log("Received: " + System.Text.Encoding.UTF8.GetString(e.Message));
		letter_received=System.Text.Encoding.UTF8.GetString(e.Message);
	} 
	/*
	void OnGUI(){
		if ( GUI.Button (new Rect (20,40,80,20), "Level 1")) {
			Debug.Log("sending...");
			client.Publish("letter", System.Text.Encoding.UTF8.GetBytes("Ready for next letter!"), MqttMsgBase.QOS_LEVEL_EXACTLY_ONCE, true);
			Debug.Log("sent");
		}
	}
	*/

}
