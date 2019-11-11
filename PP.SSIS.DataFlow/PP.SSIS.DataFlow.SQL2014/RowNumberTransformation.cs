// <copyright file="RowNumberTransformation.cs" company="Pavel Pawlowski">
// Copyright (c) 2014, 2015 All Right Reserved
//
// THIS CODE AND INFORMATION ARE PROVIDED "AS IS" WITHOUT WARRANTY OF ANY 
// KIND, EITHER EXPRESSED OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE
// IMPLIED WARRANTIES OF MERCHANTABILITY AND/OR FITNESS FOR A
// PARTICULAR PURPOSE.
//
// </copyright>
// <author>Pavel Pawlowski</author>
// <summary>Contains implementaiton of the RowNumberTransformation</summary>
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.SqlServer.Dts.Pipeline;
using Microsoft.SqlServer.Dts.Pipeline.Wrapper;
using Microsoft.SqlServer.Dts.Runtime.Wrapper;
using System.Xml.Linq;
using System.Globalization;
using System.Security.Cryptography;
using System.IO;
using PP.SSIS.DataFlow.Properties;
using PP.SSIS.DataFlow.Common;

using IDTSOutput = Microsoft.SqlServer.Dts.Pipeline.Wrapper.IDTSOutput100;
using IDTSCustomProperty = Microsoft.SqlServer.Dts.Pipeline.Wrapper.IDTSCustomProperty100;
using IDTSOutputColumn = Microsoft.SqlServer.Dts.Pipeline.Wrapper.IDTSOutputColumn100;
using IDTSInput = Microsoft.SqlServer.Dts.Pipeline.Wrapper.IDTSInput100;
using IDTSInputColumn = Microsoft.SqlServer.Dts.Pipeline.Wrapper.IDTSInputColumn100;
using IDTSVirtualInput = Microsoft.SqlServer.Dts.Pipeline.Wrapper.IDTSVirtualInput100;

namespace PP.SSIS.DataFlow
{
    /// <summary>
    /// RowNumberTransformation - Generates Row Numbesrs. 
    /// Supported only by SQL2015 and above with .NetFramework 4 and above due to usage of DynamicProperties for different data types of increment
    /// </summary>
    [DtsPipelineComponent(
        DisplayName = "Row Number"
        , Description = "Generates Row Numbers based on DataType and Increment"
        , ComponentType = ComponentType.Transform
        , IconResource = "PP.SSIS.DataFlow.Resources.RowNumber.ico"
        , CurrentVersion = 1

#if SQL2019
        , UITypeName = "PP.SSIS.DataFlow.UI.RowNumberTransformationUI, PP.SSIS.DataFlow.SQL2019, Version=1.0.0.0, Culture=neutral, PublicKeyToken=b68691a82d4fb69c"
#endif
#if SQL2017
        , UITypeName = "PP.SSIS.DataFlow.UI.RowNumberTransformationUI, PP.SSIS.DataFlow.SQL2017, Version=1.0.0.0, Culture=neutral, PublicKeyToken=b68691a82d4fb69c"
#endif
#if SQL2016
        , UITypeName = "PP.SSIS.DataFlow.UI.RowNumberTransformationUI, PP.SSIS.DataFlow.SQL2016, Version=1.0.0.0, Culture=neutral, PublicKeyToken=b99aee68060fa342"
#endif
#if SQL2014
        , UITypeName = "PP.SSIS.DataFlow.UI.RowNumberTransformationUI, PP.SSIS.DataFlow, Version=1.0.0.0, Culture=neutral, PublicKeyToken=6926746b040a83a5"
#endif
#if SQL2012
        , UITypeName = "PP.SSIS.DataFlow.UI.RowNumberTransformationUI, PP.SSIS.DataFlow, Version=1.0.0.0, Culture=neutral, PublicKeyToken=c8e167588e3f3397"
#endif
#if SQL2008
        , UITypeName = "PP.SSIS.DataFlow.UI.RowNumberTransformationUI, PP.SSIS.DataFlow, Version=1.0.0.0, Culture=neutral, PublicKeyToken=e17db8c64cd5b65b"
#endif
)]
    public class RowNumberTransformation : PipelineComponent
    {
        #region Definitions
        /// <summary>

        /// Defines Increment Type
        /// </summary>
        private enum IncrementByType
        {
            /// <summary>
            /// Value Increment
            /// </summary>
            Value,
            /// <summary>
            /// Increment by Days for Date values
            /// </summary>
            Day,
            /// <summary>
            /// Increment by Months for Date values
            /// </summary>
            Month,
            /// <summary>
            /// Increment by Weeks for Date values
            /// </summary>
            Week,
            /// <summary>
            /// Increment by Years for Data values
            /// </summary>
            Year
        }

