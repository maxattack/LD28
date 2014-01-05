using UnityEngine;
using System.Collections;

public class Hero : GameBehaviour {
	
	// The player's avatar.  If this was for realsies I
	// would use a kinematic body and write proper character
	// control, but f*ck it, this is a gamejam, so I'm just
	// going to go dynamic and hope for the best.
	
	internal static Hero inst;
	
	// designer parameters
	public float movementSpeed = 1f;
	public float jumpHeight = 2.5f;
	public float animRate = 0.05f;
	public Sprite[] spriteFrames;
	public Vector2 throwVector = vec(1, 1);
	public UpHintFX hint;
	public ActionHintFX actionHint;
	
	// cached references
	//Rigidbody2D body;
	CollisionAABB box;
	Transform node;
	Transform fx;
	SpriteRenderer spriteFx;
	int currFrame = 0;
	Sprite currSprite;
	Transform carryingRoot;
	
	// physics parameters
	Vector2 speed = vec(0,0,0);
	bool grounded = true;
	
	// effects parameters
	float framef = 0;
	Vector3 carryingRootRestPosition;
	
	// interface for objects the player can carry
	public abstract class Carryable : GameBehaviour {
		internal Transform node;
		internal Transform fx;
		
		public abstract void OnPickUp();
		public abstract void OnPutDown();
		public abstract void OnThrow();
		
	}
	
	// the player has one carrying slot
	Carryable hoveringItem = null;
	internal Carryable inventoryItem = null;
	
	bool performingPickup = false;
	
	//--------------------------------------------------------------------------------
	// UNITY EVENTS
	
	void Awake() {	

		inst = this;
	
		Assert(spriteFrames.Length == 6, "Hero has the correct animation");

		box = GetComponent<CollisionAABB>();
		
		node = transform;
		carryingRoot = node.Find("CarryingRoot");
		fx = node.Find("FX");
		
		spriteFx = fx.GetComponent<SpriteRenderer>();
		Assert(spriteFx, "Hero has a Sprite");
		
		currSprite = spriteFx.sprite;
		carryingRootRestPosition = carryingRoot.localPosition;
	}
	
	void OnDestroy() {
		if (inst == this) { inst = null; }
	}
	
	void Update() {
		var clampedDt = deltaTime;
		TickFreefall(clampedDt);
		TickMovement();
		var collision = box.Move(speed * clampedDt);
		grounded = collision.hitBottom;
		if (grounded) {
			speed.y = 0;
		}
		UpdateFX();
		if (inventoryItem == null) {
			CheckOverlap();
//			CheckPickup();			
		} else {
			CheckUseItem();
		}
	}
	
	//--------------------------------------------------------------------------------
	// INPUTS
	
	public bool LeftButtonPressing() { return Input.GetKey(KeyCode.LeftArrow); }
	public bool RightButtonPressing() { return Input.GetKey(KeyCode.RightArrow); }
	public bool JumpButtonPressed() { return Input.GetKeyDown(KeyCode.LeftShift); }
	public bool ActionButtonPressed() { return Input.GetKeyDown(KeyCode.Z); }
	public bool CancelButtonPressed() { return Input.GetKeyDown(KeyCode.DownArrow); }
	
	//--------------------------------------------------------------------------------
	// PICKUP METHODS
	
	void CheckOverlap() {
//		Assert(!inventoryItem, "Don't Check Overlaps when we already have inventory");
//		// query for items
//		var p = node.position.xy() + hitbox.center;
//		var extent = 0.5f * hitbox.size;
//		var bottomLeft = p - extent;
//		var topRight = p + extent;
//		int hitCount = Physics2D.OverlapAreaNonAlloc(bottomLeft, topRight, queryBuffer, ItemsMask);
//		
//		// just consider the first result
//		if (hitCount > 0) {
//			var item = queryBuffer[0].GetComponent<Carryable>();
//			Assert(item, "Hero only Queries Carryables");			
//			if (item != hoveringItem) {
//				hoveringItem = item;
//				hint.gameObject.SetActive(true);
//			}			
//		} else {
//			ClearHoveringItem();
//		}
//		
//		
	}
	
//	void CheckPickup() {
//		Assert(!inventoryItem, "Don't check pickups when we already have inventory");
//		if (hoveringItem != null && ActionButtonPressed()) {
//			inventoryItem = hoveringItem;
//			ClearHoveringItem();
//			inventoryItem.OnPickUp();
//			actionHint.gameObject.SetActive(true);
//			performingPickup = true;
//			Jukebox.Play("pickup");
//			StartCoroutine( DoPickup() );
//			RefreshSprite();
//		}	
//	}
//	
//	IEnumerator DoPickup() {
//		inventoryItem.node.parent = carryingRoot;
//		inventoryItem.fx.localScale = fx.localScale;
//		var p0 = inventoryItem.node.localPosition;
//		var p1 = vec(0,0,0);
//		foreach(var u in Interpolate(0.5f)) {
//			float uu = EaseOut2(u);
//			inventoryItem.node.localPosition = Vector2.Lerp(p0, p1, uu) + vec(0, Parabola(uu));
//			yield return null;
//		}
//		performingPickup = false;
//	}
//	
//	void ClearHoveringItem() {
//		if (hoveringItem != null) {
//			hint.gameObject.SetActive(false);
//			hoveringItem = null;
//		}
//	}
//		
	//--------------------------------------------------------------------------------
	// ACTION METHODS
	
