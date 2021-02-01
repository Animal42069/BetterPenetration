using System;
using UnityEngine;

namespace Core_BetterPenetration
{
    class DanPoint
    {
        public Transform transform;
        public Vector3 defaultLocalPosition;
        public Quaternion defaultLocalRotation;
        public Vector3 defaultLocalScale;
        public Vector3 defaultLossyScale;

        public DanPoint(Transform point)
        {
            transform = point;
            defaultLocalPosition = point.localPosition;
            defaultLocalRotation = point.localRotation;
            defaultLocalScale = point.localScale;
            defaultLossyScale = point.lossyScale;
        }

        public void ResetDanPoint()
        {
            transform.localPosition = defaultLocalPosition;
            transform.localRotation = defaultLocalRotation;
            transform.localScale = defaultLocalScale;
        }

        public void ScaleDanGirth(float girthScale)
        {
            transform.localScale = new Vector3(defaultLocalScale.x * girthScale, defaultLocalScale.y * girthScale, defaultLocalScale.z);
        }
    }
}
