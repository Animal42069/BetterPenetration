using UnityEngine;

namespace Core_BetterPenetration
{
    class CollisionPoint
    {
        public Transform transform;
        public CollidonPointInfo info;

        public CollisionPoint(Transform point, CollidonPointInfo collisionInfo)
        {
            transform = point;
            info = collisionInfo;
        }
    }

    class CollidonPointInfo
    {
        public string name;
        public float offset;
        public bool inward;

        public CollidonPointInfo(string transformName, float offsetValue, bool inwardDirection)
        {
            name = transformName;
            offset = offsetValue;
            inward = inwardDirection;
        }
    }
}
