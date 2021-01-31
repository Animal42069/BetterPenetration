using System.Collections.Generic;
using UnityEngine;

namespace Core_BetterPenetration
{
    class DanPoints
    {
        public Transform danTop;
        public Transform danStart;
        public Transform danEnd;
        public List<Transform> danPoints;
        public List<Vector3> danScale;
        public List<Vector3> danLossyScale;

        public DanPoints(Transform start, Transform end, Transform top, Transform[] mid = null)
        {
            danStart = start;
            danEnd = end;
            danTop = top;

            danPoints = new List<Transform>();
            danScale = new List<Vector3>();
            danLossyScale = new List<Vector3>();

            danPoints.Add(start);
            danScale.Add(start.localScale);
            danLossyScale.Add(start.lossyScale);

            if (mid != null)
            {
                foreach (var midPoint in mid)
                {
                    danPoints.Add(midPoint);
                    danScale.Add(midPoint.localScale);
                    danLossyScale.Add(midPoint.lossyScale);
                }
            }

            danPoints.Add(end);
            danScale.Add(end.localScale);
            danLossyScale.Add(end.lossyScale);
        }
    }
}
