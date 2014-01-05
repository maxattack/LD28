// Little Polygon SDK
// Copyright (C) 2013 Max Kaufmann
//
// This program is free software: you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation, either version 3 of the License, or
// (at your option) any later version.
//
// This program is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU General Public License for more details.
//
// You should have received a copy of the GNU General Public License
// along with this program.  If not, see <http://www.gnu.org/licenses/>.

using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

//--------------------------------------------------------------------------------
//
// CollisionSystem is designed to be faster and easier to use than ordinary
// physics for  simple mechanics involving Axis-Aligned Bounding Boxes, and can 
// also capture common platformer idioms like kinematic character control, jumping 
// up-through floors, or crouch-falling that Box2D and PhysX cannot model cleanly.
//
// This is not intended as a replacement for ordinary physics, but a supplement.
// You can freely mix CollisionAABBs and Rigidbody or Rigidbody2Ds (with some 
// understandable caveats) to utilize dynamics where it's handy and easier control
// where it's problematic.
//
// Mostly, Colliders are implicitly added by using CollisionAABB components to
// objects, however freestanding colliders can also be created directly (for things
// like environments).  This CollisionSystem root context object can be implicitly
// initialized to default values or explicitly initialized with designer-defined
// capacity.
//
//--------------------------------------------------------------------------------

public struct AABB {
	public Vector2 p0;
	public Vector2 p1;

	public Vector2 Center { get { return 0.5f * (p0 + p1); } }
	public Vector2 Size { get { return p1 - p0; } }
	public float Left { get { return p0.x; } }
	public float Right { get { return p1.x; } }
	public float Top { get { return p0.y; } }
	public float Bottom { get { return p1.y; } }

	public void Translate(Vector2 offset) {
		p0 += offset;
		p1 += offset;
	}

	public bool Overlaps(AABB box) {
		return  p0.x < box.p1.x && p1.x > box.p0.x &&
		        p0.y < box.p1.y && p1.y > box.p0.y ;
	}
}

enum TriggerEventType {
	Enter, Stay, Exit
}

struct TriggerEvent {
	public TriggerEventType type;
	public int trigger;
}

public struct Collision {
	public bool hitTop;
	public bool hitBottom;
	public bool hitLeft;
	public bool hitRight;

	public bool HitVertical { get { return hitTop || hitBottom; } }
	public bool HitHorizontal { get { return hitLeft || hitRight; } }
	public bool Hit { get { return hitTop || hitBottom || hitLeft || hitRight; } }
}

//--------------------------------------------------------------------------------

public class CollisionSystem : CustomBehaviour {
	public int slotCount = 128;    // max # of collider slots
	public int bucketCount = 128;  // trades storage for performance in broad-phase
	public int contactCount = 128; 

	// structure-of-arrays backing store
	Bitset freeSlots;
	Bitset broadphaseCandidates;
	Collider[] slots;
	Bitset[] buckets;
	int nContacts;
	Contact[] contacts;
	Bitset contactScratch;
	int[] oldToNewScratch;

	// singleton (bad assumption?)
	static CollisionSystem inst;

	public static CollisionSystem GetInstance() {
		if (!inst) {
			// was one placed in the scene?
			inst = FindObject<CollisionSystem>();
		}
		if (!inst) {
			// okay, make one dynamically
			inst = new GameObject("CollisionSystem", typeof(CollisionSystem))
				.GetComponent<CollisionSystem>();
		}
		return inst;
	}

	void Awake() {

		// block-allocate backing store.  Each of these arrays are of value-types
		// and are compact in memory, so everything should be speedy to look up.
		slotCount += slotCount % 32;
		bucketCount += bucketCount % 32;
		contactCount += contactCount % 32;
		freeSlots = new Bitset(slotCount);
		broadphaseCandidates = new Bitset(slotCount);
		slots = new Collider[slotCount];
		buckets = new Bitset[bucketCount];
		freeSlots.Mark();
		for(int i=0; i<bucketCount; ++i) {
			buckets[i] = new Bitset(slotCount);
		}
		nContacts = 0;
		contacts = new Contact[contactCount];
		contactScratch = new Bitset(contactCount);
		oldToNewScratch = new int[contactCount];

	}
	

	// Add a new collider to the system.  Everything is block allocated, so this is
	// really just flipping a bit and intializing some fields.
	public int AddCollider(AABB box, int categoryMask, int collisionMask, int triggerMask, object userData=null) {
		int result;
		freeSlots.ClearFirst(out result);
		slots[result].box = box;
		slots[result].categoryMask = categoryMask;
		slots[result].collisionMask = collisionMask;
		slots[result].triggerMask = triggerMask;
		slots[result].userData = userData;
		Hash(result);
		return result;
	}


	// Parameters getters for individual colliders
	public AABB GetBounds(int id) { return slots[id].box; }

	public void SetBounds(int id, AABB box) {
		Unhash (id);
		slots[id].box = box;
		Hash(id);
	}

