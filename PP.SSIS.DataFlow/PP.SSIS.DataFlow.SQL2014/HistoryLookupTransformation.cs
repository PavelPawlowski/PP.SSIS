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
using Microsoft.SqlServer.Dts.Pipeline;
using Microsoft.SqlServer.Dts.Pipeline.Wrapper;
using Microsoft.SqlServer.Dts.Runtime.Wrapper;
using System.Globalization;
using PP.SSIS.DataFlow.Properties;
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
        DisplayName = "History Lookup"
        , Description = "Handles History Lookup"
        , ComponentType = ComponentType.Transform
        , IconResource = "PP.SSIS.DataFlow.Resources.HistoryLookup.ico"
        , CurrentVersion = 2
        , SupportsBackPressure = true

#if SQL2019
    , UITypeName = "PP.SSIS.DataFlow.UI.HistoryLookupTransformationUI, PP.SSIS.DataFlow.SQL2019, Version=1.0.0.0, Culture=neutral, PublicKeyToken=b68691a82d4fb69c"
#endif
#if SQL2017
    , UITypeName = "PP.SSIS.DataFlow.UI.HistoryLookupTransformationUI, PP.SSIS.DataFlow.SQL2017, Version=1.0.0.0, Culture=neutral, PublicKeyToken=b68691a82d4fb69c"
#endif
#if SQL2016
    , UITypeName = "PP.SSIS.DataFlow.UI.HistoryLookupTransformationUI, PP.SSIS.DataFlow.SQL2016, Version=1.0.0.0, Culture=neutral, PublicKeyToken=b99aee68060fa342"
#endif
#if SQL2014
    , UITypeName = "PP.SSIS.DataFlow.UI.HistoryLookupTransformationUI, PP.SSIS.DataFlow, Version=1.0.0.0, Culture=neutral, PublicKeyToken=6926746b040a83a5"
#endif
#if SQL2012
        , UITypeName = "PP.SSIS.DataFlow.UI.HistoryLookupTransformationUI, PP.SSIS.DataFlow, Version=1.0.0.0, Culture=neutral, PublicKeyToken=c8e167588e3f3397"
#endif
#if SQL2008
        , UITypeName = "PP.SSIS.DataFlow.UI.HistoryLookupTransformationUI, PP.SSIS.DataFlow, Version=1.0.0.0, Culture=neutral, PublicKeyToken=e17db8c64cd5b65b"
