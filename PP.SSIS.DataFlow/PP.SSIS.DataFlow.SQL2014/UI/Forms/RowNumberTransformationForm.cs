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

using IDTSOutput = Microsoft.SqlServer.Dts.Pipeline.Wrapper.IDTSOutput100;
using IDTSCustomProperty = Microsoft.SqlServer.Dts.Pipeline.Wrapper.IDTSCustomProperty100;
using IDTSOutputColumn = Microsoft.SqlServer.Dts.Pipeline.Wrapper.IDTSOutputColumn100;
using IDTSInput = Microsoft.SqlServer.Dts.Pipeline.Wrapper.IDTSInput100;
using IDTSInputColumn = Microsoft.SqlServer.Dts.Pipeline.Wrapper.IDTSInputColumn100;
using IDTSVirtualInput = Microsoft.SqlServer.Dts.Pipeline.Wrapper.IDTSVirtualInput100;
using IDTSComponentMetaData = Microsoft.SqlServer.Dts.Pipeline.Wrapper.IDTSComponentMetaData100;
using IDTSDesigntimeComponent = Microsoft.SqlServer.Dts.Pipeline.Wrapper.IDTSDesigntimeComponent100;
using IDTSVirtualInputColumn = Microsoft.SqlServer.Dts.Pipeline.Wrapper.IDTSVirtualInputColumn100;
using System.Reflection;


namespace PP.SSIS.DataFlow.UI
{
    public partial class RowNumberTransformationForm : Form, IUIForm
    {
        /// <summary>
        /// Represetns ErrorMetadataColumn for displaying in the Properties Grid
        /// </summary>
        [DefaultProperty("InitialValue")]
        private class RowNumberColumn : FormOutputColumn
        {
            /// <summary>
            /// DataTypes supported by the RowNumber Transformation - mapped to corresponding Dts.Runtime.Wrapper.DataType
            /// This Enumeration is created on show only the supported DataTypes in the PropertyGrid
            /// </summary>
            public enum RowNumberDataTypeType
            {
               DT_DBDATE    = Microsoft.SqlServer.Dts.Runtime.Wrapper.DataType.DT_DBDATE,
               DT_I1        = Microsoft.SqlServer.Dts.Runtime.Wrapper.DataType.DT_I1,
               DT_I2        = Microsoft.SqlServer.Dts.Runtime.Wrapper.DataType.DT_I2,
               DT_I4        = Microsoft.SqlServer.Dts.Runtime.Wrapper.DataType.DT_I4,
               DT_I8        = Microsoft.SqlServer.Dts.Runtime.Wrapper.DataType.DT_I8,
               DT_NUMERIC   = Microsoft.SqlServer.Dts.Runtime.Wrapper.DataType.DT_NUMERIC,
               DT_R4        = Microsoft.SqlServer.Dts.Runtime.Wrapper.DataType.DT_R4,
               DT_R8        = Microsoft.SqlServer.Dts.Runtime.Wrapper.DataType.DT_R8,
               DT_UI1       = Microsoft.SqlServer.Dts.Runtime.Wrapper.DataType.DT_UI1,
               DT_UI2       = Microsoft.SqlServer.Dts.Runtime.Wrapper.DataType.DT_UI2,
               DT_UI4       = Microsoft.SqlServer.Dts.Runtime.Wrapper.DataType.DT_UI4,
               DT_UI8       = Microsoft.SqlServer.Dts.Runtime.Wrapper.DataType.DT_UI8
            }

            public RowNumberColumn(IDTSOutputColumn col) : base(col)
            {
            }
            
            IDTSCustomProperty initialValueProperty;
            IDTSCustomProperty incrementByProperty;

            /// <summary>
            /// Procerss the associated IDTSOutputColumn and retrieves the MetadataColumn specific Custom Properties
            /// </summary>
            protected override void DTSOutputColumnSetProcessing()
            {
                base.DTSOutputColumnSetProcessing();

                if (DTSOutputColumn != null)
                {
                    initialValueProperty = DTSOutputColumn.CustomPropertyCollection[Resources.RowNumberInitialValuePropertyName];
                    incrementByProperty = DTSOutputColumn.CustomPropertyCollection[Resources.RowNumberIncrementByPropertyName];

                    //Dynamically update the ReadOnly attribute of the RowNumberColumn depending on the DataType property
                    var value = this.DataType;
                    PropertyDescriptor descriptor = TypeDescriptor.GetProperties(this.GetType())["Precision"];
                    ReadOnlyAttribute attrib = (ReadOnlyAttribute)descriptor.Attributes[typeof(ReadOnlyAttribute)];
                    FieldInfo isReadOnly = attrib.GetType().GetField("isReadOnly", BindingFlags.NonPublic | BindingFlags.Instance);
                    isReadOnly.SetValue(attrib, value == RowNumberDataTypeType.DT_NUMERIC ? false : true);

                    descriptor = TypeDescriptor.GetProperties(this.GetType())["Scale"];
                    attrib = (ReadOnlyAttribute)descriptor.Attributes[typeof(ReadOnlyAttribute)];
                    isReadOnly = attrib.GetType().GetField("isReadOnly", BindingFlags.NonPublic | BindingFlags.Instance);
                    isReadOnly.SetValue(attrib, value == RowNumberDataTypeType.DT_NUMERIC ? false : true);  

                }
                else
                {
                    initialValueProperty = null;
                    incrementByProperty = null;
                }
            }

