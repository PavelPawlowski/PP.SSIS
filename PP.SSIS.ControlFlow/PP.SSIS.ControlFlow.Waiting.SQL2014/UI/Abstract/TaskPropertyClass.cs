using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.SqlServer.Dts.Runtime;

namespace PP.SSIS.ControlFlow.Waiting.UI
{
    public abstract class TaskPropertyClass<TTask> where TTask : Task
    {
        protected TTask TaskControl { get; private set; }

        public TaskPropertyClass()
        {

        }

        internal void SetTaskControl(TTask taskControl)
        {
            TaskControl = taskControl;
        }



        protected abstract void DoInitializeProperties();


        public void InitializeProperties()
        {
            DoInitializeProperties();
        }

        protected abstract void DoPersistProperties();

        public void PersistProperties()
        {
            DoPersistProperties();
        }
        
    }
}
