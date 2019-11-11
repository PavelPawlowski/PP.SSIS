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
using System.Windows.Forms;


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
    public class LookupErrorAggregationTransformationUI : DataFlowUI<LookupErrorAggregationTransformationForm>
    {
        //Dissallow DT_NTEXT, DT_TEXT and DT_IMAGE data types.
        public override bool CheckColumnForInputValidity(IDTSVirtualInput vInput, IDTSInput input, FormInputColumn icol)
        {
            bool validForInput = false;

            var dt = icol.DTSVirtualColumn.DataType;

            if (dt == DataType.DT_NTEXT || dt == DataType.DT_TEXT || dt == DataType.DT_IMAGE)
                validForInput = false;
            else
            {
                if (icol.DTSInputColumn != null)
                {
                    //IDTSCustomProperty nullCol = icol.DTSInputColumn.CustomPropertyCollection[Resources.LookupErrorAggIsNullColumnName];
                    //bool isNullCol = (bool)nullCol.Value;
                    IDTSCustomProperty keyCol = icol.DTSInputColumn.CustomPropertyCollection[Resources.LookupErrorAggIsKeyColumnName];
                    bool isKeyCol = (bool)keyCol.Value;

                    if (!isKeyCol)
                    {
                        DesignTimeComponent.SetUsageType(input.ID, vInput, icol.LineageID, DTSUsageType.UT_IGNORED);
                        icol.DTSInputColumn = null;
                    }


                }
                validForInput = true;
            }

            return validForInput;
        }

        public void SelectLookupInputColumn(System.Windows.Forms.CheckedListBox clbInputColumns, System.Windows.Forms.ListBox lbSelectedItems, int checkedIndex, System.Windows.Forms.CheckState state, int nullColumnLineageId)
        {
            FormInputColumn icol = clbInputColumns.Items[checkedIndex] as FormInputColumn;

            IDTSInput input = ComponentMetadata.InputCollection[0];

            if (icol != null)
            {
                if (state == CheckState.Checked)
                {
                    DesignTimeComponent.SetUsageType(input.ID, VirtualInput, icol.LineageID, DTSUsageType.UT_READONLY);

                    IDTSInputColumn inputCol = input.InputColumnCollection.GetInputColumnByLineageID(icol.LineageID);

                    IDTSCustomProperty keyCol = inputCol.CustomPropertyCollection[Resources.LookupErrorAggIsKeyColumnName];
                    keyCol.Value = true;

                    icol.DTSInputColumn = inputCol;


                    if (lbSelectedItems != null && !lbSelectedItems.Items.Contains(icol))
                    {
                        int sortOrder = 0;
                        if (lbSelectedItems.Items.Count > 0)
                        {
                            FormInputColumn lastCol = lbSelectedItems.Items[lbSelectedItems.Items.Count - 1] as FormInputColumn;
                            sortOrder = lastCol.SortOrder;
                        }
                        icol.SortOrder = sortOrder + 1;

                        lbSelectedItems.Items.Add(icol);
                    }
                }
                else
                {
                    bool isInput = false;
                    if (icol.DTSInputColumn != null)
                    {
                        IDTSCustomProperty prop = icol.DTSInputColumn.CustomPropertyCollection[Resources.LookupErrorAggIsNullColumnName];
                        isInput = (bool)prop.Value;
                        IDTSCustomProperty keyCol = icol.DTSInputColumn.CustomPropertyCollection[Resources.LookupErrorAggIsKeyColumnName];
                        keyCol.Value = false;

                    }

                    if (!isInput)
                    {
                        DesignTimeComponent.SetUsageType(input.ID, VirtualInput, icol.LineageID, DTSUsageType.UT_IGNORED);
                        icol.DTSInputColumn = null;
                    }

                    if (lbSelectedItems != null && lbSelectedItems.Items.Contains(icol))
                        lbSelectedItems.Items.Remove(icol);
                }

            }
        }


        //Contains List of Columns for Null Checking
        private static List<ConvertMetadataInputColumn> _inputColumns = new List<ConvertMetadataInputColumn>();

        /// <summary>
        /// List of DT_I4 columns in the ErrorInput available for asignment as InputDataColumn
        /// </summary>
        public static List<ConvertMetadataInputColumn> InputColumns
        {
            get { return _inputColumns; }
        }


        public override void Initialize(IDTSComponentMetaData dtsComponentMetadata, IServiceProvider serviceProvider)
        {
            base.Initialize(dtsComponentMetadata, serviceProvider);
            _inputColumns = new List<ConvertMetadataInputColumn>();

            IDTSVirtualInput vInput = GetVirtualInput(0); //Get Virtual Input for the Lookup Error Input
            if (vInput != null)
            {

                foreach (IDTSVirtualInputColumn vcol in vInput.VirtualInputColumnCollection)
                {
                    ConvertMetadataInputColumn iCol = new ConvertMetadataInputColumn(vcol);
                    _inputColumns.Add(iCol);
                }
            }



            //Sort the columns by name
            _inputColumns.Sort((a, b) => a.Name.CompareTo(b.Name));

            //Check if the column names repeats and in that case set the DisplayName to include also UpstreamComponentname
            for (int i = 0; i < _inputColumns.Count; i++)
            {
                if (i >= 0 && i < _inputColumns.Count - 1 && _inputColumns[i].Name == _inputColumns[i + 1].Name)
                    _inputColumns[i].DisplayName = string.Format("{0}.{1}", _inputColumns[i].UpstreamComponentName, _inputColumns[i].Name);
                else if (i > 0 && i < _inputColumns.Count && _inputColumns[i].Name == _inputColumns[i - 1].Name)
                    _inputColumns[i].DisplayName = string.Format("{0}.{1}", _inputColumns[i].UpstreamComponentName, _inputColumns[i].Name);
            }

            //add the "Not Specified" column (LineageID = 0)
            _inputColumns.Insert(0, new ConvertMetadataInputColumn(null, -1, "<< Not Specified >>", string.Empty, "<< Not Specified >>"));

        }

    }
}
