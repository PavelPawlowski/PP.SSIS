using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Xml.Linq;
using Microsoft.SqlServer.Dts.Runtime;
using PP.SSIS.ControlFlow.Logging.Properties;

namespace PP.SSIS.ControlFlow.Logging
{
    [DtsTask(
        DisplayName="Variables To Xml Task", 
        Description="Stores Variables value in Xml Format",
        TaskType="Logging",
        TaskContact="Pavel Pawlowski",
        RequiredProductLevel=DTSProductLevel.None,
        IconResource = "PP.SSIS.ControlFlow.Logging.Resources.VariablesToXml.ico"
#if SQL2017
        , UITypeName = "PP.SSIS.ControlFlow.Logging.UI.VariablesToXmlUI, PP.SSIS.ControlFlow.Logging.SQL2017, Version=1.0.0.0, Culture=neutral, PublicKeyToken=6088ee5fd022ff04"
#endif
#if SQL2016
        , UITypeName = "PP.SSIS.ControlFlow.Logging.UI.VariablesToXmlUI, PP.SSIS.ControlFlow.Logging.SQL2016, Version=1.0.0.0, Culture=neutral, PublicKeyToken=682d6707f9000ec0"
#endif
#if SQL2014
        , UITypeName = "PP.SSIS.ControlFlow.Logging.UI.VariablesToXmlUI, PP.SSIS.ControlFlow.Logging, Version=1.0.0.0, Culture=neutral, PublicKeyToken=cff0b3d3ec54bb0d"
#endif
#if SQL2012
        ,UITypeName = "PP.SSIS.ControlFlow.Logging.UI.VariablesToXmlUI, PP.SSIS.ControlFlow.Logging, Version=1.0.0.0, Culture=neutral, PublicKeyToken=9027dfa324bcd1b8"
#endif
#if SQL2008
        ,UITypeName = "PP.SSIS.ControlFlow.Logging.UI.VariablesToXmlUI, PP.SSIS.ControlFlow.Logging, Version=1.0.0.0, Culture=neutral, PublicKeyToken=0345354cb1bab4b7"
#endif
)
    ]
    public class VariablesToXmlTask : Task
    {

        private string _rootElementName = "variables";
        [Category("Variables To Xml Settings")]
        [Description("Name of the Root XML Element")]
        [DefaultValue("variables")]
        public string RootElementName
        {
            get { return _rootElementName; }
            set { _rootElementName = value; }
        }

        private string _variableElementName = "variable";
        [Category("Variables To Xml Settings")]
        [Description("Name of the variable Element")]
        public string VariableElementName
        {
            get { return _variableElementName; }
            set { _variableElementName = value; }
        }


        private bool _exportDataType = true;
        [Category("Variables To Xml Settings")]
        [Description("Specifies whether attribute with variable data type should be exportred")]
        [DefaultValue(true)]
        public bool ExportDataType
        {
            get { return _exportDataType; }
            set { _exportDataType = value; }
        }

        private bool _exportValueDataType = true;
        [Category("Variables To Xml Settings")]
        [Description("Specifies whether attribute with variable value data type should be exportred")]
        [DefaultValue(true)]
        public bool ExportValueDataType
        {
            get { return _exportValueDataType; }
            set { _exportValueDataType = value; }
        }


        //private bool _exportVariableNameSpace = true;
        //[Category("VariablesToXml")]
        //[Description("Specifies whether qualified variable names should be exported")]
        //public bool ExportNameSpace
        //{
        //    get { return _exportVariableNameSpace; }
        //    set { _exportVariableNameSpace = value; }
        //}

        private bool _exportVariablePath = true;
        [Category("Variables To Xml Settings")]
        [Description("Specifies whether attribute with variable Package Path should be exported")]
        [DefaultValue(true)]
        public bool ExportVariablePath
        {
            get { return _exportVariablePath; }
            set { _exportVariablePath = value; }
        }

        private bool _exportDescription = true;
        [Category("Variables To Xml Settings")]
        [Description("Specifies whether attribute with variable Description should be exported")]
        [DefaultValue(true)]
        public bool ExportDescription
        {
            get { return _exportDescription; }
            set { _exportDescription = value; }
        }

        List<string> _exportVariables = new List<string>();
        public ReadOnlyCollection<string> ExportVariables
        {
            get { return _exportVariables.AsReadOnly(); }
        }

        private bool _exportBinaryData = false;
        [Category("Variables To Xml Settings")]
        [Description("Specifies whether binary data of object variable should be exported")]
        [DefaultValue(false)]
        public bool ExportBinaryData
        {
            get { return _exportBinaryData; }
            set { _exportBinaryData = value; }
        }


        private SaveOptions _xmlSaveOption;
        [Category("Variables To Xml Settings")]
        [Description("Specifies the Xml SaveOptions to be used when exporting the xml")]
        [DefaultValue(SaveOptions.None)]
        public SaveOptions XmlSaveOptions
        {
            get { return _xmlSaveOption; }
            set { _xmlSaveOption = value; }
        }


