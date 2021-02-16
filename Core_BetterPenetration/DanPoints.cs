using System.Collections.Generic;
using UnityEngine;

namespace Core_BetterPenetration
{
    class DanPoints
    {
        internal Transform danTop;
        internal List<DanPoint> danPoints;

        public DanPoints(List<Transform> danTransforms, Transform top)
        {
            danTop = top;
            danPoints = new List<DanPoint>();

            foreach (var transform in danTransforms)
                danPoints.Add(new DanPoint(transform));
        }

        internal void AimDanPoints(List<Vector3> newDanPositions)
        {
            if (newDanPositions.Count != danPoints.Count)
                return;

            Quaternion danQuaternion = Quaternion.identity;
            for (int point = 0; point < danPoints.Count - 1; point++)
            {
                Vector3 forwardVector = Vector3.Normalize(newDanPositions[point + 1] - newDanPositions[point]);
                danQuaternion = Quaternion.LookRotation(forwardVector, Vector3.Cross(forwardVector, danTop.right));
                danPoints[point].transform.SetPositionAndRotation(newDanPositions[point], danQuaternion);
            }

            danPoints[danPoints.Count - 1].transform.SetPositionAndRotation(newDanPositions[danPoints.Count - 1], danQuaternion);
        }

#if !AI_STUDIO && !HS2_STUDIO
        internal void SquishDanGirth(float girthScaleFactor)
        {
            float halfGirthScaleFactor = (1 - (1 - 1 / girthScaleFactor) / 2);
            float inverseGirthScaleFactor = 1 / girthScaleFactor;

            for (int point = 0; point < danPoints.Count; point++)
            {
                if (point == 0)
                    danPoints[point].ScaleDanGirth(girthScaleFactor);
                else if (point <= danPoints.Count / 3)
                    danPoints[point].ScaleDanGirth(1.0f);
                else if (point >= danPoints.Count * 2 / 3)
                    danPoints[point].ScaleDanGirth(inverseGirthScaleFactor);
                else
                    danPoints[point].ScaleDanGirth(halfGirthScaleFactor);
            }
        }
#else
        internal void SquishDanGirth(float girthScaleFactor)
        {
            float halfGirthScaleFactor = (1 + (girthScaleFactor - 1) / 2);

            for (int point = 0; point < danPoints.Count * 2 / 3; point++)
            {
                if (point <= danPoints.Count / 3)
                    danPoints[point].ScaleDanGirth(girthScaleFactor);
                else
                    danPoints[point].ScaleDanGirth(halfGirthScaleFactor);
            }
        }
#endif

        internal void ResetDanPoints()
        {
            if (danPoints == null || danPoints.Count <= 0)
                return;

            foreach (var danPoint in danPoints)
                danPoint.ResetDanPoint();

            Quaternion danRotation = danPoints[0].transform.rotation;
            for (int point = 1; point < danPoints.Count - 1; point++)
                danPoints[point].transform.rotation = danRotation;
        }

        internal Vector3 GetDanStartPosition()
        {
            if (danPoints == null)
                return Vector3.zero;

            return danPoints[0].transform.position;
        }

        internal float GetDanLossyScale()
        {
            if (danPoints == null || danPoints?[0].transform == null)
                return 1;

            return danPoints[0].transform.lossyScale.z;
        }
    }
}