        /// <summary>
        /// RownNumberColumns for OutputColumns processing
        /// </summary>
        private class RowNumberColumn
        {
            public int Index;
            public dynamic IncrementBy;
            public IncrementByType IncrementType;
            public DataType DataType;
            public dynamic InitialValue;
        }

        private bool inputColumnsValid = true;
        /// <summary>
        /// List containing Output RowNumber Columns
        /// </summary>
        private List<RowNumberColumn> rowNumberColumns;


        #endregion

        #region Designtime
        /// <summary>
        /// Provides SSIS Component Properties
        /// </summary>
        public override void ProvideComponentProperties()
        {
            this.ComponentMetaData.Version = PipelineUtils.GetPipelineComponentVersion(this);

            this.ComponentMetaData.Name = Resources.RowNumberTransformationName;
            this.ComponentMetaData.Description = Resources.RowNumberTranformationDescription;
            this.ComponentMetaData.ContactInfo = Resources.TransformationContactInfo;

            // Reset the component.
            base.RemoveAllInputsOutputsAndCustomProperties();

            //Call base method to add input and output
            base.ProvideComponentProperties();


            //Set Input properties
            IDTSInput input = ComponentMetaData.InputCollection[0];
            input.Name = Resources.RowNumberInputName;

            //Set Output properties
            IDTSOutput output = ComponentMetaData.OutputCollection[0];
            output.Name = Resources.RowNumberOutputName;
            output.SynchronousInputID = input.ID;
        }
        /// <summary>
        /// Validate HashColumnsTransform metadata
        /// </summary>
        /// <returns></returns>
        public override DTSValidationStatus Validate()
        {
            //Check that we have only one input
            if (ComponentMetaData.InputCollection.Count != 1)
            {
                FireComponentMetadataError(0, Resources.ErrorIncorrectNumberOfInputs);
                return DTSValidationStatus.VS_NEEDSNEWMETADATA;
            }
            //Check that we have only one output
            if (ComponentMetaData.OutputCollection.Count != 1)
            {
                FireComponentMetadataError(0, Resources.ErrorIncorrectNumberOfOutputs);
                return DTSValidationStatus.VS_NEEDSNEWMETADATA;
            }

            IDTSInput input = ComponentMetaData.InputCollection[0];
            IDTSVirtualInput vInput = input.GetVirtualInput();

            //Check Input columns that they exists
            foreach (IDTSInputColumn column in input.InputColumnCollection)
            {
                try
                {
                    IDTSVirtualInputColumn100 vColumn = vInput.VirtualInputColumnCollection.GetVirtualInputColumnByLineageID(column.LineageID);
                }
                catch
                {
                    inputColumnsValid = false;
                    FireComponentMetadataError(0, string.Format(Resources.ErrorInputColumnNotInUpstreamComponent, column.IdentificationString));
                    return DTSValidationStatus.VS_NEEDSNEWMETADATA;
                }
            }

            IDTSOutput output = ComponentMetaData.OutputCollection[0];
            List<IDTSOutputColumn> redundantColumns = new List<IDTSOutputColumn>(output.OutputColumnCollection.Count);
            bool rowNumberColumnExists = false;

            foreach (IDTSOutputColumn outputColumn in output.OutputColumnCollection)
            {
                bool isRendundant = true;

                foreach (IDTSCustomProperty prop in outputColumn.CustomPropertyCollection)
                {
                    if (prop.Name == Resources.RowNumberIncrementByPropertyName)
                    {
                        isRendundant = false;
                        rowNumberColumnExists = true;
                    }

                    //TODO: Here will be checks for proper properties settings
                }
                //add redundand column to redundand list
                if (isRendundant)
                    redundantColumns.Add(outputColumn);
            }

            //remove redundand output columns
            foreach (IDTSOutputColumn100 col in redundantColumns)
            {
                output.OutputColumnCollection.RemoveObjectByID(col.ID);
            }

            if (!rowNumberColumnExists)
            {
                rowNumberColumnExists = true;
                output = ComponentMetaData.OutputCollection[0];
                IDTSOutputColumn rowNumberColumn = output.OutputColumnCollection.New();
                rowNumberColumn.Name = Resources.RowNumberDefaultColumnName;
                rowNumberColumn.Description = Resources.RowNumberDefaultColumnDescription;

                SetRowNumberColumnProperties(rowNumberColumn);
            }

            return DTSValidationStatus.VS_ISVALID;
        }
        /// <summary>
        /// Reinitialize metadata in case input cvolumns are invalid
        /// </summary>
        public override void ReinitializeMetaData()
        {
            if (!inputColumnsValid)
            {
                IDTSInput input = ComponentMetaData.InputCollection[0];
                IDTSVirtualInput100 vInput = input.GetVirtualInput();

                foreach (IDTSInputColumn col in input.InputColumnCollection)
                {
                    input.InputColumnCollection.RemoveObjectByID(col.ID);
                }
                inputColumnsValid = true;
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

                ComponentMetaData.Version = currentVersion;
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
        /// <returns>Throws error as this component does not allows inserting of new inputs</returns>
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
            //Insert new column into specified output
            IDTSOutputColumn rowNumColumn = base.InsertOutputColumnAt(outputID, outputColumnIndex, name, description);

            SetRowNumberColumnProperties(rowNumColumn);

            return rowNumColumn;
        }

        public override void SetOutputColumnDataTypeProperties(int iOutputID, int iOutputColumnID, DataType eDataType, int iLength, int iPrecision, int iScale, int iCodePage)
        {
            //Check valid data types
            switch (eDataType)
            {
                case DataType.DT_DATE:
                case DataType.DT_DBDATE:
                case DataType.DT_I1:
                case DataType.DT_I2:
                case DataType.DT_I4:
                case DataType.DT_I8:
                case DataType.DT_NUMERIC:
                case DataType.DT_R4:
                case DataType.DT_R8:
                case DataType.DT_UI1:
                case DataType.DT_UI2:
                case DataType.DT_UI4:
                case DataType.DT_UI8:
                    break;
                default:
                    throw new Exception("Unsupported Data Type");
            }


            IDTSOutputColumn col = ComponentMetaData.OutputCollection.GetObjectByID(iOutputID).OutputColumnCollection.GetObjectByID(iOutputColumnID);
            
            foreach (IDTSCustomProperty prop in col.CustomPropertyCollection)
            {
                switch (prop.Name)
                {
                    case "IncrementBy":
                        prop.Value = GetDefaultDataTypeIncrement(eDataType);
                        col.SetDataTypeProperties(eDataType, iLength, iPrecision, iScale, iCodePage);                
                        break;
                    case "InitialValue":
                        prop.Value = GetDataTypeInitialValue(eDataType);
                        col.SetDataTypeProperties(eDataType, iLength, iPrecision, iScale, iCodePage);                
                        break;
                }
            }
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
            IDTSOutputColumn col = ComponentMetaData.OutputCollection.GetObjectByID(outputID).OutputColumnCollection.GetObjectByID(outputColumnID);

            if (propertyName == Resources.RowNumberIncrementByPropertyName)
            {
                if(col.DataType == DataType.DT_DBDATE || col.DataType == DataType.DT_DATE)
                {
                    string pv = propertyValue.ToString().ToLower();
                    if (string.IsNullOrEmpty(pv))
                    {
                        propertyValue = "1d";
                    }
                    else
                    {
                        int ival;
                        char ch = pv[pv.Length - 1];
                        string val = pv.Substring(0, pv.Length - 1);

                        if (int.TryParse(pv, out ival))
                        {
                            propertyValue = ival.ToString() + "d";
                        }
                        else if ((ch == 'd' || ch == 'w' || ch == 'm' || ch == 'y') && int.TryParse(val, out ival))
                        {
                            propertyValue = pv;
                        }
                        else
                        {
                            throw new System.InvalidCastException(Resources.ErrorUnsupportedRowNumberIncrementValue);
                        }
                    }
                }
                return base.SetOutputColumnProperty(outputID, outputColumnID, propertyName, propertyValue);
            }

            if (propertyName == Resources.RowNumberInitialValuePropertyName)
                return base.SetOutputColumnProperty(outputID, outputColumnID, propertyName, propertyValue);

            throw new Exception(string.Format(Resources.ErrorOutputColumnPropertyCannotBeChanged, propertyName));
        }

        #endregion

        #region Runtime Processing
        /// <summary>
        /// PreExecute Phase for initialization of internal runtime structures
        /// </summary>
        public override void PreExecute()
        {
            base.PreExecute();
            IDTSInput input = ComponentMetaData.InputCollection[0];

            for (int i = 0; i < input.InputColumnCollection.Count; i++)
            {
                IDTSInputColumn column = input.InputColumnCollection[i];
            }

            IDTSOutput output = ComponentMetaData.OutputCollection[0];
            rowNumberColumns = new List<RowNumberColumn>();

            //Iterate thorough OutputColumns collection and generate and prepare RowNumberColumns
            foreach (IDTSOutputColumn col in output.OutputColumnCollection)
            {
                if (col.CustomPropertyCollection.Count == 2)
                {
                    RowNumberColumn numCol = new RowNumberColumn();
                    numCol.Index = BufferManager.FindColumnByLineageID(input.Buffer, col.LineageID);
                    numCol.DataType = col.DataType;


                    foreach (IDTSCustomProperty prop in col.CustomPropertyCollection)
                    {
                        switch (prop.Name)
                        {
                            case "IncrementBy":
                                if (col.DataType == DataType.DT_DATE || col.DataType == DataType.DT_DBDATE)
                                {
                                    string pv = prop.Value.ToString();
                                    char ch = pv[pv.Length - 1];
                                    switch (ch)
                                    {
                                        case 'd':
                                            numCol.IncrementType = IncrementByType.Day;
                                            break;
                                        case 'w':
                                            numCol.IncrementType = IncrementByType.Week;
                                            break;
                                        case 'm':
                                            numCol.IncrementType = IncrementByType.Month;
                                            break;
                                        case 'y':
                                            numCol.IncrementType = IncrementByType.Year;
                                            break;
                                    }
                                    numCol.IncrementBy = int.Parse(pv.Substring(0, pv.Length - 1));
                                }
                                else
                                {
                                    numCol.IncrementBy = prop.Value;
                                    numCol.IncrementType = IncrementByType.Value;
                                }
                                break;
                            case "InitialValue":
                                numCol.InitialValue = prop.Value;
                                break;
                            default:
                                break;
                        }
                    }
                    rowNumberColumns.Add(numCol);
                }
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
                    if (rowNumberColumns.Count == 0)
                        continue;

                    //foreach (RowNumberColumn col in rowNumberColumns)
                    for (int i = 0; i < rowNumberColumns.Count; i++)
                    {
                        RowNumberColumn col = rowNumberColumns[i];
                        switch (col.DataType)
                        {
                            case DataType.DT_DBDATE:
                                buffer.SetDate(col.Index, col.InitialValue);
                                DateTime val = col.InitialValue;
                                switch (col.IncrementType)
                                {
                                    case IncrementByType.Day:
                                        val = val.AddDays(col.IncrementBy);
                                        break;
                                    case IncrementByType.Week:
                                        val = val.AddDays(col.IncrementBy * 7);
                                        break;
                                    case IncrementByType.Month:
                                        val = val.AddMonths(col.IncrementBy);
                                        break;
                                    case IncrementByType.Year:
                                        val = val.AddYears(col.IncrementBy);
                                        break;
                                }
                                col.InitialValue = val;
                                continue;
                            case DataType.DT_I1:
                                buffer.SetSByte(col.Index, col.InitialValue);
                                break;
                            case DataType.DT_I2:
                                buffer.SetInt16(col.Index, col.InitialValue);
                                break;
                            case DataType.DT_I4:
                                buffer.SetInt32(col.Index, col.InitialValue);
                                break;
                            case DataType.DT_I8:
                                buffer.SetInt64(col.Index, col.InitialValue);
                                break;
                            case DataType.DT_NUMERIC:
                                buffer.SetDecimal(col.Index, col.InitialValue);
                                break;
                            case DataType.DT_R4:
                            case DataType.DT_R8:
                                buffer.SetDouble(col.Index, col.InitialValue);
                                break;
                            case DataType.DT_UI1:
                                buffer.SetByte(col.Index, col.InitialValue);
                                break;
                            case DataType.DT_UI2:
                                buffer.SetUInt16(col.Index, col.InitialValue);
                                break;
                            case DataType.DT_UI4:
                                buffer.SetUInt32(col.Index, col.InitialValue);
                                break;
                            case DataType.DT_UI8:
                                buffer.SetUInt64(col.Index, col.InitialValue);
                                break;
                            default:
                                throw new Exception("Unsupported Data Type");
                        }

                        col.InitialValue += col.IncrementBy;
                    }
                }
            }
        }

