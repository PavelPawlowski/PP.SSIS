using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;

namespace PP.SSIS.ControlFlow.Waiting.UI
{
    public class WaitForTimeProperties : TaskPropertyClass<WaitForTime>
    {
        public WaitForTimeProperties()
        {

        }

        protected override void DoInitializeProperties()
        {
            WaitTime = TaskControl.WaitTime;
            WaitNextDayIfTimePassed = TaskControl.WaitNextDayIfTimePassed;
        }

        protected override void DoPersistProperties()
        {
            TaskControl.WaitTime = WaitTime;
            TaskControl.WaitNextDayIfTimePassed = WaitNextDayIfTimePassed;
        }

        [Category("Wait For Time")]
        [Description("Specifies the Time until which the execution should be suspended and the Task should wait for it")]
        public TimeSpan WaitTime { get; set; }

        /// <summary>
        /// Gets or Sets whether to wait till next day if the WaitTime has already passedd at the time of execution
        /// </summary>
        [Category("Wait For Time")]
        [Description("In case the WaitTime has alredy passed at the time of package execution, specifeis whether the WaitForTime should wait for the WaitTIme of next day.")]
        [DefaultValue(false)]
        public bool WaitNextDayIfTimePassed { get; set; }
    }

}
