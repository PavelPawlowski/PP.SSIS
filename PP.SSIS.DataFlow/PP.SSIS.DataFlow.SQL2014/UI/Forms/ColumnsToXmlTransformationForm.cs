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
using System.Drawing.Design;

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
using PP.SSIS.DataFlow.Common;
using System.Reflection;
#endif

namespace PP.SSIS.DataFlow.UI
{
    public partial class ColumnsToXmlTransformationForm : Form, IUIForm
    {
        #region GUI Support Classes
        [DefaultProperty("XmlInputColumns")]
        private class XmlColumn : FormOutputColumn
        {

            public XmlColumn(IDTSOutputColumn col) : base(col)
            {
            }

            IDTSCustomProperty saveOptionsProperty;
            IDTSCustomProperty serializeLineage;
            IDTSCustomProperty serializeDataType;
            IDTSCustomProperty xmlInputColumns;
            IDTSCustomProperty sourceID;
            IDTSCustomProperty sourceName;

            InputColumns _inputColumns;
            protected override void DTSOutputColumnSetProcessing()
            {
                base.DTSOutputColumnSetProcessing();
                saveOptionsProperty = null;
                serializeLineage = null;
                serializeDataType = null;

                if (DTSOutputColumn != null)
                {
                    foreach (IDTSCustomProperty prop in DTSOutputColumn.CustomPropertyCollection)
                    {
                        if (prop.Name == Resources.XmlSaveOptionPropertyName)
                            saveOptionsProperty = prop;
                        else if (prop.Name == Resources.XmlSerializeLineageName)
                            serializeLineage = prop;
                        else if (prop.Name == Resources.XmlSerializeDataTypeName)
                            serializeDataType = prop;
                        else if (prop.Name == Resources.XmlInputColumnsPropertyname)
                            xmlInputColumns = prop;
                        else if (prop.Name == Resources.XmlSourceIdPropertyName)
                            sourceID = prop;
                        else if (prop.Name == Resources.XmlSourceNamePropertyName)
                            sourceName = prop;
                    }
                }

                var value = this.DataType;
                UpdateReadOnlyProperty("DataLength", value == XmlDataType.DT_WSTR ? false : true);

            }

