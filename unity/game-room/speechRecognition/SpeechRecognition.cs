using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using uPLibrary.Networking.M2Mqtt;
using uPLibrary.Networking.M2Mqtt.Messages;
using uPLibrary.Networking.M2Mqtt.Utility;
using uPLibrary.Networking.M2Mqtt.Exceptions;

namespace UnityWebGLSpeechDetection
{
	public class SpeechRecognition : MonoBehaviour
	{
		/// <summary>
		/// Reference to the text that displays detected words
		/// </summary>
		public Text _mTextDictation = null;

		/// <summary>
		/// Reference to the example text summary
		/// </summary>
		public Text _mTextSummary = null;

		/// <summary>
		/// Reference to a warning if plugin is not available
		/// </summary>
		public Text _mTextWaiting = null;

		/// <summary>
		/// Reference to the proxy
		/// </summary>
		private ISpeechDetectionPlugin _mSpeechDetectionPlugin = null;

		/// <summary>
		/// Reference to the supported languages and dialects
		/// </summary>
		private LanguageResult _mLanguageResult = null;

		/// <summary>
		/// List of detected words
		/// </summary>
		private List<string> _mWords = new List<string>();

		/// <summary>
		/// String builder to format the dictation text
		/// </summary>
		private StringBuilder _mStringBuilder = new StringBuilder();



		// Set up proxy

		public Button _mButtonCloseBrowserTab = null;

		public Button _mButtonCloseProxy = null;

		public Button _mButtonLaunchProxy = null;

		public Button _mButtonOpenBrowserTab = null;

		public Button _mButtonSetProxyPort = null;

		public InputField _mInputPort = null;

		public string receivedWord;
		public GameObject reminder;
		public GameObject start;
		public GameObject window;
		public GameObject continueButton;
		public GameObject signal;
		public GameObject error;
		private MqttClient client;

		/// <summary>
		/// Set the starting UI layout
		/// </summary>
		private void Awake()
		{
			// no need to display the summary if the plugin is missing
			SpeechDetectionUtils.SetActive(false,
				_mTextDictation, _mTextSummary);
			
		}

		// Use this for initialization
		IEnumerator Start()
		{
			receivedWord = "";
			// get the singleton instance
			_mSpeechDetectionPlugin = ProxySpeechDetectionPlugin.GetInstance();

			// check the reference to the plugin
			if (null == _mSpeechDetectionPlugin)
			{
				Debug.LogError("Proxy Speech Detection Plugin is not set!");
				yield break;
			}

			// wait for plugin to become available
			while (!_mSpeechDetectionPlugin.IsAvailable())
			{
				yield return null;
			}

			// no need to display a warning if the plugin is available
			SpeechDetectionUtils.SetActive(false, _mTextWaiting);

			// display the UI controls
			SpeechDetectionUtils.SetActive(true,
				_mTextDictation, _mTextSummary);

			// subscribe to events
			_mSpeechDetectionPlugin.AddListenerOnDetectionResult(HandleDetectionResult);

			// set the port number
			int port = 83;
			_mSpeechDetectionPlugin.ManagementSetProxyPort(port);

			// check the reference to the plugin
			if (null != _mSpeechDetectionPlugin)
			{
				// launch the proxy
				_mSpeechDetectionPlugin.ManagementLaunchProxy();
			}

			// open the browser to show the proxy
			_mSpeechDetectionPlugin.ManagementOpenBrowserTab();


		}

