using Microsoft.SqlServer.Dts.Runtime.Wrapper;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;

#if SQL2008 || SQL2008R2 || SQL2012 || SQL2014 || SQL2016 || SQL2017 || SQL2019
using IDTSVirtualInputColumn = Microsoft.SqlServer.Dts.Pipeline.Wrapper.IDTSVirtualInputColumn100;
#endif

namespace PP.SSIS.DataFlow.UI.Common
{
    /// <summary>
    /// Provides Information about available Input Columns for the InputColumnTypeConverter
    /// </summary>
    public class ConvertMetadataInputColumn : IEquatable<ConvertMetadataInputColumn>
    {
        public static readonly ConvertMetadataInputColumn NotSpecifiedInputColumn = new ConvertMetadataInputColumn(null, 0, "<< Not Specified >>", string.Empty, "<< Not Specified >>");

        IDTSVirtualInputColumn _vcol;

        public ConvertMetadataInputColumn()
            : this(null, 0, string.Empty, string.Empty, string.Empty)
        {

        }

        public ConvertMetadataInputColumn(IDTSVirtualInputColumn vcol)
            : this(vcol, vcol.Name)
        {

        }

        public ConvertMetadataInputColumn(IDTSVirtualInputColumn vcol, string displayname)
            : this(vcol, vcol.LineageID, vcol.Name, vcol.UpstreamComponentName, displayname)
        {

        }

        public ConvertMetadataInputColumn(IDTSVirtualInputColumn vcol, int lineageID, string name, string upstreamComponentName, string displayName)
        {
            _vcol = vcol;
            LineageID = lineageID;
            Name = name;
            UpstreamComponentName = upstreamComponentName;
            DisplayName = displayName;
        }
        /// <summary>
        /// LineageID of the InputColumn
        /// </summary>
        public int LineageID { get; private set; }
        /// <summary>
        /// Name of the InputColumn
        /// </summary>
        public string Name { get; private set; }
        /// <summary>
        /// InputColumn Upstream Component Name
        /// </summary>
        public string UpstreamComponentName { get; private set; }
        /// <summary>
        /// Display Name for the InputColumn
        /// </summary>
        public string DisplayName { get; set; }
        /// <summary>
        /// VirtualInputColumn
        /// </summary>
        public IDTSVirtualInputColumn VirtualColumn
        {
            get { return _vcol; }
        }

        public override string ToString()
        {
            return DisplayName ?? Name;
        }
        /// <summary>
        /// DataType of the VirtualInputColumn
        /// </summary>
        public DataType DataType
        {
            get
            {
                if (VirtualColumn == null)
                    return DataType.DT_EMPTY;
                else
                    return VirtualColumn.DataType;
            }
        }
        /// <summary>
        /// Data Length of the VirtualInputColumn
        /// </summary>
        public int DataLength
        {
            get
            {
                return VirtualColumn != null ? VirtualColumn.Length : 0;
            }
        }

        public bool Equals(ConvertMetadataInputColumn other)
        {
            if (other == null || (this.VirtualColumn != null && other.VirtualColumn == null) || (this.VirtualColumn == null && other.VirtualColumn != null))
                return false;
            else if (this.VirtualColumn == null && other.VirtualColumn == null)
                return this.LineageID.Equals(other.LineageID);
            else
                return VirtualColumn.Equals(other.VirtualColumn);

        }

    }
}
