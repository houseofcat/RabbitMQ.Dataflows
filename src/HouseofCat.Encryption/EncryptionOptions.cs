using System;
using static HouseofCat.Encryption.Enums;

namespace HouseofCat.Encryption
{
    public class EncryptionOptions
    {
        public EncryptionMethod EncryptionMethod { get; set; }
        public int MacBitSize { get; set; } = 128;
        public int NonceSize { get; set; } = 12;
        public HashOptions HashOptions { get; set; }
    }
}
