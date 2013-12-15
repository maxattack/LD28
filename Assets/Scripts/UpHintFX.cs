using UnityEngine;
using System.Collections;

public class UpHintFX : GameBehaviour {

	// cached references
	Transform fx;
	SpriteRenderer spr;
	
	// effects parameters
	Vector3 restPosition;
	
	void Awake() {
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
