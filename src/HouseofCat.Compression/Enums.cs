namespace HouseofCat.Compression
{
    public static class Enums
    {
        public enum EncryptionMethod
        {
            AES256_ARGON2ID
        }

        public enum CompressionMethod
        {
            Gzip,
            Deflate,
            Brotli,
            LZ4,
        }

        public enum SerializationMethod
        {
            JsonString,
            Utf8TextJson,
            Utf8Json,
        }
    }
}
