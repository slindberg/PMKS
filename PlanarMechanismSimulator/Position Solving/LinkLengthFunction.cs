﻿using System;
using OptimizationToolbox;

namespace PlanarMechanismSimulator
{
    internal class LinkLengthFunction : IObjectiveFunction, IDifferentiable, ITwiceDifferentiable
    {
        private readonly int varListIndex1;
        private readonly int varListIndex2;
        private readonly double origLength;

        private double x1;
        private double y1;
        private double x2;
        private double y2;
        private int jointListIndex2;

        private double deltaX
        {
            get { return x1 - x2; }
        }

        private double deltaY
        {
            get { return y1 - y2; }
        }

        private double newLengthSqared
        {
            get { return deltaX * deltaX + deltaY * deltaY; }
        }

        private double newLength
        {
            get { return Math.Sqrt(newLengthSqared); }
        }

        public LinkLengthFunction(int varListIndex1, int jointListIndex1, double X1, double Y1, int varListIndex2, int jointListIndex2, double X2, double Y2)
        {
            this.jointListIndex1 = jointListIndex1;
            this.jointListIndex2 = jointListIndex2;

            x1 = X1;
            y1 = Y1;
            x2 = X2;
            y2 = Y2;
            this.varListIndex1 = varListIndex1;
            this.varListIndex2 = varListIndex2;
            origLength = Math.Sqrt(deltaX * deltaX + deltaY * deltaY);
        }

        public double calculate(double[] x)
        {
            assignPositions(x);
            return newLengthSqared - 2 * origLength * newLength + origLength * origLength;
        }

        public double deriv_wrt_xi(double[] x, int i)
        {
            if (!(i == 2 * varListIndex1 || i == 2 * varListIndex1 + 1 || i == 2 * varListIndex2 || i == 2 * varListIndex2 + 1)) return 0;
            assignPositions(x);
            if (i == 2 * varListIndex1)
                // w.r.t. x1
                return -2 * deltaX * (origLength / newLength - 1);
            if (i == 2 * varListIndex1 + 1)
                // w.r.t. y1
                return -2 * deltaY * (origLength / newLength - 1);
            if (i == 2 * varListIndex2)
                // w.r.t. x2
                return 2 * deltaX * (origLength / newLength - 1);
            if (i == 2 * varListIndex2 + 1)
                // w.r.t. y2
                return 2 * deltaY * (origLength / newLength - 1);
            throw new Exception("Gradient:you shouldn't be seeing this! how did you get by the initial if-statement?");
        }

        public double second_deriv_wrt_ij(double[] x, int i, int j)
        {
            if ((!(i == 2 * varListIndex1 || i == 2 * varListIndex1 + 1 || i == 2 * varListIndex2 || i == 2 * varListIndex2 + 1))
                || (!(j == 2 * varListIndex1 || j == 2 * varListIndex1 + 1 || j == 2 * varListIndex2 || j == 2 * varListIndex2 + 1))) return 0;
            assignPositions(x);
            var firstTerm = 2 * (1 - origLength / newLength);
            var secondterm = 2 * origLength / Math.Pow(newLength, 3);
            if (i == j)
            {
                if ((i == 2 * varListIndex1) || (i == 2 * varListIndex2)) return firstTerm + deltaX * deltaX * secondterm;
                if ((i == 2 * varListIndex1 + 1) || (i == 2 * varListIndex2 + 1)) return firstTerm + deltaY * deltaY * secondterm;
            }
            if (i > j)
            {
                //switch so that i is always less than j. Hessians are always symmetric.
                var temp = i;
                i = j;
                j = temp;
            }
            if (i == 2 * varListIndex1)
            {
                if (j == 2 * varListIndex1 + 1) return deltaX * deltaY * secondterm;
                if (j == 2 * varListIndex2) return -firstTerm - deltaX * deltaX * secondterm;
                if (j == 2 * varListIndex2 + 1) return -deltaX * deltaY * secondterm;
            }
            if (i == 2 * varListIndex1 + 1)
            {
                if (j == 2 * varListIndex2) return -deltaY * deltaX * secondterm;
                if (j == 2 * varListIndex2 + 1) return -firstTerm - deltaY * deltaY * secondterm;
            }
            if (i == 2 * varListIndex2)
                if (j == 2 * varListIndex2 + 1) return deltaX * deltaY * secondterm;
            throw new Exception("Hessian:you shouldn't be seeing this! how did you get by the initial if-statement?");
        }

        private void assignPositions(double[] x)
        {
            if (varListIndex1 > 0 && x.GetLength(0) > 2 * varListIndex1 + 1)
            {
                x1 = x[2 * varListIndex1];
                y1 = x[2 * varListIndex1 + 1];
            }
            if (varListIndex2 > 0 && x.GetLength(0) > 2 * varListIndex2 + 1)
            {
                x2 = x[2 * varListIndex2];
                y2 = x[2 * varListIndex2 + 1];
            }
        }

        internal void SetJointPosition(int index, double x, double y)
        {
            if (index == jointListIndex1)
            {
                x1 = x;
                y1 = y;
            }
            if (index == jointListIndex2)
            {
                x2 = x;
                y2 = y;
            }
        }

        public int jointListIndex1 { get; set; }
    }
}