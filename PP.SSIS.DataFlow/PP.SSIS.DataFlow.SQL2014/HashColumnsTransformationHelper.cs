// <copyright file="HashColumnsTransformation.cs" company="Pavel Pawlowski">
// Copyright (c) 2014-2017 All Right Reserved
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

#if SQL2008 || SQL2008R2 || SQL2012 || SQL2014 || SQL2016 || SQL2017
using IDTSOutput = Microsoft.SqlServer.Dts.Pipeline.Wrapper.IDTSOutput100;
using IDTSCustomProperty = Microsoft.SqlServer.Dts.Pipeline.Wrapper.IDTSCustomProperty100;
using IDTSOutputColumn = Microsoft.SqlServer.Dts.Pipeline.Wrapper.IDTSOutputColumn100;
using IDTSInput = Microsoft.SqlServer.Dts.Pipeline.Wrapper.IDTSInput100;
using IDTSInputColumn = Microsoft.SqlServer.Dts.Pipeline.Wrapper.IDTSInputColumn100;
using IDTSVirtualInput = Microsoft.SqlServer.Dts.Pipeline.Wrapper.IDTSVirtualInput100;
using IDTSVirtualInputColumn = Microsoft.SqlServer.Dts.Pipeline.Wrapper.IDTSVirtualInputColumn100;
#endif

namespace PP.SSIS.DataFlow
{
    public class HashColumnsTransformationHelper
    {
        public static readonly string DefaultFieldtSeparator = "|";
        public static readonly string DefaultNullReplacement = "~NULL~";
        

        /// <summary>
        /// Specifies the inital size of the memory stream
        /// </summary>
        public static readonly int MemoryStreamInitialSize = 524288; //512 KB
        /// <summary>
        /// Specifies the treshold when the memory Stream for building the hash data should be shirnked to initial value
        /// </summary>
        public static readonly int MemoryStreamShringTreshod = 4194304; //4 MB

        #region Runtime Helper Methods

        /// <summary>
        /// Invocation method for parallel hash columsn processing
        /// </summary>
        /// <param name="stateObject">State object with parameters needed for hashing</param>
        public static void BuildAndCalculateHashParallel(object stateObject)
        {
            HashThreadState state = stateObject as HashThreadState;
            if (state != null)
            {
                try
                {
                    //Calculate the hash
                    BuildAndCalculateHash(state.HashColumnInfo, state.InputBufferColumns, state.PipelineBuffer, state.MemoryBuffers);
                }
                finally
                {
                    //Set the reset event to nofity the hash calculation has completed for the column
                    state.ResetEvent.Set();
                }
            }
        }

        public static void BuildAndCalculateHash(HashColumnsTransformation.HashColumnInfo hCol, List<HashColumnsTransformation.InputBufferColumnInfo> inputBufferColumns, PipelineBuffer buffer, HashMemoryBuffers mb)
        {
            //Set length of the Stream to 2.
            //We are settiong the length to 2 tyo keep the two unicode identification bytes on the beginning as StreamWriter writes the bytes only once
            mb.MemoryStream.SetLength(2);    

            //Write the input columns to Memory Stream
            HashColumnsTransformationHelper.BuildHashMemoryStream(hCol, inputBufferColumns, buffer, mb);

            //Calculate the Hash ans store it into Output columns
            HashColumnsTransformationHelper.CalculateHashAndStoreValue(hCol, mb, buffer);
        }


        /// <summary>
        /// Calculates hash of Memaory Stream ands based ont he HashColumnInformation and stores the calculated hash into the pipeline output column
        /// </summary>
        /// <param name="hCol">HashColumnInfo for hash calculation</param>
        /// <param name="ms">Memory stream to calculate the hash</param>
        /// <param name="buffer">bufefr to store the calculated hash</param>
        public static void CalculateHashAndStoreValue(HashColumnsTransformation.HashColumnInfo hCol, HashMemoryBuffers mb, PipelineBuffer buffer)
        {
            byte[] hash;
            MemoryStream ms = mb.MemoryStream;

            ms.Position = 2; //Set Position to 0 prior computing hash to move right after the unicode characters to not include them in the hash calculation
                             //Caculate Hash
            hash = hCol.HashAlgorithm.ComputeHash(ms);

            //Store the Hash into the Output HashColumn
            if (hCol.OutputDataType == DataType.DT_BYTES)
            {
                buffer.SetBytes(hCol.Index, hash);
            }
            else
            {
                string hashStr = BitConverter.ToString(hash).Replace("-", string.Empty);
                buffer.SetString(hCol.Index, hashStr);
            }

            if (ms.Capacity > MemoryStreamShringTreshod)
            {
                ms.Position = 0;
                ms.SetLength(0);
                ms.Capacity = MemoryStreamInitialSize;
            }
        }