            [Category("Row Number")]
            [ReadOnly(false)]
            [Description("Specifies value by which the initial value is incremented for each row")]
            public string IncrementBy
            {
                get
                {
                    return (incrementByProperty != null ? incrementByProperty.Value : RowNumberTransformation.GetDefaultDataTypeIncrement(DTSOutputColumn.DataType)).ToString();
                }
                set
                {
                    string propertyValue = value;

                    if (incrementByProperty != null)
                    {
                        //For DT_DBDATE process the days, weeks, months and years inrements
                        if (DataType == RowNumberDataTypeType.DT_DBDATE)
                        {
                            string pv = propertyValue.ToLower();
                            if (string.IsNullOrEmpty(pv))
                            {
                                propertyValue = "1d";
                            }
                            else
                            {
                                int ival;
                                char ch = pv[pv.Length - 1];
                                string val = pv.Substring(0, pv.Length - 1);

                                if (int.TryParse(pv, out ival))
                                {
                                    propertyValue = ival.ToString() + "d";
                                }
                                else if ((ch == 'd' || ch == 'w' || ch == 'm' || ch == 'y') && int.TryParse(val, out ival))
                                {
                                    propertyValue = pv;
                                }
                                else
                                {
                                    throw new System.InvalidCastException(Resources.ErrorUnsupportedRowNumberIncrementValue);
                                }
                            }
                            
                            incrementByProperty.Value = propertyValue;
                        }
                        else
                        {
                            incrementByProperty.Value = RowNumberTransformation.ParseInitialValue(propertyValue, DTSOutputColumn.DataType, Precision, Scale);
                        }

                        NotifyPropertyChanged("IncrementBy");
                    }
                }
            }

            [Category("Row Number")]
            [ReadOnly(false)]
            [Description("Specifies Initial Value of the RowNumber")]
            public string InitialValue
            {
                get
                {
                    //Get Initial value. In case of Null get Default value
                    object val = (initialValueProperty != null ? initialValueProperty.Value : RowNumberTransformation.GetDataTypeInitialValue(DTSOutputColumn.DataType));

                    //In case of DateTime, format as ShortDateString
                    if (initialValueProperty.Value is DateTime)
                        return ((DateTime)initialValueProperty.Value).ToShortDateString();
                    else
                        return val.ToString();
                }
                set
                {
                    if (initialValueProperty != null)
                    {
                        //Parse the string value according data type
                        initialValueProperty.Value = RowNumberTransformation.ParseInitialValue(value, DTSOutputColumn.DataType, Precision, Scale);
                        NotifyPropertyChanged("InitialValue");
                    }
                }
            }

