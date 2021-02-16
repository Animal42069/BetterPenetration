using UnityEngine;

namespace Core_BetterPenetration
{
    class CollisionPoint
    {
        internal Transform transform;
        internal CollidonPointInfo info;

        public CollisionPoint(Transform point, CollidonPointInfo collisionInfo)
        {
            transform = point;
            info = collisionInfo;
        }
    }

    class CollidonPointInfo
    {
        internal string name;
        internal float offset;
        internal bool inward;

        public CollidonPointInfo(string transformName, float offsetValue, bool inwardDirection)
        {
            name = transformName;
            offset = offsetValue;
            inward = inwardDirection;
        }
    }
}
