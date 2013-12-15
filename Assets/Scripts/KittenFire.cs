using UnityEngine;
using System.Collections;

public class KittenFire : GameBehaviour {
	
	public void KittenTrigger() {	
		Jukebox.Play("death");
		StartCoroutine(DoDeath());
	}
	
	IEnumerator DoDeath() {
		float startTime = Time.realtimeSinceStartup;
		var cnode = Camera.main.transform;
		Vector3 cameraRest = cnode.position;
		var baseColor = Camera.main.backgroundColor;
		while(Time.realtimeSinceStartup - startTime < 1f) {
			float u = Time.realtimeSinceStartup - startTime;
			Time.timeScale = 1 - EaseOut4(u);
			Camera.main.backgroundColor = Color.Lerp(baseColor, Color.red, EaseOut2(u));
			cnode.position = cameraRest + vec(0, 8f * EaseOut2(u), 0);
			yield return null;
		}
		Camera.main.backgroundColor = Color.red;
		cnode.position = cameraRest + vec(0, 8, 0);
		
		while(!(Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter))) {
			yield return null;
		}
		Time.timeScale = 1f;		
		Application.LoadLevel(Application.loadedLevel);
	}
	
}