            [Category("Row Number")]
            [Description("Specifies the data type to be used by the RowNumber transformation")]
            new public RowNumberDataTypeType DataType
            {
                get
                {
                    int type = (int)DTSOutputColumn.DataType;
                    RowNumberDataTypeType dt = (RowNumberDataTypeType)type;
                    return dt;
                }
                set
                {
                    int type = (int)DTSOutputColumn.DataType;
                    RowNumberDataTypeType dt = (RowNumberDataTypeType)type;
                    if (dt != value)
                    {
                        type = (int)value;
                        DataType colDataType = (DataType)type;
                        int precision = value == RowNumberDataTypeType.DT_NUMERIC ? 18 : 0;

                        DTSOutputColumn.SetDataTypeProperties(colDataType, 0, precision, 0, 0);

                        //Update the ReadOnly attribute on Precision and Scale Properties. Those should be ReadWrite ehwn DT_NUMERIC data type is selected
                        PropertyDescriptor descriptor = TypeDescriptor.GetProperties(this.GetType())["Precision"];
                        ReadOnlyAttribute attrib = (ReadOnlyAttribute)descriptor.Attributes[typeof(ReadOnlyAttribute)];
                        FieldInfo isReadOnly = attrib.GetType().GetField("isReadOnly", BindingFlags.NonPublic | BindingFlags.Instance);
                        isReadOnly.SetValue(attrib, value == RowNumberDataTypeType.DT_NUMERIC ? false : true);

                        descriptor = TypeDescriptor.GetProperties(this.GetType())["Scale"];
                        attrib = (ReadOnlyAttribute)descriptor.Attributes[typeof(ReadOnlyAttribute)];
                        isReadOnly = attrib.GetType().GetField("isReadOnly", BindingFlags.NonPublic | BindingFlags.Instance);
                        isReadOnly.SetValue(attrib, value == RowNumberDataTypeType.DT_NUMERIC ? false : true);

                        //When data type is changed, set the IncrementBy and InitialValue to default values
                        incrementByProperty.Value = RowNumberTransformation.GetDefaultDataTypeIncrement(colDataType);
                        initialValueProperty.Value = RowNumberTransformation.GetDataTypeInitialValue(colDataType);

                        NotifyPropertyChanged("DataType");
                        NotifyPropertyChanged("InitialValue");
                        NotifyPropertyChanged("IncrementBy");
                        NotifyPropertyChanged("DataType");
                        NotifyPropertyChanged("Precision");
                    }

                }

            }

            [Category("Row Number")]
            [ReadOnly(true)]
            [Description("Prescision of selected output column in case of DT_NUMERIC data type")]
            new public int Precision
            {
                get
                {
                    return base.Precision;
                }
                set
                {
                    if (DTSOutputColumn.Precision != value)
                    {
                        if (value < DTSOutputColumn.Scale)
                            throw new System.ArgumentException("Precision must be grater than scale");

                        //in case of DT_Numeric verify InitialValue and IncrementBy
                        if (DataType == RowNumberDataTypeType.DT_NUMERIC)
                        {
                            //in case of DT_Numeric Round the InitialValue and IncrementBy values
                            if (DataType == RowNumberDataTypeType.DT_NUMERIC)
                            {
                                decimal initValue = initialValueProperty != null && initialValueProperty.Value != null ? (decimal)initialValueProperty.Value : (decimal)0;
                                decimal incValue = incrementByProperty != null && incrementByProperty.Value != null ? (decimal)incrementByProperty.Value : (decimal)0;

                                decimal maxValue = (decimal)Math.Pow(10, (value - Scale) + 1);

                                if (initValue >= maxValue && initialValueProperty != null)
                                    initialValueProperty.Value = RowNumberTransformation.GetDataTypeInitialValue(DTSOutputColumn.DataType);
                                    
                                if (incValue >= maxValue && incrementByProperty != null)
                                    incrementByProperty.Value = RowNumberTransformation.GetDefaultDataTypeIncrement(DTSOutputColumn.DataType);
                            }
                        }

                        DTSOutputColumn.SetDataTypeProperties(DTSOutputColumn.DataType, 0, value, DTSOutputColumn.Scale, 0);
                        NotifyPropertyChanged("Precision");
                        NotifyPropertyChanged("Scale");
                        NotifyPropertyChanged("InitialValue");
                        NotifyPropertyChanged("IncrementBy");
                    }
                }
            }

            [Category("Row Number")]
            [ReadOnly(true)]
            [Description("Scale of selected output column in case of DT_NUMERIC data type")]
            new public int Scale
            {
                get
                {
                    return base.Scale;
                }
                set
                {
                    if (DTSOutputColumn.Scale != value)
                    {
                        if (value >= DTSOutputColumn.Precision)
                            throw new System.ArgumentException("Scale must be lower than Precision");

                        DTSOutputColumn.SetDataTypeProperties(DTSOutputColumn.DataType, 0, DTSOutputColumn.Precision, value, 0);

                        //in case of DT_Numeric Round the InitialValue and IncrementBy values
                        if (DataType == RowNumberDataTypeType.DT_NUMERIC)
                        {
                            decimal initValue = initialValueProperty != null && initialValueProperty.Value != null ? (decimal)initialValueProperty.Value : (decimal)0;
                            decimal incValue = incrementByProperty != null && incrementByProperty.Value != null ? (decimal)incrementByProperty.Value : (decimal)0;

                            initValue = Math.Round(initValue, value);
                            incValue = Math.Round(incValue, value);

                            if (initialValueProperty != null)
                                initialValueProperty.Value = initValue;
                            if (incrementByProperty != null)
                                incrementByProperty.Value = incValue;
                        }

                        NotifyPropertyChanged("Precision");
                        NotifyPropertyChanged("Scale");
                        NotifyPropertyChanged("InitialValue");
                        NotifyPropertyChanged("IncrementBy");
                    }
                }
            }

