using UnityEngine;

namespace Core_BetterPenetration
{
    class DanPoints
    {
        public Transform danStart;
        public Transform danEnd;
        public Transform danTop;
        public Transform[] danMid;

        public DanPoints(Transform start, Transform end, Transform top, Transform[] mid = null)
        {
            danStart = start;
            danEnd = end;
            danTop = top;
            danMid = mid;
        }
    }
}
