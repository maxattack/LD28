using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class World : GameBehaviour {

	// a database for querying scene elements.  maintains
	// the status of the scene, not the control flow.
	
	public static World inst;
	
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
		foreach(GameObject go in floorObjects) {
			locToNode[ Location.Approx(go.transform.position) ] = go;
		}
		
		// helper method
		Func< KeyValuePair<Location,GameObject> > popLoc = () => {
			var enumerator = locToNode.GetEnumerator();
			var hasVal = enumerator.MoveNext();
			Assert(hasVal);
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
			box.size = vec(1 + xmax - xmin, 1f);
			box.center = vec(0.5f * box.size.x - 0.5f, 0f);
		}
		
	}
}
