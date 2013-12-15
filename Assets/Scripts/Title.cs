using UnityEngine;
using System.Collections;

public class Title : MonoBehaviour {

	void Update () {
		if (!Application.isLoadingLevel && Input.GetMouseButtonDown(0)) {
			Application.LoadLevel("TestLevel");
		}
	}
}
