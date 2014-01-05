using UnityEngine;
using System.Collections;

public class CameraPanIn : GameBehaviour {

	IEnumerator Start () {
		if (!Application.isEditor) {
			var onlyOne = GameObject.Find("OnlyOne");
			onlyOne.SetActive(false);
			var camNode = Camera.main.transform;
			var rest = camNode.position;
			foreach(var u in Interpolate(1f)) {
				var v = 1f - EaseOut4(u);
				camNode.position = rest + vec(0, 8f * v, 0);
				yield return null;
			}
			onlyOne.SetActive(true);
		}
		Destroy(gameObject);
	}
	
}
