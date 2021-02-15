using UnityEngine;

namespace Core_BetterPenetration
{
    class DanPoint
    {
        internal Transform transform;
        internal Vector3 defaultLocalPosition;
        internal Vector3 defaultLocalScale;
        internal Vector3 defaultLossyScale;

        public DanPoint(Transform point)
        {
            this.transform = point;
            this.defaultLocalPosition = point.localPosition;
            this.defaultLocalScale = point.localScale;
            this.defaultLossyScale = point.lossyScale;
        }

        internal void ResetDanPoint()
        {
            transform.localPosition = defaultLocalPosition;
            transform.localScale = defaultLocalScale;
        }

        internal void ScaleDanGirth(float girthScale)
        {
            transform.localScale = new Vector3(defaultLocalScale.x * girthScale, defaultLocalScale.y * girthScale, defaultLocalScale.z);
        }
    }
}
