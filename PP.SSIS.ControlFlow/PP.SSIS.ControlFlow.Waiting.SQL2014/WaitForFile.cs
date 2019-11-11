using Microsoft.SqlServer.Dts.Runtime;
using PP.SSIS.ControlFlow.Waiting.Properties;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Xml;

namespace PP.SSIS.ControlFlow.Waiting
{
    [DtsTask(
        DisplayName="Wait For File Task", 
        Description="Waits for file(s) existence or not existence",
        TaskType="ExecutionControl",
        TaskContact="Pavel Pawlowski",
        RequiredProductLevel=DTSProductLevel.None,
        IconResource = "PP.SSIS.ControlFlow.Waiting.Resources.WaitForFile.ico"
#if SQL2019
        ,UITypeName = "PP.SSIS.ControlFlow.Waiting.UI.WaitForFileUI, PP.SSIS.ControlFlow.Waiting.SQL2019, Version=1.0.0.0, Culture=neutral, PublicKeyToken=c298537c023d14ce"
#endif 
#if SQL2017
        ,UITypeName = "PP.SSIS.ControlFlow.Waiting.UI.WaitForFileUI, PP.SSIS.ControlFlow.Waiting.SQL2017, Version=1.0.0.0, Culture=neutral, PublicKeyToken=c298537c023d14ce"
#endif 
#if SQL2016
        ,UITypeName = "PP.SSIS.ControlFlow.Waiting.UI.WaitForFileUI, PP.SSIS.ControlFlow.Waiting.SQL2016, Version=1.0.0.0, Culture=neutral, PublicKeyToken=828749bdd7917e4b"
#endif 
#if SQL2014
        ,UITypeName = "PP.SSIS.ControlFlow.Waiting.UI.WaitForFileUI, PP.SSIS.ControlFlow.Waiting, Version=1.0.0.0, Culture=neutral, PublicKeyToken=d958e388b0ffd524"
#endif 
#if SQL2012
        ,UITypeName = "PP.SSIS.ControlFlow.Waiting.UI.WaitForFileUI, PP.SSIS.ControlFlow.Waiting, Version=1.0.0.0, Culture=neutral, PublicKeyToken=3f1061c3fd17eb79"
#endif 
        )
    ]
    public class WaitForFile : Task, IDTSComponentPersist
    {
        /// <summary>
        /// Specifies Existence Check type
        /// </summary>
        public enum ChekFileType
        {
            /// <summary>
            /// Check for Existence of file(s)
            /// </summary>
            Existence,
            /// <summary>
            /// Check for non Existence of file(s)
            /// </summary>
            NonExistence
        }

        /// <summary>
        /// Specifies Existence Type
        /// </summary>
        public enum FileExistenceType
        {
            /// <summary>
            /// Specifies that all files has to exists or not exists to proceed
            /// </summary>
            All,
            /// <summary>
            /// Specifies that any file can exists or not exists to proceed
            /// </summary>
            Any
        }

        /// <summary>
        /// Represents Execution Result value
        /// </summary>
        public enum WaitForFileResult
        {
            /// <summary>
            /// An Error occured during execution
            /// </summary>
            Error           = -1,
            /// <summary>
            /// WaitForFile Task was not executed
            /// </summary>
            None            = 0,
            /// <summary>
            /// Specifies that All files has been found
            /// </summary>
            AllFilesFound   = 1,
            /// <summary>
            /// Specifies that some of the files has been found
            /// </summary>
            AnyFileFound    = 2,
            /// <summary>
            /// Specifies that no files has been found
            /// </summary>
            NoFileFound     = 3,
            /// <summary>
            /// Specifies that some of the fiels has not been found
            /// </summary>
            AnyFileNotFound = 4,
            /// <summary>
            /// Specifies that exeuction ended by timeout
            /// </summary>
            Timeout         = 5
        }

        /// <summary>
        /// Execution result of the WaitForFile Task
        /// </summary>
        private WaitForFileResult result = WaitForFileResult.None;

