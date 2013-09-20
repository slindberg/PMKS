﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Markup;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;
using PMKS_Silverlight_App;
using Silverlight_PMKS;

namespace PMKS_Silverlight_App
{
    public partial class FileAndEditPanel : UserControl
    {

        public FileAndEditPanel()
        {
            InitializeComponent();
            CollapseExpandButton.IsChecked = true;
        }


        private void ToggleButton_Checked(object sender, RoutedEventArgs e)
        {
            LayoutRoot.Visibility = Visibility.Visible;
            CollapseExpandArrow.RenderTransform = new CompositeTransform { ScaleY = -1, TranslateY = 6 };
        }

        private void ToggleButton_Unchecked(object sender, RoutedEventArgs e)
        {
            LayoutRoot.Visibility = Visibility.Collapsed;
            CollapseExpandArrow.RenderTransform = new CompositeTransform();
        }

        #region from EditButtons
        private void OpenButton_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new OpenFileDialog
            {
                Filter = "Text Files (*.txt)|*.txt|Comma-Separated Values (.csv)|*.csv"
            };
            if (dialog.ShowDialog() == true)
                using (var reader = dialog.File.OpenText())
                {
                    List<JointData> jointDataList = null;
                    if (JointData.ConvertTextToData(reader.ReadToEnd(), out jointDataList))
                    {
                        App.main.JointsInfo.Data.Clear();
                        App.main.LinksInfo.Data.Clear();
                        foreach (var j in jointDataList)
                        {
                            App.main.JointsInfo.Data.Add(j);
                            App.main.linkInputTable.UpdateLinksTableAterAdd(j);
                        }
                    }
                    if (jointDataList.All(jd => !jd.DrivingInput))
                        jointDataList.First(jd => jd.CanBeDriver).DrivingInput = true;
                }
            
            App.main.JointsInfo.Data.Add(new JointData());
            App.main.ParseData();

        }
        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new SaveFileDialog()
            {
                Filter = "Text Files (*.txt)|*.txt|Comma-Separated Values (.csv)|*.csv"
            };
            if (dialog.ShowDialog() == true)
                using (var stream = dialog.OpenFile())
                using (var writer = new StreamWriter(stream))
                    writer.Write(JointData.ConvertDataToText(App.main.JointsInfo.Data));
        }

        private void ClearButton_Click(object sender, RoutedEventArgs e)
        {
            App.main.JointsInfo.Data.Clear();
            App.main.LinksInfo.Data.Clear();
            App.main.JointsInfo.Data.Add(new JointData());
            App.main.ParseData();
        }
        private void RemoveButton_Click(object sender, RoutedEventArgs e)
        {
            var jointData = App.main.JointsInfo.Data;
            var Jointstable = App.main.fileAndEditPanel.dataGrid;
            if (jointData.Count > 0)
            {
                JointData removedJoint;
                if (Jointstable.SelectedItem == null)
                {
                    removedJoint = jointData[jointData.Count - 1];
                    jointData.RemoveAt(jointData.Count - 1);
                }
                else
                {
                    removedJoint = jointData[Jointstable.SelectedIndex];
                    jointData.RemoveAt(Jointstable.SelectedIndex);
                }
                App.main.linkInputTable.UpdateLinksTableAfterDeletion(removedJoint.LinkNamesList);
            }
            App.main.ParseData();
        }

        private void TargetShapeStream_OnTextChanged(object sender, TextChangedEventArgs e)
        {
            App.main.mainViewer.MainCanvas.Children.Remove(App.main.mainViewer.TargetPath);
            App.main.mainViewer.TargetPath = null;
            if (!string.IsNullOrWhiteSpace(TargetShapeStream.Text) &&
                TargetShapeStream.Text != "Enter Target Shape Stream Here.")
            {
                try
                {
                    App.main.mainViewer.TargetPath = (System.Windows.Shapes.Path)XamlReader.Load(
                        DisplayConstants.TargetPathStreamFront
                        + TargetShapeStream.Text
                        + DisplayConstants.TargetPathStreamEnd);
                    TargetShapeStream.FontStyle = FontStyles.Normal;
                    TargetShapeStream.Foreground = new SolidColorBrush(Colors.Black);
                    App.main.mainViewer.TargetPath.RenderTransform
                        = new TranslateTransform
                        {
                            X = App.main.mainViewer.XOffset,
                            Y = App.main.mainViewer.YOffset
                        };
                    App.main.mainViewer.MainCanvas.Children.Add(App.main.mainViewer.TargetPath);
                    return;
                }
                catch (Exception exc)
                {
                    App.main.status(exc.ToString());
                }
            }
            if (string.IsNullOrWhiteSpace(TargetShapeStream.Text))
            {
                App.main.mainViewer.TargetPath = null;
                TargetShapeStream.Text = "Enter Target Shape Stream Here.";
                TargetShapeStream.FontStyle = FontStyles.Italic;
                TargetShapeStream.Foreground = new SolidColorBrush(Color.FromArgb(255, 133, 133, 133));
            }
        }

        private void ExportDataButton_Click(object sender, RoutedEventArgs e)
        {
            ExportKinematicData.ExportToCSV();
        }

        private void TargetShapeStream_GotFocus(object sender, RoutedEventArgs e)
        {
            if (TargetShapeStream.Text == "Enter Target Shape Stream Here.")
                //    TargetShapeStream.Text = "";
                //else
                TargetShapeStream.SelectAll();

        }

        #endregion


        #region from original JointInputTable

        private void dataGrid_CellEditEnded(object sender, DataGridCellEditEndedEventArgs e)
        {
            App.main.ParseData();
        }

        private void dataGrid_AddNewRow(object sender, DataGridBeginningEditEventArgs e)
        {
            var currentIndex = e.Row.GetIndex();
            if (currentIndex == App.main.JointsInfo.Data.Count - 1)
                App.main.JointsInfo.Data.Add(new JointData());
        }


        private void RadioSelectInput_OnChecked(object sender, RoutedEventArgs e)
        {
            var jointData = ((RadioButton)sender).Tag as JointData;
            jointData.DrivingInput = true;
            App.main.ParseData();
        }
        private void radioSelectInput_Unchecked(object sender, RoutedEventArgs e)
        {
            var jointData = ((RadioButton)sender).Tag as JointData;
            jointData.DrivingInput = false;

        }

        private void PositionVisible_Click(object sender, RoutedEventArgs e)
        {
            var jointData = ((CheckBox)sender).Tag as JointData;
            jointData.PosVisible = (Boolean)((CheckBox)sender).IsChecked;
        }

        private void VelocityVisible_Click(object sender, RoutedEventArgs e)
        {
            var jointData = ((CheckBox)sender).Tag as JointData;
            jointData.VelocityVisible = (Boolean)((CheckBox)sender).IsChecked;

        }

        private void AccelerationVisible_Click(object sender, RoutedEventArgs e)
        {
            var jointData = ((CheckBox)sender).Tag as JointData;
            jointData.AccelerationVisible = (Boolean)((CheckBox)sender).IsChecked;

        }


        #endregion


        internal void ReportDOF(int dof)
        {
            DOFTextBox.Text = dof.ToString();
            if (dof == 1)
            {
                DOFBorder.Background = new SolidColorBrush(Color.FromArgb(255, 0, 158, 36));
                DOFBorder.BorderBrush = new SolidColorBrush(Colors.White);
            }
            else
            {
                DOFBorder.Background = new SolidColorBrush(Color.FromArgb(255,156,46,46));
                DOFBorder.BorderBrush = new SolidColorBrush(Colors.Black);
            }
        }
    }
}