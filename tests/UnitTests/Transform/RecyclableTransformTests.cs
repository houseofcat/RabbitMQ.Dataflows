using HouseofCat.Compression;
using HouseofCat.Compression.Recyclable;
using HouseofCat.Data;
using HouseofCat.Encryption;
using HouseofCat.Hashing.Argon;
using HouseofCat.Serialization;
using HouseofCat.Utilities.Time;

namespace Transform;

public class RecyclableTransformTests
{
    private readonly RecyclableTransformer _middleware;

    private const string Passphrase = "SuperNintendoHadTheBestZelda";
    private const string Salt = "SegaGenesisIsTheBestConsole";

    private static readonly byte[] _data = new byte[5000];
    private readonly MyCustomClass MyClass = new MyCustomClass();

    private static ReadOnlyMemory<byte> _serializedData;

    public RecyclableTransformTests()
    {
        Enumerable.Repeat<byte>(0xFF, 1000).ToArray().CopyTo(_data, 0);
        Enumerable.Repeat<byte>(0xAA, 1000).ToArray().CopyTo(_data, 1000);
        Enumerable.Repeat<byte>(0x1A, 1000).ToArray().CopyTo(_data, 2000);
        Enumerable.Repeat<byte>(0xAF, 1000).ToArray().CopyTo(_data, 3000);
        Enumerable.Repeat<byte>(0x01, 1000).ToArray().CopyTo(_data, 4000);

        MyClass.ByteData = _data;

        var hashingProvider = new Argon2ID_HashingProvider();
        var hashKey = hashingProvider.GetHashKey(Passphrase, Salt, 32);

        _middleware = new RecyclableTransformer(
            new JsonProvider(),
            new RecyclableGzipProvider(),
            new RecyclableAesGcmEncryptionProvider(hashKey));

        _serializedData = _middleware.Transform(MyClass);
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
    public void Transform()
    {
        var transformedData = _middleware.Transform(MyClass);

        Assert.False(transformedData.Length == 0);
        Assert.Equal(_serializedData.Length, transformedData.Length);
    }

    [Fact]
    public void TransformToStream()
    {
        var transformedStream = _middleware.TransformToStream(MyClass);

        Assert.NotNull(transformedStream);
        Assert.Equal(_serializedData.Length, transformedStream.ToArray().Length);
    }

    [Fact]
    public async Task TransformAsync()
    {
        var transformedData = await _middleware.TransformAsync(MyClass);

        Assert.False(transformedData.Length == 0);
        Assert.Equal(_serializedData.Length, transformedData.ToArray().Length);
    }

    [Fact]
    public async Task TransformToStreamAsync()
    {
        var transformedStream = await _middleware.TransformToStreamAsync(MyClass);

        Assert.NotNull(transformedStream);
        Assert.Equal(_serializedData.Length, transformedStream.ToArray().Length);
    }

    [Fact]
    public void RestoreFromStream()
    {
        var transformedStream = _middleware.TransformToStream(MyClass);
        var myCustomClass = _middleware.Restore<MyCustomClass>(transformedStream);

        Assert.NotNull(myCustomClass);
        Assert.Equal(_data.Length, myCustomClass.ByteData.Length);
    }

    [Fact]
    public async Task RestoreAsync()
    {
        var myCustomClass = await _middleware.RestoreAsync<MyCustomClass>(_serializedData);

        Assert.NotNull(myCustomClass);
        Assert.Equal(_data.Length, myCustomClass.ByteData.Length);
    }
}
