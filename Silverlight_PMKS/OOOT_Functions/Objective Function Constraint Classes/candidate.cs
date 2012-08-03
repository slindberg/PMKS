﻿/*************************************************************************
 *     This file & class is part of the Object-Oriented Optimization
 *     Toolbox (or OOOT) Project
 *     Copyright 2010 Matthew Ira Campbell, PhD.
 *
 *     OOOT is free software: you can redistribute it and/or modify
 *     it under the terms of the GNU General public License as published by
 *     the Free Software Foundation, either version 3 of the License, or
 *     (at your option) any later version.
 *  
 *     OOOT is distributed in the hope that it will be useful,
 *     but WITHOUT ANY WARRANTY; without even the implied warranty of
 *     MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 *     GNU General public License for more details.
 *  
 *     You should have received a copy of the GNU General public License
 *     along with OOOT.  If not, see <http://www.gnu.org/licenses/>.
 *     
 *     Please find further details and contact information on OOOT
 *     at http://ooot.codeplex.com/.
 *************************************************************************/
namespace OptimizationToolbox
{

    /// <summary>
    /// 
    /// </summary>
    public class Candidate
    {
        /// <summary>
        /// Gets or sets the f values.
        /// </summary>
        /// <value>
        /// The f values.
        /// </value>
        public double[] fValues { get; set; }
        /// <summary>
        /// Gets or sets the g values.
        /// </summary>
        /// <value>
        /// The g values.
        /// </value>
        public double[] gValues { get; set; }
        /// <summary>
        /// Gets or sets the h values.
        /// </summary>
        /// <value>
        /// The h values.
        /// </value>
        public double[] hValues { get; set; }
        /// <summary>
        /// Gets or sets the x vector - the vector of design variables.
        /// </summary>
        /// <value>
        /// The x.
        /// </value>
        public double[] x { get; set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="Candidate"/> class.
        /// </summary>
        /// <param name="x">The x.</param>
        /// <param name="evaluationMethods">the evaluation methods </param>
        public Candidate(double[]x, abstractOptMethod evaluationMethods = null)
        {
            this.x = (double[]) x.Clone();
            if (evaluationMethods!=null)
            {
//                evaluationMethods
            }
        }


        public Candidate(double f, double[] x)
        {
            this.fValues = new[] {f};
            this.x = (double[])x.Clone();
        }
        public Candidate(double[] f, double[] x)
        {
            this.fValues = (double[])f.Clone();
            this.x = (double[])x.Clone();
        }
    }
}