using System;
using UnityEngine;

namespace Core_BetterPenetration
{
    // Creates a Plane defined by two non-parallel lines
    class TwistedPlane
    {
        public Vector3 firstOrigin;
        public Vector3 firstVector;
        public Vector3 secondOrigin;
        public Vector3 secondVector;
        public Vector3 forwardVector;
        public Vector3 twistVector;

        public TwistedPlane(Vector3 firstOrig, Vector3 firstVec, Vector3 secondOrig, Vector3 secondVec)
        {
            firstOrigin = firstOrig;
            firstVector = firstVec;
            secondOrigin = secondOrig;
            secondVector = secondVec;
            forwardVector = secondOrig - firstOrig;
            twistVector = secondVec - firstVec;
        }

        public Vector3 GetPoint(float tDistAlongFirstVector, float tDistAlongForwardVector)
        {
            return (firstOrigin * (1 - tDistAlongForwardVector)
                  + firstVector * tDistAlongFirstVector * (1 - tDistAlongForwardVector)
                  + secondOrigin * tDistAlongForwardVector
                  + secondVector * tDistAlongFirstVector * tDistAlongForwardVector);
        }

        public bool FindIntersectValues(Vector3 lineVector, Vector3 offsetVector, Vector3 FVxLV, Vector3 TWxLV, Vector3 OFxLV, Vector3 LVxFW, double u, out double v, out double t)
        {
            double Cu = 0;
            v = 0;
            t = 0;

            for (MathHelpers.Axis axis = MathHelpers.Axis.X; axis <= MathHelpers.Axis.Z; axis++)
            {
                if (axis == MathHelpers.Axis.X)
                {
                    Cu = FVxLV.x + TWxLV.x * u;
                    if (!MathHelpers.ApproximatelyZero(Cu))
                    {
                        v = (OFxLV.x + LVxFW.x * u) / Cu;
                        break;
                    }
                }
                else if (axis == MathHelpers.Axis.Y)
                {
                    Cu = FVxLV.y + TWxLV.y * u;
                    if (!MathHelpers.ApproximatelyZero(Cu))
                    {
                        v = (OFxLV.y + LVxFW.y * u) / Cu;
                        break;
                    }
                }
                else
                {
                    Cu = FVxLV.z + TWxLV.z * u;
                    if (!MathHelpers.ApproximatelyZero(Cu))
                    {
                        v = (OFxLV.z + LVxFW.z * u) / Cu;
                        break;
                    }
                }
            }

            if (MathHelpers.ApproximatelyZero(Cu))
                return false;

            if (!MathHelpers.ApproximatelyZero(lineVector.x))
                t = (-offsetVector.x + firstVector.x * v + (forwardVector.x + twistVector.x * v) * u) / lineVector.x;
            else if (!MathHelpers.ApproximatelyZero(lineVector.y))
                t = (-offsetVector.y + firstVector.y * v + (forwardVector.y + twistVector.y * v) * u) / lineVector.y;
            else if (!MathHelpers.ApproximatelyZero(lineVector.z))
                t = (-offsetVector.z + firstVector.z * v + (forwardVector.z + twistVector.z * v) * u) / lineVector.z;
            else
                return false;

            return true;
        }

        // return true if they interstect
        public bool IntersectLineOnTwistedPlane(Vector3 lineStart, Vector3 lineEnd, bool bExtendPlaneBeyondFirstVector, bool bExtendPlaneBeyoneSecondVector, out Vector3 intersectionPoint, out Vector3 intersectForwardVector, out float distanceToSecondVector)
        {
            distanceToSecondVector = 0;
            intersectForwardVector = forwardVector;
            intersectionPoint = lineEnd;

            Vector3 lineVector = lineEnd - lineStart;
            Vector3 offsetVector = lineStart - firstOrigin;
            Vector3 TVxFW = Vector3.Cross(twistVector, forwardVector);
            Vector3 FVxFW = Vector3.Cross(firstVector, forwardVector);
            Vector3 OFxTW = Vector3.Cross(offsetVector, twistVector);
            Vector3 OFxFV = Vector3.Cross(offsetVector, firstVector);

            double quadraticA = lineVector.x * TVxFW.x + lineVector.y * TVxFW.y + lineVector.z * TVxFW.z;
            double quadraticB = lineVector.x * (FVxFW.x + OFxTW.x) + lineVector.y * (FVxFW.y + OFxTW.y) + lineVector.z * (FVxFW.z + OFxTW.z);
            double quadraticC = lineVector.x * OFxFV.x + lineVector.y * OFxFV.y + lineVector.z * OFxFV.z;
            if (MathHelpers.SolveQuadratic(quadraticA, quadraticB, quadraticC, out double u1, out double u2) == false)
                return false;

            double t1 = 0, t2 = 0;
            Vector3 FVxLV = Vector3.Cross(firstVector, lineVector);
            Vector3 TWxLV = Vector3.Cross(twistVector, lineVector);
            Vector3 OFxLV = Vector3.Cross(offsetVector, lineVector);
            Vector3 LVxFW = Vector3.Cross(lineVector, forwardVector);

            bool bIntersect1Found = false;
            if ((u1 < 4) && (u1 > -4) && (u1 >= 0 || bExtendPlaneBeyondFirstVector) && (u1 <= 1 || bExtendPlaneBeyoneSecondVector))
            {
                bIntersect1Found = this.FindIntersectValues(lineVector, offsetVector, FVxLV, TWxLV, OFxLV, LVxFW, u1, out _, out t1);
                if (bIntersect1Found && t1 < 0 || t1 > 1)
                    bIntersect1Found = false;

            }

            bool bIntersect2Found = false;
            if ((u2 < 4) && (u2 > -4) && (u2 >= 0 || bExtendPlaneBeyondFirstVector) && (u2 <= 1 || bExtendPlaneBeyoneSecondVector))
            {
                bIntersect2Found = this.FindIntersectValues(lineVector, offsetVector, FVxLV, TWxLV, OFxLV, LVxFW, u2, out _, out t2);
                if (bIntersect2Found && t2 < 0 || t2 > 1)
                    bIntersect2Found = false;
            }

            if (!bIntersect1Found && !bIntersect2Found)
                return false;

            float t = 0;
            if (bIntersect1Found)
            {
                Console.WriteLine($"t1 {t1}");
                t = (float)t1;
            }

            if (bIntersect2Found && (!bIntersect1Found || (bIntersect1Found && t2 < t1)))
            {
                Console.WriteLine($"t2 {t2}");
                t = (float)t2;
            }

            intersectionPoint = lineStart + lineVector * t;
            Vector3 intersectFirst = MathHelpers.CastSegmentToSegment(lineStart, lineVector, firstOrigin, firstVector);
            Vector3 intersectSecond = MathHelpers.CastSegmentToSegment(lineStart, lineVector, secondOrigin, secondVector);

            intersectForwardVector = Vector3.Normalize(intersectSecond - intersectFirst);
            distanceToSecondVector = Vector3.Distance(intersectSecond, intersectionPoint);

            return true;
        }

