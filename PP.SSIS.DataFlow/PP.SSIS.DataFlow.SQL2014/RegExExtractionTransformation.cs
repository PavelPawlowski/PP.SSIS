// <copyright file="ColumnsToXmlTransformation.cs" company="Pavel Pawlowski">
// Copyright (c) 2014, 2015 All Right Reserved
//
// THIS CODE AND INFORMATION ARE PROVIDED "AS IS" WITHOUT WARRANTY OF ANY 
// KIND, EITHER EXPRESSED OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE
// IMPLIED WARRANTIES OF MERCHANTABILITY AND/OR FITNESS FOR A
// PARTICULAR PURPOSE.
//
// </copyright>
// <author>Pavel Pawlowski</author>
// <summary>Contains implementaiton of the ColumnsToXmlTransformation</summary>

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.SqlServer.Dts.Pipeline;
using Microsoft.SqlServer.Dts.Pipeline.Wrapper;
using Microsoft.SqlServer.Dts.Runtime.Wrapper;
using System.Xml.Linq;
using System.Globalization;
using PP.SSIS.DataFlow.Properties;
using System.Text.RegularExpressions;
using PP.SSIS.DataFlow.Common;


using IDTSOutput = Microsoft.SqlServer.Dts.Pipeline.Wrapper.IDTSOutput100;
using IDTSCustomProperty = Microsoft.SqlServer.Dts.Pipeline.Wrapper.IDTSCustomProperty100;
using IDTSOutputColumn = Microsoft.SqlServer.Dts.Pipeline.Wrapper.IDTSOutputColumn100;
using IDTSInput = Microsoft.SqlServer.Dts.Pipeline.Wrapper.IDTSInput100;
using IDTSInputColumn = Microsoft.SqlServer.Dts.Pipeline.Wrapper.IDTSInputColumn100;
using IDTSVirtualInput = Microsoft.SqlServer.Dts.Pipeline.Wrapper.IDTSVirtualInput100;
using IDTSVirtualInputColumn = Microsoft.SqlServer.Dts.Pipeline.Wrapper.IDTSVirtualInputColumn100;


namespace PP.SSIS.DataFlow
{
    [DtsPipelineComponent(
        DisplayName = "RegEx Extraction"
        , Description = "Extracts data from a column based on regular expression"
        , ComponentType = ComponentType.Transform
        , IconResource = "PP.SSIS.DataFlow.Resources.RegExExtraction.ico"
        , CurrentVersion = 3

#if SQL2019
        , UITypeName = "PP.SSIS.DataFlow.UI.RegExExtractionTransformationUI, PP.SSIS.DataFlow.SQL2019, Version=1.0.0.0, Culture=neutral, PublicKeyToken=b68691a82d4fb69c"
#endif
#if SQL2017
        , UITypeName = "PP.SSIS.DataFlow.UI.RegExExtractionTransformationUI, PP.SSIS.DataFlow.SQL2017, Version=1.0.0.0, Culture=neutral, PublicKeyToken=b68691a82d4fb69c"
#endif
#if SQL2016
        , UITypeName = "PP.SSIS.DataFlow.UI.RegExExtractionTransformationUI, PP.SSIS.DataFlow.SQL2016, Version=1.0.0.0, Culture=neutral, PublicKeyToken=b99aee68060fa342"
#endif
#if SQL2014
        , UITypeName = "PP.SSIS.DataFlow.UI.RegExExtractionTransformationUI, PP.SSIS.DataFlow, Version=1.0.0.0, Culture=neutral, PublicKeyToken=6926746b040a83a5"
#endif
#if SQL2012
        , UITypeName = "PP.SSIS.DataFlow.UI.RegExExtractionTransformationUI, PP.SSIS.DataFlow, Version=1.0.0.0, Culture=neutral, PublicKeyToken=c8e167588e3f3397"
#endif
#if SQL2008
        , UITypeName = "PP.SSIS.DataFlow.UI.RegExExtractionTransformationUI, PP.SSIS.DataFlow, Version=1.0.0.0, Culture=neutral, PublicKeyToken=e17db8c64cd5b65b"
#endif
)
    ]
    public class RegExExtractionTransformation : PipelineComponent
    {
        #region Definitions

        private bool inputColumnsValid = true;
        private static readonly ICollection<DataType> _supportedDataTypes = new DataType[] { DataType.DT_STR, DataType.DT_WSTR, DataType.DT_TEXT, DataType.DT_NTEXT };
        /// <summary>
        /// Gets InputColumn DataTypes supported by this transformation
        /// </summary>
        public static ICollection<DataType> SupportedDataTypes
        {
            get { return _supportedDataTypes; }
        }


        /// <summary>
        /// Specifies Type of the Output Column Property
        /// </summary>
        [Flags]
        public enum SetPropertyType
        {
            /// <summary>
            /// Nothing to set
            /// </summary>
            None                            = 0x000,
            /// <summary>
            /// Represents LineageID of InputColumn
            /// </summary>
            InputColumnLineageID            = 0x001,
            /// <summary>
            /// Represetns Regular Expression to be applied on the Input Column
            /// </summary>
            RegularExpression               = 0x002,
            /// <summary>
            /// GroupCapture Number if there are multiple captures of the group
            /// </summary>
            GroupCaptureNumber              = 0x010,
            /// <summary>
            /// Represents the Match Number to return
            /// </summary>
            MatchNumber                     = 0x020,
            /// <summary>
            /// Represents the type of teh output to be produced
            /// </summary>
            RegexOutputType                 = 0x040,
            /// <summary>
            /// Represents specifiecation fo the output
            /// </summary>
            RegexOutput                     = 0x080,
            /// <summary>
            /// Regular Expresion Options
            /// </summary>
            RegexOptions                    = 0x100,
            /// <summary>
            /// Represetns all OutputColumnProperties
            /// </summary>            //
            All = InputColumnLineageID | RegularExpression | GroupCaptureNumber | MatchNumber |RegexOutputType | RegexOutput | RegexOptions
        }

        /// <summary>
        /// Specifies what will be output of the Regular Expression Matching
        /// </summary>
        public enum RegexOutputType
        {
            /// <summary>
            /// Specifies that result should be build
            /// </summary>
            Result              = 1,
            /// <summary>
            /// Capture Group specified by the name will be returned
            /// </summary>
            CaptureGroupName    = 2,
            /// <summary>
            /// Capture Group specified by number will be returned
            /// </summary>
            CaptureGroupNumber  = 3
        }

        /// <summary>
        /// Enum to provide Supported Data Types selection on the Output column
        /// </summary>
        public enum OutDataType
        {
            DT_WSTR     = DataType.DT_WSTR,
            DT_STR      = DataType.DT_STR,
            DT_NTEXT    = DataType.DT_NTEXT,
            DT_TEXT     = DataType.DT_TEXT
        }


