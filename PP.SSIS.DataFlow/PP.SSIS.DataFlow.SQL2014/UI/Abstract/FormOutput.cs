// <copyright file="FormOutput.cs" company="Pavel Pawlowski">
// Copyright (c) 2014, 2015 All Right Reserved
//
// THIS CODE AND INFORMATION ARE PROVIDED "AS IS" WITHOUT WARRANTY OF ANY 
// KIND, EITHER EXPRESSED OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE
// IMPLIED WARRANTIES OF MERCHANTABILITY AND/OR FITNESS FOR A
// PARTICULAR PURPOSE.
//
// </copyright>
// <author>Pavel Pawlowski</author>
// <summary>Contains definition of FormOutput</summary>
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.SqlServer.Dts.Pipeline.Wrapper;
using PP.SSIS.DataFlow.Properties;
using System.ComponentModel;
using Microsoft.SqlServer.Dts.Runtime.Wrapper;
using System.Runtime.CompilerServices;


using IDTSOutput = Microsoft.SqlServer.Dts.Pipeline.Wrapper.IDTSOutput100;
using IDTSCustomProperty = Microsoft.SqlServer.Dts.Pipeline.Wrapper.IDTSCustomProperty100;
using IDTSOutputColumn = Microsoft.SqlServer.Dts.Pipeline.Wrapper.IDTSOutputColumn100;
using IDTSInput = Microsoft.SqlServer.Dts.Pipeline.Wrapper.IDTSInput100;
using IDTSInputColumn = Microsoft.SqlServer.Dts.Pipeline.Wrapper.IDTSInputColumn100;
using IDTSVirtualInput = Microsoft.SqlServer.Dts.Pipeline.Wrapper.IDTSVirtualInput100;
using IDTSComponentMetaData = Microsoft.SqlServer.Dts.Pipeline.Wrapper.IDTSComponentMetaData100;
using IDTSDesigntimeComponent = Microsoft.SqlServer.Dts.Pipeline.Wrapper.IDTSDesigntimeComponent100;
using IDTSVirtualInputColumn = Microsoft.SqlServer.Dts.Pipeline.Wrapper.IDTSVirtualInputColumn100;

namespace PP.SSIS.DataFlow.UI
{
    /// <summary>
    /// Encapsultes IDTSOutput for the usage in the GUI Forms to allow easy properties modification in the PropertyGrid
    /// </summary>
    [DefaultProperty("Name")]
    public class FormOutput : INotifyPropertyChanged, INameProvider
    {
        [Browsable(false)]
        public IDTSOutput DTSOutput { get; private set; }

        private Guid guid = Guid.NewGuid();
        /// <summary>
        /// Gets the GUID which uniquely identifies the instance of the FormOutputColumn
        /// </summary>
        [Browsable(false)]
        public Guid Guid
        {
            get { return guid; }
        }

        public FormOutput(IDTSOutput output)
        {
            DTSOutput = output;
        }

        /// <summary>
        /// Gets Index of the Output in the component Outputs collection
        /// </summary>
        [ReadOnly(true)]
        [Browsable(false)]
        public int Index { get; set; }

        /// <summary>
        /// Gets ID of the Output
        /// </summary>
        [Category("Output")]
        [ReadOnly(true)]
        public int ID
        {
            get { return DTSOutput.ID; }

        }

        /// <summary>
        /// Gets or Sets the Name of the Output
        /// </summary>
        [Category("Output")]
        [Description("Name of the Output")]
        public string Name
        {
            get { return DTSOutput.Name; }
            set
            {
                if (DTSOutput.Name != value)
                {
                    DTSOutput.Name = value;
                    NotifyPropertyChanged("Name");
                }
            }
        }

        /// <summary>
        /// Gets or Sets Description of the Output
        /// </summary>
        [Category("Output")]
        [Description("Description of the Output")]
        public string Description
        {
            get { return DTSOutput.Description; }
            set
            {
                if (DTSOutput.Description != value)
                {
                    DTSOutput.Description = value;
                    NotifyPropertyChanged("Description");
                }
            }
        }

        protected void NotifyPropertyChanged(String propertyName)
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;
    }
}