        /// <summary>
        /// Builds Memory Stream data for Hashing from Input Columns
        /// </summary>
        /// <param name="hCol">HashColumnInfo</param>
        /// <param name="inputBufferColumns">InputBuferColumnInfo</param>
        /// <param name="buffer">input Buffer</param>
        /// <param name="mb">Memory Buffers</param>
        public static void BuildHashMemoryStream(HashColumnsTransformation.HashColumnInfo hCol, List<HashColumnsTransformation.InputBufferColumnInfo> inputBufferColumns, PipelineBuffer buffer, HashMemoryBuffers mb)
        {
            MemoryStream ms = mb.MemoryStream;
            StreamWriter sw = mb.StreamWriter;

            for (int i = 0; i < hCol.HashInputColumns.Count; i++)
            {
                int colIdx = hCol.HashInputColumns[i];
                HashColumnsTransformation.InputBufferColumnInfo bci = inputBufferColumns[colIdx];

                switch (hCol.HashImplmentationType)
                {
                    case HashColumnsTransformation.HashImplementationType.BinarySafe:
                        WriteColumnToStreamBinary(hCol, bci, buffer, mb, sw);
                        break;
                    case HashColumnsTransformation.HashImplementationType.UnicodeStringDelmited:
                        WriteColumnToStreamUnicodeDelimited(i, hCol, bci, buffer, mb);
                        break;
                    default:
                        WriteColumnToStreamOriginal(bci, buffer, mb);
                        break;
                }
            }
        }


        private static string TrimString(HashColumnsTransformation.HashColumnInfo hcol, string str)
        {
            switch (hcol.StringTrimming)
            {
                case HashColumnsTransformation.StringTrimming.None:
                    return str;
                case HashColumnsTransformation.StringTrimming.Right:
                    return str.TrimEnd();
                case HashColumnsTransformation.StringTrimming.Left:
                    return str.TrimStart();
                case HashColumnsTransformation.StringTrimming.Full:
                    return str.Trim();
                default:
                    return str;
            }
        }


        private static void WriteColumnToStreamBinary(HashColumnsTransformation.HashColumnInfo hCol, HashColumnsTransformation.InputBufferColumnInfo bci, PipelineBuffer buffer, HashMemoryBuffers mb, StreamWriter sw)
        {
            int ci = bci.Index;
            byte[] bdata = null;
            byte[] decimalArray = new byte[16]; //Array for storing decimal numbers
            string asciiStr = null;

            BinaryWriter bw = mb.BinaryWriter;

            BufferColumn col = buffer.GetColumnInfo(bci.Index);

            bw.Write((byte)0);//Write byte (0) as start of field;

            if (buffer.IsNull(bci.Index))
            {
                bw.Write((byte)1); //Write 1 representing NULL
                bw.Write(0); //write length of 0 for NULL
            }
            else
            {
                bw.Write((byte)0); //write 0 representing NOT NULL

                //Get buffer data
                lock (bci)
                {
                    switch (col.DataType)
                    {
                        case DataType.DT_BOOL:
                            bdata = BitConverter.GetBytes(buffer.GetBoolean(ci));
                            break;
                        case DataType.DT_BYTES:
                            bdata = buffer.GetBytes(ci);
                            break;
                        case DataType.DT_IMAGE:
                            bdata = buffer.GetBlobData(ci, 0, (int)buffer.GetBlobLength(ci));
                            break;
                        case DataType.DT_CY:
                        case DataType.DT_DECIMAL:
                        case DataType.DT_NUMERIC:
                            bdata = Encoding.ASCII.GetBytes(buffer.GetDecimal(ci).ToString(CultureInfo.InvariantCulture));
                            break;
                        case DataType.DT_DATE:
                        case DataType.DT_DBTIMESTAMP:
                        case DataType.DT_DBTIMESTAMP2:
                            bw.Write(buffer.GetDateTime(ci).ToBinary());
                            break;
                        case DataType.DT_FILETIME:
                            bw.Write(buffer.GetInt64(ci));
                            break;
                        case DataType.DT_DBDATE:
                            bw.Write(buffer.GetDate(ci).ToBinary());
                            break;
                        case DataType.DT_DBTIME:
                        case DataType.DT_DBTIME2:
                            bw.Write(buffer.GetTime(ci).Ticks);
                            break;
                        case DataType.DT_DBTIMESTAMPOFFSET:
                            var dtoffset = buffer.GetDateTimeOffset(ci);
                            BitConverter.GetBytes(dtoffset.DateTime.ToBinary()).CopyTo(decimalArray, 0);
                            BitConverter.GetBytes(dtoffset.Offset.Ticks).CopyTo(decimalArray, 8);
                            bw.Write(decimalArray);
                            break;
                        case DataType.DT_EMPTY:
                        case DataType.DT_NULL:
                            bdata = new byte[0];
                            break;
                        case DataType.DT_GUID:
                            bw.Write(Encoding.ASCII.GetBytes(buffer.GetGuid(ci).ToString()));
                            break;
                        case DataType.DT_I1:
                            asciiStr = buffer.GetSByte(ci).ToString(CultureInfo.InvariantCulture);
                            break;
                        case DataType.DT_I2:
                            asciiStr = buffer.GetInt16(ci).ToString(CultureInfo.InvariantCulture);
                            break;
                        case DataType.DT_I4:
                            asciiStr = buffer.GetInt32(ci).ToString(CultureInfo.InvariantCulture);
                            break;
                        case DataType.DT_I8:
                            asciiStr = buffer.GetInt64(ci).ToString(CultureInfo.InvariantCulture);
                            break;
                        case DataType.DT_R4:
                            asciiStr = buffer.GetSingle(ci).ToString(CultureInfo.InvariantCulture);
                            break;
                        case DataType.DT_R8:
                            asciiStr = buffer.GetDouble(ci).ToString(CultureInfo.InvariantCulture);
                            break;
                        case DataType.DT_UI1:
                            asciiStr = buffer.GetByte(ci).ToString(CultureInfo.InvariantCulture);
                            break;
                        case DataType.DT_UI2:
                            asciiStr = buffer.GetUInt16(ci).ToString(CultureInfo.InvariantCulture);
                            break;
                        case DataType.DT_UI4:
                            asciiStr = buffer.GetUInt32(ci).ToString(CultureInfo.InvariantCulture);
                            break;
                        case DataType.DT_UI8:
                            asciiStr = buffer.GetUInt64(ci).ToString(CultureInfo.InvariantCulture);
                            break;
                        case DataType.DT_NTEXT:
                        case DataType.DT_TEXT:
                        case DataType.DT_STR:
                        case DataType.DT_WSTR:
                            bdata = Encoding.Unicode.GetBytes(TrimString(hCol, buffer.GetString(ci)));
                            break;
                        default:
                            bdata = new byte[0];
                            break;
                    }
                }

                if (asciiStr != null)
                    bdata = Encoding.ASCII.GetBytes(asciiStr);

                if (bdata != null)
                {
                    bw.Write(bdata.Length); //write length of buffer
                    bw.Write(bdata);    //write bufferdata;                                    
                }
            }
        }

