using UnityEngine;
using System.Collections;

public class Apology : GameBehaviour {
	public GameObject blocker;
	
	public void KittenTrigger() {
		if (Hero.inst.inventoryItem == null) { return; }
		Jukebox.Play("put");
		blocker.SetActive(true);
		StartCoroutine(DoPan());
		Destroy(collider2D);
	}
	
	IEnumerator DoPan() {
		var cnode = Camera.main.transform;
		var p0 = cnode.position;
		var p1 = transform.position;
		foreach(var u in Interpolate(0.5f)) {
			cnode.position = Vector3.Lerp(p0, p1, EaseOut2(u));
			yield return null;
		}
	}

}
