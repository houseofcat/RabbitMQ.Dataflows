using HouseofCat.Dataflows.Pipelines;
using HouseofCat.RabbitMQ;
using HouseofCat.Utilities.File;
using System;
using System.Text;
using System.Threading.Tasks;
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

        [Fact(Skip = "only manual")]
        public async Task CreateConsumer()
        {
            var options = await JsonFileReader.ReadFileAsync<RabbitOptions>("TestConfig.json");
            Assert.NotNull(options);

            var con = new Consumer(options, "TestMessageConsumer");
            Assert.NotNull(con);
        }

        [Fact(Skip = "only manual")]
        public async Task CreateConsumerAndInitializeChannelPool()
        {
            var options = await JsonFileReader.ReadFileAsync<RabbitOptions>("TestConfig.json");
            Assert.NotNull(options);

            var con = new Consumer(options, "TestMessageConsumer");
            Assert.NotNull(con);
        }

        [Fact(Skip = "only manual")]
        public async Task CreateConsumerAndStart()
        {
            await _fixture.Topologer.CreateQueueAsync("TestConsumerQueue").ConfigureAwait(false);
            var con = new Consumer(_fixture.ChannelPool, "TestMessageConsumer");
            await con.StartConsumerAsync().ConfigureAwait(false);
        }

        [Fact(Skip = "only manual")]
        public async Task CreateConsumerStartAndStop()
        {
            var con = new Consumer(_fixture.ChannelPool, "TestMessageConsumer");

            await con.StartConsumerAsync().ConfigureAwait(false);
            await con.StopConsumerAsync().ConfigureAwait(false);
        }

        [Fact(Skip = "only manual")]
        public async Task CreateManyConsumersStartAndStop()
        {
            for (int i = 0; i < 1000; i++)
            {
                var con = new Consumer(_fixture.ChannelPool, "TestMessageConsumer");

                await con.StartConsumerAsync().ConfigureAwait(false);
                await con.StopConsumerAsync().ConfigureAwait(false);
            }
        }

        [Fact(Skip = "only manual")]
        public async Task ConsumerStartAndStopTesting()
        {
            var consumer = _fixture.RabbitService.GetConsumer("TestMessageConsumer");

            for (int i = 0; i < 100; i++)
            {
                await consumer.StartConsumerAsync().ConfigureAwait(false);
                await consumer.StopConsumerAsync().ConfigureAwait(false);
            }
        }

        [Fact(Skip = "only manual")]
        public async Task ConsumerPipelineStartAndStopTesting()
        {
            var consumerPipeline = _fixture.RabbitService.CreateConsumerPipeline<WorkState>("TestMessageConsumer", 100, false, BuildPipeline);

            for (int i = 0; i < 100; i++)
            {
                await consumerPipeline.StartAsync(true);
                await consumerPipeline.StopAsync();
            }
        }

        [Fact(Skip = "only manual")]
        public async Task ConsumerChannelBlockTesting()
        {
            var consumer = _fixture.RabbitService.GetConsumer("TestMessageConsumer");
            await consumer.StartConsumerAsync();

            _ = Task.Run(
                async () =>
                {
                    await Task.Delay(12000);
                    await consumer.StopConsumerAsync();
                });

            await consumer.ChannelExecutionEngineAsync(
                ProcessMessageAsync,
                4,
                true,
                null);
        }

        [Fact(Skip = "only manual")]
        public async Task ConsumerDirectChannelBlockTesting()
        {
            var consumer = _fixture.RabbitService.GetConsumer("TestMessageConsumer");
            await consumer.StartConsumerAsync();

            _ = Task.Run(
                async () =>
                {
                    await Task.Delay(1000);
                    await consumer.StopConsumerAsync();
                });

            await consumer.DirectChannelExecutionEngineAsync(
                ProcessMessageAsync,
                4,
                true,
                null);
        }

        public async Task<bool> ProcessMessageAsync(ReceivedData data)
        {
            await Console.Out.WriteLineAsync(Encoding.UTF8.GetString(data.Data));
            return data.AckMessage();
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
