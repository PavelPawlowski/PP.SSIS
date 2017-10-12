using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization.Formatters.Binary;
using System.Security.Cryptography;
using System.Text;

namespace PP.SSIS.DataFlow.Common
{
    /// <summary>
    /// General class for handling hash  of column values
    /// </summary>
    public class KeyDataHash : IComparable<KeyDataHash>, IComparable, IEquatable<KeyDataHash>
    {
        /// <summary>
        /// Creates a new instance of KeyDataHash using MD5 hash function
        /// </summary>
        /// <param name="data">Kye data to be hashed</param>
        public KeyDataHash(IComparable[] data) : this(data, HashAlgorithmType.MD5)
        {

        }

        /// <summary>
        /// Creates a new instance of KeyDatahash using provided HashAlgorigthm
        /// </summary>
        /// <param name="data">Key data to be hashed</param>
        /// <param name="hashAlgorithm">Hash Algorithm to be used</param>
        public KeyDataHash(IComparable[] data, HashAlgorithmType hashType)
        {
            HashAlgorithmType = hashType;

            if (hashType == HashAlgorithmType.None)
            {
                HashAlgorithm = null;
                Hash = null;
                KeyData = data;
            }
            else
            {
                HashAlgorithm = HashUtil.GetHashAlgorithm(hashType) ?? HashUtil.GetHashAlgorithm(HashAlgorithmType.MD5);
                KeyData = null;

                BinaryFormatter bf = new BinaryFormatter();

                if (data != null)
                {
                    using (MemoryStream ms = new MemoryStream())
                    {
                        bf.Serialize(ms, data);
                        ms.Position = 0;
                        //caluclate the hash value
                        Hash = HashAlgorithm.ComputeHash(ms);
                    }
                }
                else
                {
                    Hash = HashAlgorithm.ComputeHash(new byte[0]);
                }
            }

        }

        /// <summary>
        /// HashAlgorithm being used by the instance of KeyDataHash
        /// </summary>
        public HashAlgorithm HashAlgorithm { get; private set; }
        public HashAlgorithmType HashAlgorithmType { get; private set; }


        /// <summary>
        /// Hash value of the KeyData
        /// </summary>
        public byte[] Hash { get; private set; }

        public IComparable[] KeyData { get; private set; }

        public override string ToString()
        {
            if (HashAlgorithmType != HashAlgorithmType.None)
                return BitConverter.ToString(Hash);
            else
                return base.ToString();
        }

        #region Operator Overloading
        public static bool operator ==(KeyDataHash kh1, KeyDataHash kh2)
        {
            if (object.ReferenceEquals(kh1, null))
                return object.ReferenceEquals(kh1, null);
            else
                return kh1.Equals(kh2);

        }

        public static bool operator !=(KeyDataHash kh1, KeyDataHash kh2)
        {
            return !(kh1 == kh2);
        }
        #endregion

        #region IComparable Implementation
        public int CompareTo(KeyDataHash other)
        {
            if (other == null)
                return 1;
            else
                return GetHashCode().CompareTo(other.GetHashCode());
        }

        public int CompareTo(object obj)
        {
            return CompareTo(obj as KeyDataHash);
        }

        public override bool Equals(object obj)
        {
            return Equals(obj as KeyDataHash);
        }

        public bool Equals(KeyDataHash other)
        {
            if (other == null)
                return false;

            if (this.HashAlgorithmType != other.HashAlgorithmType)
                return false;
            else
            {
                if (HashAlgorithmType == HashAlgorithmType.None)
                {
                    if (KeyData.Length != other.KeyData.Length)
                        return false;

                    for (int i = 0; i < KeyData.Length; i++)
                    {
                        if (!KeyData[i].Equals(other.KeyData[i]))
                            return false;
                    }
                }
                else
                {
                    if (this.Hash.Length != other.Hash.Length)
                        return false;
                    for (int i = 0; i < Hash.Length; i++)
                    {
                        if (Hash[i] != other.Hash[i])
                            return false;
                    }
                }
            }

            return true;
        }


        private int _hashCode = 0;
        public override int GetHashCode()
        {
            if (_hashCode == 0)
            {
                if (HashAlgorithmType == HashAlgorithmType.None)
                {
                    if (KeyData == null || KeyData.Length == 0)
                        _hashCode = -1;
                    else
                    {
                        unchecked // Overflow is fine, just wrap
                        {
                            _hashCode = 17;
                            for (int i = 0; i < KeyData.Length; i++)
                            {
                                _hashCode = _hashCode * 23 + KeyData[i].GetHashCode();
                            }
                        }

                    }
                }
                else
                {
                    if (Hash == null || Hash.Length == 0)
                        _hashCode = -1;
                    else
                    {
                        unchecked
                        {
                            _hashCode = 17;
                            for (int i = 0; i < Hash.Length; i++)
                            {
                                _hashCode = _hashCode * 23 + Hash[i].GetHashCode();
                            }
                        }
                    }
                }
            }
            return _hashCode;
        }


	    #endregion    
    }
}
