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
using System.Runtime.Serialization.Formatters.Binary;
using System.IO;
using System.Security.Cryptography;
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
        DisplayName = "Lookup Error Aggregation"
        , Description = "Aggregates Lookup Errors for logging purposes"
        , ComponentType = ComponentType.Transform
        , IconResource = "PP.SSIS.DataFlow.Resources.LookupErrorAggregation.ico"
        , CurrentVersion = 2
        , SupportsBackPressure = true

#if SQL2019
        , UITypeName = "PP.SSIS.DataFlow.UI.LookupErrorAggregationTransformationUI, PP.SSIS.DataFlow.SQL2019, Version=1.0.0.0, Culture=neutral, PublicKeyToken=b68691a82d4fb69c"
#endif
#if SQL2017
        , UITypeName = "PP.SSIS.DataFlow.UI.LookupErrorAggregationTransformationUI, PP.SSIS.DataFlow.SQL2017, Version=1.0.0.0, Culture=neutral, PublicKeyToken=b68691a82d4fb69c"
#endif
#if SQL2016
        , UITypeName = "PP.SSIS.DataFlow.UI.LookupErrorAggregationTransformationUI, PP.SSIS.DataFlow.SQL2016, Version=1.0.0.0, Culture=neutral, PublicKeyToken=b99aee68060fa342"
#endif
#if SQL2014
        , UITypeName = "PP.SSIS.DataFlow.UI.LookupErrorAggregationTransformationUI, PP.SSIS.DataFlow, Version=1.0.0.0, Culture=neutral, PublicKeyToken=6926746b040a83a5"
#endif
#if SQL2012
        , UITypeName = "PP.SSIS.DataFlow.UI.LookupErrorAggregationTransformationUI, PP.SSIS.DataFlow, Version=1.0.0.0, Culture=neutral, PublicKeyToken=c8e167588e3f3397"
#endif
#if SQL2008
        , UITypeName = "PP.SSIS.DataFlow.UI.LookupErrorAggregationTransformationUI, PP.SSIS.DataFlow, Version=1.0.0.0, Culture=neutral, PublicKeyToken=e17db8c64cd5b65b"
