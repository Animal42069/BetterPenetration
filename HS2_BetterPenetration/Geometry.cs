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
            if (quadA > -0.0001 && quadA < 0.0001)
            {
                if (quadB == 0)
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
    }
}
