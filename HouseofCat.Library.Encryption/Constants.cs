namespace HouseofCat.Library.Encryption
{
    public static class Constants
    {
        public static class Argon
        {
            public const int DoP = 4;
            public const int MemorySize = 2048;
            public const int Iterations = 12;
        }

        public static class Aes256
        {
            public const int MacBitSize = 128;
            public const int NonceSize = 12;
            public const int KeySize = 32;
        }
    }
}
