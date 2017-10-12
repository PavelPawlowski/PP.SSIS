using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace PP.SSIS.DataFlow.UI
{
    /// <summary>
    /// Interface to provide Name and Unique identification inthe GUI TreeView components
    /// As in the TreeView are combined Outputs with OutputColumsn this allows easy identyification and name providing
    /// </summary>
    public interface INameProvider
    {
        /// <summary>
        /// GUID which ensures unique identification of the object in among TreeNodes
        /// </summary>
        Guid Guid { get; }
        /// <summary>
        /// Gets Name of the object implementing the INameProvider
        /// </summary>
        string Name { get; set; }
    }
}
