// <copyright file="FormInputColumn.cs" company="Pavel Pawlowski">
// Copyright (c) 2014, 2015 All Right Reserved
//
// THIS CODE AND INFORMATION ARE PROVIDED "AS IS" WITHOUT WARRANTY OF ANY 
// KIND, EITHER EXPRESSED OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE
// IMPLIED WARRANTIES OF MERCHANTABILITY AND/OR FITNESS FOR A
// PARTICULAR PURPOSE.
//
// </copyright>
// <author>Pavel Pawlowski</author>
// <summary>Contains definition of FormInputColumn</summary>
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.SqlServer.Dts.Pipeline.Wrapper;
using PP.SSIS.DataFlow.Properties;

#if SQL2008 || SQL2008R2 || SQL2012 || SQL2014 || SQL2016 || SQL2017 || SQL2019
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
    /// Encapsulates InputColumn for usage in the GUI forms for selection nad propreties handling
    /// </summary>
    public class FormInputColumn
    {
        public IDTSVirtualInputColumn DTSVirtualColumn { get; private set; }
        private IDTSInputColumn dtsInputColumn;
        private IDTSCustomProperty sortOrderProp;

        /// <summary>
        /// Gets or Sets the Associated InputColumn
        /// </summary>
        public IDTSInputColumn DTSInputColumn
        {
            get { return dtsInputColumn; }
            set
            {
                if (dtsInputColumn != value)
                {
                    dtsInputColumn = value;
                    if (dtsInputColumn != null)
                    {
                        try
                        {
                            sortOrderProp = dtsInputColumn.CustomPropertyCollection[Resources.InputSortOrderPropertyName];
                        }
                        catch
                        {
                            sortOrderProp = null;
                        }
                    }
                    else
                    {
                        sortOrderProp = null;
                    }
                }
            }
        }

        public FormInputColumn(IDTSVirtualInputColumn vcol, int index)
        {
            DTSVirtualColumn = vcol;
            displayName = vcol.Name;
            Index = index;
        }
        /// <summary>
        /// Gets ID of the INputColumn
        /// </summary>
        public int ID { get { return DTSVirtualColumn.ID; } }
        /// <summary>
        /// Gets LinateID of the InputColumn
        /// </summary>
        public int LineageID { get { return DTSVirtualColumn.LineageID; } }
        /// <summary>
        /// Gets Name of the InputColumn
        /// </summary>
        public string Name { get { return DTSVirtualColumn.Name; } }
        /// <summary>
        /// Gets Name of the InputColumn Upstream Component
        /// </summary>
        public string SourceComponent { get { return DTSVirtualColumn.SourceComponent; } }

        private string displayName;
        /// <summary>
        /// Gets or Sets DisplayName of the FormInputColumn (DisplayName is updated to include Upstream ComponentName in case multiple Columns with the same name exists)
        /// </summary>
        public string DisplayName
        {
            get { return displayName; }
            set { displayName = value; }
        }

        /// <summary>
        /// Gets or Sets Index of the InputColumn in the InputColumCollection
        /// </summary>
        public int Index { get; set; }

        /// <summary>
        /// Gets UsageType of the InputColumn
        /// </summary>
        public DTSUsageType UsageType
        {
            get
            {                
                return DTSVirtualColumn.UsageType;
            }
        }

        /// <summary>
        /// Gets or Sets the SortOrder property fo the INputColumn
        /// </summary>
        public int SortOrder
        {
            get
            {
                if (sortOrderProp != null)
                    return (int)sortOrderProp.Value;
                else
                    return 0;
            }
            set
            {
                if (sortOrderProp != null)
                {
                    sortOrderProp.Value = value;
                }
            }
        }

        /// <summary>
        /// Gets whether the InputColumn is Selecte
        /// </summary>
        public virtual bool IsSelected
        {
            get
            {
                if (DTSInputColumn != null)
                    return true;
                else
                    return false;
            }
        }

        public override string ToString()
        {
            return DisplayName;
        }


        /// <summary>
        /// Get Maximum Input Sort Order
        /// </summary>
        /// <returns>Maximu Sort Order of Input Columns</returns>
        public static int GetMaxInputColumnsSortOrder(IDTSInput input)
        {
            int maxOrder = int.MinValue;

            foreach (IDTSInputColumn col in input.InputColumnCollection)
            {
                foreach (IDTSCustomProperty prop in col.CustomPropertyCollection)
                {
                    if (prop.Name == Resources.InputSortOrderPropertyName)
                    {
                        int order = prop.Value != null ? (int)prop.Value : 0;
                        if (order > maxOrder)
                            maxOrder = order;
                        break;
                    }
                }
            }
            if (maxOrder == int.MinValue)
                maxOrder = 0;

            return maxOrder;
        }

    }
}