#endif
)
    ]
    public class HistoryLookupTransformation : PipelineComponent
    {
        #region Definitions

        private static readonly ICollection<DataType> _suppoertedDateColumnTypes = new DataType[] { DataType.DT_DATE, DataType.DT_DBDATE, DataType.DT_DBTIME, DataType.DT_DBTIME2,
            DataType.DT_DBTIMESTAMP, DataType.DT_DBTIMESTAMP2, DataType.DT_DBTIMESTAMPOFFSET, DataType.DT_I1, DataType.DT_I2, DataType.DT_I4, DataType.DT_I8, DataType.DT_UI1,
            DataType.DT_UI2, DataType.DT_UI4, DataType.DT_UI8, DataType.DT_R4, DataType.DT_R8 };
        /// <summary>
        /// Gets InputColumn DataTypes supported by this transformation as Date Columns
        /// </summary>
        public static ICollection<DataType> SupportedDateColumnCypes
        {
            get { return _suppoertedDateColumnTypes; }
        }

        private static readonly ICollection<DataType> _unsupportedInputTypes = new DataType[] { DataType.DT_TEXT, DataType.DT_NTEXT, DataType.DT_IMAGE};
        /// <summary>
        /// Gets InputColumn DataTypes not supported as Input
        /// </summary>
        public static ICollection<DataType> UnsupportedInputTypes
        {
            get { return _unsupportedInputTypes; }
        }


        private bool inputColumnsValid = true;

        [Flags]
        internal enum CustomProperties
        {                                 
            //Component Wite
            None                    =   0x00000,
            KeyHashAlgorithm        =   0x00001,
            NoMatchBehavior         =   0x00002,
            CacheType               =   0x00004,
            NoMatchCacheSize        =   0x00008,
            DefaultCacheSize        =   0x00010,

            AllComponentWide       =  KeyHashAlgorithm | NoMatchBehavior | CacheType | NoMatchCacheSize | DefaultCacheSize

            //ColumnsWide
            ,InputColumnUsageType   =   0x00100,
            DateComparison          =   0x00200,
            LookupKeyLineageID      =   0x00400,
            SourceLineageID         =   0x00800,
            OutputColumnLineageID   =   0x01000,

            AllColumnWide           = InputColumnUsageType | DateComparison | LookupKeyLineageID


            ,All                    = AllComponentWide | AllColumnWide
        }

        /// <summary>
        /// Specifies the NoMatch behavior for the component
        /// </summary>
        public enum NoMatchBehavior
        {
            /// <summary>
            /// Component will fail in case of lookup failure
            /// </summary>
            FailComponent,
            /// <summary>
            /// Rows will be redirected to error output in case of lookup failure
            /// </summary>
            RedirectToErrorOutput,
            /// <summary>
            /// Rows will be redirected to NoMatch output in case of lookup failure
            /// </summary>
            RedirectToNoMatchOutput,
            /// <summary>
            /// Looup failures will be ignored
            /// </summary>
            IgnoreError
        }        

        /// <summary>
        /// Specifies the usage of the InputColumn
        /// </summary>
        [Flags]
        public enum InputColumnUsageType
        {
            /// <summary>
            /// Usage type is not specified
            /// </summary>
            None                = 0x000,
            /// <summary>
            /// Column is ussed as Key Lookup Column
            /// </summary>
            LookupColumn        = 0x001,
            /// <summary>
            /// Column is used as DateColumn in the data input stream
            /// </summary>
            DateColumn          = 0x002,
            /// <summary>
            /// Column is used as DateFrom column in the lookup cache input
            /// </summary>
            DateFromColumn      = 0x004,
            /// <summary>
            /// Column is used as DateTo column inthe lookup cache input
            /// </summary>
            DateToColumn        = 0x008,  
            /// <summary>
            /// Column is being used as Output column from the Lookup
            /// </summary>
            OutputColumn        = 0x010,
            /// <summary>
            /// Column is being used as Lookup and Output column
            /// </summary>
            LookupAndOutputColumn = LookupColumn | OutputColumn,
            /// <summary>
            /// Column is being used ad DateFrom and OutputColumn
            /// </summary>
            DateFromAndOutputColumn = DateFromColumn | OutputColumn,
            /// <summary>
            /// Column is being used as DateTo and OutputColumns
            /// </summary>
            DateToAndOutputColumn = DateToColumn | OutputColumn
        }

        /// <summary>
        /// Specifies the type of cache for the HistoryLookup Component
        /// </summary>
        public enum CacheType
        {
            /// <summary>
            /// Full Cache. All input rows will be cached
            /// </summary>
            Full
            //Partial
        }
        
        /// <summary>
        /// Specifies the Date range comparison for the DateFrom and DateTo columns
        /// </summary>
        public enum DateComparison
        {
            /// <summary>
            /// None this is being used for other columns than DateFrom and DateTo
            /// </summary>
            None                = 0x00,
            /// <summary>
            /// Date value from Data Streem must be greater than DateFrom value
            /// </summary>
            Greater             = 0x01,
            /// <summary>
            /// Date value from Data Streem must be equal to or greater than DateFrom value
            /// </summary>
            GreaterOrEqual      = 0x02,
            /// <summary>
            /// Date value from Data Stream must be lower than DateTo value
            /// </summary>
            Lower               = 0x04,
            /// <summary>
            /// Date value from DataStream must be equal to or lower than DateTo value
            /// </summary>
            LowerOrEqual        = 0x08
        }

        /// <summary>
        /// Defines Column mapping between the Data Input and The lookkup cache INput
        /// </summary>
        private class ColumnMapping
        {
            public ColumnMapping(int dataFieldLineageID, int lookupFiledLineageID, int cacheIndex, int outputColumnSourceLineageID, int outputColumnLineageID, int keyPosition)
            {
                DataFieldIndex = -1;
                DataFieldLineageID = dataFieldLineageID;
                LookupFieldIndex = -1;
                LookupFieldLineageID = lookupFiledLineageID;
                OutputColumnCacheIndex = cacheIndex;
                OutputColumnLineageID = outputColumnLineageID;
                KeyPosition = keyPosition;
                OutputColumnIndex = -1;
                OutputColumnSourceLineageID = outputColumnSourceLineageID;
                OutputColumnSourceIndex = -1;
            }

            /// <summary>
            /// Index of column which servers as Lookup Key in the Data Input
            /// </summary>
            internal int DataFieldIndex;
            /// <summary>
            /// LineageID of column which serves as Lookup Key in the Data Input
            /// </summary>
            internal int DataFieldLineageID;

            internal int DataFieldID;
            internal int LookupFieldLineageID;
            /// <summary>
            /// Index of the column which serves as Lookup Key in the Lookup Chage Input
            /// </summary>
            internal int LookupFieldIndex;

            internal int OutputColumnSourceLineageID;
            internal int OutputColumnSourceIndex;

            /// <summary>
            /// Index Output Column Index in the LooukpMatch Output
            /// </summary>
            internal int OutputColumnIndex;
            internal int OutputColumnLineageID;
            /// <summary>
            /// Index of the cache buffer wihich will provide data for the OutputColumn in the LookupMatch Output
            /// </summary>
            public int OutputColumnCacheIndex;
            /// <summary>
            /// Index position in the Key composition
            /// </summary>
            internal int KeyPosition;
        }

        /// <summary>
        /// Privte Class to cover all the mappings needed for HistoryLookup
        /// </summary>
        private class LookupMappings
        {
            public LookupMappings()
            {
                DateFromColumnIndex = -1;
                DateToColumnIndex = -1;
                DateColumnIndex = -1;
            }

            private List<ColumnMapping> _columnsMappings = new List<ColumnMapping>();
            /// <summary>
            /// Gets  or Sets Columns Mappings List
            /// </summary>
            public List<ColumnMapping> ColumnsMappings
            {
                get { return _columnsMappings; }
                set { _columnsMappings = value; }
            }

            private List<ColumnMapping> _outputMappings = new List<ColumnMapping>();
            public List<ColumnMapping> OutputColumnMappings
            {
                get
                {
                    return _outputMappings;
                }
            }

            private List<ColumnMapping> _keyMappings = new List<ColumnMapping>();
            public List<ColumnMapping> KeyMapppings
            {
                get
                {
                    return _keyMappings;
                }
            }


            /// <summary>
            /// Gets or Sets Date Column Index
            /// </summary>
            public int DateColumnIndex { get; set; }

            public int DateColumnID { get; internal set; }

            public int DateColumnLineageID { get; internal set; }

            internal int DateFromLineageID;

            /// <summary>
            /// Gets or Sets DateFrom ColumnIndex
            /// </summary>
            public int DateFromColumnIndex { get; set; }
            /// <summary>
            /// Gets or Sets DateFrom Comparison
            /// </summary>
            public DateComparison DateFromComparison { get; set; }
            internal int DateToLineageID;

            /// <summary>
            /// Gets or Sets DateTo ColumnIndex
            /// </summary>
            public int DateToColumnIndex { get; set; }
            /// <summary>
            /// Gets or Sets DateTo Comparison
            /// </summary>
            public DateComparison DateToComparison { get; set; }

            /// <summary>
            /// Gets or Sets the total count of Columns serving as Lookup Key for cache allocation purposes
            /// </summary>
            public int KeyColumnsCount { get; set; }
            /// <summary>
            /// Gets or Set the total number of Ouitput columns for cache allocation purposes
            /// </summary>
            public int DataColumnsCount { get; set; }

            /// <summary>
            /// Gets or Sets NoMatchBehavior of the Component
            /// </summary>
            public NoMatchBehavior NoMatchBehavior = NoMatchBehavior.FailComponent;

            /// <summary>
            /// Initializes list of output columns mappings to limit theloops through all mappings when retrieving data
            /// </summary>
            internal void InitializeColumnsMappings(IDTSBufferManager100 bufferManager, int inputBufferID, int lookupBufferID)
            {
                _outputMappings = ColumnsMappings.FindAll(cm => cm.OutputColumnLineageID != -1);
                _keyMappings = ColumnsMappings.FindAll(cm => cm.KeyPosition != -1);

                DataColumnsCount = _outputMappings.Count;
                KeyColumnsCount = _keyMappings.Count;

                if (KeyColumnsCount > 0)
                {
                    foreach (ColumnMapping cm in KeyMapppings)
                    {
                        cm.DataFieldIndex = bufferManager.FindColumnByLineageID(inputBufferID, cm.DataFieldLineageID);
                        cm.LookupFieldIndex = bufferManager.FindColumnByLineageID(lookupBufferID, cm.LookupFieldLineageID);
                    }

                }

                if (DateColumnLineageID > 0)
                    DateColumnIndex = bufferManager.FindColumnByLineageID(inputBufferID, DateColumnLineageID);
                if (DateFromLineageID > 0)
                    DateFromColumnIndex = bufferManager.FindColumnByLineageID(lookupBufferID, DateFromLineageID);
                if (DateToLineageID > 0)
                    DateToColumnIndex = bufferManager.FindColumnByLineageID(lookupBufferID, DateToLineageID);

                DateFields = DateColumnIndex >= 0 && DateFromColumnIndex >= 0 && DateToColumnIndex >= 0;

                if (DataColumnsCount > 0)
                {
                    int cacheIndex = 2; //We start on index 2 as index 0 and 1 is reserverd for DateFrom and DateTo;

                    foreach (ColumnMapping cm in OutputColumnMappings)
                    {
                        cm.OutputColumnSourceIndex = bufferManager.FindColumnByLineageID(lookupBufferID, cm.OutputColumnSourceLineageID);
                        cm.OutputColumnIndex = bufferManager.FindColumnByLineageID(inputBufferID, cm.OutputColumnLineageID);
                        cm.OutputColumnCacheIndex = cacheIndex++;
                    }
                }
            }

            public bool DateFields { get; private set; }

            internal void CleaupCache()
            {
                _outputMappings = new List<DataFlow.HistoryLookupTransformation.ColumnMapping>();
                _keyMappings = new List<DataFlow.HistoryLookupTransformation.ColumnMapping>();
                _columnsMappings = new List<DataFlow.HistoryLookupTransformation.ColumnMapping>();
            }
        }
        #endregion

        #region Private Fields
        private LookupMappings _mappings = new LookupMappings(); //Mapping for History Lookup Component

        HashAlgorithmType _keyHashAlgorithm = HashAlgorithmType.MD5;
        //History Lookup Cache
        Dictionary<KeyDataHash, List<object[]>> _cache;
        Dictionary<KeyDataHash, int> _noMatchCache;
        int _noMatchCacheSize;
        DateTime _cacheStart;

        bool _cacheProcessingStarted = false;
        bool _cacheProcessed = false;                   //Indicates whether case has already been processed. Used to determine whether Input Data buffer proocessing can be started
        long _cacheRowsProcessed = 0;
        long _rootCacheEntries = 0;
        long _rowsIncacheCount = 0;

        KeyDataHash _lastCacheHash = null;              //Hash of the last row processed during buffer processing. It is being checked to avoid subseuqent lookups of the same hash value
        object[] _lastDataEntry = null;                 //Data entry of last row processed. This is to avoid unnecessary data lookup if it is same as previous row
        List<object[]> _lastHashDataEntries = null;     //Data Entries of Has processed last time during buffer processing. This si toavoid unnecessary hash lookups if new rows has the same has as previous one
        long rowsProcessed = 0;

        int matchOutputID;
        int noMatchOutputID;
        int errorOutputID;
        int startDateComparisonResult;
        int endDateComparisonResult;

        bool duplicateCacheValueWarning = false;
        bool dateColumns = false;
        #endregion

        #region DesignTime
        /// <summary>
        /// Provides SSIS Component Properties
        /// </summary>
        public override void ProvideComponentProperties()
        {            
            this.ComponentMetaData.Version = PipelineUtils.GetPipelineComponentVersion(this); 

            //Set component properties
            this.ComponentMetaData.Name = "HLT_HistoryLookup";
            this.ComponentMetaData.Description = "Handles lookup in historical data";
            this.ComponentMetaData.ContactInfo = Resources.TransformationContactInfo;

            // Reset the component.
            base.RemoveAllInputsOutputsAndCustomProperties();

            //call base method to insert input and output
            base.ProvideComponentProperties();

            //Set DataInput properties
            IDTSInput dataInput = ComponentMetaData.InputCollection[0];
            dataInput.Name = "Data Input";
            dataInput.Description = "Contains rows for which data should be looked up";

            IDTSInput lookupInput = ComponentMetaData.InputCollection.New();
            lookupInput.Name = "History Lookup Input";
            lookupInput.Description = "Contains rows from which the historical  data will be searched";



            //Set History Lookup Match Output properties
            IDTSOutput matchOutput = ComponentMetaData.OutputCollection[0];
            matchOutput.Name = "History Lookup Match Output";
            matchOutput.Description = "Contains row for which lookup was successfull or lookup errors were ignored";
            matchOutput.SynchronousInputID = dataInput.ID;
            matchOutput.IsErrorOut = false;
            matchOutput.ExclusionGroup = 1;

            //Set History Lookup NoMatch Output properties
            IDTSOutput noMatchOutput = ComponentMetaData.OutputCollection.New();
            noMatchOutput.Name = "History Lookup No Match Output";
            noMatchOutput.Description = "Contains row for which lookup was not successfull and not redirected to error output";
            noMatchOutput.SynchronousInputID = dataInput.ID;
            //noMatchOutput.Dangling = true;
            noMatchOutput.ExclusionGroup = 1;

            //Set History Lookup Error output properties
            IDTSOutput errorOutput = ComponentMetaData.OutputCollection.New();
            errorOutput.Name = "History Lookup Error Output";
            errorOutput.Description = "Contains row  for which lookup has failed";
            //errorOutput.Dangling = true;
            errorOutput.IsErrorOut = true;
            errorOutput.SynchronousInputID = dataInput.ID;
            errorOutput.ExclusionGroup = 1;

            CreateCustomProperties(CustomProperties.AllComponentWide);
        }

        /// <summary>
        /// Validate HashColumnsTransform metadata
        /// </summary>
        /// <returns></returns>
        public override DTSValidationStatus Validate()
        {
            //Validate number of Inputs (Should be 2 - Data Input and Lookup Input
            if (ComponentMetaData.InputCollection.Count != 2)
            {
                FireComponentMetadataError(0, Resources.ErrorIncorrectNumberOfInputs);
                return DTSValidationStatus.VS_NEEDSNEWMETADATA;
            }

            //Validate number of Outputs (should be 3 - Match Output, No Match Output, Error Output)
            if (ComponentMetaData.OutputCollection.Count != 3)
            {
                FireComponentMetadataError(0, Resources.ErrorIncorrectNumberOfOutputs);
                return DTSValidationStatus.VS_NEEDSNEWMETADATA;
            }

            //Get Inputs
            IDTSInput dataInput = ComponentMetaData.InputCollection[0];
            IDTSInput lookupInput = ComponentMetaData.InputCollection[1];

            ///Date Comparison Columns
            DataType dateCol = DataType.DT_EMPTY;
            DataType dateFromCol = DataType.DT_EMPTY;
            DataType dateToCol = DataType.DT_EMPTY;

            int KeyColMappings = 0;

            if (!dataInput.IsAttached)
            {
                FireComponentMetadataError(0, "Data Input is not Attached");
                return DTSValidationStatus.VS_ISBROKEN;
            }

            if (!lookupInput.IsAttached)
            {
                FireComponentMetadataError(0, "Lookup Input is not Attached");
                return DTSValidationStatus.VS_ISBROKEN;

            }
            

            //Validate Data Key Columns
            foreach (IDTSInputColumn icol in dataInput.InputColumnCollection)
            {
                IDTSCustomProperty uTypeProp = icol.CustomPropertyCollection[GetPropertyname(CustomProperties.InputColumnUsageType)];
                InputColumnUsageType usageType = (InputColumnUsageType)uTypeProp.Value;

                IDTSCustomProperty lookLinIDProp = icol.CustomPropertyCollection[GetPropertyname(CustomProperties.LookupKeyLineageID)];
                int lookupLineageID = (int)lookLinIDProp.Value;


                //if UsageType is LookupColumn, gegerate mapping in the MappingView
                if ((usageType & InputColumnUsageType.LookupColumn) == InputColumnUsageType.LookupColumn && lookupLineageID > 0)
                {
                    IDTSInputColumn lCol = null;
                    try
                    {
                        lCol = lookupInput.InputColumnCollection.GetInputColumnByLineageID(lookupLineageID);
                        IDTSCustomProperty lCustProp = lCol.CustomPropertyCollection[GetPropertyname(CustomProperties.InputColumnUsageType)];
                        InputColumnUsageType uType = (InputColumnUsageType)lCustProp.Value;
                        if ((uType & InputColumnUsageType.LookupColumn) == InputColumnUsageType.LookupColumn)
                            KeyColMappings++;
                        else
                            throw new Exception("Invalid Mapping");
                    }
                    catch
                    {
                        FireComponentMetadataError(0, "There are invalid mapping between key columns");
                        return DTSValidationStatus.VS_ISBROKEN;
                    }

                }

                //If UsageType is DateColumn, then assing the LineageID to the _historyLookupProperties for editing
                if ((usageType & HistoryLookupTransformation.InputColumnUsageType.DateColumn) == HistoryLookupTransformation.InputColumnUsageType.DateColumn)
                    dateCol = icol.DataType;
            }

            foreach (IDTSInputColumn icol in lookupInput.InputColumnCollection)
            {
                IDTSCustomProperty uTypeProp = icol.CustomPropertyCollection[GetPropertyname(CustomProperties.InputColumnUsageType)];
                InputColumnUsageType usageType = (InputColumnUsageType)uTypeProp.Value;
                IDTSCustomProperty compProp = icol.CustomPropertyCollection[GetPropertyname(CustomProperties.DateComparison)];


                //If UsageType is DateFromColumn, then assing the LineageID and DateComparison to the _historyLookupProperties for editing
                if ((usageType & InputColumnUsageType.DateFromColumn) == InputColumnUsageType.DateFromColumn)
                    dateFromCol = icol.DataType;

                //If UsageType is DateToColumn, then assing the LineageID and DateComparison to the _historyLookupProperties for editing
                if ((usageType & InputColumnUsageType.DateToColumn) == InputColumnUsageType.DateToColumn)
                    dateToCol = icol.DataType;
            }

            if (
                (dateCol != DataType.DT_EMPTY || dateFromCol != DataType.DT_EMPTY || dateToCol != DataType.DT_EMPTY)
                &&
                (dateCol != dateFromCol || dateCol != dateToCol || dateFromCol != dateToCol)
            )
            {
                FireComponentMetadataError(0, "Date comparison columns are not of the same type or not all data comparision columns are specified");
                return DTSValidationStatus.VS_ISBROKEN;
            }

            IDTSOutput matchOutput = ComponentMetaData.OutputCollection[0];
            foreach (IDTSOutputColumn oCol in matchOutput.OutputColumnCollection)
            {
                IDTSCustomProperty prop = oCol.CustomPropertyCollection[GetPropertyname(CustomProperties.SourceLineageID)];
                int srcLineageID = (int)prop.Value;

                IDTSInputColumn iCol = null;

                try
                {
                    iCol = lookupInput.InputColumnCollection.GetInputColumnByLineageID(srcLineageID);
                    IDTSCustomProperty uTypeProp = iCol.CustomPropertyCollection[GetPropertyname(CustomProperties.InputColumnUsageType)];
                    InputColumnUsageType usage = (InputColumnUsageType)uTypeProp.Value;
                    if ((usage & InputColumnUsageType.OutputColumn) != InputColumnUsageType.OutputColumn)
                        throw new Exception("Invalid Output Column Mapping");
                }
                catch
                {
                    FireComponentMetadataError(0, "THere are invalid output columns Mappings");
                    return DTSValidationStatus.VS_ISBROKEN;
                }               
            }

            if (KeyColMappings == 0)
            {
                FireComponentMetadataError(0, "Key column mappings are not defined");
                return DTSValidationStatus.VS_ISBROKEN;
            }

            return DTSValidationStatus.VS_ISVALID;
        }

        private void VerifyOutputs()
        {
            //Get Outputs
            IDTSOutput matchOutput = ComponentMetaData.OutputCollection[0];
            IDTSOutput noMatchOutput = ComponentMetaData.OutputCollection[1];
            IDTSOutput errorOutput = ComponentMetaData.OutputCollection[2];

            NoMatchBehavior behave = (NoMatchBehavior)ComponentMetaData.CustomPropertyCollection[GetPropertyname(CustomProperties.NoMatchBehavior)].Value;
            switch (behave)
            {
                case NoMatchBehavior.FailComponent:
                    if (noMatchOutput.IsAttached)
                        FireComponentMetadataWarning(-1, "NoMatch Output is Attached, but NoMatchBehavior is set to FailComponent. No rows will be sent to NoMatch Output");
                    if (errorOutput.IsAttached)
                        FireComponentMetadataWarning(-1, "Error Output is Attached, but NoMatchBehavior is set to FailComponent. No rows will be sent to Error Output");

                    break;
                case NoMatchBehavior.RedirectToErrorOutput:
                    if (noMatchOutput.IsAttached)
                        FireComponentMetadataWarning(-1, "NoMatch Output is Attached, but NoMatchBehavior is set to RedirecToErrorOutput. No rows will be sent to NoMatch Output");
                    break;
                case NoMatchBehavior.RedirectToNoMatchOutput:
                    if (noMatchOutput.IsAttached)
                        FireComponentMetadataWarning(-1, "Error Output is Attached, but NoMatchBehavior is set to RedirecToNoMatchOutput. No rows will be sent to Error Output");
                    break;
                case NoMatchBehavior.IgnoreError:
                    if (noMatchOutput.IsAttached)
                        FireComponentMetadataWarning(-1, "NoMatch Output is Attached, but NoMatchBehavior is set to IgnoreError. No rows will be sent to NoMatch Output");
                    if (errorOutput.IsAttached)
                        FireComponentMetadataWarning(-1, "Error Output is Attached, but NoMatchBehavior is set to IgnoreError. No rows will be sent to Error Output");
                    break;
                default:
                    break;
            }

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
                CustomProperties propsToCreate = CustomProperties.All;

                //Check for missing properties
                foreach (IDTSCustomProperty cprop in ComponentMetaData.CustomPropertyCollection)
                {
                    if (cprop.Name == "KeyHashAlgorithm")
                        propsToCreate ^= CustomProperties.KeyHashAlgorithm;
                    else if (cprop.Name == "NoMatchBehavior")
                        propsToCreate ^= CustomProperties.NoMatchBehavior;
                    else if (cprop.Name == "CacheType")
                        propsToCreate ^= CustomProperties.CacheType;
                    else if (cprop.Name == "NoMatchCacheSize")
                        propsToCreate ^= CustomProperties.NoMatchCacheSize;
                    else if (cprop.Name == "DefaultCacheSize")
                        propsToCreate ^= CustomProperties.DefaultCacheSize;
                }

                CreateCustomProperties(propsToCreate);

                ComponentMetaData.Version = currentVersion;
            }
        }

        public override void OnInputPathAttached(int inputID)
        {
            //int inputIndex = ComponentMetaData.InputCollection.GetObjectIndexByID(inputID);
            base.OnInputPathAttached(inputID);
            //IDTSInput input = ComponentMetaData.InputCollection.GetObjectByID(inputID);            
        }

        public override void OnOutputPathAttached(int outputID)
        {
            base.OnOutputPathAttached(outputID);

            //IDTSOutput output = ComponentMetaData.OutputCollection.GetObjectByID(outputID);
            //output.Dangling = false;
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
            //DTSUsageType uType = DTSUsageType.UT_IGNORED;//  usageType == DTSUsageType.UT_READWRITE ? DTSUsageType.UT_READONLY : usageType;
            DTSUsageType uType = usageType;

            IDTSInputColumn col = base.SetUsageType(inputID, virtualInput, lineageID, uType);

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

                if (ComponentMetaData.OutputCollection.Count >0)
                {
                    ComponentMetaData.OutputCollection[0].OutputColumnCollection.RemoveAll();
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
            throw new InvalidOperationException(Resources.ErrorDeleteInput);
            //if (outputID != ComponentMetaData.OutputCollection[0].ID)
            //    throw new Exception("Output Columns can be added only to the Error Output");

            ////Insert new column into specified output
            //IDTSOutput output = ComponentMetaData.OutputCollection.FindObjectByID(outputID);

            //bool haveErrorColumnName = false;
            //bool haveErrorDescription = false;

            //foreach (IDTSOutputColumn oCol in output.OutputColumnCollection)
            //{
            //    foreach (IDTSCustomProperty prop in oCol.CustomPropertyCollection)
            //    {
            //        if (prop.Name == Resources.ErrorMetadataOutputColumnTypeName)
            //        {
            //            OutputColumnType colType = (OutputColumnType)prop.Value;
            //            if (colType == OutputColumnType.ErrorColumnName)
            //            {
            //                haveErrorColumnName = true;
            //                break;
            //            }
            //            if (colType == OutputColumnType.ErrorDescription)
            //            {
            //                haveErrorDescription = true;
            //                break;
            //            }
            //        }
            //    }
            //    if (haveErrorDescription && haveErrorColumnName)
            //        break;
            //}


            //IDTSOutputColumn outputColumn = output.OutputColumnCollection.NewAt(outputColumnIndex);
            //outputColumn.Name = name;
            //outputColumn.Description = description;

            //if (!haveErrorColumnName)
            //    SetOutputColumnProperties(outputColumn, OutputColumnType.ErrorColumnName);
            //else if (!haveErrorDescription)
            //    SetOutputColumnProperties(outputColumn, OutputColumnType.ErrorDescription);
            //else
            //    SetOutputColumnProperties(outputColumn, OutputColumnType.ErrorColumnName);

            ////SetXmlColumnProperties(xmlColumn);

            //return outputColumn;
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
            throw new Exception(string.Format(Resources.ErrorOutputColumnPropertyCannotBeChanged, propertyName));
        }

        public override IDTSCustomProperty SetInputColumnProperty(int inputID, int inputColumnID, string propertyName, object propertyValue)
        {
            throw new Exception(string.Format(Resources.ErrorInputColumnPropertyCannotBeChanged, propertyName));
        }

        public override IDTSCustomProperty SetComponentProperty(string propertyName, object propertyValue)
        {
            IDTSCustomProperty prop = base.SetComponentProperty(propertyName, propertyValue);


            return prop;
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

            VerifyOutputs();

            rowsProcessed = 0;
            _cacheProcessed = false;
            _cacheProcessingStarted = false;
            _mappings = new LookupMappings();

            //Get Inputs and Outputs
            IDTSInput dataInput = ComponentMetaData.InputCollection[0];
            IDTSInput lookupInput = ComponentMetaData.InputCollection[1];
            IDTSOutput matchOutput = ComponentMetaData.OutputCollection[0];

            //Get the CacheSize and initialize new Cache
            int cacheSize = (int)ComponentMetaData.CustomPropertyCollection[GetPropertyname(CustomProperties.DefaultCacheSize)].Value;
            _cache = new Dictionary<KeyDataHash, List<object[]>>(cacheSize);

            _noMatchCacheSize = (int)ComponentMetaData.CustomPropertyCollection[GetPropertyname(CustomProperties.NoMatchCacheSize)].Value;
            _noMatchCache = new Dictionary<KeyDataHash, int>(_noMatchCacheSize);

            _mappings.NoMatchBehavior = (NoMatchBehavior)ComponentMetaData.CustomPropertyCollection[GetPropertyname(CustomProperties.NoMatchBehavior)].Value;

            _keyHashAlgorithm = (HashAlgorithmType)ComponentMetaData.CustomPropertyCollection[GetPropertyname(CustomProperties.KeyHashAlgorithm)].Value;

            int lookupKeyColumns = 0; //Counts number of the KeyLookupColumns;

            //Go through input columns and generate mappings for them
            for (int i = 0; i < dataInput.InputColumnCollection.Count; i++)
            {
                IDTSInputColumn iCol = dataInput.InputColumnCollection[i];

                InputColumnUsageType usageType = InputColumnUsageType.None;
                int lookupLinageId = -1;

                //Iterate though InputColumns and generatae mappings based on the usega type
                foreach (IDTSCustomProperty prop in iCol.CustomPropertyCollection)
                {
                    if (prop.Name == GetPropertyname(CustomProperties.InputColumnUsageType))
                        usageType = (InputColumnUsageType)prop.Value;
                    else if (prop.Name == GetPropertyname(CustomProperties.LookupKeyLineageID)) //Key Lookup Column cotains LineageID of corresponding column in the LookupCache Input
                    {
                        lookupLinageId = (int)prop.Value;
                    }
                }

                if (usageType != InputColumnUsageType.None)
                {
                    //If usage type is lookukp column, then it is a Key column and we create mapping for it
                    //At the beginning it does not have CacheIndex or Output index as we do not know if it is also output  column
                    if ((usageType & InputColumnUsageType.LookupColumn) == InputColumnUsageType.LookupColumn)
                    {
                        var cm = new ColumnMapping(iCol.LineageID, lookupLinageId, -1, -1, -1, lookupKeyColumns++);
                        cm.DataFieldID = iCol.ID;
                        _mappings.ColumnsMappings.Add(cm);
                    }

                    //If the UsateType is DateColumn, assigin it to appropriate field
                    if ((usageType & InputColumnUsageType.DateColumn) == InputColumnUsageType.DateColumn)
                    {
                        _mappings.DateColumnID = iCol.ID;
                        _mappings.DateColumnLineageID = iCol.LineageID;
                    }
                }
            }

            //Assign current number of LookupKey Columns
            _mappings.KeyColumnsCount = lookupKeyColumns;

            //Process Lookup Cache Input columns to determine Output Columns
            for (int i = 0; i < lookupInput.InputColumnCollection.Count; i++)
			{
                IDTSInputColumn iCol = lookupInput.InputColumnCollection[i];

                InputColumnUsageType usageType = InputColumnUsageType.None;
                DateComparison comparison = DateComparison.None;
                //int outputIndex = -1;
                int outputLineageID = -1;

                foreach (IDTSCustomProperty prop in iCol.CustomPropertyCollection)
                {
                    if (prop.Name == GetPropertyname(CustomProperties.InputColumnUsageType))
                    {
                        usageType = (InputColumnUsageType)prop.Value;
                    }
                    else if (prop.Name == GetPropertyname(CustomProperties.OutputColumnLineageID)) //Columns marked for output have LineageID of corresponding Output Columns  in the MatchInput
                    {
                        outputLineageID = (int)prop.Value;
                    }
                    else if (prop.Name == GetPropertyname(CustomProperties.DateComparison))
                    {
                        comparison = (DateComparison)prop.Value;
                    }
                }

                //if usage is DateFrom column, assign it and assign also ComparisionType
                if ((usageType & InputColumnUsageType.DateFromColumn) == InputColumnUsageType.DateFromColumn)
                {
                    _mappings.DateFromLineageID = iCol.LineageID;
                    _mappings.DateFromComparison = comparison;
                }
                //If usage is DateTo column, assign it ansd assign also Comparison Type
                if ((usageType & InputColumnUsageType.DateToColumn) == InputColumnUsageType.DateToColumn)
                {
                    _mappings.DateToLineageID = iCol.LineageID;
                    _mappings.DateToComparison = comparison;
                }

                //If usage is OutputColumn, try to find existing KeyMapping referencing that column and assing the OutputColumnIndex
                if ((usageType & InputColumnUsageType.OutputColumn) == InputColumnUsageType.OutputColumn)
                {
                    ColumnMapping mmapping = _mappings.ColumnsMappings.Find(cm => cm.LookupFieldLineageID == iCol.LineageID);
                    //If existing mapping exists, assign OutputColumnIndex
                    if (mmapping != null)
                    {
                        mmapping.OutputColumnSourceLineageID = iCol.LineageID;
                        mmapping.OutputColumnLineageID = outputLineageID;
                    }
                    else //If existing mapping does not exists, create a new mapping for Ouitput Column 
                    {
                        ColumnMapping mapping = new ColumnMapping(-1, -1, -1, iCol.LineageID, outputLineageID,  -1); //Initially CacheIndex is not set
                        _mappings.ColumnsMappings.Add(mapping);
                    }
                }
			}

            ///Initalize Columns mappings to buffer Indexe
            _mappings.InitializeColumnsMappings(BufferManager, dataInput.Buffer, lookupInput.Buffer);

            int errorOutputIndex = -1;
            //Initialize runtime variables
            matchOutputID = ComponentMetaData.OutputCollection[0].ID;
            noMatchOutputID = ComponentMetaData.OutputCollection[1].ID;
            GetErrorOutputInfo(ref errorOutputID, ref errorOutputIndex);
            
            startDateComparisonResult = _mappings.DateFromComparison == DateComparison.Greater ? 1 : 0;
            endDateComparisonResult = _mappings.DateToComparison == DateComparison.Lower ? -1 : 0;
            duplicateCacheValueWarning = false;
            dateColumns = _mappings.DateFromColumnIndex >= 0 && _mappings.DateToColumnIndex >= 0 && _mappings.DateColumnIndex >= 0;
            _rootCacheEntries = 0;
            _rowsIncacheCount = 0;
        }

        public override void IsInputReady(int[] inputIDs, ref bool[] canProcess)
        {
            for (int i = 0; i < inputIDs.Length; i++)
            {
                int index = ComponentMetaData.InputCollection.GetObjectIndexByID(inputIDs[i]);

                if (index == 1) //Cache Input
                    canProcess[i] = !_cacheProcessed;   //cache can be processed any time
                else if (index == 0) //Data Input
                    canProcess[i] = _cacheProcessed; //Data input can be processed only  after the cache is processed;
            }
        }

        /// <summary>
        /// Processes rows in the input buffer
        /// </summary>
        /// <param name="inputID">ID of the input to process the input rows</param>
        /// <param name="buffer">Pipeline bufere with rows to process</param>
        public override void ProcessInput(int inputID, PipelineBuffer buffer)
        {
            int inputIndex = ComponentMetaData.InputCollection.GetObjectIndexByID(inputID);

            if (inputIndex == 1) //Lookup Index - Load the Cache
                LoadCache(buffer);
            else if (inputIndex == 0) //Data Input - Process the data input and lookup data
                LookupData(buffer);
            else
                base.ProcessInput(inputID, buffer);
        }

        /// <summary>
        /// Processes the DataInput and handles the lookups
        /// </summary>
        /// <param name="buffer">PipelineBuffe of the DataInput</param>
        private void LookupData(PipelineBuffer buffer)
        {
            bool errorCancel = true;

            IComparable dateValue = null;

            object[] cacheEntry;

            if (!buffer.EndOfRowset)
            {
                while (buffer.NextRow())
                {
                    rowsProcessed++;
                    int errorColumnID = -1;

                    //Get the Key Columns and build the Key Hash
                    IComparable[] key = new IComparable[_mappings.KeyColumnsCount];
                    for (int i = 0; i < _mappings.KeyMapppings.Count; i++)
                    {
                        ColumnMapping cm = _mappings.KeyMapppings[i];
                        key[cm.KeyPosition] = buffer[cm.DataFieldIndex] as IComparable;
                    }

                    //Build the KeyDataHash for lookup
                    KeyDataHash kd = new KeyDataHash(key, _keyHashAlgorithm);
                    cacheEntry = null;

                    //If there are Date Comparision Columns specified, get the date
                    if (_mappings.DateColumnIndex >= 0)
                    {
                        //Get the date value
                        dateValue = buffer[_mappings.DateColumnIndex] as IComparable;
                        if (dateValue == null)
                        {
                            switch (_mappings.NoMatchBehavior)
                            {
                                case NoMatchBehavior.RedirectToErrorOutput:
                                    buffer.DirectErrorRow(errorOutputID, -2, _mappings.DateColumnLineageID);
                                    break;
                                case NoMatchBehavior.RedirectToNoMatchOutput:
                                    buffer.DirectRow(noMatchOutputID);
                                    break;
                                case NoMatchBehavior.IgnoreError:
                                    SetNullOutputColumns(buffer, _mappings.OutputColumnMappings);
                                    buffer.DirectRow(matchOutputID);
                                    break;
                                case NoMatchBehavior.FailComponent:
                                default:
                                    ComponentMetaData.FireError(0, ComponentMetaData.Name, string.Format("The range lookup value is not Comparable", rowsProcessed), null, 0, out errorCancel);
                                    if (errorCancel)
                                        return;
                                    break;
                            }
                        }
                    }

                    if (kd.Equals(_lastCacheHash))
                    {

                        if (_lastDataEntry != null && (_mappings.DateColumnIndex < 0 || (dateValue.CompareTo(_lastDataEntry[0]) >= startDateComparisonResult &&
                                dateValue.CompareTo(_lastDataEntry[1]) <= endDateComparisonResult)))
                        {
                            cacheEntry = _lastDataEntry;
                        }
                        else
                        {
                            if (_mappings.DateColumnIndex < 0)
                                cacheEntry = _lastHashDataEntries[0];
                            else
                                cacheEntry = _lastHashDataEntries.Find(d =>
                                    dateValue.CompareTo(d[0]) >= startDateComparisonResult &&
                                    dateValue.CompareTo(d[1]) <= endDateComparisonResult);

                            if (cacheEntry != null)
                                _lastDataEntry = cacheEntry;
                            else
                                errorColumnID = _mappings.DateColumnLineageID;
                        }
                    }
                    else
                    {
                        List<object[]> cacheEntries;
                        //Try to locate the Hash in Ditionary
                        if (_cache.TryGetValue(kd, out cacheEntries))
                        {
                            _lastHashDataEntries = cacheEntries;
                            _lastCacheHash = kd;

                            //No DateColumn, use first cache entry
                            if (_mappings.DateColumnIndex < 0)
                                cacheEntry = cacheEntries[0];
                            else //Find the cache entry
                            {
                                cacheEntry = cacheEntries.Find(d =>
                                   dateValue.CompareTo(d[0]) >= startDateComparisonResult
                                   &&
                                   dateValue.CompareTo(d[1]) <= endDateComparisonResult
                                    );
                                if (cacheEntry == null)
                                    errorColumnID = _mappings.DateColumnLineageID;
                            }
                            //if (cacheEntry != null)
                            _lastDataEntry = cacheEntry;
                        }
                        else
                            errorColumnID = _mappings.KeyMapppings[0].DataFieldLineageID;
                    }

                    //Cache Entry was found
                    if (cacheEntry != null)
                    {
                        //Get the output columns from cache
                        for (int i = 0; i < _mappings.OutputColumnMappings.Count; i++)
                        {
                            ColumnMapping cm = _mappings.OutputColumnMappings[i];
                            //if (cm.OutputColumnIndex != -1 && cm.OutputColumnCacheIndex != -1)
                            //{
                                buffer[cm.OutputColumnIndex] = cacheEntry[cm.OutputColumnCacheIndex];
                            //}
                        }
                        buffer.DirectRow(matchOutputID);
                    }
                    else //if (cacheEntry == null)    //No maatching data were found in the lookup cache
                    {
                        //determine what to do with not matched rows
                        switch (_mappings.NoMatchBehavior)
                        {
                            case NoMatchBehavior.RedirectToErrorOutput:
                                buffer.DirectErrorRow(errorOutputID, -1, errorColumnID); //TODO: Specify proper error Code and decide the LineageID of the Column to use
                                break;
                            case NoMatchBehavior.RedirectToNoMatchOutput:
                                buffer.DirectRow(noMatchOutputID);
                                break;
                            case NoMatchBehavior.IgnoreError:
                                SetNullOutputColumns(buffer, _mappings.OutputColumnMappings);
                                buffer.DirectRow(matchOutputID);
                                break;
                            case NoMatchBehavior.FailComponent:
                            default:
                                ComponentMetaData.FireError(0, ComponentMetaData.Name, string.Format("No data were matched for input row {0} and NoMatchBehavior is FailComponent", rowsProcessed), null, 0, out errorCancel);
                                if (errorCancel)
                                    return;
                                break;
                        }
                    }
                }
            }
        }

        private void SetNullOutputColumns(PipelineBuffer buffer, List<ColumnMapping> mappings)
        {
            for (int i = 0; i < mappings.Count; i++)
            {
                buffer.SetNull(mappings[i].OutputColumnIndex);
            }
        }


        public override string DescribeRedirectedErrorCode(int iErrorCode)
        {
            switch (iErrorCode)
            {
                case -1:
                    return "The Lookup key was not found in the lookup cache. First Key Column LineageID is returned.";
                case -2:
                    return "THe lookup Date value was not found in the history lookup cache";
                default:
                    return base.DescribeRedirectedErrorCode(iErrorCode);
            }            
        }

        /// <summary>
        /// Loads cache from the provided buffer
        /// </summary>
        /// <param name="buffer">PipelineBuffer of the Cache Input</param>
        private void LoadCache(PipelineBuffer buffer)
        {
            if (_cacheProcessingStarted == false)
            {
                _cacheStart = DateTime.Now;
                _cacheRowsProcessed = 0;
                _cacheProcessingStarted = true;
                bool fireAgain = true;
                ComponentMetaData.FireInformation(0, this.ComponentMetaData.Name, "Cache processing phase is beginning.", string.Empty, 0, ref fireAgain);
            }

            if (!buffer.EndOfRowset)
            {
                while (buffer.NextRow())
                {
                    _cacheRowsProcessed++;

                    //Initialize keys and data bufer arrays
                    IComparable[] keys = new IComparable[_mappings.KeyColumnsCount];
                    object[] data = new object[_mappings.DataColumnsCount + 2];

                    //Set the validity data fields in case validity fields are provided
                    if (dateColumns)
                    {
                        data[0] = buffer[_mappings.DateFromColumnIndex];
                        data[1] = buffer[_mappings.DateToColumnIndex];
                    }

                    //Store Key and output columsn to respective buffers
                    for (int i = 0; i < _mappings.ColumnsMappings.Count; i++)
                    {
                        var cm = _mappings.ColumnsMappings[i];

                        if (cm.KeyPosition != -1)
                            keys[cm.KeyPosition] = buffer[cm.LookupFieldIndex] as IComparable;

                        if (cm.OutputColumnLineageID != -1)
                            data[cm.OutputColumnCacheIndex] = buffer[cm.OutputColumnSourceIndex];
                    }

                    //calculate Key hash
                    KeyDataHash kd = new KeyDataHash(keys, _keyHashAlgorithm);

                    List<object[]> cacheEntries;
    
                    //Store the data into cache
                    if (_cache.TryGetValue(kd, out cacheEntries))// there already is a hash for, add new history record
                    {
                        if (dateColumns)    //Add additional values into cache only if there are data columns. Otherwise we are returning only first walue
                        {                   //and therefore we do not need to cache it
                            cacheEntries.Add(data);
                            _rowsIncacheCount++;
                        }
                        else
                        {
                            if (!duplicateCacheValueWarning)
                            {
                                duplicateCacheValueWarning = true;
                                ComponentMetaData.FireWarning(-1, ComponentMetaData.Name, "Duplicate cache entry was detected and no Date validity fields are set. Only first item in cache will be returned", null, 0);
                            }
                        }
                    }
                    else //Add new Item into the cache.
                    {
                        _rootCacheEntries++;
                        _rowsIncacheCount++;
                        cacheEntries = new List<object[]>();
                        cacheEntries.Add(data);
                        _cache.Add(kd, cacheEntries);
                    }
                }
            }

            if (buffer.EndOfRowset && !_cacheProcessed)
            {
                TimeSpan duration = DateTime.Now.Subtract(_cacheStart);
                bool fireAgain = true;
                ComponentMetaData.FireInformation(0, this.ComponentMetaData.Name, string.Format("Cache processing phase has finished. Total Rows Processed: {0}, Root Cache Entries: {1}, Total Rows In Cache: {2}, Duration: {3} seconds.", _cacheRowsProcessed, _rootCacheEntries, _rowsIncacheCount, duration.TotalSeconds.ToString()), string.Empty, 0, ref fireAgain);

                _cacheProcessed = true;
                return;
            }

        }

        public override void PostExecute()
        {
            bool fireAgain = true;
            this.ComponentMetaData.FireInformation(0, this.ComponentMetaData.Name, "Post-Execute phase is beginning. Processed " + rowsProcessed.ToString(CultureInfo.CurrentCulture) + " rows.", string.Empty, 0, ref fireAgain);
            _mappings.CleaupCache();
            base.PostExecute();
        }


        #endregion

        #region Private & internal Methods
        private void CreateCustomProperties(CustomProperties propsToCreate)
        {
            List<int> propsToRemove = new List<int>();
            foreach (IDTSCustomProperty prop in ComponentMetaData.CustomPropertyCollection)
            {
                string propName = prop.Name;
                if (
                    Enum.IsDefined(typeof(CustomProperties), propName)
                    &&
                    (propsToCreate & (CustomProperties)Enum.Parse(typeof(CustomProperties), propName)) == (CustomProperties)Enum.Parse(typeof(CustomProperties), propName)
                )
                {
                    propsToRemove.Add(prop.ID);
                }
            }

            foreach (int id in propsToRemove)
            {
                ComponentMetaData.CustomPropertyCollection.RemoveObjectByID(id);
            }



            if ((propsToCreate & CustomProperties.NoMatchBehavior) == CustomProperties.NoMatchBehavior)
            {
                IDTSCustomProperty noMatchBehavior = ComponentMetaData.CustomPropertyCollection.New();
                noMatchBehavior.Name = GetPropertyname(CustomProperties.NoMatchBehavior);
                noMatchBehavior.Description = "Specifies how the component will handle lookup errors";
                noMatchBehavior.ContainsID = false;
                noMatchBehavior.EncryptionRequired = false;
                noMatchBehavior.ExpressionType = DTSCustomPropertyExpressionType.CPET_NONE;
                noMatchBehavior.TypeConverter = typeof(NoMatchBehavior).AssemblyQualifiedName;
                noMatchBehavior.Value = NoMatchBehavior.FailComponent;
            }

            if ((propsToCreate & CustomProperties.CacheType) == CustomProperties.CacheType)
            {
                IDTSCustomProperty cacheType = ComponentMetaData.CustomPropertyCollection.New();
                cacheType.Name = GetPropertyname(CustomProperties.CacheType);
                cacheType.Description = "Specifies the cache type for the component";
                cacheType.ContainsID = false;
                cacheType.EncryptionRequired = false;
                cacheType.ExpressionType = DTSCustomPropertyExpressionType.CPET_NONE;
                cacheType.TypeConverter = typeof(CacheType).AssemblyQualifiedName;
                cacheType.Value = CacheType.Full;
            }

            if ((propsToCreate & CustomProperties.NoMatchCacheSize) == CustomProperties.NoMatchCacheSize)
            {
                IDTSCustomProperty noMatchCacheSize = ComponentMetaData.CustomPropertyCollection.New();
                noMatchCacheSize.Name = GetPropertyname(CustomProperties.NoMatchCacheSize);
                noMatchCacheSize.Description = "Specifies the size of the NoMatch cache in number of records. This specifies how much lookup failures will be stored in the cache. Default value of 0 means the NoMatch cache is disabled.";
                noMatchCacheSize.ContainsID = false;
                noMatchCacheSize.EncryptionRequired = false;
                noMatchCacheSize.ExpressionType = DTSCustomPropertyExpressionType.CPET_NONE;
                noMatchCacheSize.TypeConverter = typeof(int).AssemblyQualifiedName;
                noMatchCacheSize.Value = 0;
            }

            if ((propsToCreate & CustomProperties.DefaultCacheSize) == CustomProperties.DefaultCacheSize)
            {
                IDTSCustomProperty defaultCacheSize = ComponentMetaData.CustomPropertyCollection.New();
                defaultCacheSize.Name = GetPropertyname(CustomProperties.DefaultCacheSize);
                defaultCacheSize.Description = "Specifies thedefault size for Cache. This helps pre-allocate proper hash bbuckets for cache entries during cache loading. Ideal situaion when it is close to number of unique keys going into cache.";
                defaultCacheSize.ContainsID = false;
                defaultCacheSize.EncryptionRequired = false;
                defaultCacheSize.ExpressionType = DTSCustomPropertyExpressionType.CPET_NONE;
                defaultCacheSize.TypeConverter = typeof(int).AssemblyQualifiedName;
                defaultCacheSize.Value = 10000;
            }

            if ((propsToCreate & CustomProperties.KeyHashAlgorithm) == CustomProperties.KeyHashAlgorithm)
            {
                IDTSCustomProperty keyHash = ComponentMetaData.CustomPropertyCollection.New();
                keyHash.Name = GetPropertyname(CustomProperties.KeyHashAlgorithm);
                keyHash.Description = "Hash Algorithm to be used for hashing of the key fields in the cache";
                keyHash.ContainsID = false;
                keyHash.EncryptionRequired = false;
                keyHash.ExpressionType = DTSCustomPropertyExpressionType.CPET_NONE;
                keyHash.TypeConverter = typeof(HashAlgorithmType).AssemblyQualifiedName;
                keyHash.Value = HashAlgorithmType.None;

            }
        }

        internal static void SetDataInputColumnProperties(IDTSInputColumn col)
        {
            col.CustomPropertyCollection.RemoveAll();

            IDTSCustomProperty usageType = col.CustomPropertyCollection.New();
            usageType.Name = GetPropertyname(CustomProperties.InputColumnUsageType);
            usageType.Description = "Specifies input column uage Type";
            usageType.ContainsID = false;
            usageType.TypeConverter = typeof(InputColumnUsageType).AssemblyQualifiedName;
            usageType.ExpressionType = DTSCustomPropertyExpressionType.CPET_NONE;
            usageType.Value = InputColumnUsageType.None;

            IDTSCustomProperty lookupKey = col.CustomPropertyCollection.New();
            lookupKey.Name = GetPropertyname(CustomProperties.LookupKeyLineageID);
            lookupKey.Description = "LineageID of the corresponding Lookup Field in the Lookup Input";
            lookupKey.ContainsID = true;
            lookupKey.ExpressionType = DTSCustomPropertyExpressionType.CPET_NONE;
            lookupKey.Value = 0;
        }

        internal static void SetLookupInputColumnProperties(IDTSInputColumn col)
        {

            col.CustomPropertyCollection.RemoveAll();

            IDTSCustomProperty usageType = col.CustomPropertyCollection.New();
            usageType.Name = GetPropertyname(CustomProperties.InputColumnUsageType);
            usageType.Description = "Specifies input column uage Type";
            usageType.ContainsID = false;
            usageType.TypeConverter = typeof(InputColumnUsageType).AssemblyQualifiedName;
            usageType.ExpressionType = DTSCustomPropertyExpressionType.CPET_NONE;
            usageType.Value = InputColumnUsageType.None;

            IDTSCustomProperty lookupKey = col.CustomPropertyCollection.New();
            lookupKey.Name = GetPropertyname(CustomProperties.OutputColumnLineageID);
            lookupKey.Description = "LineageID of the corresponding OutputColumn in the LookupMatch Output";
            lookupKey.ContainsID = true;
            lookupKey.ExpressionType = DTSCustomPropertyExpressionType.CPET_NONE;
            lookupKey.Value = 0;

            IDTSCustomProperty dateComparison = col.CustomPropertyCollection.New();
            dateComparison.Name = GetPropertyname(CustomProperties.DateComparison);
            dateComparison.Description = "Specifies Date Comparison for columns with DateFrom and DateTo usage type";
            dateComparison.ContainsID = false;
            dateComparison.ExpressionType = DTSCustomPropertyExpressionType.CPET_NONE;
            dateComparison.TypeConverter = typeof(DateComparison).AssemblyQualifiedName;
            dateComparison.Value = DateComparison.None;
            
            
        }

        internal static void SetOutputColumnDataType(IDTSOutputColumn oCol, IDTSVirtualInputColumn vCol)
        {
            oCol.SetDataTypeProperties(vCol.DataType, vCol.Length, vCol.Precision, vCol.Scale, vCol.CodePage);
        }

        internal static void SetOutputColumnProperties(IDTSOutputColumn oCol)
        {
            oCol.CustomPropertyCollection.RemoveAll();

            IDTSCustomProperty srcLinID = oCol.CustomPropertyCollection.New();
            srcLinID.Name = HistoryLookupTransformation.GetPropertyname(HistoryLookupTransformation.CustomProperties.SourceLineageID);
            srcLinID.Description = "Contains Lineage ofthe source column from HistoryLookupInput";
            srcLinID.ContainsID = true;
            srcLinID.ExpressionType = DTSCustomPropertyExpressionType.CPET_NONE;
            srcLinID.Value = 0;
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

        private void FireComponentMetadataWarning(int warningCode, string message)
        {
            ComponentMetaData.FireWarning(warningCode, ComponentMetaData.Name, message, null, 0);
        }

        internal static string GetPropertyname(CustomProperties customProperty)
        {
            switch (customProperty)
            {
                case CustomProperties.KeyHashAlgorithm:
                case CustomProperties.NoMatchBehavior:
                case CustomProperties.CacheType:
                case CustomProperties.NoMatchCacheSize:
                case CustomProperties.DefaultCacheSize:
                case CustomProperties.None:
                case CustomProperties.All:
                default:
                    return customProperty.ToString();
            }
        }

        #endregion


    }
}
