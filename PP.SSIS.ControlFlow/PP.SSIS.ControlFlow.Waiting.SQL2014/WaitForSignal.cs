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
        DisplayName="Wait For Signal Task", 
        Description="Waits for Signal with Predefined sleep intervals",
        TaskType="ExecutionControl",
        TaskContact="Pavel Pawlowski",
        RequiredProductLevel=DTSProductLevel.None,
        IconResource = "PP.SSIS.ControlFlow.Waiting.Resources.WaitFor.ico"
#if SQL2017
        ,UITypeName = "PP.SSIS.ControlFlow.Waiting.UI.WaitForSignalUI, PP.SSIS.ControlFlow.Waiting.SQL2017, Version=1.0.0.0, Culture=neutral, PublicKeyToken=c298537c023d14ce"
#endif
#if SQL2016
        ,UITypeName = "PP.SSIS.ControlFlow.Waiting.UI.WaitForSignalUI, PP.SSIS.ControlFlow.Waiting.SQL2016, Version=1.0.0.0, Culture=neutral, PublicKeyToken=828749bdd7917e4b"
#endif
#if SQL2014
        ,UITypeName = "PP.SSIS.ControlFlow.Waiting.UI.WaitForSignalUI, PP.SSIS.ControlFlow.Waiting, Version=1.0.0.0, Culture=neutral, PublicKeyToken=d958e388b0ffd524"
#endif
#if SQL2012
        ,UITypeName = "PP.SSIS.ControlFlow.Waiting.UI.WaitForSignalUI, PP.SSIS.ControlFlow.Waiting, Version=1.0.0.0, Culture=neutral, PublicKeyToken=3f1061c3fd17eb79"
#endif
        )
    ]
    public class WaitForSignal : Task, IDTSComponentPersist
    {

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
        public string SignalVariable
        {
            get { return signalVariable; }
            set { signalVariable = value; }
        }



        public override DTSExecResult Validate(Connections connections, VariableDispenser variableDispenser, IDTSComponentEvents componentEvents, IDTSLogging log)
        {
            if (sleepInterval <= 0)
            {
                componentEvents.FireError(0, Resources.WaitForSignalTaskName, Resources.ErrorSleepInterval, string.Empty, 0);
                return DTSExecResult.Failure;
            }

            if (string.IsNullOrEmpty(SignalVariable))
            {
                componentEvents.FireError(0, Resources.WaitForSignalTaskName, Resources.ErrorSignalVariableHasToBeSpecified, string.Empty, 0);
                return DTSExecResult.Failure;
            }

            if (!variableDispenser.Contains(SignalVariable))
            {
                componentEvents.FireError(0, Resources.WaitForSignalTaskName, string.Format(Resources.ErrorSignalVariableDoesNotExists, SignalVariable), string.Empty, 0);
                return DTSExecResult.Failure;
            }
            else
            {
                Variables vars = null;
                try
                {
                    variableDispenser.LockOneForRead(SignalVariable, ref vars);
                    if (vars != null && vars.Contains(SignalVariable))
                    {
                        Variable v = vars[SignalVariable];
                        if (v.DataType != TypeCode.Boolean)
                        {
                            componentEvents.FireError(0, Resources.WaitForSignalTaskName, string.Format(Resources.ErrorSignalVariableNotBoolean, SignalVariable), string.Empty, 0);
                            return DTSExecResult.Failure;
                        }
                        else if (v.Namespace != "User")
                        {
                            componentEvents.FireError(0, Resources.WaitForSignalTaskName, Resources.ErrorSignalWariableNotFromUser, string.Empty, 0);
                            return DTSExecResult.Failure;
                        }
                        
                    }
                    else
                    {
                        componentEvents.FireError(0, Resources.WaitForSignalTaskName, string.Format(Resources.ErrorLockSignalVariable, SignalVariable), string.Empty, 0);
                        return DTSExecResult.Failure;
                    }
                }
                finally
                {
                    if (vars != null && vars.Locked)
                        vars.Unlock();
                }
            }
            

            return base.Validate(connections, variableDispenser, componentEvents, log);
        }

        public override DTSExecResult Execute(Connections connections, VariableDispenser variableDispenser, IDTSComponentEvents componentEvents, IDTSLogging log, object transaction)
        {
            if (sleepInterval > 0)
            {
                bool failure;
                while (!GetSignalledState(variableDispenser, componentEvents, log, out failure) && !failure)
                {                    
                    System.Threading.Thread.Sleep(sleepInterval);                    
                }
                if (failure)
                    return DTSExecResult.Failure;
                else
                    return DTSExecResult.Success;
            }
            else if (sleepInterval <= 0)
            {
                componentEvents.FireError(0, Resources.SleepTaskName, Resources.ErrorSleepInterval, string.Empty, 0);
                return DTSExecResult.Failure;
            }

            return base.Execute(connections, variableDispenser, componentEvents, log, transaction);
        }

        private bool GetSignalledState(VariableDispenser variableDispenser, IDTSComponentEvents componentEvents, IDTSLogging log, out bool failure)
        {
            bool isSignalled = false;
            failure = false;

            if (string.IsNullOrEmpty(SignalVariable))
            {
                componentEvents.FireError(0, Resources.WaitForSignalTaskName, Resources.ErrorSignalVariableHasToBeSpecified, string.Empty, 0);
                failure = true;
            }

            if (!variableDispenser.Contains(SignalVariable))
            {
                componentEvents.FireError(0, Resources.WaitForSignalTaskName, string.Format(Resources.ErrorSignalVariableDoesNotExists, SignalVariable), string.Empty, 0);
                failure = true;
            }
            else
            {
                Variables vars = null;
                variableDispenser.LockOneForRead(SignalVariable, ref vars);
                if (vars != null && vars.Contains(SignalVariable))
                {
                    Variable v = vars[SignalVariable];
                    if (v.DataType != TypeCode.Boolean)
                    {
                        componentEvents.FireError(0, Resources.WaitForSignalTaskName, string.Format(Resources.ErrorSignalVariableNotBoolean, SignalVariable), string.Empty, 0);
                        failure = true;
                    }

                    isSignalled = (bool)v.Value;
                    vars.Unlock();
                }
                else
                {
                    componentEvents.FireError(0, Resources.WaitForSignalTaskName, string.Format(Resources.ErrorLockSignalVariable, SignalVariable), string.Empty, 0);
                    failure = true;
                }
            }

            return isSignalled;
        }

        public void SaveToXML(XmlDocument doc, IDTSInfoEvents infoEvents)
        {
            var data = doc.CreateElement("WaitForSignalData");
            data.SetAttribute("CheckInterval", CheckInterval.ToString());
            data.SetAttribute("SignalVariable", SignalVariable);
            doc.AppendChild(data);
        }

        public void LoadFromXML(XmlElement node, IDTSInfoEvents infoEvents)
        {
            if (node.Name == "InnerObject") //OldFormat
            {
                foreach (XmlElement child in node.ChildNodes)
                {
                    string val = child.GetAttribute("Value");

                    switch (child.Name)
                    {
                        case "CheckInterval":
                            CheckInterval = int.Parse(val);
                            break;
                        case "SignalVariable":
                            SignalVariable = val;
                            break;
                        default:
                            break;
                    }

                }
            }
            else if (node.Name == "WaitForSignalData") //NewFormat
            {
                CheckInterval = int.Parse(node.GetAttribute("CheckInterval"));
                SignalVariable = node.GetAttribute("SignalVariable");

            }
        }
    }
}
