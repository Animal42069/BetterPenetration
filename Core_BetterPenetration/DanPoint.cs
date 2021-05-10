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
            if (transform == null)
                return;

            transform.localPosition = defaultLocalPosition;
            transform.localScale = defaultLocalScale;
        }

        internal void SetDanPointRotation(Quaternion rotation)
        {
            if (transform == null)
                return;

            transform.rotation = rotation;
        }

        internal Quaternion GetDanPointRotation()
        {
            if (transform == null)
                return Quaternion.identity;

            return transform.rotation;
        }

        internal void ScaleDanGirth(float girthScale)
        {
            if (transform == null)
                return;

            transform.localScale = new Vector3(defaultLocalScale.x * girthScale, defaultLocalScale.y * girthScale, transform.localScale.z);
        }
    }
}
