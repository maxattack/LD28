using System.Collections;
using UnityEngine;

public class MoveHintFX : GameBehaviour {

	// cached references
	Transform[] nodes;
	SpriteRenderer[] sprites;
	
	// effects parameters
	Vector3[] restPositions;
	
	bool seenLeftRight = false;
	bool seenJump = false;
	
	void Awake() {
		sprites = GetComponentsInChildren<SpriteRenderer>();
		nodes = new Transform[sprites.Length];
		restPositions = new Vector3[sprites.Length];
		
		for(int i=0; i<sprites.Length; ++i) {
			sprites[i].color = rgba(1,1,1,0);
			nodes[i] = sprites[i].transform;
			restPositions[i] = nodes[i].localPosition;
			nodes[i].localPosition = vec(restPositions[i].x,0,0);
		}
	}
	
	void Update() {
		if (!seenJump && Hero.inst.JumpButtonPressed()) {
			seenJump = true;
			for(int i=0; i<nodes.Length; ++i) {
				if (nodes[i].name.Contains("shift")) {
					nodes[i].gameObject.SetActive(false);
				}
			}
		}
		if (!seenLeftRight && (Hero.inst.LeftButtonPressing() || Hero.inst.RightButtonPressing())) {
			seenLeftRight = true;
			for(int i=0; i<nodes.Length; ++i) {
				if (!nodes[i].name.Contains("shift")) {
					nodes[i].gameObject.SetActive(false);
				}
			}
		}
	
	
		if (seenJump && seenLeftRight) {
			Destroy(gameObject);
		} else {
			for(int i=0; i<sprites.Length; ++i) {
				sprites[i].color = rgba(1, 1, 1, sprites[i].color.a.EaseTowards(1, 0.15f));
				nodes[i].localPosition = nodes[i].localPosition.EaseTowards(restPositions[i], 0.175f);
			}
		}
	}

}
