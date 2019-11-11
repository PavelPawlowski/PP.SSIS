using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Xml;
using Microsoft.SqlServer.Dts.Runtime;
using PP.SSIS.ControlFlow.Waiting.Properties;

namespace PP.SSIS.ControlFlow.Waiting
{
    [DtsTask(
        DisplayName="Wait For Time Task", 
        Description="Sleeps the execution until specified time occurs",
        TaskType="ExecutionControl",
        TaskContact="Pavel Pawlowski",
        RequiredProductLevel=DTSProductLevel.None,
        IconResource = "PP.SSIS.ControlFlow.Waiting.Resources.WaitForTime.ico"
#if SQL2017
        , UITypeName = "PP.SSIS.ControlFlow.Waiting.UI.WaitForTimeUI, PP.SSIS.ControlFlow.Waiting.SQL2017, Version=1.0.0.0, Culture=neutral, PublicKeyToken=c298537c023d14ce"
#endif
#if SQL2016
        ,UITypeName = "PP.SSIS.ControlFlow.Waiting.UI.WaitForTimeUI, PP.SSIS.ControlFlow.Waiting.SQL2016, Version=1.0.0.0, Culture=neutral, PublicKeyToken=828749bdd7917e4b"
#endif
#if SQL2014
        , UITypeName = "PP.SSIS.ControlFlow.Waiting.UI.WaitForTimeUI, PP.SSIS.ControlFlow.Waiting, Version=1.0.0.0, Culture=neutral, PublicKeyToken=d958e388b0ffd524"
#endif
#if SQL2012
        , UITypeName = "PP.SSIS.ControlFlow.Waiting.UI.WaitForTimeUI, PP.SSIS.ControlFlow.Waiting, Version=1.0.0.0, Culture=neutral, PublicKeyToken=3f1061c3fd17eb79"
#endif
        )
    ]
    public class WaitForTime : Task, IDTSComponentPersist
    {

        /// <summary>
        /// Execution restul of the WaitForTime Task returned by the ExecutionValue property
        /// </summary>
        public enum WaitForTimeResult
        {
            /// <summary>
            /// There was an error during the execution
            /// </summary>
            Error               =-1,
            /// <summary>
            /// No Result - task was not executed
            /// </summary>
            None                = 0,
            /// <summary>
            /// The WaitTime has passed and the waiting as finnished
            /// </summary>
            Passed              = 1,
            /// <summary>
            /// The WaitTime has passed in the past and WaitForTime task was not waiting for it
            /// </summary>
            PassedInPast        = 2
        }

        /// <summary>
        /// Execution result of thw WaitForTime
        /// </summary>
        WaitForTimeResult result = WaitForTimeResult.None;

        /// <summary>
        /// Represents the time span of the whole day
        /// </summary>
        private readonly TimeSpan day = new TimeSpan(1, 0, 0, 0);

        private TimeSpan waitTime = (DateTime.Now - DateTime.Today);
        /// <summary>
        /// Gets or Sets the time for which the WaitForTime Task should wait
        /// </summary>        
        [Category("Wait For Time")]
        [Description("Specifies the Time until which the execution should be suspended and the Task should wait for it")]
        public TimeSpan WaitTime
        {
            get { return waitTime; }
            set { waitTime = value; }
        }

        private bool waitNextDayIfTimePassed = false;
        /// <summary>
        /// Gets or Sets whether to wait till next day if the WaitTime has already passedd at the time of execution
        /// </summary>
        [Category("Wait For Time")]
        [Description("In case the WaitTime has alredy passed at the time of package execution, specifeis whether the WaitForTime should wait for the WaitTIme of next day.")]
        [DefaultValue(false)]
        public bool WaitNextDayIfTimePassed
        {
            get { return waitNextDayIfTimePassed; }
            set { waitNextDayIfTimePassed = value; }
        }



        /// <summary>
        /// Validates the WaitForTime Task
        /// </summary>
        /// <param name="connections"></param>
        /// <param name="variableDispenser"></param>
        /// <param name="componentEvents"></param>
        /// <param name="log"></param>
        /// <returns></returns>
        public override DTSExecResult Validate(Connections connections, VariableDispenser variableDispenser, IDTSComponentEvents componentEvents, IDTSLogging log)
        {
            bool failure = false;            

            //Check if WaitTime is a correct value.. it has to be >= 0 and has to be less than 24 hours
            if (waitTime < TimeSpan.Zero || waitTime >= day)
            {
                failure = true;
                componentEvents.FireError(0, Resources.WaitForTimeTaskName, Resources.ErrorWaitForTime, string.Empty, 0);
            }

            if (failure)
                return DTSExecResult.Failure;
            else
                return base.Validate(connections, variableDispenser, componentEvents, log);
        }

