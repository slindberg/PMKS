﻿using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Office.Tools.Ribbon;
using PlanarMechanismSimulator;

namespace ExcelPlanarMechSimulator
{
    public partial class MechSimRibbon
    {
        private void MechSimRibbon_Load(object sender, RibbonUIEventArgs e)
        {
            button_Simulate.Enabled = false;
        }

        protected Simulator pms { get; set; }

        private void button_Parse_Click(object sender, RibbonControlEventArgs e)
        {
            try
            {
                Globals.Sheet1.Range["j2"].Value2 = "?";
                Globals.Sheet1.Range["h4"].Value2 = "";
                button_Simulate.Enabled = true;
                var rng = Globals.Sheet1.Range["B3:F22"];
                object[,] data = rng.Value2;
                var numRows = data.GetLength(0);
                var LinkIDs = new List<List<string>>();
                var typList = new List<string>();
                var Positions = new List<double[]>();
                for (int i = 0; i < data.GetLength(0); i++)
                {
                    if ((data[i + 1, 1] == null) || (string.IsNullOrWhiteSpace(data[i + 1, 1].ToString()))) break;
                    typList.Add(data[i + 1, 1].ToString());
                    if (typList[i].Equals("p", StringComparison.InvariantCultureIgnoreCase)
                        || typList[i].Equals("rp", StringComparison.InvariantCultureIgnoreCase))
                    {
                        double Xtemp, Ytemp, angleTemp;
                        if (data[i + 1, 2] != null && double.TryParse(data[i + 1, 2].ToString(), out Xtemp)
                            && data[i + 1, 3] != null && double.TryParse(data[i + 1, 3].ToString(), out Ytemp)
                            && data[i + 1, 4] != null && double.TryParse(data[i + 1, 4].ToString(), out angleTemp))
                            Positions.Add(new[] { angleTemp, Xtemp, Ytemp });
                        else throw new Exception("Numerical data required (x, y, and angle) for joint at row " + (i + 1).ToString());
                    }
                    else
                    {
                        double Xtemp, Ytemp;
                        if (data[i + 1, 2] != null && double.TryParse(data[i + 1, 2].ToString(), out Xtemp)
                            && data[i + 1, 3] != null && double.TryParse(data[i + 1, 3].ToString(), out Ytemp))
                            Positions.Add(new[] { Xtemp, Ytemp });
                        else throw new Exception("Numerical data required (x, and y) for joint at row " + (i + 1).ToString());
                    }
                    if (data[i + 1, 5] != null) LinkIDs.Add(data[i + 1, 5].ToString().Split(',', ' ').ToList());
                    else throw new Exception("One or more link names required for joint at row " + (i + 1).ToString());

                }
                pms = new Simulator(LinkIDs, typList, Positions);

                if (pms.IsDyadic) Globals.Sheet1.Range["h3"].Value2 = "The mechanism is comprised of only of dyads.";
                else Globals.Sheet1.Range["h3"].Value2 = "The mechanism has non-dyadic loops.";
                int dof = pms.DegreesOfFreedom;
                Globals.Sheet1.Range["j2"].Value2 = dof;
                if (dof == 1)
                {
                    button_Simulate.Enabled = true;
                }
                else
                {
                    Globals.Sheet1.Range["h4"].Value2 = "Cannot simulate mechanisms with degrees of freedom greater than"
                                                        + " or less than 1. ";
                    button_Simulate.Enabled = false;
                    // Globals.Sheet1.Range["j2"].Style //trying to change background color.
                }

            }
            catch (Exception exc)
            {
                Globals.Sheet1.Range["h4"].Value2 += "Error in input data: " + exc.Message;
                button_Simulate.Enabled = false;
            }
            finally
            {
                if (button_Simulate.Enabled)
                    Globals.Sheet1.Range["h4"].Value2 += " Ready to simulate.";
            }
        }

