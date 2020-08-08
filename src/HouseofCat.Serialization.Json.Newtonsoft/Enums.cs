using System;
using System.Collections.Generic;
using System.Text;

namespace HouseofCat.Serialization
{
    public static class Enums
    {
        public enum SerializationMethod
        {
            BuiltinUtf8JsonString,
            BuiltinUtf8Json,
            Utf8Json,
            Utf8TextJson,
            Utf8PrettyTextJson,
            NewtonsoftJsonString,
            Newtonsoft
        }
    }
}
