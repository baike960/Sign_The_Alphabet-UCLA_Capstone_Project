using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System.IO;
using UnityEngine.UI;

public class button : MonoBehaviour {
    //public GameObject a;
	public GameObject button2;
	/*
	public int v1,v2;
	public int expectedValue;
    public string readValue;
	public GameObject rightSign;
	public GameObject wrongSign;
	public bool checkOutput;//Checking point to make sure the game gives feedback only once
	*/

	//New version members
	private Hashtable htLevel1;
	public int lengthOfWord;
	public int indexOfWord;
	public string currentLetter;
	public string word;
	public Text outputText;
	public Text score;
	public Text levelText;
	public bool letterAttempted;
	public AudioSource audioData;
	public int countMistakesForLetter;//count mistakes for current letter
	public int countMistakesForWord;//count mistaken letters for the word 
	public int currentScore;//current score for the word 
	public int level;


	private Hashtable letterReminder;
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

	public Dialogue dialogue;



	void Start(){
		//make the dictionary
		htLevel1 = new Hashtable();
		letterReminder = new Hashtable ();

		htLevel1.Add(0, "bagel");
		htLevel1.Add(1, "pizza");
		htLevel1.Add(2, "meal");
		htLevel1.Add(3, "fork");
		htLevel1.Add(4, "coffee");
		htLevel1.Add(5, "pork");
		htLevel1.Add(6, "beef");
		htLevel1.Add(7, "egg");
		htLevel1.Add(8, "milk");

		//Create a hashtable for the 26 reminder windows
		letterReminder.Add("a", a);
		letterReminder.Add("b", b);
		letterReminder.Add("c", c);
		letterReminder.Add("d", d);
		letterReminder.Add("e", e);
		letterReminder.Add("f", f);
		letterReminder.Add("g", g);
		letterReminder.Add("h", h);
		letterReminder.Add("i", i);
		letterReminder.Add("j", j);
		letterReminder.Add("k", k);
		letterReminder.Add("l", l);
		letterReminder.Add("m", m);
		letterReminder.Add("n", n);
		letterReminder.Add("o", o);
		letterReminder.Add("p", p);
		letterReminder.Add("q", q);
		letterReminder.Add("r", r);
		letterReminder.Add("s", s);
		letterReminder.Add("t", t);
		letterReminder.Add("u", u);
		letterReminder.Add("v", v);
		letterReminder.Add("w", w);
		letterReminder.Add("x", x);
		letterReminder.Add("y", y);
		letterReminder.Add("z", z);

		//count spelling mistakes for each letter of the word
		countMistakesForLetter = 0;
		//count total mistakes for the word
		countMistakesForWord = 0;

		indexOfWord = 0;
		letterAttempted = false;

		FindObjectOfType<DialogueManager>().StartDialogue(dialogue);//trigger the dialogue boxes

		level = 1;//set level 1 first;
		levelText.text="Level "+level;

	}

	void FixedUpdate(){
		//checkCorrectnessOfLetter ();

	}

	public void checkCorrectnessOfLetter(){
		currentLetter= GameObject.Find ("MQTT_controller").GetComponent<MQTT_received> ().letter_received;
		//Output differently on the receiver board based on different cases
		//if (Input.GetKeyDown (KeyCode.A)) {
			if (word [indexOfWord] + "" == currentLetter) {

				//add the score gained for this letter to current total score
				if (countMistakesForLetter == 0) {
					currentScore += 10;
				}
				else if(countMistakesForLetter>0 && countMistakesForLetter<=3)
					currentScore+=5;
				
				countMistakesForLetter = 0;//reset the count for next letter

				if (!letterAttempted) {//if current letter is not attempted
					outputText.text += "<color=green>" + currentLetter + "</color>" + "";
				} 
				else {//if current letter is attempted and is wrong
					outputText.text=outputText.text.Remove(outputText.text.Length-20,20)+"<color=green>"+currentLetter + "</color>" + "";
				}
				indexOfWord++;
				//end of word reached
				if (indexOfWord == lengthOfWord) {
					// get 10 points for each letter with no mistake; get 5 points for each if the user makes at least 1 mistake; 
					// if the user makes more than 3 mistakes, no point for this letter
					// 25 points for perfect spelling of words with no mistakes

					if (countMistakesForWord == 0)
						currentScore += 25;
		
					int temp = int.Parse (score.text) + currentScore;
					score.text = temp + "";
					if (temp >= 200) {
						level = 2;//set level 1 first;
						levelText.text="Level "+level;
						levelUpDiaglog (dialogue);
						audioData.Play (0);
						return;

					}

					setDiaglog(dialogue);//display feedback from game

					audioData.Play (0);
				}
				letterAttempted = false;
			} 
			else {//if does not match
				Debug.Log ("nooooo");
				//Show the reminder window for 3 seconds
				objectToActivate=(GameObject)letterReminder[word[indexOfWord]+""];
				StartCoroutine(ActivationRoutine());
				//Increment count of mistakes for the word
				if(countMistakesForLetter==0)
					countMistakesForWord++;
				//Increment mistakes for the letter
				countMistakesForLetter++;


				//Write the wrong letter in red to the output window
				if (!letterAttempted) {//if current letter is not attempted
					outputText.text += "<color=red>" + currentLetter + "</color>" + "";
					letterAttempted = true;
				} 
				else {
					outputText.text=outputText.text.Remove(outputText.text.Length-20,20)+"<color=red>"+currentLetter + "</color>" + "";
				}
			}

		//}
	}

	// Update is called once per frame

	public void generateWord(GameObject window){ 
		currentScore = 0;
		countMistakesForLetter = 0;
		countMistakesForWord = 0;
		outputText.text = "";
		indexOfWord = 0;
		word = htLevel1 [Random.Range (0, 9)]+"";
		lengthOfWord = word.Length;
		window.GetComponent<UnityEngine.UI.Text>().text = word;
	}
		
	public void setDiaglog(Dialogue dialogue){
		dialogue.sentences = new string[1];
		dialogue.sentences [0] = "Here is you "+word+"! Enjoy!";
		FindObjectOfType<DialogueManager> ().StartDialogue (dialogue);//pop up the dialogue box
	}

	public void levelUpDiaglog(Dialogue dialogue){
		dialogue.sentences = new string[1];
		dialogue.sentences [0] = "Level Up! "+"Current Level: "+level;
		FindObjectOfType<DialogueManager> ().StartDialogue (dialogue);//pop up the dialogue box
	}

	private IEnumerator ActivationRoutine()
	{       
		//Turn My game object that is set to false(off) to True(on).
		yield return new WaitForSeconds(1);
		objectToActivate.SetActive(true);
		//Wait for 3 secs.
		yield return new WaitForSeconds(3);
		//Game object will turn off
		objectToActivate.SetActive(false);
	}

}