        private class RegexOutput
        {
            /// <summary>
            /// Defines RegexKey - combination of pattern and options
            /// </summary>
            private struct Regexkey
            {
                public Regexkey(string pattern, RegexOptions options)
                {
                    Pattern = pattern;
                    Options = options;
                }

                public string Pattern;
                public RegexOptions Options;
            }

            /// <summary>
            /// List of defined regular expressions for reuse
            /// </summary>
            private Dictionary<Regexkey, Regex> expressions;

            public RegexOutput()
            {
                Outputs = new List<DataFlow.RegExExtractionTransformation.RegexOutputMapping>();
                expressions = new Dictionary<Regexkey, Regex>();
            }

            /// <summary>
            /// List of output mappings
            /// </summary>
            public List<RegexOutputMapping> Outputs { get; private set; }

            /// <summary>
            /// Adds a column to the Outputs list
            /// </summary>
            /// <param name="col">RegexOutputColumn to add</param>
            public  void AddColumnToList(RegExColumn col)
            {
                Regexkey key = new Regexkey(col.RegularExpression, col.Options);

                Regex r;
                if (!expressions.TryGetValue(key, out r))
                {
                    r = new Regex(key.Pattern, key.Options);
                    expressions.Add(key, r);
                }

                RegexOutputMapping output = Outputs.Find(ro => ro.Pattern == col.RegularExpression && ro.Regex.Options == col.Options && ro.InputColumnLineageID == col.SourceColumnLineageID && ro.MatchNumber == col.MatchNumber);
                if (output != null)
                    output.Columns.Add(col);
                else
                    Outputs.Add(new RegexOutputMapping(col, r));
            }

        }

        private class RegexOutputMapping
        {
            public RegexOutputMapping(RegExColumn column, Regex regex)
            {
                Pattern = column.RegularExpression;
                Regex = regex;
                Columns = new List<DataFlow.RegExExtractionTransformation.RegExColumn>();
                InputColumnLineageID = column.SourceColumnLineageID;
                InputColumnIndex = column.SourceColumnIndex;
                Columns.Add(column);
                InputColumnDataType = column.InputColumnDataType;
                MatchNumber = column.MatchNumber;
            }

            public string Pattern { get; private set; }
            public Regex Regex { get; private set; }
            public List<RegExColumn> Columns { get; private set; }

            public int InputColumnLineageID { get; private set; }

            public int InputColumnIndex { get; private set; }

            public int MatchNumber { get; private set; }
            public DataType InputColumnDataType { get; private set; }
        }


        /// <summary>
        /// Represents RegExColumn for RunTime processing
        /// </summary>
        public class RegExColumn
        {
            public RegExColumn()
                : this(-1, -1, -1, -1, string.Empty, RegexOptions.Compiled, RegexOutputType.Result, "$&", 0, 1, DataType.DT_NULL, -1, 0, DataType.DT_NULL)
            {
                
            }

            public RegExColumn(int sourceColumnLineageID, int sourceColumnIndex, int outputColumnLineageID, int outputColumnIndex,
                string regularExpression, RegexOptions options, RegexOutputType outputType, string output, int groupCaptureNumber, int matchNumber,
                DataType outputColumnDataType, int outputColumnCodePage, int outputColumnLenght, DataType inputColumnDataType)
            {
                SourceColumnLineageID = sourceColumnLineageID;
                SourceColumnIndex = sourceColumnIndex;
                OutputColumnLineageID = outputColumnLineageID;
                OutputColumIndex = outputColumnIndex;
                RegularExpression = regularExpression;
                Options = options;
                OutputType = outputType;
                Output = output;
                GroupCaptureNumber = groupCaptureNumber;
                MatchNumber = matchNumber;
                OutputColumnCodePage = outputColumnCodePage;
                OutputColumnDataType = outputColumnDataType;
                OutputColumnLength = outputColumnLenght;
                InputColumnDataType = inputColumnDataType;

                if (OutputType == RegexOutputType.CaptureGroupNumber)
                    CaptureGroupNo = int.Parse(Output);
                else
                    CaptureGroupNo = -1;
            }

            /// <summary>
            /// Gets or Sets the Name of the Output Column
            /// </summary>
            public string Name { get; internal set; }

            /// <summary>
            /// LineageID of the Source Column
            /// </summary>
            public int SourceColumnLineageID { get; internal set; }
            /// <summary>
            /// Index of the Source column in teh buffer
            /// </summary>
            public int SourceColumnIndex { get; internal set; }

            /// <summary>
            /// Lineage ID of the OutputColumn
            /// </summary>
            public int OutputColumnLineageID { get; internal set; }
            /// <summary>
            /// Index of the OutputColumn in the buffer
            /// </summary>
            public int OutputColumIndex { get; internal set; }

            /// <summary>
            /// Regular Expression to be used
            /// </summary>
            public string RegularExpression { get; internal set; }

            public RegexOptions Options { get; internal set; }

            /// <summary>
            /// Gets the Defiend RegexOutputType
            /// </summary>
            public RegexOutputType OutputType { get; internal set; }

            private string _output;
            /// <summary>
            /// Gets the defined output (Result, CaptureGroupname or CaptureGroupNumber
            /// </summary>
            public string Output
            {
                get
                {
                    return _output;
                }

                internal set
                {
                    _output = value;

                    if (OutputType == RegexOutputType.CaptureGroupNumber)
                        CaptureGroupNo = int.Parse(Output);
                    else
                        CaptureGroupNo = -1;
                }
            }

            /// <summary>
            /// Gets the Capture GroupNo in case the OutputType is CaptureGroupNumber
            /// </summary>
            public int CaptureGroupNo { get; private set; }

            /// <summary>
            /// Group Capture Number (0 = lastcampture)
            /// </summary>
            public int GroupCaptureNumber { get; internal set; }
            /// <summary>
            /// MatchNumber to return
            /// </summary>
            public int MatchNumber { get; internal set; }
            /// <summary>
            /// DataType of the Source Columns
            /// </summary>

            public DataType InputColumnDataType { get; internal set; }


            public DataType OutputColumnDataType { get; internal set; }
            /// <summary>
            /// CodePae of the Source Column
            /// </summary>
            public int OutputColumnCodePage { get; internal set; }
            public int OutputColumnLength { get; internal set; }
        }

        ///// <summary>
        ///// List of prepared RegExColumns
        ///// </summary>
        //List<RegExColumn> regExColumns = new List<RegExColumn>();
        ///// <summary>
        ///// List of unique defined Regular Expressions
        ///// </summary>
        //Dictionary<string, Regex> expressions = new Dictionary<string, Regex>();

        RegexOutput regexOutput = new RegexOutput();
        long rowsProcessed = 0;
        #endregion


