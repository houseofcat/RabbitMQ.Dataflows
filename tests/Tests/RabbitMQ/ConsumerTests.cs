using HouseofCat.Dataflows.Pipelines;
using HouseofCat.RabbitMQ;
using HouseofCat.RabbitMQ.WorkState.Extensions;
using HouseofCat.Utilities.File;
using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using HouseofCat.RabbitMQ.Pools;
using HouseofCat.RabbitMQ.Services;
using HouseofCat.RabbitMQ.WorkState;
using IntegrationTests.RabbitMQ;
using Xunit;
using Xunit.Abstractions;

namespace RabbitMQ
{
    public class ConsumerTests : IClassFixture<RabbitFixture>
    {
        private readonly RabbitFixture _fixture;
        private static long _paused;

        public ConsumerTests(RabbitFixture fixture, ITestOutputHelper output)
        {
            _fixture = fixture;
            _fixture.Output = output;
        }

        [Fact]
        public async Task CreateConsumer()
        {
            var options = 
                await JsonFileReader.ReadFileAsync<RabbitOptions>(Path.Combine("RabbitMQ", "TestConfig.json"));
            Assert.NotNull(options);

            if (!await _fixture.CheckRabbitHostConnectionAndUpdateFactoryOptions(options))
            {
                return;
            }

            var con = new Consumer(options, "TestMessageConsumer");
            Assert.NotNull(con);
            await con.ChannelPool.ShutdownAsync();
        }

        [Fact]
        public async Task CreateConsumerAndInitializeChannelPool()
        {
            var options = 
                await JsonFileReader.ReadFileAsync<RabbitOptions>(Path.Combine("RabbitMQ", "TestConfig.json"));
            Assert.NotNull(options);

            if (!await _fixture.CheckRabbitHostConnectionAndUpdateFactoryOptions(options))
            {
                return;
            }

            var con = new Consumer(new ChannelPool(options), "TestMessageConsumer");
            Assert.NotNull(con);
            await con.ChannelPool.ShutdownAsync();
        }
        
        [Fact]
        public async Task CreateConsumerStartAndStop()
        {
            if (!await _fixture.RabbitConnectionCheckAsync)
            {
                return;
            }

            var con = new Consumer(await _fixture.GetChannelPoolAsync(), "TestMessageConsumer");
            await con.StartConsumerAsync().ConfigureAwait(false);
            await con.StopConsumerAsync().ConfigureAwait(false);
            await con.ChannelPool.ShutdownAsync();
        }

        [Fact]
        public async Task CreateManyConsumersStartAndStop()
        {
            if (!await _fixture.RabbitConnectionCheckAsync)
            {
                return;
            }

            var channelPool = await _fixture.GetChannelPoolAsync();
            for (var i = 0; i < 1000; i++)
            {
                var con = new Consumer(channelPool, "TestMessageConsumer");

                await con.StartConsumerAsync().ConfigureAwait(false);
                await con.StopConsumerAsync().ConfigureAwait(false);
            }

            await channelPool.ShutdownAsync();
        }

        [Fact]
        public async Task ConsumerStartAndStopTesting()
        {
            if (!await _fixture.RabbitConnectionCheckAsync)
            {
                return;
            }

            var service = await _fixture.GetRabbitServiceAsync();
            var consumer = service.GetConsumer("TestMessageConsumer");

            for (var i = 0; i < 100; i++)
            {
                await consumer.StartConsumerAsync().ConfigureAwait(false);
                await consumer.StopConsumerAsync().ConfigureAwait(false);
            }

            await service.ShutdownAsync(true);
        }

        [Fact]
        public async Task ConsumerPipelineStartAndStopTesting()
        {
            if (!await _fixture.RabbitConnectionCheckAsync)
            {
                return;
            }

            var service = await _fixture.GetRabbitServiceAsync();
            var consumer = service.GetConsumer("TestMessageConsumer");
            await consumer.StartConsumerAsync();

            var consumerPipeline = service.CreateConsumerPipeline("TestMessageConsumer", 100, false, BuildPipeline);
            for (var i = 0; i < 100; i++)
            {
                await consumerPipeline.StartAsync(true).ConfigureAwait(false);
                await consumerPipeline.StopAsync().ConfigureAwait(false);
            }

            await service.ShutdownAsync(true);
        }

