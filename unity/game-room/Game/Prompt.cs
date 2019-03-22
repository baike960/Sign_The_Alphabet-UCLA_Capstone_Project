using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Prompt : MonoBehaviour
{
	public GameObject objectToActivate;

	private void Start()
	{
		StartCoroutine(ActivationRoutine());
	}

	private IEnumerator ActivationRoutine()
	{       
		//Turn My game object that is set to false(off) to True(on).
		yield return new WaitForSeconds(1);

		objectToActivate.SetActive(true);

		//Wait for 5 secs.
		yield return new WaitForSeconds(5);


		//Game object will turn off
		objectToActivate.SetActive(false);
	}
}
