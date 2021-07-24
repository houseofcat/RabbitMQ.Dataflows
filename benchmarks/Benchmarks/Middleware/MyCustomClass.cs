using HouseofCat.Utilities.Time;
using System.Collections.Generic;

namespace Benchmarks.Middleware
{
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
}