        private List<string> files = new List<string>();
        /// <summary>
        /// Contains list of files to be be checked
        /// </summary>
        [Browsable(false)]
        public ReadOnlyCollection<string> Files
        {
            get { return files.AsReadOnly(); }
        }

        /// <summary>
        /// Represents the time span of the whole day
        /// </summary>
        private readonly TimeSpan day = new TimeSpan(1, 0, 0, 0);


        private int checkInterval = 2000;
        /// <summary>
        /// Gets or Sets the Check interval in milliseconds
        /// </summary>
        [Category("Wait For File")]
        [Description("Specifies interval in millisecionds in which the file(s) existence/non-existence is being checked")]
        public int CheckInterval
        {
            get { return checkInterval; }
            set { checkInterval = value; }
        }

        private TimeSpan checkTimeoutInterval = TimeSpan.Zero;
        /// <summary>
        /// Gets or Sets the CheckTimeout Interval
        /// </summary>
        [Category("Wait For File")]
        [Description("Specifies the time interval in hh:mm:ss after which TieMout occurs. TimeoutInterval of 00:00:00 means there is no timeout. Task times out either on CheckTimeoutInterval or CheckTimeOutTime depending what occurs earlier.")]
        public TimeSpan CheckTimeoutInterval
        {
            get { return checkTimeoutInterval; }
            set { checkTimeoutInterval = value; }
        }

        private TimeSpan checkTimeoutTime = TimeSpan.Zero;
        /// <summary>
        /// Gets or Sets the TIme at which the files checking should time-out
        /// </summary>
        [Category("Wait For File")]
        [Description("Specifies the time of a day hh:mm:ss at which TimeOut occurs. CheckTimeOutTime of 00:00:00 means there is no timeout. Task times out either on CheckTimeoutInterval or CheckTimeOutTime depending what occurs earlier.")]
        public TimeSpan CheckTimeoutTime
        {
            get { return checkTimeoutTime; }
            set { checkTimeoutTime = value; }
        }

        private bool timeoutNextDayIfTimePassed;
        /// <summary>
        /// Gets or Sts whether Timous shoud occur next day if time has already passed
        /// </summary>
        [Category("Wait For File")]
        [Description("Specifies whether CheckTimeOutTime has already passed at the time of execution, whether to set the Timeout on the same time next day.")]
        public bool TimeoutNextDayIfTimePassed
        {
            get { return timeoutNextDayIfTimePassed; }
            set { timeoutNextDayIfTimePassed = value; }
        }


        private ChekFileType checkType = ChekFileType.Existence;
        /// <summary>
        /// Gets or Sets the Check type
        /// </summary>
        [Category("Wait For File")]
        [Description("Specifies the CheckType. Existence means files are checked for their existence. NonExistence means that files are checked for Non Existence. This means then condition is true when the file does not exists.")]
        public ChekFileType CheckType
        {
            get { return checkType; }
            set { checkType = value; }
        }

        private FileExistenceType existenceType = FileExistenceType.All;
        /// <summary>
        /// Gets or Sets the Existence Type
        /// </summary>
        [Category("Wait For File")]
        [Description("Specifies Whether Existence is being checked for All files or for any file. In case of All, all files has to exists or not exists. In case of Any, if any of the file exists or not exists depending on the Check type, the check is successfull.")]
        public FileExistenceType ExistenceType
        {
            get { return existenceType; }
            set { existenceType = value; }
        }

        private string filesToCheck;
        /// <summary>
        /// Gets or Sets the files to be checked for existence
        /// </summary>
        [Category("Wait For File")]
        [Description("List of file to be checkd for existence or non-existence")]
        public string FilesToCheck
        {
            get { return filesToCheck; }
            set 
            {
                if (filesToCheck != value)
                {
                    filesToCheck = value;
                    files = SplitFilesToCheck(value);                    
                }                
            }
        }

        /// <summary>
        /// Splits a string with file names separated by Pipe and returns them as List
        /// </summary>
        /// <param name="value">String with file names separated by Pipe</param>
        /// <returns>List of file names</returns>
        private List<string> SplitFilesToCheck(string value)
        {
            List<string> fls = new List<string>();
            if (!string.IsNullOrEmpty(value))
            {
                string[] fl = value.Split(new char[] { '|' }, StringSplitOptions.RemoveEmptyEntries);

                foreach (string file in fl)
                {
                    fls.Add(file.Trim());
                }
            }

            return fls;
        }