        private void button_Simulate_Click(object sender, RibbonControlEventArgs e)
        {
            var rpm = double.NaN;
            if (Globals.Sheet1.Range["n2"].Value2 == null || !double.TryParse(Globals.Sheet1.Range["n2"].Value2.ToString(), out rpm))
                rpm = 10.0;
            Globals.Sheet1.Range["n2"].Value2 = rpm;
            pms.InputSpeed = 2 * Math.PI * rpm / 60.0;

            var deltaAngle = double.NaN;
            if (Globals.Sheet1.Range["r2"].Value2 == null || !double.TryParse(Globals.Sheet1.Range["r2"].Value2.ToString(), out deltaAngle))
                deltaAngle = 1.0;
            Globals.Sheet1.Range["r2"].Value2 = deltaAngle;
            pms.DeltaAngle = Math.PI * deltaAngle / 180.0;


            pms.FindFullMovement();
            button_Simulate.Enabled = false;
            SortedList<double, double[,]> linkParameters = pms.LinkParameters;
            SortedList<double, double[,]> jointParameters = pms.JointParameters;
            
            //add to 2nd and 3rd sheets of spreadsheet
            //Globals.Sheet2.Range["b2:d5"].Value = linkParameters[0.0];
            int jointNumCol = 6;
            int linkNumCol = 3;

            //create headings for joint data
            mergeAndCenter(Globals.Sheet2.Range["a1:a2"]);
            Globals.Sheet2.Cells[1, 1] = "time";
            for (int i = 0; i < jointParameters[0.0].Length/jointNumCol; i++)
            {
                mergeAndCenter(Globals.Sheet2.Range[Globals.Sheet2.Cells[1, 2 + jointNumCol * i], Globals.Sheet2.Cells[1, 1 + jointNumCol * (i + 1)]]);
                Globals.Sheet2.Cells[1, 2 + jointNumCol * i].Value = ("Joint " + (i + 1) + " (") + Globals.Sheet1.Cells[i + 3, 6].Value + ")";
                Globals.Sheet2.Cells[2, 2 + jointNumCol * i].Value = "x";
                Globals.Sheet2.Cells[2, 3 + jointNumCol * i].Value = "y";
                Globals.Sheet2.Cells[2, 4 + jointNumCol * i].Value = "v-x";
                Globals.Sheet2.Cells[2, 5 + jointNumCol * i].Value = "v-y";
                Globals.Sheet2.Cells[2, 6 + jointNumCol * i].Value = "a-x";
                Globals.Sheet2.Cells[2, 7 + jointNumCol * i].Value = "a-y";
            }
            ////create headings for link data
            //mergeAndCenter(Globals.Sheet3.Range["a1:a2"]);
            //Globals.Sheet3.Cells[1, 1] = "time";
            //for (int i = 0; i < linkParameters[0.0].Length / linkNumCol; i++)
            //{
            //    mergeAndCenter(Globals.Sheet2.Range[Globals.Sheet2.Cells[1, 2 + jointNumCol * i], Globals.Sheet2.Cells[1, 1 + jointNumCol * (i + 1)]]);
            //    Globals.Sheet2.Cells[1, 2 + jointNumCol * i].Value = ("Joint " + (i + 1) + " (") + Globals.Sheet1.Cells[i + 3, 6].Value + ")";
            //    Globals.Sheet2.Cells[2, 2 + jointNumCol * i].Value = "x";
            //    Globals.Sheet2.Cells[2, 3 + jointNumCol * i].Value = "y";
            //    Globals.Sheet2.Cells[2, 4 + jointNumCol * i].Value = "v-x";
            //    Globals.Sheet2.Cells[2, 5 + jointNumCol * i].Value = "v-y";
            //    Globals.Sheet2.Cells[2, 6 + jointNumCol * i].Value = "a-x";
            //    Globals.Sheet2.Cells[2, 7 + jointNumCol * i].Value = "a-y";
            //}


            //print data
            int timeIndex = 0;
            double[,] dataValue;
            foreach (KeyValuePair<double, double[,]> dataRow in jointParameters)
            {
                Globals.Sheet2.Cells[timeIndex + 3, 1].Value = dataRow.Key;
                dataValue = dataRow.Value;
                for (int j = 0; j < dataValue.Length; j++)
                {
                    Globals.Sheet2.Cells[timeIndex + 3, 2 + j].Value = dataValue[(j / jointNumCol), (j % jointNumCol)];
                }
                timeIndex++;
            }
            //for (int i = 0; i < jointParameters.Count; i++)
            //{
            //    //foreach (KeyValuePair<double, double[,]> d in jointParameters)
            //    //{
            //    //    Globals.Sheet2.Cells[i+3, 1].Value = d.Key;
            //    //}
            //    for (int j = 0; j < jointParameters[0.0].Length; j++)
            //    {
            //        //Globals.Sheet2.Cells[7, 7].Value = jointParameters.Values(3);
            //        //Globals.Sheet2.Range[Globals.Sheet2.Cells[i + 3, 2 + jointNumCol * j], Globals.Sheet2.Cells[i + 3, 1 + jointNumCol * (j + 1)]].Value = jointParameters.GetEnumerator(1);
            //        //for (int k=0; k</jointNumCol
            //    }
            //}
            //jointParameters.Count
            //Globals.Sheet2.Cells[1, 1
            //for (int i = 0; i <= jointParameters[0.0].Length / jointNumCol; i++)
            //{
            //    Globals.Sheet2.Range[Globals.Sheet2.Cells[1, 2+jointNumCol*i], Globals.Sheet2.Cells[1, 1+jointNumCol*(i+1)]].Merge();
            //}
            //Globals.Sheet2.Range["f1:f2"].Value = linkParameters[0.0].Length;
            // hanxu
        }

        private void mergeAndCenter(Microsoft.Office.Interop.Excel.Range theRange)
        {
            //this function merges and centers the selected range of cells
            theRange.Merge();
            theRange.HorizontalAlignment = Microsoft.Office.Interop.Excel.XlVAlign.xlVAlignCenter;
            theRange.VerticalAlignment = Microsoft.Office.Interop.Excel.XlVAlign.xlVAlignCenter;
            return;
        }


        private void editBox_speed_TextChanged(object sender, RibbonControlEventArgs e)
        {

        }

        private void box_incrementType_TextChanged(object sender, RibbonControlEventArgs e)
        {

        }

        private void editBox_incrementValue_TextChanged(object sender, RibbonControlEventArgs e)
        {

        }

        private void editBox_numSteps_TextChanged(object sender, RibbonControlEventArgs e)
        {

        }

    }
}