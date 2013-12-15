using UnityEngine;
using System.Collections;

public class Kitten : Hero.Carryable {

	// Designer Parameters
	public float movementSpeed = 1f;
	public float walkAnimationSpeed = 1f;
	public Sprite[] walkFrames;
	
	// Cached References
	SpriteRenderer spriteRenderer;
	Rigidbody2D body;
	BoxCollider2D hitbox;
	Sprite currFrame;
	
	// effects parameters
	float framef = 0f;

	// collision params
	Collider2D overlap = null;

	//--------------------------------------------------------------------------------
	
	void Awake() {
		
		// cache references
		node = transform;
		fx = node.Find("FX");
		spriteRenderer = fx.GetComponent<SpriteRenderer>();
		body = GetComponent<Rigidbody2D>();
		hitbox = GetComponent<BoxCollider2D>();
		
		currFrame = spriteRenderer.sprite;
	}
	
	void Start() {
		BeginSentry();
	}
	
	void Update() {
		
		var p = node.position.xy() + hitbox.center;
		var ext = 0.5f * hitbox.size;
		var hit = Physics2D.OverlapArea(p - ext, p + ext, KittenTriggerMask);
		if (hit) {
			if (hit != overlap) {
				overlap = hit;
				hit.SendMessage("KittenTrigger");
			}
		} else if (overlap) {
			overlap = null;
		}
		
	}
	
	//--------------------------------------------------------------------------------
	// Hero.Carryable Interface
	
	public override void OnPickUp() {
		StopAllCoroutines();
		SetSprite(walkFrames[1]);
	}
	
	public override void OnPutDown() {
		BeginSentry();
	}
	
	public override void OnThrow() {
		body.isKinematic = false;
		var dx = fx.localScale.x;
		body.velocity = vec(dx * Hero.inst.throwVector.x, Hero.inst.throwVector.y);
	}
	
	void OnCollisionEnter2D(Collision2D collision) {
		// determine which contact is not ours
		if (body.isKinematic) { return; }
		
		Vector2 normal;
		Vector2 pos;
		if (collision.contacts[0].collider == hitbox) {
			normal = collision.contacts[1].normal;
			pos = collision.contacts[1].point;
		} else {
			normal = collision.contacts[0].normal;
			pos = collision.contacts[0].point;
		}
		if (Vector2.Dot(vec(0,1), normal) > 0.95f) {
			body.isKinematic = true;
			Jukebox.Play("land");
			var hit = Physics2D.Raycast(node.position.Above(0.01f), Vector3.down, 1f, BackgroundMask);
			DoBeginSentry(hit ? hit.point : pos);			
		}
	}
	
	//--------------------------------------------------------------------------------
	
	void BeginSentry() {
		// cast ray down to determine floor position
		var hit = Physics2D.Raycast(node.position.Above(0.01f), Vector3.down, 1f, BackgroundMask);
		Assert(hit, "Kitten Starts Grounded");
		DoBeginSentry(hit.point);
	}
	
	void DoBeginSentry(Vector2 p) {
		node.position = p;	
		StartCoroutine( DoSentry() );		
	}
	
	IEnumerator DoSentry() {
		
		// figure out endpoings
		Vector2 sentryLeft, sentryRight;
		World.inst.GetFloorExtents(node.position, out sentryLeft, out sentryRight);
		
		var dx = Mathf.Abs(sentryLeft.x - sentryRight.x);
		if (dx < 1f) {
			SetSprite(walkFrames[0]);
			yield break;
		}
		
		// walk from current position to the edge
		var np = node.position.xy();
		if (fx.localScale.x < 0) {
			yield return StartCoroutine(WalkFromTo(np, sentryLeft));
			SetSprite(walkFrames[0]);
			yield return new WaitForSeconds(0.5f);
			goto WalkRight;
		} else {
			yield return StartCoroutine(WalkFromTo(np, sentryRight));
			SetSprite(walkFrames[0]);
			yield return new WaitForSeconds(0.5f);
			goto WalkLeft;
		}
					
		WalkRight:
		yield return StartCoroutine(WalkFromTo(sentryLeft, sentryRight));
		SetSprite(walkFrames[0]);
		yield return new WaitForSeconds(0.5f);
		
		WalkLeft:
		yield return StartCoroutine(WalkFromTo(sentryRight, sentryLeft));
		SetSprite(walkFrames[0]);
		yield return new WaitForSeconds(0.5f);
		
		goto WalkRight;
	}
	
	IEnumerator WalkFromTo(Vector2 p0, Vector2 p1) {
		if (p1.x > p0.x) {
			fx.localScale = vec(1,1,1);
		} else {
			fx.localScale = vec(-1,1,1);
		}
		var distance = Mathf.Abs(p1.x - p0.x);
		var duration = distance / movementSpeed;
		framef = 1f;
		foreach(var d in Interpolate(duration)) {
			node.position = Vector2.Lerp(p0, p1, d);
			framef += walkAnimationSpeed * Time.deltaTime;
			SetSprite(walkFrames[ Mathf.FloorToInt(framef) % walkFrames.Length ]);
			yield return null;
		}
	}
	
	//--------------------------------------------------------------------------------
	
	bool SetSprite(Sprite spr) {
		// I don't know if this optimization really matters,
		// but whatevs.  
		if (currFrame != spr) {
			currFrame = spr;
			spriteRenderer.sprite = spr;
			return true;
		} else {
			return false;
		}
	
	}
	
	
}
