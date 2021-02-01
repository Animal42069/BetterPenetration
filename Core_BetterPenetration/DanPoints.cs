using System.Collections.Generic;
using UnityEngine;

namespace Core_BetterPenetration
{
    class DanPoints
    {
        public Transform danTop;
        public List<DanPoint> danPoints;

        public DanPoints(Transform start, Transform end, Transform top, Transform[] mid = null)
        {
            danTop = top;

            danPoints = new List<DanPoint>();
            danPoints.Add(new DanPoint(start));
            if (mid != null)
            {
                foreach (var midPoint in mid)
                    danPoints.Add(new DanPoint(midPoint));
            }
            danPoints.Add(new DanPoint(end));
        }

        public void AimDanPoints(List<Vector3> newDanPositions)
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

        public void SquishDanGirth(float girthScaleFactor)
        {
            float halfGirthScaleFactor = (1 - (1 - 1 / girthScaleFactor) / 2);
            float inverseGirthScaleFactor = 1 / girthScaleFactor;

            for (int point = 0; point < danPoints.Count; point++)
            {
                if (point <= danPoints.Count / 3)
                    danPoints[point].ScaleDanGirth(girthScaleFactor);
                else if (point >= danPoints.Count * 2 / 3)
                    danPoints[point].ScaleDanGirth(inverseGirthScaleFactor);
                else
                    danPoints[point].ScaleDanGirth(halfGirthScaleFactor);
            }
        }

        public void ResetDanPoints()
        {
            foreach (var danPoint in danPoints)
                danPoint.ResetDanPoint();
        }

        public Vector3 GetDanStartPosition()
        {
            return danPoints[0].transform.position;
        }
    }
}
