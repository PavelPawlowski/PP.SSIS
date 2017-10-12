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
using PP.SSIS.DataFlow.UI;

#if SQL2008 || SQL2008R2 || SQL2012 || SQL2014 || SQL2016 || SQL2017
using IDTSOutput = Microsoft.SqlServer.Dts.Pipeline.Wrapper.IDTSOutput100;
using IDTSCustomProperty = Microsoft.SqlServer.Dts.Pipeline.Wrapper.IDTSCustomProperty100;
using IDTSOutputColumn = Microsoft.SqlServer.Dts.Pipeline.Wrapper.IDTSOutputColumn100;
using IDTSInput = Microsoft.SqlServer.Dts.Pipeline.Wrapper.IDTSInput100;
using IDTSInputColumn = Microsoft.SqlServer.Dts.Pipeline.Wrapper.IDTSInputColumn100;
using IDTSVirtualInput = Microsoft.SqlServer.Dts.Pipeline.Wrapper.IDTSVirtualInput100;
using IDTSVirtualInputColumn = Microsoft.SqlServer.Dts.Pipeline.Wrapper.IDTSVirtualInputColumn100;
using PP.SSIS.DataFlow.Common;
#endif


namespace PP.SSIS.DataFlow
{
    [DtsPipelineComponent(
        DisplayName = "Columns To XML"
        , Description = "Transforms selected input columns to XML"
        , ComponentType = ComponentType.Transform
        , IconResource = "PP.SSIS.DataFlow.Resources.ColumnsToXml.ico"
        , CurrentVersion = 5
#if SQL2017
        , UITypeName = "PP.SSIS.DataFlow.UI.ColumnsToXmlTransformationUI, PP.SSIS.DataFlow.SQL2017, Version=1.0.0.0, Culture=neutral, PublicKeyToken=b68691a82d4fb69c"
#endif
#if SQL2016
        , UITypeName = "PP.SSIS.DataFlow.UI.ColumnsToXmlTransformationUI, PP.SSIS.DataFlow.SQL2016, Version=1.0.0.0, Culture=neutral, PublicKeyToken=b99aee68060fa342"
#endif
#if SQL2014
        , UITypeName = "PP.SSIS.DataFlow.UI.ColumnsToXmlTransformationUI, PP.SSIS.DataFlow, Version=1.0.0.0, Culture=neutral, PublicKeyToken=6926746b040a83a5"
#endif
#if SQL2012
        , UITypeName = "PP.SSIS.DataFlow.UI.ColumnsToXmlTransformationUI, PP.SSIS.DataFlow, Version=1.0.0.0, Culture=neutral, PublicKeyToken=c8e167588e3f3397"
#endif
#if SQL2008
        , UITypeName = "PP.SSIS.DataFlow.UI.ColumnsToXmlTransformationUI, PP.SSIS.DataFlow, Version=1.0.0.0, Culture=neutral, PublicKeyToken=e17db8c64cd5b65b"
#endif
)
    ]
    public class ColumnsToXmlTransformation : PipelineComponent
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
            None                    = 0x00,
            /// <summary>
            /// XmlSaveOptions prperty should be set
            /// </summary>
            XmlSaveOptions          = 0x01,
            /// <summary>
            /// XmlSerializeLIneage property should be set
            /// </summary>
            XmlSerializeLineage     = 0x02,
            /// <summary>
            /// XmlSerializeDataType property should be set
            /// </summary>
            XmlSerializeDataType    = 0x04,
            /// <summary>
            /// Xml Input Columsn property should be set
            /// </summary>
            XmlInputColumns         = 0x08,
            /// <summary>
            /// Xml Source Column property should be set
            /// </summary>
            XMlSourceID             = 0x10,
            /// <summary>
            /// Xml Source Name propety should be set
            /// </summary>
            XmlSourceName           = 0x20,
            /// <summary>
            /// All properties should be set
            /// </summary>
            All = XmlSaveOptions | XmlSerializeLineage | XmlSerializeDataType | XmlInputColumns | XMlSourceID | XmlSourceName
        }

        /// <summary>
        /// Represents outptu XMLColumn
        /// </summary>
        private class XmlColumn
        {
            /// <summary>
            /// Creates a new instance of XmlColumn
            /// </summary>
            /// <param name="index">XmlColumn Output column index</param>
            /// <param name="name">XmlColumn Output column name</param>
            /// <param name="saveOptions">Save options for the XmlColumn</param>
            public XmlColumn(int index, string name, DataType dataType, SaveOptions saveOptions, bool serializeLineage, bool serializeDataType, List<int> xmlInputColumns, string sourceID, string sourceName, int dataLen)
            {
                Index = index;
                Name = name;
                DataType = dataType;
                SaveOptions = saveOptions;
                SerializeDataType = serializeDataType;
                SerializeLineage = serializeLineage;
                XmlInputColumns = xmlInputColumns;
                SourceID = sourceID;
                SourceName = sourceName;
                DataLen = dataLen;
            }
            /// <summary>
            /// Output column Index of the XmlColumn
            /// </summary>
            public int Index;
            /// <summary>
            /// XmlColumn name
            /// </summary>
            public string Name;
            /// <summary>
            /// Xml SaveOptions of the XmlColumn
            /// </summary>
            public SaveOptions SaveOptions;
            /// <summary>
            /// Specifies whether Lineage and ID should be serialized
            /// </summary>
            public bool SerializeLineage;
            /// <summary>
            /// Specifies whether DataType should be serialized
            /// </summary>
            public bool SerializeDataType;
            /// <summary>
            /// Specifies the DataType of the XmlColumn
            /// </summary>
            public DataType DataType;
            /// <summary>
            /// Specifies the ordered list index positions to InputBufferColumns for processing
            /// </summary>
            public List<int> XmlInputColumns;
            /// <summary>
            /// Optional SourceID for the output column
            /// </summary>
            public string SourceID;
            /// <summary>
            /// Optional SourceName for the output column
            /// </summary>
            public string SourceName;
            /// <summary>
            /// Daa Length of the xml column for (for DT_WSTR)
            /// </summary>
            public int DataLen;
        }

        private bool inputColumnsValid = true;

        /// <summary>
        /// List of inputBufferColumns for processing
        /// </summary>
        List<InputBufferColumnInfo> inputBufferColumns;
        /// <summary>
        /// List of output XML columns to generate
        /// </summary>
        List<XmlColumn> outputColumns;

        /// <summary>
        /// Count of rows processed by the component during runtime
        /// </summary>
        long rowsProcessed = 0;
        #endregion

        #region DesignTime
        /// <summary>
        /// Provides SSIS Component Properties
        /// </summary>
        public override void ProvideComponentProperties()
        {
            this.ComponentMetaData.Version = PipelineUtils.GetPipelineComponentVersion(this);

            this.ComponentMetaData.Name = Resources.ColumnsToXMLTransformationName;
            this.ComponentMetaData.Description = Resources.ColumnsToXMLTransformationDescription;
            this.ComponentMetaData.ContactInfo = Resources.TransformationContactInfo;

            // Reset the component.
            base.RemoveAllInputsOutputsAndCustomProperties();

            //call base method to insert input and output
            base.ProvideComponentProperties();

            //Set Input properties
            IDTSInput input = ComponentMetaData.InputCollection[0];
            input.Name = Resources.ColumnsToXmlInputName;

            // Set Output properties
            IDTSOutput output = ComponentMetaData.OutputCollection[0];
            output.Name = Resources.ColumnsToXmlOutputName;
            output.SynchronousInputID = input.ID;

            //Add Custom Properties for the component
            AddComponentCustomProperties();
        }
        /// <summary>
        /// Validate HashColumnsTransform metadata
        /// </summary>
        /// <returns></returns>
        public override DTSValidationStatus Validate()
        {
            if (ComponentMetaData.InputCollection.Count != 1)
            {
                FireComponentMetadataError(0, Resources.ErrorIncorrectNumberOfInputs);
                return DTSValidationStatus.VS_NEEDSNEWMETADATA;
            }

            if (ComponentMetaData.OutputCollection.Count != 1)
            {
                FireComponentMetadataError(0, Resources.ErrorIncorrectNumberOfOutputs);
                return DTSValidationStatus.VS_NEEDSNEWMETADATA;
            }

            IDTSInput input = ComponentMetaData.InputCollection[0];
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

            IDTSOutput output = ComponentMetaData.OutputCollection[0];
            List<IDTSOutputColumn> redundantColumns = new List<IDTSOutputColumn>(output.OutputColumnCollection.Count);
            bool xmlColumnExists = false;
            List<int> missingLineages = new List<int>();
            List<int> missingInputLineages = new List<int>();
            bool missingError = false;

            foreach (IDTSOutputColumn outputColumn in output.OutputColumnCollection)
            {
                bool isRendundant = true;
                bool isInputColumn = false;
                foreach (IDTSInputColumn inputColumn in input.InputColumnCollection)
                {
                    if (inputColumn.Name == outputColumn.Name)
                    {
                        isRendundant = false;
                        isInputColumn = true;
                        break;
                    }
                }


                //Check if XML Column;
                if (!isInputColumn)
                {
                    foreach (IDTSCustomProperty prop in outputColumn.CustomPropertyCollection)
                    {
                        if (prop.Name == Resources.XmlInputColumnsPropertyname)
                        {
                            isRendundant = false;
                            xmlColumnExists = true;

                            var lineages = InputColumns.ParseInputLineages(prop.Value.ToString());

                            foreach (IDTSInputColumn icol in input.InputColumnCollection)
                            {
                                int l = icol.LineageID;
                                lineages.Remove(l);
                                if (lineages.Count == 0)
                                    break;
                            }

                            if (lineages.Count > 0)
                            {
                                foreach (int l in lineages)
                                {
                                    if (!missingInputLineages.Contains(l))
                                        missingInputLineages.Add(l);
                                }

                                foreach (IDTSVirtualInputColumn vcol in vInput.VirtualInputColumnCollection)
                                {
                                    int l = vcol.LineageID;
                                    lineages.Remove(l);
                                    if (lineages.Count == 0)
                                        break;
                                }

                                if (lineages.Count > 0)
                                {
                                    foreach (int l in lineages)
                                    {
                                        if (!missingLineages.Contains(l))
                                            missingLineages.Add(l);
                                    }
                                }
                            }

                        }
                        if (prop.Name == Resources.XmlSaveOptionPropertyName)
                        {
                            isRendundant = false;
                            xmlColumnExists = true;

                            if (!Enum.IsDefined(typeof(SaveOptions), prop.Value))
                            {
                                FireComponentMetadataError(0, string.Format(Resources.ErrorInvalidSaveOption, outputColumn.Name));
                                return DTSValidationStatus.VS_NEEDSNEWMETADATA;
                            }
                            else
                            {
                                if (outputColumn.DataType != DataType.DT_NTEXT && outputColumn.DataType != DataType.DT_WSTR)
                                {
                                    FireComponentMetadataError(0, string.Format(Resources.ErrorInvalidDataType, outputColumn.Name));
                                    return DTSValidationStatus.VS_NEEDSNEWMETADATA;
                                }
                            }
                        }
                        
                    }
                }

                //add redundand column to redundand list
                if (isRendundant)
                    redundantColumns.Add(outputColumn);
            }


            if (missingLineages.Count > 0)
            {

                string ml = string.Join(", ", missingLineages.ConvertAll<string>(l => l.ToString()).ToArray());

                FireComponentMetadataError(0, string.Format("Output columns are referencing input column lineages which do not exists: {0}", ml));
                missingError = true;
            }

            if (missingInputLineages.Count > 0)
            {
                string ml = string.Join(", ", missingInputLineages.ConvertAll<string>(l => l.ToString()).ToArray());

                FireComponentMetadataError(0, string.Format("Output columns are referencing input column lineages which are not selected as Input columns: {0}", ml));
                missingError = true;
            }

            if (missingError)
                return DTSValidationStatus.VS_NEEDSNEWMETADATA;



            //remove redundand output columns
            foreach (IDTSOutputColumn col in redundantColumns)
            {
                output.OutputColumnCollection.RemoveObjectByID(col.ID);
            }

            //If XmlColumn does not exists, create a default one
            if (!xmlColumnExists)
            {
                IDTSOutputColumn xmlCol = output.OutputColumnCollection.New();
                xmlCol.Name = Resources.XmlColumnDefaultName;
                xmlCol.Description = Resources.XmlColumnDefaultDesccription;

                SetXmlColumnProperties(xmlCol);
            }

            return DTSValidationStatus.VS_ISVALID;
        }
        /// <summary>
        /// Upgrades the component from earlier versions
        /// </summary>
        /// <param name="pipelineVersion"></param>
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
                IDTSInput input = ComponentMetaData.InputCollection[0];
                List<int> propsToRemove = new List<int>();
                List<KeyValuePair<int, int>> colLienagesSort = new List<KeyValuePair<int, int>>(input.InputColumnCollection.Count);

                string sourceID = string.Empty;
                string sourceName = string.Empty;
                try
                {
                    IDTSCustomProperty prop = ComponentMetaData.CustomPropertyCollection[Resources.ColumnsToXmlSourceNameProperty];
                    if (prop != null)
                    {
                        sourceName = prop.Value.ToString();
                        ComponentMetaData.CustomPropertyCollection.RemoveObjectByID(prop.ID);
                    }
                }
                catch { }

                try
                {
                    IDTSCustomProperty prop = ComponentMetaData.CustomPropertyCollection[Resources.ColumnsToXmlSourceIDProperty];
                    if (prop != null)
                    {
                        sourceID = prop.Value.ToString();
                        ComponentMetaData.CustomPropertyCollection.RemoveObjectByID(prop.ID);
                    }
                }
                catch
                { }

                foreach (IDTSInputColumn col in input.InputColumnCollection)
                {
                    int sortOrder = int.MaxValue;
                    IDTSCustomProperty prop = null;
                    propsToRemove = new List<int>();

                    foreach (IDTSCustomProperty p in col.CustomPropertyCollection)
                    {
                        if (p.Name == Resources.InputSortOrderPropertyName)
                        {
                            sortOrder = (int)p.Value;
                            propsToRemove.Add(p.ID);
                            break;
                        }
                    }

                    colLienagesSort.Add(new KeyValuePair<int, int>(col.LineageID, sortOrder));

                    foreach (int id in propsToRemove)
                    {
                        col.CustomPropertyCollection.RemoveObjectByID(id);
                    }

                }

                colLienagesSort.Sort((a, b) => a.Value.CompareTo(b.Value));


                IDTSOutput output = ComponentMetaData.OutputCollection[0];                

                foreach (IDTSOutputColumn col in output.OutputColumnCollection)
                {
                    SetPropertyType setProp = SetPropertyType.All;
                    bool additionalPropertyExists = false;

                    bool setDataType = !(col.DataType == DataType.DT_NTEXT || (col.DataType == DataType.DT_WSTR && col.Length <= 4000));

                    foreach (IDTSCustomProperty prop in col.CustomPropertyCollection)
                    {
                        if (prop.Name == Resources.XmlSaveOptionPropertyName)
                            setProp ^= SetPropertyType.XmlSaveOptions;
                        else if (prop.Name == Resources.XmlSerializeLineageName)
                            setProp ^= SetPropertyType.XmlSerializeLineage;
                        else if (prop.Name == Resources.XmlSerializeDataTypeName)
                            setProp ^= SetPropertyType.XmlSerializeDataType;
                        else if (prop.Name == Resources.XmlInputColumnsPropertyname)
                            setProp ^= SetPropertyType.XmlInputColumns;
                        else if (prop.Name == Resources.XmlSourceIdPropertyName)
                            setProp ^= SetPropertyType.XMlSourceID;
                        else if (prop.Name == Resources.XmlSourceNamePropertyName)
                            setProp ^= SetPropertyType.XmlSourceName;
                        else
                            additionalPropertyExists = true;
                    }

                    if (setProp != SetPropertyType.None || additionalPropertyExists || setDataType)
                        SetXmlColumnProperties(col, setProp, setDataType);

                    if ((setProp & SetPropertyType.XmlInputColumns) == SetPropertyType.XmlInputColumns && colLienagesSort.Count > 0)
                    {
                        string lineages = InputColumns.BuildInputLineagesString(colLienagesSort.ConvertAll<int>(kvp => kvp.Key));
                        col.CustomPropertyCollection[Resources.XmlInputColumnsPropertyname].Value = lineages;
                    }

                    if ((setProp & SetPropertyType.XMlSourceID) == SetPropertyType.XMlSourceID)
                    {
                        col.CustomPropertyCollection[Resources.XmlSourceIdPropertyName].Value = sourceID;
                    }

                    if ((setProp & SetPropertyType.XmlSourceName) == SetPropertyType.XmlSourceName)
                    {
                        col.CustomPropertyCollection[Resources.XmlSourceNamePropertyName].Value = sourceName;
                    }
                }

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

            return col;
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

                foreach (IDTSInputColumn col in input.InputColumnCollection)
                {
                    IDTSVirtualInputColumn100 vCol = vInput.VirtualInputColumnCollection.GetVirtualInputColumnByLineageID(col.LineageID);
                    if (vCol == null)
                        input.InputColumnCollection.RemoveObjectByID(col.ID);
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
            //Insert new column into specified output
            IDTSOutput output = ComponentMetaData.OutputCollection.FindObjectByID(outputID);
            IDTSOutputColumn xmlColumn = output.OutputColumnCollection.NewAt(outputColumnIndex);
            xmlColumn.Name = name;
            xmlColumn.Description = description;

            SetXmlColumnProperties(xmlColumn);

            return xmlColumn;
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

                //outputColumn.SetDataTypeProperties(DataType.DT_NTEXT, 0, 0, 0, 0);

                return base.SetOutputColumnProperty(outputID, outputColumnID, propertyName, propertyValue);
            }
            else if (propertyName == Resources.XmlSerializeLineageName || propertyName == Resources.XmlSerializeDataTypeName || propertyName == "DataType" || propertyName == "DataLength")
            {
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

                inputBufferColumns.Add(new InputBufferColumnInfo(BufferManager.FindColumnByLineageID(input.Buffer, column.LineageID),
                    column.Name, column.ID, column.LineageID, 0, column.DataType, column.Length, column.Precision, column.Scale));
            }

            outputColumns = new List<XmlColumn>();
            IDTSOutput output = ComponentMetaData.OutputCollection[0];

            foreach (IDTSOutputColumn col in output.OutputColumnCollection)
            {
                SaveOptions saveOptions = SaveOptions.None;
                bool serializeDataType = false;
                bool serializeLineage = false;
                string sourceID = string.Empty;
                string sourceName = string.Empty;
                List<int> cols = null;

                foreach (IDTSCustomProperty prop in col.CustomPropertyCollection)
                {
                    if (prop.Name == Resources.XmlSaveOptionPropertyName)
                    {
                        saveOptions = (SaveOptions)prop.Value;

                    }
                    else if (prop.Name == Resources.XmlSerializeDataTypeName)
                    {
                        serializeDataType = (bool)prop.Value;
                    }
                    else if (prop.Name == Resources.XmlSerializeLineageName)
                    {
                        serializeLineage = (bool)prop.Value;
                    }
                    else if (prop.Name == Resources.XmlInputColumnsPropertyname)
                    {
                        string colsStr = prop.Value.ToString();
                        var colLineages = InputColumns.ParseInputLineages(colsStr);

                        cols = new List<int>(colLineages.Count);

                        foreach (int lineageID in colLineages)
                        {
                            int idx = inputBufferColumns.FindIndex(ibci => ibci.LineageID == lineageID);
                            cols.Add(idx);
                        }
                    }
                    else if (prop.Name == Resources.XmlSourceIdPropertyName)
                    {
                        sourceName = prop.Value.ToString();
                    }
                    else if (prop.Name == Resources.XmlSourceNamePropertyName)
                    {
                        sourceID = prop.Value.ToString();
                    }
                }

                int index = BufferManager.FindColumnByLineageID(input.Buffer, col.LineageID);
                outputColumns.Add(new XmlColumn(index, col.Name, col.DataType, saveOptions, serializeLineage, serializeDataType, cols, sourceID, sourceName, col.Length));
            }
        }
        /// <summary>
        /// Processes rows in the inpuyt buffer
        /// </summary>
        /// <param name="inputID">ID of the input to process the input rows</param>
        /// <param name="buffer">Pipeline bufere with rows to process</param>
        public override void ProcessInput(int inputID, PipelineBuffer buffer)
        {
            if (!buffer.EndOfRowset)
            {
                while (buffer.NextRow())
                {
                    rowsProcessed++;

                    if (outputColumns.Count == 0)
                        continue;

                    XmlColumn lastCol = null;
                    XElement rowElement = null;

                    foreach (XmlColumn outCol in outputColumns)
                    {
                        if (rowElement == null || lastCol == null || lastCol.SerializeLineage != outCol.SerializeLineage || lastCol.SerializeDataType != outCol.SerializeDataType)
                        {
                            rowElement = new XElement("row");

                            //Add sourceID and sourceNme attributes to the row node if those are specified in the component properties
                            if (!string.IsNullOrEmpty(outCol.SourceID))
                                rowElement.Add(new XAttribute("sourceID", outCol.SourceID));
                            if (!string.IsNullOrEmpty(outCol.SourceName))
                                rowElement.Add(new XAttribute("sourceName", outCol.SourceName));

                            XElement columnElement = null;
                            //Process XML for selected input columns
                            for (int i = 0; i < outCol.XmlInputColumns.Count; i++)
                            {
                                int colIdx = outCol.XmlInputColumns[i];

                                InputBufferColumnInfo bci = inputBufferColumns[colIdx];
                                BufferColumn col = buffer.GetColumnInfo(bci.Index);

                                byte[] bdata;

                                columnElement = new XElement("Column");

                                //add name, id and lineageId atributes to the column node 
                                columnElement.Add(new XAttribute("name", bci.Name));
                                
                                if (outCol.SerializeLineage)    //Serialize ID and LineageId
                                    columnElement.Add(new XAttribute("id", bci.ID), new XAttribute("lineageId", bci.LineageID));

                                if (outCol.SerializeDataType) //Serialize Data Type Information
                                {
                                    columnElement.Add(
                                            new XAttribute("dataType", bci.DataType.ToString())
                                            ,new XAttribute("length", bci.Length)
                                            ,new XAttribute("precision", bci.Precision)
                                            ,new XAttribute("scale", bci.Scale)
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
                                    switch (col.DataType)
                                    {
                                        case DataType.DT_BYTES:
                                            bdata = buffer.GetBytes(bci.Index);
                                            colData = BytesToHexString(bdata);  //convert binary data to a hexadecimal string
                                            break;
                                        case DataType.DT_IMAGE:
                                            bdata = buffer.GetBlobData(bci.Index, 0, (int)buffer.GetBlobLength(bci.Index));
                                            colData = BytesToHexString(bdata); //convert binary data to a hexadecimal string
                                            break;
                                        case DataType.DT_NTEXT:
                                            bdata = buffer.GetBlobData(bci.Index, 0, (int)buffer.GetBlobLength(bci.Index));
                                            colData = Encoding.Unicode.GetString(bdata);
                                            break;
                                        case DataType.DT_TEXT:
                                            bdata = buffer.GetBlobData(bci.Index, 0, (int)buffer.GetBlobLength(bci.Index));
                                            colData = Encoding.GetEncoding(col.CodePage).GetString(bdata);
                                            break;
                                        default:
                                            colData = Convert.ToString(buffer[bci.Index], CultureInfo.InvariantCulture);
                                            break;
                                    }

                                    columnElement.SetValue(colData);
                                    rowElement.Add(columnElement);
                                }
                            }
                        }

                        if (outCol.DataType == DataType.DT_WSTR)
                        {
                            string str = rowElement.ToString(outCol.SaveOptions);                            
                            if (str.Length > outCol.DataLen)
                            {
                                bool cancel = false;
                                string msg = string.Format("Data Truncation has occured when processing row {0}. Could not write {1} characters into column [{2}] of length {3}", rowsProcessed, str.Length, outCol.Name, outCol.DataLen);
                                this.ComponentMetaData.FireError(0, this.ComponentMetaData.Name, msg, string.Empty, 0, out cancel);
                                if (cancel)
                                {
                                    throw new System.Exception(msg);
                                }
                                break;
                            }
                            buffer.SetString(outCol.Index, str);
                        }
                        else
                            buffer.AddBlobData(outCol.Index, Encoding.Unicode.GetBytes(rowElement.ToString(outCol.SaveOptions)));
                        lastCol = outCol;
                    }
                }
            }
        }
        /// <summary>
        /// PostExecute Phase of teh component
        /// </summary>
        public override void PostExecute()
        {
            bool fireAgain = true;
            this.ComponentMetaData.FireInformation(0, this.ComponentMetaData.Name, "Post-Execute phase is beginning. Processed " + rowsProcessed.ToString(CultureInfo.CurrentCulture) + " rows.", string.Empty, 0, ref fireAgain);
            base.PostExecute();
        }

        #endregion

        #region Private internal Methods
        /// <summary>
        /// Adds Custom component properties to the ColumnsToXmlTransform
        /// </summary>
        private void AddComponentCustomProperties()
        {
            //Keeping for future usage
        }

        internal static void SetXmlColumnProperties(IDTSOutputColumn xmlColumn)
        {
            SetXmlColumnProperties(xmlColumn, SetPropertyType.All, true);
        }

        /// <summary>
        /// Sets XmlColumn properties for Output Column
        /// </summary>
        /// <param name="xmlColumn">Output Column to set XmlColumn Properties</param>
        internal static void SetXmlColumnProperties(IDTSOutputColumn xmlColumn, SetPropertyType propType, bool setDataType)
        {
            List<int> IdsToRemove = new List<int>();

            foreach (IDTSCustomProperty prop in xmlColumn.CustomPropertyCollection)
            {
                if (
                    (prop.Name == Resources.XmlSaveOptionPropertyName && (propType & SetPropertyType.XmlSaveOptions) == SetPropertyType.XmlSaveOptions) ||
                    (prop.Name == Resources.XmlSerializeLineageName && (propType & SetPropertyType.XmlSerializeLineage) == SetPropertyType.XmlSerializeLineage) ||
                    (prop.Name == Resources.XmlSerializeDataTypeName && (propType & SetPropertyType.XmlSerializeDataType) == SetPropertyType.XmlSerializeDataType) ||
                    (prop.Name == Resources.XmlInputColumnsPropertyname && (propType & SetPropertyType.XmlInputColumns) == SetPropertyType.XmlInputColumns) ||
                    (prop.Name == Resources.XmlSourceIdPropertyName && (propType & SetPropertyType.XMlSourceID) == SetPropertyType.XMlSourceID) ||
                    (prop.Name == Resources.XmlSourceNamePropertyName && (propType & SetPropertyType.XmlSourceName) == SetPropertyType.XmlSourceName)
                )
                {
                    IdsToRemove.Add(prop.ID);
                }
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

            if ((propType & SetPropertyType.XmlInputColumns) == SetPropertyType.XmlInputColumns)
            {
                IDTSCustomProperty storeDataType = xmlColumn.CustomPropertyCollection.New();
                storeDataType.Name = Resources.XmlInputColumnsPropertyname;
                storeDataType.Description = "Specifies the columns to be used to build Xml";
                storeDataType.ContainsID = true;
                storeDataType.EncryptionRequired = false;
                storeDataType.ExpressionType = DTSCustomPropertyExpressionType.CPET_NONE;
                storeDataType.Value = string.Empty;
            }

            if ((propType & SetPropertyType.XMlSourceID) == SetPropertyType.XMlSourceID)
            {
                IDTSCustomProperty storeDataType = xmlColumn.CustomPropertyCollection.New();
                storeDataType.Name = Resources.XmlSourceIdPropertyName;
                storeDataType.Description = "Specifies optional descriptive ID of the culumns source";
                storeDataType.ContainsID = false;
                storeDataType.EncryptionRequired = false;
                storeDataType.ExpressionType = DTSCustomPropertyExpressionType.CPET_NONE;
                storeDataType.Value = string.Empty;
            }

            if ((propType & SetPropertyType.XmlSourceName) == SetPropertyType.XmlSourceName)
            {
                IDTSCustomProperty storeDataType = xmlColumn.CustomPropertyCollection.New();
                storeDataType.Name = Resources.XmlSourceNamePropertyName;
                storeDataType.Description = "Specifies optional descriptive Name of the columns source";
                storeDataType.ContainsID = false;
                storeDataType.EncryptionRequired = false;
                storeDataType.ExpressionType = DTSCustomPropertyExpressionType.CPET_NONE;
                storeDataType.Value = string.Empty;
            }

            //Column Data Type
            if (setDataType)
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

        #endregion


    }
}
