using System;
using UnityEngine;

namespace HS2_BetterPenetration
{
    public static class Geometry
    {
        // Casts a point onto an infinite line defined by two points on the line
        public static Vector3 castToSegment(Vector3 position, Vector3 linePointStart, Vector3 linePointEnd, out float normDistAlongSegment)
        {
            Vector3 line = linePointEnd - linePointStart;
            normDistAlongSegment = Vector3.Dot(position - linePointStart, line) / Vector3.Dot(line, line);
            return Vector3.LerpUnclamped(linePointStart, linePointEnd, normDistAlongSegment);
        }

        public static double DegToRad(double degrees)
        {
            return (degrees * Math.PI / 180);
        }

        public static double RadToDeg(double radians)
        {
            return (radians * 180 / Math.PI);
        }

        public static Vector3 ConstrainLineToHitPlane(Vector3 lineStart, Vector3 lineEnd, float lineLength, float minLineLength, Vector3 hitPlaneStart, Vector3 hitPlaneEnd, Plane hitPlane, bool bExtendPlaneBeyondEnd, ref bool bExtendPlaneBeyondStart, out float angleLineToNewLine, out bool bHitPointFound)
        {
            Vector3 newLineEnd = lineEnd;
            bool bExtendPlaneBeyondStartReturn = false;
            bHitPointFound = false;
            angleLineToNewLine = 0;

            if (hitPlane.GetSide(lineEnd) || bExtendPlaneBeyondStart)
            {
                Vector3 lineVector = Vector3.Normalize(lineEnd - lineStart);
                Ray lineRay = new Ray(lineStart, Vector3.Normalize(lineVector));
                bool lineHitPlane = hitPlane.Raycast(lineRay, out float hitDistance);

                if (lineHitPlane && (hitDistance <= lineLength))
                {
                    Vector3 hitPoint = lineRay.GetPoint(hitDistance);
                    castToSegment(hitPoint, hitPlaneEnd, hitPlaneStart, out float normDistAlongSegment);
                    if (normDistAlongSegment >= 0 || bExtendPlaneBeyondEnd)
                    {
                        if (hitDistance >= minLineLength)
                        {
                            newLineEnd = hitPoint;
                            bHitPointFound = true;
                        }
                        else
                        {
                            lineLength = minLineLength;
                            if (normDistAlongSegment < 1 || bExtendPlaneBeyondStart)
                            {
                                Vector3 hitVector = Vector3.Normalize(hitPlaneEnd - hitPlaneStart);
                                Vector3 planeVector = Vector3.Normalize(Vector3.Cross(lineVector, hitPlane.normal));
                                Vector3 linePlaneForwardVector = hitVector;
                                float angleLineToPlane = (float)DegToRad(Vector3.Angle(lineVector, -linePlaneForwardVector));
                                float angleNewLineToPlane = (float)Math.Asin(hitDistance * Math.Sin(angleLineToPlane) / lineLength);
                                angleLineToNewLine = (float)Math.PI - angleLineToPlane - angleNewLineToPlane;
                                float distanceAlongPlane = (float)(lineLength * Math.Sin(angleLineToNewLine) / Math.Sin(angleLineToPlane));
                                newLineEnd = hitPoint + distanceAlongPlane * linePlaneForwardVector;
                                angleLineToNewLine = (float)RadToDeg(angleLineToNewLine);
                                bHitPointFound = true;

                                if (!bExtendPlaneBeyondEnd)
                                {
                                    float angleNewLineEndToPlaneLine = Vector3.Angle(newLineEnd - hitPlaneEnd, -hitVector);
                                    if (angleNewLineEndToPlaneLine > 90)
                                    {
                                        Vector3 newHitPoint;
                                        angleNewLineEndToPlaneLine -= 90;

                                        float angleHitPlaneEndToHitPlaneVector = Vector3.Angle(hitPlaneEnd - hitPoint, linePlaneForwardVector);
                                        float distanceNewLineEndToHitPlaneEnd = Vector3.Distance(newLineEnd, hitPlaneEnd);
                                        float distanceNewLineEndToMax = (float)(distanceNewLineEndToHitPlaneEnd * Math.Sin(DegToRad(angleNewLineEndToPlaneLine)) / Math.Sin(DegToRad(angleHitPlaneEndToHitPlaneVector)));
                                        newHitPoint = hitPoint + (distanceAlongPlane - distanceNewLineEndToHitPlaneEnd) * linePlaneForwardVector;
                                        newLineEnd = new Ray(lineStart, Vector3.Normalize(newHitPoint - lineStart)).GetPoint(lineLength);
                                        angleLineToNewLine = Vector3.Angle(newLineEnd - lineStart, lineVector);
                                        bExtendPlaneBeyondStartReturn = true;
                                        bHitPointFound = false;
                                    }
                                }
                            }
                        }
                    }
                }
            }
            bExtendPlaneBeyondStart = bExtendPlaneBeyondStartReturn;
            return newLineEnd;
        }
    }
}
