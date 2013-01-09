﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using OptimizationToolbox;
using PlanarMechanismSimulator.PositionSolving;
using PlanarMechanismSimulator.VelocityAndAcceleration;

namespace PlanarMechanismSimulator
{
    public partial class Simulator : IDependentAnalysis
    {
        private static AutoResetEvent forwardDone = new AutoResetEvent(false);
        private static AutoResetEvent backwardDone = new AutoResetEvent(false);

        public void FindFullMovement()
        {
            if (double.IsNaN(DeltaAngle) && double.IsNaN(FixedTimeStep) && double.IsNaN(MaxSmoothingError))
                throw new Exception(
                    "Either the smoothing error angle delta or the time step must be specified.");
            bool useErrorMethod = !double.IsNaN(MaxSmoothingError);

            #region Set up initial point parameters (x, x-dot, x-double-dot, etc.)

            double[,] initJointParams, initLinkParams;
            SetInitialVelocityAndAcceleration(ijoints, ilinks, out initJointParams, out initLinkParams);

            JointParameters = new TimeSortedList { { 0.0, initJointParams } };
            LinkParameters = new TimeSortedList { { 0.0, initLinkParams } };

            InputRange = new[] { inputLink.AngleInitial, inputLink.AngleInitial };

            #endregion

            if (useErrorMethod)
            {
#if DEBUGSERIAL
                SimulateWithinError(ijoints, ilinks, true);
#elif SILVERLIGHT
                List<joint> newJoints = ijoints.Select(j => j.copy()).ToList();
                List<link> newLinks = ilinks.Select(c => c.copy(ijoints, newJoints)).ToList();
                foreach (joint j in newJoints)
                {
                    j.Link1 = newLinks[ilinks.IndexOf(j.Link1)];
                    if (j.Link2 != null)
                        j.Link2 = newLinks[ilinks.IndexOf(j.Link2)];
                }
                var forwardThread = new Thread(()=>
                    {
                        SimulateWithinError(ijoints, ilinks, true);
                        forwardDone.Set();
                    });
                var backwardThread = new Thread(()=>
                    {
                        SimulateWithinError(newJoints, newLinks, false);
                        backwardDone.Set();
                    });
                forwardThread.Start();
                backwardThread.Start();
                if (forwardDone.WaitOne() && backwardDone.WaitOne())
                {
                    forwardDone = new AutoResetEvent(false);
                    backwardDone = new AutoResetEvent(false);
                }
#else
                var newJoints = ijoints.Select(j => j.copy()).ToList();
                var newLinks = ilinks.Select(c => c.copy(ijoints, newJoints)).ToList();
                foreach (var j in newJoints)
                {
                    j.Link1 = newLinks[ilinks.IndexOf(j.Link1)];
                    if (j.Link2 != null)
                        j.Link2 = newLinks[ilinks.IndexOf(j.Link2)];
                }
                Parallel.Invoke(
                    /*** Stepping Forward in Time ***/
                    () => SimulateWithinError(ijoints, ilinks, true),
                    /*** Stepping Backward in Time ***/
                    () => SimulateWithinError(newJoints, newLinks, false));
#endif
            }
            else
            {
#if DEBUGSERIAL
                SimulateWithFixedDelta(ijoints, ilinks, true);
#elif SILVERLIGHT
                List<joint> newJoints = ijoints.Select(j => j.copy()).ToList();
                List<link> newLinks = ilinks.Select(c => c.copy(ijoints, newJoints)).ToList();
                foreach (joint j in newJoints)
                {
                    j.Link1 = newLinks[ilinks.IndexOf(j.Link1)];
                    if (j.Link2 != null)
                        j.Link2 = newLinks[ilinks.IndexOf(j.Link2)];
                }
                var forwardThread = new Thread(()=>
                    {
                        SimulateWithFixedDelta(ijoints, ilinks, true);
                        forwardDone.Set();
                    });
                var backwardThread = new Thread(()=>
                    {
                        SimulateWithFixedDelta(newJoints, newLinks, false);
                        backwardDone.Set();
                    });
                forwardThread.Start();
                backwardThread.Start();
                if (forwardDone.WaitOne() && backwardDone.WaitOne())
                {
                    forwardDone = new AutoResetEvent(false);
                    backwardDone = new AutoResetEvent(false);
                }
#else
                var newJoints = ijoints.Select(j => j.copy()).ToList();
                var newLinks = ilinks.Select(c => c.copy(ijoints, newJoints)).ToList();
                foreach (var j in newJoints)
                {
                    j.Link1 = newLinks[ilinks.IndexOf(j.Link1)];
                    if (j.Link2 != null)
                        j.Link2 = newLinks[ilinks.IndexOf(j.Link2)];
                }
                Parallel.Invoke(
                    /*** Stepping Forward in Time ***/
                    () => SimulateWithFixedDelta(ijoints, ilinks, true),
                    /*** Stepping Backward in Time ***/
                    () => SimulateWithFixedDelta(newJoints, newLinks, false));
#endif
            }
        }

