// <copyright file="ColumnsToXmlTransformationForm.cs" company="Pavel Pawlowski">
// Copyright (c) 2014, 2015 All Right Reserved
//
// THIS CODE AND INFORMATION ARE PROVIDED "AS IS" WITHOUT WARRANTY OF ANY 
// KIND, EITHER EXPRESSED OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE
// IMPLIED WARRANTIES OF MERCHANTABILITY AND/OR FITNESS FOR A
// PARTICULAR PURPOSE.
//
// </copyright>
// <author>Pavel Pawlowski</author>
// <summary>Contains UI Form for ColumnsToXmlTransformation</summary>

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using Microsoft.SqlServer.Dts.Runtime;
using Microsoft.SqlServer.Dts.Pipeline.Wrapper;
using PP.SSIS.DataFlow.Properties;
using Microsoft.SqlServer.Dts.Runtime.Wrapper;
using System.Runtime.CompilerServices;
using System.Xml.Linq;
using PP.SSIS.DataFlow.UI.Common;
using PP.SSIS.DataFlow.Common;

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
    public partial class HistoryLookupTransformationForm : Form, IUIForm
    {
        private delegate void ObjectPropertiesChanged(HistoryLookupProperties prop);

        public enum DateFromComparison
        {
            /// <summary>
            /// Date value from Data Streem must be greater than DateFrom value
            /// </summary>
            Greater         = 0x01,
            /// <summary>
            /// Date value from Data Streem must be equal to or greater than DateFrom value
            /// </summary>
            GreaterOrEqual  = 0x02,
        }

        public enum DateToComparison
        {
            Lower           = 0x04,
            /// <summary>
            /// Date value from DataStream must be equal to or lower than DateTo value
            /// </summary>
            LowerOrEqual    = 0x08
        }


        [DefaultProperty("NoMatchBehavior")]
        private class HistoryLookupProperties
        {
            private IDTSCustomProperty _noMatchBehavior;
            private IDTSCustomProperty _caheType;
            private IDTSCustomProperty _noMatchCacheSize;
            private IDTSCustomProperty _defaultCacheSize;
            private IDTSCustomProperty _keyHashAlgorithm;
            

            public HistoryLookupProperties(IDTSCustomProperty noMatchBehavior, IDTSCustomProperty cacheType, IDTSCustomProperty noMatchCacheSize, IDTSCustomProperty defaultCacheSize, IDTSCustomProperty keyHashAlgorithm)
            {
                this._noMatchBehavior = noMatchBehavior;
                this._caheType = cacheType;
                this._noMatchCacheSize = noMatchCacheSize;
                this._defaultCacheSize = defaultCacheSize;
                this._keyHashAlgorithm = keyHashAlgorithm;
            }

            [Category("History Lookup Transformation")]
            [Description("Specifies how the component will handle lookup errors")]
            [DefaultValue(HistoryLookupTransformation.NoMatchBehavior.FailComponent)]
            public HistoryLookupTransformation.NoMatchBehavior NoMatchBehavior
            {
                get
                {
                    return (HistoryLookupTransformation.NoMatchBehavior)_noMatchBehavior.Value;
                }
                set
                {
                    _noMatchBehavior.Value = value;
                    NotifyObjectUpdated();
                }
            }

            [Category("History Lookup Transformation")]
            [Description("Specifies the CacheType. At the moment only Full is supported")]
            [DefaultValue(HistoryLookupTransformation.CacheType.Full)]
            public HistoryLookupTransformation.CacheType CacheType
            {
                get
                {
                    return (HistoryLookupTransformation.CacheType)_caheType.Value;
                }
                set
                {
                    _caheType.Value = value;
                    NotifyObjectUpdated();
                }
            }

            [Category("History Lookup Transformation")]
            [Description("Specifies the size of the NoMatch cache in number of records. This specifies how much lookup failures will be stored in the cache. Default value of 0 means the NoMatch cache is disabled.")]
            [DefaultValue(0)]
            public int NoMatchCacheSize
            {
                get
                {
                    return (int)_noMatchCacheSize.Value;
                }
                set
                {
                    _noMatchCacheSize.Value = value;
                    NotifyObjectUpdated();
                }
            }

            [Category("History Lookup Transformation")]
            [Description("Specifies thedefault size for Cache. This helps pre-allocate proper hash bbuckets for cache entries during cache loading. Ideal situaion when it is close to number of unique keys going into cache.")]
            [DefaultValue(10000)]
            public int DefaultCacheSize
            {
                get
                {
                    return (int)_defaultCacheSize.Value;
                }
                set
                {
                    _defaultCacheSize.Value = value;
                    NotifyObjectUpdated();
                }
            }

            [Category("History Lookup Transformation")]
            [Description("Specifies the Hash Algorithm to be used for hashing the lookup keys. A proper algorithm should be selected to ensure generated keys uniques. None = no hashing will be used and original key data will be stored in cache and used for lookups.")]
            [DefaultValue(HashAlgorithmType.None)]
            public HashAlgorithmType KeyHashAlgorithm
            {
                get
                {
                    return (HashAlgorithmType)_keyHashAlgorithm.Value;
                }
                set
                {
                    _keyHashAlgorithm.Value = value;
                    NotifyObjectUpdated();
                }
            }

            [Browsable(false)]
            public int DataDateColumnLineageID { get; set; }

            /// <summary>
            /// Gets or Sets the InputDataColumn by it's name (re-translates name to and from LineageID)
            /// </summary>
            private string _dataDateColumn = null;
            [Category("History Lookup Transformation - DateColumns")]
            [Description("Specifies the Date columns from the Data Input which will be used to lookup history data.")]
            [TypeConverter(typeof(DataDateColumnTypeConverter))]
            public string DataDateColumn
            {
                get
                {
                    if (_dataDateColumn == null)
                    {                        

                        var emic = HistoryLookupTransformationUI.InputDateColumns.Find(ic => ic.LineageID == DataDateColumnLineageID);
                        if (emic == null && DataDateColumnLineageID != 0)
                        {
                            DataDateColumnLineageID = 0;
                            emic = HistoryLookupTransformationUI.InputDateColumns.Find(ic => ic.LineageID == DataDateColumnLineageID);
                        }

                        if (emic != null)
                        {
                            _dataDateColumn = emic.DisplayName;
                        }
                    }
                    return _dataDateColumn;
                }
                set
                {
                    _dataDateColumn = value;
                    var emic = HistoryLookupTransformationUI.InputDateColumns.Find(ic => ic.DisplayName == _dataDateColumn);
                    if (emic != null)
                        DataDateColumnLineageID = emic.LineageID;
                    else
                    {
                        _dataDateColumn = null;
                        DataDateColumnLineageID = 0;
                    }
                    NotifyObjectUpdated();
                    //NotifyPropertyChanged("DataDateColumn");
                }
            }


            [Browsable(false)]
            public int LookupDateFromLineageID { get; set; }


            private DateFromComparison _lookupDateFromComparison = DateFromComparison.GreaterOrEqual;

            /// <summary>
            /// Gets or Sets the DateComparison for the LookupDateFromColumn
            /// </summary>
            [Category("History Lookup Transformation - DateColumns")]
            [Description("Specifies the Date Comparisonfor the LookupDateFromColumn.")]
            [DefaultValue(DateFromComparison.GreaterOrEqual)]
            public DateFromComparison LookupDateFromComparison
            {
                get { return _lookupDateFromComparison; }
                set
                {
                    _lookupDateFromComparison = value;
                    NotifyObjectUpdated();
                }
            }

            private DateToComparison _lookupDateToComparison = DateToComparison.Lower;

            /// <summary>
            /// Gets or Sets the DateComparison for the LookupDateToColumn
            /// </summary>
            [Category("History Lookup Transformation - DateColumns")]
            [Description("Specifies the Date Comparisonfor the LookupDateToColumn.")]
            [DefaultValue(DateToComparison.Lower)]
            public DateToComparison LookupDateToComparison
            {
                get { return _lookupDateToComparison; }
                set
                {
                    _lookupDateToComparison = value;
                    NotifyObjectUpdated();
                }
            }


            /// <summary>
            /// Gets or Sets the InputDataColumn by it's name (re-translates name to and from LineageID)
            /// </summary>
            private string _lookupDateFrom = null;
            [Category("History Lookup Transformation - DateColumns")]
            [Description("Specifies the Date columns from the History Lookup Input which will be used ValidFrom column.")]
            [TypeConverter(typeof(LookupDateColumnTypeConverter))]
            public string LookupDateFromColumn
            {
                get
                {
                    if (_lookupDateFrom == null)
                    {

                        var emic = HistoryLookupTransformationUI.LookupDateColumns.Find(ic => ic.LineageID == LookupDateFromLineageID);
                        if (emic == null && LookupDateFromLineageID != 0)
                        {
                            LookupDateFromLineageID = 0;
                            emic = HistoryLookupTransformationUI.LookupDateColumns.Find(ic => ic.LineageID == LookupDateFromLineageID);
                        }

                        if (emic != null)
                        {
                            _lookupDateFrom = emic.DisplayName;
                        }
                    }
                    return _lookupDateFrom;
                }
                set
                {
                    _lookupDateFrom = value;
                    var emic = HistoryLookupTransformationUI.LookupDateColumns.Find(ic => ic.DisplayName == _lookupDateFrom);
                    if (emic != null)
                        LookupDateFromLineageID = emic.LineageID;
                    else
                    {
                        _lookupDateFrom = null;
                        LookupDateFromLineageID = 0;
                    }
                    NotifyObjectUpdated();
                }
            }


            [Browsable(false)]
            public int LookupDateToLineageID { get; set; }

            /// <summary>
            /// Gets or Sets the InputDataColumn by it's name (re-translates name to and from LineageID)
            /// </summary>
            private string _lookupDateTo = null;
            [Category("History Lookup Transformation - DateColumns")]
            [Description("Specifies the Date columns from the History Lookup Input which will be used ValidTo column.")]
            [TypeConverter(typeof(LookupDateColumnTypeConverter))]
            public string LookupDateToColumn
            {
                get
                {
                    if (_lookupDateTo == null)
                    {

                        var emic = HistoryLookupTransformationUI.LookupDateColumns.Find(ic => ic.LineageID == LookupDateToLineageID);
                        if (emic == null && LookupDateToLineageID != 0)
                        {
                            LookupDateToLineageID = 0;
                            emic = HistoryLookupTransformationUI.LookupDateColumns.Find(ic => ic.LineageID == LookupDateToLineageID);
                        }

                        if (emic != null)
                        {
                            _lookupDateTo = emic.DisplayName;
                        }
                    }
                    return _lookupDateTo;
                }
                set
                {
                    _lookupDateTo = value;
                    var emic = HistoryLookupTransformationUI.LookupDateColumns.Find(ic => ic.DisplayName == _lookupDateTo);
                    if (emic != null)
                        LookupDateToLineageID = emic.LineageID;
                    else
                    {
                        _lookupDateTo = null;
                        LookupDateToLineageID = 0;
                    }

                    NotifyObjectUpdated();
                    //NotifyPropertyChanged("DataDateColumn");
                }
            }

            public event ObjectPropertiesChanged ObjectPropertiesChanged;

            private void NotifyObjectUpdated()
            {
                if (ObjectPropertiesChanged != null)
                    ObjectPropertiesChanged(this);
            }
        }

        /// <summary>
        /// Type Converter for the DataDateColumn (provides list of available InputColumns)
        /// </summary>
        public class DataDateColumnTypeConverter : StringConverter
        {
            public override bool GetStandardValuesExclusive(ITypeDescriptorContext context)
            {
                return true;
            }

            public override bool GetStandardValuesSupported(ITypeDescriptorContext context)
            {
                return true;
            }

            public override TypeConverter.StandardValuesCollection GetStandardValues(ITypeDescriptorContext context)
            {
                return new StandardValuesCollection(HistoryLookupTransformationUI.InputDateColumns.ConvertAll<string>(i => i.DisplayName));
            }
        }

        /// <summary>
        /// Type Converter for the LookupDateColumns (provides list of available InputColumns)
        /// </summary>
        public class LookupDateColumnTypeConverter : StringConverter
        {
            public override bool GetStandardValuesExclusive(ITypeDescriptorContext context)
            {
                return true;
            }

            public override bool GetStandardValuesSupported(ITypeDescriptorContext context)
            {
                return true;
            }

            public override TypeConverter.StandardValuesCollection GetStandardValues(ITypeDescriptorContext context)
            {
                return new StandardValuesCollection(HistoryLookupTransformationUI.LookupDateColumns.ConvertAll<string>(i => i.DisplayName));
            }
        }

        private class OutputColumnDetail
        {
            public OutputColumnDetail(IDTSVirtualInputColumn virtualInputColumn, string aliasName, int id)
            {
                VirtualInputColumn = virtualInputColumn;
                AliasName = aliasName;
                ID = id;
            }
            public IDTSVirtualInputColumn VirtualInputColumn { get; private set; }
            public int ID { get; private set; }
            public string AliasName { get; private set; }
            public int OutputLineageID { get; set; }
        }

        private class ColumnMappingDetail
        {
            public ColumnMappingDetail(int dataColumnLineageID, int lookupColumnLineageID)
            {
                DataColumnLineageID = dataColumnLineageID;
                LookupColumnLineageID = lookupColumnLineageID;
            }
            public int DataColumnLineageID { get; private set; }
            public int LookupColumnLineageID { get; private set; }
        }

        private IUIHelper uiHelper;
        private bool _formLoading = false;
        HistoryLookupProperties _historyLookupProperties;

        public IUIHelper UIHelper
        {
            get { return uiHelper; }
            private set { uiHelper = value; }
        }

        public HistoryLookupTransformationForm()
        {
            InitializeComponent();
        }

        public void InitializeUIForm(IUIHelper uiHelper)
        {
            this.UIHelper = uiHelper;
            this.Text = string.Format(Resources.EditorFor, UIHelper.ComponentMetadata.Name);
        }


        private void btnOk_Click(object sender, EventArgs e)
        {
            UpdateComponentData();

            this.Close();
        }

        private void UpdateComponentData()
        {
            //Set History Looup Properties
            UIHelper.ComponentMetadata.CustomPropertyCollection[HistoryLookupTransformation.GetPropertyname(HistoryLookupTransformation.CustomProperties.NoMatchBehavior)].Value = _historyLookupProperties.NoMatchBehavior;
            UIHelper.ComponentMetadata.CustomPropertyCollection[HistoryLookupTransformation.GetPropertyname(HistoryLookupTransformation.CustomProperties.CacheType)].Value = _historyLookupProperties.CacheType;
            UIHelper.ComponentMetadata.CustomPropertyCollection[HistoryLookupTransformation.GetPropertyname(HistoryLookupTransformation.CustomProperties.NoMatchCacheSize)].Value = _historyLookupProperties.NoMatchCacheSize;
            UIHelper.ComponentMetadata.CustomPropertyCollection[HistoryLookupTransformation.GetPropertyname(HistoryLookupTransformation.CustomProperties.DefaultCacheSize)].Value = _historyLookupProperties.DefaultCacheSize;
            UIHelper.ComponentMetadata.CustomPropertyCollection[HistoryLookupTransformation.GetPropertyname(HistoryLookupTransformation.CustomProperties.KeyHashAlgorithm)].Value = _historyLookupProperties.KeyHashAlgorithm;

            switch (_historyLookupProperties.NoMatchBehavior)
            {
                case HistoryLookupTransformation.NoMatchBehavior.FailComponent:
                    UIHelper.Input.ErrorRowDisposition = DTSRowDisposition.RD_FailComponent;
                    UIHelper.Input.TruncationRowDisposition = DTSRowDisposition.RD_FailComponent;
                    UIHelper.Input2.ErrorRowDisposition = DTSRowDisposition.RD_FailComponent;
                    UIHelper.Input2.TruncationRowDisposition = DTSRowDisposition.RD_FailComponent;
                    UIHelper.ComponentMetadata.UsesDispositions = false;
                    break;
                case HistoryLookupTransformation.NoMatchBehavior.RedirectToErrorOutput:
                    UIHelper.Input.ErrorRowDisposition = DTSRowDisposition.RD_RedirectRow;
                    UIHelper.Input.TruncationRowDisposition = DTSRowDisposition.RD_RedirectRow;
                    UIHelper.Input2.ErrorRowDisposition = DTSRowDisposition.RD_RedirectRow;
                    UIHelper.Input2.TruncationRowDisposition = DTSRowDisposition.RD_RedirectRow;
                    UIHelper.ComponentMetadata.UsesDispositions = true;
                    break;
                case HistoryLookupTransformation.NoMatchBehavior.RedirectToNoMatchOutput:
                    UIHelper.Input.ErrorRowDisposition = DTSRowDisposition.RD_NotUsed;
                    UIHelper.Input.TruncationRowDisposition = DTSRowDisposition.RD_NotUsed;
                    UIHelper.Input2.ErrorRowDisposition = DTSRowDisposition.RD_NotUsed;
                    UIHelper.Input2.TruncationRowDisposition = DTSRowDisposition.RD_NotUsed;
                    UIHelper.ComponentMetadata.UsesDispositions = false;
                    break;
                case HistoryLookupTransformation.NoMatchBehavior.IgnoreError:
                    UIHelper.Input.ErrorRowDisposition = DTSRowDisposition.RD_IgnoreFailure;
                    UIHelper.Input.TruncationRowDisposition = DTSRowDisposition.RD_IgnoreFailure;
                    UIHelper.Input2.ErrorRowDisposition = DTSRowDisposition.RD_IgnoreFailure;
                    UIHelper.Input2.TruncationRowDisposition = DTSRowDisposition.RD_IgnoreFailure;
                    UIHelper.ComponentMetadata.UsesDispositions = false;
                    break;
                default:
                    break;
            }

            //Lists to hold information about processed ooutput columns
            List<OutputColumnDetail> outputCols = new List<OutputColumnDetail>();
            List<OutputColumnDetail> processedOutputCols = new List<OutputColumnDetail>();
            List<int> oColsToRemove = new List<int>();

            if (UIHelper.Output != null)
            {
                //Get Output Columns Detailsfrom Output Columns ListView
                foreach (ListViewItem lvi in lvOutputColumns.Items)
                {
                    if (lvi.Checked)
                        outputCols.Add(new OutputColumnDetail((IDTSVirtualInputColumn)lvi.Tag, lvi.SubItems[1].Text, lvi.SubItems[1].Tag != null ? (int)lvi.SubItems[1].Tag : -1));
                }

                //Iterate through existing OutputColumns and try to update them
                foreach (IDTSOutputColumn oCol in UIHelper.Output.OutputColumnCollection)
                {
                    OutputColumnDetail od = outputCols.Find(ocd => ocd.ID == oCol.ID);
                    if (od != null)
                    {
                        outputCols.Remove(od);
                        processedOutputCols.Add(od);

                        oCol.Name = od.AliasName;
                        oCol.CustomPropertyCollection[HistoryLookupTransformation.GetPropertyname(HistoryLookupTransformation.CustomProperties.SourceLineageID)].Value = od.VirtualInputColumn.LineageID;
                        HistoryLookupTransformation.SetOutputColumnDataType(oCol, od.VirtualInputColumn);
                        od.OutputLineageID = oCol.LineageID;
                    }
                    else
                    {
                        oColsToRemove.Add(oCol.ID);
                    }
                }

                //Remove Not Existing Output Columns
                foreach (int id in oColsToRemove)
                {
                    UIHelper.Output.OutputColumnCollection.RemoveObjectByID(id);
                }

                //Add new OutputColumns
                foreach (OutputColumnDetail od in outputCols)
                {
                    processedOutputCols.Add(od);
                    IDTSOutputColumn oCol = UIHelper.Output.OutputColumnCollection.New();
                    oCol.Name = od.AliasName;
                    HistoryLookupTransformation.SetOutputColumnProperties(oCol);
                    HistoryLookupTransformation.SetOutputColumnDataType(oCol, od.VirtualInputColumn);
                    oCol.CustomPropertyCollection[HistoryLookupTransformation.GetPropertyname(HistoryLookupTransformation.CustomProperties.SourceLineageID)].Value = od.VirtualInputColumn.LineageID;
                    od.OutputLineageID = oCol.LineageID;
                }
            }


            List<ColumnMappingDetail> mappings = new List<ColumnMappingDetail>(lvMappings.Items.Count);
            //Process DataInput
            if (UIHelper.VirtualInput != null)
            {

                //Iterete through mappings and store them in List for easy searching
                foreach (ListViewItem lvi in lvMappings.Items)
                {
                    mappings.Add(new ColumnMappingDetail(((ConvertMetadataInputColumn)lvi.Tag).LineageID, ((ConvertMetadataInputColumn)lvi.SubItems[1].Tag).LineageID));
                }

                //Iterate tghrough virtual input
                foreach (IDTSVirtualInputColumn vCol in UIHelper.VirtualInput.VirtualInputColumnCollection)
                {
                    int lookupLineageID = 0;

                    HistoryLookupTransformation.InputColumnUsageType usageType = HistoryLookupTransformation.InputColumnUsageType.None;

                    //if column LineageID equalsto DataDateColumnLineageID in HistoryLookuProperties, then it was selected as Date Column
                    if (vCol.LineageID == _historyLookupProperties.DataDateColumnLineageID)
                        usageType |= HistoryLookupTransformation.InputColumnUsageType.DateColumn;

                    //Try to lookup defined mapping
                    ColumnMappingDetail cmd = mappings.Find(cm => cm.DataColumnLineageID == vCol.LineageID);

                    //If mapping was found then it is lookup column
                    if (cmd != null)
                    {
                        usageType |= HistoryLookupTransformation.InputColumnUsageType.LookupColumn;
                        lookupLineageID = cmd.LookupColumnLineageID;
                    }

                    //If usageType is None then ignore column
                    if (usageType == HistoryLookupTransformation.InputColumnUsageType.None)
                        UIHelper.DesignTimeComponent.SetUsageType(UIHelper.Input.ID, UIHelper.VirtualInput, vCol.LineageID, DTSUsageType.UT_IGNORED);
                    else
                    {
                        IDTSInput inp = UIHelper.ComponentMetadata.InputCollection[0];
                        IDTSInputColumn iCol = UIHelper.DesignTimeComponent.SetUsageType(UIHelper.Input.ID, UIHelper.VirtualInput, vCol.LineageID, DTSUsageType.UT_READONLY);

                        HistoryLookupTransformation.SetDataInputColumnProperties(iCol);

                        iCol.CustomPropertyCollection[HistoryLookupTransformation.GetPropertyname(HistoryLookupTransformation.CustomProperties.InputColumnUsageType)].Value = usageType;
                        iCol.CustomPropertyCollection[HistoryLookupTransformation.GetPropertyname(HistoryLookupTransformation.CustomProperties.LookupKeyLineageID)].Value = lookupLineageID;

                    }

                }

            }

            //Process HistoryLookupInput
            if (UIHelper.VirtualInput2 != null)
            {
                foreach (IDTSVirtualInputColumn vCol in UIHelper.VirtualInput2.VirtualInputColumnCollection)
                {
                    int oColLineageID = 0;
                    HistoryLookupTransformation.DateComparison dateComparison = HistoryLookupTransformation.DateComparison.None;
                    HistoryLookupTransformation.InputColumnUsageType usageType = HistoryLookupTransformation.InputColumnUsageType.None;
                    ColumnMappingDetail mapping = mappings.Find(m => m.LookupColumnLineageID == vCol.LineageID);

                    if (mapping != null)
                        usageType |= HistoryLookupTransformation.InputColumnUsageType.LookupColumn;

                    //If it matches LooupDateFromLineageID then it is LookupFromDate
                    if (vCol.LineageID == _historyLookupProperties.LookupDateFromLineageID)
                    {
                        usageType |= HistoryLookupTransformation.InputColumnUsageType.DateFromColumn;
                        dateComparison = (HistoryLookupTransformation.DateComparison)_historyLookupProperties.LookupDateFromComparison;
                    }

                    //If it matches LookupDateToLineageID then it is LookupToDate
                    if (vCol.LineageID == _historyLookupProperties.LookupDateToLineageID)
                    {
                        usageType |= HistoryLookupTransformation.InputColumnUsageType.DateToColumn;
                        dateComparison = (HistoryLookupTransformation.DateComparison)_historyLookupProperties.LookupDateToComparison;
                    }

                    var ocol = processedOutputCols.Find(oc => oc.VirtualInputColumn.LineageID == vCol.LineageID);
                    if (ocol != null)
                    {
                        usageType |= HistoryLookupTransformation.InputColumnUsageType.OutputColumn;
                        oColLineageID = ocol.OutputLineageID;
                    }

                    //Uf UsageType is None then Ignore the input column
                    if (usageType == HistoryLookupTransformation.InputColumnUsageType.None)
                    {
                        UIHelper.DesignTimeComponent.SetUsageType(UIHelper.Input2.ID, UIHelper.VirtualInput2, vCol.LineageID, DTSUsageType.UT_IGNORED);
                    }
                    else //Set the column as Input Column
                    {
                        IDTSInputColumn iCol = UIHelper.DesignTimeComponent.SetUsageType(UIHelper.Input2.ID, UIHelper.VirtualInput2, vCol.LineageID, DTSUsageType.UT_READONLY);

                        HistoryLookupTransformation.SetLookupInputColumnProperties(iCol);
                        iCol.CustomPropertyCollection[HistoryLookupTransformation.GetPropertyname(HistoryLookupTransformation.CustomProperties.InputColumnUsageType)].Value = usageType;
                        iCol.CustomPropertyCollection[HistoryLookupTransformation.GetPropertyname(HistoryLookupTransformation.CustomProperties.OutputColumnLineageID)].Value = oColLineageID;
                        iCol.CustomPropertyCollection[HistoryLookupTransformation.GetPropertyname(HistoryLookupTransformation.CustomProperties.DateComparison)].Value = dateComparison;

                    }
                }
            }

        }

        private void ColumnsToXmlTransformationForm_Load(object sender, EventArgs e)
        {
            //Get Current HistoryLookukp properties
            IDTSCustomProperty noMatchBehavior = UIHelper.ComponentMetadata.CustomPropertyCollection[HistoryLookupTransformation.GetPropertyname(HistoryLookupTransformation.CustomProperties.NoMatchBehavior)];
            IDTSCustomProperty cacheType = UIHelper.ComponentMetadata.CustomPropertyCollection[HistoryLookupTransformation.GetPropertyname(HistoryLookupTransformation.CustomProperties.CacheType)];
            IDTSCustomProperty noMatchCacheSize = UIHelper.ComponentMetadata.CustomPropertyCollection[HistoryLookupTransformation.GetPropertyname(HistoryLookupTransformation.CustomProperties.NoMatchCacheSize)];
            IDTSCustomProperty defaultCacheSize = UIHelper.ComponentMetadata.CustomPropertyCollection[HistoryLookupTransformation.GetPropertyname(HistoryLookupTransformation.CustomProperties.DefaultCacheSize)];
            IDTSCustomProperty keyhashAlgorithm = UIHelper.ComponentMetadata.CustomPropertyCollection[HistoryLookupTransformation.GetPropertyname(HistoryLookupTransformation.CustomProperties.KeyHashAlgorithm)];

            _historyLookupProperties = new HistoryLookupProperties(noMatchBehavior, cacheType, noMatchCacheSize, defaultCacheSize, keyhashAlgorithm);

            try
            {
                _formLoading = true;
                //Load Input columns and populate mappings ListView
                LoadInputColumns();

                //Load Lookup and Output Columsn and populate Output Columns ListView
                LoadOutputColumns();
            }
            finally
            {
                _formLoading = false;
            }

            //assign handler to detect properties changes to validate status
            _historyLookupProperties.ObjectPropertiesChanged += Properties_ObjectPropertiesChanged;

            //Check current prperties
            Properties_ObjectPropertiesChanged(_historyLookupProperties);

            //Assing current proerties to PropetyGrid
            propComponent.SelectedObject = _historyLookupProperties;
        }

        private StringBuilder propertiesErrors = new StringBuilder();
        private void Properties_ObjectPropertiesChanged(HistoryLookupProperties prop)
        {
            propertiesErrors = new StringBuilder();

            List<DataType> dataTypes = new List<DataType>();

            var dataDateCol = HistoryLookupTransformationUI.InputDateColumns.Find(ic => ic.LineageID == prop.DataDateColumnLineageID);
            if (dataDateCol != null && !dataTypes.Contains(dataDateCol.DataType))
                dataTypes.Add(dataDateCol.DataType);
            var lookupDateFromCol = HistoryLookupTransformationUI.LookupDateColumns.Find(ic => ic.LineageID == prop.LookupDateFromLineageID);
            if (lookupDateFromCol != null && !dataTypes.Contains(lookupDateFromCol.DataType))
                dataTypes.Add(lookupDateFromCol.DataType);
            var lookupDateToCol = HistoryLookupTransformationUI.LookupDateColumns.Find(ic => ic.LineageID == prop.LookupDateToLineageID);
            if (lookupDateToCol != null && !dataTypes.Contains(lookupDateToCol.DataType))
                dataTypes.Add(lookupDateToCol.DataType);

            if (dataTypes.Count > 1)
                propertiesErrors.AppendLine("All DateColumns must be of the same DataType");

            if (prop.DefaultCacheSize < 1)
                propertiesErrors.Append("DefaultCacheSize mut be greater than 0");

            if (prop.NoMatchCacheSize < 0)
                propertiesErrors.Append("NoMatchCacheSize must be greater or equal to 0");

            ValidateForErrors();
        }

        private void ValidateForErrors()
        {
            StringBuilder errors = new StringBuilder(propertiesErrors.ToString());
            if (errors.Length > 0)
                errors.AppendLine();

            List<string> outputNames = new List<string>();
            foreach (ListViewItem lvi in lvOutputColumns.Items)
            {
                if (lvi.Checked)
                {
                    if (outputNames.Contains(lvi.Text))
                        errors.AppendLine("Multiple Output Columns have the same Alias.");
                    else
                        outputNames.Add(lvi.Text);
                }
            }

            outputNames = null;

            if (lvMappings.Items.Count == 0)
                errors.AppendLine("Nocolumn Mapping is defined");

            lblError.Text = errors.ToString();
            errorProvider.SetError(pnlError, errors.ToString());
            btnOk.Enabled = lblError.Text == string.Empty;
        }

        private void LoadInputColumns()
        {
            try
            {
                lvMappings.SuspendLayout();
                lvMappings.Items.Clear();
                int missingCount = 0;

                if (UIHelper.Input != null && UIHelper.VirtualInput != null && UIHelper.Input2 != null && UIHelper.VirtualInput2 != null)
                {

                    //Generate ColumnsMappings basedon InputColumns
                    foreach (IDTSInputColumn col in UIHelper.Input.InputColumnCollection)
                    {
                        IDTSCustomProperty uTypeProp = col.CustomPropertyCollection[HistoryLookupTransformation.GetPropertyname(HistoryLookupTransformation.CustomProperties.InputColumnUsageType)];
                        HistoryLookupTransformation.InputColumnUsageType usageType = (HistoryLookupTransformation.InputColumnUsageType)uTypeProp.Value;

                        IDTSCustomProperty lookLinIDProp = col.CustomPropertyCollection[HistoryLookupTransformation.GetPropertyname(HistoryLookupTransformation.CustomProperties.LookupKeyLineageID)];
                        int lookupLineageID = (int)lookLinIDProp.Value;

                        //if UsageType is LookupColumn, gegerate mapping in the MappingView
                        if ((usageType & HistoryLookupTransformation.InputColumnUsageType.LookupColumn) == HistoryLookupTransformation.InputColumnUsageType.LookupColumn && lookupLineageID > 0)
                        {
                            ConvertMetadataInputColumn inputDataCol = HistoryLookupTransformationUI.InputColumns.Find(ic => ic.VirtualColumn != null && ic.LineageID != 0 && ic.VirtualColumn.LineageID == col.LineageID);
                            ConvertMetadataInputColumn lookupCol = HistoryLookupTransformationUI.LookupColumns.Find(ic => ic.VirtualColumn != null && ic.LineageID != 0 && ic.VirtualColumn.LineageID == lookupLineageID);

                            if (inputDataCol != null && lookupCol != null && inputDataCol.DataType == lookupCol.DataType)
                            {
                                var li = lvMappings.Items.Add(inputDataCol.DisplayName);
                                li.Tag = inputDataCol;
                                var si = li.SubItems.Add(lookupCol.DisplayName);
                                si.Tag = lookupCol;
                            }
                            else
                                missingCount++;
                        }

                        //If UsageType is DateColumn, then assing the LineageID to the _historyLookupProperties for editing
                        if((usageType & HistoryLookupTransformation.InputColumnUsageType.DateColumn) == HistoryLookupTransformation.InputColumnUsageType.DateColumn)
                        {
                            _historyLookupProperties.DataDateColumnLineageID = col.LineageID;
                        }
                    }

                    foreach (IDTSInputColumn col in UIHelper.Input2.InputColumnCollection)
                    {
                        IDTSCustomProperty uTypeProp = col.CustomPropertyCollection[HistoryLookupTransformation.GetPropertyname(HistoryLookupTransformation.CustomProperties.InputColumnUsageType)];
                        HistoryLookupTransformation.InputColumnUsageType usageType = (HistoryLookupTransformation.InputColumnUsageType)uTypeProp.Value;
                        IDTSCustomProperty compProp = col.CustomPropertyCollection[HistoryLookupTransformation.GetPropertyname(HistoryLookupTransformation.CustomProperties.DateComparison)];
                        HistoryLookupTransformation.DateComparison dateComparison = (HistoryLookupTransformation.DateComparison)compProp.Value;

                        //If UsageType is DateFromColumn, then assing the LineageID and DateComparison to the _historyLookupProperties for editing
                        if ((usageType & HistoryLookupTransformation.InputColumnUsageType.DateFromColumn) == HistoryLookupTransformation.InputColumnUsageType.DateFromColumn)
                        {
                            _historyLookupProperties.LookupDateFromLineageID = col.LineageID;
                            _historyLookupProperties.LookupDateFromComparison = dateComparison == HistoryLookupTransformation.DateComparison.None ? DateFromComparison.GreaterOrEqual : (DateFromComparison)dateComparison;
                        }

                        //If UsageType is DateToColumn, then assing the LineageID and DateComparison to the _historyLookupProperties for editing
                        if ((usageType & HistoryLookupTransformation.InputColumnUsageType.DateToColumn) == HistoryLookupTransformation.InputColumnUsageType.DateToColumn)
                        {
                            _historyLookupProperties.LookupDateToLineageID = col.LineageID;
                            _historyLookupProperties.LookupDateToComparison = dateComparison == HistoryLookupTransformation.DateComparison.None ? DateToComparison.Lower : (DateToComparison)dateComparison;
                        }
                    }

                }
                if (missingCount > 0)
                    MessageBox.Show("There are invalid mappings between columns. Invalid Mappings are removed", "History Lookup Transformation", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
            }
            finally
            {
                lvMappings.ResumeLayout();
                btnRemoveMapping.Enabled = lvMappings.SelectedIndices.Count > 0;
            }
        }

        private void LoadOutputColumns()
        {
            try
            {
                lvOutputColumns.SuspendLayout();
                lvOutputColumns.Items.Clear();

                if (UIHelper.Input2 != null && UIHelper.VirtualInput2 != null && UIHelper.Output != null)
                {
                    //Iterate through the VirualInputColumns
                    foreach (IDTSVirtualInputColumn vCol in UIHelper.VirtualInput2.VirtualInputColumnCollection)
                    {
                        var item = lvOutputColumns.Items.Add(vCol.Name);
                        item.Tag = vCol; //Store VirtualInputColumn in the Tag
                        var liOutput = item.SubItems.Add(string.Empty);
                        item.Checked = false;


                        foreach (IDTSOutputColumn oCol in UIHelper.Output.OutputColumnCollection)
                        {
                            IDTSCustomProperty prop = oCol.CustomPropertyCollection[HistoryLookupTransformation.GetPropertyname(HistoryLookupTransformation.CustomProperties.SourceLineageID)];
                            int sourceLineageID = (int)prop.Value;
                            if (vCol.LineageID == sourceLineageID)
                            {
                                item.Checked = true;
                                liOutput.Text = oCol.Name;
                                liOutput.Tag = oCol.ID; //StoreOutputColumn ID into the First SubItem Tag
                                break;
                            }
                        }
                    }

                }

            }
            finally
            {
                lvOutputColumns.ResumeLayout();
            }

        }

        void o_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            //if (e.PropertyName == "Name")
            //{
            //    UIHelper.UpdateTreeViewNodeName(sender, trvOutputColumns);
            //}
        }

        private void lvOutputColumns_SubItemClicked(object sender, SubItemEventArgs e)
        {
            if (_formLoading) return;
            if (e.SubItem == 1)
            {
                if (e.Item.Checked && e.Item.SubItems.Count > 0)
                    lvOutputColumns.StartEditing(txtOutputAlias, e.Item, e.SubItem);
            }
        }

        private void lvOutputColumns_SubItemEndEditing(object sender, SubItemEndEditingEventArgs e)
        {

        }

        private void lvOutputColumns_ItemChecked(object sender, ItemCheckedEventArgs e)
        {
            if (_formLoading) return;
            if (e.Item.Checked)
            {
                if (e.Item.SubItems.Count < 2)
                    e.Item.SubItems.Add(e.Item.Text);
                else
                    e.Item.SubItems[1].Text = e.Item.Text;
            }
            else
            {
                e.Item.SubItems[1].Text = string.Empty;
            }
        }

        private void lvOutputColumns_ColumnClick(object sender, ColumnClickEventArgs e)
        {
            if (e.Column == 0)
            {
                int newSort = (int)lvOutputColumns.Sorting;
                newSort++;
                if (newSort > 2)
                    newSort = 1;
                lvOutputColumns.Sorting = (SortOrder)newSort;
            }
        }

        private void lvMappings_SelectedIndexChanged(object sender, EventArgs e)
        {
            btnRemoveMapping.Enabled = lvMappings.SelectedIndices.Count > 0;
        }

        private void btnAddMapping_Click(object sender, EventArgs e)
        {
            if (HistoryLookupTransformationUI.InputColumns.Count <= 1 || HistoryLookupTransformationUI.LookupColumns.Count <= 1)
            {
                MessageBox.Show("No Input Columns Available. Check that Inputs are attached", "History Lookup Column Mapping", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
            }
            else
            {
                //Get already mapped columns from both data and lookup
                List<ConvertMetadataInputColumn> existingDataColumns = new List<ConvertMetadataInputColumn>(HistoryLookupTransformationUI.InputColumns.Count);
                List<ConvertMetadataInputColumn> existingLookupColumns = new List<ConvertMetadataInputColumn>(HistoryLookupTransformationUI.LookupColumns.Count);
                foreach (ListViewItem lvi in lvMappings.Items)
                {
                    ConvertMetadataInputColumn src = lvi.Tag as ConvertMetadataInputColumn;
                    ConvertMetadataInputColumn lkp = lvi.SubItems[1].Tag as ConvertMetadataInputColumn;
                    if (!existingDataColumns.Contains(src))
                        existingDataColumns.Add(src);
                    if (!existingLookupColumns.Contains(lkp))
                        existingLookupColumns.Add(lkp);
                }

                //Get available coolumns by removing already mapped
                List<ConvertMetadataInputColumn> availableSource = HistoryLookupTransformationUI.InputColumns.FindAll(ic => !existingDataColumns.Contains(ic) && ic.LineageID != 0);
                List<ConvertMetadataInputColumn> availableLookup = HistoryLookupTransformationUI.LookupColumns.FindAll(ic => !existingLookupColumns.Contains(ic) && ic.LineageID != 0);

                //Show Mapping Form
                AddHistoryLookupMapping ahm = new AddHistoryLookupMapping(availableSource, availableLookup);

                //If mappingform is confirmed, generate new mapping
                if (ahm.ShowDialog(this) == DialogResult.OK)
                {
                    ConvertMetadataInputColumn src = ahm.SelectedInputColumn;
                    ConvertMetadataInputColumn lkp = ahm.SelectedLookupColumn;
                    if (src != null && lkp != null)
                    {
                        try
                        {
                            lvMappings.SuspendLayout();
                            var lviSrc = lvMappings.Items.Add(src.DisplayName);
                            lviSrc.Tag = src;
                            var lviLkp = lviSrc.SubItems.Add(lkp.DisplayName);
                            lviLkp.Tag = lkp;
                        }
                        finally
                        {
                            lvMappings.ResumeLayout();
                        }
                    }

                    ValidateForErrors();
                }
            }
        }

        private void btnRemoveMapping_Click(object sender, EventArgs e)
        {
            if (lvMappings.SelectedItems.Count > 0)
            {
                try
                {
                    lvMappings.SuspendLayout();
                    List<ListViewItem> selected = new List<ListViewItem>(lvMappings.SelectedItems.Count);
                    foreach (ListViewItem lvi in lvMappings.SelectedItems)
                    {
                        selected.Add(lvi);
                    }

                    foreach (ListViewItem lvi in selected)
                    {
                        lvMappings.Items.Remove(lvi);
                    }
                }
                finally
                {
                    lvMappings.ResumeLayout();
                }
            }
        }
    }
}