	void CheckUseItem() {
		Assert(inventoryItem, "Don't check use items if there's nothing to utilize");
		if (!performingPickup) {
			if (ActionButtonPressed()) {
				Jukebox.Play("throw");
				PopInventoryItem().OnThrow();
			} else if (grounded && CancelButtonPressed()) {
				performingPickup = true;
				Jukebox.Play("put");
				StartCoroutine(DoPutDown());
			}
		}
	}
	
	IEnumerator DoPutDown() {
		inventoryItem.node.parent = null;
		var p0 = inventoryItem.node.position;
		var p1 = node.position;
		foreach(var u in Interpolate(0.333f)) {
			float uu = EaseOut2(u);
			inventoryItem.node.position = Vector3.Lerp(p0, p1, uu) + vec(0, Parabola(uu), 0);
			yield return null;
		}
		PopInventoryItem().OnPutDown();
		performingPickup = false;
	}
	
	Carryable PopInventoryItem() {
		Assert(inventoryItem, "Can't pop an inventory item we don't have");
		var result = inventoryItem;
		inventoryItem.node.parent = null;
		inventoryItem = null;
		RefreshSprite();
		return result;
	}
		
	//--------------------------------------------------------------------------------
	// MOVEMENT TICKS

	void TickFreefall(float dt) {
		var gravity = Physics2D.gravity.y;
		if(grounded && JumpButtonPressed()) {
			Jukebox.Play("jump");
			speed.y = Mathf.Sqrt( -2f * gravity * jumpHeight );
		} else {
			speed.y += gravity * dt;		
		}		
	}
	
	void TickMovement() {
		if (LeftButtonPressing()) {
			speed.x = speed.x.EaseTowards(-movementSpeed, 0.2f);
			fx.localScale = vec(-1, 1, 1);
			if (inventoryItem) { inventoryItem.fx.localScale = vec(-1,1,1); }
		} else if (RightButtonPressing()) {
			speed.x = speed.x.EaseTowards(movementSpeed, 0.2f);
			fx.localScale = vec(1, 1, 1);
			if (inventoryItem) { inventoryItem.fx.localScale = vec(1,1,1); }
		} else {
			speed.x = speed.x.EaseTowards(0, 0.2f);
		}		
	}
	
	//--------------------------------------------------------------------------------
	// VISUAL EFFECTS
	
	void UpdateFX() {
		if (grounded) {
			
			var sx = Mathf.Abs(speed.x);
			if (sx > 0.2f) {
				framef += animRate * sx;
				framef %= 3f;
				int fr = Mathf.FloorToInt(framef);
				if (SetSprite(fr) && fr == 0) {
					Jukebox.Play("footfall");
				}
			} else {
				framef = 0f;
				if (SetSprite(0)) {
					Jukebox.Play("footfall");
				}
			}
		} else {
			framef = 0f;
			SetSprite(2);
		}		
	}
	
	bool RefreshSprite() {
		return SetSprite(currFrame);
	}
	
	bool SetSprite(int frame) {
		currFrame = frame;
	
		if (inventoryItem != null) {
			frame += 3;
		}

		// I don't know if this optimization really matters,
		// but whatevs.  
		if (currSprite != spriteFrames[frame]) {
			currSprite = spriteFrames[frame];
			spriteFx.sprite = currSprite;
			carryingRoot.localPosition = 
				carryingRootRestPosition + vec(0, (frame%3)/12f, 0);
			return true;
		} else {
			return false;
		}
	}
	
			
	
}
