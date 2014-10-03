﻿using System;
using System.Collections.Generic;
using System.Linq;
using PMKS.VelocityAndAcceleration;
using PMKS;
using StarMathLib;

namespace PMKS
{
    public static class Constants
    {

        /// <summary>
        ///   This is used below in the close enough to zero booleans to match points
        ///   (see below: sameCloseZero). In order to avoid strange round-off issues - 
        ///   even with doubles - I have implemented this function when comparing the
        ///   position of points (mostly in checking for a valid transformation (see
        ///   ValidTransformation) and if other nodes comply (see otherNodesComply).
        /// </summary>
        public const double epsilonSame = 10e-12;

        public const double epsilon = 10e-9;
        internal const double ErrorInDeterminingCompleteCycle = 0.01;
        internal const double rangeMultiplier = 5.0;
        internal const int numberOfTries = 50;
        public const double SmallPerturbationFraction = 0.003;
        public const double DefaultStepSize = 0.5;
        public const double MinimumStepSize = 0.001;
        public const int MaxItersInPositionError = 10;
        public const double ConservativeErrorEstimation = 0.9;
        public const double ErrorEstimateInertia = 2.0;
        public const double ErrorSizeIncrease = 1.2;
        public const long MaxItersInNonDyadicSolver = 300;
        public const double DefaultInputSpeed = 1.0;

        public static TimeSpan MaxTimeToFindMatrixOrders = new TimeSpan((long)2000000);
        public const double XRangeLimitFactor = 5.0;
        public const double YRangeLimitFactor = 5.0;
        public const double XMinimumFactor = 1e-8;
        public const double YMinimumFactor = 1e-8;
        public const double JointAccelerationLimitFactor = 75.0;
        public const double LinkAccelerationLimitFactor = 75.0;
        public const double JointVelocityLimitFactor = 75.0;
        public const double LinkVelocityLimitFactor = 75.0;
        public const double FullCircle = 2 * Math.PI;
        public const double MaxSlope = 10e9;

        public static Boolean sameCloseZero(double x1)
        {
            return Math.Abs(x1) < epsilonSame;
        }

        public static Boolean sameCloseZero(double x1, double x2)
        {
            return sameCloseZero(x1 - x2);
        }


        #region DistanceSquared

        public static double distanceSqared(double x1, double y1, double x2 = 0, double y2 = 0)
        {
            return (x1 - x2) * (x1 - x2) + (y1 - y2) * (y1 - y2);
        }

        internal static double distanceSqared(point point1, point point2)
        {
            return distanceSqared(point1.x, point1.y, point2.x, point2.y);
        }

        #endregion

        #region Distance

        internal static double distance(double x1, double y1, double x2 = 0, double y2 = 0)
        {
            return Math.Sqrt(distanceSqared(x1, y1, x2, y2));
        }

        internal static double distance(point point1, point point2)
        {
            return distance(point1.x, point1.y, point2.x, point2.y);
        }

        #endregion

        #region Angle

        public static double angle(point start, point end)
        {
            return angle(start.x, start.y, end.x, end.y);
        }


        public static double angle(double startX, double startY, double endX, double endY)
        {
            return Math.Atan2(endY - startY, endX - startX);
        }

        #endregion

        public static point solveViaIntersectingLines(double slopeA, point ptA, double slopeB, point ptB)
        {
            if (sameCloseZero(ptA.x, ptB.x) && sameCloseZero(ptA.y, ptB.y)) return ptA;
            if (sameCloseZero(slopeA, slopeB)) return new point(Double.NaN, Double.NaN);
            var offsetA = ptA.y - slopeA * ptA.x;
            var offsetB = ptB.y - slopeB * ptB.x;
            if (verticalSlope(slopeA))
                return new point(ptA.x, slopeB * ptA.x + offsetB);
            if (verticalSlope(slopeB))
                return new point(ptB.x, slopeA * ptB.x + offsetA);

            var x = (offsetB - offsetA) / (slopeA - slopeB);
            var y = slopeA * x + offsetA;
            return new point(x, y);
        }

        private static Boolean verticalSlope(double slope)
        {
            return (Double.IsNaN(slope) || Double.IsInfinity(slope)
                    || Math.Abs(slope) > Constants.MaxSlope);
        }
    }
}
