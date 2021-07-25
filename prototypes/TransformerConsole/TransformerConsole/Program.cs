using HouseofCat.Compression;
using HouseofCat.Data.Recyclable;
using HouseofCat.Encryption;
using HouseofCat.Hashing;
using HouseofCat.Serialization;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace TransformerConsole
{
    public static class Program
    {
        private static RecyclableTransformer _middleware;

        private const string Passphrase = "SuperNintendoHadTheBestZelda";
        private const string Salt = "SegaGenesisIsTheBestConsole";

        private static byte[] _data = new byte[5000];
        private static MyCustomClass MyClass = new MyCustomClass();

        private static ArraySegment<byte> _serializedData;

        public static async Task Main(string[] args)
        {
            Setup();

            await Serialize_7KB_Async();
            Deserialize_7KB();

            Console.ReadKey();
        }

        public static async Task Serialize_7KB_Async()
        {
            for (var i = 0; i < 200; i++)
            {
                await _middleware.InputAsync(MyClass);
            }
        }

        public static void Deserialize_7KB()
        {
            for (var i = 0; i < 200; i++)
            {
                _middleware.Output<MyCustomClass>(_serializedData);
            }
        }

        public static void Setup()
        {
            Enumerable.Repeat<byte>(0xFF, 1000).ToArray().CopyTo(_data, 0);
            Enumerable.Repeat<byte>(0xAA, 1000).ToArray().CopyTo(_data, 1000);
            Enumerable.Repeat<byte>(0x1A, 1000).ToArray().CopyTo(_data, 2000);
            Enumerable.Repeat<byte>(0xAF, 1000).ToArray().CopyTo(_data, 3000);
            Enumerable.Repeat<byte>(0x01, 1000).ToArray().CopyTo(_data, 4000);

            MyClass.ByteData = _data;

            var hashingProvider = new Argon2IDHasher();
            var hashKey = hashingProvider.GetHashKey(Passphrase, Salt, 32);

            _middleware = new RecyclableTransformer(
                new NewtonsoftJsonProvider(),
                new RecyclableAesGcmEncryptionProvider(hashKey, hashingProvider.Type),
                new RecyclableGzipProvider());

            (var buffer, _) = _middleware.Input(MyClass);
            _serializedData = buffer.ToArray();
        }
    }
}