        #region DesignTime
        /// <summary>
        /// Provides SSIS Component Properties
        /// </summary>
        public override void ProvideComponentProperties()
        {
            this.ComponentMetaData.Version = PipelineUtils.GetPipelineComponentVersion(this);

            //Set component properties
            this.ComponentMetaData.Name = Resources.RegExExtractionTransformationName;
            this.ComponentMetaData.Description = Resources.GetExExtractionTransformationDescription;
            this.ComponentMetaData.ContactInfo = Resources.TransformationContactInfo;

            // Reset the component.
            base.RemoveAllInputsOutputsAndCustomProperties();

            //call base method to insert input and output
            base.ProvideComponentProperties();

            //Set Error Input properties
            IDTSInput errorInput = ComponentMetaData.InputCollection[0];
            errorInput.Name = Resources.RegExExtractionInputName;
            errorInput.Description = Resources.RegExExtractionInputDescription;

            // Set Error Output properties
            IDTSOutput output = ComponentMetaData.OutputCollection[0];
            output.Name = Resources.RegExExtractionOutputName;
            output.Description = Resources.RegExExtractionOutputDescription;
            output.SynchronousInputID = errorInput.ID;
            output.IsErrorOut = true;

            //remove all custom proeprties
            ComponentMetaData.CustomPropertyCollection.RemoveAll();

        }

        /// <summary>
        /// Validate HashColumnsTransform metadata
        /// </summary>
        /// <returns></returns>
        public override DTSValidationStatus Validate()
        {
            DTSValidationStatus status = DTSValidationStatus.VS_ISVALID;

            //Validate number of Inputs (Should be 1)
            if (ComponentMetaData.InputCollection.Count != 1)
            {
                FireComponentMetadataError(0, Resources.ErrorIncorrectNumberOfInputs);
                status = DTSValidationStatus.VS_NEEDSNEWMETADATA;
            }

            //Validate number of Outputs (should be 1)
            if (ComponentMetaData.OutputCollection.Count != 1)
            {
                FireComponentMetadataError(0, Resources.ErrorIncorrectNumberOfOutputs);
                status =  DTSValidationStatus.VS_NEEDSNEWMETADATA;
            }

            if (status != DTSValidationStatus.VS_ISVALID)
                return status;

            //Get Inputs
            IDTSInput input = ComponentMetaData.InputCollection[0];
            IDTSVirtualInput vInput = input.GetVirtualInput();
            //Get Output
            IDTSOutput output = ComponentMetaData.OutputCollection[0];

            //Check whether Error Input is attached
            if (!input.IsAttached)
            {
                FireComponentMetadataError(0, "Input is not attached");
                return DTSValidationStatus.VS_NEEDSNEWMETADATA;
            }

            //UpdateOutput column Properties
            UpdateOutputColumnPropertiess(input, vInput, output);

            //Check Output Columns properties
            foreach (IDTSOutputColumn oCol in output.OutputColumnCollection)
            {
                IDTSCustomProperty prop = oCol.CustomPropertyCollection[Resources.RegExExtractionInputColumnLinageIdPropertyName];
                IDTSCustomProperty outTypeProp = oCol.CustomPropertyCollection[Resources.RegExExtractionRegexOutputType];
                IDTSCustomProperty outProp = oCol.CustomPropertyCollection[Resources.RegExExtractionRegexOutput];

                int lineageID = (int)prop.Value;
                string colname = string.Format("{0}", oCol.Name);

                //CheckLineage
                if (lineageID != 0)
                {
                    IDTSVirtualInputColumn vCol = vInput.VirtualInputColumnCollection.GetVirtualInputColumnByLineageID(lineageID);
                    if (vCol == null)
                    {
                        FireComponentMetadataError(0, string.Format(Resources.ErrorInputColumnLineageNotExists, lineageID));
                        status = DTSValidationStatus.VS_NEEDSNEWMETADATA;
                    }
                    else if (!SupportedDataTypes.Contains(vCol.DataType))
                    {
                        string vColName = string.Format("{0}.{1}", vCol.UpstreamComponentName, vCol.Name);

                        FireComponentMetadataError(0, 
                            string.Format(Resources.ErrorValidateColumn, colname,
                            string.Format(Resources.ErrorInputColumnDataTypeNotSupported, vCol.DataType.ToString(), vColName, lineageID)));
                        status = DTSValidationStatus.VS_NEEDSNEWMETADATA;
                        
                    }
                }
                else
                {
                    FireComponentMetadataError(0, string.Format(Resources.ErrorValidateColumn, colname, Resources.ErrorInputColumnNotSpecified));
                    status = DTSValidationStatus.VS_NEEDSNEWMETADATA;
                }

                //Check Regex
                prop = oCol.CustomPropertyCollection[Resources.RegExExtractionRegularExpressionName];
                string expr = prop.Value.ToString();
                Regex r = null;
                try
                {
                    r = new Regex(expr);
                }
                catch(Exception e)
                {
                    
                    FireComponentMetadataError(0, string.Format(Resources.ErrorValidateColumn, colname, e.Message));
                    status = DTSValidationStatus.VS_NEEDSNEWMETADATA;
                }

                if (r != null)
                {
                    RegexOutputType outputType = (RegexOutputType)outTypeProp.Value;
                    string outVal = outProp.Value.ToString();

                    if (outputType == RegexOutputType.CaptureGroupName)
                    {
                        string grpName = outVal;
                        if (grpName != string.Empty)
                        {
                            string[] groupNames = r.GetGroupNames();
                            if (!groupNames.Contains(grpName))
                            {
                                FireComponentMetadataError(0, string.Format(Resources.ErrorValidateColumn, colname,
                                    string.Format(Resources.ErrorGroupNameNotExistsInRegex, grpName, expr)));
                                status = DTSValidationStatus.VS_NEEDSNEWMETADATA;
                            }
                        }
                    }
                    else if (outputType == RegexOutputType.CaptureGroupNumber)
                    {
                        int grpNum = int.Parse(outVal);
                        int[] grpNumbers = r.GetGroupNumbers();
                        if (!grpNumbers.Contains(grpNum))
                        {
                            FireComponentMetadataError(0, string.Format(Resources.ErrorValidateColumn, colname,
                                string.Format(Resources.ErrorGroupNumberNotExistsInRegex, grpNum, expr)));
                            status = DTSValidationStatus.VS_NEEDSNEWMETADATA;
                        }
                    }
                }

                //Check GroupCaptureNumber
                prop = oCol.CustomPropertyCollection[Resources.RegExExtractionGroupCaptureNumberPropertyName];
                int captureNumber = (int)prop.Value;
                if (captureNumber < 0)
                {
                    FireComponentMetadataError(0, string.Format(Resources.ErrorValidateColumn, colname, Resources.ErrorGrouCaptureNumberLowerThanZero));
                    status = DTSValidationStatus.VS_NEEDSNEWMETADATA;
                }

                //Check MatchNumber
                prop = oCol.CustomPropertyCollection[Resources.RegExExtractionMatchNumberPropertyName];
                int matchNumber = (int)prop.Value;
                if (matchNumber < 1)
                {
                    FireComponentMetadataError(0, string.Format(Resources.ErrorValidateColumn, colname, Resources.ErrorMatchNumberLowerThanOne));
                    status = DTSValidationStatus.VS_NEEDSNEWMETADATA;
                }
            }

            return status;
        }

