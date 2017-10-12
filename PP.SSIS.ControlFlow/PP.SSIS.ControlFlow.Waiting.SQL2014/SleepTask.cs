using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using Microsoft.SqlServer.Dts.Runtime;
using PP.SSIS.ControlFlow.Waiting.Properties;

namespace PP.SSIS.ControlFlow.Waiting
{
    [DtsTask(
        DisplayName="Sleep Task", 
        Description="Sleeps current execution for specified duration in milliseconds",
        TaskType="ExecutionControl",
        TaskContact="Pavel Pawlowski",
        RequiredProductLevel=DTSProductLevel.None,
        IconResource = "PP.SSIS.ControlFlow.Waiting.Resources.Sleep.ico"
#if SQL2017
        ,UITypeName = "PP.SSIS.ControlFlow.Waiting.UI.SleepTaskUI, PP.SSIS.ControlFlow.Waiting.SQL2017, Version=1.0.0.0, Culture=neutral, PublicKeyToken=c298537c023d14ce"
#endif
#if SQL2016
        ,UITypeName = "PP.SSIS.ControlFlow.Waiting.UI.SleepTaskUI, PP.SSIS.ControlFlow.Waiting.SQL2016, Version=1.0.0.0, Culture=neutral, PublicKeyToken=828749bdd7917e4b"
#endif
#if SQL2014
        ,UITypeName = "PP.SSIS.ControlFlow.Waiting.UI.SleepTaskUI, PP.SSIS.ControlFlow.Waiting, Version=1.0.0.0, Culture=neutral, PublicKeyToken=d958e388b0ffd524"
#endif
#if SQL2012
        ,UITypeName = "PP.SSIS.ControlFlow.Waiting.UI.SleepTaskUI, PP.SSIS.ControlFlow.Waiting, Version=1.0.0.0, Culture=neutral, PublicKeyToken=3f1061c3fd17eb79"
#endif
        )
    ]
    public class SleepTask : Task
    {

        private int sleepInterval = 1000;
        /// <summary>
        /// Getw or Sets the Sleep Interval in milliseconds
        /// </summary>
        [Category("Sleep Task")]
        [Description("Sleeps the thread execution for specified amount of milliseconds")]
        [DefaultValue(1000)]
        public int SleepInterval
        {
            get { return sleepInterval; }
            set { sleepInterval = value; }
        }

        /// <summary>
        /// Validates the Task
        /// </summary>
        /// <param name="connections"></param>
        /// <param name="variableDispenser"></param>
        /// <param name="componentEvents"></param>
        /// <param name="log"></param>
        /// <returns></returns>
        public override DTSExecResult Validate(Connections connections, VariableDispenser variableDispenser, IDTSComponentEvents componentEvents, IDTSLogging log)
        {
            if (sleepInterval <= 0)
            {
                componentEvents.FireError(0, Resources.SleepTaskName, Resources.ErrorSleepInterval, string.Empty, 0);
                return DTSExecResult.Failure;
            }

            return base.Validate(connections, variableDispenser, componentEvents, log);
        }

        /// <summary>
        /// Executes tasks. Does the actual sleep
        /// </summary>
        /// <param name="connections"></param>
        /// <param name="variableDispenser"></param>
        /// <param name="componentEvents"></param>
        /// <param name="log"></param>
        /// <param name="transaction"></param>
        /// <returns></returns>
        public override DTSExecResult Execute(Connections connections, VariableDispenser variableDispenser, IDTSComponentEvents componentEvents, IDTSLogging log, object transaction)
        {
            if (sleepInterval > 0)
            {
                System.Threading.Thread.Sleep(sleepInterval);
            }
            else
            {
                componentEvents.FireError(0, Resources.SleepTaskName, Resources.ErrorSleepInterval, string.Empty, 0);
                return DTSExecResult.Failure;
            }

            return base.Execute(connections, variableDispenser, componentEvents, log, transaction);
        }
    }


	

}
