using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PooledObject : CustomBehaviour {
	
	// subclass this to treat a behaviour as the basis 
	// of a poolable object.  Stores simple metadata
	// related to pooling
	
	internal GameObjectPool pool;
	internal PooledObject next;
	
	public void PutBack() {
		
		// Return to the pool, if it's still around, otherwise
		// just plain-old destroy this object.  Either way it'll
		// be out of the scene.
		if (pool) {
			pool.PutBack( this );
		} else {
			Destroy( gameObject );
		}
		
	}
}

public class GameObjectPool : CustomBehaviour {
	
	public static GameObjectPool CreatePoolFor(PooledObject prefab) {
		// make a builder method for this just to decouple the
		// implementation details a bit
		
		var result = new GameObject(prefab.name + "Pool", typeof(GameObjectPool))
			.GetComponent<GameObjectPool>();
		result.prefab = prefab;
		return result;		
	}
	
	// designer params (forces designers to subclass PooledObject)
	public PooledObject prefab;
	
	// head reference for a plain-old linked-list
	PooledObject firstIdle;
	
	// also maintain an active list? (requires doubly-linked list)
	
	public PooledObject TakeOut() {
		if (firstIdle != null) {
		
			// activate an old instance
			var result = firstIdle;
			firstIdle = firstIdle.next;		
			result.next = null;
			result.gameObject.SetActive( true );
			return result;
			
		} else {
			
			// instantiate a new instance
			var result = Dup( prefab );
			result.pool = this;
			return result;
			
		}
	}
	
	public T TakeOut<T>() where T : PooledObject {
		var result = TakeOut() as T;
		Assert(result != null, "Pool Always Produces a new Instance");
		return result;
	}	
	
	public PooledObject TakeOut(Vector3 position) {
		if (firstIdle != null) {
		
			// activate an old instance
			var result = firstIdle;
			firstIdle = firstIdle.next;		
			result.next = null;
			result.transform.position = position;
			result.gameObject.SetActive( true );
			return result;
			
		} else {
			
			// instantiate a new instance
			var result = Dup( prefab, position );
			result.pool = this;
			return result;
			
		}
	}
	
	public T TakeOut<T>(Vector3 position) where T : PooledObject {
		var result = TakeOut(position) as T;
		Assert(result != null, "Pool Always Produces a New Instance");
		return result;
	}
	
	public void PutBack(PooledObject obj) {
		Assert(obj.pool == this, "Instance Came from This Pool");

		// prepend to the head of the linked list
		obj.gameObject.SetActive( false );
		obj.next = firstIdle;
		firstIdle = obj;
	}
	
	public void Drain() {
	
		// destroy all the idle instances
		while(firstIdle != null) {
			var prev = firstIdle;
			firstIdle = firstIdle.next;
			Destroy( prev.gameObject );
		}
		
	}
	
	void OnDestroy() {
	
		// take out the idle instances when you diiieee
		Drain();
		
	}
	
}
