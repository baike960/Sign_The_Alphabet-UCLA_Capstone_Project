using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class exit : MonoBehaviour
{
	//public GameObject error;
    void OnMouseOver()
    {
        Debug.Log("on-top");
        if (Input.GetMouseButtonUp(0)){
            Debug.Log("clicked");
			//error.SetActive (true);

            SceneManager.LoadScene("Sample");
        }

    }

}
