using ConsumerWorkflowMetrics.Models;
using HouseofCat.Compression;
using HouseofCat.Encryption;
using HouseofCat.Metrics;
using HouseofCat.RabbitMQ.Services;
using HouseofCat.RabbitMQ.Workflows;
using HouseofCat.Serialization;
using Microsoft.Extensions.Logging;
using System;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace ConsumerWorkflowMetrics.Services
{
    public class ConsumerWorkflowService
    {
        public Task Completion { get; protected set; }
        private ConsumerWorkflow<WorkState> _workflow;
        private readonly ILogger<ConsumerWorkflowService> _logger;
        private readonly IRabbitService _rabbitService;
        private readonly ISerializationProvider _serializationProvider;
        private readonly ICompressionProvider _compressionProvider;
        private readonly IEncryptionProvider _encryptionProvider;
        private readonly IMetricsProvider _metricsProvider;

        public int ConsumerCount = 3;
        public long GlobalCount = 100_000;
        public long ActionCount = 8;
        public long CurrentCount;
        public bool EnsureOrdered = false;
        public bool SimulateIODelay = false;
        public int MinIODelay = 40;
        public int MaxIODelay = 60;
        public bool AwaitShutdown = true;
        public bool LogOutcome = false;
        public bool UseStreamPipeline = false;
        public int MaxDoP = Environment.ProcessorCount / 2;
        public Random Rand = new Random();

        public ConsumerWorkflowService(
            ILoggerFactory logger,
            IRabbitService rabbitService,
            ISerializationProvider serializationProvider,
            ICompressionProvider compressionProvider,
            IEncryptionProvider encryptionProvider,
            IMetricsProvider metricsProvider)
        {
            _logger = logger.CreateLogger<ConsumerWorkflowService>();
            _rabbitService = rabbitService;
            _serializationProvider = serializationProvider;
            _compressionProvider = compressionProvider;
            _encryptionProvider = encryptionProvider;
            _metricsProvider = metricsProvider;
        }

        public async Task BuildAndStartWorkflowAsync(
            string workflowName,
            string consumerName,
            int consumerCount,
            int maxDoP = 4,
            bool ensureOrdered = false,
            int capacity = 200)
        {
            _workflow = new ConsumerWorkflow<WorkState>(
                rabbitService: _rabbitService,
                workflowName: workflowName,
                consumerName: consumerName,
                consumerCount: consumerCount)
                .SetSerilizationProvider(_serializationProvider)
                .SetEncryptionProvider(_encryptionProvider)
                .SetCompressionProvider(_compressionProvider)
                .SetMetricsProvider(_metricsProvider)
                .WithBuildState<Message>("Message", maxDoP, ensureOrdered)
                .WithDecryptionStep(maxDoP, ensureOrdered)
                .WithDecompressionStep(maxDoP, ensureOrdered)
                .AddStep(RetrieveObjectFromState, maxDoP, ensureOrdered)
                .AddStep(ProcessStepAsync, maxDoP, ensureOrdered)
                .AddStep(AckMessage, maxDoP, ensureOrdered)
                .WithErrorHandling(ErrorHandlingAsync, capacity, maxDoP, ensureOrdered)
                .WithFinalization(FinalizationAsync, maxDoP, ensureOrdered);

            Completion = _workflow.Completion;

            await _workflow.StartAsync();
        }

        private static WorkState RetrieveObjectFromState(WorkState state)
        {
            try
            {
                state.Message = (Message)state.Data["Item"];
                state.Data.Remove("Item");
            }
            catch { }

            return state;
        }

        private async Task<WorkState> ProcessStepAsync(WorkState state)
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

        private WorkState AckMessage(WorkState state)
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

        private async Task ErrorHandlingAsync(WorkState state)
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

        private async Task FinalizationAsync(WorkState state)
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
                    await _workflow.StopAsync().ConfigureAwait(false);
                }
            }
        }
    }
}
