using UnityEngine;
using System.Collections;

public class ExplodingBlock : GameBehaviour {

	public GameObject explosionPrefab;

	public void OnTriggered() {
		Dup(explosionPrefab, transform.position);
		Destroy(gameObject);
	}
	
}
