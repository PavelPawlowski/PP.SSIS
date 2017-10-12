using System.Security.Cryptography;

namespace PP.SSIS.DataFlow.Common
{
    /// <summary>
    /// Provides information about supported HASH Algorithms
    /// </summary>
    public enum HashAlgorithmType
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

    public class HashUtil
    {

        public static int GetHashAlgorithmBinarySize(HashAlgorithmType hashType)
        {
            switch (hashType)
            {
                case HashAlgorithmType.None:
                case HashAlgorithmType.MD5:
                    return 16;
                case HashAlgorithmType.RipeMD160:
                case HashAlgorithmType.SHA1:
                   return 20;
                case HashAlgorithmType.SHA256:
                    return 32;
                case HashAlgorithmType.SHA384:
                    return 48;
                case HashAlgorithmType.SHA512:
                    return 64;
                case HashAlgorithmType.CRC32:
                    return 4;
                default:
                    return 16;
            }
        }

        public static HashAlgorithm GetHashAlgorithm(HashAlgorithmType hashType)
        {
            HashAlgorithm alg = null;

            switch (hashType)
            {
                case HashAlgorithmType.None:
                    break;
                case HashAlgorithmType.MD5:
                    alg = new MD5CryptoServiceProvider();
                    break;
                case HashAlgorithmType.RipeMD160:
                    alg = new RIPEMD160Managed();
                    break;
                case HashAlgorithmType.SHA1:
                    alg = new SHA1CryptoServiceProvider();
                    break;
                case HashAlgorithmType.SHA256:
                    alg = new SHA256CryptoServiceProvider();
                    break;
                case HashAlgorithmType.SHA384:
                    alg = new SHA384CryptoServiceProvider();
                    break;
                case HashAlgorithmType.SHA512:
                    alg = new SHA512CryptoServiceProvider();
                    break;
                case HashAlgorithmType.CRC32:
                    alg = new Crc32();
                    break;
            }
            return alg;
        }
    }
}