	public int GetCategoryMask(int id) { return slots[id].categoryMask; }
	public void SetCategoryMask(int id, int mask) { slots[id].categoryMask = mask; }
	public int GetCollisionMask(int id) { return slots[id].collisionMask; }
	public void SetCollisionMask(int id, int mask) { slots[id].collisionMask = mask; }
	public int GetTriggerMask(int id) { return slots[id].triggerMask; }
	public void SetTriggerMask(int id, int mask) { slots[id].triggerMask = mask; }
	public object GetUserData(int id) { return slots[id].userData; }
	public void SetUserData(int id, object data) { slots[id].userData = data; }

	// Enumerates boxes which overlap the given box, filtered by their category.
	IEnumerable<int> QueryColliders(AABB box, int mask) {

		// iterate through broad phase candidates looking for matches
		BroadPhase(box);
		var lister = new Bitset.BitLister(broadphaseCandidates);
		int slot;
		while(lister.Next(out slot)) {
			if ((slots[slot].categoryMask & mask) != 0 && slots[slot].box.Overlaps(box)) {
				yield return slot;
			}
		}

	}

	// Enumerate differences between the current trigger-status of the given collider,
	// and the last time that this method was invoked.
	IEnumerable<TriggerEvent> QueryTriggers(int id) {

		// identify relevant contacts
		contactScratch.Clear();
		for(int i=0; i<nContacts; ++i) {
			if (contacts[i].collider == id) {
				contactScratch.Mark(i);
			}
		}

		// Iterate through actual overlaps
		BroadPhase(slots[id].box);
		var lister = new Bitset.BitLister(broadphaseCandidates);
		int slot;
		while(lister.Next (out slot)) {
			if (slots[id].Triggers(ref slots[slot])) {
				int i = FindTrigger(slot);
				contactScratch.Clear(i);
				if (i < nContacts) {
					yield return new TriggerEvent() {
						type = TriggerEventType.Stay, trigger = slot
					};
				} else {
					Assert(nContacts < contactCount, "Contacts are within capacity");
					nContacts++;
					contacts[i].collider = id;
					contacts[i].trigger = slot;
					yield return new TriggerEvent() {
						type = TriggerEventType.Enter, trigger = slot
					};
				}
			}
		}

		// find exit triggers
		for(int i=0; i<nContacts; ++i) {
			oldToNewScratch[i] = i;
		}
		int contactIndex;
		while(contactScratch.ClearFirst(out contactIndex)) {
			int actualIndex = oldToNewScratch[contactIndex];
			yield return new TriggerEvent() {
				type = TriggerEventType.Exit, trigger = contacts[actualIndex].trigger
			};
			--nContacts;
			if (actualIndex < nContacts) {
				contacts[actualIndex] = contacts[nContacts];
				oldToNewScratch[nContacts] = actualIndex;
			}
		}
	}

	int FindTrigger(int trigger) {
		// Private helper function for QueryTriggers
		var lister = new Bitset.BitLister(contactScratch);
		int idx;
		while(lister.Next(out idx)) {
			if (contacts[idx].trigger == trigger) {
				return idx;
			}
		}
		return nContacts;
	}


	// Attempt the move the given collider by the given offset.  The motion is decomposed
	// into separate X and Y motions which are taken "as far as they can" without violating
	// non-overlapping constraints with other colliders.  The result indicates the sides
	// on which contacts occured.  E.g. hitBottom is useful for determinine if a character
	// is grounded.
	public Collision Move(int id, Vector2 offset) {
		Collision result = new Collision();

		// unhash so we don't self-collide, and because we intent to change the given
		// bounding box.
		Unhash (id);

		// perform broad phase over the whole sweep of the motion
		AABB sweep = slots[id].box;
		if (offset.x < 0) { sweep.p0.x += offset.x; }
		else { sweep.p1.x += offset.x; }
		if (offset.y < 0) { sweep.p0.y += offset.y; }
		else { sweep.p1.y += offset.y; }
		BroadPhase(sweep);
		var lister = new Bitset.BitLister(broadphaseCandidates);
		int slot = 0;

		var size = slots[id].box.Size;

		// Check axes and resolve overlaps separately ("move as far as we can in Y first, 
		// then as far as we can in X").  Helps us fit "snugly" into corners.
		// Works because everything is Axis-Aligned.  In a more general case we'd need to
		// decompose parallel and perpendicular vectors on a case-by-case basis.

		if (offset.y > 0) {

			// moving up
			slots[id].box.p1.y += offset.y;
			while(lister.Next(out slot)) {
				if (slots[id].Collides(ref slots[slot])) {
					slots[id].box.p1.y = slots[slot].box.p0.y;
					result.hitTop = true;
				}
			}
			slots[id].box.p0.y = slots[id].box.p1.y - size.y;


		} else {

			// moving down
			slots[id].box.p0.y += offset.y;
			while(lister.Next(out slot)) {
				if (slots[id].Collides(ref slots[slot])) {
					slots[id].box.p0.y = slots[slot].box.p1.y;
					result.hitBottom = true;
				}
			}
			slots[id].box.p1.y = slots[id].box.p0.y + size.y;

		}

		lister.Reset();

		if (offset.x > 0) {
			
			// moving right
			slots[id].box.p1.x += offset.x;
			while(lister.Next(out slot)) {
				if (slots[id].Collides(ref slots[slot])) {
					slots[id].box.p1.x = slots[slot].box.p0.x;
					result.hitRight = true;
				}
			}
			slots[id].box.p0.x = slots[id].box.p1.x - size.x;
			
			
		} else {
			
			// moving left
			slots[id].box.p0.x += offset.x;
			while(lister.Next(out slot)) {
				if (slots[id].Collides(ref slots[slot])) {
					slots[id].box.p0.x = slots[slot].box.p1.x;
					result.hitLeft = true;
				}
			}
			slots[id].box.p1.x = slots[id].box.p0.x + size.x;
			
		}

		// rehash the new bounding box
		Hash (id);
		return result;
	}

