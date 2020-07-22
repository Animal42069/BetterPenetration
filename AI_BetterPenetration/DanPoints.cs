using UnityEngine;

namespace AI_BetterPenetration
{
    class DanPoints
    {
        public Transform danStart;
        public Transform danEnd;
        public Transform danTop;

        public DanPoints(Transform start,Transform end, Transform top)
        {
            danStart = start;
            danEnd = end;
            danTop = top;
        }
    }
}