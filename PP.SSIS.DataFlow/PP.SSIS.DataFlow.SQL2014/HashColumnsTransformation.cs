// <copyright file="HashColumnsTransformation.cs" company="Pavel Pawlowski">
// Copyright (c) 2012 - 2078 All Right Reserved
//
// THIS CODE AND INFORMATION ARE PROVIDED "AS IS" WITHOUT WARRANTY OF ANY 
// KIND, EITHER EXPRESSED OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE
// IMPLIED WARRANTIES OF MERCHANTABILITY AND/OR FITNESS FOR A
// PARTICULAR PURPOSE.
//
// </copyright>
// <author>Pavel Pawlowski</author>
// <summary>Contains implementaiton of the HashColumnsTransformation</summary>
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
using System.ComponentModel;
using System.Threading;
using PP.SSIS.DataFlow.UI;
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
    [DtsPipelineComponent(DisplayName = "Hash Columns Transformation"
        ,ComponentType = ComponentType.Transform
        ,IconResource = "PP.SSIS.DataFlow.Resources.HashColumns.ico"
        ,CurrentVersion = 8
#if SQL2019
        , UITypeName = "PP.SSIS.DataFlow.UI.HashColumnsTransformationUI, PP.SSIS.DataFlow.SQL2019, Version=1.0.0.0, Culture=neutral, PublicKeyToken=b68691a82d4fb69c"
#endif
#if SQL2017
        , UITypeName = "PP.SSIS.DataFlow.UI.HashColumnsTransformationUI, PP.SSIS.DataFlow.SQL2017, Version=1.0.0.0, Culture=neutral, PublicKeyToken=b68691a82d4fb69c"
#endif
#if SQL2016
        , UITypeName = "PP.SSIS.DataFlow.UI.HashColumnsTransformationUI, PP.SSIS.DataFlow.SQL2016, Version=1.0.0.0, Culture=neutral, PublicKeyToken=b99aee68060fa342"
#endif
#if SQL2014
        , UITypeName = "PP.SSIS.DataFlow.UI.HashColumnsTransformationUI, PP.SSIS.DataFlow, Version=1.0.0.0, Culture=neutral, PublicKeyToken=6926746b040a83a5"
#endif        
#if SQL2012
        ,UITypeName = "PP.SSIS.DataFlow.UI.HashColumnsTransformationUI, PP.SSIS.DataFlow, Version=1.0.0.0, Culture=neutral, PublicKeyToken=c8e167588e3f3397"
#endif        
#if SQL2008
        ,UITypeName = "PP.SSIS.DataFlow.UI.HashColumnsTransformationUI, PP.SSIS.DataFlow, Version=1.0.0.0, Culture=neutral, PublicKeyToken=e17db8c64cd5b65b"
#endif
)
    ]
    public class HashColumnsTransformation : PipelineComponent
    {
        #region Definitions

        #region Enumerations
        public enum HashType
        {
            /// <summary>
            /// No choice has been made
            /// </summary>
            None = 0,
            /// <summary>
            /// Creates an MD5 Hash
            /// </summary>
            MD5 = 1,        //128bits 16 bytes
            /// <summary>
            /// Creates an RipeMD160 Hash
            /// </summary>
            RipeMD160 = 2,      //160 bits 20 bytes
            /// <summary>
            /// Creates a SHA1 Hash
            /// </summary>
            SHA1 = 3,        //160 bits 20 bytes
            /// <summary>
            /// Creates a SHA256 Hash
            /// </summary>
            SHA256 = 4,        //256 bits 32 bytes
            /// <summary>
            /// Creates a SHA384 Hash
            /// </summary>
            SHA384 = 5,       //384 bits 48 bytes
            /// <summary>
            /// Creates a SHA512 Hash
            /// </summary>
            SHA512 = 6,       //512 bits 64 bytes
            /// <summary>
            /// Creates a CRC32 Hash
            /// </summary>
            CRC32 = 7       //32 bits 4 bytes
        }

        /// <summary>
        /// Specifies the Hashing Implementaiont
        /// </summary>
        public enum HashImplementationType
        {
            /// <summary>
            /// Original backward compatible implementation
            /// </summary>
            OriginalBinary = 0,
            /// <summary>
            /// New Binary Safe IMplementation
            /// </summary>
            BinarySafe = 1,
            /// <summary>
            /// THe column values are passed as delimited string. Uses Null Replacement 
            /// </summary>
            UnicodeStringDelmited = 2,
            /// <summary>
            /// Column values are passed as delimited string with null indicator prior each column
            /// </summary>
            UnicodeStringDelimitedNullSafe = 3,
            /// <summary>
            /// Column values are pased as delimited string with null indicator prior each column and data length prior each string and binary column
            /// </summary>
            UnicodeStringDelmitedSafe = 4
        }

        /// <summary>
        /// Specifies the Trimming of String during Hashing
        /// </summary>
        public enum StringTrimming
        {
            /// <summary>
            /// No trimming will be performed
            /// </summary>
            None    = 0,
            /// <summary>
            /// Trailing whitespace characters will be trimmed
            /// </summary>
            Right   = 1,
            /// <summary>
            /// Leading whitespace characters will be trimmed
            /// </summary>
            Left    = 2,
            /// <summary>
            /// Both Leading and Trailing whitespace characters will be trimmed
            /// </summary>
            Full    = 3
        }

        /// <summary>
        /// Specifies the data type of the hash column
        /// </summary>
        public enum HashOutputDataType
        {
            /// <summary>
            /// Output as Binary
            /// </summary>
            DT_BYTES = DataType.DT_BYTES,
            /// <summary>
            /// Output as Unicode String
            /// </summary>
            DT_WSTR = DataType.DT_WSTR,
            /// <summary>
            /// Output as String
            /// </summary>
            DT_STR = DataType.DT_STR,
        }

        [Flags]
        public enum HashOuputColumnProperties
        {
            None = 0x000,
            HashAlgorithm = 0x001,
            HashColumns = 0x002,
            HashImplementationType = 0x004,
            HashFieldSeparator = 0x010,
            HashNullReplacement = 0x020,
            TrimStrings = 0x040,

            All = HashAlgorithm | HashColumns | HashImplementationType | HashFieldSeparator | HashNullReplacement | TrimStrings
        }

        /// <summary>
        /// Identifies type of parallel processing
        /// </summary>
        public enum ParallelProcessing
        {
            /// <summary>
            /// Parallel processing disabled
            /// </summary>
            Off = 0,
            /// <summary>
            /// Automatic parallel mode when there is more than 5 output columns
            /// </summary>
            Auto = 1,
            /// <summary>
            /// Parallel processing enabled
            /// </summary>
            On = 2
        }
        #endregion


        /// <summary>
        /// Rrepresent Buffer Column Information for internal usage.
        /// </summary>
        public class InputBufferColumnInfo
        {
            /// <summary>
            /// Creates instance of BufferColumnInfo
            /// </summary>
            /// <param name="index">Index of the Input Buffer Column</param>
            /// <param name="name">Name of the Input Buffer Column</param>
            /// <param name="id">ID of the Input Buffer Column</param>
            /// <param name="lineageID">LineageID of the Input Buffer Column</param>
            public InputBufferColumnInfo(int index, string name, int id, int lineageID, int sortOrder)
            {
                Index = index;
                Name = name;
                ID = id;
                LineageID = lineageID;
                SortOrder = sortOrder;
            }

            /// <summary>
            /// Index of the Input Buffer Column
            /// </summary>
            public int Index;
            /// <summary>
            /// name of the Input Buffer Column
            /// </summary>
            public string Name;
            /// <summary>
            /// ID Of the Input Buffer Column
            /// </summary>
            public int ID;
            /// <summary>
            /// LineageID of the Input Buffer Column
            /// </summary>
            public int LineageID;
            /// <summary>
            /// Sort Order of the InputColumn
            /// </summary>
            public int SortOrder;
        }

        /// <summary>
        /// Represents Output Hash Column Infromation for internal processing
        /// </summary>
        public class HashColumnInfo
        {
            public HashColumnInfo() : this(0, null, HashType.MD5, null, HashImplementationType.OriginalBinary, DataType.DT_BYTES, HashColumnsTransformationHelper.DefaultFieldtSeparator, HashColumnsTransformationHelper.DefaultNullReplacement, StringTrimming.None)
            {

            }

            /// <summary>
            /// Creates instance of HashColumnInfo
            /// </summary>
            /// <param name="index">Output Column Index of the HashColumn</param>
            /// <param name="name">Output Column Name of the HashColumn</param>
            /// <param name="hashType">HashType of the HashColumn/param>
            /// <param name="hashAlgorithm">Hash Algorithm to be used for HashColumn</param>
            public HashColumnInfo(int index, string name, HashType hashType, HashAlgorithm hashAlgorithm, HashImplementationType implementationType, DataType dataType, string delimiter, string nullReplacement, StringTrimming trimming)
            {
                Index = index;
                Name = name;
                HashType = hashType;
                HashAlgorithm = hashAlgorithm;
                HashInputColumns = new List<int>();
                HashImplmentationType = implementationType;
                OutputDataType = dataType;
                HashFieldsDelimiter = delimiter;
                NullReplacement = nullReplacement;
                StringTrimming = trimming;
            }

            /// <summary>
            /// Output Column Index of the HashColumn
            /// </summary>
            public int Index;
            /// <summary>
            /// Output Column Name of the HashColumn
            /// </summary>
            public string Name;
            /// <summary>
            /// HashType fo the HashColumn
            /// </summary>
            public HashType HashType;
            /// <summary>
            /// Hash Algorithm to be used for hashColumn
            /// </summary>
            public HashAlgorithm HashAlgorithm;
            /// <summary>
            /// Index pointers to InputBufferColumns to construct HASH
            /// </summary>
            public List<int> HashInputColumns;
            public DataType OutputDataType;
            public HashImplementationType HashImplmentationType;
            public string HashFieldsDelimiter;
            public string NullReplacement;
            public StringTrimming StringTrimming;                   
        }

        /// <summary>
        /// Indiciates whether input columns are valid and whetyher it is necessary to reinitialize them
        /// </summary>
        private bool inputColumnsValid = true;
        
        /// <summary>
        /// Input Buffer Columns used for Hash Generation
        /// </summary>
        List<InputBufferColumnInfo> inputBufferColumns;
        /// <summary>
        /// List containing Output Hash Columns
        /// </summary>
        private List<HashColumnInfo> hashColumns;

        /// <summary>
        /// Internal Memory Stream for storing bytes
        /// </summary>
        private HashMemoryBuffers[] memoryBuffers;

        private long bufferLoops = 0; //Number of buffers processed for Garbage Collection;
        private const long MaxBufferLoopsForGC = 100; //Number of buffer loops after which the GC.Collect wil be called
        private long rowsProcessed;
        private int numOfThreads;
        private ParallelProcessing processParallel = ParallelProcessing.Off;
        ManualResetEvent[] threadReset;       

        #endregion

        #region Designtime
        /// <summary>
        /// Provides SSIS Component Properties
        /// </summary>
        public override void ProvideComponentProperties()
        {
            this.ComponentMetaData.Version = PipelineUtils.GetPipelineComponentVersion(this);

            this.ComponentMetaData.Name = Resources.HashColumnsTransformationName;
            this.ComponentMetaData.Description = Resources.HashColumnsTransformationDescription;
            this.ComponentMetaData.ContactInfo = Resources.TransformationContactInfo;

            // Reset the component.
            base.RemoveAllInputsOutputsAndCustomProperties();

            //Call base method to add input and output
            base.ProvideComponentProperties();

            //Set Input properties
            IDTSInput input = ComponentMetaData.InputCollection[0];
            input.Name = Resources.HashColumnsInputName;

            //Set Output properties
            IDTSOutput output = ComponentMetaData.OutputCollection[0];
            output.Name = Resources.HashColumnsOutputName;
            output.SynchronousInputID = input.ID;

            HashColumnsTransformationHelper.AddComponentCustomProperties(ComponentMetaData);
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
            //Check number of Outputs
            if (ComponentMetaData.OutputCollection.Count != 1)
            {
                FireComponentMetadataError(0, Resources.ErrorIncorrectNumberOfOutputs);
                return DTSValidationStatus.VS_NEEDSNEWMETADATA;
            }



            IDTSInput input = ComponentMetaData.InputCollection[0];
            IDTSVirtualInput vInput = input.GetVirtualInput();
            bool missingError = false;

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
            bool hashColumnExists = false;
            List<int> missingLineages = new List<int>();
            List<int> missingInputLineages = new List<int>();

            foreach (IDTSOutputColumn outputColumn in output.OutputColumnCollection)
            {
                bool isRendundant = true;
                bool isInputColumn = false;

                //Check if output column is mapped input column
                foreach (IDTSInputColumn inputColumn in input.InputColumnCollection)
                {
                    if (inputColumn.Name == outputColumn.Name)
                    {
                        isRendundant = false;
                        isInputColumn = true;
                        break;
                    }
                }
                
                //Check if it is HashColumn
                if (!isInputColumn)
                {
                    HashOuputColumnProperties missingProps = HashOuputColumnProperties.All;
                    List<int> lineages = null;
                    HashType hashType = HashType.None;
                    List<int> inputColumnsLineages = new List<int>(input.InputColumnCollection.Count);

                    foreach (IDTSCustomProperty prop in outputColumn.CustomPropertyCollection)
                    {                        

                        if (prop.Name == Resources.HashAlgorithmPropertyName) // "HashAlgorithm")
                        {
                            isRendundant = false;
                            hashColumnExists = true;
                            missingProps ^= HashOuputColumnProperties.HashAlgorithm;


                            //Validate HashType
                            if (!Enum.IsDefined(typeof(HashType), prop.Value))
                            {
                                FireComponentMetadataError(0, string.Format(Resources.ErrorInvalidHashType, outputColumn.Name));
                                return DTSValidationStatus.VS_NEEDSNEWMETADATA;
                            }
                            else
                            {
                                hashType = (HashType)prop.Value;
                                int dataLength;

                                HashColumnsTransformationHelper.GetHashTypeDataType(hashType, outputColumn.DataType, out dataLength);

                                if (outputColumn.Length != dataLength)
                                {
                                    FireComponentMetadataError(0, string.Format(Resources.ErrorInvalidDataType, outputColumn.Name));
                                    return DTSValidationStatus.VS_NEEDSNEWMETADATA;
                                }
                            }

                        }
                        else if (prop.Name == Resources.HashColumnHashInputColumnsPropertyName)
                        {
                            missingProps ^= HashOuputColumnProperties.HashColumns;

                            lineages = InputColumns.ParseInputLineages(prop.Value.ToString());
                            inputColumnsLineages.AddRange(lineages);

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
                        else if (prop.Name == Resources.HashColumnHashImplementationTypePropertyName)
                            missingProps ^= HashOuputColumnProperties.HashImplementationType;
                        else if (prop.Name == Resources.HashColumnHashFieldSeparatorPropertyName) // "HashFieldSeparator" )
                            missingProps ^= HashOuputColumnProperties.HashFieldSeparator;
                        else if (prop.Name == Resources.HashColumnNullReplacementValue)
                            missingProps ^= HashOuputColumnProperties.HashNullReplacement;
                        else if (prop.Name == Resources.HashColumnStringTrimmingPropertyName)
                            missingProps ^= HashOuputColumnProperties.TrimStrings;
                    }

                    if (missingProps != HashOuputColumnProperties.None)
                    {
                        HashColumnsTransformationHelper.SetHashColumnProperties(missingProps, outputColumn);
                    }

                    if (lineages.Count == 0 && (inputColumnsLineages == null || inputColumnsLineages.Count == 0))
                        FireComponentMetadataWarning(0, string.Format("No input columns are specified for output column [{0}]. Hash will not be computed for that column", outputColumn.Name));
                    if (hashType == HashType.None)
                        FireComponentMetadataWarning(0, string.Format("Hash Algorithm for column [{0}] is set to None. Hash will not be computed for that column", outputColumn.Name));
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

            if (missingInputLineages.Count >0)
            {
                string ml = string.Join(", ", missingInputLineages.ConvertAll<string>(l => l.ToString()).ToArray());

                FireComponentMetadataError(0, string.Format("Output columns are referencing input column lineages which are not selected as Input columns: {0}", ml));
                missingError = true;                
            }

            if (missingError)
                return DTSValidationStatus.VS_NEEDSNEWMETADATA;

            //remove redundand output columns
            foreach (IDTSOutputColumn100 col in redundantColumns)
            {
                output.OutputColumnCollection.RemoveObjectByID(col.ID);
            }

            if (!hashColumnExists)
            {
                hashColumnExists = true;
                output = ComponentMetaData.OutputCollection[0];
                IDTSOutputColumn hashColumn = output.OutputColumnCollection.New();
                hashColumn.Name = Resources.HashColumnDefaultName;
                hashColumn.Description = Resources.HashColumnDefaultDesccription;

                HashColumnsTransformationHelper.SetHashColumnProperties(HashOuputColumnProperties.All, hashColumn);
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

                List<int> columnsToRemove = new List<int>();

                foreach (IDTSInputColumn col in input.InputColumnCollection)
                {
                    IDTSVirtualInputColumn100 vCol = vInput.VirtualInputColumnCollection.GetVirtualInputColumnByLineageID(col.LineageID);
                    if (vCol == null)
                    {
                        columnsToRemove.Add(col.ID);
                    }
                    else
                    {
                        bool hasSortOrder = false;
                        foreach (IDTSCustomProperty prop in col.CustomPropertyCollection)
                        {
                            if (prop.Name == Resources.InputSortOrderPropertyName)
                            {
                                hasSortOrder = true;
                                break;
                            }
                        }

                        if (!hasSortOrder)
                            columnsToRemove.Add(col.ID);
                    }
                        
                }

                foreach (int id in columnsToRemove)
                {
                    input.InputColumnCollection.RemoveObjectByID(id);
                }

                inputColumnsValid = true;
            }
        }

        public override void PerformUpgrade(int pipelineVersion)
        {
            DtsPipelineComponentAttribute componentAttribute = (DtsPipelineComponentAttribute)Attribute.GetCustomAttribute(this.GetType(), typeof(DtsPipelineComponentAttribute), false);
            int currentVersion = componentAttribute.CurrentVersion;


            IDTSCustomProperty parallel = null;
            try
            {
                parallel = ComponentMetaData.CustomPropertyCollection[Resources.HashTransformationParallelProcessingPropertyName];
            }
            catch
            {
                parallel = ComponentMetaData.CustomPropertyCollection.New();
                parallel.Name = Resources.HashTransformationParallelProcessingPropertyName;
                parallel.Description = "Specifies whether Hash processing for multiple output columsn should run in multiple threads.";
                parallel.ContainsID = false;
                parallel.EncryptionRequired = false;
                parallel.TypeConverter = typeof(ParallelProcessing).AssemblyQualifiedName;
                parallel.Value = ParallelProcessing.Off;
            }




            // Get the attributes for the SSIS Package
            int metadataVersion = ComponentMetaData.Version;

            if (currentVersion < ComponentMetaData.Version)
            {
                throw new Exception(Properties.Resources.ErrorWrongRuntimeVersion);
            }
            else if (currentVersion > ComponentMetaData.Version) //Current implementaiton is newer than previous. Perform Upgrade
            {
                IDTSInput input = ComponentMetaData.InputCollection[0];
                List<int> propsToRemove = new List<int>();
                List<KeyValuePair<int, int>> colLienagesSort = new List<KeyValuePair<int, int>>(input.InputColumnCollection.Count);

                foreach (IDTSInputColumn col in input.InputColumnCollection)
                {
                    int sortOrder = int.MaxValue;
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

                HashType hashType = HashType.MD5;

                try
                {
                    IDTSCustomProperty100 oldProperty =
                      ComponentMetaData.CustomPropertyCollection["HashType"];
                    hashType = (HashType)oldProperty.Value;
                    ComponentMetaData.CustomPropertyCollection.RemoveObjectByIndex("HashType");
                }
                catch
                {
                    // If the old custom property is not available, ignore the error.
                }

                IDTSOutput output = ComponentMetaData.OutputCollection[0];

                foreach (IDTSOutputColumn col in output.OutputColumnCollection)
                {

                    IDTSCustomProperty hashTypeProp = null;
                    HashOuputColumnProperties missingProps = HashOuputColumnProperties.All;
                    HashImplementationType implementationType = HashImplementationType.OriginalBinary;
                    HashOutputDataType outputDataType = HashOutputDataType.DT_BYTES;
                    propsToRemove = new List<int>();

                    foreach (IDTSCustomProperty prop in col.CustomPropertyCollection)
                    {
                        if (prop.Name == Resources.HashAlgorithmPropertyName)
                        {
                            hashTypeProp = prop;
                            missingProps ^= HashOuputColumnProperties.HashAlgorithm;
                        }
                        else if (prop.Name == Resources.HashColumnHashInputColumnsPropertyName)
                        {
                            missingProps ^= HashOuputColumnProperties.HashColumns;
                        }
                        else if (prop.Name == Resources.HashColumnHashImplementationTypePropertyName)
                        {
                            missingProps ^= HashOuputColumnProperties.HashImplementationType;
                            implementationType = (HashImplementationType)prop.Value;
                        }
                        else if (prop.Name == Resources.HashColumnHashOutputDataTypePropertyName)
                        {
                            propsToRemove.Add(prop.ID);
                        }
                        else if (prop.Name == Resources.HashColumnHashFieldSeparatorPropertyName)
                        {
                            missingProps ^= HashOuputColumnProperties.HashFieldSeparator;
                        }
                        else if (prop.Name == Resources.HashColumnNullReplacementValue)
                        {
                            missingProps ^= HashOuputColumnProperties.HashNullReplacement;
                        }
                        else if (prop.Name == Resources.HashColumnStringTrimmingPropertyName)
                        {
                            missingProps ^= HashOuputColumnProperties.TrimStrings;
                        }
                    }

                    foreach (int id in propsToRemove)
                    {
                        col.CustomPropertyCollection.RemoveObjectByID(id);
                    }


                    if (hashTypeProp != null && missingProps != HashOuputColumnProperties.None)
                    {
                        HashColumnsTransformationHelper.SetHashColumnProperties(missingProps, col, hashType, implementationType, outputDataType, ComponentMetaData.LocaleID);
                    }

                    if ((missingProps & HashOuputColumnProperties.HashColumns) == HashOuputColumnProperties.HashColumns)
                    {
                        string lineages = InputColumns.BuildInputLineagesString(colLienagesSort.ConvertAll<int>(kvp => kvp.Key));
                        col.CustomPropertyCollection[Resources.HashColumnHashInputColumnsPropertyName].Value = lineages;
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
            IDTSInput input = ComponentMetaData.InputCollection.GetObjectByID(inputID);

            DTSUsageType uType = usageType == DTSUsageType.UT_READWRITE ? DTSUsageType.UT_READONLY : usageType;

            IDTSInputColumn col = base.SetUsageType(inputID, virtualInput, lineageID, uType);

            return col;
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
            IDTSOutput output = ComponentMetaData.OutputCollection.FindObjectByID(outputID);
            IDTSOutputColumn hashColumn = output.OutputColumnCollection.NewAt(outputColumnIndex);
            hashColumn.Name = name;
            hashColumn.Description = description;

            HashColumnsTransformationHelper.SetHashColumnProperties(HashOuputColumnProperties.All, hashColumn);

            return hashColumn;
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
            bool error = false;
            IDTSOutput output = ComponentMetaData.OutputCollection.GetObjectByID(outputID);
            IDTSOutputColumn outputColumn = output.OutputColumnCollection.FindObjectByID(outputColumnID);
            HashOutputDataType outputDataType = HashOutputDataType.DT_BYTES;
            HashImplementationType implType = HashImplementationType.OriginalBinary;

            //Try to get the OutputDataType;
            try
            {
                var p = outputColumn.CustomPropertyCollection[Resources.HashColumnHashOutputDataTypePropertyName];
                if (p != null)
                    outputDataType = (HashOutputDataType)p.Value;

                var pImp = outputColumn.CustomPropertyCollection[Resources.HashColumnHashImplementationTypePropertyName];
                if (pImp != null)
                    implType = (HashImplementationType)p.Value;
            }
            catch
            {
            }

            //in case of HasshType property update, update output column data type
            if (propertyName == Resources.HashAlgorithmPropertyName)
            {
                HashColumnsTransformationHelper.SetHashColumnDataType((HashType)propertyValue, outputColumn, ComponentMetaData.LocaleID);
            }
            else if (propertyName == Resources.HashColumnHashInputColumnsPropertyName)
            {
                if (propertyValue != null && propertyValue.ToString() != string.Empty)
                {
                    var lineages = InputColumns.ParseInputLineages(propertyValue.ToString());
                    IDTSInput input = ComponentMetaData.InputCollection[0];
                    IDTSVirtualInput vInput = input.GetVirtualInput();
                    foreach (IDTSVirtualInputColumn vcol in vInput.VirtualInputColumnCollection)
                    {
                        DTSUsageType usage = lineages.Remove(vcol.LineageID) ? DTSUsageType.UT_READONLY : DTSUsageType.UT_IGNORED;

                        IDTSInputColumn icol = null;
                                                
                        try
                        {
                            icol = input.InputColumnCollection.GetInputColumnByLineageID(vcol.LineageID);
                        }
                        catch { }
                        if ((icol == null && usage == DTSUsageType.UT_READONLY) || (icol != null && usage == DTSUsageType.UT_IGNORED))
                        {
                            SetUsageType(input.ID, vInput, vcol.LineageID, usage);
                        }
                    }

                    if (lineages.Count > 0)
                    {
                        string wrongLineages = string.Join(", ", lineages.ConvertAll<string>(l => l.ToString()).ToArray());

                        throw new Exception(string.Format("Folowing inputcolumn lineages were not found: {0}", wrongLineages));
                    }
                }
            }
            else if (propertyName == Resources.HashColumnHashImplementationTypePropertyName)
            {
            }
            else if (propertyName == Resources.HashColumnHashFieldSeparatorPropertyName)
            {
            }
            else if (propertyName == Resources.HashColumnNullReplacementValue)
            {
            }
            else if (propertyName == "CodePage" && outputDataType == HashOutputDataType.DT_STR)
            {
            }
            else
            {
                error = true;   
            }

            if (error)
                throw new Exception(string.Format(Resources.ErrorOutputColumnPropertyCannotBeChanged, propertyName));
            else
                return base.SetOutputColumnProperty(outputID, outputColumnID, propertyName, propertyValue);
        }

        public override void SetOutputColumnDataTypeProperties(int iOutputID, int iOutputColumnID, DataType eDataType, int iLength, int iPrecision, int iScale, int iCodePage)
        {
            IDTSOutput output = ComponentMetaData.OutputCollection.GetObjectByID(iOutputID);
            IDTSOutputColumn ocol = output.OutputColumnCollection.GetObjectByID(iOutputColumnID);

            HashOutputDataType outputDataType = HashOutputDataType.DT_BYTES;
            //Try to get the OutputDataType;
            try
            {
                var p = ocol.CustomPropertyCollection[Resources.HashColumnHashOutputDataTypePropertyName];
                if (p != null)
                {
                    outputDataType = (HashOutputDataType)p.Value;
                }
            }
            catch
            {
            }

            if (outputDataType == HashOutputDataType.DT_STR)
            {
                if (eDataType == ocol.DataType && iLength == ocol.Length && iPrecision == ocol.Precision && iScale == ocol.Scale)
                {
                    ocol.SetDataTypeProperties(eDataType, iLength, iPrecision, iScale, iCodePage);
                    return;
                }
            }

            base.SetOutputColumnDataTypeProperties(iOutputID, iOutputColumnID, eDataType, iLength, iPrecision, iScale, iCodePage);
        }

        public override IDTSCustomProperty SetInputColumnProperty(int inputID, int inputColumnID, string propertyName, object propertyValue)
        {
            if (propertyName == Resources.InputSortOrderPropertyName)
            {
                int val = Convert.ToInt32(propertyValue);
                IDTSInput input = ComponentMetaData.InputCollection.GetObjectByID(inputID);

                HashColumnsTransformationHelper.UpdateSortOrder(input, inputColumnID, val);

                var prop = base.SetInputColumnProperty(inputID, inputColumnID, propertyName, propertyValue);
                

                return prop;
            }

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

            hashColumns = new List<HashColumnInfo>();

            IDTSInput input = ComponentMetaData.InputCollection[0];
            IDTSOutput output = ComponentMetaData.OutputCollection[0];

            inputBufferColumns = new List<InputBufferColumnInfo>(input.InputColumnCollection.Count);

            //Get whethe we should process output columns in parallel            
            try
            {
                IDTSCustomProperty parallel = ComponentMetaData.CustomPropertyCollection[Resources.HashTransformationParallelProcessingPropertyName];
                processParallel = (ParallelProcessing)parallel.Value;
            }
            catch
            {
                processParallel = ParallelProcessing.Off;
            }


            switch (processParallel)
            {
                case ParallelProcessing.Auto:
                    numOfThreads = Math.Max(1, HashColumnsTransformationHelper.GetNumberOfProcessorCores() - 1);
                    break;
                case ParallelProcessing.On:
                    numOfThreads = Math.Max(1, HashColumnsTransformationHelper.GetNumberOfProcessorCores());
                    break;
                default:
                    numOfThreads = 1;
                    break;
            }

            //In Auto Parallelism enable the parallel processing only if there is 6 or more output columns
            int cntOutputCols = output.OutputColumnCollection.Count;
            if (processParallel == ParallelProcessing.Auto && cntOutputCols < 6)
                numOfThreads = 1;

            if (numOfThreads > 1)
            {
                threadReset = new ManualResetEvent[cntOutputCols];
                memoryBuffers = new HashMemoryBuffers[cntOutputCols]; //Define memory streams for parallel processing
            }
            else
            {
                memoryBuffers = new HashMemoryBuffers[1]; //Define single memory stream
            }            

            //Initialize memory streams
            for (int i = 0; i < memoryBuffers.Length; i++)
            {
                memoryBuffers[i] = new HashMemoryBuffers(HashColumnsTransformationHelper.MemoryStreamInitialSize);
            }

            for (int i = 0; i < input.InputColumnCollection.Count; i++)
            {
                IDTSInputColumn column = input.InputColumnCollection[i];

                inputBufferColumns.Add(new InputBufferColumnInfo(BufferManager.FindColumnByLineageID(input.Buffer, column.LineageID),
                    column.Name, column.ID, column.LineageID, 0));
            }

            foreach (IDTSOutputColumn col in output.OutputColumnCollection)
            {
                HashColumnInfo newHashColumn = null;


                foreach (IDTSCustomProperty prop in col.CustomPropertyCollection)
                {
                    if (prop.Name == Resources.HashAlgorithmPropertyName)
                    { 
                        int index = BufferManager.FindColumnByLineageID(input.Buffer, col.LineageID);
                        HashType hashType = (HashType)prop.Value;
                        HashAlgorithm alg = null;

                        //Get hash algorithm based on the hash type
                        switch (hashType)
                        {
                            case HashType.None:
                                break;
                            case HashType.MD5:
                                alg = new MD5CryptoServiceProvider();
                                break;
                            case HashType.RipeMD160:
                                alg = new RIPEMD160Managed();
                                break;
                            case HashType.SHA1:
                                alg = new SHA1CryptoServiceProvider();
                                break;
                            case HashType.SHA256:
                                alg = new SHA256CryptoServiceProvider();
                                break;
                            case HashType.SHA384:
                                alg = new SHA384CryptoServiceProvider();
                                break;
                            case HashType.SHA512:
                                alg = new SHA512CryptoServiceProvider();
                                break;
                            case HashType.CRC32:
                                alg = new Crc32();
                                break;
                        }
                        if (newHashColumn == null)
                            newHashColumn = new HashColumnInfo();

                        newHashColumn.Index = index;
                        newHashColumn.Name = col.Name;
                        newHashColumn.HashType = hashType;
                        newHashColumn.HashAlgorithm = alg;
                        newHashColumn.OutputDataType = col.DataType;
                    }
                    else if (prop.Name == Resources.HashColumnHashInputColumnsPropertyName)
                    {
                        string colsStr = prop.Value.ToString();
                        var colLineages = InputColumns.ParseInputLineages(colsStr);

                        List<int> cols = new List<int>(colLineages.Count);
                       
                        foreach (int lineageID in colLineages)
                        {
                            int idx = inputBufferColumns.FindIndex(ibci => ibci.LineageID == lineageID);
                            cols.Add(idx);
                        }

                        if (newHashColumn == null)
                            newHashColumn = new HashColumnInfo();
                        newHashColumn.HashInputColumns = cols;

                    }
                    else if (prop.Name == Resources.HashColumnHashImplementationTypePropertyName)
                    {
                        if (newHashColumn == null)
                            newHashColumn = new HashColumnInfo();

                        newHashColumn.HashImplmentationType = (HashImplementationType)prop.Value;

                    }
                    else if (prop.Name == Resources.HashColumnHashFieldSeparatorPropertyName)
                    {
                        if (newHashColumn == null)
                            newHashColumn = new HashColumnInfo();

                        newHashColumn.HashFieldsDelimiter = prop.Value.ToString();
                    }
                    else if (prop.Name == Resources.HashColumnNullReplacementValue)
                    {
                        if (newHashColumn == null)
                            newHashColumn = new HashColumnInfo();

                        newHashColumn.NullReplacement = prop.Value.ToString();
                    }
                    else if (prop.Name == Resources.HashColumnStringTrimmingPropertyName)
                    {
                        if (newHashColumn == null)
                            newHashColumn = new HashColumnInfo();
                        newHashColumn.StringTrimming = (StringTrimming)prop.Value;
                    }

                }

                if (newHashColumn != null && newHashColumn.Index != 0)
                {
                    hashColumns.Add(newHashColumn);
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

            //PassThreadState passThreadState;

            if (!buffer.EndOfRowset)
            {
                while (buffer.NextRow())
                {
                    rowsProcessed++;

                    if (hashColumns.Count == 0)
                        continue;

                    //int workerThreads;
                    //int portThreads;
                    //ThreadPool.GetMaxThreads(out workerThreads, out portThreads);
                    //ThreadPool.GetAvailableThreads(out workerThreads, out portThreads);

                    //Calculate hash for each Output Hash Columns
                    if (numOfThreads > 1) //Parallel Processing
                    {
                        HashColumnInfo hCol;
                        for (int i = 0; i < hashColumns.Count; i++)
                        {
                            HashMemoryBuffers mb = memoryBuffers[i];
                            ManualResetEvent resetEvent;
                            hCol = hashColumns[i];

                            if (hCol.HashType == HashType.None || hCol.HashAlgorithm == null || hCol.HashInputColumns.Count == 0)
                            {
                                resetEvent = new ManualResetEvent(true);
                                buffer.SetNull(hCol.Index);
                                continue;
                            }
                            else
                            {
                                resetEvent = new ManualResetEvent(false);
                            }

                            threadReset[i] = resetEvent;

                            //Create State object to pass parametes
                            HashThreadState state = new HashThreadState(hCol, inputBufferColumns, buffer, mb, resetEvent);
                            //Queue the work item on the ThreadPool
                            ThreadPool.QueueUserWorkItem(new WaitCallback(HashColumnsTransformationHelper.BuildAndCalculateHashParallel), state);
                        }

                        //Wait for all columns calculations - all stete objects has to be set;
                        WaitHandle.WaitAll(threadReset);
                    }
                    else //Serial Processing
                    {
                        HashMemoryBuffers mb = memoryBuffers[0];

                        foreach (HashColumnInfo hCol in hashColumns)
                        {
                            if (hCol.HashAlgorithm == null)
                                continue;

                            HashColumnsTransformationHelper.BuildAndCalculateHash(hCol, inputBufferColumns, buffer, mb);
                        }
                    }
                }

                bufferLoops++;
                if (bufferLoops >= MaxBufferLoopsForGC)
                {
                    bufferLoops = 0;
                    GC.Collect();
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

        #region Support Methods
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

        #endregion
    }
}