        private void SetInitialVelocityAndAcceleration(List<joint> ijoints, List<link> ilinks, out double[,] initJointParams, out double[,] initLinkParams)
        {
            initJointParams = WriteJointStatesVariablesToMatrixAndToLast(ijoints);
            initLinkParams = WriteLinkStatesVariablesToMatrixAndToLast(ilinks);

            var posFinder = new PositionFinder(ijoints, ilinks, gearsData, inputJointIndex);
            var velSolver = new VelocitySolver(ijoints, ilinks, firstInputJointIndex, inputJointIndex, inputLinkIndex, InputSpeed);
            var accelSolver = new AccelerationSolver(ijoints, ilinks, firstInputJointIndex, inputJointIndex, inputLinkIndex, InputSpeed);

            double smallTimeStep = Constants.SmallPerturbationFraction * FixedTimeStep;
            if (velSolver.Solve())
            {
                for (int i = 0; i <= inputJointIndex; i++)
                {
                    initJointParams[i, 2] = ijoints[i].vx;
                    initJointParams[i, 3] = ijoints[i].vy;
                }
                for (int i = 0; i <= inputLinkIndex; i++)
                    initLinkParams[i, 1] = ilinks[i].Velocity;
                if (accelSolver.Solve())
                {
                    for (int i = 0; i <= inputJointIndex; i++)
                    {
                        initJointParams[i, 4] = ijoints[i].ax;
                        initJointParams[i, 5] = ijoints[i].ay;
                    }
                    for (int i = 0; i <= inputLinkIndex; i++)
                        initLinkParams[i, 2] = ilinks[i].Acceleration;
                }
                else
                {
                    /* velocity was successfully found, but not acceleration. */
                    if (posFinder.DefineNewPositions(smallTimeStep * InputSpeed) &&
                        velSolver.Solve())
                    {
                        /* forward difference on velocities to create accelerations. */
                        for (int i = 0; i <= inputJointIndex; i++)
                        {
                            initJointParams[i, 4] = (ijoints[i].vx - ijoints[i].vxLast) / smallTimeStep;
                            initJointParams[i, 5] = (ijoints[i].vy - ijoints[i].vyLast) / smallTimeStep;
                        }
                        for (int i = 0; i <= inputLinkIndex; i++)
                            initLinkParams[i, 2] = (ilinks[i].Velocity - ilinks[i].VelocityLast) / smallTimeStep;

                        /* since the position solving wrote values to joints[i].x and .y, we need to reset them, for further work. */
                        for (int i = 0; i <= inputJointIndex; i++)
                        {
                            ijoints[i].x = initJointParams[i, 0];
                            ijoints[i].y = initJointParams[i, 1];
                            ijoints[i].vx = initJointParams[i, 2];
                            ijoints[i].vy = initJointParams[i, 3];
                            ijoints[i].ax = initJointParams[i, 4];
                            ijoints[i].ay = initJointParams[i, 5];
                        }
                        for (int i = 0; i <= inputLinkIndex; i++)
                        {
                            ilinks[i].Angle = initLinkParams[i, 0];
                            ilinks[i].Velocity = initLinkParams[i, 1];
                            ilinks[i].Acceleration = initLinkParams[i, 2];
                        }
                    }
                }
                return;
            }
            var ForwardJointParams = new double[numJoints, 2];
            var ForwardLinkParams = new double[numLinks];
            /*** Stepping Forward in Time ***/
            bool forwardSuccess = posFinder.DefineNewPositions(smallTimeStep * InputSpeed);
            if (forwardSuccess)
            {
                NumericalVelocity(smallTimeStep, ijoints, ilinks);
                for (int i = 0; i < numJoints; i++)
                {
                    ForwardJointParams[i, 0] = ijoints[i].x;
                    ForwardJointParams[i, 0] = ijoints[i].y;
                }
                for (int i = 0; i < numLinks; i++)
                    ForwardLinkParams[i] = ilinks[i].Angle;
            }
            /*** Stepping Backward in Time ***/
            var BackwardJointParams = new double[numJoints, 2];
            var BackwardLinkParams = new double[numLinks];
            bool backwardSuccess = posFinder.DefineNewPositions(-smallTimeStep * InputSpeed);
            if (backwardSuccess)
            {
                NumericalVelocity(-smallTimeStep, ijoints, ilinks);
                for (int i = 0; i < numJoints; i++)
                {
                    BackwardJointParams[i, 0] = ijoints[i].x;
                    BackwardJointParams[i, 0] = ijoints[i].y;
                }
                for (int i = 0; i < numLinks; i++)
                    BackwardLinkParams[i] = ilinks[i].Angle;
            }
            if (forwardSuccess && backwardSuccess)
            {
                /* central difference puts values in init parameters. */
                for (int i = 0; i <= inputJointIndex; i++)
                {
                    /* first-order central finite difference */
                    initJointParams[i, 2] = (ForwardJointParams[i, 0] - BackwardJointParams[i, 0]) / (2 * smallTimeStep);
                    initJointParams[i, 3] = (ForwardJointParams[i, 1] - BackwardJointParams[i, 1]) / (2 * smallTimeStep);
                    /* second-order central finite difference */
                    initJointParams[i, 4] = (ForwardJointParams[i, 0] - 2 * initJointParams[i, 0] +
                                             BackwardJointParams[i, 0]) / (smallTimeStep * smallTimeStep);
                    initJointParams[i, 5] = (ForwardJointParams[i, 1] - 2 * initJointParams[i, 1] +
                                             BackwardJointParams[i, 1]) / (smallTimeStep * smallTimeStep);
                }
                for (int i = 0; i <= inputLinkIndex; i++)
                {
                    /* first-order central finite difference */
                    initLinkParams[i, 1] = (ForwardLinkParams[i] - BackwardLinkParams[i]) / (2 * smallTimeStep);
                    /* second-order central finite difference */
                    initLinkParams[i, 2] = (ForwardLinkParams[i] - 2 * initLinkParams[i, 0] + BackwardLinkParams[i])
                                           / (smallTimeStep * smallTimeStep);
                }
            }
            else if (forwardSuccess)
            {
                /* forward difference puts values in init parameters. */
                for (int i = 0; i <= inputJointIndex; i++)
                {
                    /* first-order forward finite difference */
                    initJointParams[i, 2] = (ForwardJointParams[i, 0] - initJointParams[i, 0]) / smallTimeStep;
                    initJointParams[i, 3] = (ForwardJointParams[i, 1] - initJointParams[i, 1]) / smallTimeStep;
                }
                for (int i = 0; i <= inputLinkIndex; i++)
                    /* first-order forward finite difference */
                    initLinkParams[i, 1] = (ForwardLinkParams[i] - initLinkParams[i, 0]) / smallTimeStep;
            }
            else if (backwardSuccess)
            {
                /* backward difference puts values in init parameters. */
                for (int i = 0; i <= inputJointIndex; i++)
                {
                    /* first-order backward finite difference */
                    initJointParams[i, 2] = (initJointParams[i, 0] - BackwardJointParams[i, 0]) / smallTimeStep;
                    initJointParams[i, 3] = (initJointParams[i, 1] - BackwardJointParams[i, 1]) / smallTimeStep;
                }
                for (int i = 0; i <= inputLinkIndex; i++)
                    /* first-order backward finite difference */
                    initLinkParams[i, 1] = (initLinkParams[i, 0] - BackwardLinkParams[i]) / smallTimeStep;
            }
            /* since the position solving wrote values to joints[i].x and .y, we need to reset them, for further work. */
            for (int i = 0; i <= inputJointIndex; i++)
            {
                ijoints[i].x = initJointParams[i, 0];
                ijoints[i].y = initJointParams[i, 1];
                ijoints[i].vx = initJointParams[i, 2];
                ijoints[i].vy = initJointParams[i, 3];
                ijoints[i].ax = initJointParams[i, 4];
                ijoints[i].ay = initJointParams[i, 5];
            }
            for (int i = 0; i <= inputLinkIndex; i++)
            {
                ilinks[i].Angle = initLinkParams[i, 0];
                ilinks[i].Velocity = initLinkParams[i, 1];
                ilinks[i].Acceleration = initLinkParams[i, 2];
            }
        }


