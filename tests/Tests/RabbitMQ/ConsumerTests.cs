using HouseofCat.Dataflows.Pipelines;
using HouseofCat.RabbitMQ;
using HouseofCat.RabbitMQ.WorkState.Extensions;
using HouseofCat.Utilities.File;
using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using HouseofCat.RabbitMQ.WorkState;
using Xunit;
using Xunit.Abstractions;

namespace RabbitMQ
{
    public class ConsumerTests : IClassFixture<RabbitFixture>
    {
        private readonly RabbitFixture _fixture;

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

            var con = new Consumer(options, "TestMessageConsumer");
            Assert.NotNull(con);
        }

        [Fact]
        public async Task CreateConsumerAndStart()
        {
            if (!await _fixture.RabbitConnectionCheckAsync)
            {
                return;
            }

            var topologer = await _fixture.TopologerAsync;
            await topologer.CreateQueueAsync("TestConsumerQueue").ConfigureAwait(false);
            var con = new Consumer(await _fixture.ChannelPoolAsync, "TestMessageConsumer");
            await con.StartConsumerAsync().ConfigureAwait(false);
        }

        [Fact]
        public async Task CreateConsumerStartAndStop()
        {
            if (!await _fixture.RabbitConnectionCheckAsync)
            {
                return;
            }

            var con = new Consumer(await _fixture.ChannelPoolAsync, "TestMessageConsumer");

            await con.StartConsumerAsync().ConfigureAwait(false);
            await con.StopConsumerAsync().ConfigureAwait(false);
        }

        [Fact]
        public async Task CreateManyConsumersStartAndStop()
        {
            if (!await _fixture.RabbitConnectionCheckAsync)
            {
                return;
            }

            var channelPool = await _fixture.ChannelPoolAsync;
            for (var i = 0; i < 1000; i++)
            {
                var con = new Consumer(channelPool, "TestMessageConsumer");

                await con.StartConsumerAsync().ConfigureAwait(false);
                await con.StopConsumerAsync().ConfigureAwait(false);
            }
        }

        [Fact]
        public async Task ConsumerStartAndStopTesting()
        {
            if (!await _fixture.RabbitConnectionCheckAsync)
            {
                return;
            }

            var service = await _fixture.RabbitServiceAsync;
            var consumer = service.GetConsumer("TestMessageConsumer");

            for (var i = 0; i < 100; i++)
            {
                await consumer.StartConsumerAsync().ConfigureAwait(false);
                await consumer.StopConsumerAsync().ConfigureAwait(false);
            }
        }

        [Fact]
        public async Task ConsumerPipelineStartAndStopTesting()
        {
            if (!await _fixture.RabbitConnectionCheckAsync)
            {
                return;
            }

            var service = await _fixture.RabbitServiceAsync;
            var consumer = service.GetConsumer("TestMessageConsumer");
            await consumer.StartConsumerAsync();

            await PublishRandomLetter();

            var consumerPipeline = service.CreateConsumerPipeline("TestMessageConsumer", 100, false, BuildPipeline);
            await Task.WhenAll(Enumerable.Range(0, 100).Select(async _ =>
            {
                await consumerPipeline.StartAsync(true);
                await Task.Delay(500);
                await consumerPipeline.StopAsync();
            }));
        }

        [Fact]
        public async Task ConsumerChannelBlockTesting()
        {
            if (!await _fixture.RabbitConnectionCheckAsync)
            {
                return;
            }

            var service = await _fixture.RabbitServiceAsync;
            var consumer = service.GetConsumer("TestMessageConsumer");
            await consumer.StartConsumerAsync();

            await PublishRandomLetter();

            _ = Task.Run(
                async () =>
                {
                    await Task.Delay(1000);
                    await consumer.StopConsumerAsync();
                });

            await consumer.ChannelExecutionEngineAsync(TryProcessMessageAsync);
        }

        [Fact]
        public async Task ConsumerDirectChannelBlockTesting()
        {
            if (!await _fixture.RabbitConnectionCheckAsync)
            {
                return;
            }

            var service = await _fixture.RabbitServiceAsync;
            var consumer = service.GetConsumer("TestMessageConsumer");
            await consumer.StartConsumerAsync();

            await PublishRandomLetter();

            _ = Task.Run(
                async () =>
                {
                    await Task.Delay(1000);
                    await consumer.StopConsumerAsync();
                });

            await consumer.DirectChannelExecutionEngineAsync(TryProcessMessageAsync);
        }

        [Fact]
        public async Task ConsumerDirectChannelReaderBlockTesting()
        {
            if (!await _fixture.RabbitConnectionCheckAsync)
            {
                return;
            }

            var service = await _fixture.RabbitServiceAsync;
            var consumer = service.GetConsumer("TestMessageConsumer");
            await consumer.StartConsumerAsync();

            await PublishRandomLetter();

            _ = Task.Run(
                async () =>
                {
                    await Task.Delay(1000);
                    await consumer.StopConsumerAsync();
                });

            await consumer.DirectChannelExecutionEngineAsync(ProcessMessageAsync, FinaliseAsync);
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

        private async Task PublishRandomLetter()
        {
            var letter = MessageExtensions.CreateSimpleRandomLetter("TestRabbitServiceQueue", 2000);
            var publisher = await _fixture.PublisherAsync;
            await publisher.PublishAsync(letter, false).ConfigureAwait(false);
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

        private WorkState DeserializeStep(IReceivedData receivedData)
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

        private async Task<WorkState> ProcessStepAsync(WorkState state)
        {
            await Task.Yield();

            if (state.DeserializeStepSuccess)
            {
                state.ProcessStepSuccess = true;
            }

            return state;
        }

        private async Task<WorkState> AckMessageAsync(WorkState state)
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

        private async Task FinaliseAsync(IRabbitWorkState state)
        {
            await Task.Yield();
            // Lastly mark the excution pipeline finished for this message.
            state.ReceivedData?.Complete(); // This impacts wait to completion step in the Pipeline.
        }
    }
}
