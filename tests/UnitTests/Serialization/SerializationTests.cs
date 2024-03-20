using HouseofCat.Serialization;
using HouseofCat.Utilities.Time;
using MessagePack;

namespace Serialization;

public class SerializationTests
{
    private readonly ISerializationProvider _jsonProvider;
    private readonly ISerializationProvider _newtonsoftProvider;
    private readonly ISerializationProvider _messagePackProvider;

    public SerializationTests()
    {
        _jsonProvider = new JsonProvider();
        _newtonsoftProvider = new NewtonsoftJsonProvider();
        _messagePackProvider = new MessagePackProvider();
    }

    #region Object for Deserialization

    public class MyCustomClass
    {
        public MyCustomEmbeddedClass EmbeddedClass { get; set; } = new MyCustomEmbeddedClass();
        public string MyString { get; set; } = "Crazy String Value";

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

    [MessagePackObject]
    public class MyCustomClass2
    {
        [Key(0)]
        public MyCustomEmbeddedClass2 EmbeddedClass { get; set; } = new MyCustomEmbeddedClass2();

        [Key(1)]
        public string MyString { get; set; } = "Crazy String Value";

        [Key(2)]
        public Dictionary<string, object> Data { get; set; } = new Dictionary<string, object>
        {
            { "I like to eat", "Apples and Bananas" },
            { "TestKey", 12 },
            { "TestKey2", 12.0 },
            { "Date", Time.GetDateTimeNow(Time.Formats.CatRFC3339) }
        };

        [Key(3)]
        public IDictionary<string, object> AbstractData { get; set; } = new Dictionary<string, object>
        {
            { "I like to eat", "Apples and Bananas" },
            { "TestKey", 12 },
            { "TestKey2", 12.0 },
            { "Date", Time.GetDateTimeNow(Time.Formats.CatRFC3339) }
        };

        [Key(4)]
        public MyCustomSubClass2 SubClass { get; set; } = new MyCustomSubClass2();

        [MessagePackObject]
        public class MyCustomEmbeddedClass2
        {
            [Key(0)]
            public int HappyNumber { get; set; } = 42;
            [Key(1)]
            public byte HappyByte { get; set; } = 0xFE;
        }
    }

    [MessagePackObject]
    public class MyCustomSubClass2
    {
        [Key(0)]
        public List<int> Ints { get; set; } = new List<int> { 2, 4, 6, 8, 10 };
        [Key(1)]
        public List<double> Doubles { get; set; } = new List<double> { 1.0, 1.0, 2.0, 3.0, 5.0, 8.0, 13.0 };
    }

    #endregion

    [Fact]
    public void JsonProvider_SerializeDeserialize()
    {
        var customObject = new MyCustomClass();

        var data = _jsonProvider.Serialize(customObject);
        Assert.False(data.Length == 0);

        var deserializedObject = _jsonProvider.Deserialize<MyCustomClass>(data);
        Assert.NotNull(deserializedObject);

        // Spot Check
        Assert.Equal(customObject.MyString, deserializedObject.MyString);
        Assert.Equal(customObject.EmbeddedClass.HappyByte, deserializedObject.EmbeddedClass.HappyByte);
        //Assert.Equal(customObject.Data["TestKey"], deserializedObject.Data["TestKey"]); // Not Supported Yet
        //Assert.Equal(customObject.AbstractData["TestKey2"], deserializedObject.AbstractData["TestKey2"]); // Not Supported Yet
        Assert.Equal(customObject.SubClass.Ints.ElementAt(3), deserializedObject.SubClass.Ints.ElementAt(3));
    }

