using System;
using UnityEngine;

namespace Core_BetterPenetration
{
    class DanPoint
    {
        public Transform transform;
        public Vector3 defaultLocalPosition;
        public Vector3 defaultLocalScale;
        public Vector3 defaultLossyScale;

        public DanPoint(Transform point)
        {
            this.transform = point;
            this.defaultLocalPosition = point.localPosition;
            this.defaultLocalScale = point.localScale;
            this.defaultLossyScale = point.lossyScale;
        }

        public void ResetDanPoint()
        {
            transform.localPosition = defaultLocalPosition;
            transform.localScale = defaultLocalScale;
        }

        public void ScaleDanGirth(float girthScale)
        {
            transform.localScale = new Vector3(defaultLocalScale.x * girthScale, defaultLocalScale.y * girthScale, defaultLocalScale.z);
        }
    }
}