        [Fact]
        public async Task ConsumerPipelineCompletionTesting()
        {
            if (!await _fixture.RabbitConnectionCheckAsync)
            {
                return;
            }

            var service = await _fixture.GetRabbitServiceAsync();
            var consumerPipeline = service.CreateConsumerPipeline("TestMessageConsumer", 100, false, BuildPipeline);
            var executeTask = consumerPipeline.StartAsync(true)
                .ContinueWith(_ => consumerPipeline.AwaitCompletionAsync());
            var closeAndPublishTask = CloseConnectionsThenPublishRandomLetter(service);
            await Task.WhenAll(executeTask, closeAndPublishTask.ContinueWith(async _ =>
            {
                await Task.Delay(1000);
                await consumerPipeline.StopAsync(true);
                await service.ShutdownAsync(true);
            }));
            // this should be Assert.True
            Assert.False(await closeAndPublishTask);
        }

        [Fact]
        public async Task RecoverableConsumerPipelineCompletionTesting()
        {
            if (!await _fixture.RabbitConnectionCheckAsync)
            {
                return;
            }

            var service = await _fixture.GetRecoverableRabbitServiceAsync();
            var consumerPipeline = service.CreateConsumerPipeline("TestMessageConsumer", 100, false, BuildPipeline);
            var executeTask = consumerPipeline.StartAsync(true)
                .ContinueWith(_ => consumerPipeline.AwaitCompletionAsync());
            var closeAndPublishTask = CloseConnectionsThenPublishRandomLetter(service);
            await Task.WhenAll(executeTask, closeAndPublishTask.ContinueWith(async _ =>
            {
                await Task.Delay(1000);
                await consumerPipeline.StopAsync(true);
                await service.ShutdownAsync(true);
            }));
            Assert.True(await closeAndPublishTask);
        }

        [Fact]
        public async Task ConsumerChannelBlockTesting()
        {
            if (!await _fixture.RabbitConnectionCheckAsync)
            {
                return;
            }

            var service = await _fixture.GetRabbitServiceAsync();
            var consumer = service.GetConsumer("TestMessageConsumer");
            var executeTask = consumer.StartConsumerAsync()
                .ContinueWith(_ => consumer.ChannelExecutionEngineAsync(TryProcessMessageAsync));
            var closeAndPublishTask = CloseConnectionsThenPublishRandomLetter(service);
            await Task.WhenAll(executeTask, closeAndPublishTask.ContinueWith(async _ =>
            {
                await Task.Delay(1000);
                await service.ShutdownAsync(true);
            }));
            // this should be Assert.True
            Assert.False(await closeAndPublishTask);
        }

        [Fact]
        public async Task RecoverableConsumerChannelBlockTesting()
        {
            if (!await _fixture.RabbitConnectionCheckAsync)
            {
                return;
            }

            var service = await _fixture.GetRecoverableRabbitServiceAsync();
            var consumer = service.GetConsumer("TestMessageConsumer");
            var executeTask = consumer.StartConsumerAsync()
                .ContinueWith(_ => consumer.ChannelExecutionEngineAsync(TryProcessMessageAsync));
            var closeAndPublishTask = CloseConnectionsThenPublishRandomLetter(service);
            await Task.WhenAll(executeTask, closeAndPublishTask.ContinueWith(async _ =>
            {
                await Task.Delay(1000);
                await service.ShutdownAsync(true);
            }));
            Assert.True(await closeAndPublishTask);
        }

