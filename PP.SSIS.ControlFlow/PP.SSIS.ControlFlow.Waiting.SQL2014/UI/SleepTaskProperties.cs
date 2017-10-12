using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;

namespace PP.SSIS.ControlFlow.Waiting.UI
{
    public class SleepTaskProperties : TaskPropertyClass<SleepTask>
    {
        public SleepTaskProperties()
        {

        }

        protected override void DoInitializeProperties()
        {
            SleepInterval = TaskControl.SleepInterval;
        }

        protected override void DoPersistProperties()
        {
            TaskControl.SleepInterval = SleepInterval;
        }

        /// <summary>
        /// Getw or Sets the Sleep Interval in milliseconds
        /// </summary>
        [Category("Sleep Task")]
        [Description("Sleeps the thread execution for specified amount of milliseconds")]
        [DefaultValue(1000)]
        public int SleepInterval { get; set; }
    }

}
