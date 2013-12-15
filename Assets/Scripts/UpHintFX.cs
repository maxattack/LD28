using UnityEngine;
using System.Collections;

public class UpHintFX : GameBehaviour {

	Transform node;
	Transform fx;
	SpriteRenderer spr;
	Vector3 restPosition;
	
	void Awake() {
		node = this.transform;
		spr = GetComponentInChildren<SpriteRenderer>();
		fx = spr.transform;
		restPosition = fx.localPosition;
	}
	
	void OnEnable() {
		spr.color = rgba(1, 1, 1, 0);
		fx.localPosition = Vector3.zero;
	}
	
	void Update() {
		spr.color = rgba(1, 1, 1, spr.color.a.EaseTowards(1, 0.15f));
		fx.localPosition = fx.localPosition.EaseTowards(restPosition, 0.175f);
	}

}
