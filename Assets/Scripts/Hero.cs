using UnityEngine;
using System.Collections;

public class Hero : GameBehaviour {
	
	// The player's avatar.  If this was for realsies I
	// would use a kinematic body and write proper character
	// control, but f*ck it, this is a gamejam, so I'm just
	// going to go dynamic and hope for the best.
	
	public float movementSpeed = 1f;
	public float gravity = -40f;
	public float jumpHeight = 2.5f;
	public float animRate = 0.05f;
	
	public Sprite[] spriteFrames;
	
	Rigidbody2D body;
	BoxCollider2D hitbox;
	Transform node;
	Transform fx;
	SpriteRenderer spriteFx;
	Sprite currSprite;
	
	Vector2 speed = vec(0,0,0);
	bool grounded = true;
	
	float framef = 0;
	
	void Awake() {	
		body = GetComponent<Rigidbody2D>();
		hitbox = GetComponent<BoxCollider2D>();
		node = transform;
		spriteFx = GetComponentInChildren<SpriteRenderer>();
		fx = spriteFx.transform;
		
		currSprite = spriteFx.sprite;
		
		Assert(spriteFrames.Length == 3);
	}
	
	
	
	void Update() {
		
		// use the clamped DT so we don't get huge timesteps
		var dt = deltaTime;
		
		// freefall
		if(grounded && Input.GetKeyDown(KeyCode.Space)) {
			Jukebox.Play("jump");
			speed.y = Mathf.Sqrt( -2f * gravity * jumpHeight );
		} else {
			speed.y += gravity * dt;		
		}
		
		// movement
		if (Input.GetKey(KeyCode.LeftArrow)) {
			speed.x = speed.x.EaseTowards(-movementSpeed, 0.2f);
			fx.localScale = vec(-1, 1, 1);
		} else if (Input.GetKey(KeyCode.RightArrow)) {
			speed.x = speed.x.EaseTowards(movementSpeed, 0.2f);
			fx.localScale = vec(1, 1, 1);
		} else {
			speed.x = speed.x.EaseTowards(0, 0.2f);
		}

		// separate axis updating - first we slide along the Y-axis, then
		// along the X-axis.  In each case "sliding" means testing the target
		// position, and resolving collisions by snapping to the edge.
		
		// (making the assumption that all background layer colliders have no
		// rotation.)

		var offset = speed * dt;				
		var extent = 0.5f * hitbox.size;
		
		// hack - avoid "ground jitter"
		if (offset.y < 0 && offset.y > -collisionSlop) {
			offset.y -= collisionSlop;
		}

		// Y-axis
		
		grounded = false; // recomputed every time we solve the y-axis
		
		var p1 = node.position.xy() + vec(0f, offset.y);
		var bottomLeft = p1 + hitbox.center - extent;
		var topRight = p1 + hitbox.center + extent;		
		var ycollider = Physics2D.OverlapArea(bottomLeft, topRight, BackgroundMask) as BoxCollider2D;
		if (ycollider) {
			speed.y = 0;
			var y = ycollider.transform.position.y + ycollider.center.y;
			if (offset.y < 0f) {	
				// going down, check the top of the collider			
				var top = y + 0.5f * ycollider.size.y + collisionSlop;
				p1.y = top - hitbox.center.y + extent.y;
				grounded = true;
			} else {
				// going up, check the bottom of the collider
				var bottom = y - 0.5f * ycollider.size.y - collisionSlop;
				p1.y = bottom - hitbox.center.y - extent.y;				
			}
		}
		
		// X-axis
		if (!Mathf.Approximately(offset.x, 0)) {
			p1.x += offset.x;
			bottomLeft = p1 + hitbox.center - extent;
			topRight = p1 + hitbox.center + extent;		
			
			var xcollider = Physics2D.OverlapArea(bottomLeft, topRight, BackgroundMask) as BoxCollider2D;
			if (xcollider) {
				speed.x = 0;
				var x = xcollider.transform.position.x + xcollider.center.x;
				if (offset.x < 0f) {
					// going left, check right side of collider
					var right = x + 0.5f * xcollider.size.x + collisionSlop;
					p1.x = right - hitbox.center.x + extent.x;
				} else {			
					// going right, check the left side of the collider
					var left = x - 0.5f * xcollider.size.x - collisionSlop;
					p1.x = left - hitbox.center.x - extent.x;
				}
			}
		}
			
		node.position = vec(p1, 0);
		
		// update animation
		if (grounded) {
			
			var sx = Mathf.Abs(speed.x);
			if (sx > 0.2f) {
				framef += animRate * sx;
				framef %= (float)spriteFrames.Length;
				int fr = Mathf.FloorToInt(framef);
				if (SetSprite(spriteFrames[fr]) && fr == 0) {
					Jukebox.Play("footfall");
				}
			} else {
				framef = 0f;
				if (SetSprite(spriteFrames[0])) {
					Jukebox.Play("footfall");
				}
			}
		} else {
			framef = 0f;
			SetSprite(spriteFrames[2]);
		}	
		
		
	}
	
	bool SetSprite(Sprite spr) {
		// I don't know if this optimization really matters,
		// but whatevs.  
		if (currSprite != spr) {
			currSprite = spr;
			spriteFx.sprite = spr;
			return true;
		} else {
			return false;
		}
	}
	
}
