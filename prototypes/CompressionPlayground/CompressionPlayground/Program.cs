using HouseofCat.Compression;
using HouseofCat.Serialization;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace TransformerConsole
{
    public static class Program
    {
        private static byte[] _data = new byte[5000];
        private static MyCustomClass MyClass = new MyCustomClass();

        private static ArraySegment<byte> _serializedData;
        private static ISerializationProvider _serializationProvider;
        private static ICompressionProvider _gzipProvider;
        private static ICompressionProvider _deflateProvider;

        public static async Task Main(string[] args)
        {
            Setup();

            await GzipCompressAsync();
            await DeflateCompressAsync();

            Console.ReadKey();
        }

        public static async Task GzipCompressAsync()
        {
            var compressedData = _gzipProvider.Compress(_serializedData);
            var length = GetLength(compressedData);

            if (_serializedData.Count == length)
            {
                await Console.Out.WriteLineAsync("LastFourLength is good.");
            }
        }

        private static int GetLength(ArraySegment<byte> compressedData)
        {
            var lastFour = compressedData.AsSpan(compressedData.Count - 4, 4);
            var lastFourDerivedLength = BitConverter.ToInt32(lastFour);
            // little endian reversal
            var rfcDerivedLengthBytes = (lastFour[3] << 24) | (lastFour[2] << 24) + (lastFour[1] << 8) + lastFour[0];
            return (((((lastFour[4 - 1] << 8) | lastFour[3 - 1]) << 8) | lastFour[2 - 1]) << 8) | lastFour[1 - 1];
        }

        public static async Task DeflateCompressAsync()
        {
            var compressedData = _deflateProvider.Compress(_serializedData.ToArray());
            var span = compressedData.Slice(compressedData.Count - 8, 8);
            var length = BitConverter.ToInt32(span);

            if (compressedData.Count == length)
            {
                await Console.Out.WriteLineAsync("Length is good.");
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
            _serializationProvider = new Utf8JsonProvider();
            _gzipProvider = new GzipProvider();
            _deflateProvider = new DeflateProvider();
            _serializedData = _serializationProvider.Serialize(MyClass).ToArray();
        }
    }
}
