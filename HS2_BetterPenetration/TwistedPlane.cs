using System;
using UnityEngine;

namespace HS2_BetterPenetration
{

    // Creates a Plane defined by two non-parallel lines
    class TwistedPlane
    {
        public Vector3 firstOrigin;
        public Vector3 firstVector;
        public Vector3 firstUpVector;
        public Vector3 secondOrigin;
        public Vector3 secondVector;
        public Vector3 secondUpVector;
        public Vector3 forwardVector;
        public Vector3 twistVector;

        public TwistedPlane(Vector3 firstOrig, Vector3 firstVec, Vector3 firstUp, Vector3 secondOrig, Vector3 secondVec, Vector3 secondUp)
        {
            firstOrigin = firstOrig;
            firstVector = firstVec;
            firstUpVector = Vector3.Normalize(firstUp);
            secondOrigin = secondOrig;
            secondVector = secondVec;
            forwardVector = secondOrig - firstOrig;
            twistVector = secondVec - firstVec;
            secondUpVector = Vector3.Normalize(secondUp);
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

            for (Geometry.Axis axis = Geometry.Axis.X; axis <= Geometry.Axis.Z; axis++)
            {
                if (axis == Geometry.Axis.X)
                {
                    Cu = FVxLV.x + TWxLV.x * u;
                    if (Cu != 0)
                    {
                        v = (OFxLV.x + LVxFW.x * u) / Cu;
                        break;
                    }
                }
                else if (axis == Geometry.Axis.Y)
                {
                    Cu = FVxLV.y + TWxLV.y * u;
                    if (Cu != 0)
                    {
                        v = (OFxLV.y + LVxFW.y * u) / Cu;
                        break;
                    }
                }
                else
                {
                    Cu = FVxLV.z + TWxLV.z * u;
                    if (Cu != 0)
                    {
                        v = (OFxLV.z + LVxFW.z * u) / Cu;
                        break;
                    }
                }
            }

            if (Cu == 0)
                return false;

            if (lineVector.x != 0)
                t = (-offsetVector.x + firstVector.x * v + (forwardVector.x + twistVector.x * v) * u) / lineVector.x;
            else if (lineVector.y != 0)
                t = (-offsetVector.y + firstVector.y * v + (forwardVector.y + twistVector.y * v) * u) / lineVector.y;
            else if (lineVector.z != 0)
                t = (-offsetVector.z + firstVector.z * v + (forwardVector.z + twistVector.z * v) * u) / lineVector.z;
            else
                return false;

            return true;
        }

        // return true if they interstect
        public bool IntersectLineOnTwistedPlane(Vector3 lineStart, Vector3 lineEnd, bool bExtendPlaneBeyondFirstVector, bool bExtendPlaneBeyoneSecondVector, out Vector3 intersectionPoint, out Vector3 intersectForwardVector, out float distanceToSecondVector)
        {
            Console.WriteLine("IntersectLineOnTwistedPlane");

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

            Console.WriteLine("quadraticA " + quadraticA + "quadraticB " + quadraticB + "quadraticC " + quadraticC);

            if (Geometry.SolveQuadratic(quadraticA, quadraticB, quadraticC, out double u1, out double u2) == false)
                return false;

            double v1 = 0, v2 = 0;
            double t1 = 0, t2 = 0;
            Vector3 FVxLV = Vector3.Cross(firstVector, lineVector);
            Vector3 TWxLV = Vector3.Cross(twistVector, lineVector);
            Vector3 OFxLV = Vector3.Cross(offsetVector, lineVector);
            Vector3 LVxFW = Vector3.Cross(lineVector, forwardVector);

            Console.WriteLine("u1 " + u1 + " u2 " + u2);

            bool bIntersect1Found = false;
            if ((u1 < 4) && (u1 > -4) && (u1 >= 0 || bExtendPlaneBeyondFirstVector) && (u1 <= 1 || bExtendPlaneBeyoneSecondVector))
            {
                bIntersect1Found = this.FindIntersectValues(lineVector, offsetVector, FVxLV, TWxLV, OFxLV, LVxFW, u1, out v1, out t1);
                // not interested in any intersects that aren't on the line
                if (bIntersect1Found && t1 < 0 || t1 > 1)
                    bIntersect1Found = false;

            }

            bool bIntersect2Found = false;
            if ((u2 < 4) && (u2 > -4) && (u2 >= 0 || bExtendPlaneBeyondFirstVector) && (u2 <= 1 || bExtendPlaneBeyoneSecondVector))
            {
                bIntersect2Found  = this.FindIntersectValues(lineVector, offsetVector, FVxLV, TWxLV, OFxLV, LVxFW, u2, out v2, out t2);
                // not interested in any intersects that aren't on the line
                if (bIntersect2Found && t2 < 0 || t2 > 1)
                    bIntersect2Found = false;
            }

            if (!bIntersect1Found && !bIntersect2Found)
                return false;

            float t = 0, v = 0;
            if (bIntersect1Found)
            {
                t = (float)t1;
                v = (float)v1;
                Console.WriteLine("t1 " + t1 + " v1 " + v1);
            }
            if (bIntersect2Found && (!bIntersect1Found || (bIntersect1Found && t2 < t1)))
            {
                t = (float)t2;
                v = (float)v2;
                Console.WriteLine("t2 " + t1 + " v2 " + v1);
            }

            intersectionPoint = lineStart + lineVector * t;
            intersectForwardVector = Vector3.Normalize((secondOrigin + secondVector * v) - (firstOrigin + firstVector * v));
            distanceToSecondVector = Vector3.Distance(intersectionPoint, secondOrigin + secondVector * v);

            Console.WriteLine("intersectionPoint" + intersectionPoint.x + " , " + intersectionPoint.y + " , " + intersectionPoint.z);
            Console.WriteLine("intersectForwardVector" + intersectForwardVector.x + " , " + intersectForwardVector.y + " , " + intersectForwardVector.z);

            return true;
        }