#endif
)
    ]
    public class LookupErrorAggregationTransformation : PipelineComponent
    {
        #region Definitions

        /// <summary>
        /// Defines which property details should be set
        /// </summary>
        [Flags]
        public enum SetPropertyType
        {
            /// <summary>
            /// None of the properties should be set
            /// </summary>
            None                    =   0x00,
            /// <summary>
            /// XmlSaveOptions prperty should be set
            /// </summary>
            XmlSaveOptions          =   0x01,
            /// <summary>
            /// XmlSerializeLIneage property should be set
            /// </summary>
            XmlSerializeLineage     =   0x02,
            /// <summary>
            /// XmlSerializeDataType property should be set
            /// </summary>
            XmlSerializeDataType    =   0x04,
            /// <summary>
            /// All properties should be set
            /// </summary>
            All                     = XmlSaveOptions | XmlSerializeLineage | XmlSerializeDataType
        }

        [Flags]
        public enum CustomProperties
        {
            None                    = 0x000,
            HashAlgorithm           = 0x001,
            NullColumn              = 0x008,

            All = HashAlgorithm| NullColumn
        }

        private class LookupErrorData
        {
            public LookupErrorData(byte[] columnsInfoBuffer)
            {
                ColumnsInfoBuffer = columnsInfoBuffer;
                Count = 0;
            }
            public byte[] ColumnsInfoBuffer { get; private set; }

            public int Count { get; set; }
        }

        private struct ErrorColumn
        {
            public ErrorColumn(string name, int index)
            {
                Name = name;
                Index = index;
            }
            public int Index;
            public string Name;
        }

        private class OutputColumnsInfo
        {
            public int LookupDetailsXmlIndex = -1;
            public int CountIndex = -1;
            public int KeysCountIndex = -1;
            public int TotalRowsCountIndex = -1;
            public int LookupSourceIDIndex = -1;
            public int LookupSourceDescriptionIndex = -1;
        }

        private bool inputColumnsValid = true;

        List<InputBufferColumnInfo> inputBufferColumns;
        List<KeyValuePair<ErrorColumn, int>> upstreamColumnsMappings;
        Dictionary<KeyDataHash, LookupErrorData> _lookupErrorsCache;

        LookupErrorData _lastLookupError = null;
        KeyDataHash _lastLookupKey = null;
        
        string _lookupSourceDescription = null;
        string _lookupSourceID = null;
        int _nullColumnIndexID = -1;

        PipelineBuffer _errorBuffer = null;
        int _errorRowsCount = 0;
        int _upstreadDataRowsProcessed = 0;
        int _upstreamDataErrorRowsProcessed = 0;
        int _upstreamErrorRowsProcessed = 0;
        int rowsProcessed = 0;

        bool _dataEOR = false; //Error Data End Of Rowset;
        bool _upstreamEOR = false; //Upstream error data End of Rowset;
        bool _erroOutputAttached = false; //specifies whether ErrorOutput is attacched. Used to determine whether it has sense to  process errors.

        bool _serializeLineage;
        bool _serializeDataType;
        SaveOptions _saveOptions = SaveOptions.None;

        OutputColumnsInfo _outputColumns = new OutputColumnsInfo();

        HashAlgorithmType _hashAlgorithm = HashAlgorithmType.None;
        #endregion

        #region DesignTime
        /// <summary>
        /// Provides SSIS Component Properties
        /// </summary>
        public override void ProvideComponentProperties()
        {
            this.ComponentMetaData.Name = Resources.LookupErrorAggName;
            this.ComponentMetaData.Description = Resources.LookupErrorAggDescription;
            this.ComponentMetaData.ContactInfo = Resources.TransformationContactInfo;

            // Reset the component.
            base.RemoveAllInputsOutputsAndCustomProperties();

            //call base method to insert input and output
            base.ProvideComponentProperties();

            //Set Input properties
            IDTSInput input = ComponentMetaData.InputCollection[0];
            input.Name = Resources.LookupErrorAggErrorInputName;
            input.Description = Resources.LookupErrorAggErrorInputDescription;

            // Set Output properties
            IDTSOutput output = ComponentMetaData.OutputCollection[0];
            output.Name = Resources.LookupErrorAggErrorOutputName;
            output.Description = Resources.LookupErrorAggErrorOutputDescription;
            output.SynchronousInputID = input.ID;

            IDTSInput errorInput = ComponentMetaData.InputCollection.New();
            errorInput.Name = Resources.LookupErrorAggUpstreamErrorInputNamme;
            errorInput.Description = Resources.LookupErrorAggUpstreamErrorInputDescription;
            errorInput.Dangling = true;

            IDTSOutput errorOutput = ComponentMetaData.OutputCollection.New();
            errorOutput.Name = Resources.LookupErrorAggAggregatedErrorOutputName;
            errorOutput.Description = Resources.LookupErrorAggAggregatedErrorOutputDescription;

            //Add Custom Properties for the component
            AddComponentCustomProperties(CustomProperties.All);

            AddErrorOutputDefaultColumns(errorOutput);
        }

        /// <summary>
        /// Validate HashColumnsTransform metadata
        /// </summary>
        /// <returns></returns>
        public override DTSValidationStatus Validate()
        {
            if (ComponentMetaData.InputCollection.Count != 2)
            {
                FireComponentMetadataError(0, Resources.ErrorIncorrectNumberOfInputs);
                return DTSValidationStatus.VS_NEEDSNEWMETADATA;
            }

            if (ComponentMetaData.OutputCollection.Count != 2)
            {
                FireComponentMetadataError(0, Resources.ErrorIncorrectNumberOfOutputs);
                return DTSValidationStatus.VS_NEEDSNEWMETADATA;
            }

            IDTSInput input = ComponentMetaData.InputCollection[0];
            if (!input.IsAttached)
            {
                FireComponentMetadataError(0, PP.SSIS.DataFlow.Properties.Resources.ErrorLookupErrorAggInputNotAttached);
                return DTSValidationStatus.VS_NEEDSNEWMETADATA;
            }

            IDTSVirtualInput vInput = input.GetVirtualInput();

            foreach (IDTSInputColumn column in input.InputColumnCollection)
            {
                try
                {
                    IDTSVirtualInputColumn100 vColumn = vInput.VirtualInputColumnCollection.GetVirtualInputColumnByLineageID(column.LineageID);
                }
                catch
                {
                    FireComponentMetadataError(0, string.Format(Resources.ErrorInputColumnNotInUpstreamComponent, column.IdentificationString));
                    inputColumnsValid = false;
                    return DTSValidationStatus.VS_NEEDSNEWMETADATA;
                }
            }


            IDTSCustomProperty nullColumn = ComponentMetaData.CustomPropertyCollection[Resources.LookupErrorAggNullColumnName];
            int nullColumnLineageID = (int)nullColumn.Value;
            if (nullColumnLineageID != -1)
            {
                IDTSVirtualInputColumn nullVColumn = null;
                foreach (IDTSVirtualInputColumn vCol in vInput.VirtualInputColumnCollection)
                {
                    if (vCol.LineageID == nullColumnLineageID)
                    {
                        nullVColumn = vCol;
                        break;
                    }
                }
                if (nullVColumn != null)
                {
                    IDTSInputColumn inCol;
                    if (nullVColumn.UsageType == DTSUsageType.UT_IGNORED)
                        inCol = SetUsageType(input.ID, vInput, nullVColumn.LineageID, DTSUsageType.UT_READONLY);
                    else
                        inCol = input.InputColumnCollection.GetInputColumnByLineageID(nullVColumn.LineageID);

                    IDTSCustomProperty isNullColumn = inCol.CustomPropertyCollection[Resources.LookupErrorAggIsNullColumnName];
                    isNullColumn.Value = true;
                }
                else
                {
                    nullColumn.Value = -1;
                }
            }

            IDTSOutput output = ComponentMetaData.OutputCollection[1];
            bool lookupSourceIDExists = false;
            bool xmlColumnExists = false;
            bool countColumnExits = false;

            string lookupSourceID = string.Empty;

            foreach (IDTSOutputColumn outputColumn in output.OutputColumnCollection)
            {
                if (outputColumn.Name == Resources.LookupErrorAggLookupSourceIDName)
                {
                    lookupSourceIDExists = true;
                    lookupSourceID = outputColumn.CustomPropertyCollection[Resources.LookupErrorAggLookupSourceIDName].Value.ToString();
                }
                else if (outputColumn.Name == Resources.LookupErrorAggLookpDetaislXmlName)
                    xmlColumnExists = true;
                else if (outputColumn.Name == Resources.LookupErrorAggCountName)
                    countColumnExits = true;
            }


            if (string.IsNullOrEmpty(lookupSourceID))
            {
                FireComponentMetadataError(0, Resources.ErrorLookupErrorAggSourceIDNotProvided);
                return DTSValidationStatus.VS_NEEDSNEWMETADATA;
            }



            if (!lookupSourceIDExists)
            {
                IDTSOutputColumn col = output.OutputColumnCollection.New();
                col.Name = Resources.LookupErrorAggLookupSourceIDName;
                col.Description = Resources.LookupErrorAggLookupSourceIDDescription;

                col.SetDataTypeProperties(DataType.DT_WSTR, 50, 0, 0, 0);
            }
            //If XmlColumn does not exists, create a default one
            if (!xmlColumnExists)
            {
                IDTSOutputColumn xmlCol = output.OutputColumnCollection.New();
                xmlCol.Name = Resources.LookupErrorAggLookpDetaislXmlName;
                xmlCol.Description = Resources.LookupErrorAggLookpDetaislXmlDescription;

                SetXmlColumnProperties(xmlCol);
            }
            if (!countColumnExits)
            {
                IDTSOutputColumn col = output.OutputColumnCollection.New();
                col.Name = Resources.LookupErrorAggCountName;
                col.Description = Resources.LookupErrorAggCountDescription;
                col.SetDataTypeProperties(DataType.DT_I4, 0, 0, 0, 0);
            }

            IDTSInput upstreamInput = ComponentMetaData.InputCollection[1];
            IDTSOutput errorOutput = ComponentMetaData.OutputCollection[1];

            if (upstreamInput.IsAttached)
            {
                vInput = upstreamInput.GetVirtualInput();
                int requiredColumnsCount = 0;
                bool wrongTypes = false;

                foreach (IDTSVirtualInputColumn col in vInput.VirtualInputColumnCollection)
                {
                    string outColName = null;

                    switch (col.Name)
                    {
                        case "LookupSourceID":
                        case "LookupDetailsXml":
                        case "Count":
                            requiredColumnsCount++;
                            outColName = col.Name;
                            break;
                        case "KeysCount":
                        case "TotalRowsCount":
                        case "LookupSourceDescription":
                            outColName = col.Name;
                            break;
                    }

                    if (outColName != null)
                    {
                        foreach (IDTSOutputColumn oCol in errorOutput.OutputColumnCollection)
                        {
                            if (oCol.Name == outColName)
                            {
                                if (oCol.DataType != col.DataType || oCol.Length != col.Length)
                                    wrongTypes = true;
                                break;
                            }
                        }
                    }
                }
                
                if (requiredColumnsCount != 3 || wrongTypes)
                {
                    FireComponentMetadataError(0, Resources.ErrorLookupErrorAggUpstreamNotAggregatedLookupError);
                    return DTSValidationStatus.VS_NEEDSNEWMETADATA;
                }


            }


            return DTSValidationStatus.VS_ISVALID;
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
                CustomProperties propsToSet = CustomProperties.All;
                foreach (IDTSCustomProperty prop in ComponentMetaData.CustomPropertyCollection)
                {
                    string propName = prop.Name;
                    if (Enum.IsDefined(typeof(CustomProperties), propName))
                        propsToSet ^= (CustomProperties)Enum.Parse(typeof(CustomProperties), propName);

                }

                AddComponentCustomProperties(propsToSet);

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
            if (inputID == ComponentMetaData.InputCollection[0].ID)
            {
                int maxSortOrder = GetMaxInputColumnsSortOrder();

                DTSUsageType uType = usageType == DTSUsageType.UT_READWRITE ? DTSUsageType.UT_READONLY : usageType;

                IDTSInputColumn col = base.SetUsageType(inputID, virtualInput, lineageID, uType);

                if (uType != DTSUsageType.UT_IGNORED)
                {
                    IDTSCustomProperty prop = null;
                    IDTSCustomProperty nullColumn = null;
                    IDTSCustomProperty keyColumn = null;

                    foreach (IDTSCustomProperty p in col.CustomPropertyCollection)
                    {
                        if (p.Name == Resources.InputSortOrderPropertyName)
                            prop = p;
                        else if (p.Name == Resources.LookupErrorAggIsNullColumnName)
                            nullColumn = p;
                        else if (p.Name == Resources.LookupErrorAggIsKeyColumnName)
                            keyColumn = p;

                        if (prop != null && nullColumn != null && keyColumn != null)
                            break;
                    }

                    if (prop == null)
                        prop = col.CustomPropertyCollection.New();

                    prop.Name = Resources.InputSortOrderPropertyName;
                    prop.Description = Resources.InputSortOrderPropertyDescription;
                    prop.ContainsID = false;
                    prop.EncryptionRequired = false;
                    prop.ExpressionType = DTSCustomPropertyExpressionType.CPET_NONE;
                    prop.Value = maxSortOrder + 1;


                    if (nullColumn == null)
                    {
                        nullColumn = col.CustomPropertyCollection.New();
                        nullColumn.Name = Resources.LookupErrorAggIsNullColumnName;
                        nullColumn.Description = Resources.LookupErrorAggIsNullColumnDescription;
                        nullColumn.ContainsID = false;
                        nullColumn.EncryptionRequired = false;
                        nullColumn.ExpressionType = DTSCustomPropertyExpressionType.CPET_NONE;
                        nullColumn.Value = false;
                    }
                    if (keyColumn == null)
                    {
                        keyColumn = col.CustomPropertyCollection.New();
                        keyColumn.Name = Resources.LookupErrorAggIsKeyColumnName;
                        keyColumn.Description = Resources.LookupErrorAggIsKeyColumnDescription;
                        keyColumn.ContainsID = false;
                        keyColumn.EncryptionRequired = false;
                        keyColumn.ExpressionType = DTSCustomPropertyExpressionType.CPET_NONE;
                        keyColumn.Value = false;
                    }
                }

                return col;
            }
            else
                return base.SetUsageType(inputID, virtualInput, lineageID, usageType);
        }

        /// <summary>
        /// Get Maximum Input Sort Order
        /// </summary>
        /// <returns>Maximu Sort Order of Input Columns</returns>
        private int GetMaxInputColumnsSortOrder()
        {
            int maxOrder = int.MinValue;

            IDTSInput input = ComponentMetaData.InputCollection[0];
            foreach (IDTSInputColumn col in input.InputColumnCollection)
            {
                foreach (IDTSCustomProperty prop in col.CustomPropertyCollection)
                {
                    if (prop.Name == Resources.InputSortOrderPropertyName)
                    {
                        int order = prop.Value != null ? (int)prop.Value : 0;
                        if (order > maxOrder)
                            maxOrder = order;
                        break;
                    }
                }
            }
            if (maxOrder == int.MinValue)
                maxOrder = 0;

            return maxOrder;
        }


        /// <summary>
        /// Reinitialize metadata in case input cvolumns are invalid
        /// </summary>
        public override void ReinitializeMetaData()
        {
            if (!inputColumnsValid)
            {
                IDTSInput input = ComponentMetaData.InputCollection[0];
                IDTSVirtualInput vInput = input.GetVirtualInput();
                List<int> idsToRemove = new List<int>();

                foreach (IDTSInputColumn col in input.InputColumnCollection)
                {
                    IDTSVirtualInputColumn100 vCol = vInput.VirtualInputColumnCollection.GetVirtualInputColumnByLineageID(col.LineageID);
                    if (vCol == null)
                        idsToRemove.Add(col.ID);
                }

                foreach (int id in idsToRemove)
                {
                    input.InputColumnCollection.RemoveObjectByID(id);
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
        /// <summary>
        /// Disallow inserting new output
        /// </summary>
        /// <param name="insertPlacement">Placement of new output</param>
        /// <param name="outputID">ID of new output</param>
        /// <returns>Throws error as this component does not allows inserting of new outputs</returns>
        public override IDTSOutput InsertOutput(DTSInsertPlacement insertPlacement, int outputID)
        {
            throw new InvalidOperationException(Resources.ErrorAddOutput);
            //IDTSOutput o = base.InsertOutput(insertPlacement, outputID);
            //o.Description = Resources.HashColumnOutputDescription;
            //return o;
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
            throw new InvalidOperationException(Resources.ErrorAddColumn);
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
            //in case of HasshType property update, update output column data type
            if (propertyName == Resources.XmlSaveOptionPropertyName)
            {
                IDTSOutput output = ComponentMetaData.OutputCollection[0];
                IDTSOutputColumn outputColumn = output.OutputColumnCollection.FindObjectByID(outputColumnID);

                outputColumn.SetDataTypeProperties(DataType.DT_NTEXT, 0, 0, 0, 0);

                return base.SetOutputColumnProperty(outputID, outputColumnID, propertyName, propertyValue);
            }
            else if (propertyName == Resources.XmlSerializeLineageName || propertyName == Resources.XmlSerializeDataTypeName)
            {
                return base.SetOutputColumnProperty(outputID, outputColumnID, propertyName, propertyValue);
            }
            else if (propertyName == Resources.LookupErrorAggLookupSourceIDName)
            {
                //TODO: Add possibility to set the length of the output column
                string val = propertyValue.ToString();
                if (val.Length > 50)
                    throw new Exception("Maximum length is 50");
                return base.SetOutputColumnProperty(outputID, outputColumnID, propertyName, propertyValue);
            }
            else if (propertyName == Resources.LookupErrorAggLookupSourceDescriptionName)
            {
                //TODO: Add possibility to set the length of the output column
                string val = propertyValue.ToString();
                if (val.Length > 50)
                    throw new Exception("Maximum length is 50");
                return base.SetOutputColumnProperty(outputID, outputColumnID, propertyName, propertyValue);
            }
            throw new Exception(string.Format(Resources.ErrorOutputColumnPropertyCannotBeChanged, propertyName));
        }

        public override IDTSCustomProperty SetInputColumnProperty(int inputID, int inputColumnID, string propertyName, object propertyValue)
        {
            if (propertyName == Resources.InputSortOrderPropertyName)
            {
                int val = Convert.ToInt32(propertyValue);
                UpdateSortOrder(inputID, inputColumnID, val);
                var prop = base.SetInputColumnProperty(inputID, inputColumnID, propertyName, propertyValue);


                return prop;
            }

            throw new Exception(string.Format(Resources.ErrorInputColumnPropertyCannotBeChanged, propertyName));
        }

        public override void OnInputPathAttached(int inputID)
        {
            int idx = ComponentMetaData.InputCollection.GetObjectIndexByID(inputID);

            if (idx == 1)
            {
                IDTSInput errInput = ComponentMetaData.InputCollection[idx];
                IDTSVirtualInput vInput = errInput.GetVirtualInput();
                foreach (IDTSVirtualInputColumn vCol in vInput.VirtualInputColumnCollection)
                {
                    SetUsageType(inputID, vInput, vCol.LineageID, DTSUsageType.UT_READONLY);
                }
            }
            base.OnInputPathAttached(inputID);
        }

        public override void OnInputPathDetached(int inputID)
        {
            int idx = ComponentMetaData.InputCollection.GetObjectIndexByID(inputID);

            if (idx == 1)
            {
                IDTSInput errInput = ComponentMetaData.InputCollection[idx];
                IDTSVirtualInput vInput = errInput.GetVirtualInput();
                foreach (IDTSVirtualInputColumn vCol in vInput.VirtualInputColumnCollection)
                {                    
                    SetUsageType(inputID, vInput, vCol.LineageID, DTSUsageType.UT_IGNORED);
                }
            }
            base.OnInputPathDetached(inputID);
        }

        private void UpdateSortOrder(int inputID, int inputColumnID, int val)
        {
            IDTSInput input = ComponentMetaData.InputCollection.GetObjectByID(inputID);

            IDTSInputColumn currentColumn = input.InputColumnCollection.GetObjectByID(inputColumnID);
            IDTSCustomProperty currentProp = currentColumn.CustomPropertyCollection[Resources.InputSortOrderPropertyName];
            int currentValue = (int)currentProp.Value;

            foreach (IDTSInputColumn col in input.InputColumnCollection)
            {
                if (col.ID == inputColumnID)
                    continue;

                IDTSCustomProperty prop = col.CustomPropertyCollection[Resources.InputSortOrderPropertyName];
                if (prop != null && (int)prop.Value == val)
                {
                    prop.Value = currentValue;
                    //UpdateSortOrder(inputID, col.ID, val + 1);
                }
            }
        }

        #endregion

        #region Runtime Processing
        /// <summary>
        /// Provides feedbacks about inputs available for processing
        /// </summary>
        /// <param name="inputIDs">List of Input Ids available</param>
        /// <param name="canProcess">specifies which inputs are aredy for processing</param>
        public override void IsInputReady(int[] inputIDs, ref bool[] canProcess)
        {
            //all inputs can process data at any time
            for (int i = 0; i < inputIDs.Length; i++)
            {
                canProcess[i] = true;
            }

        }

        /// <summary>
        /// PreExecute Phase for initialization of internal runtime structures
        /// </summary>
        public override void PreExecute()
        {
            bool fireAgain = true;
            rowsProcessed = 0;
            ComponentMetaData.FireInformation(0, this.ComponentMetaData.Name, "Pre-Execute phase is beginning.", string.Empty, 0, ref fireAgain);

            IDTSInput input = ComponentMetaData.InputCollection[0];
            inputBufferColumns = new List<InputBufferColumnInfo>(input.InputColumnCollection.Count);

            for (int i = 0; i < input.InputColumnCollection.Count; i++)
            {
                IDTSInputColumn column = input.InputColumnCollection[i];
                IDTSCustomProperty prop = column.CustomPropertyCollection[Resources.InputSortOrderPropertyName];
                int order = (int)prop.Value;
                IDTSCustomProperty keyCol = column.CustomPropertyCollection[Resources.LookupErrorAggIsKeyColumnName];
                bool isKeyCol = (bool)keyCol.Value;
                if (isKeyCol)
                {
                    inputBufferColumns.Add(new InputBufferColumnInfo(BufferManager.FindColumnByLineageID(input.Buffer, column.LineageID),
                        column.Name, column.ID, column.LineageID, order, column.DataType, column.Length, column.Precision, column.Scale));
                }
            }

            //Sort the columns from the input buffer according the Sort Order
            inputBufferColumns.Sort((a, b) => a.SortOrder.CompareTo(b.SortOrder));

            _hashAlgorithm = (HashAlgorithmType)ComponentMetaData.CustomPropertyCollection[GetPropertyName(CustomProperties.HashAlgorithm)].Value;

            upstreamColumnsMappings = new List<KeyValuePair<ErrorColumn,int>>(ComponentMetaData.OutputCollection[1].OutputColumnCollection.Count);

            IDTSInput upstreamInput = ComponentMetaData.InputCollection[1];
            IDTSOutput errorOutput = ComponentMetaData.OutputCollection[1];
            _erroOutputAttached = errorOutput.IsAttached;


            //Get Indexes of the Output Columns
            for (int i = 0; i < errorOutput.OutputColumnCollection.Count; i++)
            {
                IDTSOutputColumn col = errorOutput.OutputColumnCollection[i];

                int outColIDX = BufferManager.FindColumnByLineageID(errorOutput.Buffer, col.LineageID);

                switch (col.Name)
                {
                    case "LookupDetailsXml":
                        _outputColumns.LookupDetailsXmlIndex = outColIDX;
                        //get the XmlProperties
                        foreach (IDTSCustomProperty prop in col.CustomPropertyCollection)
                        {
                            if (prop.Name == Resources.XmlSaveOptionPropertyName)
                            {
                                _saveOptions = (SaveOptions)prop.Value;

                            }
                            else if (prop.Name == Resources.XmlSerializeDataTypeName)
                            {
                                _serializeDataType = (bool)prop.Value;
                            }
                            else if (prop.Name == Resources.XmlSerializeLineageName)
                            {
                                _serializeLineage = (bool)prop.Value;
                            }
                        }

                        break;
                    case "Count":
                        _outputColumns.CountIndex = outColIDX;
                        break;
                    case "KeysCount":
                        _outputColumns.KeysCountIndex = outColIDX;
                        break;
                    case "TotalRowsCount":
                        _outputColumns.TotalRowsCountIndex = outColIDX;
                        break;
                    case "LookupSourceID":
                        _lookupSourceID = col.CustomPropertyCollection[Resources.LookupErrorAggLookupSourceIDName].Value.ToString();
                        _outputColumns.LookupSourceIDIndex = outColIDX;
                        break;
                    case "LookupSourceDescription":
                        _lookupSourceDescription = col.CustomPropertyCollection[Resources.LookupErrorAggLookupSourceDescriptionName].Value.ToString();
                        _outputColumns.LookupSourceDescriptionIndex = outColIDX;
                        break;
                }
            }

            //Map Upstream ErrorInput columns to Error Output Columns
            if (upstreamInput.IsAttached && _erroOutputAttached)
            {
                IDTSVirtualInput vInput = upstreamInput.GetVirtualInput();
                foreach (IDTSVirtualInputColumn col in vInput.VirtualInputColumnCollection)
                {                    
                    string outColName = null;
                    switch (col.Name)
                    {
                        case "LookpDetaislXml":
                        case "Count":
                        case "KeysCount":
                        case "TotalRowsCount":
                        case "LookupSourceID":
                        case "LookupSourceDescription":
                            outColName = col.Name;
                            break;
                    }

                    if (outColName != null)
                    {
                        foreach (IDTSOutputColumn oCol in errorOutput.OutputColumnCollection)
                        {
                            if (oCol.Name == outColName)
                            {
                                int outIdx = BufferManager.FindColumnByLineageID(errorOutput.Buffer, oCol.LineageID);
                                int inIdx = BufferManager.FindColumnByLineageID(upstreamInput.Buffer, col.LineageID);

                                upstreamColumnsMappings.Add(new KeyValuePair<ErrorColumn, int>(new ErrorColumn(outColName, outIdx), inIdx));
                                break;
                            }
                        }
                    }
                }
            }
            else
            {
                _upstreamEOR = true;
            }


            //Get NullColumn LineageID & Index
            int nullColumnLineageID = (int)ComponentMetaData.CustomPropertyCollection[GetPropertyName(CustomProperties.NullColumn)].Value;
            if (nullColumnLineageID != -1)
                _nullColumnIndexID = BufferManager.FindColumnByLineageID(input.ID, nullColumnLineageID);
            else
                _nullColumnIndexID = -1;

            rowsProcessed = 0;
            _errorRowsCount = 0;
            _upstreadDataRowsProcessed = 0;
            _upstreamDataErrorRowsProcessed = 0;
            _upstreamErrorRowsProcessed = 0;

            //Initilize LookupErrorsCache
            _lookupErrorsCache = new Dictionary<KeyDataHash, LookupErrorData>(10000);

        }

        /// <summary>
        /// Processes the Outputs - used to get the buffer of the Error Input
        /// </summary>
        /// <param name="outputs">Numer of Outputs</param>
        /// <param name="outputIDs">IDs of Outputs</param>
        /// <param name="buffers">Output Buffers</param>
        public override void PrimeOutput(int outputs, int[] outputIDs, PipelineBuffer[] buffers)
        {
            if (outputs > 0)
            {
                for (int i = 0; i < outputs; i++)
                {
                    //If output is Error Output, assign the bufffer to errorBuffer
                    if (outputIDs[i] == ComponentMetaData.OutputCollection[1].ID && _erroOutputAttached)
                    {
                        _errorBuffer = buffers[i];
                        break;
                    }
                }

            }
            base.PrimeOutput(outputs, outputIDs, buffers);
        }
        /// <summary>
        /// Processes rows in the inpuyt buffer
        /// </summary>
        /// <param name="inputID">ID of the input to process the input rows</param>
        /// <param name="buffer">Pipeline bufere with rows to process</param>
        public override void ProcessInput(int inputID, PipelineBuffer buffer)
        {
            int inputIndex = ComponentMetaData.InputCollection.GetObjectIndexByID(inputID);
            //If the Iinput  is UpstreamError Input and therea are availale data and we have Error Output attached process it to the error output
            if (inputIndex == 1)
            {
                ProcessUpstreamErrors(inputID, buffer);
                return;
            }

            //if there is no error output attached or we do not have any input bufer columns, we do not need to process the cache and errors
            if (!_erroOutputAttached || inputBufferColumns.Count == 0)
            {
                base.ProcessInput(inputID, buffer);
            }
            else if (!buffer.EndOfRowset)
            {
                while (buffer.NextRow())
                {
                    rowsProcessed++;
                    _upstreadDataRowsProcessed = 0;

                    //If using NullColumn (Index != -1) and the column is NOT NULL, then continue as it is not error/no match column
                    if (_nullColumnIndexID != -1 && !buffer.IsNull(_nullColumnIndexID))
                        continue;

                    _errorRowsCount++;
                    _upstreamDataErrorRowsProcessed++;

                    //Build key key
                    IComparable[] keyData = new IComparable[inputBufferColumns.Count];
                    for (int i = 0; i < inputBufferColumns.Count; i++)
                    {
                        InputBufferColumnInfo bci = inputBufferColumns[i];
                        keyData[i] = buffer[bci.Index] as IComparable;
                    }
                    KeyDataHash kd = new KeyDataHash(keyData, _hashAlgorithm);


                    LookupErrorData lookupError = null;
                    if (_lastLookupKey == kd && _lastLookupError != null)
                    {
                        lookupError = _lastLookupError;
                    }
                    else
                    {
                        if (!_lookupErrorsCache.TryGetValue(kd, out lookupError))
                        {
                            byte[] xmlBuffer = GetXmlData(buffer);
                            lookupError = new LookupErrorData(xmlBuffer);
                            _lookupErrorsCache.Add(kd, lookupError);
                        }
                    }

                    lookupError.Count++;
                    _lastLookupKey = kd;
                    _lastLookupError = lookupError;
                }
            }
            else if (buffer.EndOfRowset && _erroOutputAttached && _errorBuffer != null && _dataEOR == false)
            {
                if (_lookupErrorsCache != null && _lookupErrorsCache.Count != 0)
                {
                    int keysCount = _lookupErrorsCache.Count;
                    foreach (LookupErrorData led in _lookupErrorsCache.Values)
                    {
                        _errorBuffer.AddRow();
                        if (_outputColumns.LookupSourceIDIndex != -1)
                            _errorBuffer.SetString(_outputColumns.LookupSourceIDIndex, _lookupSourceID);
                        if (_outputColumns.LookupSourceDescriptionIndex != -1)
                            _errorBuffer.SetString(_outputColumns.LookupSourceDescriptionIndex, _lookupSourceDescription);
                        if (_outputColumns.LookupDetailsXmlIndex != -1)
                            _errorBuffer.AddBlobData(_outputColumns.LookupDetailsXmlIndex, led.ColumnsInfoBuffer);
                        if (_outputColumns.CountIndex != -1)
                            _errorBuffer.SetInt32(_outputColumns.CountIndex, led.Count);
                        if (_outputColumns.KeysCountIndex != -1)
                            _errorBuffer.SetInt32(_outputColumns.KeysCountIndex, keysCount);
                        if (_outputColumns.TotalRowsCountIndex != -1)
                            _errorBuffer.SetInt32(_outputColumns.TotalRowsCountIndex, _errorRowsCount);
                    }
                }

                if (_upstreamEOR)
                    _errorBuffer.SetEndOfRowset();

                _dataEOR = true;
            }
        }

        /// <summary>
        /// Gets Xml Represantation of the Key Columns
        /// </summary>
        /// <param name="buffer">Buffer to get the data</param>
        /// <returns>byte stream with the XML columns representation</returns>
        private byte[] GetXmlData(PipelineBuffer buffer)
        {
            XElement rowElement = new XElement("lookupError");

            //Add sourceID and sourceNme attributes to the row node if those are specified in the component properties
            if (!string.IsNullOrEmpty(_lookupSourceID))
                rowElement.Add(new XAttribute(PP.SSIS.DataFlow.Properties.Resources.LookupErrorAggLookupSourceIDName, _lookupSourceID));
            if (!string.IsNullOrEmpty(_lookupSourceDescription))
                rowElement.Add(new XAttribute(PP.SSIS.DataFlow.Properties.Resources.LookupErrorAggLookupSourceDescriptionName, _lookupSourceDescription));

            XElement columnElement = null;
            //Process XML for selected input columns
            for (int i = 0; i < inputBufferColumns.Count; i++)
            {
                InputBufferColumnInfo bci = inputBufferColumns[i];
                BufferColumn col = buffer.GetColumnInfo(bci.Index);

                byte[] bdata;

                columnElement = new XElement("Column");

                //add name, id and lineageId atributes to the column node 
                columnElement.Add(new XAttribute("name", bci.Name));

                if (_serializeLineage)    //Serialize ID and LineageId
                    columnElement.Add(new XAttribute("id", bci.ID), new XAttribute("lineageId", bci.LineageID));

                if (_serializeLineage) //Serialize Data Type Information
                {
                    columnElement.Add(
                            new XAttribute("dataType", bci.DataType.ToString())
                            , new XAttribute("length", bci.Length)
                            , new XAttribute("precision", bci.Precision)
                            , new XAttribute("scale", bci.Scale)
                        );
                }

                //if column value is null add isNull attribute to the column node
                if (buffer.IsNull(bci.Index))
                {
                    columnElement.Add(new XAttribute("isNull", true));
                }
                else //get data for the column and store them as data of the column node
                {
                    string colData = string.Empty;
                    bdata = null;
                    switch (col.DataType)
                    {
                        case DataType.DT_BYTES:
                            bdata = buffer.GetBytes(bci.Index);
                            break;
                        case DataType.DT_IMAGE:
                            bdata = buffer.GetBlobData(bci.Index, 0, (int)buffer.GetBlobLength(bci.Index));
                            break;
                        case DataType.DT_NTEXT:
                            bdata = buffer.GetBlobData(bci.Index, 0, (int)buffer.GetBlobLength(bci.Index));
                            break;
                        case DataType.DT_TEXT:
                            bdata = buffer.GetBlobData(bci.Index, 0, (int)buffer.GetBlobLength(bci.Index));
                            break;
                        default:
                            colData = Convert.ToString(buffer[bci.Index], CultureInfo.InvariantCulture);
                            break;
                    }

                    if (colData == string.Empty && bdata != null)
                        colData = BitConverter.ToString(bdata).Replace("-", string.Empty);

                    columnElement.SetValue(colData);
                }
                rowElement.Add(columnElement);
            }

            return Encoding.Unicode.GetBytes(rowElement.ToString(_saveOptions));
        }

        /// <summary>
        /// Processes the Upstream ErrorInput to the Error Output
        /// </summary>
        /// <param name="buffer">Buffer of the Upstream Error Input</param>
        private void ProcessUpstreamErrors(int inputID, PipelineBuffer buffer)
        {
            if (_errorBuffer == null)
                base.ProcessInput(inputID, buffer);
            else if (!buffer.EndOfRowset)
            {
                while(buffer.NextRow())
                {
                    rowsProcessed++;
                    _errorRowsCount++;
                    _upstreamErrorRowsProcessed++;


                    _errorBuffer.AddRow();
                    foreach (KeyValuePair<ErrorColumn, int> col in upstreamColumnsMappings)
                    {
                        _errorBuffer[col.Key.Index] = buffer[col.Value];
                        //switch (col.Key.Name)
                        //{
                        //    case "LookpDetaislXml":
                        //        uint blobLen = buffer.GetBlobLength(col.Value);
                        //        byte[] blob = buffer.GetBlobData(col.Value, 0, (int)blobLen);
                        //        _errorBuffer.AddBlobData(col.Key.Index, blob);
                        //        break;
                        //    case "Count":
                        //    case "KeysCount":
                        //    case "TotalRowsCount":
                        //        _errorBuffer.SetInt32(col.Key.Index, buffer.GetInt32(col.Value));
                        //        break;
                        //    case "LookupSourceID":
                        //    case "LookupSourceDescription":
                        //        _errorBuffer.SetString(col.Key.Index, buffer.GetString(col.Value));
                        //        break;
                        //}
                    }

                }

            }
            else if (buffer.EndOfRowset)
            {
                _upstreamEOR = true;
                if (_dataEOR)
                    _errorBuffer.SetEndOfRowset();
            }
        }


        public override void PostExecute()
        {
            bool fireAgain = true;
            string msg = string.Format("Post-Execute phase is beginning. Total Rows processed: {0}, Error Aggregated Rows produced: {1}, Upstream Data Rows Processed: {2}, Upstream Data Error Rows aggregated {3}, Upstream Error Agg Rows processed: {4}",
                rowsProcessed, _errorRowsCount, _upstreadDataRowsProcessed, _upstreamDataErrorRowsProcessed, _upstreamErrorRowsProcessed);

            ComponentMetaData.FireInformation(0, ComponentMetaData.Name, msg, string.Empty, 0, ref fireAgain);
            base.PostExecute();
        }

        #endregion

        #region Private & internal Methods
        private void AddErrorOutputDefaultColumns(IDTSOutput errorOutput)
        {

            //TODO: Add possibility to set datatype and the length of the output column
            IDTSOutputColumn col = errorOutput.OutputColumnCollection.New();
            col.Name = Resources.LookupErrorAggLookupSourceIDName;
            col.Description = Resources.LookupErrorAggLookupSourceIDDescription;
            col.SetDataTypeProperties(DataType.DT_WSTR, 50, 0, 0, 0);

            IDTSCustomProperty prop = col.CustomPropertyCollection.New();
            prop.Name = Resources.LookupErrorAggLookupSourceIDName;
            prop.Description = Resources.LookupErrorAggLookupSourceIDDescription;
            prop.Value = string.Empty;


            //TODO: Add possibility to set datatype and the length of the output column
            col = errorOutput.OutputColumnCollection.New();
            col.Name = Resources.LookupErrorAggLookupSourceDescriptionName;
            col.Description = Resources.LookupErrorAggLookupSourceDescriptionDescription;
            col.SetDataTypeProperties(DataType.DT_WSTR, 128, 0, 0, 0);

            prop = col.CustomPropertyCollection.New();
            prop.Name = Resources.LookupErrorAggLookupSourceDescriptionName;
            prop.Description = Resources.LookupErrorAggLookupSourceDescriptionDescription;
            prop.Value = string.Empty;

            IDTSOutputColumn xmlCol = errorOutput.OutputColumnCollection.New();
            xmlCol.Name = Resources.LookupErrorAggLookpDetaislXmlName;
            xmlCol.Description = Resources.LookupErrorAggLookpDetaislXmlDescription;
            SetXmlColumnProperties(xmlCol);

            col = errorOutput.OutputColumnCollection.New();
            col.Name = Resources.LookupErrorAggCountName;
            col.Description = PP.SSIS.DataFlow.Properties.Resources.LookupErrorAggCountDescription;
            col.SetDataTypeProperties(DataType.DT_I4, 0, 0, 0, 0);

            col = errorOutput.OutputColumnCollection.New();
            col.Name = Resources.LookupErrorAggKeysCountName;
            col.Description = Resources.LookupErrorAggKeysCountDescription;
            col.SetDataTypeProperties(DataType.DT_I4, 0, 0, 0, 0);

            col = errorOutput.OutputColumnCollection.New();
            col.Name = "TotalRowsCount";
            col.Description = "Counts total number of rows";
            col.SetDataTypeProperties(DataType.DT_I4, 0, 0, 0, 0);

        }

        /// <summary>
        /// Adds Custom component properties to the ColumnsToXmlTransform
        /// </summary>
        private void AddComponentCustomProperties(CustomProperties propsToCreate)
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

            IDTSCustomProperty nullColumnLineageID = ComponentMetaData.CustomPropertyCollection.New();
            nullColumnLineageID.Name = GetPropertyName(CustomProperties.NullColumn);
            nullColumnLineageID.Description = Resources.LookupErrorAggNullColumnDescription;
            nullColumnLineageID.ContainsID = true;
            nullColumnLineageID.Value = -1;

            IDTSCustomProperty hashAlg = ComponentMetaData.CustomPropertyCollection.New();
            hashAlg.Name = GetPropertyName(CustomProperties.HashAlgorithm);
            hashAlg.Description = "Hash lagorithm to be used for hashing key comluns";
            hashAlg.ContainsID = false;
            hashAlg.TypeConverter = typeof(HashAlgorithmType).AssemblyQualifiedName;
            hashAlg.Value = HashAlgorithmType.None;
        }

        /// <summary>
        /// Sets all properties of an XML Column
        /// </summary>
        /// <param name="xmlColumn">XmlColumn to set properties</param>
        internal static void SetXmlColumnProperties(IDTSOutputColumn xmlColumn)
        {
            SetXmlColumnProperties(xmlColumn, SetPropertyType.All);
        }

        /// <summary>
        /// Sets XmlColumn properties for Output Column
        /// </summary>
        /// <param name="xmlColumn">Output Column to set XmlColumn Properties</param>
        internal static void SetXmlColumnProperties(IDTSOutputColumn xmlColumn, SetPropertyType propType)
        {
            List<int> IdsToRemove = new List<int>();

            foreach (IDTSCustomProperty prop in xmlColumn.CustomPropertyCollection)
            {
                if (prop.Name == Resources.XmlSaveOptionPropertyName && (propType & SetPropertyType.XmlSaveOptions) == SetPropertyType.XmlSaveOptions)
                    IdsToRemove.Add(prop.ID);
                else if (prop.Name == Resources.XmlSerializeLineageName && (propType & SetPropertyType.XmlSerializeLineage) == SetPropertyType.XmlSerializeLineage)
                    IdsToRemove.Add(prop.ID);
                else if (prop.Name == Resources.XmlSerializeDataTypeName && (propType & SetPropertyType.XmlSerializeDataType) == SetPropertyType.XmlSerializeDataType)
                    IdsToRemove.Add(prop.ID);
                //else
                //    IdsToRemove.Add(prop.ID);
            }
            
            foreach (int id in IdsToRemove)
            {
                xmlColumn.CustomPropertyCollection.RemoveObjectByID(id);
            }

            
            //Add Save Options
            if ((propType & SetPropertyType.XmlSaveOptions) == SetPropertyType.XmlSaveOptions)
            {
                IDTSCustomProperty xmlSaveOption = xmlColumn.CustomPropertyCollection.New();
                xmlSaveOption.Description = Resources.XmlSaveOptionsDescriptions;
                xmlSaveOption.Name = Resources.XmlSaveOptionPropertyName;
                xmlSaveOption.ContainsID = false;
                xmlSaveOption.EncryptionRequired = false;
                xmlSaveOption.ExpressionType = DTSCustomPropertyExpressionType.CPET_NONE;
                xmlSaveOption.TypeConverter = typeof(SaveOptions).AssemblyQualifiedName;
                xmlSaveOption.Value = SaveOptions.None;
            }

            //Lineage & Colun IDs information
            if ((propType & SetPropertyType.XmlSerializeLineage) == SetPropertyType.XmlSerializeLineage)
            {
                IDTSCustomProperty lineage = xmlColumn.CustomPropertyCollection.New();
                lineage.Description = Resources.XmlSerializeLineageDescription;
                lineage.Name = Resources.XmlSerializeLineageName;
                lineage.ContainsID = false;
                lineage.EncryptionRequired = false;
                lineage.ExpressionType = DTSCustomPropertyExpressionType.CPET_NONE;
                lineage.TypeConverter = typeof(bool).AssemblyQualifiedName;
                lineage.Value = false;
            }

            //Data Type Information
            if ((propType & SetPropertyType.XmlSerializeDataType) == SetPropertyType.XmlSerializeDataType)
            {
                IDTSCustomProperty storeDataType = xmlColumn.CustomPropertyCollection.New();
                storeDataType.Description = Resources.XmlStoreDataTypeDescription;
                storeDataType.Name = Resources.XmlSerializeDataTypeName;
                storeDataType.ContainsID = false;
                storeDataType.EncryptionRequired = false;
                storeDataType.ExpressionType = DTSCustomPropertyExpressionType.CPET_NONE;
                storeDataType.TypeConverter = typeof(bool).AssemblyQualifiedName;
                storeDataType.Value = false;
            }

            //Column Data Type
            xmlColumn.SetDataTypeProperties(DataType.DT_NTEXT, 0, 0, 0, 0);
        }
        /// <summary>
        /// Converts bytes buffer to a sequence of Hexadecimal values 
        /// </summary>
        /// <param name="data">data buffer to be converted to a string of hexadecimals</param>
        /// <returns>string with hexadecimal sequence of the original bytes</returns>
        private string BytesToHexString(byte[] data)
        {
            char[] lookup = new char[] { '0', '1', '2', '3', '4', '5', '6', '7', '8', '9', 'A', 'B', 'C', 'D', 'E', 'F' };
            int i = 0, p = 2, l = data.Length;
            char[] c = new char[l * 2 + 2];
            byte d; c[0] = '0'; c[1] = 'x';
            while (i < l)
            {
                d = data[i++];
                c[p++] = lookup[d / 0x10];
                c[p++] = lookup[d % 0x10];
            }
            return new string(c, 0, c.Length);
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

        internal static string GetPropertyName(CustomProperties customProperty)
        {
            switch (customProperty)
            {
                case CustomProperties.None:
                case CustomProperties.HashAlgorithm:
                case CustomProperties.NullColumn:
                case CustomProperties.All:
                default:
                    return customProperty.ToString();
            }
        }

        #endregion


    }
}
