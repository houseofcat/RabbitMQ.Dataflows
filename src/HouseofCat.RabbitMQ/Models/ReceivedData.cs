using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System;
using System.Text;
using System.Threading.Tasks;

namespace HouseofCat.RabbitMQ
{
    public interface IReceivedData
    {
        bool Ackable { get; }
        IModel Channel { get; set; }

        string ContentType { get; }
        bool Encrypted { get; }
        string EncryptionType { get; }
        DateTime EncryptedDateTime { get; }
        bool Compressed { get; }
        string CompressionType { get; }

        byte[] Data { get; set; }
        ulong DeliveryTag { get; }
        Letter Letter { get; set; }

        IBasicProperties Properties { get; }

        bool AckMessage();
        void Complete();
        Task<bool> Completion { get; }

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
        public Letter Letter { get; set; }

        // Headers
        public string ContentType { get; private set; }
        public bool Encrypted { get; private set; }
        public string EncryptionType { get; private set; }
        public DateTime EncryptedDateTime { get; private set; }
        public bool Compressed { get; private set; }
        public string CompressionType { get; private set; }

        private TaskCompletionSource<bool> _completionSource = new TaskCompletionSource<bool>();
        public Task<bool> Completion => _completionSource.Task;

        private bool _disposedValue;

        public ReceivedData(
            IModel channel,
            BasicGetResult result,
            bool ackable)
        {
            Ackable = ackable;
            Channel = channel;
            DeliveryTag = result.DeliveryTag;
            Properties = result.BasicProperties;
            Data = result.Body.ToArray();

            ReadHeaders();
        }

        public ReceivedData(
            IModel channel,
            BasicDeliverEventArgs args,
            bool ackable)
        {
            Ackable = ackable;
            Channel = channel;
            DeliveryTag = args.DeliveryTag;
            Properties = args.BasicProperties;
            Data = args.Body.ToArray();

            ReadHeaders();
        }

        private void ReadHeaders()
        {
            if (Properties?.Headers != null && Properties.Headers.ContainsKey(Constants.HeaderForObjectType))
            {
                ContentType = Encoding.UTF8.GetString((byte[])Properties.Headers[Constants.HeaderForObjectType]);

                if (Properties.Headers.ContainsKey(Constants.HeaderForEncrypted))
                { Encrypted = (bool)Properties.Headers[Constants.HeaderForEncrypted]; }

                if (Properties.Headers.ContainsKey(Constants.HeaderForEncryption))
                { EncryptionType = Encoding.UTF8.GetString((byte[])Properties.Headers[Constants.HeaderForEncryption]); }

                if (Properties.Headers.ContainsKey(Constants.HeaderForEncryptDate))
                { EncryptedDateTime = DateTime.Parse(Encoding.UTF8.GetString((byte[])Properties.Headers[Constants.HeaderForEncryptDate])); }

                if (Properties.Headers.ContainsKey(Constants.HeaderForCompressed))
                { Compressed = (bool)Properties.Headers[Constants.HeaderForCompressed]; }

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
        /// A way to indicate this message is fully finished with.
        /// </summary>
        public void Complete() => _completionSource.SetResult(true);


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
