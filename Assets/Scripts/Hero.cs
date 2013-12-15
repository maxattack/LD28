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
	
	// cached references
	Rigidbody2D body;
	BoxCollider2D hitbox;
	Transform node;
	Transform fx;
	SpriteRenderer spriteFx;
	Sprite currSprite;
	UpHintFX hint;
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
	Carryable inventoryItem = null;
	
	bool performingPickup = false;
	
	//--------------------------------------------------------------------------------
	
	void Awake() {	
		inst = this;
	
		Assert(spriteFrames.Length == 3, "Hero has Three Frames of Animation");

		body = GetComponent<Rigidbody2D>();
		hitbox = GetComponent<BoxCollider2D>();
		
		node = transform;
		carryingRoot = node.Find("CarryingRoot");
		fx = node.Find("FX");
		
		spriteFx = fx.GetComponent<SpriteRenderer>();
		Assert(spriteFx, "Hero has a Sprite");
		
		hint = GetComponentInChildren<UpHintFX>();
		Assert(hint != null, "Hero has a Hint UI");
		
		currSprite = spriteFx.sprite;
		carryingRootRestPosition = carryingRoot.localPosition;
	}
	
	void Start() {
		hint.gameObject.SetActive(false);
	}
	
	void OnDestroy() {
		if (inst == this) { inst = null; }
	}
	
	void Update() {
		var clampedDt = deltaTime;
		TickFreefall(clampedDt);
		TickMovement();
		var offset = SolveMotion(speed * clampedDt);				
		UpdateFX();
		if (inventoryItem == null) {
			CheckOverlap();
			CheckPickup();			
		} else {
			CheckUseItem();
		}
	}
	
	//--------------------------------------------------------------------------------
	
	void CheckOverlap() {
		Assert(!inventoryItem, "Don't Check Overlaps when we already have inventory");
		// query for items
		var p = node.position.xy() + hitbox.center;
		var extent = 0.5f * hitbox.size;
		var bottomLeft = p - extent;
		var topRight = p + extent;
		int hitCount = Physics2D.OverlapAreaNonAlloc(bottomLeft, topRight, queryBuffer, ItemsMask);
		
		// just consider the first result
		if (hitCount > 0) {
			var item = queryBuffer[0].GetComponent<Carryable>();
			Assert(item, "Hero only Queries Carryables");			
			if (item != hoveringItem) {
				hoveringItem = item;
				hint.gameObject.SetActive(true);
			}			
		} else {
			ClearHoveringItem();
		}
		
		
	}
	
	void CheckPickup() {
		Assert(!inventoryItem, "Don't check pickups when we already have inventory");
		if (hoveringItem != null && Input.GetKeyDown(KeyCode.UpArrow)) {
			inventoryItem = hoveringItem;
			ClearHoveringItem();
			inventoryItem.OnPickUp();
			performingPickup = true;
			Jukebox.Play("pickup");
			StartCoroutine( DoPickup() );
		}	
	}
	
	IEnumerator DoPickup() {
		inventoryItem.node.parent = carryingRoot;
		inventoryItem.fx.localScale = fx.localScale;
		var p0 = inventoryItem.node.localPosition;
		var p1 = vec(0,0,0);
		foreach(var u in Interpolate(0.5f)) {
			float uu = EaseOut2(u);
			inventoryItem.node.localPosition = Vector2.Lerp(p0, p1, uu) + vec(0, Parabola(uu));
			yield return null;
		}
		performingPickup = false;
	}
	
	//--------------------------------------------------------------------------------
	
	void CheckUseItem() {
		Assert(inventoryItem, "Don't check use items if there's nothing to utilize");
		if (!performingPickup) {
			if (Input.GetKeyDown(KeyCode.Z)) {
				Jukebox.Play("throw");
				PopInventoryItem().OnThrow();
			} else if (grounded && Input.GetKeyDown(KeyCode.DownArrow)) {
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
		return result;
	}
		
	//--------------------------------------------------------------------------------
	
	void ClearHoveringItem() {
		if (hoveringItem != null) {
			hint.gameObject.SetActive(false);
			hoveringItem = null;
		}
	}
		
	//--------------------------------------------------------------------------------

	void TickFreefall(float dt) {
		var gravity = Physics2D.gravity.y;
		if(grounded && Input.GetKeyDown(KeyCode.LeftShift)) {
			Jukebox.Play("jump");
			speed.y = Mathf.Sqrt( -2f * gravity * jumpHeight );
		} else {
			speed.y += gravity * dt;		
		}		
	}
	
	//--------------------------------------------------------------------------------
	
	void TickMovement() {
		if (Input.GetKey(KeyCode.LeftArrow)) {
			speed.x = speed.x.EaseTowards(-movementSpeed, 0.2f);
			fx.localScale = vec(-1, 1, 1);
			if (inventoryItem) { inventoryItem.fx.localScale = vec(-1,1,1); }
		} else if (Input.GetKey(KeyCode.RightArrow)) {
			speed.x = speed.x.EaseTowards(movementSpeed, 0.2f);
			fx.localScale = vec(1, 1, 1);
			if (inventoryItem) { inventoryItem.fx.localScale = vec(1,1,1); }
		} else {
			speed.x = speed.x.EaseTowards(0, 0.2f);
		}		
	}
	
	//--------------------------------------------------------------------------------
	
	Vector3 SolveMotion(Vector2 offset) {
		// separate axis updating - first we slide along the Y-axis, then
		// along the X-axis.  In each case "sliding" means testing the target
		// position, and resolving collisions by snapping to the edge.
		
		// (making the assumption that all background layer colliders have no
		// rotation.)
		
		var p0 = node.position.xy();
		
		var center = hitbox.center;
		var extent = 0.5f * hitbox.size;
		
		// hack - avoid "ground jitter"
		if (offset.y < 0 && offset.y > -collisionSlop) {
			offset.y -= collisionSlop;
		}

		// Solve Y-axis
		
		grounded = false; // recomputed every time we solve the y-axis
		
		var p1 = node.position.xy() + vec(0f, offset.y);
		var bottomLeft = p1 + center - extent;
		var topRight = p1 + center + extent;		
		var ycollider = Physics2D.OverlapArea(bottomLeft, topRight, BackgroundMask) as BoxCollider2D;
		if (ycollider) {
			speed.y = 0;
			var y = ycollider.transform.position.y + ycollider.center.y;
			if (offset.y < 0f) {	
				// going down, check the top of the collider			
				var top = y + 0.5f * ycollider.size.y + collisionSlop;
				p1.y = top - center.y + extent.y;
				grounded = true;
			} else {
				// going up, check the bottom of the collider
				var bottom = y - 0.5f * ycollider.size.y - collisionSlop;
				p1.y = bottom - center.y - extent.y;				
			}
		}
		
		// Solve X-axis
		
		if (!Mathf.Approximately(offset.x, 0)) {
			p1.x += offset.x;
			bottomLeft = p1 + center - extent;
			topRight = p1 + center + extent;		
			
			var xcollider = Physics2D.OverlapArea(bottomLeft, topRight, BackgroundMask) as BoxCollider2D;
			if (xcollider) {
				speed.x = 0;
				var x = xcollider.transform.position.x + xcollider.center.x;
				if (offset.x < 0f) {
					// going left, check right side of collider
					var right = x + 0.5f * xcollider.size.x + collisionSlop;
					p1.x = right - center.x + extent.x;
				} else {			
					// going right, check the left side of the collider
					var left = x - 0.5f * xcollider.size.x - collisionSlop;
					p1.x = left - center.x - extent.x;
				}
			}
		}
			
		node.position = p1;
		
		return p1 - p0;
	}
	
	//--------------------------------------------------------------------------------
	
	void UpdateFX() {
		if (grounded) {
			
			var sx = Mathf.Abs(speed.x);
			if (sx > 0.2f) {
				framef += animRate * sx;
				framef %= (float)spriteFrames.Length;
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
	
	//--------------------------------------------------------------------------------
	
	bool SetSprite(int frame) {
		// I don't know if this optimization really matters,
		// but whatevs.  
		if (currSprite != spriteFrames[frame]) {
			currSprite = spriteFrames[frame];
			spriteFx.sprite = currSprite;
			carryingRoot.localPosition = 
				carryingRootRestPosition + vec(0, frame/12f, 0);
			return true;
		} else {
			return false;
		}
	}
	
			
	
}
