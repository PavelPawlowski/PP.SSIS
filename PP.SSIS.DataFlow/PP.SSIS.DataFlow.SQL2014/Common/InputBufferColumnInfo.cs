// <copyright file="ColumnsToXmlTransformation.cs" company="Pavel Pawlowski">
// Copyright (c) 2014, 2015 All Right Reserved
//
// THIS CODE AND INFORMATION ARE PROVIDED "AS IS" WITHOUT WARRANTY OF ANY 
// KIND, EITHER EXPRESSED OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE
// IMPLIED WARRANTIES OF MERCHANTABILITY AND/OR FITNESS FOR A
// PARTICULAR PURPOSE.
//
// </copyright>
// <author>Pavel Pawlowski</author>
// <summary>Contains implementaiton of the ColumnsToXmlTransformation</summary>

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.SqlServer.Dts.Pipeline;
using Microsoft.SqlServer.Dts.Pipeline.Wrapper;
using Microsoft.SqlServer.Dts.Runtime.Wrapper;
using System.Xml.Linq;
using System.Globalization;
using PP.SSIS.DataFlow.Properties;

#if SQL2008 || SQL2008R2 || SQL2012 || SQL2014 || SQL2016 || SQL2017 || SQL2019
using IDTSOutput = Microsoft.SqlServer.Dts.Pipeline.Wrapper.IDTSOutput100;
using IDTSCustomProperty = Microsoft.SqlServer.Dts.Pipeline.Wrapper.IDTSCustomProperty100;
using IDTSOutputColumn = Microsoft.SqlServer.Dts.Pipeline.Wrapper.IDTSOutputColumn100;
using IDTSInput = Microsoft.SqlServer.Dts.Pipeline.Wrapper.IDTSInput100;
using IDTSInputColumn = Microsoft.SqlServer.Dts.Pipeline.Wrapper.IDTSInputColumn100;
using IDTSVirtualInput = Microsoft.SqlServer.Dts.Pipeline.Wrapper.IDTSVirtualInput100;
using IDTSVirtualInputColumn = Microsoft.SqlServer.Dts.Pipeline.Wrapper.IDTSVirtualInputColumn100;
#endif
namespace PP.SSIS.DataFlow.Common
{
    /// <summary>
    /// Rrepresent Buffer Column Information for internal usage.
    /// </summary>
    public struct InputBufferColumnInfo
    {
        /// <summary>
        /// Creates instance of BufferColumnInfo
        /// </summary>
        /// <param name="index">Index of the Input Buffer Column</param>
        /// <param name="name">Name of the Input Buffer Column</param>
        /// <param name="id">ID of the Input Buffer Column</param>
        /// <param name="lineageID">LineageID of the Input Buffer Column</param>
        public InputBufferColumnInfo(int index, string name, int id, int lineageID, int sortOrder) :
            this(index, name, id, lineageID, sortOrder, DataType.DT_EMPTY, 0, 0, 0)
        {

        }

        /// <summary>
        /// Creates instance of BufferColumnInfo
        /// </summary>
        /// <param name="index">Index of the Input Buffer Column</param>
        /// <param name="name">Name of the Input Buffer Column</param>
        /// <param name="id">ID of the Input Buffer Column</param>
        /// <param name="lineageID">LineageID of the Input Buffer Column</param>
        public InputBufferColumnInfo(int index, string name, int id, int lineageID, int sortOrder, DataType dataType, int length, int precision, int scale)
        {
            Index = index;
            Name = name;
            ID = id;
            LineageID = lineageID;
            SortOrder = sortOrder;
            DataType = dataType;
            Length = length;
            Precision = precision;
            Scale = scale;
        }

        /// <summary>
        /// Index of the Input Buffer Column
        /// </summary>
        public int Index;
        /// <summary>
        /// name of the Input Buffer Column
        /// </summary>
        public string Name;
        /// <summary>
        /// ID Of the Input Buffer Column
        /// </summary>
        public int ID;
        /// <summary>
        /// LineageID of the Input Buffer Column
        /// </summary>
        public int LineageID;
        /// <summary>
        /// Sort Order of the InputColumn
        /// </summary>
        public int SortOrder;

        /// <summary>
        /// DataType of the InputColumns
        /// </summary>
        public DataType DataType;

        /// <summary>
        /// Precision of the Input Column
        /// </summary>
        public int Precision;

        /// <summary>
        /// Scale of the Input Column
        /// </summary>
        public int Scale;

        /// <summary>
        /// Length of the Input column in bytes
        /// </summary>
        public int Length;

    }
}