        private void SimulateWithFixedDelta(List<joint> ijoints, List<link> ilinks, Boolean Forward)
        {
            double timeStep = Forward ? FixedTimeStep : -FixedTimeStep;
            double currentTime = 0.0;
            Boolean validPosition;
            var posFinder = new PositionFinder(ijoints, ilinks, gearsData, inputJointIndex);
            var velSolver = new VelocitySolver(ijoints, ilinks, firstInputJointIndex, inputJointIndex, inputLinkIndex, InputSpeed);
            var accelSolver = new AccelerationSolver(ijoints, ilinks, firstInputJointIndex, inputJointIndex, inputLinkIndex, InputSpeed);
            do
            {
                #region Find Next Positions

                // this next function puts the xNumerical and yNumerical values in the joints
                NumericalPosition(timeStep, ijoints, ilinks);
                double delta = InputSpeed * timeStep;
                // this next function puts the x and y values in the joints
                validPosition = posFinder.DefineNewPositions(delta);

                #endregion

                if (validPosition)
                {
                    if (Forward)
                        lock (InputRange)
                        {
                            InputRange[1] = ilinks[inputLinkIndex].Angle;
                        }
                    else
                        lock (InputRange)
                        {
                            InputRange[0] = ilinks[inputLinkIndex].Angle;
                        }

                    #region Find Velocities for Current Position

                    // this next functions puts the vx and vy values as well as the vx_unit and vy_unit in the joints
                    if (!velSolver.Solve())
                    {
                        Status += "Instant Centers could not be found at" + currentTime + ".";
                        NumericalVelocity(timeStep, ijoints, ilinks);
                    }

                    #endregion

                    #region Find Accelerations for Current Position

                    // this next functions puts the ax and ay values in the joints
                    if (!accelSolver.Solve())
                    {
                        Status += "Analytical acceleration could not be found at" + currentTime + ".";
                        NumericalAcceleration(timeStep, ijoints, ilinks);
                    }

                    #endregion

                    currentTime += timeStep;
                    double[,] jointParams = WriteJointStatesVariablesToMatrixAndToLast(ijoints);
                    double[,] linkParams = WriteLinkStatesVariablesToMatrixAndToLast(ilinks);
                    if (Forward)
                    {
                        lock (JointParameters)
                            JointParameters.AddNearEnd(currentTime, jointParams);
                        lock (LinkParameters)
                            LinkParameters.AddNearEnd(currentTime, linkParams);
                    }
                    else
                    {
                        lock (JointParameters)
                            JointParameters.AddNearBegin(currentTime, jointParams);
                        lock (LinkParameters)
                            LinkParameters.AddNearBegin(currentTime, linkParams);
                    }
                }
            } while (validPosition && lessThanFullRotation());
        }