        private string _variablesToExport = string.Empty;
        [Category("Variables To Xml Variables")]
        [Description("Colon, semicolon or pipe delimited list of variables to export")]
        [DefaultValue(true)]
        public string VariablesToExport
        {
            get { return _variablesToExport; }
            set
            {
                _variablesToExport = value;
                _exportVariables = SplitVariablesToExport(value);
            }
        }

        private string _xmlVariable = string.Empty;
        [Category("Variables To Xml Variables")]
        [Description("Variable to which will be stored the final Xml")]
        [DefaultValue("")]
        public string XmlVariable
        {
            get { return _xmlVariable; }
            set
            {
                if (_xmlVariable != value)
                {
                    _xmlVariable = value;
                }
            }
        }

        private List<string> SplitVariablesToExport(string value)
        {
            List<string> vars = new List<string>();
            if (!string.IsNullOrEmpty(value))
            {
                string[] fl = value.Split(new char[] { ',', ';', '|' }, StringSplitOptions.RemoveEmptyEntries);

                foreach (string var in fl)
                {
                    vars.Add(var.Trim());
                }
            }

            return vars;
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
            Debug.Print("Validation Started");
            string xmlVariableQualifiedName = string.Empty;
            //Check XmlVariables
            if (string.IsNullOrEmpty(_xmlVariable))
            {
                componentEvents.FireError(0, Resources.VariablesToXmlTaskName, Resources.ErrorXmlVariableEmpty, string.Empty, 0);
                return DTSExecResult.Failure;
            }
            else if (!variableDispenser.Contains(_xmlVariable)) //Check Variable exists
            {
                componentEvents.FireError(0, Resources.VariablesToXmlTaskName, string.Format(Resources.ErrorXmlVariableNotExists, _xmlVariable), string.Empty, 0);
                return DTSExecResult.Failure;
            }
            else
            {
                Variables vars = null;
                try
                {
                    variableDispenser.Reset();
                    variableDispenser.LockOneForWrite(_xmlVariable, ref vars);
                    if (vars == null || vars.Locked == false)   //Check if wariable is writable
                    {
                        componentEvents.FireError(0, Resources.VariablesToXmlTaskName, string.Format(Resources.ErrorLockingXmlVariable, _xmlVariable), string.Empty, 0);
                        return DTSExecResult.Failure;
                    }
                    else
                    {
                        Variable v = vars[0];
                        TypeCode dataType = v.DataType;
                        xmlVariableQualifiedName = v.QualifiedName;

                        if (v.Namespace != "User")  //Check namespace. Vaiabe should be writable in the user name space
                        {
                            componentEvents.FireError(0, Resources.VariablesToXmlTaskName, Resources.ErrorXmlVariableNotUser, string.Empty, 0);
                            return DTSExecResult.Failure;
                        }

                        if (v.DataType != TypeCode.String && v.DataType != TypeCode.Object) //Variable has to be string or object data type to be able to store XML
                        {
                            componentEvents.FireError(0, Resources.VariablesToXmlTaskName, Resources.ErrorXmlVaraibleNotString, string.Empty, 0);
                            return DTSExecResult.Failure;
                        }

                        if (v.EvaluateAsExpression == true) //Variable cannot be expression to be writable
                        {
                            componentEvents.FireError(0, Resources.VariablesToXmlTaskName, Resources.ErrorXmlVariableCannotHaveExpression, string.Empty, 0);
                            return DTSExecResult.Failure;
                        }

                    }
                }
                finally
                {
                    if (vars != null && vars.Locked)
                        vars.Unlock();
                }
            }


            //Check VariableElementName
            if (string.IsNullOrEmpty(_variableElementName)) //Variable element has to be specified
            {
                componentEvents.FireError(0, Resources.VariablesToXmlTaskName, Resources.ErrorVariableElementEmpty, string.Empty, 0);
                return DTSExecResult.Failure;
            }
            else
            {
                try
                {
                    XElement element = new XElement(_variableElementName);  //Check the VariableElementName is proper name for XmlElement
                }
                catch
                {
                    componentEvents.FireError(0, Resources.VariablesToXmlTaskName, string.Format(Resources.ErrorNotValidElementName, _rootElementName), string.Empty, 0);
                    return DTSExecResult.Failure;
                }
            }

            //Check RootElementName
            if (string.IsNullOrEmpty(_rootElementName)) //Che that RootElementName is specified
            {
                componentEvents.FireError(0, Resources.VariablesToXmlTaskName, Resources.ErrorRootXmlElementCannotBeEmpty, string.Empty, 0);
                return DTSExecResult.Failure;
            }
            else
            {
                try
                {
                    XElement element = new XElement(_rootElementName);  //Check that RootElementName is valid XmlElement name
                }
                catch
                {
                    componentEvents.FireError(0, Resources.VariablesToXmlTaskName, string.Format(Resources.ErrorNotValidElementName, _rootElementName), string.Empty, 0);
                    return DTSExecResult.Failure;
                }
            }

            //Check ExportVaraibles
            if (_exportVariables.Count > 0)
            {
                Variables vars = null;
                try
                {
                    variableDispenser.Reset();
                    foreach (string var in _exportVariables)
                    {
                        if (!variableDispenser.Contains(var))   //Check that all variables to be exported exists
                        {
                            componentEvents.FireError(0, Resources.VariablesToXmlTaskName, Resources.ErrorInvalidVariables, string.Empty, 0);
                            return DTSExecResult.Failure;
                        }
                        variableDispenser.LockForRead(var);
                    }
                    variableDispenser.GetVariables(ref vars);
                    if (vars == null || vars.Locked == false)   //Check that all varaibles to be exported are readable
                    {
                        componentEvents.FireError(0, Resources.VariablesToXmlTaskName, Resources.ErrorLockingReadVariables, string.Empty, 0);
                        return DTSExecResult.Failure;
                    }
                    else
                    {
                        foreach (Variable v in vars)
                        {
                            if (v.QualifiedName == xmlVariableQualifiedName)    //Check that XmlVariable is not present int he Export Variable list. (Cannot export and write to the same variable)
                            {
                                componentEvents.FireError(0, Resources.VariablesToXmlTaskName, Resources.ErrorXmlVariableCannotBeExported, string.Empty, 0);
                            }
                        }
                    }

                }
                finally
                {
                    if (vars != null && vars.Locked)
                        vars.Unlock();
                    variableDispenser.Reset();
                }
            }

            Debug.Print("Validation Finished Successfully");
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
            DTSExecResult chk = Validate(connections, variableDispenser, componentEvents, log);
            if (chk == DTSExecResult.Failure)
                return chk;

            XElement variablesElement = new XElement(RootElementName);

            Variables vars = null;
            try
            {
                //Lock XmlVariable for writing
                variableDispenser.LockForWrite(XmlVariable);

                //Lock all export variables for reading
                foreach (string variable in _exportVariables)
                {
                    variableDispenser.LockForRead(variable);
                }
                
                variableDispenser.GetVariables(ref vars);
                if (vars != null && vars.Locked == false)
                {
                    componentEvents.FireError(0, Resources.VariablesToXmlTaskName, Resources.ErrorLockingVariables, string.Empty, 0);
                    return DTSExecResult.Failure;
                }
                else
                {
                    //build the export exml
                    foreach (string var in _exportVariables)
                    {
                        Variable v = vars[var];
                        XElement varElement = new XElement(_variableElementName);
                        varElement.Add(new XAttribute("name", v.QualifiedName));

                        if (ExportDataType)
                            varElement.Add(new XAttribute("dataType", v.DataType));

                        if (v.Value == null)
                        {
                            varElement.Add(new XAttribute("isNull", true));
                        }
                        else
                        {
                            Type ot = v.Value.GetType();
                            if (ExportValueDataType)
                                varElement.Add(new XAttribute("valueDataType", ot.FullName));
                            
                            switch (v.DataType)
                            {
                                case TypeCode.DateTime:
                                    varElement.SetValue(((DateTime)v.Value).ToString(DateTimeFormatInfo.InvariantInfo.SortableDateTimePattern));
                                    break;
                                case TypeCode.DBNull:
                                    varElement.Add(new XAttribute("isDBNull", true));
                                    break;
                                case TypeCode.Empty:
                                    varElement.Add(new XAttribute("isEmpty", true));
                                    break;
                                case TypeCode.Object:
                                    object o = v.Value;
                                    if (ot == typeof(TimeSpan))
                                        varElement.SetValue(Convert.ToString(o, CultureInfo.InvariantCulture));
                                    else if (ot.IsEnum)
                                        varElement.SetValue(Enum.GetName(ot, o));
                                    else if (ExportBinaryData)
                                    {
                                        //TODO: Implement other data types
                                        varElement.Add(new XAttribute("notImplementedDataType", true));                                        
                                    }
                                    else
                                    {
                                        varElement.Add(new XAttribute("binaryDataNotExported", true));
                                        varElement.SetValue(ot.FullName);
                                    }

                                    break;
                                default:
                                    string s = Convert.ToString(v.Value, CultureInfo.InvariantCulture);
                                    varElement.SetValue(s);
                                    break;
                            }
                            
                        }
                        
                        if (ExportVariablePath)
                            varElement.Add(new XAttribute("path", v.GetPackagePath()));
                        if (ExportDescription && v.Description != string.Empty)
                            varElement.Add(new XAttribute("description", v.Description));

                        variablesElement.Add(varElement);
                    }

                    Variable xmlVar = vars[XmlVariable];
                    xmlVar.Value = variablesElement.ToString(XmlSaveOptions);

                }
            }
            finally
            {
                if (vars != null && vars.Locked)
                    vars.Unlock();
            }
            

            return base.Execute(connections, variableDispenser, componentEvents, log, transaction);
        }

    }
}