    [Fact]
    public void JsonProvider_SerializeDeserialize_Stream()
    {
        var customObject = new MyCustomClass();

        var memoryStream = new MemoryStream();
        _jsonProvider.Serialize(memoryStream, customObject);
        Assert.False(memoryStream.Length == 0);

        var deserializedObject = _jsonProvider.Deserialize<MyCustomClass>(memoryStream);
        Assert.NotNull(deserializedObject);

        // Spot Check
        Assert.Equal(customObject.MyString, deserializedObject.MyString);
        Assert.Equal(customObject.EmbeddedClass.HappyByte, deserializedObject.EmbeddedClass.HappyByte);
        //Assert.Equal(customObject.Data["TestKey"], deserializedObject.Data["TestKey"]); // Not Supported Yet
        //Assert.Equal(customObject.AbstractData["TestKey2"], deserializedObject.AbstractData["TestKey2"]); // Not Supported Yet
        Assert.Equal(customObject.SubClass.Ints.ElementAt(3), deserializedObject.SubClass.Ints.ElementAt(3));
    }

    [Fact]
    public void NewtonsoftJsonProvider_SerializeDeserialize()
    {
        var customObject = new MyCustomClass();

        var data = _newtonsoftProvider.Serialize(customObject);
        Assert.False(data.Length == 0);

        var deserializedObject = _newtonsoftProvider.Deserialize<MyCustomClass>(data);
        Assert.NotNull(deserializedObject);

        // Spot Check
        Assert.Equal(customObject.MyString, deserializedObject.MyString);
        Assert.Equal(customObject.EmbeddedClass.HappyByte, deserializedObject.EmbeddedClass.HappyByte);
        Assert.Equal(customObject.Data["TestKey"], Convert.ToInt32(deserializedObject.Data["TestKey"])); // deserialized int32 (as object) to int64
        Assert.Equal(customObject.AbstractData["TestKey2"], deserializedObject.AbstractData["TestKey2"]);
        Assert.Equal(customObject.SubClass.Ints.ElementAt(3), deserializedObject.SubClass.Ints.ElementAt(3));
    }

    [Fact]
    public void NewtonsoftJsonProvider_SerializeDeserialize_Stream()
    {
        var customObject = new MyCustomClass();

        var memoryStream = new MemoryStream();
        _newtonsoftProvider.Serialize(memoryStream, customObject);
        Assert.False(memoryStream.Length == 0);

        var deserializedObject = _newtonsoftProvider.Deserialize<MyCustomClass>(memoryStream);
        Assert.NotNull(deserializedObject);

        // Spot Check
        Assert.Equal(customObject.MyString, deserializedObject.MyString);
        Assert.Equal(customObject.EmbeddedClass.HappyByte, deserializedObject.EmbeddedClass.HappyByte);
        //Assert.Equal(customObject.Data["TestKey"], deserializedObject.Data["TestKey"]); // Not Supported Yet
        //Assert.Equal(customObject.AbstractData["TestKey2"], deserializedObject.AbstractData["TestKey2"]); // Not Supported Yet
        Assert.Equal(customObject.SubClass.Ints.ElementAt(3), deserializedObject.SubClass.Ints.ElementAt(3));
    }

    [Fact]
    public void MessagePackProvider_SerializeDeserialize()
    {
        var customObject = new MyCustomClass();

        Assert.Throws<MessagePackSerializationException>(() => _messagePackProvider.Serialize(customObject));
    }

    [Fact]
    public void MessagePackProvider_SerializeDeserialize_WithMessagePackObject()
    {
        var customObject = new MyCustomClass2();

        var data = _messagePackProvider.Serialize(customObject);
        Assert.False(data.Length == 0);

        var deserializedObject = _messagePackProvider.Deserialize<MyCustomClass2>(data);
        Assert.NotNull(deserializedObject);

        // Spot Check
        Assert.Equal(customObject.MyString, deserializedObject.MyString);
        Assert.Equal(customObject.EmbeddedClass.HappyByte, deserializedObject.EmbeddedClass.HappyByte);
        Assert.Equal(customObject.Data["TestKey"], Convert.ToInt32(deserializedObject.Data["TestKey"])); // deserialized int32 (as object) to double
        Assert.Equal(customObject.AbstractData["TestKey2"], deserializedObject.AbstractData["TestKey2"]);
        Assert.Equal(customObject.SubClass.Ints.ElementAt(3), deserializedObject.SubClass.Ints.ElementAt(3));
    }
}
