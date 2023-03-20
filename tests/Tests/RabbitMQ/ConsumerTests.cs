using HouseofCat.Dataflows.Pipelines;
using HouseofCat.RabbitMQ;
using HouseofCat.RabbitMQ.WorkState.Extensions;
using HouseofCat.Utilities.File;
using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using HouseofCat.RabbitMQ.WorkState;
using Xunit;
using Xunit.Abstractions;

namespace RabbitMQ
{
    public class ConsumerTests : IClassFixture<RabbitFixture>
    {
        private readonly RabbitFixture _fixture;

        public class MessageState : RabbitWorkState
        {
            public string MessageAsString => Encoding.UTF8.GetString(ReceivedData.Data);
            public bool AcknowledgeSuccess { get; set; }
        }

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

            await (await _fixture.TopologerAsync).CreateQueueAsync("TestConsumerQueue").ConfigureAwait(false);
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

            for (var i = 0; i < 1000; i++)
            {
                var con = new Consumer(await _fixture.ChannelPoolAsync, "TestMessageConsumer");

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

            var consumer = (await _fixture.RabbitServiceAsync).GetConsumer("TestMessageConsumer");

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

            var consumerPipeline = (await _fixture.RabbitServiceAsync)
                .CreateConsumerPipeline<WorkState>("TestMessageConsumer", 100, false, BuildPipeline);

            for (var i = 0; i < 100; i++)
            {
                await consumerPipeline.StartAsync(true);
                await consumerPipeline.StopAsync();
            }
        }

        [Fact]
        public async Task ConsumerChannelBlockTesting()
        {
            if (!await _fixture.RabbitConnectionCheckAsync)
            {
                return;
            }

            var consumer = (await _fixture.RabbitServiceAsync).GetConsumer("TestMessageConsumer");
            await consumer.StartConsumerAsync();

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

            var consumer = (await _fixture.RabbitServiceAsync).GetConsumer("TestMessageConsumer");
            await consumer.StartConsumerAsync();

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

            var consumer = (await _fixture.RabbitServiceAsync).GetConsumer("TestMessageConsumer");
            await consumer.StartConsumerAsync();

            _ = Task.Run(
                async () =>
                {
                    await Task.Delay(1000);
                    await consumer.StopConsumerAsync();
                });

            await consumer.DirectChannelExecutionEngineAsync(ProcessMessageAsync, FinaliseAsync);
        }

        public async Task<bool> TryProcessMessageAsync(ReceivedData data)
        {
            var state = await ProcessMessageAsync(data);
            return state is MessageState { AcknowledgeSuccess: true };
        }

        public async Task<IRabbitWorkState> ProcessMessageAsync(ReceivedData data)
        {
            var state = new MessageState { ReceivedData = data };
            await Console.Out.WriteLineAsync(state.MessageAsString);
            state.AcknowledgeSuccess = data.AckMessage();
            return state;
        }

        public async Task FinaliseAsync(IRabbitWorkState state)
        {
            await Task.Yield();
            state.ReceivedData?.Complete();
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

            pipeline
                .Finalize((state) =>
                {
                    // Lastly mark the excution pipeline finished for this message.
                    state.ReceivedData?.Complete(); // This impacts wait to completion step in the Pipeline.
                });

            return pipeline;
        }

        public class Message
        {
            public long MessageId { get; set; }
            public string StringMessage { get; set; }
        }

        public class WorkState : HouseofCat.RabbitMQ.WorkState.RabbitWorkState
        {
            public Message Message { get; set; }
            public ulong LetterId { get; set; }
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
                state.Message = state.ReceivedData.ContentType switch
                {
                    HouseofCat.RabbitMQ.Constants.HeaderValueForLetter =>
                        _fixture.SerializationProvider
                        .Deserialize<Message>(state.ReceivedData.Letter.Body),

                    _ => _fixture.SerializationProvider
                        .Deserialize<Message>(state.ReceivedData.Data)
                };

                if (state.ReceivedData.Data.Length > 0 && state.Message != null && state.ReceivedData.Letter != null)
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
    }
}
