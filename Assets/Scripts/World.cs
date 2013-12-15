using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class World : GameBehaviour {

	// a database for querying scene elements.  maintains
	// the status of the scene, not the control flow.
	
	public static World inst;
	
	Location origin;
	BitArray floorMask;
	int maskWidth;
	int maskHeight;
	
	public bool InRange(Location loc) { 
		return loc.x >= origin.x && 
		       loc.y >= origin.y && 
		       loc.x < origin.x + maskWidth && 
		       loc.y < origin.y + maskHeight; 
	}
	
	public bool HasFloorTileAt(Location loc) { 
		return InRange(loc) && floorMask[(loc - origin).Address(maskWidth)]; 
	}	
	
	public bool ProperFloorTileAt(Location loc) {
		return HasFloorTileAt(loc) && !HasFloorTileAt(loc.Above);
	}
	
	public bool GetFloorExtents(Vector2 point, out Vector2 left, out Vector2 right) {
		
		// determine which tile this point rests on top of
		var tileLoc = Location.FromTopCenter(point);
		
		if (!ProperFloorTileAt(tileLoc)) {
			left = point;
			right = point;
			return false;
		}
		
		var leftLoc = tileLoc;
		while(ProperFloorTileAt(leftLoc.Left)) {
			leftLoc = leftLoc.Left;
		}
		var rightLoc = tileLoc;
		while(ProperFloorTileAt(rightLoc.Right)) {
			rightLoc = rightLoc.Right;
		}
		
		left = leftLoc.TopCenterPoint;
		right = rightLoc.TopCenterPoint;
		
		return true;
		
	}
		
	
	void Awake() {
		inst = this;
		InitializeFloorColliders();	
	}
	
	void OnDestroy() {
		if (inst == this) { inst = null; }
	}
	
	
	
	void InitializeFloorColliders() {
		
		// to prevent floors with internal edges, we hash the logical locations
		// of the floor tiles and then create horizontally-coalesced colliders
		// for runs of tiles	
		
		var floorObjects = GameObject.FindGameObjectsWithTag( FloorTag );
		var locToNode = new Dictionary<Location, GameObject>();
		
		var minLocation = new Location(9999, 9999);
		var maxLocation = new Location(-9999, -9999); 
		
		foreach(GameObject go in floorObjects) {
			var loc = Location.Approx(go.transform.localPosition);
			if (locToNode.ContainsKey(loc)) {
				Destroy(go);
			} else {
				locToNode[loc] = go;
			}
			minLocation = Location.Min(minLocation, loc);
			maxLocation = Location.Max(maxLocation, loc);
		}
		
		// initialize bitmask of floors
		origin = minLocation;
		maskWidth = maxLocation.x - minLocation.x + 1;
		maskHeight = maxLocation.y - minLocation.y + 1;
		floorMask = new BitArray(maskWidth * maskHeight);
		foreach(Location loc in locToNode.Keys) {
			floorMask[(loc - origin).Address(maskWidth)] = true;
		}
		
		
		// helper method
		Func< KeyValuePair<Location,GameObject> > popLoc = () => {
			var enumerator = locToNode.GetEnumerator();
			var hasVal = enumerator.MoveNext();
			Assert(hasVal, "Popping Location from Nonempty Hashset");
			var result = enumerator.Current;
			locToNode.Remove(result.Key);
			return result;
		};
		
		while(locToNode.Count > 0) {
			var kvp = popLoc();
			var xmin = kvp.Key.x;
			var xmax = kvp.Key.x;
			var y = kvp.Key.y;
			var obj = kvp.Value;
			// search left for adjacent nodes
			while(locToNode.ContainsKey(Loc(xmin-1,y))) {
				--xmin;
				obj = locToNode[ Loc(xmin,y) ];
				locToNode.Remove( Loc(xmin,y) );
			}
			// search right for adjacent nodes
			while(locToNode.ContainsKey(Loc(xmax+1,y))) {
				locToNode.Remove( Loc(++xmax, y) );
			}
			// create the box
			var box = obj.AddComponent<BoxCollider2D>();
			box.size = vec(1 + xmax - xmin, 1f);//, 2);
			box.center = vec(0.5f * box.size.x - 0.5f, 0f);//, 0);
		}
		
	}
}