            [Category("Xml Column")]
            [Description("Specifies the columns to be used to build Xml")]
            [Editor(typeof(InputColumnsUIEditor), typeof(UITypeEditor))]
            [TypeConverter(typeof(ExpandableObjectConverter))]
            [RefreshProperties(RefreshProperties.All)]
            [ReadOnly(false)]
            public InputColumns XmlInputColumns
            {
                get
                {
                    if (_inputColumns == null)
                    {
                        if (xmlInputColumns != null)
                        {
                            string lineages = xmlInputColumns.Value.ToString();
                            _inputColumns = new InputColumns(lineages);
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
                    if (xmlInputColumns != null)
                    {
                        if (value != null)
                        {
                            string lineages = value.GetInputLineagesString();
                            xmlInputColumns.Value = lineages;
                        }
                    }
                    NotifyPropertyChanged("XmlInputColumns");
                }
            }

            [Category("Xml Column")]
            [Description("Specifies whether serailize Columns ID and LineageID")]
            public bool SerializeLineage
            {
                get
                {
                    if (serializeLineage != null)
                        return (bool)serializeLineage.Value;
                    else
                        return false;
                }
                set
                {
                    if (serializeLineage != null)
                    {
                        serializeLineage.Value = value;
                        NotifyPropertyChanged("SerializeLineage");
                    }
                }
            }


            [Category("Xml Column")]
            [Description("Specifies whether column DataType information should be serialized")]
            public bool SerializeDataType
            {
                get
                {
                    if (serializeDataType != null)
                        return (bool)serializeDataType.Value;
                    else
                        return false;
                }
                set
                {
                    if (serializeDataType != null)
                    {
                        serializeDataType.Value = value;
                        NotifyPropertyChanged("SerializeDataType");
                    }
                }
            }

            [Category("Xml Column")]
            [Description("Save Options of the Xml column")]
            public SaveOptions XmlSaveOptions
            {
                get
                {
                    if (saveOptionsProperty != null)
                    {
                        SaveOptions saveOptions = (SaveOptions)saveOptionsProperty.Value;
                        return saveOptions;
                    }
                    return SaveOptions.None;
                }
                set
                {
                    if (saveOptionsProperty != null)
                    {
                        saveOptionsProperty.Value = value;

                        NotifyPropertyChanged("XmlSaveOptions");
                    }
                }
            }

            [Category("Xml Column")]
            [Description("Specifies optional descriptive ID of the culumns source")]
            public string SourceID
            {
                get
                {
                    if (sourceID != null)
                    {
                        string id = sourceID.Value.ToString();
                        return id;
                    }
                    return string.Empty;
                }
                set
                {
                    if (sourceID != null)
                    {
                        sourceID.Value = value;
                        NotifyPropertyChanged("SourceID");
                    }

                }
            }

            [Category("Xml Column")]
            [Description("Specifies optional descriptive Name of the columns source")]
            public string SourceName
            {
                get
                {
                    if (sourceName != null)
                    {
                        string id = sourceName.Value.ToString();
                        return id;
                    }
                    return string.Empty;
                }
                set
                {
                    if (sourceName != null)
                    {
                        sourceName.Value = value;
                        NotifyPropertyChanged("SourceID");
                    }

                }
            }

            public static XmlColumn CreateInstance(IDTSOutputColumn col)
            {
                return new XmlColumn(col);
            }

            public static XmlColumn CreateNewInstance(IDTSOutput output)
            {
                IDTSOutputColumn col = output.OutputColumnCollection.New();
                col.Name = Resources.XmlColumnDefaultName + col.ID;
                col.Description = Resources.XmlColumnDefaultDesccription;
                ColumnsToXmlTransformation.SetXmlColumnProperties(col);

                XmlColumn c = new XmlColumn(col);
                return c;
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

            [Category("Output Column Data Type")]
            [ReadOnly(false)]
            [Description("The data type of selected output column")]
            [RefreshProperties(RefreshProperties.All)]
            new public XmlDataType DataType
            {
                get
                {
                    return (XmlDataType)((int)DTSOutputColumn.DataType);
                }
                set
                {
                    DataType dt = (DataType)((int)value);
                    int len;
                    if (dt != DTSOutputColumn.DataType)
                    {
                        if (dt == Microsoft.SqlServer.Dts.Runtime.Wrapper.DataType.DT_WSTR)
                            len = 4000;
                        else
                            len = 0;
                        DTSOutputColumn.SetDataTypeProperties(dt, len, 0, 0, 0);

                        UpdateReadOnlyProperty("DataLength", value == XmlDataType.DT_WSTR ? false : true);
                    }

                    NotifyPropertyChanged("DataType");
                    NotifyPropertyChanged("DataLenght");
                }
            }
        }

        #endregion

        private IUIHelper uiHelper;

        public IUIHelper UIHelper
        {
            get { return uiHelper; }
            private set { uiHelper = value; }
        }

        public ColumnsToXmlTransformationForm()
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
                    XmlColumn ocol = nd.Tag as XmlColumn;
                    if (ocol != null)
                    {
                        var colInputLineages = ocol.XmlInputColumns.GetInputLineages();
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

        private void ColumnsToXmlTransformationForm_Load(object sender, EventArgs e)
        {
            UIHelper.FormInitialize(null, null, trvOutputColumns, XmlColumn.CreateInstance, o_PropertyChanged);
        }

        void o_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == "Name")
            {
                UIHelper.UpdateTreeViewNodeName(sender, trvOutputColumns);
            }
        }

        private void trvOutpuColumns_AfterSelect(object sender, TreeViewEventArgs e)
        {
            if (e.Node != null)
            {
                if (e.Node.Tag is XmlColumn)
                {
                    //Update the RedOnly for Prescision and Scale based ont he DataType
                    var val = ((XmlColumn)e.Node.Tag).DataType;
                    PropertyDescriptor descriptor = TypeDescriptor.GetProperties(e.Node.Tag.GetType())["DataLength"];
                    ReadOnlyAttribute attrib = (ReadOnlyAttribute)descriptor.Attributes[typeof(ReadOnlyAttribute)];
                    FieldInfo isReadOnly = attrib.GetType().GetField("isReadOnly", BindingFlags.NonPublic | BindingFlags.Instance);
                    isReadOnly.SetValue(attrib, val == XmlDataType.DT_WSTR ? false : true);
                }

                propOutputColumn.SelectedObject = e.Node.Tag;
            }
            else
                propOutputColumn.SelectedObject = null;

            btnRemoveColumn.Enabled = e.Node.Tag is XmlColumn;
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
            UIHelper.AddOutputColumn(trvOutputColumns, XmlColumn.CreateNewInstance, o_PropertyChanged);
        }

        private void btnRemoveColumn_Click(object sender, EventArgs e)
        {
            UIHelper.RemoveOutputColumn(trvOutputColumns);
        }
    }
}
