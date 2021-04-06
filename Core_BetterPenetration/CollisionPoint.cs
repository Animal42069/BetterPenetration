using UnityEngine;

namespace Core_BetterPenetration
{
    class CollisionPoint
    {
        internal Transform transform;
        internal CollisionPointInfo info;

        public CollisionPoint(Transform point, CollisionPointInfo collisionInfo)
        {
            transform = point;
            info = collisionInfo;
        }

        public void UpdateCollisionInfo(CollisionPointInfo collisionInfo)
        {
            info = collisionInfo;
        }

    }

    class CollisionPointInfo
    {
        internal string name;
        internal float offset;
        internal bool inward;

        public CollisionPointInfo(string transformName, float offsetValue, bool inwardDirection)
        {
            name = transformName;
            offset = offsetValue;
            inward = inwardDirection;
        }
    }
}
