using UnityEngine;

namespace HS2_BetterPenetration
{
    class DanPoints
    {
        public Transform danStart;
        public Transform danEnd;
        public Transform danTop;
        public Transform danSao00;
        public Transform danSao01;

        public DanPoints(Transform start, Transform end, Transform top, Transform sao00, Transform sao01)
        {
            danStart = start;
            danEnd = end;
            danTop = top;
            danSao00 = sao00;
            danSao01 = sao01;
        }
    }
}