        public override void PerformUpgrade(int pipelineVersion)
        {
            DtsPipelineComponentAttribute componentAttribute = (DtsPipelineComponentAttribute)Attribute.GetCustomAttribute(this.GetType(), typeof(DtsPipelineComponentAttribute), false);
            int currentVersion = componentAttribute.CurrentVersion;

            // Get the attributes for the SSIS Package
            int metadataVersion = ComponentMetaData.Version;

            if (currentVersion < ComponentMetaData.Version)
            {
                throw new Exception(Properties.Resources.ErrorWrongRuntimeVersion);
            }
            else if (currentVersion > ComponentMetaData.Version)
            {
                //CheckOutputColumns Propertiesand add if missing
                IDTSOutput output = ComponentMetaData.OutputCollection[0];
                UpdateOutputColumnPropertiess(null, null, output);

                ComponentMetaData.Version = currentVersion;
            }
        }

        /// <summary>
        /// Creates a new Input Column
        /// </summary>
        /// <param name="inputID"></param>
        /// <param name="virtualInput"></param>
        /// <param name="lineageID"></param>
        /// <param name="usageType"></param>
        /// <returns></returns>
        public override IDTSInputColumn SetUsageType(int inputID, IDTSVirtualInput virtualInput, int lineageID, DTSUsageType usageType)
        {
            DTSUsageType uType = usageType == DTSUsageType.UT_READWRITE ? DTSUsageType.UT_READONLY : usageType;

            IDTSInputColumn col = base.SetUsageType(inputID, virtualInput, lineageID, uType);

            IDTSOutput output = ComponentMetaData.OutputCollection[0];

            if (uType == DTSUsageType.UT_IGNORED)
            {
                //Remove Mappped Output Columns
                List<int> iDsToRemove = new List<int>();
                foreach (IDTSOutputColumn oCol in output.OutputColumnCollection)
                {
                    IDTSCustomProperty prop = oCol.CustomPropertyCollection[Resources.RegExExtractionInputColumnLinageIdPropertyName]; ;
                    int lin = (int)prop.Value;
                    if (lin == lineageID)
                        prop.Value = 0;
                }                
            }
            else
            {
            }

            return col;
        }

        /// <summary>
        /// Reinitialize metadata in case input cvolumns are invalid
        /// </summary>
        public override void ReinitializeMetaData()
        {
            if (!inputColumnsValid)
            {
                foreach (IDTSInput input in ComponentMetaData.InputCollection)
                {
                    List<int> colIDsToRemove = new List<int>();
                    IDTSVirtualInput vInput = input.GetVirtualInput();
                    foreach (IDTSInputColumn col in input.InputColumnCollection)
                    {
                        IDTSVirtualInputColumn vCol = vInput.VirtualInputColumnCollection.GetVirtualInputColumnByLineageID(col.LineageID);
                        if (vCol == null)
                            colIDsToRemove.Add(col.ID);
                    }

                    foreach (int id in colIDsToRemove)
                    {
                        input.InputColumnCollection.RemoveObjectByID(id);
                    }
                }
                inputColumnsValid = true;
            }
        }
        /// <summary>
        /// Disallow inserting new inputs
        /// </summary>
        /// <param name="insertPlacement">Placemet of new input</param>
        /// <param name="inputID">ID of new Input</param>
        /// <returns>Throws error as this component does not allows inserting of new inputs</returns>
        public override IDTSInput InsertInput(DTSInsertPlacement insertPlacement, int inputID)
        {
            throw new InvalidOperationException(Resources.ErrorAddInput);
        }
         ///<summary>
        /// Disallow inserting new output
        /// </summary>
        /// <param name="insertPlacement">Placement of new output</param>
        /// <param name="outputID">ID of new output</param>
        /// <returns>Throws error as this component does not allows inserting of new outputs</returns>
        public override IDTSOutput InsertOutput(DTSInsertPlacement insertPlacement, int outputID)
        {
            throw new InvalidOperationException(Resources.ErrorAddOutput);
        }
        /// <summary>
        /// Trhows exception when trying to delete output as this component does not allows deleting outputs
        /// </summary>
        /// <param name="outputID">ID Of Output to be deleted</param>
        public override void DeleteOutput(int outputID)
        {
            throw new InvalidOperationException(Resources.ErrorDeleteOutput);
        }
        /// <summary>
        /// Trhows exception when trying to delete input as this component does not allows deleting inputs
        /// </summary>
        /// <param name="inputID">ID Of Input to be deleted</param>
        public override void DeleteInput(int inputID)
        {
            throw new InvalidOperationException(Resources.ErrorDeleteInput);
        }

        public override void SetOutputColumnDataTypeProperties(int iOutputID, int iOutputColumnID, DataType eDataType, int iLength, int iPrecision, int iScale, int iCodePage)
        {
            if (eDataType == DataType.DT_TEXT || eDataType == DataType.DT_NTEXT || eDataType == DataType.DT_STR || eDataType == DataType.DT_WSTR)
            {
                IDTSOutput output = ComponentMetaData.OutputCollection.GetObjectByID(iOutputID);
                IDTSOutputColumn oCol = output.OutputColumnCollection.GetObjectByID(iOutputColumnID);
                if (oCol != null)
                    oCol.SetDataTypeProperties(eDataType, iLength, iPrecision, iScale, iCodePage);
            }
            else
            {
                base.SetOutputColumnDataTypeProperties(iOutputID, iOutputColumnID, eDataType, iLength, iPrecision, iScale, iCodePage);
            }
        }

