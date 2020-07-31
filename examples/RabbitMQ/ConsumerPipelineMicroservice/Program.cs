using HouseofCat.RabbitMQ;
using HouseofCat.RabbitMQ.Services;
using HouseofCat.RabbitMQ.Pipelines;
using HouseofCat.Workflows.Pipelines;
using Microsoft.Extensions.Logging;
using System;
using System.Diagnostics;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace Examples.RabbitMQ.ConsumerPipelineMicroservice
{
    public static class Program
    {
        public static Stopwatch Stopwatch;
        public static LogLevel LogLevel = LogLevel.Information;
        public static long GlobalCount = 10000;
        public static bool EnsureOrdered = false; // use with simulate IO delay to determine if ensuring order is causing delays
        public static bool SimulateIODelay = false;
        public static int MinIODelay = 50;
        public static int MaxIODelay = 100;
        public static bool AwaitShutdown = true;
        public static bool LogOutcome = false;
        public static bool UseStreamPipeline = true;
        public static int MaxDoP = 64;
        public static Random Rand = new Random();

        public static async Task Main()
        {
            var microservice = new ConsumerPipelineMicroservice();
            await Console.Out.WriteLineAsync("Run a ConsumerPipelineMicroservice demo... press any key to continue!").ConfigureAwait(false);
            Console.ReadKey(); // memory snapshot baseline

            await microservice
                .StartAsync()
                .ConfigureAwait(false);

            await Console.Out.WriteLineAsync("\r\nStatistics!").ConfigureAwait(false);
            await Console.Out.WriteLineAsync($"MaxDoP: {MaxDoP}, Ensure Ordered: {EnsureOrdered}").ConfigureAwait(false);
            await Console.Out.WriteLineAsync($"SimulateIODelay: {SimulateIODelay}, MinIODelay: {MinIODelay}ms, MaxIODelay: {MaxIODelay}ms").ConfigureAwait(false);
            await Console.Out.WriteLineAsync($"AwaitShutdown: {AwaitShutdown}, LogOutcome: {LogOutcome}").ConfigureAwait(false);
            await Console.Out.WriteLineAsync($"UseStreamPipeline: {UseStreamPipeline}").ConfigureAwait(false);
            await Console.Out.WriteLineAsync($"Finished processing {GlobalCount} messages (Steps: {GlobalCount * 3}) in {Stopwatch.ElapsedMilliseconds} milliseconds.").ConfigureAwait(false);

            var rate = GlobalCount / (Stopwatch.ElapsedMilliseconds / 1.0) * 1000.0;
            await Console.Out.WriteLineAsync($"Rate: {rate} msg/s.").ConfigureAwait(false);
            await Console.Out.WriteLineAsync($"Rate: {rate * 3} functions/s.").ConfigureAwait(false);
            await Console.Out.WriteLineAsync("\r\nClient Finished! Press any key to start the shutdown!").ConfigureAwait(false);
            Console.ReadKey(); // checking for memory leak (snapshots)

            await microservice
                .ShutdownAsync()
                .ConfigureAwait(false);

            await Console.Out.WriteLineAsync("\r\nAll finished cleanup! Press any key to exit...").ConfigureAwait(false);
            Console.ReadKey(); // checking for memory leak (snapshots)
        }
    }

    public class ConsumerPipelineMicroservice
    {
        private IRabbitService _rabbitService;
        private ILogger<ConsumerPipelineMicroservice> _logger;
        private IConsumerPipeline<WorkState> _consumerPipeline;

        private string _errorQueue;
        private long _targetCount;
        private long _currentMessageCount;

        public async Task StartAsync()
        {
            _targetCount = Program.GlobalCount;
            await Console.Out.WriteLineAsync("\r\nRunning example...\r\n").ConfigureAwait(false);

            _rabbitService = await SetupAsync().ConfigureAwait(false);
            _errorQueue = _rabbitService.Options.GetConsumerOptions("ConsumerFromConfig").ErrorQueueName;

            _consumerPipeline = _rabbitService.CreateConsumerPipeline("ConsumerFromConfig", Program.MaxDoP, Program.EnsureOrdered, BuildPipeline);
            Program.Stopwatch = Stopwatch.StartNew();
            await _consumerPipeline.StartAsync(Program.UseStreamPipeline).ConfigureAwait(false);
            if (Program.AwaitShutdown)
            {
                await Console.Out.WriteLineAsync("\r\nAwaiting full ConsumerPipeline finish...\r\n").ConfigureAwait(false);
                await _consumerPipeline.AwaitCompletionAsync().ConfigureAwait(false);
            }
            Program.Stopwatch.Stop();
            await Console.Out.WriteLineAsync("\r\nExample finished...").ConfigureAwait(false);
        }

        public async Task ShutdownAsync()
        {
            await _rabbitService.ShutdownAsync(false);
        }

        private async Task<RabbitService> SetupAsync()
        {
            var letterTemplate = new Letter("", "TestRabbitServiceQueue", null, new LetterMetadata());
            var loggerFactory = LoggerFactory.Create(builder => builder.AddConsole().SetMinimumLevel(Program.LogLevel));
            _logger = loggerFactory.CreateLogger<ConsumerPipelineMicroservice>();
            var rabbitService = new RabbitService(
                "Config.json",
                "passwordforencryption",
                "saltforencryption",
                loggerFactory);

            await rabbitService
                .Topologer
                .CreateQueueAsync("TestRabbitServiceQueue")
                .ConfigureAwait(false);

            for (var i = 0L; i < _targetCount; i++)
            {
                var letter = letterTemplate.Clone();
                letter.Body = JsonSerializer.SerializeToUtf8Bytes(new Message { StringMessage = $"Sensitive ReceivedLetter {i}", MessageId = i });
                letter.LetterId = (ulong)i;
                await rabbitService
                    .Publisher
                    .PublishAsync(letter, true, true)
                    .ConfigureAwait(false);
            }

            return rabbitService;
        }

        private IPipeline<ReceivedData, WorkState> BuildPipeline(int maxDoP, bool? ensureOrdered = null)
        {
            var pipeline = new Pipeline<ReceivedData, WorkState>(
                maxDoP,
                healthCheckInterval: TimeSpan.FromSeconds(10),
                pipelineName: "ConsumerPipelineExample",
                ensureOrdered);

            pipeline.AddAsyncStep<ReceivedData, WorkState>(DeserializeStepAsync);
            pipeline.AddAsyncStep<WorkState, WorkState>(ProcessStepAsync);
            pipeline.AddAsyncStep<WorkState, WorkState>(AckMessageAsync);

            pipeline
                .Finalize(async (state) =>
                {
                    if (Program.LogOutcome)
                    {
                        if (state.AllStepsSuccess)
                        { _logger.LogInformation($"{DateTime.Now:yyyy/MM/dd hh:mm:ss.fff} - Id: {state.Message?.MessageId} - Finished route successfully."); }
                        else
                        { _logger.LogInformation($"{DateTime.Now:yyyy/MM/dd hh:mm:ss.fff} - Id: {state.Message?.MessageId} - Finished route unsuccesfully."); }
                    }

                    // Lastly mark the excution pipeline finished for this message.
                    state.ReceivedData?.Complete(); // This impacts wait to completion step in the Pipeline.

                    if (Program.AwaitShutdown)
                    {
                        Interlocked.Increment(ref _currentMessageCount);
                        if (_currentMessageCount == _targetCount - 1)
                        {
                            await _consumerPipeline.StopAsync().ConfigureAwait(false);
                        }
                    }
                });

            return pipeline;
        }

        public class Message
        {
            public long MessageId { get; set; }
            public string StringMessage { get; set; }
        }

        public class WorkState : HouseofCat.RabbitMQ.Pipelines.WorkState
        {
            public Message Message { get; set; }
            public ulong LetterId { get; set; }
            public bool DeserializeStepSuccess { get; set; }
            public bool ProcessStepSuccess { get; set; }
            public bool AcknowledgeStepSuccess { get; set; }
            public bool AllStepsSuccess => DeserializeStepSuccess && ProcessStepSuccess && AcknowledgeStepSuccess;
        }

        private async Task<WorkState> DeserializeStepAsync(IReceivedData receivedData)
        {
            var state = new WorkState
            {
                ReceivedData = receivedData
            };

            try
            {
                state.Message = state.ReceivedData.ContentType switch
                {
                    Constants.HeaderValueForLetter => await receivedData
                        .GetTypeFromJsonAsync<Message>()
                        .ConfigureAwait(false),

                    _ => await receivedData
                        .GetTypeFromJsonAsync<Message>(decrypt: false, decompress: false)
                        .ConfigureAwait(false),
                };

                if (state.ReceivedData.Data.Length > 0 && (state.Message != null || state.ReceivedData.Letter != null))
                { state.DeserializeStepSuccess = true; }
            }
            catch
            { }

            return state;
        }

        private async Task<WorkState> ProcessStepAsync(WorkState state)
        {
            await Task.Yield();

            _logger.LogDebug($"{DateTime.Now:yyyy/MM/dd hh:mm:ss.fff} - Id: {state.Message?.MessageId} - Deserialize Step Success? {state.DeserializeStepSuccess}");

            if (state.DeserializeStepSuccess)
            {
                _logger.LogDebug($"{DateTime.Now:yyyy/MM/dd hh:mm:ss.fff} - Id: {state.Message?.MessageId} - Received: {state.Message?.StringMessage}");

                state.ProcessStepSuccess = true;

                // Simulate processing.
                if (Program.SimulateIODelay)
                {
                    await Task.Delay(Program.Rand.Next(Program.MinIODelay, Program.MaxIODelay)).ConfigureAwait(false);
                }
            }
            else
            {
                var failed = await _rabbitService
                    .Publisher
                    .PublishAsync("", _errorQueue, state.ReceivedData.Data, null)
                    .ConfigureAwait(false);

                var stringBody = string.Empty;

                try
                { stringBody = await state.ReceivedData.GetBodyAsUtf8StringAsync().ConfigureAwait(false); }
                catch (Exception ex) { _logger.LogError(ex, "What?!"); }

                if (failed)
                {
                    _logger.LogError($"{DateTime.Now:yyyy/MM/dd hh:mm:ss.fff} - This failed to deserialize and publish to ErrorQueue!\r\n{stringBody}\r\n");
                }
                else
                {
                    _logger.LogError($"{DateTime.Now:yyyy/MM/dd hh:mm:ss.fff} - This failed to deserialize. Published to ErrorQueue!\r\n{stringBody}\r\n");

                    // So we ack the message
                    state.ProcessStepSuccess = true;
                }
            }

            return state;
        }

        private async Task<WorkState> AckMessageAsync(WorkState state)
        {
            await Task.Yield();

            _logger.LogDebug($"{DateTime.Now:yyyy/MM/dd hh:mm:ss.fff} - Id: {state.Message?.MessageId} - Process Step Success? {state.ProcessStepSuccess}");

            if (state.ProcessStepSuccess)
            {
                _logger.LogDebug($"{DateTime.Now:yyyy/MM/dd hh:mm:ss.fff} - Id: {state.Message?.MessageId} - Acking message...");

                if (state.ReceivedData.AckMessage())
                { state.AcknowledgeStepSuccess = true; }
            }
            else
            {
                _logger.LogDebug($"{DateTime.Now:yyyy/MM/dd hh:mm:ss.fff} - Id: {state.Message?.MessageId} - Nacking message...");

                if (state.ReceivedData.NackMessage(true))
                { state.AcknowledgeStepSuccess = true; }
            }

            return state;
        }
    }
}
