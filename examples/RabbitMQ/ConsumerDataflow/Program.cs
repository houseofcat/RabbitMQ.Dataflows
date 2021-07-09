using HouseofCat.Compression;
using HouseofCat.Encryption;
using HouseofCat.Hashing;
using HouseofCat.Metrics;
using HouseofCat.RabbitMQ;
using HouseofCat.RabbitMQ.Dataflows;
using HouseofCat.RabbitMQ.Services;
using HouseofCat.RabbitMQ.WorkState;
using HouseofCat.Serialization;
using Microsoft.Extensions.Logging;
using System;
using System.Diagnostics;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Examples.RabbitMQ.ConsumerWorkflow
{
    public static class Program
    {
        public static ConsumerDataflow<WorkState> _workflow;
        public static Stopwatch Stopwatch;
        public static LogLevel LogLevel = LogLevel.Information;
        public static int ConsumerCount = 4;
        public static long GlobalCount = 100_000;
        public static long ActionCount = 8;
        public static long CurrentCount;
        public static bool EnsureOrdered = false; // use with simulate IO delay to determine if ensuring order is causing delays
        public static bool SimulateIODelay = false;
        public static int MinIODelay = 40;
        public static int MaxIODelay = 60;
        public static bool AwaitShutdown = true;
        public static bool LogOutcome = false;
        public static bool UseStreamPipeline = false;
        public static int MaxDoP = Environment.ProcessorCount;
        public static Random Rand = new Random();

        private static ILogger<ConsumerDataflow<WorkState>> _logger;
        private static IRabbitService _rabbitService;
        private static ISerializationProvider _serializationProvider;
        private static IHashingProvider _hashingProvider;
        private static ICompressionProvider _compressionProvider;
        private static IEncryptionProvider _encryptionProvider;
        private static IMetricsProvider _metricsProvider;

        public static async Task Main()
        {
            await Console.Out.WriteLineAsync("Run a ConsumerWorkflow demo... press any key to continue!").ConfigureAwait(false);
            Console.ReadKey(); // memory snapshot baseline

            // Create RabbitService
            await SetupAsync().ConfigureAwait(false);

            await Console.Out.WriteLineAsync("Setting up Workflow...").ConfigureAwait(false);

            var workflowName = "MyConsumerWorkflow";
            _workflow = new ConsumerDataflow<WorkState>(
                rabbitService: _rabbitService,
                workflowName: workflowName,
                consumerName: "ConsumerFromConfig",
                consumerCount: ConsumerCount)
                .SetSerializationProvider(_serializationProvider)
                .SetEncryptionProvider(_encryptionProvider)
                .SetCompressionProvider(_compressionProvider)
                .SetMetricsProvider(_metricsProvider)
                .WithBuildState<Message>("Message", MaxDoP, false, 200)
                .WithDecryptionStep(MaxDoP, false, 200)
                .WithDecompressionStep(MaxDoP, false, 200)
                .AddStep(RetrieveObjectFromState, $"{workflowName}_RetrieveObjectFromState", true, null, MaxDoP, EnsureOrdered, 200)
                .AddStep(ProcessStepAsync, $"{workflowName}_ProcessStep", !SimulateIODelay, null, MaxDoP, EnsureOrdered, 200)
                .AddStep(AckMessage, $"{workflowName}_AckMessage", true, null, MaxDoP, EnsureOrdered, 200)
                .WithErrorHandling(ErrorHandlingAsync, 200, MaxDoP, false)
                .WithFinalization(FinalizationAsync, MaxDoP, false);

            await Console.Out.WriteLineAsync("Starting Workflow...").ConfigureAwait(false);
            await _workflow
                .StartAsync()
                .ConfigureAwait(false);

            Stopwatch = Stopwatch.StartNew();
            await Console.Out.WriteLineAsync("Waiting for workflow to be signaled complete...").ConfigureAwait(false);
            await _workflow.Completion.ConfigureAwait(false);

            await Console.Out.WriteLineAsync("\r\nStatistics!").ConfigureAwait(false);
            await Console.Out.WriteLineAsync($"MaxDoP: {MaxDoP}, Ensure Ordered: {EnsureOrdered}").ConfigureAwait(false);
            await Console.Out.WriteLineAsync($"SimulateIODelay: {SimulateIODelay}, MinIODelay: {MinIODelay}ms, MaxIODelay: {MaxIODelay}ms").ConfigureAwait(false);
            await Console.Out.WriteLineAsync($"AwaitShutdown: {AwaitShutdown}, LogOutcome: {LogOutcome}").ConfigureAwait(false);
            await Console.Out.WriteLineAsync($"UseStreamPipeline: {UseStreamPipeline}").ConfigureAwait(false);
            await Console.Out.WriteLineAsync($"Finished processing {GlobalCount} messages (Steps: {GlobalCount * ActionCount}) in {Stopwatch.ElapsedMilliseconds} milliseconds.").ConfigureAwait(false);

            var rate = GlobalCount / (Stopwatch.ElapsedMilliseconds / 1.0) * 1000.0;
            await Console.Out.WriteLineAsync($"Rate: {rate} msg/s.").ConfigureAwait(false);
            await Console.Out.WriteLineAsync($"Rate: {rate * ActionCount} actions/s.").ConfigureAwait(false);
            await Console.Out.WriteLineAsync("\r\nClient Finished! Press any key to start the shutdown!").ConfigureAwait(false);
            Console.ReadKey(); // checking for memory leak (snapshots)

            await Console.Out.WriteLineAsync("\r\nAll finished cleanup! Press any key to exit...").ConfigureAwait(false);
            Console.ReadKey(); // checking for memory leak (snapshots)
        }

        private static async Task SetupAsync()
        {
            var letterTemplate = new Letter("", "TestRabbitServiceQueue", null, new LetterMetadata());
            var loggerFactory = LoggerFactory.Create(builder => builder.AddConsole().SetMinimumLevel(LogLevel));
            _logger = loggerFactory.CreateLogger<ConsumerDataflow<WorkState>>();

            _hashingProvider = new Argon2IDHasher();
            var hashKey = await _hashingProvider.GetHashKeyAsync("passwordforencryption", "saltforencryption", 32).ConfigureAwait(false);

            _metricsProvider = new NullMetricsProvider();
            _encryptionProvider = new AesGcmEncryptionProvider(hashKey, _hashingProvider.Type);
            _compressionProvider = new LZ4PickleProvider();
            _serializationProvider = new Utf8JsonProvider();

            _rabbitService = new RabbitService(
                "Config.json",
                _serializationProvider,
                _encryptionProvider,
                _compressionProvider,
                loggerFactory);

            await _rabbitService
                .Topologer
                .CreateQueueAsync("TestRabbitServiceQueue")
                .ConfigureAwait(false);
        }

        public class Message
        {
            public long MessageId { get; set; }
            public string StringMessage { get; set; }
        }

        public class WorkState : RabbitWorkState
        {
            public Message Message { get; set; }
            public bool DeserializeStepSuccess => Message != null;
            public bool ProcessStepSuccess { get; set; }
            public bool AcknowledgeStepSuccess { get; set; }
            public bool AllStepsSuccess => DeserializeStepSuccess && ProcessStepSuccess && AcknowledgeStepSuccess;
        }

        private static WorkState RetrieveObjectFromState(WorkState state)
        {
            try
            {
                state.Message = (Message)state.Data["Message"];
                state.Data.Remove("Message");
            }
            catch { state.IsFaulted = true; }

            return state;
        }

        private static async Task<WorkState> ProcessStepAsync(WorkState state)
        {
            _logger.LogDebug($"{DateTime.Now:yyyy/MM/dd hh:mm:ss.fff} - Id: {state.Message?.MessageId} - Deserialize Step Success? {state.DeserializeStepSuccess}");

            if (state.DeserializeStepSuccess)
            {
                _logger.LogDebug($"{DateTime.Now:yyyy/MM/dd hh:mm:ss.fff} - Id: {state.Message?.MessageId} - Received: {state.Message?.StringMessage}");

                state.ProcessStepSuccess = true;

                // Simulate processing.
                if (SimulateIODelay)
                {
                    await Task.Delay(Rand.Next(MinIODelay, MaxIODelay)).ConfigureAwait(false);
                }
            }

            return state;
        }

        private static WorkState AckMessage(WorkState state)
        {
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

        private static async Task ErrorHandlingAsync(WorkState state)
        {
            var failed = await _rabbitService
                .Publisher
                .PublishAsync("", "TestRabbitServiceQueue.Error", state.ReceivedData.Data, null)
                .ConfigureAwait(false);

            var stringBody = string.Empty;

            try
            { stringBody = Encoding.UTF8.GetString(state.ReceivedData.Data); }
            catch (Exception ex) { _logger.LogError(ex, "What?!"); }

            if (failed)
            {
                _logger.LogError($"{DateTime.Now:yyyy/MM/dd hh:mm:ss.fff} - This failed to deserialize and publish to ErrorQueue!\r\n{stringBody}\r\n");
            }
            else
            {
                _logger.LogError($"{DateTime.Now:yyyy/MM/dd hh:mm:ss.fff} - This failed to deserialize. Published to ErrorQueue!\r\n{stringBody}\r\n");
            }
        }

        private static async Task FinalizationAsync(WorkState state)
        {
            if (LogOutcome)
            {
                if (state.AllStepsSuccess)
                { _logger.LogInformation($"{DateTime.Now:yyyy/MM/dd hh:mm:ss.fff} - Id: {state.Message?.MessageId} - Finished route successfully."); }
                else
                { _logger.LogInformation($"{DateTime.Now:yyyy/MM/dd hh:mm:ss.fff} - Id: {state.Message?.MessageId} - Finished route unsuccesfully."); }
            }

            // Lastly mark the excution pipeline finished for this message.
            state.ReceivedData?.Complete(); // This impacts wait to completion step in the Pipeline.

            if (AwaitShutdown)
            {
                Interlocked.Increment(ref CurrentCount);
                if (CurrentCount == GlobalCount - 1)
                {
                    Stopwatch.Stop();
                    await _workflow.StopAsync().ConfigureAwait(false);
                }
            }
        }
    }
}
