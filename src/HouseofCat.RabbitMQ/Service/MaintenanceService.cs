using HouseofCat.RabbitMQ.Pools;
using HouseofCat.Utilities.Errors;
using RabbitMQ.Client;
using System.Threading.Tasks;

namespace CookedRabbit.Core.Service
{
    public interface IMaintenanceService
    {
        Task<bool> PurgeQueueAsync(IChannelPool channelPool, string queueName, bool deleteQueueAfter = false);
        Task<bool> TransferAllMessagesAsync(IChannelPool originChannelPool, IChannelPool targetChannelPool, string originQueueName, string targetQueueName);
        Task<bool> TransferAllMessagesAsync(IChannelPool channelPool, string originQueueName, string targetQueueName);
        Task<bool> TransferMessageAsync(IChannelPool originChannelPool, IChannelPool targetChannelPool, string originQueueName, string targetQueueName);
        Task<bool> TransferMessageAsync(IChannelPool channelPool, string originQueueName, string targetQueueName);
    }

    public class MaintenanceService : IMaintenanceService
    {
        public async Task<bool> PurgeQueueAsync(
            IChannelPool channelPool,
            string queueName,
            bool deleteQueueAfter = false)
        {
            Guard.AgainstNull(channelPool, nameof(channelPool));
            Guard.AgainstNullOrEmpty(queueName, nameof(queueName));

            var error = false;
            var channelHost = await channelPool.GetChannelAsync().ConfigureAwait(false);

            try
            {
                channelHost.GetChannel().QueuePurge(queueName);

                if (deleteQueueAfter)
                {
                    channelHost.GetChannel().QueueDelete(queueName, false, false);
                }
            }
            catch { error = true; }
            finally
            {
                await channelPool
                    .ReturnChannelAsync(channelHost, error);
            }

            return error;
        }

        public async Task<bool> TransferMessageAsync(
            IChannelPool channelPool,
            string originQueueName,
            string targetQueueName)
        {
            Guard.AgainstNull(channelPool, nameof(channelPool));
            Guard.AgainstNullOrEmpty(originQueueName, nameof(originQueueName));
            Guard.AgainstNullOrEmpty(targetQueueName, nameof(targetQueueName));

            var error = false;
            var channelHost = await channelPool.GetChannelAsync().ConfigureAwait(false);
            var properties = channelHost.GetChannel().CreateBasicProperties();
            properties.DeliveryMode = 2;

            try
            {
                var result = channelHost.GetChannel().BasicGet(originQueueName, true);

                if (result?.Body != null)
                {
                    channelHost.GetChannel().BasicPublish(string.Empty, targetQueueName, false, properties, result.Body);
                }
            }
            catch { error = true; }
            finally
            {
                await channelPool
                    .ReturnChannelAsync(channelHost, error);
            }

            return error;
        }

        public async Task<bool> TransferMessageAsync(
            IChannelPool originChannelPool,
            IChannelPool targetChannelPool,
            string originQueueName,
            string targetQueueName)
        {
            Guard.AgainstNull(originChannelPool, nameof(originChannelPool));
            Guard.AgainstNull(targetChannelPool, nameof(targetChannelPool));
            Guard.AgainstNullOrEmpty(originQueueName, nameof(originQueueName));
            Guard.AgainstNullOrEmpty(targetQueueName, nameof(targetQueueName));

            var error = false;
            var channelHost = await originChannelPool.GetChannelAsync().ConfigureAwait(false);
            var properties = channelHost.GetChannel().CreateBasicProperties();
            properties.DeliveryMode = 2;

            BasicGetResult result = null;
            try
            {
                result = channelHost.GetChannel().BasicGet(originQueueName, true);
            }
            catch { error = true; }
            finally
            {
                await originChannelPool
                    .ReturnChannelAsync(channelHost, error);
            }

            if (!error && result?.Body != null)
            {
                try
                {
                    var targetChannelHost = await targetChannelPool.GetChannelAsync().ConfigureAwait(false);
                    targetChannelHost.GetChannel().BasicPublish(string.Empty, targetQueueName, false, properties, result.Body);
                }
                catch { error = true; }
                finally
                {
                    await targetChannelPool
                        .ReturnChannelAsync(channelHost, error);
                }
            }

            return error;
        }

        public async Task<bool> TransferAllMessagesAsync(
            IChannelPool channelPool,
            string originQueueName,
            string targetQueueName)
        {
            Guard.AgainstNull(channelPool, nameof(channelPool));
            Guard.AgainstNullOrEmpty(originQueueName, nameof(originQueueName));
            Guard.AgainstNullOrEmpty(targetQueueName, nameof(targetQueueName));

            var error = false;
            var channelHost = await channelPool.GetChannelAsync().ConfigureAwait(false);
            var properties = channelHost.GetChannel().CreateBasicProperties();
            properties.DeliveryMode = 2;

            try
            {
                BasicGetResult result = null;

                while (true)
                {
                    result = channelHost.GetChannel().BasicGet(originQueueName, true);
                    if (result == null) { break; }

                    if (result?.Body != null)
                    {
                        channelHost.GetChannel().BasicPublish(string.Empty, targetQueueName, false, properties, result.Body);
                    }
                }
            }
            catch { error = true; }
            finally
            {
                await channelPool
                    .ReturnChannelAsync(channelHost, error);
            }

            return error;
        }

        public async Task<bool> TransferAllMessagesAsync(
            IChannelPool originChannelPool,
            IChannelPool targetChannelPool,
            string originQueueName,
            string targetQueueName)
        {
            Guard.AgainstNull(originChannelPool, nameof(originChannelPool));
            Guard.AgainstNull(targetChannelPool, nameof(targetChannelPool));
            Guard.AgainstNullOrEmpty(originQueueName, nameof(originQueueName));
            Guard.AgainstNullOrEmpty(targetQueueName, nameof(targetQueueName));

            var error = false;
            var channelHost = await originChannelPool.GetChannelAsync().ConfigureAwait(false);
            var properties = channelHost.GetChannel().CreateBasicProperties();
            properties.DeliveryMode = 2;

            BasicGetResult result = null;

            while (true)
            {
                try
                {
                    result = channelHost.GetChannel().BasicGet(originQueueName, true);
                    if (result == null) { break; }
                }
                catch { error = true; }
                finally
                {
                    await originChannelPool
                        .ReturnChannelAsync(channelHost, error);
                }

                if (!error && result?.Body != null)
                {
                    try
                    {
                        var targetChannelHost = await targetChannelPool.GetChannelAsync().ConfigureAwait(false);
                        targetChannelHost.GetChannel().BasicPublish(string.Empty, targetQueueName, false, properties, result.Body);
                    }
                    catch { error = true; }
                    finally
                    {
                        await targetChannelPool
                            .ReturnChannelAsync(channelHost, error);
                    }
                }
            }

            return error;
        }
    }
}