            /// <summary>
            /// Creates new Instance of the MetadataColumn
            /// </summary>
            /// <param name="col">IDTSOutputColumn to be associated wi the MetadataColumn</param>
            /// <returns>MetadataColumn</returns>
            public static RowNumberColumn CreateInstance(IDTSOutputColumn col)
            {
                return new RowNumberColumn(col);
            }

            /// <summary>
            /// Creates a new instance of the IDTSOutputColumn enccapsualted in the MetadataSolumn
            /// </summary>
            /// <param name="output">Output where the new OutputColumn should be created</param>
            /// <returns></returns>
            public static RowNumberColumn CreateNewInstance(IDTSOutput output)
            {
                IDTSOutputColumn col = output.OutputColumnCollection.New();
                col.Name = Resources.RowNumberDefaultColumnName + col.ID.ToString();

                RowNumberTransformation.SetRowNumberColumnProperties(col);

                RowNumberColumn c = new RowNumberColumn(col);
                return c;
            }
        }

        private IUIHelper uiHelper;
        protected IUIHelper UIHelper
        {
            get { return uiHelper; }
            private set { uiHelper = value; }
        }


        public RowNumberTransformationForm()
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
            this.Close();
        }

        private void HashColumnsTransformationForm_Load(object sender, EventArgs e)
        {
            UIHelper.FormInitialize(null, null, trvOutputColumns, RowNumberColumn.CreateInstance, o_PropertyChanged);
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
            //e.Node.Tag
            if (e.Node != null)
            {
                if (e.Node.Tag is RowNumberColumn)
                {
                    //Update the RedOnly for Prescision and Scale based ont he DataType
                    var val = ((RowNumberColumn)e.Node.Tag).DataType;
                    PropertyDescriptor descriptor = TypeDescriptor.GetProperties(e.Node.Tag.GetType())["Precision"];
                    ReadOnlyAttribute attrib = (ReadOnlyAttribute)descriptor.Attributes[typeof(ReadOnlyAttribute)];
                    FieldInfo isReadOnly = attrib.GetType().GetField("isReadOnly", BindingFlags.NonPublic | BindingFlags.Instance);
                    isReadOnly.SetValue(attrib, val == RowNumberColumn.RowNumberDataTypeType.DT_NUMERIC ? false : true);

                    descriptor = TypeDescriptor.GetProperties(e.Node.Tag.GetType())["Scale"];
                    attrib = (ReadOnlyAttribute)descriptor.Attributes[typeof(ReadOnlyAttribute)];
                    isReadOnly = attrib.GetType().GetField("isReadOnly", BindingFlags.NonPublic | BindingFlags.Instance);
                    isReadOnly.SetValue(attrib, val == RowNumberColumn.RowNumberDataTypeType.DT_NUMERIC ? false : true);  

                }

                propOutputColumn.SelectedObject = e.Node.Tag;
            }
            else
                propOutputColumn.SelectedObject = null;

            btnRemoveColumn.Enabled = e.Node.Tag is RowNumberColumn;
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
            UIHelper.AddOutputColumn(trvOutputColumns, RowNumberColumn.CreateNewInstance, o_PropertyChanged);
        }

        private void btnRemoveColumn_Click(object sender, EventArgs e)
        {
            UIHelper.RemoveOutputColumn(trvOutputColumns);
        }

        private void propOutputColumn_PropertyValueChanged(object s, PropertyValueChangedEventArgs e)
        {
            if (e.ChangedItem.Label == "DataType")
            {
                RowNumberColumn col = (RowNumberColumn)propOutputColumn.SelectedObject;
                RowNumberColumn.RowNumberDataTypeType oldType = (RowNumberColumn.RowNumberDataTypeType)e.OldValue;                
                if (oldType != col.DataType) //Reasign SelectedObject to reflect REaOnly Attirbute Change on Precision and Scale
                {
                    propOutputColumn.SelectedObject = propOutputColumn.SelectedObject;
                }
            }
            else if (propOutputColumn.SelectedObject is RowNumberColumn && ((RowNumberColumn)propOutputColumn.SelectedObject).DataType == RowNumberColumn.RowNumberDataTypeType.DT_NUMERIC &&
                (e.ChangedItem.Label == "Precision" || e.ChangedItem.Label == "Scale")) //reasign SelectedObject to reflect eventual chagne in the InitialValue or IncrementBy
            {
                propOutputColumn.SelectedObject = propOutputColumn.SelectedObject;
            }
        }
    }
}