        #endregion

        #region Private internal Methods
        /// <summary>
        /// Gets Initial increment value based on the DataType
        /// </summary>
        /// <param name="eDataType">DataType</param>
        /// <returns>InitialRowNumberValue</returns>
        public static object GetDataTypeInitialValue(DataType eDataType)
        {
            switch (eDataType)
            {
                case DataType.DT_DBDATE:
                    return new DateTime(1900, 1, 1);
                case DataType.DT_I1:
                    return (sbyte)1;
                case DataType.DT_I2:
                    return (Int16)1;
                case DataType.DT_I4:
                    return 1;
                case DataType.DT_I8:
                    return (long)1;
                case DataType.DT_NUMERIC:
                    return (decimal)1;
                case DataType.DT_R4:
                    return (float)1;
                case DataType.DT_R8:
                    return (double)1;
                case DataType.DT_UI1:
                    return (byte)1;
                case DataType.DT_UI2:
                    return (UInt16)1;
                case DataType.DT_UI4:
                    return (UInt32)1;
                case DataType.DT_UI8:
                    return (UInt64)1;
                default:
                    throw new Exception("Unsupported Data Type");
            }
        }


        public static object ParseInitialValue(string value, DataType eDataType, int precision, int scale)
        {
            switch (eDataType)
            {
                case DataType.DT_DBDATE:
                    return DateTime.Parse(value);
                case DataType.DT_I1:
                    return sbyte.Parse(value);
                case DataType.DT_I2:
                    return Int16.Parse(value);
                case DataType.DT_I4:
                    return int.Parse(value);
                case DataType.DT_I8:
                    return long.Parse(value);
                case DataType.DT_NUMERIC:
                    return Math.Round(decimal.Parse(value), scale);
                case DataType.DT_R4:
                    return float.Parse(value);
                case DataType.DT_R8:
                    return double.Parse(value);
                case DataType.DT_UI1:
                    return byte.Parse(value);
                case DataType.DT_UI2:
                    return UInt16.Parse(value);
                case DataType.DT_UI4:
                    return UInt32.Parse(value);
                case DataType.DT_UI8:
                    return UInt64.Parse(value);
                default:
                    throw new Exception("Unsupported Data Type");
            }
        }