        /// <summary>
        /// Inserts new Output columns as specified position in specified output
        /// </summary>
        /// <param name="outputID">ID of the output to insert new output column</param>
        /// <param name="outputColumnIndex">Index of the inserted output column</param>
        /// <param name="name">Name of the inserted output column</param>
        /// <param name="description">Description of the inserted column</param>
        /// <returns>Inserted output column</returns>
        public override IDTSOutputColumn InsertOutputColumnAt(int outputID, int outputColumnIndex, string name, string description)
        {
            //Insert new column into specified output
            IDTSOutput output = ComponentMetaData.OutputCollection.FindObjectByID(outputID);

            IDTSOutputColumn outputColumn = output.OutputColumnCollection.NewAt(outputColumnIndex);
            outputColumn.Name = name;
            outputColumn.Description = description;

            SetOutputColumnProperties(outputColumn, SetPropertyType.All, null, null);
            return outputColumn;
        }
        /// <summary>
        /// Sets value of the custom column property
        /// </summary>
        /// <param name="outputID">Output ID</param>
        /// <param name="outputColumnID">Output Column ID</param>
        /// <param name="propertyName">Name of the custom Property</param>
        /// <param name="propertyValue">Property value to be set</param>
        /// <returns>Custom property</returns>
        public override IDTSCustomProperty SetOutputColumnProperty(int outputID, int outputColumnID, string propertyName, object propertyValue)
        {
            object valueToSet;
            IDTSInput input = ComponentMetaData.InputCollection[0];
            IDTSVirtualInput vInput = input.GetVirtualInput();
            IDTSOutput output = ComponentMetaData.OutputCollection.GetObjectByID(outputID);
            IDTSOutputColumn ocol = output.OutputColumnCollection.GetObjectByID(outputColumnID);

            //Validation of individual properties
            if (propertyName == Resources.RegExExtractionInputColumnLinageIdPropertyName)
            {

                int lineageID = (int)propertyValue;

                if (lineageID == 0) //In case of 0, loop through OutputColumns for Source Lineasges. to remove not used input columns
                {
                    List<int> lineages = new List<int>(output.OutputColumnCollection.Count);
                    foreach (IDTSOutputColumn oCol in output.OutputColumnCollection)
                    {
                        if (oCol.ID == outputColumnID) //bypass current OutputColumn
                            continue;
                        IDTSCustomProperty prop = oCol.CustomPropertyCollection[Resources.RegExExtractionInputColumnLinageIdPropertyName];
                        int linId = (int)prop.Value;
                        if (linId > 0 && !lineages.Contains(linId))
                            lineages.Add(linId);
                    }

                    //Remove not used Inputcolumns from InputColumns
                    foreach (IDTSVirtualInputColumn vCol in vInput.VirtualInputColumnCollection)
                    {
                        if (!lineages.Contains(vCol.LineageID))
                            base.SetUsageType(input.ID, vInput, vCol.LineageID, DTSUsageType.UT_IGNORED);
                    }
                }
                else
                {
                    IDTSVirtualInputColumn vCol = vInput.VirtualInputColumnCollection.GetVirtualInputColumnByLineageID(lineageID);
                    if (vCol == null)
                    {
                        throw new System.ArgumentException(string.Format(Resources.ErrorInputColumnLineageNotExists, lineageID));
                    }
                    else
                    {
                        if (!SupportedDataTypes.Contains(vCol.DataType))
                        {
                            string vColName = string.Format("{0}.{1}", vCol.UpstreamComponentName, vCol.Name);
                            throw new System.ArgumentException(string.Format(Resources.ErrorInputColumnDataTypeNotSupported, vCol.DataType.ToString(), vColName, vCol.LineageID));
                        }
                        else
                        {
                            base.SetUsageType(input.ID, vInput, lineageID, DTSUsageType.UT_READONLY);
                        }
                    }
                }
                valueToSet = lineageID;
            }
            else if (propertyName == Resources.RegExExtractionRegularExpressionName)
            {
                valueToSet = propertyValue.ToString();
            }
            else if (propertyName == Resources.RegExExtractionGroupCaptureNumberPropertyName)
            {
                int captureNumber = (int)propertyValue;
                valueToSet = captureNumber;
            }
            else if (propertyName == Resources.RegExExtractionMatchNumberPropertyName)
            {
                int matchNumber = (int)propertyValue;
                valueToSet = matchNumber;
            }
            else if (propertyName == Resources.RegExExtractionRegexOutputType)
            {
                RegexOutputType outputType = (RegexOutputType)propertyValue;
                valueToSet = outputType;
            }
            else if (propertyName == Resources.RegExExtractionRegexOutput)
            {
                valueToSet = propertyValue.ToString();
                IDTSCustomProperty outType = ocol.CustomPropertyCollection[Resources.RegExExtractionRegexOutputType];
                RegexOutputType outputType = (RegexOutputType)outType.Value;
                if (outputType == RegexOutputType.CaptureGroupNumber)
                {
                    int val;
                    if (!int.TryParse(valueToSet.ToString(), out val))
                    throw new Exception(string.Format("RegexOutput has to be an integer number when RegExOutputType is CaptureGroupNumber. Error parsing '{0}'", valueToSet.ToString()));
                }                
            }
            else
            {
                throw new Exception(string.Format(Resources.ErrorOutputColumnPropertyCannotBeChanged, propertyName));
            }

            return base.SetOutputColumnProperty(outputID, outputColumnID, propertyName, valueToSet);
        }

        public override IDTSCustomProperty SetInputColumnProperty(int inputID, int inputColumnID, string propertyName, object propertyValue)
        {
            throw new Exception(string.Format(Resources.ErrorInputColumnPropertyCannotBeChanged, propertyName));
        }

        #endregion

