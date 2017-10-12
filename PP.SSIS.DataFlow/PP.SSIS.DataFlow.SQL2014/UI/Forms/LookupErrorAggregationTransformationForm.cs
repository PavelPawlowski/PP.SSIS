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
    public partial class LookupErrorAggregationTransformationForm : Form, IUIForm
    {
        [DefaultProperty("LookupSourceID")]
        private class LookupSourceIDColumn : FormOutputColumn
        {
            IDTSCustomProperty prop;

            public LookupSourceIDColumn(IDTSOutputColumn col) : base(col)
            {
            }

            protected override void DTSOutputColumnSetProcessing()
            {
                base.DTSOutputColumnSetProcessing();
                prop = DTSOutputColumn.CustomPropertyCollection[Resources.LookupErrorAggLookupSourceIDName];
            }

            [Category("LookupSource")]
            [Description("Identification fo the source for the aggreegation. Generally it is the Lookup Component Name")]
            public string LookupSourceID
            {
                get
                {
                    return prop.Value.ToString();
                }
                set
                {
                    prop.Value = value;
                    NotifyPropertyChanged("LookupSourceID");
                }
            }
        }

        [DefaultProperty("LookupSourceDescription")]
        [Description("Optional description of the source for aggragation. Generally it describes the lookup component which is source for the aggregation")]
        private class LookupSourceDescriptionColumn : FormOutputColumn
        {
            IDTSCustomProperty prop;

            public LookupSourceDescriptionColumn(IDTSOutputColumn col) : base(col)
            {
            }
            protected override void DTSOutputColumnSetProcessing()
            {
                base.DTSOutputColumnSetProcessing();
                prop = DTSOutputColumn.CustomPropertyCollection[Resources.LookupErrorAggLookupSourceDescriptionName];
            }

            [Category("LookupSource")]
            public string LookupSourceDescription
            {
                get
                {
                    return prop.Value.ToString();
                }
                set
                {
                    prop.Value = value;
                    NotifyPropertyChanged("LookupSourceID");
                }
            }

        }

        [DefaultProperty("XmlSaveOptions")]
        private class LookupErrorColumn : FormOutputColumn
        {

            public LookupErrorColumn(IDTSOutputColumn col) : base(col)
            {
            }

            IDTSCustomProperty saveOptionsProperty;
            IDTSCustomProperty serializeLineage;
            IDTSCustomProperty serializeDataType;

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

            public static FormOutputColumn CreateInstance(IDTSOutputColumn col)
            {
                if (col.Name == "LookupDetailsXml")
                    return new LookupErrorColumn(col);
                else if (col.Name == Resources.LookupErrorAggLookupSourceIDName)
                    return new LookupSourceIDColumn(col);
                else if (col.Name == Resources.LookupErrorAggLookupSourceDescriptionName)
                    return new LookupSourceDescriptionColumn(col);
                else
                    return new FormOutputColumn(col);
            }

            public static FormOutputColumn CreateNewInstance(IDTSOutput output)
            {
                IDTSOutputColumn col = output.OutputColumnCollection.New();
                col.Name = Resources.XmlColumnDefaultName + col.ID;
                col.Description = Resources.XmlColumnDefaultDesccription;
                ColumnsToXmlTransformation.SetXmlColumnProperties(col);

                LookupErrorColumn c = new LookupErrorColumn(col);
                return c;
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
                return new StandardValuesCollection(LookupErrorAggregationTransformationUI.InputColumns.ConvertAll<string>(i => i.DisplayName));
            }
        }


        [DefaultProperty("NullColumn")]
        private class LookupErrorAggregationProperties : INotifyPropertyChanged
        {
            private IDTSCustomProperty hashAlgorithmType;
            private IDTSCustomProperty nullColumn;
            private string _nullColumnName;


            public LookupErrorAggregationProperties(IDTSCustomProperty hashAlgorithm, IDTSCustomProperty nullColumn)
            {
                //this.lookupSourceDescription = sourceName;
                //this.lookupSourceID = sourceID;
                this.hashAlgorithmType = hashAlgorithm;
                this.nullColumn = nullColumn;
            }


            [Category("Lookup Error Aggregation")]
            [Description("Specifies the Hash Algorithm to be used for hashing the keys. When null the individual key values are being used in cache.")]
            [DefaultValue(HashAlgorithmType.None)]
            public HashAlgorithmType HashAlgorithmType
            {
                get
                {
                    return (HashAlgorithmType)hashAlgorithmType.Value;
                }
                set
                {
                    hashAlgorithmType.Value = value;
                    NotifyPropertyChanged("HashAlgorithmType");
                }
            }


            [Category("Lookup Error Aggregation")]
            [Description("Process only when selected column is null. Used to detect lookup errors when Ignore is selected in the Lookup Component.")]
            [TypeConverter(typeof(InputColumnTypeConverter))]
            public string NullColumn
            {
                get
                {
                    if (_nullColumnName == null)
                    {
                        int lineageID = nullColumn != null ? (int)nullColumn.Value : -1;
                        var cmic = LookupErrorAggregationTransformationUI.InputColumns.Find(ic => ic.LineageID == lineageID);
                        if (cmic != null)
                            _nullColumnName = cmic.DisplayName;
                        else if (lineageID != -1 && nullColumn != null)
                            nullColumn.Value = -1;
                    }
                    return _nullColumnName;
                }
                set
                {
                    if (nullColumn != null)
                    {
                        _nullColumnName = value;
                        var cmic = LookupErrorAggregationTransformationUI.InputColumns.Find(ic => ic.DisplayName == _nullColumnName);
                        if (cmic != null)
                            nullColumn.Value = cmic.LineageID;
                        else
                            nullColumn.Value = -1;
                        NotifyPropertyChanged("NullColumn");
                        NotifyPropertyChanged("NullColumnLineageId");
                    }
                }
            }

            [Browsable(false)]
            public int NullColumnLineageId
            {

                get
                {
                    return nullColumn != null ? (int)nullColumn.Value : -1;
                }
                set
                {
                    if (nullColumn != null)
                    {
                        _nullColumnName = null;
                        nullColumn.Value = value;
                        NotifyPropertyChanged("NullColumn");
                        NotifyPropertyChanged("NullColumnLineageId");
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

        private IUIHelper uiHelper;

        public IUIHelper UIHelper
        {
            get { return uiHelper; }
            private set { uiHelper = value; }
        }

        public LookupErrorAggregationTransformationForm()
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
            if (properties.NullColumnLineageId != -1)
            {
                IDTSInput input = UIHelper.GetInput(0);
                IDTSVirtualInput vInput = UIHelper.GetVirtualInput(0);
                IDTSVirtualInputColumn vCol = vInput.VirtualInputColumnCollection.GetVirtualInputColumnByLineageID(properties.NullColumnLineageId); ;
                IDTSInputColumn col;
                if (vCol.UsageType != DTSUsageType.UT_IGNORED)
                    col = input.InputColumnCollection.GetInputColumnByLineageID(vCol.LineageID);
                else
                    col = UIHelper.DesignTimeComponent.SetUsageType(input.ID, vInput, properties.NullColumnLineageId, DTSUsageType.UT_READONLY);

                IDTSCustomProperty nullCol = col.CustomPropertyCollection[Resources.LookupErrorAggIsNullColumnName];
                nullCol.Value = true;

            }
            this.Close();
        }

        LookupErrorAggregationProperties properties;

        private void ColumnsToXmlTransformationForm_Load(object sender, EventArgs e)
        {
            UIHelper.FormInitialize(clbInputColumns, lbSelectedItems, trvOutputColumns, LookupErrorColumn.CreateInstance, o_PropertyChanged);

            IDTSCustomProperty hashAlgorithm = UIHelper.ComponentMetadata.CustomPropertyCollection[LookupErrorAggregationTransformation.GetPropertyName(LookupErrorAggregationTransformation.CustomProperties.HashAlgorithm)];
            IDTSCustomProperty nullColumn = UIHelper.ComponentMetadata.CustomPropertyCollection[LookupErrorAggregationTransformation.GetPropertyName(LookupErrorAggregationTransformation.CustomProperties.NullColumn)];

            properties = new LookupErrorAggregationProperties(hashAlgorithm, nullColumn);

            propComponent.SelectedObject = properties;
        }

        void o_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == "Name")
            {
                UIHelper.UpdateTreeViewNodeName(sender, trvOutputColumns);
            }
        }

        private void clbInputColumns_ItemCheck(object sender, ItemCheckEventArgs e)
        {
            if (UIHelper.Loading)
                return;

            var helper = UIHelper as LookupErrorAggregationTransformationUI;
            if (helper != null)
            {
                helper.SelectLookupInputColumn(clbInputColumns, lbSelectedItems, e.Index, e.NewValue, properties.NullColumnLineageId);
            }
        }

        private void btnUp_Click(object sender, EventArgs e)
        {
            UIHelper.MoveSelectedItem(lbSelectedItems, true);
        }

        private void btnDown_Click(object sender, EventArgs e)
        {
            UIHelper.MoveSelectedItem(lbSelectedItems, false);
        }

        private void lbSelectedItems_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Modifiers == Keys.Control)
            {
                if (e.KeyCode == Keys.Up)
                {
                    UIHelper.MoveSelectedItem(lbSelectedItems, true);
                    e.Handled = true;
                }
                else if (e.KeyCode == Keys.Down)
                {
                    UIHelper.MoveSelectedItem(lbSelectedItems, false);
                    e.Handled = true;
                }
            }
        }

        private void trvOutpuColumns_AfterSelect(object sender, TreeViewEventArgs e)
        {
            //e.Node.Tag
            if (e.Node != null)
                propOutputColumn.SelectedObject = e.Node.Tag;
            else
                propOutputColumn.SelectedObject = null;

            var outCol = e.Node.Tag as FormOutputColumn;
            bool canRemove = false;
            if (outCol != null)
            {
                switch (outCol.Name)
                {
                    case "KeysCount":
                    case "TotalRowsCount":
                    case "LookupSourceDescription":
                        canRemove = true;
                        break;
               }
            }
            btnRemoveColumn.Enabled = canRemove;
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
            UIHelper.AddOutputColumn(trvOutputColumns, LookupErrorColumn.CreateNewInstance, o_PropertyChanged);
        }

        private void btnRemoveColumn_Click(object sender, EventArgs e)
        {
            UIHelper.RemoveOutputColumn(trvOutputColumns);
        }

        private void selectAllToolStripMenuItem_Click(object sender, EventArgs e)
        {
            for (int i = 0; i < clbInputColumns.Items.Count; i++)
            {
                clbInputColumns.SetItemChecked(i, true);
            }
        }

        private void deselectAllToolStripMenuItem_Click(object sender, EventArgs e)
        {
            for (int i = 0; i < clbInputColumns.Items.Count; i++)
            {
                clbInputColumns.SetItemChecked(i, false);
            }
        }

    }
}
