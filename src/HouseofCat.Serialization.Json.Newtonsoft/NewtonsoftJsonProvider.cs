using HouseofCat.Utilities.Errors;
using Newtonsoft.Json;
using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace HouseofCat.Serialization
{
    public class NewtonsoftJsonProvider : ISerializationProvider
    {
        private JsonSerializer _jsonSerializer = new JsonSerializer();

        public TOut Deserialize<TOut>(ReadOnlyMemory<byte> input)
        {
            Guard.AgainstEmpty(input, nameof(input));

            return JsonConvert.DeserializeObject<TOut>(Encoding.UTF8.GetString(input.Span));
        }

        public TOut Deserialize<TOut>(string input)
        {
            return JsonConvert.DeserializeObject<TOut>(input);
        }

        public TOut Deserialize<TOut>(Stream inputStream)
        {
            Guard.AgainstNullOrEmpty(inputStream, nameof(inputStream));

            if (inputStream.Position == inputStream.Length) { inputStream.Seek(0, SeekOrigin.Begin); }

            var buffer = new Span<byte>(new byte[inputStream.Length]);
            var bytesRead = inputStream.Read(buffer);

            if (bytesRead == 0) throw new InvalidDataException();

            var jsonReader = new JsonTextReader(new StringReader(Encoding.UTF8.GetString(buffer)));

            return _jsonSerializer.Deserialize<TOut>(jsonReader);
        }

        public async Task<TOut> DeserializeAsync<TOut>(Stream inputStream)
        {
            Guard.AgainstNullOrEmpty(inputStream, nameof(inputStream));

            if (inputStream.Position == inputStream.Length) { inputStream.Seek(0, SeekOrigin.Begin); }

            var buffer = new Memory<byte>(new byte[inputStream.Length]);
            var bytesRead = await inputStream.ReadAsync(buffer);

            if (bytesRead == 0) throw new InvalidDataException();

            var jsonReader = new JsonTextReader(new StringReader(Encoding.UTF8.GetString(buffer.Span)));

            return _jsonSerializer.Deserialize<TOut>(jsonReader);
        }

        public byte[] Serialize<TIn>(TIn input)
        {
            return Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(input));
        }

        public void Serialize<TIn>(Stream outputStream, TIn input)
        {
            outputStream.Write(Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(input)));
            outputStream.Seek(0, SeekOrigin.Begin);
        }

        public async Task SerializeAsync<TIn>(Stream outpuStream, TIn input)
        {
            using StreamWriter writer = new StreamWriter(outpuStream, leaveOpen: true);
            using JsonTextWriter jsonWriter = new JsonTextWriter(writer);

            _jsonSerializer.Serialize(jsonWriter, input);
            await jsonWriter.FlushAsync();
        }

        public string SerializeToPrettyString<TIn>(TIn input)
        {
            throw new NotImplementedException();
        }

        public string SerializeToString<TIn>(TIn input)
        {
            return JsonConvert.SerializeObject(input);
        }
    }
}