        #region Runtime Processing
        /// <summary>
        /// PreExecute Phase for initialization of internal runtime structures
        /// </summary>
        public override void PreExecute()
        {
            bool fireAgain = true;
            rowsProcessed = 0;
            ComponentMetaData.FireInformation(0, this.ComponentMetaData.Name, "Pre-Execute phase is beginning.", string.Empty, 0, ref fireAgain);

            IDTSInput input = ComponentMetaData.InputCollection[0];         //RegExExtraction Input
            IDTSVirtualInput vInput = input.GetVirtualInput();              //RegExExtraction Virtual Input
            IDTSOutput output = ComponentMetaData.OutputCollection[0];      //RegExExtraction Output

            regexOutput = new DataFlow.RegExExtractionTransformation.RegexOutput();



            //Loop through RegEx Output Columns collection and prepare list of RegExColumns
            foreach (IDTSOutputColumn oCol in output.OutputColumnCollection)
            {
                RegExColumn rCol = new RegExColumn();
                DataType inputColumnDataType = DataType.DT_NULL;
                string inputColumnName = string.Empty;
                int inputColumnLineageID = 0;

                rCol.Name = oCol.Name;
                rCol.OutputColumnLineageID = oCol.LineageID;
                //Get Index position in the Buffer
                rCol.OutputColumIndex = BufferManager.FindColumnByLineageID(input.Buffer, oCol.LineageID);
                rCol.OutputColumnDataType = oCol.DataType;
                rCol.OutputColumnCodePage = oCol.CodePage;
                rCol.OutputColumnLength = oCol.Length;

                //Process Output columns properties
                foreach (IDTSCustomProperty prop in oCol.CustomPropertyCollection)
                {
                    //Regular Expression
                    if (prop.Name == Resources.RegExExtractionRegularExpressionName)
                        rCol.RegularExpression = (string)prop.Value;                    
                    else if (prop.Name == Resources.RegExExtractionRegexOutputType) //GroupName
                        rCol.OutputType = (RegexOutputType)prop.Value;                    
                    else if (prop.Name == Resources.RegExExtractionRegexOutput) //GroupNumber
                        rCol.Output = (string)prop.Value;                    
                    else if (prop.Name == Resources.RegExExtractionGroupCaptureNumberPropertyName) //GroupCaptureNumber
                        rCol.GroupCaptureNumber = (int)prop.Value;                    
                    else if (prop.Name == Resources.RegExExtractionMatchNumberPropertyName) //MatchNumber
                        rCol.MatchNumber = (int)prop.Value;                    
                    else if (prop.Name == Resources.RegExExtractionInputColumnLinageIdPropertyName) //InputclumnLineageID
                    {
                        int lineageID = (int)prop.Value;
                        if (lineageID > 0)
                        {
                            IDTSVirtualInputColumn vCol = vInput.VirtualInputColumnCollection.GetVirtualInputColumnByLineageID(lineageID);
                            if (vCol != null)
                            {
                                rCol.SourceColumnIndex = BufferManager.FindColumnByLineageID(input.Buffer, lineageID);
                                //Store DataType and codePage in the rCol so we do not need to retrieve it per row in ProcessInput
                                rCol.SourceColumnLineageID = vCol.LineageID;
                                inputColumnDataType = vCol.DataType;
                                inputColumnLineageID = vCol.LineageID;
                                inputColumnName = vCol.Name;
                                rCol.InputColumnDataType = vCol.DataType;
                            }
                        }
                        else
                        {
                            break;
                        }
                    }
                    else if (prop.Name == Resources.RegExExtractionRegexOptions)
                    {
                        rCol.Options = (RegexOptions)prop.Value;
                    }
                }


                bool cancel;
                //validate column
                if (!SupportedDataTypes.Contains(inputColumnDataType))
                    ComponentMetaData.FireError(0, ComponentMetaData.Name, string.Format("Output Column [{0}]({1}): Input columns [{2}]({3} has unsupported data type [(4}]", rCol.Name, rCol.OutputColumnLineageID, inputColumnName, inputColumnLineageID, inputColumnDataType), null, 0, out cancel);
                else if (rCol.MatchNumber <= 0)
                    ComponentMetaData.FireError(0, ComponentMetaData.Name, string.Format("Output Column [{0}]({1}): Defined MatchNumber ({2} <= 0", rCol.Name, rCol.OutputColumnLineageID, rCol.MatchNumber), null, 0, out cancel);
                else if (rCol.GroupCaptureNumber < 0)
                    ComponentMetaData.FireError(0, ComponentMetaData.Name, string.Format("Output Column [{0}]({1}): Defined GroupCaptureNumber ({2} < 0", rCol.Name, rCol.OutputColumnLineageID, rCol.GroupCaptureNumber), null, 0, out cancel);


                regexOutput.AddColumnToList(rCol);
            }
        }

        /// <summary>
        /// Processes rows in the inpuyt buffer
        /// </summary>
        /// <param name="inputID">ID of the input to process the input rows</param>
        /// <param name="buffer">Pipeline bufere with rows to process</param>
        public override void ProcessInput(int inputID, PipelineBuffer buffer)
        {
            int outputsCount = regexOutput.Outputs.Count;
            string sourceValue = null;                  //Value of the SourceColumn to appy RegularExpression
            Match match;                                //match to be processed
            string resultValue = null;
            Group grp = null;                          //Group to return
            bool processGroup = false;

            //int columnsCount = regExColumns.Count;      //Count of OutputColumns
            //string matchValue;                          //value to return base don Regular Expression

            if (!buffer.EndOfRowset)
            {
                while (buffer.NextRow())
                {
                    rowsProcessed++;

                    if (outputsCount == 0)
                        continue;

                    foreach (RegexOutputMapping mapping in regexOutput.Outputs)
                    {
                        match = null;

                        switch (mapping.InputColumnDataType)
                        {
                            case DataType.DT_STR:
                            case DataType.DT_WSTR:
                            case DataType.DT_TEXT:
                            case DataType.DT_NTEXT:
                                sourceValue = buffer.GetString(mapping.InputColumnIndex);
                                break;
                            default: //Ignore not supported Data Types
                                continue;
                        }
                        
                        //If rcol.MatchNumber == 1 then return FirstMatch, othewrise returnappropriate Match from Matches
                        if (mapping.MatchNumber == 1)
                        {
                            match = mapping.Regex.Match(sourceValue);
                        }
                        else
                        {
                            var matches = mapping.Regex.Matches(sourceValue);

                            //If there is not such MatchNumber, continue with next mapping
                            if (mapping.MatchNumber <= matches.Count)
                                match = matches[mapping.MatchNumber - 1];
                        }


                        foreach (RegExColumn rCol in mapping.Columns)
                        {
                            resultValue = null;
                            //if the match is not successfull, set all output columns to null and continue with next mapping
                            if (match == null || !match.Success)
                            {
                                buffer.SetNull(rCol.OutputColumIndex);
                                continue;
                            }

                            switch (rCol.OutputType)
                            {
                                case RegexOutputType.Result:
                                    resultValue = match.Result(rCol.Output);
                                    processGroup = false;
                                    grp = null;
                                    break;
                                case RegexOutputType.CaptureGroupName:
                                    grp = match.Groups[rCol.Output];
                                    processGroup = true;
                                    break;
                                case RegexOutputType.CaptureGroupNumber:
                                    grp = match.Groups[rCol.CaptureGroupNo];
                                    processGroup = true;
                                    break;
                                default:
                                    buffer.SetNull(rCol.OutputColumIndex);
                                    continue;
                            }

                            if (processGroup)
                            {
                                //If GroupCaptureNumber == 0 then use the last capture stored in value otherwise take appropriate Capture
                                if (rCol.GroupCaptureNumber == 0)
                                    resultValue = grp.Value;
                                else if (rCol.GroupCaptureNumber > grp.Captures.Count) //If the GroupCaptureNumber is > number of captures, contineu with next column
                                {
                                    buffer.SetNull(rCol.OutputColumIndex);
                                    continue;
                                }
                                else
                                    resultValue = grp.Captures[rCol.GroupCaptureNumber - 1].Value;
                            }

                            buffer.SetString(rCol.OutputColumIndex, resultValue);

                        }
                    }
                }
            }
        }

        public override void PostExecute()
        {
            bool fireAgain = true;
            this.ComponentMetaData.FireInformation(0, this.ComponentMetaData.Name, "Post-Execute phase is beginning. Processed " + rowsProcessed.ToString(CultureInfo.CurrentCulture) + " rows.", string.Empty, 0, ref fireAgain);

            base.PostExecute();
        }


        #endregion

        #region Private & internal Methods