        private static void WriteColumnToStreamUnicodeDelimited(int columnPosition, HashColumnsTransformation.HashColumnInfo hCol, HashColumnsTransformation.InputBufferColumnInfo bci, PipelineBuffer buffer, HashMemoryBuffers mb)
        {
            int ci = bci.Index;
            byte[] bdata = null;
            string strData = null;
            bool writeLen = false;
            bool isNull;
            bool trim = false;

            StreamWriter sw = mb.StreamWriter;

            BufferColumn col = buffer.GetColumnInfo(bci.Index);

            isNull = buffer.IsNull(ci);

            //When not first field, write field delimiter
            if (columnPosition > 0)
                sw.Write(hCol.HashFieldsDelimiter);

            if (isNull)
            {
                if (hCol.HashImplmentationType == HashColumnsTransformation.HashImplementationType.UnicodeStringDelimitedNullSafe ||
                    hCol.HashImplmentationType == HashColumnsTransformation.HashImplementationType.UnicodeStringDelmitedSafe)
                { //Safe handling: write 1 indicating null and field delimiter.
                    sw.Write(1);
                    sw.Write(hCol.HashFieldsDelimiter);
                }
                else //Non Safe handling = write replacement value
                {
                    sw.Write(hCol.NullReplacement);
                }
                sw.Flush();
                return; //return. no need for other processing as null value is stored in the field.
            }
            else if (hCol.HashImplmentationType == HashColumnsTransformation.HashImplementationType.UnicodeStringDelimitedNullSafe || 
                hCol.HashImplmentationType == HashColumnsTransformation.HashImplementationType.UnicodeStringDelmitedSafe)
            { //Saefe handling: write 0 indicating non null value and field delimiter. Field value will be after the delimiter
                sw.Write(0);
                sw.Write(hCol.HashFieldsDelimiter);
            }

            //Get buffer data
            lock (bci)
            {
                switch (col.DataType)
                {
                    case DataType.DT_BOOL:
                        sw.Write(buffer.GetBoolean(ci) ? 1 : 0);
                        break;
                    case DataType.DT_BYTES:
                        bdata = buffer.GetBytes(ci);
                        break;
                    case DataType.DT_IMAGE:
                        bdata = buffer.GetBlobData(ci, 0, (int)buffer.GetBlobLength(ci));
                        break;
                    case DataType.DT_CY:
                    case DataType.DT_DECIMAL:
                    case DataType.DT_NUMERIC:
                        strData = buffer.GetDecimal(ci).ToString(CultureInfo.InvariantCulture);
                        break;
                    case DataType.DT_DATE:
                        strData = buffer.GetDateTime(ci).ToString("yyyy-MM-dd HH");
                        break;
                    case DataType.DT_DBTIMESTAMP:
                        strData = buffer.GetDateTime(ci).ToString("yyyy-MM-dd HH:mm:ss.fff");
                        break;
                    case DataType.DT_DBTIMESTAMP2:
                        strData = buffer.GetDateTime(ci).ToString("yyyy-MM-dd HH:mm:ss.fffffff");
                        break;
                    case DataType.DT_FILETIME:
                        sw.Write(buffer.GetInt64(ci));
                        break;
                    case DataType.DT_DBDATE:
                        strData = buffer.GetDate(ci).ToString("yyyy-MM-dd");
                        break;
#if NET35
                    case DataType.DT_DBTIME:
                        strData = new DateTime(buffer.GetTime(ci).Ticks).ToString("HH:mm:ss");
                        break;
                    case DataType.DT_DBTIME2:
                        strData = new DateTime(buffer.GetTime(ci).Ticks).ToString("HH:mm:ss.fffffff");
                        break;
#else
                    case DataType.DT_DBTIME:
                        strData = buffer.GetTime(ci).ToString("HH:mm:ss");
                        break;
                    case DataType.DT_DBTIME2:
                        strData = buffer.GetTime(ci).ToString("HH:mm:ss.fffffff");
                        break;
#endif
                    case DataType.DT_DBTIMESTAMPOFFSET:
                        strData = buffer.GetDateTimeOffset(ci).ToString("yyyy-MM-dd HH:mm:ss.fffffff zzz");
                        break;
                    case DataType.DT_EMPTY:
                    case DataType.DT_NULL:
                        bdata = new byte[0];
                        break;
                    case DataType.DT_GUID:
                        strData = buffer.GetGuid(ci).ToString();
                        break;
                    case DataType.DT_I1:
                        sw.Write(buffer.GetSByte(ci));
                        break;
                    case DataType.DT_I2:
                        sw.Write(buffer.GetInt16(ci));
                        break;
                    case DataType.DT_I4:
                        sw.Write(buffer.GetInt32(ci));
                        break;
                    case DataType.DT_I8:
                        sw.Write(buffer.GetInt64(ci));
                        break;
                    case DataType.DT_R4:
                        sw.Write(buffer.GetSingle(ci));
                        break;
                    case DataType.DT_R8:
                        sw.Write(buffer.GetDouble(ci));
                        break;
                    case DataType.DT_UI1:
                        sw.Write(buffer.GetByte(ci));
                        break;
                    case DataType.DT_UI2:
                        sw.Write(buffer.GetUInt16(ci));
                        break;
                    case DataType.DT_UI4:
                        sw.Write(buffer.GetUInt32(ci));
                        break;
                    case DataType.DT_UI8:
                        sw.Write(buffer.GetUInt64(ci));
                        break;
                    case DataType.DT_NTEXT:
                    case DataType.DT_TEXT:
                    case DataType.DT_STR:
                    case DataType.DT_WSTR:
                        trim = true;
                        strData = buffer.GetString(ci);
                        if (hCol.HashImplmentationType == HashColumnsTransformation.HashImplementationType.UnicodeStringDelmitedSafe)
                            writeLen = true;
                        break;
                    default:
                        bdata = new byte[0];
                        break;
                }
            }

            if (bdata != null)
                strData = BitConverter.ToString(bdata).Replace("-", "");

            if (strData != null)
            {
                if (trim)
                    strData = TrimString(hCol, strData);

                if (writeLen)
                {
                    sw.Write(strData.Length);
                    sw.Write(hCol.HashFieldsDelimiter);
                }
                sw.Write(strData);
            }

            sw.Flush();
        }

