using System.Collections.Generic;
using UnityEngine;

namespace Core_BetterPenetration
{
    class CollisionPoints
    {
        public List<CollisionPoint> frontCollisionPoints;
        public List<CollisionPoint> backCollisionPoints;
        public Transform headCollisionPoint;

        public CollisionPoints()
        {
            frontCollisionPoints = new List<CollisionPoint>();
            backCollisionPoints = new List<CollisionPoint>();
        }

        public CollisionPoints(List<CollisionPoint> frontPoints, List<CollisionPoint> backPoints, Transform headPoint)
        {
            frontCollisionPoints = frontPoints;
            backCollisionPoints = backPoints;
            headCollisionPoint = headPoint;
        }
    }
}
