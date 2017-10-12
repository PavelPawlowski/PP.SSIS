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
using System.Drawing.Design;
using System.Windows.Forms.Design;
using System.Globalization;

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
    public partial class HashColumnsTransformationForm : Form, IUIForm
    {
        #region Definitions
        private IUIHelper uiHelper;
        protected IUIHelper UIHelper
        {
            get { return uiHelper; }
            private set
            {
                uiHelper = value;
            }
        }

        private bool inputIsAttached = true;
        #endregion

        #region GUI Support Classes
        /// <summary>
        /// Class Representing the Hash Output Column
        /// </summary>
        [DefaultProperty("HashColumns")]
        internal class HashOutputColumn : FormOutputColumn
        {
            public static bool InputIsAttached { get; internal set; }

            public HashOutputColumn(IDTSOutputColumn col) : base(col)
            {

            }

            InputColumns _inputColumns = new InputColumns();

            IDTSCustomProperty hashTypeProperty;
            IDTSCustomProperty hashInputColumns;
            IDTSCustomProperty hashFieldSeparator;
            IDTSCustomProperty hashImplementationType;
            IDTSCustomProperty nullReplacementValue;
            IDTSCustomProperty stringTrimming;

            protected override void DTSOutputColumnSetProcessing()
            {
                base.DTSOutputColumnSetProcessing();
                if (DTSOutputColumn != null)
                {
                    hashTypeProperty = DTSOutputColumn.CustomPropertyCollection[Resources.HashAlgorithmPropertyName];
                    hashInputColumns = DTSOutputColumn.CustomPropertyCollection[Resources.HashColumnHashInputColumnsPropertyName];
                    hashImplementationType = DTSOutputColumn.CustomPropertyCollection[Resources.HashColumnHashImplementationTypePropertyName];
                    hashFieldSeparator = DTSOutputColumn.CustomPropertyCollection[Resources.HashColumnHashFieldSeparatorPropertyName];
                    nullReplacementValue = DTSOutputColumn.CustomPropertyCollection[Resources.HashColumnNullReplacementValue];
                    stringTrimming = DTSOutputColumn.CustomPropertyCollection[Resources.HashColumnStringTrimmingPropertyName];

                    _inputColumns = null;
                }
                else
                {
                    hashTypeProperty = null;
                    hashInputColumns = null;
                    hashFieldSeparator = null;
                    nullReplacementValue = null;
                    stringTrimming = null;
                    _inputColumns = new InputColumns();
                }

                UpdateReadOnlyProperty("HashInputColumns", !InputIsAttached);
                UpdateReadOnlyProperties();
            }

            /// <summary>
            /// Iinput columns to be hashed
            /// </summary>
            [Category("Hash Column")]
            [Description("Specifies the columns to be used for Hash Generation")]
            [Editor(typeof(InputColumnsUIEditor), typeof(UITypeEditor))]
            [TypeConverter(typeof(ExpandableObjectConverter))]
            [RefreshProperties(RefreshProperties.All)]
            [ReadOnly(false)]
            public InputColumns HashInputColumns
            {
                get
                {
                    if (_inputColumns == null)
                    {
                        if (hashInputColumns != null)
                        {
                            string lineages = hashInputColumns.Value.ToString();
                            _inputColumns = new InputColumns(lineages);
                            if (lineages != _inputColumns.GetInputLineagesString())
                                hashInputColumns.Value = _inputColumns.GetInputLineagesString();
                        }
                        else
                        {
                            _inputColumns = new InputColumns();
                        }
                    }

                    return _inputColumns;
                }
                set
                {
                    _inputColumns = value;
                    if (hashInputColumns != null)
                    {
                        if (value != null)
                        {
                            string lineages = value.GetInputLineagesString();
                            hashInputColumns.Value = lineages;
                        }
                    }
                    NotifyPropertyChanged("HashInputColumns");
                }
            }

            /// <summary>
            /// Hahs Algorithm to be used
            /// </summary>
            [Category("Hash Column")]
            [Description("Specifies the HASH algorithm to be used for the Hash Column")]
            [DefaultValue(HashColumnsTransformation.HashType.MD5)]
            public HashColumnsTransformation.HashType HashType
            {
                get
                {
                    if (hashTypeProperty != null)
                    {
                        HashColumnsTransformation.HashType hashType = (HashColumnsTransformation.HashType)hashTypeProperty.Value;
                        return hashType;
                    }
                    return HashColumnsTransformation.HashType.None;
                }
                set
                {
                    if (hashTypeProperty != null)
                    {
                        hashTypeProperty.Value = value;
                        int dataLen;
                        HashColumnsTransformationHelper.GetHashTypeDataType(value, DTSOutputColumn.DataType, out dataLen);
                        DTSOutputColumn.SetDataTypeProperties(DTSOutputColumn.DataType, dataLen, 0, 0, DTSOutputColumn.CodePage);

                        NotifyPropertyChanged("HashType");
                    }
                }
            }

            /// <summary>
            /// Gets or Sets the HashImplementation type of the Hash Calculation
            /// </summary>
            [Category("Hash Column")]
            [Description("Specifies the implementaion version of the HASH transformation")]
            [RefreshProperties(RefreshProperties.All)]
            [DefaultValue(HashColumnsTransformation.HashImplementationType.BinarySafe)]
            public HashColumnsTransformation.HashImplementationType HashImplementationType
            {
                get
                {
                    if (hashImplementationType != null)
                    {
                        HashColumnsTransformation.HashImplementationType implTypew = (HashColumnsTransformation.HashImplementationType)hashImplementationType.Value;
                        return implTypew;
                    }
                    return HashColumnsTransformation.HashImplementationType.OriginalBinary;
                }
                set
                {
                    if (hashImplementationType != null)
                    {
                        hashImplementationType.Value = value;

                        UpdateReadOnlyProperties();
                        NotifyPropertyChanged("HashImplementationType");
                    }
                }
            }

            /// <summary>
            /// Gets or Sets the StringTrimming
            /// </summary>
            [Category("Hash Column")]
            [Description("Specifies Whether and how string values should be trimmed. Not Used by Original Implementation.")]
            [ReadOnly(false)]
            [DefaultValue(HashColumnsTransformation.StringTrimming.None)]
            public HashColumnsTransformation.StringTrimming StringTrimming
            {
                get
                {
                    if (stringTrimming != null)
                    {
                        HashColumnsTransformation.StringTrimming trim = (HashColumnsTransformation.StringTrimming)stringTrimming.Value;
                        return trim;
                    }
                    return HashColumnsTransformation.StringTrimming.None;
                }
                set
                {
                    if (stringTrimming != null)
                    {
                        stringTrimming.Value = value;
                        NotifyPropertyChanged("StringTrimming");
                    }
                }
            }

            /// <summary>
            /// Gets or Sets the NULL replacement value
            /// </summary>
            [Category("Hash Column")]
            [Description("Specifies Hash Filed separator for Unicode String Implementations")]
            [ReadOnly(false)]
            [DefaultValue("~NULL~")]
            public string NullReplacementValue
            {
                get
                {
                    if (nullReplacementValue != null)
                    {
                        string nullValue = nullReplacementValue.Value.ToString();
                        return nullValue;
                    }
                    return HashColumnsTransformationHelper.DefaultNullReplacement;
                }
                set
                {
                    if (nullReplacementValue != null)
                    {
                        nullReplacementValue.Value = value;
                        NotifyPropertyChanged("NullReplacementValue");
                    }
                }
            }

            /// <summary>
            /// Separator to for the input columns in case of using the String variant of building hash
            /// </summary>
            [Category("Hash Column")]
            [Description("Specifies Hash Filed separator for Unicode String Implementations")]
            [ReadOnly(false)]
            [DefaultValue("|")]
            public string HashFieldSeparator
            {
                get
                {
                    if (hashFieldSeparator != null)
                    {
                        string separator = hashFieldSeparator.Value.ToString();
                        return separator;
                    }
                    else
                        return HashColumnsTransformationHelper.DefaultFieldtSeparator;

                }
                set
                {
                    if (hashFieldSeparator != null)
                    {
                        hashFieldSeparator.Value = value;
                        NotifyPropertyChanged("HashFieldSeparator");
                    }
                }
            }

            [Category("Output Column Data Type")]
            [ReadOnly(false)]
            [Description("The data type of selected output column")]
            [RefreshProperties(RefreshProperties.All)]
            [DefaultValue(HashColumnsTransformation.HashOutputDataType.DT_BYTES)]
            new public HashColumnsTransformation.HashOutputDataType DataType
            {
                get
                {
                    return (HashColumnsTransformation.HashOutputDataType)((int)DTSOutputColumn.DataType);
                }
                set
                {
                    DataType dt = (DataType)((int)value);
                    if (dt != DTSOutputColumn.DataType)
                    {
                        int dataLen;
                        int codePage = 0;
                        if (dt == Microsoft.SqlServer.Dts.Runtime.Wrapper.DataType.DT_STR)
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

                        HashColumnsTransformationHelper.GetHashTypeDataType(HashType, dt, out dataLen);
                        DTSOutputColumn.SetDataTypeProperties(dt, dataLen, 0, 0, codePage);
                        UpdateReadOnlyProperties();
                        NotifyPropertyChanged("DataType");
                    }
                }
            }

            /// <summary>
            /// Gets or Sets the CodePage fo the column DataType
            /// </summary>
            [ReadOnly(false)]
            public override int CodePage
            {
                get
                {
                    return base.CodePage;
                }

                set
                {
                    if (DTSOutputColumn != null && DataType == HashColumnsTransformation.HashOutputDataType.DT_STR)
                    {
                        DTSOutputColumn.SetDataTypeProperties(base.DataType, base.DataLength, base.Precision, base.Scale, value);
                        NotifyPropertyChanged("CodePage");
                    }
                }
            }

            [ReadOnly(true)]
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

            [ReadOnly(true)]
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
            /// Creates instance of the HashOutputColumn base on existing output column
            /// </summary>
            /// <param name="col">Output columns representing the HashOutputColumn</param>
            /// <returns></returns>
            public static HashOutputColumn CreateInstance(IDTSOutputColumn col)
            {
                return new HashOutputColumn(col);
            }

            /// <summary>
            /// Create a new instance of the HashOutputColumn
            /// </summary>
            /// <param name="output">output in which the instance should be created</param>
            /// <returns></returns>
            public static HashOutputColumn CreateNewInstance(IDTSOutput output)
            {
                IDTSOutputColumn col = output.OutputColumnCollection.New();
                col.Name = Resources.HashColumnDefaultName + col.ID;
                col.Description = Resources.HashColumnDefaultDesccription;

                HashColumnsTransformationHelper.SetHashColumnProperties(HashColumnsTransformation.HashOuputColumnProperties.All, col);

                HashOutputColumn c = new HashOutputColumn(col);
                return c;
            }

            /// <summary>
            /// Updates ReadOnly attributes depending on the state
            /// </summary>
            private void UpdateReadOnlyProperties()
            {
                UpdateReadOnlyProperty("CodePage", DTSOutputColumn.DataType != Microsoft.SqlServer.Dts.Runtime.Wrapper.DataType.DT_STR);
                UpdateReadOnlyProperty("StringTrimming", HashImplementationType == HashColumnsTransformation.HashImplementationType.OriginalBinary);

                bool replacementReadOnly = HashImplementationType != HashColumnsTransformation.HashImplementationType.UnicodeStringDelmited;
                bool separatorDisabled = HashImplementationType == HashColumnsTransformation.HashImplementationType.OriginalBinary || HashImplementationType == HashColumnsTransformation.HashImplementationType.BinarySafe;

                UpdateReadOnlyProperty("NullReplacementValue", replacementReadOnly);
                UpdateReadOnlyProperty("HashFieldSeparator", separatorDisabled);
            }

            #region Support Classes
            #endregion
        }

        [DefaultProperty("ParallelProcessing")]
        internal class HashComponentProperties
        {
            private IDTSCustomProperty parallelProcessing;

            public HashComponentProperties(IDTSCustomProperty parallelProcessing)
            {
                this.parallelProcessing = parallelProcessing;
            }

            [Category("Hash Transformation")]
            [Description("Specifies whether Hash processing for multiple output columsn should run in multiple threads. Auto = processes columns in parallel only if there is more than 6 output columns.")]
            public HashColumnsTransformation.ParallelProcessing ParallelProcessing
            {
                get
                {
                    if (parallelProcessing != null)
                        return (HashColumnsTransformation.ParallelProcessing)parallelProcessing.Value;
                    else
                        return HashColumnsTransformation.ParallelProcessing.Off;
                }
                set
                {
                    if (parallelProcessing != null)
                    {
                        parallelProcessing.Value = value;
                    }
                }
            }
        }

        #endregion


        public HashColumnsTransformationForm()
        {
            InitializeComponent();
        }

        public void InitializeUIForm(IUIHelper uiHelper)
        {
            this.UIHelper = uiHelper;
            InputColumnsUIEditor.UIHelper = uiHelper;
            this.Text = string.Format(Resources.EditorFor, UIHelper.ComponentMetadata.Name);
        }


        private void btnOk_Click(object sender, EventArgs e)
        {
            ProcessInputColumns();

            this.Close();
        }

        private void ProcessInputColumns()
        {
            List<int> inputLineages = new List<int>();
            //Get lineages of input columns for all output HASH Columns
            TreeNode node = trvOutputColumns.SelectedNode;
            if (node != null)
            {
                if (node.Tag is FormOutputColumn)
                    node = node.Parent;

                //Iterate through Hash Output Columns
                foreach (TreeNode nd in node.Nodes)
                {
                    HashOutputColumn ocol = nd.Tag as HashOutputColumn;
                    if (ocol != null)
                    {
                        var colInputLineages = ocol.HashInputColumns.GetInputLineages();
                        //Iterate through each input column lineages and store unique lineages in the inputLineages List
                        foreach (int lineageID in colInputLineages)
                        {
                            if (!inputLineages.Contains(lineageID))
                                inputLineages.Add(lineageID);
                        }
                    }
                }
            }

            var iCols = UIHelper.GetFormInputColumns();
            foreach (FormInputColumn icol in iCols)
            {
                bool isSelected = inputLineages.Contains(icol.LineageID);
                if (isSelected != icol.IsSelected)
                    UIHelper.SelectInputColumn(icol.LineageID, isSelected);
            }

        }

        private void HashColumnsTransformationForm_Load(object sender, EventArgs e)
        {
            IDTSInput input = UIHelper.GetInput(0);
            if (input == null || input.IsAttached == false)
                inputIsAttached = false;
            HashOutputColumn.InputIsAttached = inputIsAttached;            

            UIHelper.FormInitialize(null, null, trvOutputColumns, HashOutputColumn.CreateInstance, o_PropertyChanged);

            IDTSCustomProperty parallelProcessing = UIHelper.ComponentMetadata.CustomPropertyCollection[Resources.HashTransformationParallelProcessingPropertyName];

            HashComponentProperties properties = new HashComponentProperties(parallelProcessing);

            propComponent.SelectedObject = properties;

            if(inputIsAttached == false)
                MessageBox.Show("Hash Columns Transformation has no input attached. Columns selection will be disabled","Input not attached", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);                
        }

        void o_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {                        
            if (e.PropertyName == "Name")
            {                
                UIHelper.UpdateTreeViewNodeName(sender, trvOutputColumns);
            }

            HashOutputColumn hoc = sender as HashOutputColumn;
            if (hoc != null && hoc.AssociatedTreeNode != null)
            {
                if (hoc.HashType == HashColumnsTransformation.HashType.None || hoc.HashInputColumns.Count == 0)
                    hoc.AssociatedTreeNode.StateImageIndex = 1;
                else
                    hoc.AssociatedTreeNode.StateImageIndex = 0;
            }

        }

        private void trvOutpuColumns_AfterSelect(object sender, TreeViewEventArgs e)
        {
            //e.Node.Tag
            if (e.Node != null)
                propOutputColumn.SelectedObject = e.Node.Tag;
            else
                propOutputColumn.SelectedObject = null;

            btnRemoveColumn.Enabled = e.Node.Tag is HashOutputColumn;
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
            var outCol = UIHelper.AddOutputColumn(trvOutputColumns, HashOutputColumn.CreateNewInstance, o_PropertyChanged);
            //outCol.PropertyChanged += OutCol_PropertyChanged;
        }

        private void OutCol_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {

        }

        private void btnRemoveColumn_Click(object sender, EventArgs e)
        {
            UIHelper.RemoveOutputColumn(trvOutputColumns);
        }

    }
}
