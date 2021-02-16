using System.Collections.Generic;
using UnityEngine;

namespace Core_BetterPenetration
{
    class CollisionPoints
    {
        internal List<CollisionPoint> frontCollisionPoints;
        internal List<CollisionPoint> backCollisionPoints;

        public CollisionPoints()
        {
            frontCollisionPoints = new List<CollisionPoint>();
            backCollisionPoints = new List<CollisionPoint>();
        }

        public CollisionPoints(List<CollisionPoint> frontPoints, List<CollisionPoint> backPoints)
        {
            frontCollisionPoints = frontPoints;
            backCollisionPoints = backPoints;
        }
    }
}
