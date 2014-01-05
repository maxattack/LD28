using UnityEngine;
using System.Collections;

public class Printer : MonoBehaviour {

	void OnTriggerEnter2D(Collider2D other) {
		print("Colliding with: " + other);
	}
}