        /// <summary>
        /// Check internal list of file names for validity
        /// </summary>
        /// <returns>True in case the file names are valid otherwise false</returns>
        private bool CheckFilesToCheck()
        {
            bool valid = true;
            foreach (string file in files)
            {
                try
                {
                    FileInfo fi = new FileInfo(file);
                }
                catch
                {
                    valid = false;
                    break;
                }
            }
            return valid;
        }
        
        /// <summary>
        /// Deserializes teh WaitForFile settings from XML
        /// </summary>
        /// <param name="node"></param>
        /// <param name="infoEvents"></param>
        void IDTSComponentPersist.LoadFromXML(System.Xml.XmlElement node, IDTSInfoEvents infoEvents)
        {
            if (node.Name == "WaitForFilesData")
            {
                if (node.HasAttributes) // new Format;
                {
                    WaitForFile.ChekFileType chType;
                    if (Enum.TryParse<WaitForFile.ChekFileType>(node.GetAttribute("checkType"), out chType))
                        this.CheckType = chType;
                    else
                        infoEvents.FireError(0, Resources.WaitForFileTaskName, string.Format(Resources.ErrorCouldNotDeserializeProperty, "CheckType", node.GetAttribute("checkType")), string.Empty, 0);

                    WaitForFile.FileExistenceType existenceType;

                    if (Enum.TryParse<WaitForFile.FileExistenceType>(node.GetAttribute("existenceType"), out existenceType))
                        this.ExistenceType = existenceType;
                    else
                        infoEvents.FireError(0, Resources.WaitForFileTaskName, string.Format(Resources.ErrorCouldNotDeserializeProperty, "ExistenceType", node.GetAttribute("existenceType")), string.Empty, 0);

                    TimeSpan ts;
                    if (TimeSpan.TryParse(node.GetAttribute("checkTimeoutInterval"), out ts))
                        this.checkTimeoutInterval = ts;
                    else
                        infoEvents.FireError(0, Resources.WaitForFileTaskName, string.Format(Resources.ErrorCouldNotDeserializeProperty, "CheckTimeout", node.GetAttribute("checkTimeoutInterval")), string.Empty, 0);

                    TimeSpan tst;
                    if (TimeSpan.TryParse(node.GetAttribute("checkTimeoutTime"), out tst))
                        this.checkTimeoutTime = tst;
                    else
                        infoEvents.FireError(0, Resources.WaitForFileTaskName, string.Format(Resources.ErrorCouldNotDeserializeProperty, "CheckTimeout", node.GetAttribute("checkTimeoutTime")), string.Empty, 0);

                    int chkInterval;

                    if (int.TryParse(node.GetAttribute("checkInterval"), out chkInterval))
                        this.CheckInterval = chkInterval;
                    else
                        infoEvents.FireError(0, Resources.WaitForFileTaskName, string.Format(Resources.ErrorCouldNotDeserializeProperty, "CheckInterval", node.GetAttribute("checkInterval")), string.Empty, 0);


                    bool timeoutNextDay;
                    if (bool.TryParse(node.GetAttribute("timeoutNextDayIfTimePassed"), out timeoutNextDay))
                        this.TimeoutNextDayIfTimePassed = timeoutNextDay;
                    else
                        infoEvents.FireError(0, Resources.WaitForFileTaskName, string.Format(Resources.ErrorCouldNotDeserializeProperty, "TimeoutNextDayIfTimePassed", node.GetAttribute("timeoutNextDayIfTimePassed")), string.Empty, 0);


                    foreach (XmlElement nodeData in node.ChildNodes)
                    {
                        if (nodeData.Name == "checkFiles")
                        {
                            List<string> fls = new List<string>();
                            foreach (XmlElement nd in nodeData.ChildNodes)
                            {
                                if (nd.Name == "file")
                                    fls.Add(nd.GetAttribute("name"));
                            }

                            FilesToCheck = string.Join<string>("|", fls);

                        }
                    }
                }
                else //old format
                {
                    foreach (XmlNode nodeData in node.ChildNodes)
                    {
                        switch (nodeData.Name)
                        {
                            case "checkType":
                                WaitForFile.ChekFileType chType;
                                if (Enum.TryParse<WaitForFile.ChekFileType>(nodeData.InnerText, out chType))
                                    this.CheckType = chType;
                                else
                                    infoEvents.FireError(0, Resources.WaitForFileTaskName, string.Format(Resources.ErrorCouldNotDeserializeProperty, "CheckType", node.InnerText), string.Empty, 0);
                                break;
                            case "existenceType":
                                WaitForFile.FileExistenceType existenceType;

                                if (Enum.TryParse<WaitForFile.FileExistenceType>(nodeData.InnerText, out existenceType))
                                    this.ExistenceType = existenceType;
                                else
                                    infoEvents.FireError(0, Resources.WaitForFileTaskName, string.Format(Resources.ErrorCouldNotDeserializeProperty, "ExistenceType", node.InnerText), string.Empty, 0);
                                break;
                            case "checkTimeoutInterval":
                                TimeSpan ts;
                                if (TimeSpan.TryParse(nodeData.InnerText, out ts))
                                    this.checkTimeoutInterval = ts;
                                else
                                    infoEvents.FireError(0, Resources.WaitForFileTaskName, string.Format(Resources.ErrorCouldNotDeserializeProperty, "CheckTimeout", node.InnerText), string.Empty, 0);
                                break;
                            case "checkTimeoutTime":
                                TimeSpan tst;
                                if (TimeSpan.TryParse(nodeData.InnerText, out tst))
                                    this.checkTimeoutTime = tst;
                                else
                                    infoEvents.FireError(0, Resources.WaitForFileTaskName, string.Format(Resources.ErrorCouldNotDeserializeProperty, "CheckTimeout", node.InnerText), string.Empty, 0);
                                break;
                            case "checkInterval":
                                int chkInterval;

                                if (int.TryParse(nodeData.InnerText, out chkInterval))
                                    this.CheckInterval = chkInterval;
                                else
                                    infoEvents.FireError(0, Resources.WaitForFileTaskName, string.Format(Resources.ErrorCouldNotDeserializeProperty, "CheckInterval", node.InnerText), string.Empty, 0);
                                break;
                            case "timeoutNextDayIfTimePassed":
                                bool timeoutNextDay;
                                if (bool.TryParse(nodeData.InnerText, out timeoutNextDay))
                                    this.TimeoutNextDayIfTimePassed = timeoutNextDay;
                                else
                                    infoEvents.FireError(0, Resources.WaitForFileTaskName, string.Format(Resources.ErrorCouldNotDeserializeProperty, "TimeoutNextDayIfTimePassed", node.InnerText), string.Empty, 0);
                                break;
                            case "checkFiles":
                                List<string> fls = new List<string>();
                                foreach (XmlNode nd in nodeData.ChildNodes)
                                {
                                    if (nd.Name == "file")
                                        fls.Add(nd.InnerText);
                                }

                                FilesToCheck = string.Join<string>("|", fls);
                                break;
                        }
                    }
                }
            }

        }
        /// <summary>
        /// Serializes the WaitForFile settings into XML
        /// </summary>
        /// <param name="doc"></param>
        /// <param name="infoEvents"></param>
        void IDTSComponentPersist.SaveToXML(System.Xml.XmlDocument doc, IDTSInfoEvents infoEvents)
        {
            XmlElement data = doc.CreateElement("WaitForFilesData");
            doc.AppendChild(data);

            data.SetAttribute("checkType", CheckType.ToString());
            data.SetAttribute("existenceType", ExistenceType.ToString());
            data.SetAttribute("checkTimeoutInterval", CheckTimeoutInterval.ToString());
            data.SetAttribute("checkTimeoutTime", checkTimeoutTime.ToString());
            data.SetAttribute("checkInterval", CheckInterval.ToString());
            data.SetAttribute("timeoutNextDayIfTimePassed", TimeoutNextDayIfTimePassed.ToString());

            XmlElement filesNode = doc.CreateElement("checkFiles");
            data.AppendChild(filesNode);

            foreach (string file in files)
            {
                XmlElement fileNode =  doc.CreateElement("file");
                fileNode.SetAttribute("name", file);
                filesNode.AppendChild(fileNode);
            }
        }

