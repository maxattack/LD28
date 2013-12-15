using UnityEngine;
using System.Collections;

public class KittenButton : GameBehaviour {
	public bool oneTime = true;
	public GameObject triggerDelegate;
	public Sprite pressedSprite;
	Sprite unpressedSprite;
	
	public void KittenTrigger() {
		Jukebox.Play("button");
		
		unpressedSprite = GetComponent<SpriteRenderer>().sprite;
		
		var sr = GetComponent<SpriteRenderer>();
		if (oneTime) {
			sr.sprite = pressedSprite;
			Destroy( collider2D );
			Destroy( this );
		} else {
			sr.sprite = sr.sprite == pressedSprite ? unpressedSprite : pressedSprite;
		}
		
		if (triggerDelegate) {
			triggerDelegate.SendMessage("OnTriggered");
		}		
	}
	
}