        public Vector3 ConstrainLineToTwistedPlane(Vector3 lineStart, Vector3 lineEnd, ref float lineLength, float minLineLength, ref bool bExtendPlaneBeyondStart, bool bExtendPlaneBeyondEnd, out bool bHitPointFound)
        {
            Vector3 newLineEnd;
            Vector3 lineVector = Vector3.Normalize(lineEnd - lineStart);
            bHitPointFound = false;

            Console.WriteLine("ConstrainLineToTwistedPlane");
            Vector3 planeRightVector = Vector3.Normalize(firstVector + secondVector);
            Vector3 planeUpVector = Vector3.Normalize(Vector3.Cross(planeRightVector, forwardVector));
            Plane upPlane = new Plane(planeUpVector, firstOrigin + forwardVector/2);

            bool bAbovePlane = upPlane.GetSide(lineEnd);
            bool bVectorPointsTowardNormal = false;
            if (Vector3.Magnitude(upPlane.normal + lineVector) < Vector3.Magnitude(upPlane.normal - lineVector))
                bVectorPointsTowardNormal = true;

            Console.WriteLine("planeUpVector " + planeUpVector.x + " , " + planeUpVector.y + " , " + planeUpVector.z);

            if (bVectorPointsTowardNormal == bAbovePlane)
            {
                Console.WriteLine("Plane Detects don't agree!");
            }
/*
            if (bVectorPointsTowardNormal)
            {
                Console.WriteLine("Vector pointing towards Plane Normal");
                bExtendPlaneBeyondStart = false;
                return lineEnd;
            }
*/
            if (!bAbovePlane)
            {
                Console.WriteLine("Dan109 below plane");
                bExtendPlaneBeyondStart = false;
                return lineEnd;
             }

            bAbovePlane = upPlane.GetSide(lineStart);


            if (bAbovePlane)
            {

            }

            bool bIntersectFound = this.IntersectLineOnTwistedPlane(lineStart, lineEnd, bExtendPlaneBeyondStart, bExtendPlaneBeyondEnd, out Vector3 hitPoint, out Vector3 lineForwardVector, out float distanceToEdgeOfPlane);

            if (!bIntersectFound)
            {
                Console.WriteLine("No Intersect");
                bExtendPlaneBeyondStart = false;
                return lineEnd;
            }

            double hitDistance = Vector3.Distance(hitPoint, lineStart);
            if (hitDistance > lineLength)
            {
                Console.WriteLine("hitDistance > lineLength");
                bExtendPlaneBeyondStart = false;
                return lineEnd;
            }

            if (hitDistance > minLineLength)
            {
                Console.WriteLine("hitDistance > minLineLength");
                lineLength = (float)hitDistance;
                bHitPointFound = true;
                bExtendPlaneBeyondStart = false;
                return hitPoint;
            }

            lineLength = minLineLength;
            double angleLineToPlane = (double)Geometry.DegToRad(Vector3.Angle(lineVector, -lineForwardVector));
            double angleNewLineToPlane = (double)Math.Asin(hitDistance * Math.Sin(angleLineToPlane) / lineLength);
            double angleLineToNewLine = (double)Math.PI - angleLineToPlane - angleNewLineToPlane;
            float distanceAlongPlane = (float)(lineLength * Math.Sin(angleLineToNewLine) / Math.Sin(angleLineToPlane));
            Console.WriteLine("angleLineToPlane " + Geometry.RadToDeg(angleLineToPlane));
            Console.WriteLine("angleNewLineToPlane " + Geometry.RadToDeg(angleNewLineToPlane));
            Console.WriteLine("angleLineToNewLine " + Geometry.RadToDeg(angleLineToNewLine));
            Console.WriteLine("distanceAlongPlane " + distanceAlongPlane);

            if (!bExtendPlaneBeyondEnd)
            {
                if (distanceAlongPlane > distanceToEdgeOfPlane)
                {
                    newLineEnd = hitPoint + distanceToEdgeOfPlane * lineForwardVector;
                    newLineEnd = lineStart + Vector3.Normalize(newLineEnd - lineStart) * lineLength;
                    bExtendPlaneBeyondStart = true;
                    Console.WriteLine("distanceToEdgeOfPlane " + distanceToEdgeOfPlane);
                    Console.WriteLine("newLineEnd " + newLineEnd.x + " , " + newLineEnd.y + " , " + newLineEnd.z);
                    return newLineEnd;
                }
            }

            newLineEnd = hitPoint + distanceAlongPlane * lineForwardVector;
            bHitPointFound = true;
            bExtendPlaneBeyondStart = false;
            return newLineEnd;
        }
    }
}
