// <copyright file="UIForm.cs" company="Pavel Pawlowski">
// Copyright (c) 2014, 2015 All Right Reserved
//
// THIS CODE AND INFORMATION ARE PROVIDED "AS IS" WITHOUT WARRANTY OF ANY 
// KIND, EITHER EXPRESSED OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE
// IMPLIED WARRANTIES OF MERCHANTABILITY AND/OR FITNESS FOR A
// PARTICULAR PURPOSE.
//
// </copyright>
// <author>Pavel Pawlowski</author>
// <summary>Contains General implementation of UI Form</summary>
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using Microsoft.SqlServer.Dts.Runtime;

using IDTSOutput = Microsoft.SqlServer.Dts.Pipeline.Wrapper.IDTSOutput100;
using IDTSCustomProperty = Microsoft.SqlServer.Dts.Pipeline.Wrapper.IDTSCustomProperty100;
using IDTSOutputColumn = Microsoft.SqlServer.Dts.Pipeline.Wrapper.IDTSOutputColumn100;
using IDTSInput = Microsoft.SqlServer.Dts.Pipeline.Wrapper.IDTSInput100;
using IDTSInputColumn = Microsoft.SqlServer.Dts.Pipeline.Wrapper.IDTSInputColumn100;
using IDTSVirtualInput = Microsoft.SqlServer.Dts.Pipeline.Wrapper.IDTSVirtualInput100;
using IDTSComponentMetaData = Microsoft.SqlServer.Dts.Pipeline.Wrapper.IDTSComponentMetaData100;
using IDTSDesigntimeComponent = Microsoft.SqlServer.Dts.Pipeline.Wrapper.IDTSDesigntimeComponent100;

namespace PP.SSIS.DataFlow.UI
{
    public class UIForm : Form
    {
        Variables variables;
        Connections connections;
        IDTSComponentMetaData componentMetadata;
        IDTSDesigntimeComponent designTimeComponent;

        protected Variables Variables
        {
            get { return variables; }
        }

        protected Connections Connections
        {
            get { return connections; }
        }

        protected IDTSComponentMetaData ComponentMetadata
        {
            get { return componentMetadata; }
        }

        protected IDTSDesigntimeComponent DesignTimeComponent
        {
            get { return designTimeComponent; }
        }

        public UIForm()
        {
        }

        public void InitializeUIForm(IDTSComponentMetaData componentMetadata, Variables variables, Connections connections, IDTSDesigntimeComponent designTimeComponent)
        {
            this.variables = variables;
            this.connections = connections;
            this.componentMetadata = componentMetadata;
            this.designTimeComponent = designTimeComponent;
        }
    }
}
