﻿using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Net;
using System.Windows;
using System.Reflection;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Ink;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;

namespace PMKS_Silverlight_App
{
    public class JointsViewModel : DependencyObject
    {
        public static readonly DependencyProperty DataCollectionProperty
            = DependencyProperty.Register("Data",
                                          typeof(ObservableCollection<JointData>), typeof(JointsViewModel),
                                          new PropertyMetadata(null));

        public ObservableCollection<JointData> Data
        {
            get { return (ObservableCollection<JointData>)GetValue(DataCollectionProperty); }
            set { SetValue(DataCollectionProperty, value); }
        }

        public JointsViewModel()
        {
            Data = new ObservableCollection<JointData>()
                {
                    new JointData {JointType = "R (pin joint)", XPos = "0.0", YPos = "0.0", LinkNames = "ground, input"},
                    new JointData {JointType = "p", XPos = "0.0", YPos = "10.0", Angle="0", LinkNames = "input coupler"},
                    new JointData
                        {JointType = "R (pin joint)", XPos = "12.0", YPos = "10.0", LinkNames = "coupler,output "},
                    new JointData
                        {JointType = "r", XPos = "14.0", YPos = "0.0", LinkNames = "ground, output"},
                                        new JointData()
                    //new JointData {JointType = "R (pin joint)", XPos = "0.0", YPos = "0.0", LinkNames = "ground, input"},
                    //new JointData {JointType = "R (pin joint)", XPos = "0.0", YPos = "8.0", LinkNames = "input coupler"},
                    //new JointData
                    //    {JointType = "R (pin joint)", XPos = "10.0", YPos = "12.0", LinkNames = "coupler,output "},
                    //new JointData
                    //    {JointType = "R (pin joint)", XPos = "10.0", YPos = "0.0", LinkNames = "ground, output"},
                    //                    new JointData()
                };
        }
    }
}

