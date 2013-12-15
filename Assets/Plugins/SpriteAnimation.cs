using UnityEngine;
using System.Collections;

[RequireComponent(typeof(SpriteRenderer))]
public class SpriteAnimation : CustomBehaviour {
	
	// designer parameters
	public Sprite[] frames;
	public int[] loopIndices;
	public float fps = 10f;
	public bool once = false;
	
	// cached references
	SpriteRenderer spr;

	void Awake() {
		spr = GetComponent<SpriteRenderer>();
		if (loopIndices.Length > 0) {
			if (once) {
				StartCoroutine(DoOnceAndDestroy());
			} else {
				StartCoroutine(DoLoop());
			}
		}
	}
	
	IEnumerator DoOnceAndDestroy() {
		float totalTime = loopIndices.Length / fps;
		int frame = -1;
		for(float t=0f; t<totalTime; t+=Time.deltaTime) {
			int nextFrame = Mathf.FloorToInt(t * fps);
			if (frame != nextFrame) {
				SetFrame(loopIndices[nextFrame]);
				frame = nextFrame;
			}
			yield return null;
		}
		Destroy(gameObject);
	}
	
	IEnumerator DoLoop() {
		float totalTime = loopIndices.Length / fps;
		int frame = -1;
		for(;;) {
			for(float t=0f; t<totalTime; t+=Time.deltaTime) {
				int nextFrame = Mathf.FloorToInt(t * fps);
				if (frame != nextFrame) {
					SetFrame(loopIndices[nextFrame]);
					frame = nextFrame;
				}
				yield return null;
			}
		}
	}
	
	public void SetFrame(int i) {
		spr.sprite = frames[Mathf.Clamp(i, 0, frames.Length-1)];
	}
}
