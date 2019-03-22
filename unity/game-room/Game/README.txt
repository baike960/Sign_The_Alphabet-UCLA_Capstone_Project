album.cs: includes a scene manager function that switches the scene to the slides room when triggered.

button.cs: this file includes all the codes necessary to sustain the game mechanisms in the game room, including processing data received from the server, keep tracking of the score based on rules, generate the dictionary of words and randomly picking one for each turn, changing the words displayed on the input and output bars, running the timers, etc. The script is triggered by a push button, which will be replaced by voice processing later.

DialogueManager.cs: the main controller of the dialogue system that includes all the main functions such as StartDialogue and DisplayNextSentence.

DialogueTrigger.cs: triggers the StartDialogue function.

Dialogue.cs: a cache that stores all the sentences to be displayed in the dialogue box.

exit.cs: this file contains scene-switching functions that load designated scenes.

WayPoints.cs: this files contains functions that control the movement of the main character in the main menu, who moves in a triangular path across three game rooms.

MQTT_received.cs: this file imports the Unity's built-in MQTT library to allow for communication between computers via TCP/IP. It creates a "letter" channel and a "command" channel that receives and publishes information.

MQTT_menu.cs: same function as MQTT_received.cs, but is made specially for the menu.

Timer.cs: contains a timer that records time.

Prompt.cs: this file uses coroutine to set a prompt bar active for 3 seconds.

movement.cs: this file serves the same function as WayPoint.cs, except it allows the character to move in all directions instead of on fixed path.

tutorial_monitor.cs: a central controller that constantly monitors every change taking place in the tutorial room and responds accordingly.


