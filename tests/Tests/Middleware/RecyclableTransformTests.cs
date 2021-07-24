using HouseofCat.Compression;
using HouseofCat.Data;
using HouseofCat.Data.Recyclable;
using HouseofCat.Dataflows;
using HouseofCat.Encryption;
using HouseofCat.Hashing;
using HouseofCat.Serialization;
using HouseofCat.Utilities.Time;
using Microsoft.Toolkit.HighPerformance;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace Middleware
{
    public class RecyclableTransformTests
    {
        private readonly ITestOutputHelper _output;
        private readonly RecyclableTransformer _middleware;

        private const string Passphrase = "SuperNintendoHadTheBestZelda";
        private const string Salt = "SegaGenesisIsTheBestConsole";

        private static byte[] _data = new byte[5000];
        private MyCustomClass MyClass = new MyCustomClass();

        private static int _originalSize;

        private static ArraySegment<byte> _serializedData;
        private static long _serializedLength;

        public RecyclableTransformTests(ITestOutputHelper output)
        {
            _output = output;
            Enumerable.Repeat<byte>(0xFF, 1000).ToArray().CopyTo(_data, 0);
            Enumerable.Repeat<byte>(0xAA, 1000).ToArray().CopyTo(_data, 1000);
            Enumerable.Repeat<byte>(0x1A, 1000).ToArray().CopyTo(_data, 2000);
            Enumerable.Repeat<byte>(0xAF, 1000).ToArray().CopyTo(_data, 3000);
            Enumerable.Repeat<byte>(0x01, 1000).ToArray().CopyTo(_data, 4000);

            MyClass.ByteData = _data;

            var hashingProvider = new Argon2IDHasher();
            var hashKey = hashingProvider
                .GetHashKeyAsync(Passphrase, Salt, 32)
                .GetAwaiter()
                .GetResult();

            var serializationProvider = new Utf8JsonProvider();
            _originalSize = serializationProvider.Serialize(MyClass).Length;

            _middleware = new RecyclableTransformer(
                serializationProvider,
                new RecyclableAesGcmEncryptionProvider(hashKey, hashingProvider.Type),
                new RecyclableGzipProvider());

            (_serializedData, _serializedLength) = _middleware.Input(MyClass);
        }

        #region MyCustomObject

        public class MyCustomClass
        {
            public MyCustomEmbeddedClass EmbeddedClass { get; set; } = new MyCustomEmbeddedClass();
            public string MyString { get; set; } = "Crazy String Value";
            public byte[] ByteData { get; set; }

            public Dictionary<string, object> Data { get; set; } = new Dictionary<string, object>
            {
                { "I like to eat", "Apples and Bananas" },
                { "TestKey", 12 },
                { "TestKey2", 12.0 },
                { "Date", Time.GetDateTimeNow(Time.Formats.CatRFC3339) }
            };

            public IDictionary<string, object> AbstractData { get; set; } = new Dictionary<string, object>
            {
                { "I like to eat", "Apples and Bananas" },
                { "TestKey", 12 },
                { "TestKey2", 12.0 },
                { "Date", Time.GetDateTimeNow(Time.Formats.CatRFC3339) }
            };

            public MyCustomSubClass SubClass { get; set; } = new MyCustomSubClass();

            public class MyCustomEmbeddedClass
            {
                public int HappyNumber { get; set; } = 42;
                public byte HappyByte { get; set; } = 0xFE;
            }
        }

        public class MyCustomSubClass
        {
            public List<int> Ints { get; set; } = new List<int> { 2, 4, 6, 8, 10 };
            public List<double> Doubles { get; set; } = new List<double> { 1.0, 1.0, 2.0, 3.0, 5.0, 8.0, 13.0 };
        }

        #endregion

        [Fact]
        public void Serialize()
        {
            (var serializedData, var length) = _middleware.Input(MyClass);

            Assert.NotNull(serializedData);
            Assert.Equal(_serializedLength, serializedData.ToArray().Length);
        }

        [Fact]
        public void Deserialize()
        {
            var myCustomClass = _middleware.Output<MyCustomClass>(_serializedData.ToArray());

            Assert.NotNull(myCustomClass);
            Assert.Equal(_data.Length, myCustomClass.ByteData.Length);
        }

        [Fact]
        public async Task SerializeAsync()
        {
            (var serializedData, var length) = await _middleware.InputAsync(MyClass);

            Assert.NotNull(serializedData);
            Assert.Equal(_serializedLength, serializedData.ToArray().Length);
        }

        [Fact]
        public async Task DeserializeAsync()
        {
            var myCustomClass = await _middleware.OutputAsync<MyCustomClass>(_serializedData);

            Assert.NotNull(myCustomClass);
            Assert.Equal(_data.Length, myCustomClass.ByteData.Length);
        }
    }
}
