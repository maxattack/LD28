using System;
using System.Collections;
using UnityEngine;

public struct Location {

	// struct to represent logical tile
	// locations
	
	public int x, y;
	
	public Location(int ax, int ay) { x = ax; y = ay; }
	
	public static Location Approx(Vector2 pos) {
		return new Location(
			Mathf.RoundToInt(pos.x),
			Mathf.RoundToInt(pos.y)
		);
	}

	public static Location Approx(Vector3 pos) {
		return new Location(
			Mathf.RoundToInt(pos.x),
			Mathf.RoundToInt(pos.y)
		);
	}
	
	public static Location Min(Location u, Location v) {
		return new Location(
			Math.Min(u.x, v.x),
			Math.Min(u.y, v.y)
		);
	}
	
	public static Location Max(Location u, Location v) {
		return new Location(
			Math.Max(u.x, v.x),
			Math.Max(u.y, v.y)
		);
	}
	
	public static Location operator+(Location u, Location v) {
		return new Location(u.x + v.x, u.y + v.y);
	}
	
	public static Location operator-(Location u, Location v) {
		return new Location(u.x - v.x, u.y - v.y);
	}
	
	public int Address(int pitch) {
		return y * pitch + x;
	}
	
	public Location Above { get { return new Location(x, y+1); } }
	public Location Below { get { return new Location(x, y-1); } }
	public Location Left { get { return new Location(x-1, y); } }
	public Location Right { get { return new Location(x+1, y); } }
	
	public Vector2 TopCenterPoint { get { return new Vector2(x+0.5f, y+1f); } }
	public static Location FromTopCenter(Vector2 v) {
		return new Location(
			Mathf.FloorToInt(v.x),
			Mathf.FloorToInt(v.y-0.5f)
		);
	}
}

public class GameBehaviour : CustomBehaviour {
	
	// A bunch of static methods take save on typing... :P
	
	public const string FloorTag = "Floor";	
	public const int BackgroundLayer = 8;
	public const int BackgroundMask = 1 << 8;
	public const int ItemsLayer = 9;
	public const int ItemsMask = 1 << 9;
	
	public static Location Loc(int x, int y) { return new Location(x, y); }
	
	public float deltaTime { get { return Mathf.Min(Time.deltaTime, 1f/40f);} }
	
	public const float collisionSlop = 0.025f; // how much we shake off each collision surface to avoid error accumulation
	
	public static Collider2D[] queryBuffer = new Collider2D[8];
	
}