        public Vector3 ConstrainLineToTwistedPlane(Vector3 lineStart, Vector3 lineEnd, ref float lineLength, float minLineLength, ref bool bExtendPlaneBeyondStart, bool bExtendPlaneBeyondEnd, out bool bHitPointFound)
        {
            Vector3 newLineEnd;
            Vector3 lineVector = Vector3.Normalize(lineEnd - lineStart);
            bHitPointFound = false;

            // create a normal plane approximation to determine side, not the most accurate but close enough
            Vector3 planeRightVector = Vector3.Normalize(firstVector + secondVector);
            Vector3 planeUpVector = Vector3.Normalize(Vector3.Cross(planeRightVector, lineVector));
            Plane upPlane = new Plane(planeUpVector, firstOrigin + lineVector / 2);
            bool bAbovePlane = upPlane.GetSide(lineEnd);
            if (!bAbovePlane)
            {
                bExtendPlaneBeyondStart = false;
                return lineEnd;
            }

            /*     Console.WriteLine($"upPlane.normal {upPlane.normal}");
                 Console.WriteLine($"upPlane.distance {upPlane.distance}");
                 Console.WriteLine($"bAbovePlane {bAbovePlane}");
                 Console.WriteLine($"IntersectLineOnTwistedPlane");
                 Console.WriteLine($"lineStart {lineStart}");
                 Console.WriteLine($"lineStart {lineEnd}");
            */
            bool bIntersectFound = IntersectLineOnTwistedPlane(lineStart, lineEnd, bExtendPlaneBeyondStart, bExtendPlaneBeyondEnd, out Vector3 hitPoint, out Vector3 lineForwardVector, out float distanceToEdgeOfPlane);

            if (!bIntersectFound)
            {
                bExtendPlaneBeyondStart = false;
                return lineEnd;
            }

        //    Console.WriteLine("bIntersectFound");
            double hitDistance = Vector3.Distance(hitPoint, lineStart);
         //   Console.WriteLine($"hitDistance {hitDistance} lineLength {lineLength} minLineLength {minLineLength}");
            if (hitDistance > lineLength)
            {
                bExtendPlaneBeyondStart = false;
                return lineEnd;
            }

            if (hitDistance > minLineLength)
            {
                lineLength = (float)hitDistance;
                bHitPointFound = true;
                bExtendPlaneBeyondStart = false;
                return hitPoint;
            }

            lineLength = minLineLength;

            double angleLineToPlane = (double)MathHelpers.DegToRad(Vector3.Angle(lineVector, -lineForwardVector));
            MathHelpers.SolveSSATriangle(lineLength, hitDistance, angleLineToPlane, out double distanceAlongPlane, out _, out _);
           

     //           double angleLineToPlane = (double)MathHelpers.DegToRad(Vector3.Angle(lineVector, -lineForwardVector));
      //      double angleNewLineToPlane = (double)Math.Asin(hitDistance * Math.Sin(angleLineToPlane) / lineLength);
     //       double angleLineToNewLine = (double)Math.PI - angleLineToPlane - angleNewLineToPlane;
     //       float distanceAlongPlane = (float)(lineLength * Math.Sin(angleLineToNewLine) / Math.Sin(angleLineToPlane));

       //     Console.WriteLine($"hitPoint {hitPoint.x:F3}, {hitPoint.y:F3}, {hitPoint.z:F3}");
       //     Console.WriteLine($"lineForwardVector {lineForwardVector.x:F3}, {lineForwardVector.y:F3}, {lineForwardVector.z:F3}");
       //     Console.WriteLine($"bExtendPlaneBeyondEnd {bExtendPlaneBeyondEnd} distanceAlongPlane {distanceAlongPlane} distanceToEdgeOfPlane {distanceToEdgeOfPlane}");

            if (!bExtendPlaneBeyondEnd)
            {
                if (distanceAlongPlane > distanceToEdgeOfPlane)
                {
                    newLineEnd = hitPoint + distanceToEdgeOfPlane * lineForwardVector;
                    newLineEnd = lineStart + Vector3.Normalize(newLineEnd - lineStart) * lineLength;
                    bExtendPlaneBeyondStart = true;
                    return newLineEnd;
                }
            }
         
            newLineEnd = hitPoint + (float)distanceAlongPlane * lineForwardVector;
            bHitPointFound = true;
            bExtendPlaneBeyondStart = false;
            return newLineEnd;
        }
    }
}