        private static void WriteColumnToStreamOriginal(HashColumnsTransformation.InputBufferColumnInfo bci, PipelineBuffer buffer, HashMemoryBuffers mb)
        {
            int ci = bci.Index;
            byte[] bdata = null;
            byte[] decimalArray = mb.DecimalArray; //Array for storing decimal numbers

            BinaryWriter bw = mb.BinaryWriter;
            StreamWriter sw = mb.StreamWriter;

            BufferColumn col = buffer.GetColumnInfo(bci.Index);

            bw.Write((int)col.DataType); //write data type

            if (buffer.IsNull(bci.Index))
            {
                bw.Write((byte)1); //Write 1 representing NULL
                bw.Write(0); //write length of 0 for NULL
            }
            else
            {
                bw.Write((byte)0); //write 0 representing NOT NULL

                //Get buffer data
                lock (bci)
                {
                    switch (col.DataType)
                    {
                        case DataType.DT_BOOL:
                            bdata = BitConverter.GetBytes(buffer.GetBoolean(ci));
                            break;
                        case DataType.DT_BYTES:
                            bdata = buffer.GetBytes(ci);
                            break;
                        case DataType.DT_IMAGE:
                        case DataType.DT_NTEXT:
                        case DataType.DT_TEXT:
                            bdata = buffer.GetBlobData(ci, 0, (int)buffer.GetBlobLength(ci));
                            break;
                        case DataType.DT_CY:
                        case DataType.DT_DECIMAL:
                        case DataType.DT_NUMERIC:
                            var ia = decimal.GetBits(buffer.GetDecimal(ci));
                            for (int j = 0; j < 4; j++)
                            {
                                int k = 4 * j;
                                decimalArray[k] = (byte)(ia[j] & 0xFF);
                                decimalArray[k + 1] = (byte)(ia[j] >> 8 & 0xFF);
                                decimalArray[k + 2] = (byte)(ia[j] >> 16 & 0xFF);
                                decimalArray[k + 3] = (byte)(ia[j] >> 24 & 0xFF);
                            }
                            bdata = decimalArray;
                            break;
                        case DataType.DT_DATE:
                        case DataType.DT_DBTIMESTAMP:
                        case DataType.DT_DBTIMESTAMP2:
                            bdata = BitConverter.GetBytes(buffer.GetDateTime(ci).ToBinary());
                            break;
                        case DataType.DT_FILETIME:
                            bdata = BitConverter.GetBytes(buffer.GetInt64(ci));
                            break;
                        case DataType.DT_DBDATE:
                            bw.Write(buffer.GetDate(ci).ToBinary());
                            break;
                        case DataType.DT_DBTIME:
                        case DataType.DT_DBTIME2:
                            bdata = BitConverter.GetBytes(DateTime.MinValue.Add(buffer.GetTime(ci)).ToBinary());
                            break;
                        case DataType.DT_DBTIMESTAMPOFFSET:
                            var dtoffset = buffer.GetDateTimeOffset(ci);
                            BitConverter.GetBytes(dtoffset.DateTime.ToBinary()).CopyTo(bdata, 0);
                            BitConverter.GetBytes(DateTime.MinValue.Add(dtoffset.Offset).ToBinary()).CopyTo(bdata, 8);
                            bdata = decimalArray;
                            break;
                        case DataType.DT_EMPTY:
                        case DataType.DT_NULL:
                            bdata = new byte[0];
                            break;
                        case DataType.DT_GUID:
                            bdata = buffer.GetGuid(ci).ToByteArray();
                            break;
                        case DataType.DT_I1:
                            bdata = BitConverter.GetBytes(buffer.GetSByte(ci));
                            break;
                        case DataType.DT_I2:
                            bdata = BitConverter.GetBytes(buffer.GetInt16(ci));
                            break;
                        case DataType.DT_I4:
                            bdata = BitConverter.GetBytes(buffer.GetInt32(ci));
                            break;
                        case DataType.DT_I8:
                            bdata = BitConverter.GetBytes(buffer.GetInt64(ci));
                            break;
                        case DataType.DT_R4:
                            bdata = BitConverter.GetBytes(buffer.GetSingle(ci));
                            break;
                        case DataType.DT_R8:
                            bdata = BitConverter.GetBytes(buffer.GetDouble(ci));
                            break;
                        case DataType.DT_UI1:
                            bdata = BitConverter.GetBytes(buffer.GetByte(ci));
                            break;
                        case DataType.DT_UI2:
                            bdata = BitConverter.GetBytes(buffer.GetUInt16(ci));
                            break;
                        case DataType.DT_UI4:
                            bdata = BitConverter.GetBytes(buffer.GetUInt32(ci));
                            break;
                        case DataType.DT_UI8:
                            bdata = BitConverter.GetBytes(buffer.GetUInt64(ci));
                            break;
                        case DataType.DT_STR:
                        case DataType.DT_WSTR:
                            bdata = Encoding.Unicode.GetBytes(buffer.GetString(ci));
                            break;
                        default:
                            bdata = new byte[0];
                            break;
                    }
                }

                if (bdata != null)
                {
                    bw.Write(bdata.Length); //write length of buffer
                    bw.Write(bdata);    //write bufferdata;                                    
                }
            }
        }

