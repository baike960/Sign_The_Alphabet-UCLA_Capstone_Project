using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class tutorial_monitor : MonoBehaviour
{
	private Hashtable rounds;
	private Hashtable letterTable;
	public GameObject a;
	public GameObject b;
	public GameObject c;
	public GameObject d;
	public GameObject e;
	public GameObject f;
	public GameObject g;
	public GameObject h;
	public GameObject i;
	public GameObject j;
	public GameObject k;
	public GameObject l;
	public GameObject m;
	public GameObject n;
	public GameObject o;
	public GameObject p;
	public GameObject q;
	public GameObject r;
	public GameObject s;
	public GameObject t;
	public GameObject u;
	public GameObject v;
	public GameObject w;
	public GameObject x;
	public GameObject y;
	public GameObject z;
	public GameObject objectToActivate;
	public bool nextRound;
	public int index;
	public string input;
	public string currentLetter;
	public string[] positiveComments;
	public string[] correctiveComments;

	public Dialogue dialogue;

    void Start()
    {
		positiveComments = new string[3];
		positiveComments[0]="Good job!";
		positiveComments[1]="Just like that! Keep going!";
		positiveComments[2]="Way to go!";

		correctiveComments = new string[6];
		correctiveComments[0]="Oops! Wrong answer. Please try again.";
		correctiveComments[1]="Wrong answer! Try to keep your hand static or adjust lighting conditon!";
		correctiveComments[2]="Remember not to confuse v with k!";//corrective comment for k
		correctiveComments[3]="Remember not to confuse d with z!";//corrective comment for d
		//correctiveComments[4]="Remember not to confuse g with h!";//corrective comment for g
		//correctiveComments[5]="Remember not to confuse h with g!";//corrective comment for h
		rounds = new Hashtable();
		letterTable = new Hashtable ();

		//Create a hashtable for the 26 rounds
		/*
		rounds.Add(0, a);
		rounds.Add(1, b);
		rounds.Add(2, c);
		rounds.Add(3, d);
		rounds.Add(4, e);
		rounds.Add(5, f);
		rounds.Add(6, g);
		rounds.Add(7, h);
		rounds.Add(8, i);
		rounds.Add(9, j);
		rounds.Add(10, k);
		rounds.Add(11, l);
		rounds.Add(12, m);
		rounds.Add(13, n);
		rounds.Add(14, o);
		rounds.Add(15, p);
		rounds.Add(16, q);
		rounds.Add(17, r);
		rounds.Add(18, s);
		rounds.Add(19, t);
		rounds.Add(20, u);
		rounds.Add(21, v);
		rounds.Add(22, w);
		rounds.Add(23, x);
		rounds.Add(24, y);
		rounds.Add(25, z);
		*/
		rounds.Add(0, a);
		rounds.Add(1, b);
		rounds.Add(2, c);
		//rounds.Add(3, d);
		rounds.Add(3, e);
		rounds.Add(4, f);
		rounds.Add(5, g);
		//rounds.Add(7, h);
		rounds.Add(6, i);
		//rounds.Add(9, j);
		rounds.Add(7, k);
		rounds.Add(8, l);
		rounds.Add(9, m);
		//rounds.Add(13, n);
		rounds.Add(10, o);
		rounds.Add(11, p);
		//rounds.Add(16, q);
		rounds.Add(12, r);
		//rounds.Add(18, s);
		//rounds.Add(19, t);
		//rounds.Add(20, u);
		//rounds.Add(21, v);
		//rounds.Add(22, w);
		//rounds.Add(23, x);
		//rounds.Add(24, y);
		rounds.Add(13, z);





		letterTable.Add (0, "a");
		letterTable.Add (1, "b");
		letterTable.Add (2, "c");
		letterTable.Add (3, "e");
		letterTable.Add (4, "f");
		letterTable.Add (5, "g");
		letterTable.Add (6, "i");
		letterTable.Add (7, "k");
		letterTable.Add (8, "l");
		letterTable.Add (9, "m");
		letterTable.Add (10, "o");
		letterTable.Add (11, "p");
		letterTable.Add (12, "r");
		letterTable.Add (13, "z");

		nextRound = false;
		index = 0;

    }

    // Update is called once per frame
    void Update()
    {
		//if the game signals to go to next round, show the next letter and continue the game
		if (nextRound) {
			objectToActivate=(GameObject)rounds[index];
			objectToActivate.SetActive(true);
			//currentLetter = (char)('a' + (char)index)+"";
			currentLetter=(string)letterTable[index];
			Debug.Log (currentLetter);
			index++;
			nextRound = false;//set it false so the current letter does not change
		}

    }

	public void checkCorrectness(){
		input= GameObject.Find ("MQTT_controller").GetComponent<MQTT_received> ().letter_received;
		//check correctness of gesture input by user
		//if (Input.GetKeyDown (KeyCode.A)) {
			//if correct, provide positive feedback and move on to next round
			if (input == currentLetter) {
				objectToActivate.SetActive (false);
				setNextRound ();
				setDiaglog (dialogue, true);
			} else {
				setDiaglog (dialogue, false);
			}
		//}
	}
			
	//set the queue of sentences for the dialogue box
	public void setDiaglog(Dialogue dialogue, bool isCorrect){
		dialogue.sentences = new string[1];
		if (isCorrect) {
			if (currentLetter == "z") {
				dialogue.sentences [0] = "Congratulations on finishing the tutorial! Go on to the real challenge!";
			}
			else
				dialogue.sentences [0] = positiveComments [Random.Range (0, 3)];
		} 
		else {
			if (currentLetter == "k") {
				dialogue.sentences [0] = correctiveComments [2];
			} 
			else if (currentLetter == "d") {
				dialogue.sentences [0] = correctiveComments [3];
			} 
			/*
			else if (currentLetter == "g") {
				dialogue.sentences [0] = correctiveComments [4];
			} 
			else if (currentLetter == "h") {
				dialogue.sentences [0] = correctiveComments [5];
			} 
			*/
			else {
				dialogue.sentences [0] = correctiveComments [Random.Range (0, 2)];
			}
		}
		FindObjectOfType<DialogueManager> ().StartDialogue (dialogue);
	}
	public void setNextRound(){
		nextRound = true;
	}
		
}
