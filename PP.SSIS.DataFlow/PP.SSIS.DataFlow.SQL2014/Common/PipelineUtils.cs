using Microsoft.SqlServer.Dts.Pipeline;
using System.ComponentModel;

namespace PP.SSIS.DataFlow.Common
{
    /// <summary>
    /// Contains generel support methods for Pipeline Components handling
    /// </summary>
    class PipelineUtils
    {
        /// <summary>
        /// Gets the Version of the Pipeline Component based ont he DTSPipelineComponentAttribute
        /// </summary>
        /// <param name="comp">Pipeline component to return the version</param>
        /// <returns>Version of the provided pipeline component</returns>
        public static int GetPipelineComponentVersion(PipelineComponent comp)
        {
            int version = 0;
            if (comp != null)
            {
                var attribs = TypeDescriptor.GetAttributes(comp.GetType());
                DtsPipelineComponentAttribute pc = (DtsPipelineComponentAttribute)attribs[typeof(DtsPipelineComponentAttribute)];
                System.Reflection.PropertyInfo pi = pc.GetType().GetProperty("CurrentVersion");
                if (pi != null)
                {
                    
                    version = (int)pi.GetValue(pc, null);
                }
            }
            return version;
        }

    }
}
