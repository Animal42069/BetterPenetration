using System;
using UnityEngine;

namespace Core_BetterPenetration
{
    internal static class MathHelpers
    {
        public enum Axis
        {
            X = 0,
            Y = 1,
            Z = 2
        }

        // Casts a point onto an infinite line defined by two points on the line
        private static Vector3 CastToSegment(Vector3 position, Vector3 lineStart, Vector3 lineVector)
        {
            Vector3 lineEnd = lineStart + lineVector;
            float normDistAlongSegment = Vector3.Dot(position - lineStart, lineVector) / Vector3.Magnitude(lineVector);
            return Vector3.LerpUnclamped(lineStart, lineEnd, normDistAlongSegment);
        }

        // Finds the point on segment C to D that is closest to segment A to B
        // SEGMENT C D must be normalized
        internal static Vector3 CastSegmentToSegment(Vector3 projFromStart, Vector3 projFromVector, Vector3 projToStart, Vector3 projToVector)
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

        internal static double DegToRad(double degrees)
        {
            return (degrees * Math.PI / 180);
        }

        internal static double RadToDeg(double radians)
        {
            return (radians * 180 / Math.PI);
        }

        // returns the two solutions of a quadratic equation
        internal static bool SolveQuadratic(double quadA, double quadB, double quadC, out double solution1, out double solution2)
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

        internal static bool ApproximatelyZero(double value)
        {
            return (value > -0.0001 && value < 0.0001);
        }

        internal static void SolveSSATriangle(double sideA, double sideB, double angleA, out double sideC, out double angleB, out double angleC)
        {
            sideC = 0;
            angleB = 0;
            angleC = 0;

            if (ApproximatelyZero(sideA) || ApproximatelyZero(sideB) || ApproximatelyZero(angleA))
                return;

            angleB = Math.Asin(Math.Sin(angleA) * sideB / sideA);
            angleC = Math.PI - angleA - angleB;
            sideC = sideA * Math.Sin(angleC) / Math.Sin(angleA);
       }

        // Return True if Vectors are close enough to each other
        internal static bool VectorsEqual(Vector3 firstVector, Vector3 secondVector, float threshold = 0.01f)
        {
            if (Math.Abs(firstVector.x - secondVector.x) > threshold)
                return false;

            if (Math.Abs(firstVector.y - secondVector.y) > threshold)
                return false;

            if (Math.Abs(firstVector.z - secondVector.z) > threshold)
                return false;

            return true;
        }
    }
}