        public static int GetNumberOfProcessorCores()
        {
            try
            {
                Int64 processorMask = System.Diagnostics.Process.GetCurrentProcess().ProcessorAffinity.ToInt64();
                int numProcessors = (int)Math.Log(processorMask, 2) + 1;
                return Math.Max(1, numProcessors);
            }
            catch
            {
                return 1;
            }
        }


#endregion

#region Designtime Helper Methods
        /// <summary>
        /// Sets HashColumn default properties
        /// </summary>
        /// <param name="hashColumn">HashColumn which properties should be set</param>
        internal static void SetHashColumnProperties(HashColumnsTransformation.HashOuputColumnProperties propsToSet, IDTSOutputColumn hashColumn)
        {
            SetHashColumnProperties(propsToSet, hashColumn, HashColumnsTransformation.HashType.MD5);
        }

        /// <summary>
        /// Sets HashColumn default properties
        /// </summary>
        /// <param name="hashColumn">HashColumn which properties should be set</param>
        internal static void SetHashColumnProperties(HashColumnsTransformation.HashOuputColumnProperties propsToSet, IDTSOutputColumn hashColumn, HashColumnsTransformation.HashType hashTYpe)
        {
            SetHashColumnProperties(propsToSet, hashColumn, HashColumnsTransformation.HashType.MD5, HashColumnsTransformation.HashImplementationType.BinarySafe, HashColumnsTransformation.HashOutputDataType.DT_BYTES, 0);
        }