        private void SimulateWithinError(List<joint> joints, List<link> links, Boolean Forward)
        {
            double startingPosChange = Forward ? Constants.DefaultStepSize : -Constants.DefaultStepSize;
            if (inputJoint.jointType == JointTypes.P) startingPosChange *= AverageLength;
            double maxLengthError = MaxSmoothingError * AverageLength;
            double currentTime = 0.0;
            Boolean validPosition;
            var posFinder = new PositionFinder(joints, links, gearsData, inputJointIndex);
            var velSolver = new VelocitySolver(ijoints, ilinks, firstInputJointIndex, inputJointIndex, inputLinkIndex, InputSpeed);
            var accelSolver = new AccelerationSolver(ijoints, ilinks, firstInputJointIndex, inputJointIndex, inputLinkIndex, InputSpeed);
            do
            {
                #region Find Next Positions
                int k = 0;
                double upperError, timeStep;
                do
                {
                    timeStep = startingPosChange / InputSpeed;
                    NumericalPosition(timeStep, joints, links);
                    validPosition = posFinder.DefineNewPositions(startingPosChange);
                    upperError = posFinder.PositionError - maxLengthError;
                    if (validPosition && upperError < 0)
                    {
                        startingPosChange *= Constants.ErrorSizeIncrease;
                        // startingPosChange = startingPosChange * maxLengthError / (maxLengthError + upperError);
                    }
                    else startingPosChange *= Constants.ConservativeErrorEstimation * 0.5;
                } while (upperError > 0 && k++ < Constants.MaxItersInPositionError);
                //var tempStep = startingPosChange;
                //startingPosChange = (Constants.ErrorEstimateInertia * prevStep + startingPosChange) / (1 + Constants.ErrorEstimateInertia);
                //prevStep = tempStep;

                #endregion

                if (validPosition)
                {
                    if (Forward)
                        lock (InputRange)
                        {
                            InputRange[1] = links[inputLinkIndex].Angle;
                        }
                    else
                        lock (InputRange)
                        {
                            InputRange[0] = links[inputLinkIndex].Angle;
                        }

                    #region Find Velocities for Current Position

                    // this next functions puts the vx and vy values as well as the vx_unit and vy_unit in the joints
                    if (!velSolver.Solve())
                    {
                        Status += "Instant Centers could not be found at" + currentTime + ".";
                        NumericalVelocity(timeStep, joints, links);
                    }

                    #endregion

                    #region Find Accelerations for Current Position

                    // this next functions puts the ax and ay values in the joints
                    if (!accelSolver.Solve())
                    {
                        Status += "Analytical acceleration could not be found at" + currentTime + ".";
                        NumericalAcceleration(timeStep, joints, links);
                    }

                    #endregion

                    currentTime += timeStep;
                    double[,] jointParams = WriteJointStatesVariablesToMatrixAndToLast(joints);
                    double[,] linkParams = WriteLinkStatesVariablesToMatrixAndToLast(links);
                    if (Forward)
                    {
                        lock (JointParameters)
                            JointParameters.AddNearEnd(currentTime, jointParams);
                        lock (LinkParameters)
                            LinkParameters.AddNearEnd(currentTime, linkParams);
                    }
                    else
                    {
                        lock (JointParameters)
                            JointParameters.AddNearBegin(currentTime, jointParams);
                        lock (LinkParameters)
                            LinkParameters.AddNearBegin(currentTime, linkParams);
                    }
                }
            } while (validPosition && lessThanFullRotation());
        }


        private double[,] WriteJointStatesVariablesToMatrixAndToLast(List<joint> joints)
        {
            var jointParams = new double[numJoints, 6];
            for (int i = 0; i < numJoints; i++)
            {
                joint j = joints[i];
                jointParams[i, 0] = j.x;
                jointParams[i, 1] = j.y;
                jointParams[i, 2] = j.vx;
                jointParams[i, 3] = j.vy;
                jointParams[i, 4] = j.ax;
                jointParams[i, 5] = j.ay;
                j.xLast = j.x;
                j.yLast = j.y;
                j.vxLast = j.vx;
                j.vyLast = j.vy;
            }
            return jointParams;
        }

        private double[,] WriteLinkStatesVariablesToMatrixAndToLast(List<link> links)
        {
            var linkParams = new double[numLinks, 3];
            for (int i = 0; i < numLinks; i++)
            {
                link l = links[i];
                linkParams[i, 0] = l.Angle;
                linkParams[i, 1] = l.Velocity;
                linkParams[i, 2] = l.Acceleration;
                l.AngleLast = l.Angle;
                l.VelocityLast = l.Velocity;
            }
            return linkParams;
        }
    }
}