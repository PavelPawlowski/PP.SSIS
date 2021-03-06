﻿// <copyright file="ColumnsToXmlTransformationUI.cs" company="Pavel Pawlowski">
// Copyright (c) 2014, 2015 All Right Reserved
//
// THIS CODE AND INFORMATION ARE PROVIDED "AS IS" WITHOUT WARRANTY OF ANY 
// KIND, EITHER EXPRESSED OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE
// IMPLIED WARRANTIES OF MERCHANTABILITY AND/OR FITNESS FOR A
// PARTICULAR PURPOSE.
//
// </copyright>
// <author>Pavel Pawlowski</author>
// <summary>Contains UI Helper for ColumnsToXmlTransformation</summary>

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.SqlServer.Dts.Pipeline;
using Microsoft.SqlServer.Dts.Pipeline.Wrapper;
using Microsoft.SqlServer.Dts.Runtime.Wrapper;
using System.Xml.Linq;
using System.Globalization;
using System.Security.Cryptography;
using System.IO;
using PP.SSIS.DataFlow.Properties;
using PP.SSIS.DataFlow.UI.Common;

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
    public class RegExExtractionTransformationUI : DataFlowUI<RegExExtractionTransformationForm>
    {
        //Contains List of Input Columns available for RegExExtration transformation
        private static List<ConvertMetadataInputColumn> _inputColumns = new List<ConvertMetadataInputColumn>();

        /// <summary>
        /// List of Columsn available for input for RegExExtraction Transformation
        /// </summary>
        public static List<ConvertMetadataInputColumn> InputColumns
        {
            get { return _inputColumns; }
        }


        public override void Initialize(IDTSComponentMetaData dtsComponentMetadata, IServiceProvider serviceProvider)
        {
            base.Initialize(dtsComponentMetadata, serviceProvider);
            _inputColumns = new List<ConvertMetadataInputColumn>();


            
            IDTSVirtualInput vInput = GetVirtualInput(0); //Get virtualInput for the RegExExtractionTransformation
            if (vInput != null)
            {

                foreach (IDTSVirtualInputColumn vcol in vInput.VirtualInputColumnCollection)
                {
                    if (RegExExtractionTransformation.SupportedDataTypes.Contains(vcol.DataType))
                    {
                        ConvertMetadataInputColumn iCol = new ConvertMetadataInputColumn(vcol);
                        _inputColumns.Add(iCol);
                    }
                }
            }



            //Sort the columns by name
            _inputColumns.Sort((a, b) => a.Name.CompareTo(b.Name));

            //Check if the column names repeats and in that cas set the DisplayName to include also UpstreamComponentname
            for (int i = 0; i < _inputColumns.Count; i++)
            {
                if (i >= 0 && i < _inputColumns.Count - 1 && _inputColumns[i].Name == _inputColumns[i + 1].Name)
                    _inputColumns[i].DisplayName = string.Format("{0}.{1}", _inputColumns[i].UpstreamComponentName, _inputColumns[i].Name);
                else if (i > 0 && i < _inputColumns.Count && _inputColumns[i].Name == _inputColumns[i - 1].Name)
                    _inputColumns[i].DisplayName = string.Format("{0}.{1}", _inputColumns[i].UpstreamComponentName, _inputColumns[i].Name);
            }

            //add the "Not Specified" column (LineageID = 0)
            _inputColumns.Insert(0, ConvertMetadataInputColumn.NotSpecifiedInputColumn);

        }

    }
}