        public static void SetHashColumnProperties(HashColumnsTransformation.HashOuputColumnProperties propsToSet, IDTSOutputColumn hashColumn, HashColumnsTransformation.HashType hashType, HashColumnsTransformation.HashImplementationType implementation, HashColumnsTransformation.HashOutputDataType dataType, int locale)
        {
            List<int> propsToRemove = new List<int>();
            foreach (IDTSCustomProperty prop in hashColumn.CustomPropertyCollection)
            {
                if (
                    (prop.Name == Resources.HashAlgorithmPropertyName && (propsToSet & HashColumnsTransformation.HashOuputColumnProperties.HashAlgorithm) == HashColumnsTransformation.HashOuputColumnProperties.HashAlgorithm) ||
                    (prop.Name == Resources.HashColumnHashInputColumnsPropertyName && (propsToSet & HashColumnsTransformation.HashOuputColumnProperties.HashColumns) == HashColumnsTransformation.HashOuputColumnProperties.HashColumns) ||
                    (prop.Name == Resources.HashColumnHashFieldSeparatorPropertyName && (propsToSet & HashColumnsTransformation.HashOuputColumnProperties.HashFieldSeparator) == HashColumnsTransformation.HashOuputColumnProperties.HashFieldSeparator) ||
                    (prop.Name == Resources.HashColumnHashImplementationTypePropertyName && (propsToSet & HashColumnsTransformation.HashOuputColumnProperties.HashImplementationType) == HashColumnsTransformation.HashOuputColumnProperties.HashImplementationType) ||
                    (prop.Name == Resources.HashColumnNullReplacementValue && (propsToSet & HashColumnsTransformation.HashOuputColumnProperties.HashNullReplacement) == HashColumnsTransformation.HashOuputColumnProperties.HashNullReplacement) ||
                    (prop.Name == Resources.HashColumnStringTrimmingPropertyName && (propsToSet & HashColumnsTransformation.HashOuputColumnProperties.TrimStrings) == HashColumnsTransformation.HashOuputColumnProperties.TrimStrings)
                )
                {
                    propsToRemove.Add(prop.ID);
                }
            }

            foreach (int id in propsToRemove)
            {
                hashColumn.CustomPropertyCollection.RemoveObjectByID(id);
            }

            if ((propsToSet & HashColumnsTransformation.HashOuputColumnProperties.HashAlgorithm) == HashColumnsTransformation.HashOuputColumnProperties.HashAlgorithm)
            {
                //as the Output columns are Hash Columns, add HashType property with default MD5
                IDTSCustomProperty hashAlgorithm = hashColumn.CustomPropertyCollection.New();
                hashAlgorithm.Description = Resources.HashTypePropertyDescription;
                hashAlgorithm.Name = Resources.HashAlgorithmPropertyName;
                hashAlgorithm.ContainsID = false;
                hashAlgorithm.EncryptionRequired = false;
                hashAlgorithm.ExpressionType = DTSCustomPropertyExpressionType.CPET_NONE;
                hashAlgorithm.TypeConverter = typeof(HashColumnsTransformation.HashType).AssemblyQualifiedName;
                hashAlgorithm.Value = hashType;
            }

            if ((propsToSet & HashColumnsTransformation.HashOuputColumnProperties.HashImplementationType) == HashColumnsTransformation.HashOuputColumnProperties.HashImplementationType)
            {
                IDTSCustomProperty hashImplementation = hashColumn.CustomPropertyCollection.New();
                hashImplementation.Description = "Implementation version of the Hash Type";
                hashImplementation.Name = Resources.HashColumnHashImplementationTypePropertyName;  //"HashImplementationType";
                hashImplementation.ContainsID = false;
                hashImplementation.EncryptionRequired = false;
                hashImplementation.ExpressionType = DTSCustomPropertyExpressionType.CPET_NONE;
                hashImplementation.TypeConverter = typeof(HashColumnsTransformation.HashImplementationType).AssemblyQualifiedName;
                hashImplementation.Value = implementation;
            }

            if ((propsToSet & HashColumnsTransformation.HashOuputColumnProperties.HashColumns) == HashColumnsTransformation.HashOuputColumnProperties.HashColumns)
            {
                IDTSCustomProperty hashColumns = hashColumn.CustomPropertyCollection.New();
                hashColumns.Description = "List of input columns to build HASH";
                hashColumns.Name = Resources.HashColumnHashInputColumnsPropertyName; // "HashInputColumns";
                hashColumns.ContainsID = true;
                hashColumns.EncryptionRequired = false;
                hashColumns.ExpressionType = DTSCustomPropertyExpressionType.CPET_NONE;
                hashColumns.Value = string.Empty;
            }

            if ((propsToSet & HashColumnsTransformation.HashOuputColumnProperties.HashFieldSeparator) == HashColumnsTransformation.HashOuputColumnProperties.HashFieldSeparator)
            {
                IDTSCustomProperty hashFieldSeparator = hashColumn.CustomPropertyCollection.New();
                hashFieldSeparator.Description = "Specifies the field separator for Unicode String Implementations";
                hashFieldSeparator.Name = Resources.HashColumnHashFieldSeparatorPropertyName;
                hashFieldSeparator.ContainsID = false;
                hashFieldSeparator.EncryptionRequired = false;
                hashFieldSeparator.ExpressionType = DTSCustomPropertyExpressionType.CPET_NONE;
                hashFieldSeparator.Value = DefaultFieldtSeparator;
            }

            if ((propsToSet & HashColumnsTransformation.HashOuputColumnProperties.HashNullReplacement) == HashColumnsTransformation.HashOuputColumnProperties.HashNullReplacement)
            {
                IDTSCustomProperty hashFieldSeparator = hashColumn.CustomPropertyCollection.New();
                hashFieldSeparator.Description = "Specifies the NULL replacement value for non NULL safe Unicode String Implementations";
                hashFieldSeparator.Name = Resources.HashColumnNullReplacementValue;
                hashFieldSeparator.ContainsID = false;
                hashFieldSeparator.EncryptionRequired = false;
                hashFieldSeparator.ExpressionType = DTSCustomPropertyExpressionType.CPET_NONE;
                hashFieldSeparator.Value = DefaultNullReplacement;
            }

            if ((propsToSet & HashColumnsTransformation.HashOuputColumnProperties.TrimStrings) == HashColumnsTransformation.HashOuputColumnProperties.TrimStrings)
            {
                IDTSCustomProperty stringTrimming = hashColumn.CustomPropertyCollection.New();
                stringTrimming.Description = "Specifies whether and how string values should be trimmed";
                stringTrimming.Name = Resources.HashColumnStringTrimmingPropertyName;
                stringTrimming.ContainsID = false;
                stringTrimming.EncryptionRequired = false;
                stringTrimming.ExpressionType = DTSCustomPropertyExpressionType.CPET_NONE;
                stringTrimming.TypeConverter = typeof(HashColumnsTransformation.StringTrimming).AssemblyQualifiedName;
                stringTrimming.Value = HashColumnsTransformation.StringTrimming.None;
            }

            if (
                ((propsToSet & HashColumnsTransformation.HashOuputColumnProperties.HashAlgorithm) == HashColumnsTransformation.HashOuputColumnProperties.HashAlgorithm)
            )
            {
                //Set data type for the column accordingly with the hashType - default for MD4
                SetHashColumnDataType(hashType, hashColumn, locale);
            }
        }