	// Deallocate the collide from the system, removing any relevant
	// contacts with others.
	public void RemoveCollider(int id) {

		// remove relevant contacts
		int i = nContacts;
		while(i>0) {
			--i;
			if (contacts[i].collider == id || contacts[i].trigger == id) {
				if (i != nContacts-1) {
					contacts[i] = contacts[nContacts-1];
				}
				--nContacts;
			}
		}

		// deallocate slot
		Unhash(id);
		freeSlots.Mark(id);

	}

	// spatial hashing methods.  We decouple the logical grid-sectors (which may
	// be infinite) from a finite backing store.  We map sectors to buckets using 
	// a hash that should be generally balanced.  This is a lot like "separate chaining"
	// except that it's super-inexpensive since the sets are just bit arrays.

	int HashBucket(int x, int y) {
		unchecked {
			var result = (((((int)0x811c9dc5 ^ x) * 0x01000193) ^ y) * 0x01000193) % buckets.Length;
			result = result < 0 ? result + buckets.Length : result;
			return result;
		}
	}

	// grid-sectors are a 1x1m square, following the PhysX and Box2D convention of tuning
	// for objects which are around "human scale."  We round instead of floor since objects
	// heuristically line-up on grid-lines (e.g. tile-based games), so we don't want to 
	// have lots of common precision singularities.

	void Hash(int id) {
		// mark all the sectors that this box overlaps
		int minX = Mathf.RoundToInt(slots[id].box.p0.x);
		int minY = Mathf.RoundToInt(slots[id].box.p0.y);
		int maxX = Mathf.RoundToInt(slots[id].box.p1.x);
		int maxY = Mathf.RoundToInt(slots[id].box.p1.y);
		for(int x=minX; x<=maxX; ++x)
		for(int y=minY; y<=maxY; ++y) {
			buckets[ HashBucket(x,y) ].Mark(id);
		}
	}

	void Unhash(int id) {
		// clear this box from these sectors before moving it
		int minX = Mathf.RoundToInt(slots[id].box.p0.x);
		int minY = Mathf.RoundToInt(slots[id].box.p0.y);
		int maxX = Mathf.RoundToInt(slots[id].box.p1.x);
		int maxY = Mathf.RoundToInt(slots[id].box.p1.y);
		for(int x=minX; x<=maxX; ++x)
		for(int y=minY; y<=maxY; ++y) {
			buckets[ HashBucket(x,y) ].Clear(id);
		}
	}

	void BroadPhase(AABB sweep) {
		// union all the sectors that this box overlaps to identify likely collisions
		int minX = Mathf.RoundToInt(sweep.p0.x);
		int minY = Mathf.RoundToInt(sweep.p0.y);
		int maxX = Mathf.RoundToInt(sweep.p1.x);
		int maxY = Mathf.RoundToInt(sweep.p1.y);
		broadphaseCandidates.Clear();
		for(int x=minX; x<=maxX; ++x)
		for(int y=minY; y<=maxY; ++y) {
			broadphaseCandidates.Union(buckets[ HashBucket(x,y) ]);
		}
	}

	// Internal record-keeping structs

	struct Collider {
		public AABB box;
		public int categoryMask;
		public int collisionMask;
		public int triggerMask;
		public object userData;

		public bool Collides(ref Collider c) {
			return (collisionMask & c.categoryMask) != 0 && box.Overlaps(c.box);
		}
		
		public bool Triggers(ref Collider c) {
			return (triggerMask & c.categoryMask) != 0 && box.Overlaps(c.box);
		}

	}

	struct Contact {
		public int collider;
		public int trigger;
	}

	#if UNITY_EDITOR
	void OnDrawGizmos() {
		if (!Application.isPlaying) {
			return;
		}
			
		// render bounding box in local frame
		Gizmos.color = Color.yellow;
		var occupiedSlots = freeSlots.Clone ();
		occupiedSlots.Negate();
		foreach(var slot in occupiedSlots.ListBits()) {
			var box = GetBounds(slot);
			Gizmos.DrawLine(box.p0, vec(box.p0.x, box.p1.y));
			Gizmos.DrawLine(vec(box.p0.x, box.p1.y), box.p1);
			Gizmos.DrawLine(box.p1, vec(box.p1.x, box.p0.y));
			Gizmos.DrawLine(vec(box.p1.x, box.p0.y), box.p0);

		}

	}
	#endif

}
