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
	
	public const string FloorTag = "Floor";	
	
	public const int BackgroundLayer = 8;
	public const int BackgroundMask = 1 << 8;
	
	public static Location Loc(int x, int y) { return new Location(x, y); }
	
	public float deltaTime { get { return Mathf.Min(Time.deltaTime, 1f/40f);} }
	
	public const float collisionSlop = 0.025f; // how much we shake off each collision surface to avoid error accumulation
	
}