        /// <summary>
        /// Sets DataType of the Output Clumn according the HashType
        /// </summary>
        /// <param name="hashType">Hash Type of the output Column</param>
        /// <param name="outputColumn">Ourtput Column to set data type</param>
        public static void SetHashColumnDataType(HashColumnsTransformation.HashType hashType, IDTSOutputColumn outputColumn, int locale)
        {
            int dataLength;

            DataType dt = outputColumn.DataType;
            if (dt != DataType.DT_BYTES && dt != DataType.DT_WSTR && dt != DataType.DT_WSTR)
                dt = DataType.DT_BYTES;

            GetHashTypeDataType(hashType, dt, out dataLength);

            outputColumn.SetDataTypeProperties(dt, dataLength, 0, 0, outputColumn.CodePage);

        }
        /// <summary>
        /// Gets Data Type for particular HashType
        /// </summary>
        /// <param name="hashType">HashType to get the Data Type</param>
        /// <param name="dataType">Output DataType for HashType</param>
        /// <param name="dataLength">Data Length for HashType</param>
        public static void GetHashTypeDataType(HashColumnsTransformation.HashType hashType, DataType outputDataType, out int dataLength)
        {
            switch (hashType)
            {
                case HashColumnsTransformation.HashType.None:
                    dataLength = 1;
                    break;
                case HashColumnsTransformation.HashType.MD5:
                    dataLength = 16;
                    break;
                case HashColumnsTransformation.HashType.RipeMD160:
                case HashColumnsTransformation.HashType.SHA1:
                    dataLength = 20;
                    break;
                case HashColumnsTransformation.HashType.SHA256:
                    dataLength = 32;
                    break;
                case HashColumnsTransformation.HashType.SHA384:
                    dataLength = 48;
                    break;
                case HashColumnsTransformation.HashType.SHA512:
                    dataLength = 64;
                    break;
                case HashColumnsTransformation.HashType.CRC32:
                    dataLength = 4;
                    break;
                default:
                    dataLength = 16;
                    break;
            }

            if (hashType != HashColumnsTransformation.HashType.None)
            {
                switch (outputDataType)
                {
                    case DataType.DT_BYTES:
                        break;
                    case DataType.DT_WSTR:
                        dataLength = dataLength * 2;
                        break;
                    case DataType.DT_STR:
                        dataLength = dataLength * 2;
                        break;
                    default:
                        break;
                }
            }
        }

