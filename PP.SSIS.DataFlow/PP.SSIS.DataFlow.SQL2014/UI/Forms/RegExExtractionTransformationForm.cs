// <copyright file="HashColumnsTransformationForm.cs" company="Pavel Pawlowski">
// Copyright (c) 2014, 2015 All Right Reserved
//
// THIS CODE AND INFORMATION ARE PROVIDED "AS IS" WITHOUT WARRANTY OF ANY 
// KIND, EITHER EXPRESSED OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE
// IMPLIED WARRANTIES OF MERCHANTABILITY AND/OR FITNESS FOR A
// PARTICULAR PURPOSE.
//
// </copyright>
// <author>Pavel Pawlowski</author>
// <summary>Contains UI Form for HasColumnsTransformation</summary>

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Windows.Forms;
using Microsoft.SqlServer.Dts.Pipeline.Wrapper;
using PP.SSIS.DataFlow.Properties;
using Microsoft.SqlServer.Dts.Runtime.Wrapper;
using System.Text.RegularExpressions;
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
    public partial class RegExExtractionTransformationForm : Form, IUIForm
    {
        /// <summary>
        /// Represetns ErrorMetadataColumn for displaying in the Properties Grid
        /// </summary>
        [DefaultProperty("InputDataColumn")]
        private class MetadataColumn : FormOutputColumn
        {
            public MetadataColumn(IDTSOutputColumn col) : base(col)
            {
            }
            
            IDTSCustomProperty inputDataColumnProperty;
            IDTSCustomProperty regularExpression;
            IDTSCustomProperty groupCaptureNumber;
            IDTSCustomProperty matchNumber;
            IDTSCustomProperty regexOutputType;
            IDTSCustomProperty regexOutput;
            IDTSCustomProperty regexOptions;

            /// <summary>
            /// Procerss the associated IDTSOutputColumn and retrieves the MetadataColumn specific Custom Properties
            /// </summary>
            protected override void DTSOutputColumnSetProcessing()
            {
                base.DTSOutputColumnSetProcessing();

                if (DTSOutputColumn != null)
                {
                    inputDataColumnProperty = DTSOutputColumn.CustomPropertyCollection[Resources.RegExExtractionInputColumnLinageIdPropertyName];
                    regularExpression = DTSOutputColumn.CustomPropertyCollection[Resources.RegExExtractionRegularExpressionName];
                    groupCaptureNumber = DTSOutputColumn.CustomPropertyCollection[Resources.RegExExtractionGroupCaptureNumberPropertyName];
                    matchNumber = DTSOutputColumn.CustomPropertyCollection[Resources.RegExExtractionMatchNumberPropertyName];
                    regexOutput = DTSOutputColumn.CustomPropertyCollection[Resources.RegExExtractionRegexOutput];
                    regexOutputType = DTSOutputColumn.CustomPropertyCollection[Resources.RegExExtractionRegexOutputType];
                    regexOptions = DTSOutputColumn.CustomPropertyCollection[Resources.RegExExtractionRegexOptions];
                }
                else
                {
                    inputDataColumnProperty = null;
                    regularExpression = null;
                    matchNumber = null;
                    groupCaptureNumber = null;
                    regexOutput = null;
                    regexOutputType = null;
                    regexOptions = null;
                }

                UpdateReadOnlyProperties();
            }

            public override string Name
            {
                get
                {
                    return base.Name;
                }

                set
                {
                    base.Name = value;
                }
            }

            public override string Description
            {
                get
                {
                    return base.Description;
                }

                set
                {
                    base.Description = value;
                }
            }

            [Browsable(false)]
            public int InputDataColumnLineageID
            {
                get
                {
                    return inputDataColumnProperty != null ? (int)inputDataColumnProperty.Value : 0;
                }
            }

            /// <summary>
            /// Gets or Sets the InputDataColumn by it's name (re-translates name to and from LineageID)
            /// </summary>
            private string _inputDataColumn = null;
            [Category("RegEx Extraction")]
            [Description("Specifies column containing input data for selected RegEx OutputColumn.")]
            [RefreshProperties(RefreshProperties.All)]
            [TypeConverter(typeof(InputColumnTypeConverter))]
            [DefaultValue("<< Not Specified >>")]
            public string InputDataColumn
            {
                get
                {
                    if (_inputDataColumn == null)
                    {
                        int lineageId = inputDataColumnProperty != null ? (int)inputDataColumnProperty.Value : 0;

                        var emic = RegExExtractionTransformationUI.InputColumns.Find(ic => ic.LineageID == lineageId);
                        if (emic != null)
                        {
                            _inputDataColumn = emic.DisplayName;
                        }
                        else
                        {
                            if (lineageId != 0 && inputDataColumnProperty != null)
                                inputDataColumnProperty.Value = 0;                                
                        }
                    }
                    return _inputDataColumn;
                }
                set
                {
                    if (inputDataColumnProperty != null)
                    {
                        _inputDataColumn = value;
                        var emic = RegExExtractionTransformationUI.InputColumns.Find(ic => ic.DisplayName == _inputDataColumn);

                        int linID = emic != null ? emic.LineageID : 0;


                        inputDataColumnProperty.Value = linID;

                        if (UIHelper != null)
                            RegExExtractionTransformation.SetOutputColumnDataType(DTSOutputColumn, UIHelper.VirtualInput, linID);

                        UpdateReadOnlyProperties();
                        NotifyPropertyChanged("InputDataColumn");
                    }
                }
            }

            /// <summary>
            /// Gets or Sets whether to include UpstreamComponent Name as ColumnName
            /// </summary>
            [Category("RegEx Extraction")]
            [Description("Specifies Regular Expression to be applied on the input column.")]
            [DefaultValue("")]
            public string RegularExpression
            {
                get
                {
                    return regularExpression != null ? (string)regularExpression.Value : string.Empty;
                }
                set
                {
                    if (regularExpression != null)
                    {
                        Regex r = new Regex(value);
                        regularExpression.Value = value;
                        NotifyPropertyChanged("RegularExpression");
                    }
                }
            }

            [Category("RegEx Extraction")]
            [Description("Specifies type of output to produce by the component.")]
            [RefreshProperties(RefreshProperties.All)]
            [DefaultValue(RegExExtractionTransformation.RegexOutputType.Result)]
            public RegExExtractionTransformation.RegexOutputType RegexOutputType
            {
                get
                {
                    if (regexOutputType != null)
                        return (RegExExtractionTransformation.RegexOutputType)regexOutputType.Value;
                    else
                        return RegExExtractionTransformation.RegexOutputType.Result;
                }
                set
                {
                    if (regexOutputType != null)
                    {
                        regexOutputType.Value = value;

                        if (value == RegExExtractionTransformation.RegexOutputType.CaptureGroupNumber)
                        {
                            int groupNo;
                            if (!int.TryParse(RegexOutput, out groupNo))
                            {
                                RegexOutput = "0";
                            }
                        }

                        UpdateReadOnlyProperties();
                        NotifyPropertyChanged("RegexOutputType");
                    }
                }
            }

            [Category("RegEx Extraction")]
            [Description("Rgex output defitions. Group Name or Number or Result specification.")]
            public string RegexOutput
            {
                get
                {
                    if (regexOutput != null)
                        return regexOutput.Value.ToString();
                    else
                        return string.Empty;
                }
                set
                {
                    if (regexOutput != null)
                    {
                        if (RegexOutputType == RegExExtractionTransformation.RegexOutputType.CaptureGroupNumber)
                        {
                            int groupNo;
                            if (!int.TryParse(value, out groupNo))
                            {
                                throw new Exception(string.Format("RegexOutput has to be an integer number when RegExOutputType is CaptureGroupNumber. Error parsing '{0}'", value));
                            }

                        }
                        regexOutput.Value = value;
                        NotifyPropertyChanged("RegexOutput");
                    }
                }
            }

            /// <summary>
            /// Gets or Sets the name of the Regular Expression Group to return as the output column.
            /// </summary>
            [Category("RegEx Extraction")]
            [Description("Specifies the Capture of the CaptureGroup to return in there are more captures of that group. Default 0 means last capture.")]
            [DefaultValue(0)]
            [ReadOnly(true)]
            public int GroupCaptureNumber
            {
                get
                {
                    return groupCaptureNumber != null ? (int)groupCaptureNumber.Value : 0;
                }
                set
                {
                    if (value < 0)
                        throw new System.ArgumentException(Resources.ErrorGrouCaptureNumberLowerThanZero);

                    if (groupCaptureNumber != null)
                    {
                        groupCaptureNumber.Value = value;
                        NotifyPropertyChanged("GroupCaptureNumber");
                    }
                }
            }

            [Category("RegEx Extraction")]
            [Description("Rgex output defitions. Group Name or Number or Result specification.")]
            [ReadOnly(false)]
            [DefaultValue(RegexOptions.Compiled)]
            [Editor(typeof(FlagEnumUIEditor), typeof(System.Drawing.Design.UITypeEditor))]
            public RegexOptions RegexOptions
            {
                get
                {
                    return regexOptions != null ? (RegexOptions)regexOptions.Value : RegexOptions.None;
                }
                set
                {
                    if (regexOptions != null)
                    {
                        regexOptions.Value = value;

                        NotifyPropertyChanged("RegexOptions");
                    }

                }

            }


            /// <summary>
            /// Gets or Sets the Group Number of the Regular Expression to return as output column.
            /// </summary>
            [Category("RegEx Extraction")]
            [Description("Specifies the Match Number of the Regular Expression to return as output column.")]
            [DefaultValue(1)]
            public int MatchNumber
            {
                get
                {
                    return matchNumber != null ? (int)matchNumber.Value : 1;
                }
                set
                {
                    if (matchNumber != null)
                    {
                        if (value <= 0)
                            throw new System.ArgumentException(Resources.ErrorMatchNumberLowerThanOne);

                        matchNumber.Value = value;
                        NotifyPropertyChanged("MatchNumber");
                    }
                }
            }

            [Category("Output Column Data Type")]
            [Description("The data type of selected output column")]
            [RefreshProperties(RefreshProperties.All)]
            [ReadOnly(false)]
            new public RegExExtractionTransformation.OutDataType DataType
            {
                get
                {
                    return (RegExExtractionTransformation.OutDataType)((int)base.DataType);
                }
                set
                {
                    DataType dt = (DataType)((int)value);
                    if (dt != DTSOutputColumn.DataType)
                    {
                        int codePage = 0;
                        if (dt == Microsoft.SqlServer.Dts.Runtime.Wrapper.DataType.DT_STR || dt == Microsoft.SqlServer.Dts.Runtime.Wrapper.DataType.DT_TEXT)
                        {
                            codePage = 1252;
                            //Get default code page for current locale
                            if (InputColumnsUIEditor.UIHelper != null)
                            {
                                var locale = InputColumnsUIEditor.UIHelper.ComponentMetadata.LocaleID;
                                var ci = System.Globalization.CultureInfo.GetCultureInfo(locale);
                                codePage = ci.TextInfo.ANSICodePage;
                            }
                        }

                        int dataLen = 0;
                        if (dt == Microsoft.SqlServer.Dts.Runtime.Wrapper.DataType.DT_STR || dt == Microsoft.SqlServer.Dts.Runtime.Wrapper.DataType.DT_WSTR)
                        {
                            dataLen = DataLength;
                            int lineageId = inputDataColumnProperty != null ? (int)inputDataColumnProperty.Value : 0;

                            if (lineageId > 0)
                            {
                                ConvertMetadataInputColumn emic = RegExExtractionTransformationUI.InputColumns.Find(ic => ic.LineageID == lineageId);
                                if (emic != null)
                                    dataLen = emic.DataLength;
                            }

                            if (dataLen == 0)
                                dataLen = 50;
                        }

                        DTSOutputColumn.SetDataTypeProperties(dt, dataLen, 0, 0, codePage);
                        UpdateReadOnlyProperties();
                        NotifyPropertyChanged("DataType");
                    }
                }
            }

            [ReadOnly(false)]
            public override int DataLength
            {
                get
                {
                    return base.DataLength;
                }

                set
                {
                    if (DTSOutputColumn != null && DataType == RegExExtractionTransformation.OutDataType.DT_STR || DataType == RegExExtractionTransformation.OutDataType.DT_WSTR)
                    {
                        DTSOutputColumn.SetDataTypeProperties(base.DataType, value, base.Precision, base.Scale, base.CodePage);
                        NotifyPropertyChanged("DataLength");
                    }
                }
            }

            [ReadOnly(true)]
            public override int CodePage
            {
                get
                {
                    return base.CodePage;
                }

                set
                {
                    if (DTSOutputColumn != null && DataType == RegExExtractionTransformation.OutDataType.DT_STR || DataType == RegExExtractionTransformation.OutDataType.DT_TEXT)
                    {
                        DTSOutputColumn.SetDataTypeProperties(base.DataType, base.DataLength, base.Precision, base.Scale, value);
                        NotifyPropertyChanged("CodePage");
                    }
                }
            }

            [Browsable(false)]
            public override int Precision
            {
                get
                {
                    return base.Precision;
                }

                set
                {
                    base.Precision = value;
                }
            }

            [Browsable(false)]
            public override int Scale
            {
                get
                {
                    return base.Scale;
                }

                set
                {
                    base.Scale = value;
                }
            }

            /// <summary>
            /// Creates new Instance of the MetadataColumn
            /// </summary>
            /// <param name="col">IDTSOutputColumn to be associated wi the MetadataColumn</param>
            /// <returns>MetadataColumn</returns>
            public static MetadataColumn CreateInstance(IDTSOutputColumn col)
            {
                return new MetadataColumn(col);
            }

            /// <summary>
            /// Creates a new instance of the IDTSOutputColumn enccapsualted in the MetadataSolumn
            /// </summary>
            /// <param name="output">Output where the new OutputColumn should be created</param>
            /// <returns></returns>
            public static MetadataColumn CreateNewInstance(IDTSOutput output)
            {
                IDTSOutputColumn col = output.OutputColumnCollection.New();
                col.Name = Resources.RegExExpressionDefaultOutputColumnName + col.ID.ToString();
                
                RegExExtractionTransformation.SetOutputColumnProperties(col, RegExExtractionTransformation.SetPropertyType.All, null, null);

                MetadataColumn c = new MetadataColumn(col);
                return c;
            }

            private void UpdateReadOnlyProperties()
            {
                UpdateReadOnlyProperty("GroupCaptureNumber", RegexOutputType == RegExExtractionTransformation.RegexOutputType.Result);
                UpdateReadOnlyProperty("CodePage", !(base.DataType == Microsoft.SqlServer.Dts.Runtime.Wrapper.DataType.DT_STR || base.DataType == Microsoft.SqlServer.Dts.Runtime.Wrapper.DataType.DT_TEXT));
                UpdateReadOnlyProperty("DataLength", !(base.DataType == Microsoft.SqlServer.Dts.Runtime.Wrapper.DataType.DT_STR || base.DataType == Microsoft.SqlServer.Dts.Runtime.Wrapper.DataType.DT_WSTR));
            }
        }

        /// <summary>
        /// Type Converter for the InputDataColumn (provides list of available InputColumns)
        /// </summary>
        public class InputColumnTypeConverter : StringConverter
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
                return new StandardValuesCollection(RegExExtractionTransformationUI.InputColumns.ConvertAll<string>(i => i.DisplayName));
            }
        }


        private IUIHelper uiHelper;
        protected IUIHelper UIHelper
        {
            get { return uiHelper; }
            private set { uiHelper = value; }
        }


        public RegExExtractionTransformationForm()
        {
            InitializeComponent();
        }

        public void InitializeUIForm(IUIHelper uiHelper)
        {
            this.uiHelper = uiHelper;
            this.Text = string.Format(Resources.EditorFor, UIHelper.ComponentMetadata.Name);
        }


        private void btnOk_Click(object sender, EventArgs e)
        {
            List<int> inputLineages = new List<int>();
            foreach (TreeNode outputNode in trvOutputColumns.Nodes)
            {
                foreach (TreeNode node in outputNode.Nodes)
                {
                    MetadataColumn col = node.Tag as MetadataColumn;
                    if (col != null && col.InputDataColumnLineageID > 0 && !inputLineages.Contains(col.InputDataColumnLineageID))
                        inputLineages.Add(col.InputDataColumnLineageID);
                }
            }

            foreach (IDTSVirtualInputColumn iCol in UIHelper.VirtualInput.VirtualInputColumnCollection)
            {
                DTSUsageType usageType = inputLineages.Contains(iCol.LineageID) ? DTSUsageType.UT_READONLY : DTSUsageType.UT_IGNORED;
                UIHelper.DesignTimeComponent.SetUsageType(UIHelper.Input.ID, UIHelper.VirtualInput, iCol.LineageID, usageType);
            }

            this.Close();
        }

        private void HashColumnsTransformationForm_Load(object sender, EventArgs e)
        {
            UIHelper.FormInitialize(null, null, trvOutputColumns, MetadataColumn.CreateInstance, o_PropertyChanged);
        }

        void o_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {                        
            if (e.PropertyName == "Name")
            {                
                UIHelper.UpdateTreeViewNodeName(sender, trvOutputColumns);
            }
            MetadataColumn col = sender as MetadataColumn;
            if (col != null && col.AssociatedTreeNode != null)
            {
                if (col.RegularExpression == string.Empty || col.RegexOutput == string.Empty || col.InputDataColumnLineageID <= 0)
                    col.AssociatedTreeNode.StateImageIndex = 1;
                else
                    col.AssociatedTreeNode.StateImageIndex = 0;



            }
        }

        private void trvOutpuColumns_AfterSelect(object sender, TreeViewEventArgs e)
        {
            //e.Node.Tag
            if (e.Node != null)
                propOutputColumn.SelectedObject = e.Node.Tag;
            else
                propOutputColumn.SelectedObject = null;

            btnRemoveColumn.Enabled = e.Node.Tag is MetadataColumn;
            btnAddColumn.Enabled = !(e.Node.Tag is FormOutput && ((FormOutput)e.Node.Tag).Index != 0);
        }

        private void trvOutpuColumns_AfterLabelEdit(object sender, NodeLabelEditEventArgs e)
        {
            if (string.IsNullOrEmpty(e.Label) || e.Label.Trim() == string.Empty)
            {
                e.CancelEdit = true;
                return;
            }

            if (e.Node != null && e.Node.Tag is INameProvider)
            {
                INameProvider p = e.Node.Tag as INameProvider;
                p.Name = e.Label;
                propOutputColumn.SelectedObject = e.Node.Tag;
            }

        }

        private void btnAddColumn_Click(object sender, EventArgs e)
        {
            UIHelper.AddOutputColumn(trvOutputColumns, MetadataColumn.CreateNewInstance, o_PropertyChanged);
        }

        private void btnRemoveColumn_Click(object sender, EventArgs e)
        {
            UIHelper.RemoveOutputColumn(trvOutputColumns);
        }
    }
}