        /// <summary>
        /// De-Serializes WaitForTime properties from XmlElement
        /// </summary>
        /// <param name="node"></param>
        /// <param name="infoEvents"></param>
        void IDTSComponentPersist.LoadFromXML(System.Xml.XmlElement node, IDTSInfoEvents infoEvents)
        {
            if (node.Name == "WaitForTimeData")
            {
                if (node.HasAttributes) //new format
                {
                    TimeSpan ts;
                    if (TimeSpan.TryParse(node.GetAttribute("waitTime"), out ts))
                        WaitTime = ts;
                    else
                        infoEvents.FireError(0, Resources.WaitForTimeTaskName, string.Format(Resources.ErrorCouldNotDeserializeProperty, "WaitTime", node.GetAttribute("checkType")), string.Empty, 0);

                    bool wnd;
                    if (bool.TryParse(node.GetAttribute("waitNextDayIfTimePassed"), out wnd))
                        WaitNextDayIfTimePassed = wnd;
                    else
                        infoEvents.FireError(0, Resources.WaitForTimeTaskName, string.Format(Resources.ErrorCouldNotDeserializeProperty, "WaitNextDayIfTimePassed", node.GetAttribute("waitNextDayIfTimePassed")), string.Empty, 0);
                }
                else //old format
                {
                    foreach (XmlNode nd in node.ChildNodes)
                    {
                        switch (nd.Name)
                        {
                            case "waitTime":
                                TimeSpan ts;
                                if (TimeSpan.TryParse(nd.InnerText, out ts))
                                    WaitTime = ts;
                                else
                                    infoEvents.FireError(0, Resources.WaitForTimeTaskName, string.Format(Resources.ErrorCouldNotDeserializeProperty, "WaitTime", nd.InnerText), string.Empty, 0);
                                break;
                            case "waitNextDayIfTimePassed":
                                bool wnd;
                                if (bool.TryParse(nd.InnerText, out wnd))
                                    WaitNextDayIfTimePassed = wnd;
                                else
                                    infoEvents.FireError(0, Resources.WaitForTimeTaskName, string.Format(Resources.ErrorCouldNotDeserializeProperty, "WaitNextDayIfTimePassed", nd.InnerText), string.Empty, 0);
                                break;
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Serializes the WaitForTime component into the DTSX package
        /// </summary>
        /// <param name="doc"></param>
        /// <param name="infoEvents"></param>
        void IDTSComponentPersist.SaveToXML(System.Xml.XmlDocument doc, IDTSInfoEvents infoEvents)
        {
            XmlElement data = doc.CreateElement("WaitForTimeData");
            doc.AppendChild(data);

            data.SetAttribute("waitTime", waitTime.ToString());
            data.SetAttribute("waitNextDayIfTimePassed", WaitNextDayIfTimePassed.ToString());
        }

        /// <summary>
        /// Gets the execution value of the WaitForTime Task
        /// </summary>
        public override object ExecutionValue
        {
            get
            {
                return result;
            }
        }


        /// <summary>
        /// Executes the WaitForTimeTask - invokes the actual waiting for specified time
        /// </summary>
        /// <param name="connections"></param>
        /// <param name="variableDispenser"></param>
        /// <param name="componentEvents"></param>
        /// <param name="log"></param>
        /// <param name="transaction"></param>
        /// <returns></returns>
        public override DTSExecResult Execute(Connections connections, VariableDispenser variableDispenser, IDTSComponentEvents componentEvents, IDTSLogging log, object transaction)
        {
            //Actual time at the time of execution
            DateTime now = DateTime.Now;

            result = WaitForTimeResult.None;

            //Check the WaitTime for correct value
            if (waitTime < TimeSpan.Zero || waitTime >= day)
            {
                componentEvents.FireError(0, Resources.WaitForTimeTaskName, Resources.ErrorWaitForTime, string.Empty, 0);
                result = WaitForTimeResult.Error;
                return DTSExecResult.Failure;
            }

            DateTime finalWaitTime = DateTime.Today.Add(waitTime);


            if (finalWaitTime < now)
            {
                if (!WaitNextDayIfTimePassed)
                    result = WaitForTimeResult.PassedInPast;
                else
                    finalWaitTime = finalWaitTime.AddDays(1);
            }

            while(result == WaitForTimeResult.None)
            {
                if (finalWaitTime <= DateTime.Now)
                {
                    result = WaitForTimeResult.Passed;
                }

                if (result == WaitForTimeResult.None)
                {
                    int sleepTime = (finalWaitTime - DateTime.Now).Milliseconds;
                    if (sleepTime <= 0)
                        sleepTime = 50;
                    System.Threading.Thread.Sleep(sleepTime);
                }
            }

            return base.Execute(connections, variableDispenser, componentEvents, log, transaction);
        }


    }
}
