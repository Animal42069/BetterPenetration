using System;
using UnityEngine;

namespace AI_BetterPenetration
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

        public static Vector3 ConstrainLineToHitPlane(Vector3 lineStart, Vector3 lineEnd, float lineLength, Vector3 hitPlaneStart, Vector3 hitPlaneEnd, Plane hitPlane, ref bool bExtendPlaneBeyondStart, out float angleLineToNewLine)
        {
            Vector3 newLineEnd = lineEnd;
            angleLineToNewLine = 0;
            bool bExtendPlaneBeyondStartReturn = false;

            if (hitPlane.GetSide(lineEnd) || bExtendPlaneBeyondStart)
            {
                Ray lineRay = new Ray(lineStart, Vector3.Normalize(lineEnd - lineStart));
                bool lineHitPlane = hitPlane.Raycast(lineRay, out float hitDistance);

                if (lineHitPlane && (hitDistance < lineLength || bExtendPlaneBeyondStart))
                {
                    Vector3 hitPoint = lineRay.GetPoint(hitDistance);
                    castToSegment(hitPoint, hitPlaneEnd, hitPlaneStart, out float normDistAlongSegment);
                    if (normDistAlongSegment > 0 && (normDistAlongSegment < 1 || bExtendPlaneBeyondStart))
                    {

                        Vector3 lineVector = Vector3.Normalize(lineEnd - lineStart);
                        Vector3 hitVector = Vector3.Normalize(hitPlaneEnd - hitPlaneStart);
                        Vector3 planeVector = Vector3.Normalize(Vector3.Cross(lineVector, hitPlane.normal));
                        Vector3 linePlaneForwardVector = Vector3.Normalize(Vector3.Cross(hitPlane.normal, planeVector));

                        if (Vector3.Magnitude(linePlaneForwardVector + hitVector) < Vector3.Magnitude(linePlaneForwardVector - hitVector))
                            linePlaneForwardVector = -linePlaneForwardVector;


                        float angleLineToPlane = Vector3.Angle(lineVector, -linePlaneForwardVector);

                        angleLineToPlane = (float)DegToRad(angleLineToPlane);
                        float angleNewLineToPlane = (float)Math.Asin(hitDistance * Math.Sin(angleLineToPlane) / lineLength);
                        angleLineToNewLine = (float)Math.PI - angleLineToPlane - angleNewLineToPlane;
                        float distanceAlongPlane = (float)(lineLength * Math.Sin(angleLineToNewLine) / Math.Sin(angleLineToPlane));
                        newLineEnd = hitPoint + distanceAlongPlane * linePlaneForwardVector;
                        angleLineToNewLine = Vector3.Angle(newLineEnd - lineStart, lineVector);

                        Vector3 maxLineVector = Geometry.castToSegment(hitPlaneEnd, newLineEnd, lineEnd, out normDistAlongSegment);
                        if (normDistAlongSegment > 0 && normDistAlongSegment < 1)
                        {
                            newLineEnd = new Ray(lineStart, Vector3.Normalize(maxLineVector - lineStart)).GetPoint(lineLength);
                            angleLineToNewLine = Vector3.Angle(newLineEnd - lineStart, lineVector);
                            bExtendPlaneBeyondStartReturn = true;
                        }
                    }
                }
            }
            bExtendPlaneBeyondStart = bExtendPlaneBeyondStartReturn;
            return newLineEnd;
        }
    }
}
