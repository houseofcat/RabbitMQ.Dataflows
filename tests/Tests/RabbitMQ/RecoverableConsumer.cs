using System.Linq;
using System.Threading.Tasks;
using HouseofCat.Logger;
using HouseofCat.RabbitMQ;
using HouseofCat.RabbitMQ.Pools;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client.Events;

namespace IntegrationTests.RabbitMQ
{
    public class RecoverableConsumer : Consumer
    {
        private static class Consumers
        {
            public const string ConsumerRegistered = "Consumer ({0}) registered (CT:{1}).";
            public const string ConsumerAsyncRegistered = "Consumer ({0}) async registered (CT:{1}).";
            public const string ConsumerUnregistered = "Consumer ({0}) unregistered (CT:{1}).";
            public const string ConsumerAsyncUnregistered = "Consumer ({0}) async unregistered (CT:{1}).";
        }
        
        public RecoverableConsumer(RabbitOptions options, string consumerName)
            : base(new ChannelPool(options), consumerName)
        { }

        public RecoverableConsumer(IChannelPool channelPool, string consumerName)
            : base(channelPool, channelPool.Options.GetConsumerOptions(consumerName))
        { }

        public RecoverableConsumer(IChannelPool channelPool, ConsumerOptions consumerOptions)
            : base(channelPool, consumerOptions, LogHelper.GetLogger<RecoverableConsumer>())
        { }
        
        protected override void AddEventHandlers(EventingBasicConsumer consumer)
        {
            base.AddEventHandlers(consumer);
            consumer.Registered += ConsumerRegister;
            consumer.Unregistered += ConsumerUnregistered;
        }

        protected override void RemoveEventHandlers(EventingBasicConsumer consumer)
        {
            base.RemoveEventHandlers(consumer);
            consumer.Registered -= ConsumerRegister;
            consumer.Unregistered -= ConsumerUnregistered;
        }

        protected virtual void ConsumerRegister(object _, ConsumerEventArgs args)
        {
            var consumerTag = args.ConsumerTags.First();
            Logger.LogDebug(
                Consumers.ConsumerRegistered,
                ConsumerOptions.ConsumerName,
                consumerTag);
            if (ChanHost is IRecoverableChannelHost recoveryAwareChannelHost)
            {
                recoveryAwareChannelHost.RecordConsumerTag(consumerTag);
            }
        }

        protected virtual void ConsumerUnregistered(object _, ConsumerEventArgs args)
        {
            var consumerTag = args.ConsumerTags.First();
            Logger.LogDebug(
                Consumers.ConsumerAsyncUnregistered,
                ConsumerOptions.ConsumerName,
                consumerTag);
            if (ChanHost is IRecoverableChannelHost recoveryAwareChannelHost)
            {
                recoveryAwareChannelHost.DeleteRecordedConsumerTag(consumerTag);
            }
        }

        protected override void AddAsyncEventHandlers(AsyncEventingBasicConsumer asyncConsumer)
        {
            base.AddAsyncEventHandlers(asyncConsumer);
            asyncConsumer.Registered += ConsumerRegisterAsync;
            asyncConsumer.Unregistered += ConsumerUnregisteredAsync;
        }

        protected override void RemoveAsyncEventHandlers(AsyncEventingBasicConsumer asyncConsumer)
        {
            base.RemoveAsyncEventHandlers(asyncConsumer);
            asyncConsumer.Registered -= ConsumerRegisterAsync;
            asyncConsumer.Unregistered -= ConsumerUnregisteredAsync;
        }

        protected virtual async Task ConsumerRegisterAsync(object _, ConsumerEventArgs args)
        {
            var consumerTag = args.ConsumerTags.First();
            Logger.LogDebug(
                Consumers.ConsumerAsyncRegistered,
                ConsumerOptions.ConsumerName,
                consumerTag);
            if (ChanHost is IRecoverableChannelHost recoveryAwareChannelHost)
            {
                await recoveryAwareChannelHost.RecordConsumerTagAsync(consumerTag).ConfigureAwait(false);
            }
        }

        protected virtual async Task ConsumerUnregisteredAsync(object _, ConsumerEventArgs args)
        {
            var consumerTag = args.ConsumerTags.First();
            Logger.LogDebug(
                Consumers.ConsumerUnregistered,
                ConsumerOptions.ConsumerName,
                consumerTag);
            if (ChanHost is IRecoverableChannelHost recoveryAwareChannelHost)
            {
                await recoveryAwareChannelHost.DeleteRecordedConsumerTagAsync(consumerTag).ConfigureAwait(false);
            }
        }
    }
}