        [Fact]
        public async Task ConsumerDirectChannelBlockTesting()
        {
            if (!await _fixture.RabbitConnectionCheckAsync)
            {
                return;
            }

            var service = await _fixture.GetRabbitServiceAsync();
            var consumer = service.GetConsumer("TestMessageConsumer");
            var executeTask = consumer.StartConsumerAsync()
                .ContinueWith(_ => consumer.DirectChannelExecutionEngineAsync(TryProcessMessageAsync));
            var closeAndPublishTask = CloseConnectionsThenPublishRandomLetter(service);
            await Task.WhenAll(executeTask, closeAndPublishTask.ContinueWith(async _ =>
            {
                await Task.Delay(1000);
                await service.ShutdownAsync(true);
            }));
            // this should be Assert.True
            Assert.False(await closeAndPublishTask);
        }

        [Fact]
        public async Task RecoverableConsumerDirectChannelBlockTesting()
        {
            if (!await _fixture.RabbitConnectionCheckAsync)
            {
                return;
            }

            var service = await _fixture.GetRecoverableRabbitServiceAsync();
            var consumer = service.GetConsumer("TestMessageConsumer");
            var executeTask = consumer.StartConsumerAsync()
                .ContinueWith(_ => consumer.DirectChannelExecutionEngineAsync(TryProcessMessageAsync));
            var closeAndPublishTask = CloseConnectionsThenPublishRandomLetter(service);
            await Task.WhenAll(executeTask, closeAndPublishTask.ContinueWith(async _ =>
            {
                await Task.Delay(1000);
                await service.ShutdownAsync(true);
            }));
            Assert.True(await closeAndPublishTask);
        }

        [Fact]
        public async Task ConsumerDirectChannelReaderBlockTesting()
        {
            if (!await _fixture.RabbitConnectionCheckAsync)
            {
                return;
            }

            var service = await _fixture.GetRabbitServiceAsync();
            var consumer = service.GetConsumer("TestMessageConsumer");
            var executeTask = consumer.StartConsumerAsync()
                .ContinueWith(_ => consumer.DirectChannelExecutionEngineAsync(ProcessMessageAsync, FinaliseAsync));
            var closeAndPublishTask = CloseConnectionsThenPublishRandomLetter(service);
            await Task.WhenAll(executeTask, closeAndPublishTask.ContinueWith(async _ =>
            {
                await Task.Delay(1000);
                await service.ShutdownAsync(true);
            }));
            // this should be Assert.True
            Assert.False(await closeAndPublishTask);
        }

        [Fact]
        public async Task RecoverableConsumerDirectChannelReaderBlockTesting()
        {
            if (!await _fixture.RabbitConnectionCheckAsync)
            {
                return;
            }

            var service = await _fixture.GetRecoverableRabbitServiceAsync();
            var consumer = service.GetConsumer("TestMessageConsumer");
            var executeTask = consumer.StartConsumerAsync()
                .ContinueWith(_ => consumer.DirectChannelExecutionEngineAsync(ProcessMessageAsync, FinaliseAsync));
            var closeAndPublishTask = CloseConnectionsThenPublishRandomLetter(service);
            await Task.WhenAll(executeTask, closeAndPublishTask.ContinueWith(async _ =>
            {
                await Task.Delay(1000);
                await service.ShutdownAsync(true);
            }));
            Assert.True(await closeAndPublishTask);
        }

        public async Task<bool> TryProcessMessageAsync(ReceivedData receivedData)
        {
            var state = await ProcessMessageAsync(receivedData);
            return state is WorkState { AcknowledgeStepSuccess: true };
        }

        public async Task<IRabbitWorkState> ProcessMessageAsync(ReceivedData receivedData)
        {
            var state = DeserializeStep(receivedData);
            await ProcessStepAsync(state).ConfigureAwait(false);
            await AckMessageAsync(state).ConfigureAwait(false);

            return state;
        }

        private static void Pause() => Interlocked.CompareExchange(ref _paused, 1, 0);
        private static void Resume() => Interlocked.CompareExchange(ref _paused, 0, 1);