        /// <summary>
        /// Sets XmlColumn properties for Output Column
        /// </summary>
        /// <param name="outputColumn">Output Column to set XmlColumn Properties</param>
        /// <param name="propType">Property types to set</param>
        internal static void SetOutputColumnProperties(IDTSOutputColumn outputColumn, SetPropertyType propType, IDTSVirtualInput vInput, int? inputColumnLineageID)
        {
            List<int> IdsToRemove = new List<int>();
            List<string> customProperties = new List<string>(new string[] { Resources.RegExExtractionInputColumnLinageIdPropertyName, Resources.RegExExtractionRegularExpressionName,
            Resources.RegExExtractionRegexOutputType, Resources.RegExExtractionRegexOutput, Resources.RegExExtractionGroupCaptureNumberPropertyName,
            Resources.RegExExtractionRegexOptions, Resources.RegExExtractionMatchNumberPropertyName});

            //Loop tghrough properties and remote those we are setting and those which are not valid for ErrorMetadata OutputColumn
            foreach (IDTSCustomProperty prop in outputColumn.CustomPropertyCollection)
            {
                if (
                    (prop.Name == Resources.RegExExtractionInputColumnLinageIdPropertyName && (propType & SetPropertyType.InputColumnLineageID) == SetPropertyType.InputColumnLineageID) ||
                    (prop.Name == Resources.RegExExtractionRegularExpressionName && (propType & SetPropertyType.RegularExpression) == SetPropertyType.RegularExpression) ||
                    (prop.Name == Resources.RegExExtractionGroupCaptureNumberPropertyName && (propType & SetPropertyType.GroupCaptureNumber) == SetPropertyType.GroupCaptureNumber) ||
                    (prop.Name == Resources.RegExExtractionMatchNumberPropertyName && (propType & SetPropertyType.MatchNumber) == SetPropertyType.MatchNumber) ||
                    (prop.Name == Resources.RegExExtractionRegexOutputType && (propType & SetPropertyType.RegexOutputType) == SetPropertyType.RegexOutputType) ||
                    (prop.Name == Resources.RegExExtractionRegexOutput && (propType & SetPropertyType.RegexOutput) == SetPropertyType.RegexOutput) ||
                    (prop.Name == Resources.RegExExtractionRegexOptions && (propType & SetPropertyType.RegexOptions) == SetPropertyType.RegexOptions)
                )
                {
                    IdsToRemove.Add(prop.ID);
                }


                if (!customProperties.Contains(prop.Name))
                    IdsToRemove.Add(prop.ID);
            }

            foreach (int id in IdsToRemove)
            {
                outputColumn.CustomPropertyCollection.RemoveObjectByID(id);
            }



            //InputColumnLineageID
            if ((propType & SetPropertyType.InputColumnLineageID) == SetPropertyType.InputColumnLineageID)
            {
                IDTSCustomProperty outputColumnType = outputColumn.CustomPropertyCollection.New();
                outputColumnType.Description = Resources.RegExExtractionInputColumnLinageIdPropertyDescription;
                outputColumnType.Name = Resources.RegExExtractionInputColumnLinageIdPropertyName;
                outputColumnType.ContainsID = true;
                outputColumnType.EncryptionRequired = false;
                outputColumnType.ExpressionType = DTSCustomPropertyExpressionType.CPET_NONE;
                outputColumnType.TypeConverter = typeof(int).AssemblyQualifiedName;
                outputColumnType.Value = inputColumnLineageID ?? 0;
            }

            //RegularExpression
            if ((propType & SetPropertyType.RegularExpression) == SetPropertyType.RegularExpression)
            {
                IDTSCustomProperty regularExpression = outputColumn.CustomPropertyCollection.New();
                regularExpression.Description = Resources.RegExExtractionRegularExpressionDescription;
                regularExpression.Name = Resources.RegExExtractionRegularExpressionName;
                regularExpression.ContainsID = false;
                regularExpression.EncryptionRequired = false;
                regularExpression.ExpressionType = DTSCustomPropertyExpressionType.CPET_NOTIFY;
                regularExpression.TypeConverter = typeof(string).AssemblyQualifiedName;
                regularExpression.Value = string.Empty;
            }

            if ((propType & SetPropertyType.GroupCaptureNumber) == SetPropertyType.GroupCaptureNumber)
            {
                IDTSCustomProperty groupCptureNumber = outputColumn.CustomPropertyCollection.New();
                groupCptureNumber.Description = Resources.RegExExtractionGroupCaptureNumberPropertyDescription;
                groupCptureNumber.Name = Resources.RegExExtractionGroupCaptureNumberPropertyName;
                groupCptureNumber.ContainsID = false;
                groupCptureNumber.EncryptionRequired = false;
                groupCptureNumber.ExpressionType = DTSCustomPropertyExpressionType.CPET_NOTIFY;
                groupCptureNumber.TypeConverter = typeof(int).AssemblyQualifiedName;
                groupCptureNumber.Value = 0;
            }

            //MatchNumber
            if ((propType & SetPropertyType.MatchNumber) == SetPropertyType.MatchNumber)
            {
                IDTSCustomProperty matchNumber = outputColumn.CustomPropertyCollection.New();
                matchNumber.Description = Resources.RegExExtractionMatchNumberPropertyDescription;
                matchNumber.Name = Resources.RegExExtractionMatchNumberPropertyName;
                matchNumber.ContainsID = false;
                matchNumber.EncryptionRequired = false;
                matchNumber.ExpressionType = DTSCustomPropertyExpressionType.CPET_NOTIFY;
                matchNumber.TypeConverter = typeof(int).AssemblyQualifiedName;
                matchNumber.Value = 1;
            }


            //RegexOutputTYpe
            if ((propType & SetPropertyType.RegexOutputType) == SetPropertyType.RegexOutputType)
            {
                IDTSCustomProperty groupName = outputColumn.CustomPropertyCollection.New();
                groupName.Description = "Specifies the type of of the output";
                groupName.Name = Resources.RegExExtractionRegexOutputType;
                groupName.ContainsID = false;
                groupName.EncryptionRequired = false;
                groupName.ExpressionType = DTSCustomPropertyExpressionType.CPET_NONE;
                groupName.TypeConverter = typeof(RegexOutputType).AssemblyQualifiedName;
                groupName.Value = RegexOutputType.Result;
            }

            //RegexOutput
            if ((propType & SetPropertyType.RegexOutput) == SetPropertyType.RegexOutput)
            {
                IDTSCustomProperty groupNumber = outputColumn.CustomPropertyCollection.New();
                groupNumber.Description = "Contains definition of the output";
                groupNumber.Name = Resources.RegExExtractionRegexOutput;
                groupNumber.ContainsID = false;
                groupNumber.EncryptionRequired = false;
                groupNumber.ExpressionType = DTSCustomPropertyExpressionType.CPET_NONE;
                groupNumber.TypeConverter = typeof(string).AssemblyQualifiedName;
                groupNumber.Value = "$&";
            }

            //RegexOptions
            if ((propType & SetPropertyType.RegexOptions) == SetPropertyType.RegexOptions)
            {
                IDTSCustomProperty groupNumber = outputColumn.CustomPropertyCollection.New();
                groupNumber.Description = "Contains options for the regular expression";
                groupNumber.Name = Resources.RegExExtractionRegexOptions;
                groupNumber.ContainsID = false;
                groupNumber.EncryptionRequired = false;
                groupNumber.ExpressionType = DTSCustomPropertyExpressionType.CPET_NONE;
                groupNumber.TypeConverter = typeof(RegexOptions).AssemblyQualifiedName;
                groupNumber.Value = RegexOptions.Compiled;
            }

            SetOutputColumnDataType(outputColumn, vInput, inputColumnLineageID);
        }