        ///// <summary>
        ///// Get Maximum Input Sort Order
        ///// </summary>
        ///// <returns>Maximu Sort Order of Input Columns</returns>
        //public static int GetMaxInputColumnsSortOrder(IDTSInput input)
        //{
        //    int maxOrder = 0;

        //    foreach (IDTSInputColumn col in input.InputColumnCollection)
        //    {
        //        foreach (IDTSCustomProperty prop in col.CustomPropertyCollection)
        //        {
        //            if (prop.Name == Resources.InputSortOrderPropertyName)
        //            {
        //                int order = prop.Value != null ? (int)prop.Value : 0;
        //                if (order > maxOrder)
        //                    maxOrder = order;
        //                break;
        //            }
        //        }
        //    }
        //    return maxOrder;
        //}
        public static void UpdateSortOrder(IDTSInput input, int inputColumnID, int val)
        {
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

        internal static void AddComponentCustomProperties(IDTSComponentMetaData100 componentMetaData)
        {
            IDTSCustomProperty parallel = null;
            parallel = componentMetaData.CustomPropertyCollection.New();
            parallel.Name = Resources.HashTransformationParallelProcessingPropertyName;
            parallel.Description = "Specifies whether Hash processing for multiple output columsn should run in multiple threads.";
            parallel.ContainsID = false;
            parallel.EncryptionRequired = false;
            parallel.TypeConverter = typeof(HashColumnsTransformation.ParallelProcessing).AssemblyQualifiedName;
            parallel.Value = HashColumnsTransformation.ParallelProcessing.Off;
        }

#endregion
    }


    public class HashThreadState
    {
        public HashThreadState(HashColumnsTransformation.HashColumnInfo hCol, List<HashColumnsTransformation.InputBufferColumnInfo> inputBufferColumns, PipelineBuffer buffer, HashMemoryBuffers mb, ManualResetEvent resetEvent)
        {
            this.HashColumnInfo = hCol;
            this.InputBufferColumns = inputBufferColumns;
            this.PipelineBuffer = buffer;
            this.MemoryBuffers = mb;
            this.ResetEvent = resetEvent;
        }

        public HashColumnsTransformation.HashColumnInfo HashColumnInfo { get; private set; }
        public List<HashColumnsTransformation.InputBufferColumnInfo> InputBufferColumns { get; private set; }
        public PipelineBuffer PipelineBuffer { get; private set; }
        public HashMemoryBuffers MemoryBuffers { get; private set; }
        public ManualResetEvent ResetEvent { get; private set; }
    }

    /// <summary>
    /// Holds Memory Buffers
    /// </summary>
    public class HashMemoryBuffers
    {
        public HashMemoryBuffers(int streamCapacity)
        {
            this.MemoryStream = new MemoryStream(streamCapacity);
            this.StreamWriter = new StreamWriter(MemoryStream, Encoding.Unicode, 8000);
            this.BinaryWriter = new BinaryWriter(MemoryStream);
            this.DecimalArray = new byte[16];

            //Ensure the Unicode identification bytes are written to the beginnging of memory Stream;
            StreamWriter.Write("");
            StreamWriter.Flush();
        }

        public MemoryStream MemoryStream { get; private set; }
        public StreamWriter StreamWriter { get; private set; }
        public BinaryWriter BinaryWriter { get; private set; }
        public byte[] DecimalArray { get; private set; }
    }

}