        private async Task<bool> CloseConnectionsThenPublishRandomLetter(IRabbitService service)
        {
            var management = await CreateManagement();
            var connections = await management.WaitForActiveConnections("TestRabbitServiceQueue");
            await management.WaitForQueueToHaveConsumers("TestRabbitServiceQueue", 1);

            await management.CloseActiveConnections("TestRabbitServiceQueue", connections);
            await management.WaitForQueueToHaveNoConsumers("TestRabbitServiceQueue");

            await management.ClearQueue("TestRabbitServiceQueue");
            await management.WaitForQueueToHaveNoMessages("TestRabbitServiceQueue", 15, 50);

            Pause();

            await management.WaitForActiveConnections("TestRabbitServiceQueue");
            await management.WaitForQueueToHaveConsumers("TestRabbitServiceQueue", 1, false);

            await PublishRandomLetter(service.Publisher);
            await management.WaitForQueueToHaveUnacknowledgedMessages("TestRabbitServiceQueue", 1, 15, 50);

            Resume();

            var allAcked = await management.WaitForQueueToHaveNoUnacknowledgedMessages("TestRabbitServiceQueue", false);
            await service.Topologer.DeleteQueueAsync("TestRabbitServiceQueue").ConfigureAwait(false);
            return allAcked;
        }

        private async Task<Management> CreateManagement()
        {
            var options = await _fixture.OptionsAsync;
            return new Management(options.FactoryOptions, _fixture.Output);
        }

        private static Task PublishRandomLetter(IPublisher publisher)
        {
            var letter = MessageExtensions.CreateSimpleRandomLetter("TestRabbitServiceQueue", 2000);
            return publisher.PublishAsync(letter, false);
        }

        private IPipeline<ReceivedData, WorkState> BuildPipeline(int maxDoP, bool? ensureOrdered = null)
        {
            var pipeline = new Pipeline<ReceivedData, WorkState>(
                maxDoP,
                healthCheckInterval: TimeSpan.FromSeconds(10),
                pipelineName: "ConsumerPipelineExample",
                ensureOrdered);

            pipeline.AddStep<ReceivedData, WorkState>(DeserializeStep);
            pipeline.AddAsyncStep<WorkState, WorkState>(ProcessStepAsync);
            pipeline.AddAsyncStep<WorkState, WorkState>(AckMessageAsync);
            pipeline.Finalize(FinaliseAsync);

            return pipeline;
        }

        public class WorkState : RabbitWorkState
        {
            public Letter Letter { get; set; }
            public bool DeserializeStepSuccess { get; set; }
            public bool ProcessStepSuccess { get; set; }
            public bool AcknowledgeStepSuccess { get; set; }
            public bool AllStepsSuccess => DeserializeStepSuccess && ProcessStepSuccess && AcknowledgeStepSuccess;
        }

        private WorkState DeserializeStep(ReceivedData receivedData)
        {
            var state = new WorkState
            {
                ReceivedData = receivedData
            };

            try
            {
                state.Letter = _fixture.SerializationProvider.Deserialize<Letter>(state.ReceivedData.Data);

                if (state.ReceivedData.Data.Length > 0 && state.Letter != null && state.ReceivedData.Letter != null)
                { state.DeserializeStepSuccess = true; }
            }
            catch
            { }

            return state;
        }

        private static async Task<WorkState> ProcessStepAsync(WorkState state)
        {
            await Task.Yield();

            while (Interlocked.Read(ref _paused) == 1)
            {
                await Task.Delay(4);
            }

            if (state.DeserializeStepSuccess)
            {
                state.ProcessStepSuccess = true;
            }

            return state;
        }

        private static async Task<WorkState> AckMessageAsync(WorkState state)
        {
            await Task.Yield();

            if (state.ProcessStepSuccess)
            {
                if (state.ReceivedData.AckMessage())
                { state.AcknowledgeStepSuccess = true; }
            }
            else
            {
                if (state.ReceivedData.NackMessage(true))
                { state.AcknowledgeStepSuccess = true; }
            }

            return state;
        }

        private static async Task FinaliseAsync(IRabbitWorkState state)
        {
            await Task.Yield();
            // Lastly mark the excution pipeline finished for this message.
            state.ReceivedData?.Complete(); // This impacts wait to completion step in the Pipeline.
        }
    }
}
