using UnityEngine;
using System.Collections;

public struct Location {
	public int x, y;
	
	public Location(int ax, int ay) { x = ax; y = ay; }
	
	public static Location Approx(Vector3 pos) {
		return new Location(
			Mathf.RoundToInt(pos.x),
			Mathf.RoundToInt(pos.y)
		);
	}
}

public class GameBehaviour : CustomBehaviour {
	
	// A bunch of static methods take save on typing... :P
	
	public static string FloorTag = "Floor";	
	
	public static Location Loc(int x, int y) { return new Location(x, y); }

}
