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

using UnityEngine;
using System.Collections;

public class CollisionAABB : CustomBehaviour {

	public Rect localBoundingBox;
	public LayerMask collisionMask;
	int colliderId;
	Transform node;
	CollisionSystem context;

	void Awake() {

		// save reference to collision context
		context = CollisionSystem.GetInstance();

		// save reference to node
		node = this.transform;

		// convert local rect to contextual AABB
		var p0 = node.TransformPoint( 
			localBoundingBox.x, 
			localBoundingBox.y, 
			0 
		).xy();
		var p1 = node.TransformPoint(
			localBoundingBox.x + localBoundingBox.width, 
			localBoundingBox.y + localBoundingBox.height, 
			0
		).xy();

		// register AABB with context
		colliderId = context.AddCollider(
			new AABB() { p0 = p0, p1 = p1 },
			0x00000001 << gameObject.layer, 
			collisionMask.value,
			0,
			this
		);

	}

	public Collision Move(Vector2 offset) {

		// TODO: Break very-large motions into substeps?

		// forward request to context, apply result
		var result = context.Move(colliderId, offset);
		var p0 = context.GetBounds (colliderId).p0;
		node.position = vec(
			p0 - vec(localBoundingBox.x, localBoundingBox.y), 
			node.position.z
		);
		return result;

	}
	#if UNITY_EDITOR
	void OnDrawGizmos() {
		if (!Application.isPlaying) {

			// render bounding box in local frame
			Gizmos.color = Color.yellow;
			var node = (this.node ? this.node : transform);
			var p0 = node.TransformPoint( 
	             localBoundingBox.x, 
	             localBoundingBox.y, 
	             0 
             ).xy();
			var p1 = node.TransformPoint(
				localBoundingBox.x + localBoundingBox.width, 
				localBoundingBox.y + localBoundingBox.height, 
				0
			).xy();
			Gizmos.DrawLine(p0, vec(p0.x, p1.y));
			Gizmos.DrawLine(vec(p0.x, p1.y), p1);
			Gizmos.DrawLine(p1, vec(p1.x, p0.y));
			Gizmos.DrawLine(vec(p1.x, p0.y), p0);

		}

	}
	#endif


}