        /// <summary>
        /// Gets Default Increment based on data type
        /// </summary>
        /// <param name="eDataType">DataType to get the Increment</param>
        /// <returns>Default Increment</returns>
        public static object GetDefaultDataTypeIncrement(DataType eDataType)
        {
            switch (eDataType)
            {
                case DataType.DT_DBDATE:
                    return "1d";
                case DataType.DT_I1:
                    return (sbyte)1;
                case DataType.DT_I2:
                    return (Int16)1;
                case DataType.DT_I4:
                    return 1;
                case DataType.DT_I8:
                    return (long)1;
                case DataType.DT_NUMERIC:
                    return (decimal)1;
                case DataType.DT_R4:
                    return (float)1;
                case DataType.DT_R8:
                    return (double)1;
                case DataType.DT_UI1:
                    return (byte)1;
                case DataType.DT_UI2:
                    return (UInt16)1;
                case DataType.DT_UI4:
                    return (UInt32)1;
                case DataType.DT_UI8:
                    return (UInt64)1;
                default:
                    throw new Exception("Unsupported Data Type");
            }
        }

        /// <summary>
        /// Sets RowNumber default properties
        /// </summary>
        /// <param name="rowNumberColumn">RowNumber column which properties should be set</param>
        public static void SetRowNumberColumnProperties(IDTSOutputColumn rowNumberColumn)
        {
            rowNumberColumn.CustomPropertyCollection.RemoveAll();

            rowNumberColumn.SetDataTypeProperties(DataType.DT_I4, 0, 0, 0, 0);

            //Initial Value
            IDTSCustomProperty initialValue = rowNumberColumn.CustomPropertyCollection.New();
            initialValue.Name = Resources.RowNumberInitialValuePropertyName;
            initialValue.Description = Resources.RowNumberInitialValuePropertyDescription;
            initialValue.ContainsID = false;
            initialValue.EncryptionRequired = false;
            initialValue.ExpressionType = DTSCustomPropertyExpressionType.CPET_NOTIFY;
            initialValue.Value = 1;

            //Increment By
            IDTSCustomProperty incrementBy = rowNumberColumn.CustomPropertyCollection.New();
            incrementBy.Name = Resources.RowNumberIncrementByPropertyName;
            incrementBy.Description = Resources.RowNumberIncrementByPropertyDescription;
            incrementBy.ContainsID = false;
            incrementBy.EncryptionRequired = false;
            incrementBy.ExpressionType = DTSCustomPropertyExpressionType.CPET_NOTIFY;
            incrementBy.Value = 1;
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
