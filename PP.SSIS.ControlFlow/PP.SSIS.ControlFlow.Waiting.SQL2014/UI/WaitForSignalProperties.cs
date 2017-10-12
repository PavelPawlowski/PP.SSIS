using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;

namespace PP.SSIS.ControlFlow.Waiting.UI
{
    public class WaitForSignalProperties : TaskPropertyClass<WaitForSignal>
    {
        public WaitForSignalProperties()
        {

        }

        protected override void DoInitializeProperties()
        {
            CheckInterval = TaskControl.CheckInterval;
            SignalVariable = TaskControl.SignalVariable;
        }

        protected override void DoPersistProperties()
        {
            TaskControl.CheckInterval = CheckInterval;
            TaskControl.SignalVariable = SignalVariable;
        }

        private int sleepInterval = 2000;
        [Category("Wait For Signal")]
        [Description("Specifies interval in milliseconds between each chek for the Signal variable value")]
        [DefaultValue(2000)]
        public int CheckInterval
        {
            get { return sleepInterval; }
            set { sleepInterval = value; }
        }

        private string signalVariable;
        [Category("Wait For Signal")]
        [Description("Boolean variable which is checked for true value. When the vaiable returns true, then event is signalled and Components exits the Chek loop.")]
        [TypeConverter(typeof(SignalVariableTypeConverter))]
        public string SignalVariable
        {
            get { return signalVariable; }
            set { signalVariable = value; }
        }
    }

    public class SignalVariableTypeConverter : StringConverter
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
            return new StandardValuesCollection(WaitForSignalUI.SignalVariables);            
            //return new StandardValuesCollection(new string[] { "Val2", "Val1" });
        }
    }
}
