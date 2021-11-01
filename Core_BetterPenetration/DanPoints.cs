using System.Collections.Generic;
using UnityEngine;

namespace Core_BetterPenetration
{
    class DanPoints
    {
        internal Transform danTop;
        internal List<DanPoint> danPoints;
        internal Transform danEnd;
        internal Transform bellyEnd;

        public DanPoints(List<Transform> danTransforms, Transform top, Transform end = null, Transform bellyEnd = null)
        {
            danTop = top;
            danEnd = end;
            this.bellyEnd = bellyEnd;
            danPoints = new List<DanPoint>();

            foreach (var transform in danTransforms)
                danPoints.Add(new DanPoint(transform));
        }

        internal void AimDanPoints(List<Vector3> newDanPositions, bool aimTop, Vector3 bellyEndPosition)
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

            if (danEnd != null)
                danEnd.transform.SetPositionAndRotation(newDanPositions[danPoints.Count - 1], danQuaternion);

            if (bellyEnd != null && bellyEndPosition != Vector3.zero)
                bellyEnd.transform.SetPositionAndRotation(bellyEndPosition, danQuaternion);

            if (aimTop)
                AimDanTop();
        }

        internal void AimDanTop()
        {
            danTop.transform.rotation = danPoints[0].transform.rotation;
        }

#if !STUDIO
        internal void SquishDanGirth(float girthScaleFactor)
        {
            float points = danPoints.Count - 1;
            if (points <= 0)
                return;

            float inverseScaleFactor = (girthScaleFactor - (girthScaleFactor - 1) / points) / girthScaleFactor;

            for (int point = 0; point < danPoints.Count; point++)
            {
                if (point == 0)
                    danPoints[point].ScaleDanGirth(girthScaleFactor);
                else
                    danPoints[point].ScaleDanGirth(inverseScaleFactor);
            }
        }
#else
        internal void SquishDanGirth(float girthScaleFactor)
        {
            float points = danPoints.Count - 2;
            if (points <= 0)
                return;

            float inverseScaleFactor = (girthScaleFactor - (girthScaleFactor - 1) / points) / girthScaleFactor;

            for (int point = 1; point < danPoints.Count; point++)
            {
                if (point == 1)
                    danPoints[point].ScaleDanGirth(girthScaleFactor);
                else
                    danPoints[point].ScaleDanGirth(inverseScaleFactor);
            }
        }
#endif

        internal void ResetDanPoints()
        {
            if (danPoints == null || danPoints.Count <= 0)
                return;

            foreach (var danPoint in danPoints)
                danPoint.ResetDanPoint();

            Quaternion danRotation = danPoints[0].GetDanPointRotation();
            for (int point = 1; point < danPoints.Count - 1; point++)
                danPoints[point].SetDanPointRotation(danRotation);
        }

        internal Vector3 GetDanStartPosition()
        {
            if (danPoints == null)
                return Vector3.zero;

            return danPoints[0].transform.position;
        }

        internal Vector3 GetDanEndPosition()
        {
            if (danPoints == null)
                return Vector3.zero;

            return danPoints[danPoints.Count - 1].transform.position;
        }

        internal float GetDanLossyScale()
        {
            if (danPoints == null || danPoints?[0].transform == null)
                return 1;

            return danPoints[0].transform.lossyScale.z;
        }
    }
}
