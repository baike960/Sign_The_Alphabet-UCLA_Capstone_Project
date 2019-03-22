using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class album : MonoBehaviour
{
	void OnMouseOver()
	{
		Debug.Log("on-top");
		if (Input.GetMouseButtonUp(0)){
			Debug.Log("clicked");
			SceneManager.LoadScene("album");
		}

	}
}