        public override DTSExecResult Validate(Connections connections, VariableDispenser variableDispenser, IDTSComponentEvents componentEvents, IDTSLogging log)
        {
            bool failure = false;

            if (checkInterval <= 0)
            {
                componentEvents.FireError(0, Resources.WatForFileTaskName, Resources.ErrorCheckInterval, string.Empty, 0);
                failure = true;
            }            

            if (files.Count == 0)
            {
                componentEvents.FireError(0, Resources.WaitForFileTaskName, Resources.ErrorMissingFileToCheck, string.Empty, 0);
                failure = true;
            }
            else
            {
                failure = !CheckFilesToCheck();
                if (failure)
                    componentEvents.FireError(0, Resources.WaitForFileTaskName, Resources.ErrorNotValidFileToCheck, string.Empty, 0);
            }


            if (failure)
                return DTSExecResult.Failure;
            else
                return base.Validate(connections, variableDispenser, componentEvents, log);
        }

        [Browsable(false)]
        public override object ExecutionValue
        {
            get
            {
                return result;
            }
        }

        public override DTSExecResult Execute(Connections connections, VariableDispenser variableDispenser, IDTSComponentEvents componentEvents, IDTSLogging log, object transaction)
        {
            //Actual time at the time of execution
            DateTime now = DateTime.Now;
            DateTime executionStart = DateTime.Now;
            result = WaitForFileResult.None;
            DateTime timeoutOn = checkTimeoutTime == TimeSpan.Zero ? DateTime.MaxValue :  DateTime.Today.Add(checkTimeoutTime);

            if (!CheckFilesToCheck())
            {
                componentEvents.FireError(0, Resources.WaitForFileTaskName, Resources.ErrorNotValidFileToCheck, string.Empty, 0);
                result = WaitForFileResult.Error;
                return DTSExecResult.Failure;
            }

            int filesCount = files.Count;

            if (timeoutOn < executionStart)
            {
                if (!TimeoutNextDayIfTimePassed)
                    result = WaitForFileResult.Timeout;
                else
                    timeoutOn = timeoutOn.AddDays(1);
            }
            

            while (result == WaitForFileResult.None)
            {
                int existsCount = 0;
                int notexistsCount = 0;

                foreach (string file in files)
                {
                    if (File.Exists(file))
                        existsCount++;
                    else
                        notexistsCount++;

                    switch (checkType)
                    {
                        case ChekFileType.Existence:
                            if (existenceType == FileExistenceType.All && existsCount == filesCount)
                                result = WaitForFileResult.AllFilesFound;
                            else if (existenceType == FileExistenceType.Any && existsCount > 0)
                                result = WaitForFileResult.AnyFileFound;
                            break;
                        case ChekFileType.NonExistence:
                            if (existenceType == FileExistenceType.All && notexistsCount == filesCount)
                                result = WaitForFileResult.NoFileFound;
                            else if (existenceType == FileExistenceType.Any && notexistsCount > 0)
                                result = WaitForFileResult.AnyFileNotFound;
                            break;
                    }
                    if (result != WaitForFileResult.None)
                        break;
                }

                TimeSpan currentDuration = DateTime.Now - executionStart;
                if ((CheckTimeoutInterval != TimeSpan.Zero && currentDuration >= CheckTimeoutInterval) ||
                    (checkTimeoutTime != TimeSpan.Zero && timeoutOn <= DateTime.Now))
                {
                    result = WaitForFileResult.Timeout;
                }

                if (result == WaitForFileResult.None)
                    System.Threading.Thread.Sleep(checkInterval);
            }

            return base.Execute(connections, variableDispenser, componentEvents, log, transaction);
        }


    }


	

}
