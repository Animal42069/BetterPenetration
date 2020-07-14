using System.Collections.Generic;
using UnityEngine;

namespace AI_BetterPenetration
{
    class ConstrainPoints
    {
        public List<Transform> frontConstrainPoints;
        public List<Transform> backConstrainPoints;
        public Transform headConstrainPoint;

        public ConstrainPoints()
        {
            frontConstrainPoints = new List<Transform>();
            backConstrainPoints = new List<Transform>();
        }

        public ConstrainPoints(List<Transform> frontPoints, List<Transform> backPoints, Transform headPoint)
        {
            frontConstrainPoints = frontPoints;
            backConstrainPoints = backPoints;
            headConstrainPoint = headPoint;
        }
    }
}
