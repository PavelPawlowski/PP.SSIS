using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.SqlServer.Dts.Runtime;

namespace PP.SSIS.ControlFlow.Waiting.UI
{
    public class WaitForSignalUI : TaskUIGeneric<WaitForSignal, WaitForSignalProperties>
    {
        private static List<string> _signalVariables;

        public static ReadOnlyCollection<string> SignalVariables
        {
            get { return _signalVariables.AsReadOnly(); }
        }


        public override void Initialize(Microsoft.SqlServer.Dts.Runtime.TaskHost taskHost, IServiceProvider serviceProvider)
        {
            base.Initialize(taskHost, serviceProvider);

            _signalVariables = new List<string>();

            foreach (Variable v in TaskHost.Variables)
            {
                if (v.DataType == TypeCode.Boolean && v.Namespace == "User")
                    _signalVariables.Add(v.QualifiedName);
            }
        }
        

    }
}
