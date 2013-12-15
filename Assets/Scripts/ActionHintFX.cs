using System.Collections;
using UnityEngine;

public class ActionHintFX : GameBehaviour {
	
	// cached references
	Transform[] nodes;
	SpriteRenderer[] sprites;
	
	// effects parameters
	Vector3[] restPositions;
	
	void Awake() {
		sprites = GetComponentsInChildren<SpriteRenderer>();
		nodes = new Transform[sprites.Length];
		restPositions = new Vector3[sprites.Length];
		
		for(int i=0; i<sprites.Length; ++i) {
			nodes[i] = sprites[i].transform;
			restPositions[i] = nodes[i].localPosition;
		}
	}
	
	void OnEnable() {
		for(int i=0; i<sprites.Length; ++i) {
			sprites[i].color = rgba(1,1,1,0);
			nodes[i].localPosition = vec(restPositions[i].x,0,0);
		}		
	}
	
	void Update() {
		if (!Hero.inst.inventoryItem) {
			gameObject.SetActive(false);
		} else {
			for(int i=0; i<sprites.Length; ++i) {
				sprites[i].color = rgba(1, 1, 1, sprites[i].color.a.EaseTowards(1, 0.15f));
				nodes[i].localPosition = nodes[i].localPosition.EaseTowards(restPositions[i], 0.175f);
			}
		}
	}

}