        /// <summary>
        /// Sets RegEx OutputColumn DataType based ont he InputColumnType
        /// </summary>
        /// <param name="outputColumn">OutputColumn to set DataTyupe</param>
        /// <param name="vInput">Virtual Input to get Input Column Data Type</param>
        /// <param name="inputColumnLineageID">LineageID of the input columnto base the OutputColumnDataType. If inputColumnLineageID is null, then dataType is not beingg set</param>
        internal static void SetOutputColumnDataType(IDTSOutputColumn outputColumn, IDTSVirtualInput vInput, int? inputColumnLineageID)
        {
            if (inputColumnLineageID != null)
            {
                if (vInput != null && inputColumnLineageID.Value > 0)
                {
                    IDTSVirtualInputColumn vCol = vInput.VirtualInputColumnCollection.GetVirtualInputColumnByLineageID(inputColumnLineageID.Value);
                    outputColumn.SetDataTypeProperties(vCol.DataType, vCol.Length, vCol.Precision, vCol.Scale, vCol.CodePage);
                }
                else
                {
                    outputColumn.SetDataTypeProperties(DataType.DT_WSTR, 1, 0, 0, 0);
                }
            }
        }

        /// <summary>
        /// Updates Output Columns properties so they match the lates implementation of the component
        /// </summary>
        /// <param name="output">Output which OutputColumns should be updated</param>
        private static void UpdateOutputColumnPropertiess(IDTSInput input, IDTSVirtualInput vInput, IDTSOutput output)
        {
            foreach (IDTSOutputColumn oCol in output.OutputColumnCollection)
            {
                SetPropertyType existingProps = SetPropertyType.None;
                SetPropertyType propToSet = SetPropertyType.All;
                List<int> propsToRemove = new List<int>();
                int? inputLineageID = 0;
                int groupNo = 0;
                string groupName = null;

                foreach (IDTSCustomProperty prop in oCol.CustomPropertyCollection)
                {
                    if (prop.Name == Resources.RegExExtractionInputColumnLinageIdPropertyName)
                    {
                        existingProps |= SetPropertyType.InputColumnLineageID;
                        propToSet ^= SetPropertyType.InputColumnLineageID;
                        prop.ContainsID = true;

                        if (input == null || vInput == null)
                            inputLineageID = null;
                        else
                        {
                            int lineageID = (int)prop.Value;
                            if (lineageID > 0)
                            {
                                IDTSVirtualInputColumn vcol = vInput.VirtualInputColumnCollection.GetVirtualInputColumnByLineageID(lineageID);
                                if (vcol != null && SupportedDataTypes.Contains(vcol.DataType))
                                    inputLineageID = lineageID;
                                else
                                    inputLineageID = null;

                            }
                            else
                                inputLineageID = 0;
                        }

                    }
                    else if (prop.Name == Resources.RegExExtractionRegularExpressionName)
                    {
                        existingProps |= SetPropertyType.RegularExpression;
                        propToSet ^= SetPropertyType.RegularExpression;
                        prop.ExpressionType = DTSCustomPropertyExpressionType.CPET_NOTIFY;
                    }
                    else if (prop.Name == Resources.RegExExtractionGroupCaptureNumberPropertyName)
                    {
                        existingProps |= SetPropertyType.GroupCaptureNumber;
                        propToSet ^= SetPropertyType.GroupCaptureNumber;
                        prop.ExpressionType = DTSCustomPropertyExpressionType.CPET_NOTIFY;
                    }
                    else if (prop.Name == Resources.RegExExtractionMatchNumberPropertyName)
                    {
                        existingProps |= SetPropertyType.MatchNumber;
                        propToSet ^= SetPropertyType.MatchNumber;
                        prop.ExpressionType = DTSCustomPropertyExpressionType.CPET_NOTIFY;
                    }
                    else if (prop.Name == Resources.RegExExtractionRegexOutputType)
                    {
                        existingProps |= SetPropertyType.RegexOutputType;
                        propToSet ^= SetPropertyType.RegexOutputType;
                    }
                    else if (prop.Name == Resources.RegExExtractionRegexOutput)
                    {
                        existingProps |= SetPropertyType.RegexOutput;
                        propToSet ^= SetPropertyType.RegexOutput;
                    }
                    else if (prop.Name == Resources.RegExExtractionGroupNumberPropertyName)
                    {
                        groupNo = (int)prop.Value;
                        propsToRemove.Add(prop.ID);
                    }
                    else if (prop.Name == Resources.RegExExtractionGroupNamePropertyName)
                    {
                        groupName = prop.Value.ToString();
                        propsToRemove.Add(prop.ID);
                    }
                    else if (prop.Name == Resources.RegExExtractionRegexOptions)
                    {
                        existingProps |= SetPropertyType.RegexOptions;
                        propToSet ^= SetPropertyType.RegexOptions;
                    }
                    else
                        propsToRemove.Add(prop.ID);
                }

                if (existingProps != SetPropertyType.All)
                    SetOutputColumnProperties(oCol, propToSet, vInput, inputLineageID);

                if (groupNo > 0)
                {
                    IDTSCustomProperty type = oCol.CustomPropertyCollection[Resources.RegExExtractionRegexOutputType];
                    type.Value = RegexOutputType.CaptureGroupNumber;
                    IDTSCustomProperty rOut = oCol.CustomPropertyCollection[Resources.RegExExtractionRegexOutput];
                    rOut.Value = groupNo;
                }
                else if (!string.IsNullOrEmpty(groupName))
                {
                    IDTSCustomProperty type = oCol.CustomPropertyCollection[Resources.RegExExtractionRegexOutputType];
                    type.Value = RegexOutputType.CaptureGroupName;
                    IDTSCustomProperty rOut = oCol.CustomPropertyCollection[Resources.RegExExtractionRegexOutput];
                    rOut.Value = groupName;
                }


                //Remove not supported properties
                foreach (int propID in propsToRemove)
                {
                    oCol.CustomPropertyCollection.RemoveObjectByID(propID);
                }
            }
        }


        /// <summary>
        /// Fires ComponentMetadata Error
        /// </summary>
        /// <param name="errorCode">Error code to fire</param>
        /// <param name="message">Error message to fire</param>
        /// <returns>Boleean representing cancel event</returns>
        private bool FireComponentMetadataError(int errorCode, string message)
        {
            bool cancel;
            ComponentMetaData.FireError(errorCode, ComponentMetaData.Name, message, string.Empty, 0, out cancel);
            return cancel;
        }

        #endregion


    }
}
