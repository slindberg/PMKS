﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using OptimizationToolbox;

namespace PlanarMechanismSimulator
{
    public partial class Simulator : IDependentAnalysis
    {
        public void FindFullMovement()
        {
            if ((double.IsNaN(DeltaAngle)) && (double.IsNaN(FixedTimeStep)))
                throw new Exception(
                    "Either the angle delta or the time step must be specified.");

            #region Set up initial point parameters (x, x-dot, x-double-dot, etc.)

            SetUpDyadicVelocityObjects();
            JointParameters = new TimeSortedList();
            LinkParameters = new TimeSortedList();

            var initPivotParams = new double[numJoints, 6];
            for (int i = 0; i < numJoints; i++)
            {
                initPivotParams[i, 0] = joints[i].xInitial;
                initPivotParams[i, 1] = joints[i].yInitial;
            }
            var initLinkParams = new double[numLinks, 3];
            for (int i = 0; i < numLinks; i++)
                initLinkParams[i, 0] = links[i].AngleInitial;
            InitializeGroundAndInputSpeedAndAcceleration(initPivotParams, initLinkParams);
            InputRange = new[] { inputLink.AngleInitial, inputLink.AngleInitial };
            JointParameters.Add(0.0, initPivotParams);
            LinkParameters.Add(0.0, initLinkParams);
            posFinder = new PositionFinder(joints, links, gearsData, inputJointIndex);
            var posFinderBackwards = setUpNewPositionFinder();
            /* attempt to find velocities and accelerations at initial point analytically
             * there is no point in trying numerically as this is the first point and the numerical methods
             * perform finite difference of current and last time steps. */
            if (!(findVelocitiesThroughICMethod(0.0, true) && findAccelerationAnalytically(0.0, true)))
            {
                var ForwardJointParams = (double[,])initPivotParams.Clone();
                var ForwardLinkParams = (double[,])initLinkParams.Clone();
                bool forwardSuccess = false;
                var BackwardJointParams = (double[,])initPivotParams.Clone();
                var BackwardLinkParams = (double[,])initLinkParams.Clone();
                bool backwardSuccess = false;
                var smallTimeStep = Constants.SmallPerturbationFraction * FixedTimeStep;
                forwardSuccess =
                    microPerturbForFiniteDifferenceOfVelocityAndAcceleration(smallTimeStep,
                                                                             ForwardJointParams, ForwardLinkParams,
                                                                             initPivotParams, initLinkParams, posFinder);
                if (forwardSuccess)
                    NumericalVelocity(smallTimeStep, ForwardJointParams, ForwardLinkParams, initPivotParams,
                                      initLinkParams);
                /*** Stepping Backward in Time ***/
                backwardSuccess = microPerturbForFiniteDifferenceOfVelocityAndAcceleration(-smallTimeStep,
                                                                                           BackwardJointParams,
                                                                                           BackwardLinkParams,
                                                                                           initPivotParams,
                                                                                           initLinkParams,
                                                                                           posFinderBackwards);
                if (backwardSuccess)
                    NumericalVelocity(-smallTimeStep, BackwardJointParams, BackwardLinkParams, initPivotParams,
                                      initLinkParams);


                if (forwardSuccess && backwardSuccess)
                {
                    /* central difference puts values in init parameters. */
                    for (int i = 0; i < firstInputJointIndex; i++)
                    {
                        initPivotParams[i, 2] = (ForwardJointParams[i, 2] + BackwardJointParams[i, 2]) / 2;
                        initPivotParams[i, 3] = (ForwardJointParams[i, 3] + BackwardJointParams[i, 3]) / 2;
                        initPivotParams[i, 4] = (ForwardJointParams[i, 2] - BackwardJointParams[i, 2]) / (2 * smallTimeStep);
                        initPivotParams[i, 5] = (ForwardJointParams[i, 3] - BackwardJointParams[i, 3]) / (2 * smallTimeStep);
                    }
                    for (int i = 0; i < inputLinkIndex; i++)
                    {
                        initLinkParams[i, 1] = (ForwardLinkParams[i, 1] + BackwardLinkParams[i, 1]) / 2;
                        initLinkParams[i, 2] = (ForwardLinkParams[i, 1] - BackwardLinkParams[i, 1]) / (2 * smallTimeStep);
                    }
                }
                else if (forwardSuccess)
                {
                    for (int i = 0; i < firstInputJointIndex; i++)
                    {
                        initPivotParams[i, 2] = ForwardJointParams[i, 2];
                        initPivotParams[i, 3] = ForwardJointParams[i, 3];
                    }
                    for (int i = 0; i < inputLinkIndex; i++)
                        initLinkParams[i, 1] = ForwardLinkParams[i, 1];
                }
                else if (backwardSuccess)
                {
                    for (int i = 0; i < firstInputJointIndex; i++)
                    {
                        initPivotParams[i, 2] = BackwardJointParams[i, 2];
                        initPivotParams[i, 3] = BackwardJointParams[i, 3];
                    }
                    for (int i = 0; i < inputLinkIndex; i++)
                        initLinkParams[i, 1] = BackwardLinkParams[i, 1];
                }
            }

            #endregion
#if DEBUGSERIAL
            SimulateWithFixedDelta(FixedTimeStep, initPivotParams, initLinkParams, true, posFinder);
#elif SILVERLIGHT
            var forwardThread = new Thread(delegate()
                {
                    SimulateWithFixedDelta(FixedTimeStep, initPivotParams, initLinkParams, true, posFinder);
                    forwardDone.Set();
                });
            var backwardThread = new Thread(delegate()
                {
                    SimulateWithFixedDelta(-FixedTimeStep, initPivotParams, initLinkParams, false, posFinderBackwards);
                    backwardDone.Set();
                });
            forwardThread.Start(); backwardThread.Start();
            if (forwardDone.WaitOne() && backwardDone.WaitOne())
            {
                forwardDone = new AutoResetEvent(false);
                backwardDone = new AutoResetEvent(false);
            }
#else
            Parallel.Invoke(
                /*** Stepping Forward in Time ***/
                () => SimulateWithFixedDelta(FixedTimeStep, initPivotParams, initLinkParams, true, posFinder),
                /*** Stepping Backward in Time ***/
                () => SimulateWithFixedDelta(-FixedTimeStep, initPivotParams, initLinkParams, false, posFinderBackwards));
#endif
        }