		/// <summary>
		/// Handler for speech detection events
		/// </summary>
		/// <param name="detectionResult"></param>
		/// <returns></returns>
		bool HandleDetectionResult(DetectionResult detectionResult)
		{
			
			if (null == detectionResult)
			{
				return false;
			}
			SpeechRecognitionResult[] results = detectionResult.results;
			if (null == results)
			{
				return false;
			}
			foreach (SpeechRecognitionResult result in results)
			{
				SpeechRecognitionAlternative[] alternatives = result.alternatives;
				if (null == alternatives)
				{
					continue;
				}
				foreach (SpeechRecognitionAlternative alternative in alternatives)
				{
					if (string.IsNullOrEmpty(alternative.transcript))
					{
						continue;
					}

					string lower = alternative.transcript.ToLower();
					Debug.LogFormat("Detected: {0}", lower);

					if (result.isFinal)
					{
						_mWords.Add(string.Format("[FINAL] \"{0}\" Confidence={1}", 
							alternative.transcript,
							alternative.confidence));
						
						//voice processing!!!!!!!!!!!!!!!!
						receivedWord = alternative.transcript;
						string[] receivedWords = receivedWord.Split (' ');
						receivedWord = receivedWords [0];
						receivedWord = classifier (receivedWord);
						UIController obj_reminder=reminder.GetComponent<UIController>();
						DialogueTrigger obj_start = start.GetComponent<DialogueTrigger> ();
						button obj_button = start.GetComponent<button> ();
						DialogueManager obj_continue = continueButton.GetComponent<DialogueManager> ();
						MQTT_received obj_mqtt=signal.GetComponent<MQTT_received>();
						if (receivedWord == "reminder") {
							obj_reminder.Show ();
						} else if (receivedWord == "close") {
							obj_reminder.Hide ();
						} else if (receivedWord == "start") {
							
							obj_button.generateWord (window);
						} else if (receivedWord == "compare") {
							obj_button.checkCorrectnessOfLetter ();
						} else if (receivedWord == "next") {
							obj_continue.DisplayNextSentence ();
						} else if (receivedWord == "exit") {
							error.SetActive (true);

							SceneManager.LoadScene ("Sample");
						} else if (receivedWord == "obtain") {
							obj_mqtt.sendCommand ();
						} else if (receivedWord == "waitress") {
							obj_start.TriggerDialogue ();
						}

					}
					else
					{
						_mWords.Add(string.Format("\"{0}\" Confidence={1}",
							alternative.transcript,
							alternative.confidence));
					}
				}
			}

			while (_mWords.Count > 15)
			{
				_mWords.RemoveAt(0);
			}

			if (_mTextDictation)
			{
				if (_mStringBuilder.Length > 0)
				{
					_mStringBuilder.Remove(0, _mStringBuilder.Length);
				}
				foreach (string text in _mWords)
				{
					_mStringBuilder.AppendLine(text);
				}
				_mTextDictation.text = _mStringBuilder.ToString();
				Debug.Log (_mTextDictation.text);

			}

			// dictation doesn't need to handle the event
			return false;
		}

		IEnumerator Example()
		{
			print(Time.time);
			yield return new WaitForSeconds(3);
			print(Time.time);
		}

		public static int edit_distance(string first, string second)
		{
			// Get the length of both.  If either is 0, return
			// the length of the other, since that number of insertions
			// would be required.

			int n = first.Length, m = second.Length;
			if (n == 0) return m;
			if (m == 0) return n;


			// Rather than maintain an entire matrix (which would require O(n*m) space),
			// just store the current row and the next row, each of which has a length m+1,
			// so just O(m) space. Initialize the current row.

			int curRow = 0, nextRow = 1;
			int[][] rows = new int[][] { new int[m + 1], new int[m + 1] };
			for (int j = 0; j <= m; ++j) rows[curRow][j] = j;

			// For each virtual row (since we only have physical storage for two)
			for (int i = 1; i <= n; ++i)
			{
				// Fill in the values in the row
				rows[nextRow][0] = i;
				for (int j = 1; j <= m; ++j)
				{
					int dist1 = rows[curRow][j] + 1;
					int dist2 = rows[nextRow][j - 1] + 1;
					int dist3 = rows[curRow][j - 1] +
						(first[i - 1].Equals(second[j - 1]) ? 0 : 1);
					rows[nextRow][j] = System.Math.Min(dist1, System.Math.Min(dist2, dist3));
				}

				// Swap the current and next rows
				if (curRow == 0)
				{
					curRow = 1;
					nextRow = 0;
				}
				else
				{
					curRow = 0;
					nextRow = 1;
				}
			}
			// Return the computed edit distance
			return rows[curRow][m];
		}

		public static string classifier(string str)
		{
			string sub_x = "x";
			string sub_pa = "pa";
			string sub_clo = "clo";
			string sub_tar = "tar";
			string sub_re = "re";
			string sub_tai = "tai";
			string sub_wai = "wai";
			string sub_ne = "ne";
			if(str == "start" || str == "obtain" || str == "close"|| str == "reminder"|| str == "compare" || str == "exit" || str == "waitress" || str == "next") {
				return str;
			}
			else {
				if (str.Contains (sub_x) && edit_distance (str, "exit") <= 2) {
					return "exit";
				} else if (str.Contains (sub_pa) && edit_distance (str, "compare") <= 5 || str=="pair") {
					return "compare";
				} else if (str.Contains (sub_clo) && edit_distance (str, "close") <= 3) {
					return "close";
				} else if (str.Contains (sub_tar) && edit_distance (str, "start") <= 3) {
					return "start";
				} else if (str.Contains (sub_re) && edit_distance (str, "reminder") <= 3) {
					return "reminder";
				} else if (str.Contains (sub_tai) && edit_distance (str, "obtain") <= 3) {
					return "obtain";
				} else if (str.Contains (sub_wai) && edit_distance (str, "waitress") <= 3) {
					return "waitress";
				} else if (str.Contains (sub_ne) && edit_distance (str, "next") <= 2) {
					return "next";
				}
				else{
						// Return original word in this case. The user probably didn't say the keyword.
						return str;
					}
				}
		}
	}
}