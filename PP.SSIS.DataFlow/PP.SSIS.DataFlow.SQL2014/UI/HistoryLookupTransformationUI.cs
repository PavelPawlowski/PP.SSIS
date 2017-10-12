// <copyright file="ColumnsToXmlTransformationUI.cs" company="Pavel Pawlowski">
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

#if SQL2008 || SQL2008R2 || SQL2012 || SQL2014 || SQL2016 || SQL2017
using IDTSOutput = Microsoft.SqlServer.Dts.Pipeline.Wrapper.IDTSOutput100;
using IDTSCustomProperty = Microsoft.SqlServer.Dts.Pipeline.Wrapper.IDTSCustomProperty100;
using IDTSOutputColumn = Microsoft.SqlServer.Dts.Pipeline.Wrapper.IDTSOutputColumn100;
using IDTSInput = Microsoft.SqlServer.Dts.Pipeline.Wrapper.IDTSInput100;
using IDTSInputColumn = Microsoft.SqlServer.Dts.Pipeline.Wrapper.IDTSInputColumn100;
using IDTSVirtualInput = Microsoft.SqlServer.Dts.Pipeline.Wrapper.IDTSVirtualInput100;
using IDTSComponentMetaData = Microsoft.SqlServer.Dts.Pipeline.Wrapper.IDTSComponentMetaData100;
using IDTSDesigntimeComponent = Microsoft.SqlServer.Dts.Pipeline.Wrapper.IDTSDesigntimeComponent100;
using IDTSVirtualInputColumn = Microsoft.SqlServer.Dts.Pipeline.Wrapper.IDTSVirtualInputColumn100;
#endif


namespace PP.SSIS.DataFlow.UI
{
    public class HistoryLookupTransformationUI : DataFlowUI<HistoryLookupTransformationForm>
    {
        //Contains List of Input Columns available in the DataInput
        private static List<ConvertMetadataInputColumn> _inputColumns = new List<ConvertMetadataInputColumn>();
        //Contains list of Input Columns available in the HistoryLookupInput
        private static List<ConvertMetadataInputColumn> _lookupColumns = new List<ConvertMetadataInputColumn>();

        //Contains List of Input Columns available in the DataInput as DateColumns
        private static List<ConvertMetadataInputColumn> _inputDateColumns = new List<ConvertMetadataInputColumn>();
        //Contains list of Input Columns available in the HistoryLookupInput as DateColumns
        private static List<ConvertMetadataInputColumn> _lookupDateColumns = new List<ConvertMetadataInputColumn>();


        /// <summary>
        /// List of Columsn available in the DataInput
        /// </summary>
        public static List<ConvertMetadataInputColumn> InputColumns
        {
            get { return _inputColumns; }
        }

        /// <summary>
        /// List of Columns available in the HistoryLookupInput
        /// </summary>
        public static List<ConvertMetadataInputColumn> LookupColumns
        {
            get { return _lookupColumns; }
        }


        public static List<ConvertMetadataInputColumn> InputDateColumns
        {
            get
            {
                if (_inputDateColumns.Count == 0 && InputColumns != null && InputColumns.Count > 0)
                {
                    _inputDateColumns = InputColumns.FindAll(ic => ic.LineageID == 0 || (ic.VirtualColumn != null && HistoryLookupTransformation.SupportedDateColumnCypes.Contains(ic.VirtualColumn.DataType)));
                    
                }
                return _inputDateColumns;
            }
        }

        public static List<ConvertMetadataInputColumn> LookupDateColumns
        {
            get
            {
                if (_lookupDateColumns.Count == 0 && InputColumns != null && InputColumns.Count > 0)
                {
                    _lookupDateColumns = LookupColumns.FindAll(ic => ic.LineageID == 0 || (ic.VirtualColumn != null && HistoryLookupTransformation.SupportedDateColumnCypes.Contains(ic.VirtualColumn.DataType)));
                }
                return _lookupDateColumns;
            }
        }


        public override void Initialize(IDTSComponentMetaData dtsComponentMetadata, IServiceProvider serviceProvider)
        {
            base.Initialize(dtsComponentMetadata, serviceProvider);
            _inputColumns = new List<ConvertMetadataInputColumn>();
            _lookupColumns = new List<ConvertMetadataInputColumn>();
            _inputDateColumns = new List<ConvertMetadataInputColumn>();
            _lookupDateColumns = new List<ConvertMetadataInputColumn>();

            //Load InputColumns
            if (ComponentMetadata != null && ComponentMetadata.InputCollection.Count > 0)
            {
                IDTSVirtualInput vInput = GetVirtualInput(0); //Get virtualInput for the Data Input
                if (vInput != null)
                {

                    foreach (IDTSVirtualInputColumn vcol in vInput.VirtualInputColumnCollection)
                    {
                        if (CheckColumnForInputValidity(vcol))
                        {
                            ConvertMetadataInputColumn iCol = new ConvertMetadataInputColumn(vcol);
                            _inputColumns.Add(iCol);
                        }
                    }
                }

                //Sort the columns by name
                _inputColumns.Sort((a, b) => a.Name.CompareTo(b.Name));

                //Check if teh column names repeats and in that cas set the DisplayName tto include also UpstreamComponentname
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

            //Load LookupColumns
            if (ComponentMetadata != null && ComponentMetadata.InputCollection.Count > 1)
            {
                IDTSVirtualInput vInput = GetVirtualInput(1); //Get virtualInput for the Data Input
                if (vInput != null)
                {

                    foreach (IDTSVirtualInputColumn vcol in vInput.VirtualInputColumnCollection)
                    {
                        if (CheckColumnForInputValidity(vcol))
                        {
                            ConvertMetadataInputColumn iCol = new ConvertMetadataInputColumn(vcol);
                            _lookupColumns.Add(iCol);
                        }
                    }
                }

                //Sort the columns by name
                _lookupColumns.Sort((a, b) => a.Name.CompareTo(b.Name));

                //Check if teh column names repeats and in that cas set the DisplayName tto include also UpstreamComponentname
                for (int i = 0; i < _lookupColumns.Count; i++)
                {
                    if (i >= 0 && i < _lookupColumns.Count - 1 && _lookupColumns[i].Name == _lookupColumns[i + 1].Name)
                        _lookupColumns[i].DisplayName = string.Format("{0}.{1}", _lookupColumns[i].UpstreamComponentName, _lookupColumns[i].Name);
                    else if (i > 0 && i < _lookupColumns.Count && _lookupColumns[i].Name == _lookupColumns[i - 1].Name)
                        _lookupColumns[i].DisplayName = string.Format("{0}.{1}", _lookupColumns[i].UpstreamComponentName, _lookupColumns[i].Name);
                }

                //add the "Not Specified" column (LineageID = 0)
                _lookupColumns.Insert(0, ConvertMetadataInputColumn.NotSpecifiedInputColumn);
            }
        }

        //Dissallow DT_NTEXT, DT_TEXT and DT_IMAGE data types.
        public override bool CheckColumnForInputValidity(IDTSVirtualInputColumn vInputColumn)
        {
            if (vInputColumn == null)
                return false;
            else
            {
                var dt = vInputColumn.DataType;

                return !HistoryLookupTransformation.UnsupportedInputTypes.Contains(dt);
            }
        }
    }
}