        private PositionFinder setUpNewPositionFinder()
        {
            var newJoints = joints.Select(j => j.copy()).ToList();
            var newLinks = links.Select(c => c.copy(joints, newJoints)).ToList();
            foreach (var j in newJoints)
            {
                j.Link1 = newLinks[links.IndexOf(j.Link1)];
                if (j.Link2 != null)
                    j.Link2 = newLinks[links.IndexOf(j.Link2)];
            }
            return new PositionFinder(newJoints, newLinks, gearsData, inputJointIndex);
        }

        private static AutoResetEvent forwardDone = new AutoResetEvent(false);
        private static AutoResetEvent backwardDone = new AutoResetEvent(false);

        private bool microPerturbForFiniteDifferenceOfVelocityAndAcceleration(double[,] currentJointParams,
            double[,] currentLinkParams, double[,] initPivotParams, double[,] initLinkParams, PositionFinder posFinder)
        {
            if (!posFinder.DefineNewPositions(Constants.SmallPerturbationFraction * FixedTimeStep * InputSpeed,
                currentJointParams, currentLinkParams, initPivotParams, initLinkParams))
                return false;

            return true;
        }

        private void SimulateWithFixedDelta(double[,] lastPivotParams, double[,] lastLinkParams, Boolean Forward, PositionFinder posFinder)
        {
            var currentTime = 0.0;
            Boolean validPosition;
            do
            {
                var currentLinkParams = new double[numLinks, 3];
                var currentPivotParams = new double[numJoints, 6];

                #region Find Next Positions

                NumericalPosition(FixedTimeStep, currentPivotParams, currentLinkParams,
                                  lastPivotParams, lastLinkParams);
                var delta = InputSpeed * FixedTimeStep;
                validPosition = posFinder.DefineNewPositions(delta, currentPivotParams, currentLinkParams,
                                                   lastPivotParams, lastLinkParams);
                #endregion

                if (validPosition)
                {
                    if (Forward)
                        lock (InputRange)
                        {
                            InputRange[1] = currentLinkParams[inputLinkIndex, 0];
                        }
                    else
                        lock (InputRange)
                        {
                            InputRange[0] = currentLinkParams[inputLinkIndex, 0];
                        }
                    InitializeGroundAndInputSpeedAndAcceleration(currentPivotParams, currentLinkParams);

                    #region Find Velocities for Current Position

                    if (!findVelocitiesThroughICMethod(currentTime, true))
                    {
                        Status += "Instant Centers could not be found at" + currentTime + ".";
                        NumericalVelocity(FixedTimeStep, currentPivotParams, currentLinkParams,
                                          lastPivotParams, lastLinkParams);
                    }

                    #endregion

                    #region Find Accelerations for Current Position

                    if (!findAccelerationAnalytically(currentTime, true))
                    {
                        Status += "Analytical acceleration could not be found at" + currentTime + ".";
                        NumericalAcceleration(FixedTimeStep, currentPivotParams, currentLinkParams,
                                              lastPivotParams, lastLinkParams);
                    }

                    #endregion

                    currentTime += FixedTimeStep;
                    if (Forward)
                    {
                        lock (JointParameters)
                            JointParameters.AddNearEnd(currentTime, currentPivotParams);
                        lock (LinkParameters)
                            LinkParameters.AddNearEnd(currentTime, currentLinkParams);
                    }
                    else
                    {
                        lock (JointParameters)
                            JointParameters.AddNearBegin(currentTime, currentPivotParams);
                        lock (LinkParameters)
                            LinkParameters.AddNearBegin(currentTime, currentLinkParams);
                    }
                    lastPivotParams = currentPivotParams;
                    lastLinkParams = currentLinkParams;
                }
            } while (validPosition && lessThanFullRotation());
        }


        private void loopWithSmoothingError(double smoothError, double[,] lastPivotParams, double[,] lastLinkParams,
                                            Boolean Forward)
        {
            //if (!double.IsNaN(MaxSmoothingError) && positionError >= MaxSmoothingError)
            //    posResult = PositionAnalysisResults.ErrorExceeded;
            //else
        }
    }
}
