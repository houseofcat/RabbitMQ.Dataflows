using HouseofCat.Compression.Builtin;
using HouseofCat.Encryption;
using HouseofCat.Utilities;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace HouseofCat.RabbitMQ
{
    public interface IReceivedData
    {
        bool Ackable { get; }
        IModel Channel { get; set; }
        string ContentType { get; }
        byte[] Data { get; set; }
        ulong DeliveryTag { get; }
        Letter Letter { get; }
        IBasicProperties Properties { get; }

        bool AckMessage();
        void Complete();
        Task<bool> Completion();
        Task<ReadOnlyMemory<byte>> GetBodyAsync(bool decrypt = false, bool decompress = false);
        Task<string> GetBodyAsUtf8StringAsync(bool decrypt = false, bool decompress = false);
        Task<TResult> GetTypeFromJsonAsync<TResult>(bool decrypt = false, bool decompress = false, JsonSerializerOptions jsonSerializerOptions = null);
        Task<IEnumerable<TResult>> GetTypesFromJsonAsync<TResult>(bool decrypt = false, bool decompress = false, JsonSerializerOptions jsonSerializerOptions = null);
        bool NackMessage(bool requeue);
        bool RejectMessage(bool requeue);
    }

    public class ReceivedData : IReceivedData, IDisposable
    {
        public IBasicProperties Properties { get; }
        public bool Ackable { get; }
        public IModel Channel { get; set; }
        public ulong DeliveryTag { get; }
        public byte[] Data { get; set; }
        public Letter Letter { get; private set; }

        // Headers
        public string ContentType { get; private set; }
        public bool Encrypted { get; private set; }
        public string EncryptionType { get; private set; }
        public DateTime EncryptedDateTime { get; private set; }
        public bool Compressed { get; private set; }
        public string CompressionType { get; private set; }

        private TaskCompletionSource<bool> _completionSource = new TaskCompletionSource<bool>();
        private bool _disposedValue;
        private readonly ReadOnlyMemory<byte> _hashKey;

        public ReceivedData(
            IModel channel,
            BasicGetResult result,
            bool ackable,
            ReadOnlyMemory<byte> hashKey)
        {
            Ackable = ackable;
            Channel = channel;
            DeliveryTag = result.DeliveryTag;
            Properties = result.BasicProperties;
            Data = result.Body.ToArray();
            _hashKey = hashKey;

            ReadHeaders();
        }

        public ReceivedData(
            IModel channel,
            BasicDeliverEventArgs args,
            bool ackable,
            ReadOnlyMemory<byte> hashKey)
        {
            Ackable = ackable;
            Channel = channel;
            DeliveryTag = args.DeliveryTag;
            Properties = args.BasicProperties;
            Data = args.Body.ToArray();
            _hashKey = hashKey;

            ReadHeaders();
        }

        private void ReadHeaders()
        {
            if (Properties?.Headers != null && Properties.Headers.ContainsKey(Constants.HeaderForObjectType))
            {
                ContentType = Encoding.UTF8.GetString((byte[])Properties.Headers[Constants.HeaderForObjectType]);

                if (Properties.Headers.ContainsKey(Constants.HeaderForEncrypted))
                { Encrypted = bool.Parse(Encoding.UTF8.GetString((byte[])Properties.Headers[Constants.HeaderForEncrypted])); }

                if (Properties.Headers.ContainsKey(Constants.HeaderForEncryption))
                { EncryptionType = Encoding.UTF8.GetString((byte[])Properties.Headers[Constants.HeaderForEncryption]); }

                if (Properties.Headers.ContainsKey(Constants.HeaderForEncryptDate))
                { EncryptedDateTime = DateTime.Parse(Encoding.UTF8.GetString((byte[])Properties.Headers[Constants.HeaderForEncryptDate])); }

                if (Properties.Headers.ContainsKey(Constants.HeaderForCompressed))
                { Compressed = bool.Parse(Encoding.UTF8.GetString((byte[])Properties.Headers[Constants.HeaderForCompressed])); }

                if (Properties.Headers.ContainsKey(Constants.HeaderForCompression))
                { CompressionType = Encoding.UTF8.GetString((byte[])Properties.Headers[Constants.HeaderForCompression]); }
            }
            else
            {
                ContentType = Constants.HeaderValueForUnknown;
            }
        }

        /// <summary>
        /// Acknowledges the message server side.
        /// </summary>
        public bool AckMessage()
        {
            var success = true;

            if (Ackable)
            {
                try
                {
                    Channel?.BasicAck(DeliveryTag, false);
                    Channel = null;
                }
                catch { success = false; }
            }

            return success;
        }

        /// <summary>
        /// Negative Acknowledges the message server side with option to requeue.
        /// </summary>
        public bool NackMessage(bool requeue)
        {
            var success = true;

            if (Ackable)
            {
                try
                {
                    Channel?.BasicNack(DeliveryTag, false, requeue);
                    Channel = null;
                }
                catch { success = false; }
            }

            return success;
        }

        /// <summary>
        /// Reject Message server side with option to requeue.
        /// </summary>
        public bool RejectMessage(bool requeue)
        {
            var success = true;

            if (Ackable)
            {
                try
                {
                    Channel?.BasicReject(DeliveryTag, requeue);
                    Channel = null;
                }
                catch { success = false; }
            }

            return success;
        }

        /// <summary>
        /// Use this method to retrieve the internal buffer as byte[]. Decomcrypts only apply to non-Letter data.
        /// <para>Combine this with AMQP header X-CR-OBJECTTYPE to get message wrapper payloads.</para>
        /// <para>Header Example: ("X-CR-OBJECTTYPE", "LETTER")</para>
        /// <para>Header Example: ("X-CR-OBJECTTYPE", "MESSAGE")</para>
        /// <em>Note: Always decomrypts Letter bodies to get type regardless of parameters.</em>
        /// </summary>
        public async Task<ReadOnlyMemory<byte>> GetBodyAsync(bool decrypt = false, bool decompress = false)
        {
            switch (ContentType)
            {
                case Constants.HeaderValueForLetter:

                    await CreateLetterFromDataAsync().ConfigureAwait(false);

                    return Letter.Body;

                case Constants.HeaderValueForMessage:
                default:

                    await DecomcryptDataAsync(decrypt, decompress).ConfigureAwait(false);

                    return Data;
            }
        }

        /// <summary>
        /// Use this method to retrieve the internal buffer as string. Decomcrypts only apply to non-Letter data.
        /// <para>Combine this with AMQP header X-CR-OBJECTTYPE to get message wrapper payloads.</para>
        /// <para>Header Example: ("X-CR-OBJECTTYPE", "LETTER")</para>
        /// <para>Header Example: ("X-CR-OBJECTTYPE", "MESSAGE")</para>
        /// <em>Note: Always decomrypts Letter bodies to get type regardless of parameters.</em>
        /// </summary>
        public async Task<string> GetBodyAsUtf8StringAsync(bool decrypt = false, bool decompress = false)
        {
            switch (ContentType)
            {
                case Constants.HeaderValueForLetter:

                    await CreateLetterFromDataAsync().ConfigureAwait(false);

                    return Encoding.UTF8.GetString(Letter.Body);

                case Constants.HeaderValueForMessage:
                default:

                    await DecomcryptDataAsync(decrypt, decompress).ConfigureAwait(false);

                    return Encoding.UTF8.GetString(Data);
            }
        }

        /// <summary>
        /// Use this method to attempt to deserialize into your type based on internal buffer. Decomcrypts only apply to non-Letter data.
        /// <para>Combine this with AMQP header X-CR-OBJECTTYPE to get message wrapper payloads.</para>
        /// <para>Header Example: ("X-CR-OBJECTTYPE", "LETTER")</para>
        /// <para>Header Example: ("X-CR-OBJECTTYPE", "MESSAGE")</para>
        /// <em>Note: Always decomcrypts Letter bodies to get type regardless of parameters.</em>
        /// </summary>
        /// <typeparam name="TResult"></typeparam>
        /// <param name="decrypt"></param>
        /// <param name="decompress"></param>
        /// <param name="jsonSerializerOptions"></param>
        public async Task<TResult> GetTypeFromJsonAsync<TResult>(bool decrypt = false, bool decompress = false, JsonSerializerOptions jsonSerializerOptions = null)
        {
            switch (ContentType)
            {
                case Constants.HeaderValueForLetter:

                    await CreateLetterFromDataAsync().ConfigureAwait(false);

                    return JsonSerializer.Deserialize<TResult>(Letter.Body.AsSpan(), jsonSerializerOptions);

                case Constants.HeaderValueForMessage:
                default:

                    if (Bytes.IsJson(Data))
                    {
                        await DecomcryptDataAsync(decrypt, decompress).ConfigureAwait(false);

                        return JsonSerializer.Deserialize<TResult>(Data.AsSpan(), jsonSerializerOptions);
                    }
                    else
                    { return default; }
            }
        }

        /// <summary>
        /// Use this method to attempt to deserialize into your types based on internal buffer. Decomcrypts only apply to non-Letter data.
        /// <para>Combine this with AMQP header X-CR-OBJECTTYPE to get message wrapper payloads.</para>
        /// <para>Header Example: ("X-CR-OBJECTTYPE", "LETTER")</para>
        /// <para>Header Example: ("X-CR-OBJECTTYPE", "MESSAGE")</para>
        /// <em>Note: Always decomcrypts Letter bodies to get type regardless of parameters.</em>
        /// </summary>
        /// <typeparam name="TResult"></typeparam>
        /// <param name="decrypt"></param>
        /// <param name="decompress"></param>
        /// <param name="jsonSerializerOptions"></param>
        public async Task<IEnumerable<TResult>> GetTypesFromJsonAsync<TResult>(bool decrypt = false, bool decompress = false, JsonSerializerOptions jsonSerializerOptions = null)
        {
            switch (ContentType)
            {
                case Constants.HeaderValueForLetter:

                    await CreateLetterFromDataAsync().ConfigureAwait(false);

                    return JsonSerializer.Deserialize<List<TResult>>(Letter.Body.AsSpan(), jsonSerializerOptions);

                case Constants.HeaderValueForMessage:
                default:

                    if (Bytes.IsJsonArray(Data))
                    {
                        await DecomcryptDataAsync(decrypt, decompress).ConfigureAwait(false);

                        return JsonSerializer.Deserialize<List<TResult>>(Data, jsonSerializerOptions);
                    }
                    else
                    { return default(List<TResult>); }
            }
        }

        public async Task CreateLetterFromDataAsync()
        {
            if (Letter == null)
            { Letter = JsonSerializer.Deserialize<Letter>(Data); }

            if (Encrypted && Letter.LetterMetadata.Encrypted && _hashKey.Length > 0)
            {
                Letter.Body = AesEncrypt.Aes256Decrypt(Letter.Body, _hashKey);
                Letter.LetterMetadata.Encrypted = false;
                Encrypted = false;
            }

            if (Compressed && Letter.LetterMetadata.Compressed)
            {
                Letter.Body = await Gzip
                    .DecompressAsync(Letter.Body)
                    .ConfigureAwait(false);

                Letter.LetterMetadata.Compressed = false;
                Compressed = false;
            }
        }

        public async Task DecomcryptDataAsync(bool decrypt = false, bool decompress = false)
        {
            if (Encrypted && decrypt && _hashKey.Length > 0)
            {
                Data = AesEncrypt.Aes256Decrypt(Data, _hashKey);
                Encrypted = false;
            }

            if (Compressed && decompress)
            {
                Data = await Gzip
                    .DecompressAsync(Data)
                    .ConfigureAwait(false);
                Compressed = false;
            }
        }

        /// <summary>
        /// A way to indicate this message is fully finished with.
        /// </summary>
        public void Complete() => _completionSource.SetResult(true);

        /// <summary>
        /// A way to await the message until it is marked complete.
        /// </summary>
        public Task<bool> Completion() => _completionSource.Task;

        protected virtual void Dispose(bool disposing)
        {
            if (!_disposedValue)
            {
                if (disposing)
                {
                    _completionSource.Task.Dispose();
                }

                if (Channel != null) { Channel = null; }
                if (Letter != null) { Letter = null; }

                _disposedValue = true;
            }
        }

        public void Dispose()
        {
            // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }
    }
}
