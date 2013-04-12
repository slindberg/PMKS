﻿using System.Collections.Generic;
using System.Windows;

namespace PMKS_Silverlight_App
{
    public enum AngleType
    {
        Radians, Degrees
    }
    public enum LengthType
    {
        mm, inches
    }
    public static class DisplayConstants
    {
        public const double PenThicknessRatio = 1;
        public const double VelocityLengthRatio = 0.3;
        public const double AccelLengthRatio = 0.5;
        public const double TickDistance = 24.0; // one-quarter of an inch
        public const double JointSize = 4.0; // one-sixteenth of an inch
        public const double AxesBuffer = 10; // one inch
        public const double UnCroppedDimension = 99999;
    }
    public class JointTypeProvider
    {
        public List<string> jointTypeList
        {
            get { return new List<string> { "R (pin joint)", "P (sliding block)", "RP (pin in slot)", "G (gear teeth)" }; }
        }
    }
}