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
		if (collision.contacts[0].collider == hitbox) {
			normal = collision.contacts[1].normal;
		} else {
			normal = collision.contacts[0].normal;
		}
		if (Vector2.Dot(vec(0,1), normal) > 0.95f) {
			body.isKinematic = true;
			Jukebox.Play("land");
			BeginSentry();
		}
	}
	
	//--------------------------------------------------------------------------------
	
	void BeginSentry() {
		// cast ray down to determine floor position
		var hit = Physics2D.Raycast(node.position.Above(0.01f), Vector3.down, 1f, BackgroundMask);
		Assert(hit, "Kitten Starts Grounded");
		node.position = hit.point;	
		StartCoroutine( DoSentry() );	
	}
	
	IEnumerator DoSentry() {
		
		// figure out endpoings
		Vector2 sentryLeft, sentryRight;
		World.inst.GetFloorExtents(node.position, out sentryLeft, out sentryRight);
		
		// walk from current position to the edge
		if (fx.localScale.x < 0) {
			yield return StartCoroutine(WalkFromTo(node.position.xy(), sentryLeft));
			SetSprite(walkFrames[0]);
			yield return new WaitForSeconds(0.5f);
			goto WalkRight;
		} else {
			yield return StartCoroutine(WalkFromTo(node.position.xy(), sentryRight));
			SetSprite(walkFrames[0]);
			yield return new WaitForSeconds(0.5f);
			goto WalkLeft;
		}
					
		WalkRight:
		fx.localScale = vec(1,1,1);
		yield return StartCoroutine(WalkFromTo(sentryLeft, sentryRight));
		SetSprite(walkFrames[0]);
		yield return new WaitForSeconds(0.5f);
		
		WalkLeft:
		fx.localScale = vec(-1,1,1);
		yield return StartCoroutine(WalkFromTo(sentryRight, sentryLeft));
		SetSprite(walkFrames[0]);
		yield return new WaitForSeconds(0.5f);
		
		goto WalkRight;
	}
	
	IEnumerator WalkFromTo(Vector2 p0, Vector2 p1) {
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
