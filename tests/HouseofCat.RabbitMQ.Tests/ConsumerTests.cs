//using HouseofCat.RabbitMQ;
//using HouseofCat.RabbitMQ.Pools;
//using HouseofCat.RabbitMQ.Services;
//using HouseofCat.Utilities.File;
//using HouseofCat.Workflows.Pipelines;
//using System;
//using System.Threading.Tasks;
//using Xunit;
//using Xunit.Abstractions;

//namespace HouseofCat.RabbitMQ.Tests
//{
//    public class ConsumerTests
//    {
//        private readonly ITestOutputHelper output;
//        private readonly Options options;
//        private readonly IChannelPool channelPool;
//        private readonly Topologer topologer;
//        private readonly IRabbitService rabbitService;

//        public ConsumerTests(ITestOutputHelper output)
//        {
//            this.output = output;
//            options = JsonFileReader.ReadFileAsync<Options>("Config.json").GetAwaiter().GetResult();

//            channelPool = new ChannelPool(options);
//            topologer = new Topologer(options);
//            rabbitService = new RabbitService("Config.json", null, null, null, null);
//        }

//        [Fact]
//        public async Task CreateConsumer()
//        {
//            var options = await JsonFileReader.ReadFileAsync<Options>("TestConfig.json");
//            Assert.NotNull(options);

//            var con = new Consumer(options, "TestMessageConsumer");
//            Assert.NotNull(con);
//        }

//        [Fact]
//        public async Task CreateConsumerAndInitializeChannelPool()
//        {
//            var options = await JsonFileReader.ReadFileAsync<Options>("TestConfig.json");
//            Assert.NotNull(options);

//            var con = new Consumer(options, "TestMessageConsumer");
//            Assert.NotNull(con);
//        }

//        [Fact]
//        public async Task CreateConsumerAndStart()
//        {
//            await topologer.CreateQueueAsync("TestConsumerQueue").ConfigureAwait(false);
//            var con = new Consumer(channelPool, "TestMessageConsumer");
//            await con.StartConsumerAsync().ConfigureAwait(false);
//        }

//        [Fact]
//        public async Task CreateConsumerStartAndStop()
//        {
//            var con = new Consumer(channelPool, "TestMessageConsumer");

//            await con.StartConsumerAsync().ConfigureAwait(false);
//            await con.StopConsumerAsync().ConfigureAwait(false);
//        }

//        [Fact]
//        public async Task CreateManyConsumersStartAndStop()
//        {
//            for (int i = 0; i < 1000; i++)
//            {
//                var con = new Consumer(channelPool, "TestConsumer");

//                await con.StartConsumerAsync().ConfigureAwait(false);
//                await con.StopConsumerAsync().ConfigureAwait(false);
//            }
//        }

//        [Fact]
//        public async Task ConsumerStartAndStopTesting()
//        {
//            var consumer = rabbitService.GetConsumer("ConsumerFromConfig");

//            for (int i = 0; i < 100; i++)
//            {
//                await consumer.StartConsumerAsync().ConfigureAwait(false);
//                await consumer.StopConsumerAsync().ConfigureAwait(false);
//            }
//        }

//        [Fact]
//        public async Task ConsumerPipelineStartAndStopTesting()
//        {
//            var consumerPipeline = rabbitService.CreateConsumerPipeline<WorkState>("ConsumerFromConfig", 100, false, BuildPipeline);

//            for (int i = 0; i < 100; i++)
//            {
//                await consumerPipeline.StartAsync(true);
//                await consumerPipeline.StopAsync();
//            }
//        }

//        private IPipeline<ReceivedData, WorkState> BuildPipeline(int maxDoP, bool? ensureOrdered = null)
//        {
//            var pipeline = new Pipeline<ReceivedData, WorkState>(
//                maxDoP,
//                healthCheckInterval: TimeSpan.FromSeconds(10),
//                pipelineName: "ConsumerPipelineExample",
//                ensureOrdered);

//            pipeline.AddAsyncStep<ReceivedData, WorkState>(DeserializeStepAsync);
//            pipeline.AddAsyncStep<WorkState, WorkState>(ProcessStepAsync);
//            pipeline.AddAsyncStep<WorkState, WorkState>(AckMessageAsync);

//            pipeline
//                .Finalize((state) =>
//                {
//                    // Lastly mark the excution pipeline finished for this message.
//                    state.ReceivedData?.Complete(); // This impacts wait to completion step in the Pipeline.
//                });

//            return pipeline;
//        }

//        public class Message
//        {
//            public long MessageId { get; set; }
//            public string StringMessage { get; set; }
//        }

//        public class WorkState : HouseofCat.RabbitMQ.Pipelines.WorkState
//        {
//            public Message Message { get; set; }
//            public ulong LetterId { get; set; }
//            public bool DeserializeStepSuccess { get; set; }
//            public bool ProcessStepSuccess { get; set; }
//            public bool AcknowledgeStepSuccess { get; set; }
//            public bool AllStepsSuccess => DeserializeStepSuccess && ProcessStepSuccess && AcknowledgeStepSuccess;
//        }

//        private async Task<WorkState> DeserializeStepAsync(IReceivedData receivedData)
//        {
//            var state = new WorkState
//            {
//                ReceivedData = receivedData
//            };

//            try
//            {
//                state.Message = state.ReceivedData.ContentType switch
//                {
//                    Constants.HeaderValueForLetter => await receivedData
//                        .GetTypeFromJsonAsync<Message>()
//                        .ConfigureAwait(false),

//                    _ => await receivedData
//                        .GetTypeFromJsonAsync<Message>(decrypt: false, decompress: false)
//                        .ConfigureAwait(false),
//                };

//                if (state.ReceivedData.Data.Length > 0 && (state.Message != null || state.ReceivedData.Letter != null))
//                { state.DeserializeStepSuccess = true; }
//            }
//            catch
//            { }

//            return state;
//        }

//        private async Task<WorkState> ProcessStepAsync(WorkState state)
//        {
//            await Task.Yield();

//            if (state.DeserializeStepSuccess)
//            {
//                state.ProcessStepSuccess = true;
//            }

//            return state;
//        }

//        private async Task<WorkState> AckMessageAsync(WorkState state)
//        {
//            await Task.Yield();

//            if (state.ProcessStepSuccess)
//            {
//                if (state.ReceivedData.AckMessage())
//                { state.AcknowledgeStepSuccess = true; }
//            }
//            else
//            {
//                if (state.ReceivedData.NackMessage(true))
//                { state.AcknowledgeStepSuccess = true; }
//            }

//            return state;
//        }
//    }
//}
