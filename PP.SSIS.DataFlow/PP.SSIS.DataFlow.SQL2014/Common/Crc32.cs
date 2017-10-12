using System;
using System.Security.Cryptography;


namespace PP.SSIS.DataFlow
{
    /// <summary>
    /// CRC32 Hash Algorithm implementation
    /// </summary>
    public class Crc32 : HashAlgorithm
    {
        public const UInt32 DefaultPolynomial = 0xedb88320;
        public const UInt32 DefaultHashSeed = 0xffffffff;

        private UInt32 hash;
        private UInt32 hashSeed;
        private UInt32[] hashTable;
        private static UInt32[] defaultHashTable;

        /// <summary>
        /// Creates instance of the Crc32 hash Algorithm
        /// </summary>
        public Crc32()
        {
            hashTable = InitializeHashTable(DefaultPolynomial);
            hashSeed = DefaultHashSeed;
            Initialize();
        }

        public Crc32(UInt32 polynomial, UInt32 hashSeed)
        {
            hashTable = InitializeHashTable(polynomial);
            this.hashSeed = hashSeed;
            Initialize();
        }

        public override void Initialize()
        {
            hash = hashSeed;
        }

        protected override void HashCore(byte[] buffer, int start, int length)
        {            
            hash = HashCore(buffer, hashTable, hash, start, length);
        }

        protected override byte[] HashFinal()
        {
            byte[] hashBuffer = UInt32ToBigEndianBytes(~hash);
            this.HashValue = hashBuffer;
            return hashBuffer;
        }

        /// <summary>
        /// Returns Hash Size in bits
        /// </summary>
        public override int HashSize
        {
            get { return 32; }
        }

        /// <summary>
        /// Compute CRC32 hash of input buffer
        /// </summary>
        /// <param name="inputBuffer">Input buffer to compute hash value</param>
        /// <returns>CRC32 hash of the input buffer</returns>
        public static UInt32 Compute(byte[] inputBuffer)
        {
            return HashCore(inputBuffer, InitializeHashTable(DefaultPolynomial), DefaultHashSeed, 0, inputBuffer.Length);
        }
        /// <summary>
        /// Compute CRC32 hash of INput buffer with specified hash seed
        /// </summary>
        /// <param name="inputBuffer">Input buffer to compute hash value</param>
        /// <param name="hashSeed">hashSeed to be used for CRC32 hash calculation</param>
        /// <returns>CRC32 hash of the input buffer</returns>
        public static UInt32 Compute(byte[] inputBuffer, UInt32 hashSeed)
        {
            return HashCore(inputBuffer, InitializeHashTable(DefaultPolynomial), hashSeed, 0, inputBuffer.Length);
        }
        /// <summary>
        /// Compute CRC32 hash of input buffer with specified hash seed and polynomial
        /// </summary>
        /// <param name="buffer">Input bufefr to calculate hash value</param>
        /// <param name="hashSeed">hashSeed to be used for CRC32 hash calculation</param>
        /// <param name="polynomial">polynomial to be used for CRC32 hash calculation</param>
        /// <returns>CRC32 hash of the input buffer</returns>
        public static UInt32 Compute(byte[] buffer, UInt32 hashSeed, UInt32 polynomial)
        {
            return HashCore(buffer, InitializeHashTable(polynomial), hashSeed, 0, buffer.Length);
        }

        /// <summary>
        /// Initialize hashTable
        /// </summary>
        /// <param name="polynomial">Polynomial to initialize hash table</param>
        /// <returns>Hash table based od provided polynomial</returns>
        private static UInt32[] InitializeHashTable(UInt32 polynomial)
        {
            if (polynomial == DefaultPolynomial && defaultHashTable != null)
                return defaultHashTable;

            UInt32[] hashTable = new UInt32[256];
            for (int i = 0; i < 256; i++)
            {
                UInt32 item = (UInt32)i;
                for (int j = 0; j < 8; j++)
                    if ((item & 1) == 1)
                        item = (item >> 1) ^ polynomial;
                    else
                        item = item >> 1;
                hashTable[i] = item;
            }

            if (polynomial == DefaultPolynomial)
                defaultHashTable = hashTable;

            return hashTable;
        }

        /// <summary>
        /// Calculate CRC32 Hash
        /// </summary>
        /// <param name="inputBuffer">input buffer to calculatae CRC32 hash</param>
        /// <param name="hashTable">hash table to be used for calculation</param>
        /// <param name="hashSeed">hashSeed to be used for calculation</param>
        /// <param name="start">INputBufeer start possition</param>
        /// <param name="length">Number of bytes from start position to calculate hash</param>
        /// <returns></returns>
        private static UInt32 HashCore(byte[] inputBuffer, UInt32[] hashTable, UInt32 hashSeed, int start, int length)
        {
            UInt32 crc = hashSeed;
            for (int i = start; i < length; i++)
                unchecked
                {
                    crc = (crc >> 8) ^ hashTable[inputBuffer[i] ^ crc & 0xff];
                }
            return crc;
        }

        /// <summary>
        /// Converts UInt32 to big endina encoded byted
        /// </summary>
        /// <param name="x">Uint32 value to be big endian encoded</param>
        /// <returns>Big Endian encoded bytes representing original UInt32</returns>
        private byte[] UInt32ToBigEndianBytes(UInt32 x)
        {
            //Return BigEndian encoded UInt332 bytes - in backward order
            return new byte[] { (byte)((x >> 24) & 0xff), (byte)((x >> 16) & 0xff), (byte)((x >> 8) & 0xff), (byte)(x & 0xff) };
        }
    }
}
