// <copyright file="FormOutputColumn.cs" company="Pavel Pawlowski">
// Copyright (c) 2014, 2015 All Right Reserved
//
// THIS CODE AND INFORMATION ARE PROVIDED "AS IS" WITHOUT WARRANTY OF ANY 
// KIND, EITHER EXPRESSED OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE
// IMPLIED WARRANTIES OF MERCHANTABILITY AND/OR FITNESS FOR A
// PARTICULAR PURPOSE.
//
// </copyright>
// <author>Pavel Pawlowski</author>
// <summary>Contains implementation of the FormOutputColumn</summary>
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.SqlServer.Dts.Pipeline.Wrapper;
using PP.SSIS.DataFlow.Properties;
using System.ComponentModel;
using Microsoft.SqlServer.Dts.Runtime.Wrapper;
using System.Runtime.CompilerServices;
using System.Globalization;
using System.Reflection;
using System.Windows.Forms;


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
    /// <summary>
    /// Encapsulates IDTSOutputColumn for the purpose of GUI Forms to allow modifications in the PropertyGrid
    /// </summary>
    public class FormOutputColumn : INotifyPropertyChanged, INameProvider
    {
        private Guid guid = Guid.NewGuid();
        [Browsable(false)]
        public Guid Guid
        {
            get { return guid; }
        }

        public FormOutputColumn(IDTSOutputColumn col)
        {
            DTSOutputColumn = col;
        }

        private IDTSOutputColumn dtsOutputColumn;
        /// <summary>
        /// Gets or Sets associated IDTSOutputColumn
        /// </summary>
        [Browsable(false)]
        public IDTSOutputColumn DTSOutputColumn
        {
            get { return dtsOutputColumn; }
            private set
            {
                if (dtsOutputColumn != value)
                {
                    dtsOutputColumn = value;
                    DTSOutputColumnSetProcessing();
                    NotifyPropertyChanged("DTSOutputColumn");
                }
            }
        }

        /// <summary>
        /// Process IDTSOutputColumn just after axsociation with FormOutputColumn
        /// </summary>
        protected virtual void DTSOutputColumnSetProcessing()
        {

        }

        /// <summary>
        /// Gets or Sets Name of the OutputColumn
        /// </summary>
        [Category("Output Column")]
        [Description("Name of the Output Column")]
        public virtual string Name
        {
            get { return DTSOutputColumn.Name; }
            set
            {
                if (DTSOutputColumn.Name != value)
                {
                    DTSOutputColumn.Name = value;

                    NotifyPropertyChanged("Name");
                }
            }
        }

        /// <summary>
        /// Gets or Sets Description of the OutputColumn
        /// </summary>
        [Category("Output Column")]
        [Description("Description of selected output column")]
        public virtual  string Description
        {
            get { return DTSOutputColumn.Description; }
            set
            {
                if (DTSOutputColumn.Description != value)
                {
                    DTSOutputColumn.Description = value;
                    NotifyPropertyChanged("Description");
                }
            }
        }
        /// <summary>
        /// Gets ID of the OutputColumn
        /// </summary>
        [Category("Output Column")]
        [ReadOnly(true)]
        public virtual int ID
        {
            get { return DTSOutputColumn.ID; }
        }

        /// <summary>
        /// Gets LineageID of the OutputColumn
        /// </summary>
        [Category("Output Column")]
        [ReadOnly(true)]
        public virtual int LineageID
        {
            get { return DTSOutputColumn.LineageID; }
        }

        /// <summary>
        /// Gets DataType of the OutputColumn
        /// </summary>
        [Category("Output Column Data Type")]
        [ReadOnly(true)]
        [Description("The data type of selected output column")]
        public virtual DataType DataType
        {
            get { return DTSOutputColumn.DataType; }
        }

        /// <summary>
        /// Gets DataLength of the OutputColumn
        /// </summary>
        [Category("Output Column Data Type")]
        [ReadOnly(true)]
        [Description("Data length of selected output column")]
        public virtual int DataLength
        {
            get { return DTSOutputColumn.Length; }
            set
            {
                DTSOutputColumn.SetDataTypeProperties(DTSOutputColumn.DataType, value, DTSOutputColumn.Precision, DTSOutputColumn.Scale, DTSOutputColumn.CodePage);
            }
        }

        /// <summary>
        /// Gets Precision of the OutputColumn
        /// </summary>
        [Category("Output Column Data Type")]
        [ReadOnly(true)]
        [Description("Prescision of selected output column")]
        public virtual int Precision
        {
            get { return DTSOutputColumn.Precision; }
            set { }
        }

        /// <summary>
        /// Gets Scale of the OutputColumn
        /// </summary>
        [Category("Output Column Data Type")]
        [ReadOnly(true)]
        [Description("Scale of selected output column")]
        public virtual int Scale
        {
            get { return DTSOutputColumn.Scale; }
            set { }
        }

        [Category("Output Column Data Type")]
        [ReadOnly(true)]
        [Browsable(true)]
        [Description("Code Page of selected output column")]
        [TypeConverter(typeof(CodePageConverter))]
        public virtual int CodePage
        {
            get { return DTSOutputColumn.CodePage; }
            set { }
        }

        [Browsable(false)]
        public int OutputID { get; set; }

        /// <summary>
        /// Gets Index of the OutputColumn in the OutputColumns Collection
        /// </summary>
        [Browsable(false)]
        public int Index { get; set; }

        TreeNode _associatedTreeNode;
        [Browsable(false)]
        public TreeNode AssociatedTreeNode
        {
            get
            {
                return _associatedTreeNode;
            }
            set
            {
                _associatedTreeNode = value;
                NotifyPropertyChanged("AssociatedTreeNode");
            }
        }


        /// <summary>
        /// Updates the ReadOnlyAttribute on specified property
        /// </summary>
        /// <param name="propertyName">Name of the property to set the ReadOnly attribute</param>
        /// <param name="status">Status fo the ReadOnly property</param>
        protected void UpdateReadOnlyProperty(string propertyName, bool status)
        {
            try
            {
                PropertyDescriptor descriptor = TypeDescriptor.GetProperties(this.GetType())[propertyName];
                ReadOnlyAttribute attrib = (ReadOnlyAttribute)descriptor.Attributes[typeof(ReadOnlyAttribute)];
                FieldInfo isReadOnly = attrib.GetType().GetField("isReadOnly", BindingFlags.NonPublic | BindingFlags.Instance);
                if (isReadOnly != null)
                    isReadOnly.SetValue(attrib, status);
            }
            catch { }
        }


        /// <summary>
        /// Type converter for CodePage int value to string band back for the Property Grid purposes
        /// </summary>
        public class CodePageConverter : TypeConverter
        {
            private struct EncodingInfoData
            {
                public EncodingInfoData(int codePage, string name, string displayName)
                {
                    CodePage = codePage;
                    Name = name;
                    DisplayName = displayName;
                    FullName = string.Format("{0} ({1})", codePage, displayName);
                }
                public int CodePage;
                public string Name;
                public string DisplayName;
                public string FullName;

                public override string ToString()
                {
                    return FullName;
                }
            }

            List<EncodingInfoData> encodings;

            public CodePageConverter()
            {
                var encList = Encoding.GetEncodings();
                encodings = new List<EncodingInfoData>(encList.Length);

                foreach (EncodingInfo ei in encList)
                {
                    encodings.Add(new EncodingInfoData(ei.CodePage, ei.Name, ei.DisplayName));
                }
            }
            public override bool GetStandardValuesSupported(ITypeDescriptorContext context)
            {
                return true;
            }

            public override StandardValuesCollection GetStandardValues(ITypeDescriptorContext context)
            {
                StandardValuesCollection svc = new StandardValuesCollection(encodings.ConvertAll<string>(e => e.FullName));

                return svc;
            }

            public override bool CanConvertFrom(ITypeDescriptorContext context, Type sourceType)
            {
                if (sourceType == typeof(string))
                {
                    return true;
                }

                return base.CanConvertFrom(context, sourceType);
            }

            public override object ConvertFrom(ITypeDescriptorContext context, CultureInfo culture, object value)
            {
                if (value is string)
                {
                    string val = (string)value;
                    int codePage = int.MinValue;
                    int.TryParse(val, out codePage);

                    foreach (EncodingInfoData ei in encodings)
                    {
                        if (val == ei.FullName || val == ei.DisplayName || val == ei.Name || codePage == ei.CodePage)
                            return ei.CodePage;
                    }

                }
                return base.ConvertFrom(context, culture, value);
            }

            public override bool CanConvertTo(ITypeDescriptorContext context, Type destinationType)
            {
                if (destinationType == typeof(string))
                    return true;
                else
                    return base.CanConvertTo(context, destinationType);
            }

            public override object ConvertTo(ITypeDescriptorContext context, CultureInfo culture, object value, Type destinationType)
            {
                if (destinationType == typeof(string) && value is int)
                {
                    int enc = (int)value;
                    foreach (EncodingInfoData ei in encodings)
                    {
                        if (enc == ei.CodePage)
                            return ei.FullName;
                    }

                }
                return base.ConvertTo(context, culture, value, destinationType);
            }
        }


        protected void NotifyPropertyChanged(String propertyName)
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
            }
        }


        [Browsable(false)]
        public IUIHelper UIHelper { get; set; }

        public event PropertyChangedEventHandler PropertyChanged;
    }

    /// <summary>
    /// Delegate for Creation of new FormOutputColumns
    /// </summary>
    /// <param name="outputColumn">IDTSOutputColumn to be associated with the new FormOutputColumn</param>
    /// <returns>FormOutputColumn with associated IDTSOutputColumn</returns>
    public delegate FormOutputColumn OutputColumnCreator(IDTSOutputColumn outputColumn);

    /// <summary>
    /// Delegate for Creation of the FormOutputColumn and new IDTSOutputColumn assocaited with that FormOutputColumn
    /// </summary>
    /// <param name="output">IDTSOutput where the new IDTSOutputColumn shoudl be created</param>
    /// <returns>FormOutputColumn with associatged IDTSOutputColumn</returns>
    public delegate FormOutputColumn NewOutputColumnCreator(IDTSOutput output);

    /// <summary>
    /// Delegate for Creation of the FormOutputColumn and new IDTSOutputColumn assocaited with that FormOutputColumn
    /// </summary>
    /// <param name="output">IDTSOutput where the new IDTSOutputColumn shoudl be created</param>
    /// <returns>FormOutputColumn with associatged IDTSOutputColumn</returns>
    public delegate TFormOutputColumn NewOutputColumnCreator<TFormOutputColumn>(IDTSOutput output) where TFormOutputColumn : FormOutputColumn;

    /// <summary>
    /// Delegate for Creation of the FormOutputColumn and new IDTSOutputColumn assocaited with that FormOutputColumn
    /// </summary>
    /// <param name="output">IDTSOutput where the new IDTSOutputColumn shoudl be created</param>
    /// <param name="uiHelper">IUIHelper to be available during creation of the new Output Column</param>
    /// <returns>FormOutputColumn with associatged IDTSOutputColumn</returns>
    public delegate FormOutputColumn NewUiOutputColumnCreator(IDTSOutput output, IUIHelper uiHelper);

}
