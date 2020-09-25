using System;
using UnityEngine;

namespace HS2_BetterPenetration
{
    public static class Geometry
    {
        public enum Axis
        {
            X = 0,
            Y = 1,
            Z = 2
        }

        // Casts a point onto an infinite line defined by two points on the line
        public static Vector3 CastToSegment(Vector3 position, Vector3 lineStart, Vector3 lineVector)
        {
            Vector3 lineEnd = lineStart + lineVector;
            float normDistAlongSegment = Vector3.Dot(position - lineStart, lineVector) / Vector3.Magnitude(lineVector);
            return Vector3.LerpUnclamped(lineStart, lineEnd, normDistAlongSegment);
        }

        // Finds the point on segment C to D that is closest to segment A to B
        // SEGMENT C D must be normalized
        public static Vector3 CastSegmentToSegment(Vector3 projFromStart, Vector3 projFromVector, Vector3 projToStart, Vector3 projToVector)
        {
            Vector3 projFromEnd = projFromStart + projFromVector;
            Vector3 inPlaneStart = projFromStart - Vector3.Dot(projFromStart - projToStart, projToVector) * projToVector;
            Vector3 inPlaneEnd = projFromEnd - Vector3.Dot(projFromEnd - projToStart, projToVector) * projToVector;
            Vector3 inPlaneVector = inPlaneEnd - inPlaneStart;
            Vector3 inToPlaneStart = projToStart - inPlaneStart;

            float dotinToPlaneStartinPlaneVector = Vector3.Dot(inToPlaneStart, inPlaneVector);
            float inPlaneVectorMag = Vector3.Magnitude(inPlaneVector);
            float normDistAlongLine = dotinToPlaneStartinPlaneVector / inPlaneVectorMag;

            return CastToSegment(projFromStart + projFromVector * normDistAlongLine, projToStart, projToVector);
        }

        public static double DegToRad(double degrees)
        {
            return (degrees * Math.PI / 180);
        }

        public static double RadToDeg(double radians)
        {
            return (radians * 180 / Math.PI);
        }

        // returns the two solutions of a quadratic equation
        public static bool SolveQuadratic(double quadA, double quadB, double quadC, out double solution1, out double solution2)
        {
            solution1 = solution2 = 0;
            if (ApproximatelyZero(quadA))
            {
                if (ApproximatelyZero(quadB))
                    return false;

                solution1 = solution2 = -quadC / quadB;
                return true;
            }

            double quadD = (quadB * quadB) - (4 * quadA * quadC);
            if (quadD >= 0)
            {
                solution1 = (-quadB + Math.Sqrt(quadD)) / (2 * quadA);
                solution2 = (-quadB - Math.Sqrt(quadD)) / (2 * quadA);
                return true;
            }

            return false;
        }

        public static bool ApproximatelyZero(double value)
        {
            return (value > -0.0001 && value < 0.0001);
        }

    }
}
