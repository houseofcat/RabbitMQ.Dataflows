using ConsumerDataflowMetrics.Models;
using HouseofCat.Compression;
using HouseofCat.Encryption.Providers;
using HouseofCat.Metrics;
using HouseofCat.RabbitMQ.Dataflows;
using HouseofCat.RabbitMQ.Services;
using HouseofCat.Serialization;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System;
using System.Text;
using System.Threading.Tasks;

namespace ConsumerDataflowMetrics.Services
{
    public class ConsumerDataflowService
    {
        public Task Completion { get; protected set; }
        private ConsumerDataflow<WorkState> _workflow;
        private readonly IConfiguration _config;
        private readonly ILogger<ConsumerDataflowService> _logger;
        private readonly IRabbitService _rabbitService;
        private readonly ISerializationProvider _serializationProvider;
        private readonly ICompressionProvider _compressionProvider;
        private readonly IEncryptionProvider _encryptionProvider;
        private readonly IMetricsProvider _metricsProvider;

        private bool _simulateIODelay = true;
        private int _minIODelay = 50;
        private int _maxIODelay = 100;
        private bool _logStepOutcomes = false;
        private Random _rand = new Random();

        public ConsumerDataflowService(
            IConfiguration config,
            ILoggerFactory logger,
            IRabbitService rabbitService,
            ISerializationProvider serializationProvider,
            ICompressionProvider compressionProvider,
            IEncryptionProvider encryptionProvider,
            IMetricsProvider metricsProvider)
        {
            _config = config;
            _logger = logger.CreateLogger<ConsumerDataflowService>();
            _rabbitService = rabbitService;
            _serializationProvider = serializationProvider;
            _compressionProvider = compressionProvider;
            _encryptionProvider = encryptionProvider;
            _metricsProvider = metricsProvider;
        }

        public async Task BuildAndStartDataflowAsync()
        {
            var workflowName = _config.GetValue<string>("HouseofCat:ConsumerDataflowOptions:DataflowName");
            var consumerName = _config.GetValue<string>("HouseofCat:ConsumerDataflowOptions:ConsumerName");
            var consumerCount = _config.GetValue<int>("HouseofCat:ConsumerDataflowOptions:ConsumerCount");
            var maxDoP = _config.GetValue<int>("HouseofCat:ConsumerDataflowOptions:MaxDoP");
            var ensureOrdered = _config.GetValue<bool>("HouseofCat:ConsumerDataflowOptions:EnsureOrdered");
            var capacity = _config.GetValue<int>("HouseofCat:ConsumerDataflowOptions:Capacity");

            _simulateIODelay = _config.GetValue<bool>("HouseofCat:ConsumerDataflowOptions:SimulateIODelay");
            _minIODelay = _config.GetValue<int>("HouseofCat:ConsumerDataflowOptions:MinIODelay");
            _maxIODelay = _config.GetValue<int>("HouseofCat:ConsumerDataflowOptions:MaxIODelay");

            _logStepOutcomes = _config.GetValue<bool>("HouseofCat:ConsumerDataflowOptions:LogStepOutcomes");

            _workflow = new ConsumerDataflow<WorkState>(
                rabbitService: _rabbitService,
                workflowName: workflowName,
                consumerName: consumerName,
                consumerCount: consumerCount)
                .SetSerializationProvider(_serializationProvider)
                .SetEncryptionProvider(_encryptionProvider)
                .SetCompressionProvider(_compressionProvider)
                .SetMetricsProvider(_metricsProvider)
                .WithBuildState<Message>("Message", maxDoP, ensureOrdered)
                .WithDecryptionStep(maxDoP, ensureOrdered)
                .WithDecompressionStep(maxDoP, ensureOrdered)
                .AddStep(RetrieveObjectFromState, $"{workflowName}_RetrieveObjectFromState", true, null, maxDoP, ensureOrdered)
                .AddStep(ProcessStepAsync, $"{workflowName}_ProcessStep", !_simulateIODelay, null, maxDoP, ensureOrdered)
                .AddStep(AckMessage, $"{workflowName}_AckMessage", true, null, maxDoP, ensureOrdered)
                .WithErrorHandling(ErrorHandlingAsync, capacity, maxDoP, ensureOrdered)
                .WithFinalization(Finalization, maxDoP, ensureOrdered);

            Completion = _workflow.Completion;

            await _workflow
                .StartAsync()
                .ConfigureAwait(false);
        }

        private static WorkState RetrieveObjectFromState(WorkState state)
        {
            try
            {
                state.Message = (Message)state.Data["Message"]; // Must Match The BuildState Key Value
                state.Data.Remove("Message");
            }
            catch
            {
                state.IsFaulted = true; // Mark this step a failure.
            }

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
                if (_simulateIODelay)
                {
                    await Task.Delay(_rand.Next(_minIODelay, _maxIODelay)).ConfigureAwait(false);
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
            catch (Exception ex)
            {
                _logger.LogError(ex, "What?!");
            }

            if (failed)
            {
                _logger.LogError($"{DateTime.Now:yyyy/MM/dd hh:mm:ss.fff} - This failed to deserialize and publish to ErrorQueue!\r\n{stringBody}\r\n");
            }
            else
            {
                _logger.LogError($"{DateTime.Now:yyyy/MM/dd hh:mm:ss.fff} - This failed to deserialize. Published to ErrorQueue!\r\n{stringBody}\r\n");
            }
        }

        private void Finalization(WorkState state)
        {
            if (_logStepOutcomes)
            {
                if (state.AllStepsSuccess)
                { _logger.LogInformation($"{DateTime.Now:yyyy/MM/dd hh:mm:ss.fff} - Id: {state.Message?.MessageId} - Finished route successfully."); }
                else
                { _logger.LogInformation($"{DateTime.Now:yyyy/MM/dd hh:mm:ss.fff} - Id: {state.Message?.MessageId} - Finished route unsuccesfully."); }
            }

            // Lastly mark the excution pipeline finished for this message.
            state.ReceivedData?.Complete(); // This impacts wait to completion step in the Pipeline.
        }
    }
}
