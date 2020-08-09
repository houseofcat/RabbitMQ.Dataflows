using HouseofCat.RabbitMQ;
using HouseofCat.Workflows.Pipelines;
using System;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace HouseofCat.IntegrationTests.RabbitMQ
{
    public class Pipeline_AddStep_Tests
    {
        private readonly ITestOutputHelper _output;

        public Pipeline_AddStep_Tests(ITestOutputHelper output)
        {
            _output = output;
        }

        [Fact]
        public void BuildSimplePipelineTest()
        {
            // Arrange
            var pipeline = new Pipeline<ReceivedData, WorkState>(5);

            // Act
            pipeline.AddStep<ReceivedData, WorkState>(DeserializeStep);
            pipeline.AddAsyncStep<WorkState, WorkState>(ProcessStepAsync);
            pipeline.AddAsyncStep<WorkState, WorkState>(AckMessageAsync);
            pipeline.AddStep<WorkState, WorkState>(LogStep);

            pipeline
                .Finalize((state) =>
                {
                    if (state.AllStepsSuccess)
                    { _output.WriteLine($"{DateTime.Now:yyyy/MM/dd hh:mm:ss.fff} - LetterId: {state.LetterId} - Finished route successful."); }
                    else
                    { _output.WriteLine($"{DateTime.Now:yyyy/MM/dd hh:mm:ss.fff} - LetterId: {state.LetterId} - Finished route unsuccesfully."); }

                    // Lastly mark the excution pipeline finished for this message.
                    state.ReceivedData.Complete(); // This impacts wait to completion step in the WorkFlowEngine.
                });

            // Assert
            Assert.Equal(pipeline.StepCount, 5);
        }

        private static WorkState DeserializeStep(ReceivedData data)
        {
            var state = new WorkState();
            try
            {
                //var decodedLetter = data.GetTypeFromJsonAsync<Letter>();
            }
            catch
            { state.DeserializeStepSuccess = false; }

            return state;
        }

        private static async Task<WorkState> ProcessStepAsync(WorkState state)
        {
            await Console
                .Out
                .WriteLineAsync($"{DateTime.Now:yyyy/MM/dd hh:mm:ss.fff} - LetterId: {state.LetterId} - Deserialize Step Success? {state.DeserializeStepSuccess}")
                .ConfigureAwait(false);

            if (state.DeserializeStepSuccess)
            {
                await Console
                    .Out
                    .WriteLineAsync($"{DateTime.Now:yyyy/MM/dd hh:mm:ss.fff} - LetterId: {state.LetterId} - Received: {state.Message.StringMessage}")
                    .ConfigureAwait(false);

                state.ProcessStepSuccess = true;
            }

            return state;
        }

        private async Task<WorkState> AckMessageAsync(WorkState state)
        {
            await Task.Yield();

            _output
                .WriteLine($"{DateTime.Now:yyyy/MM/dd hh:mm:ss.fff} - LetterId: {state.LetterId} - Process Step Success? {state.ProcessStepSuccess}");

            if (state.ProcessStepSuccess)
            {
                _output
                    .WriteLine($"{DateTime.Now:yyyy/MM/dd hh:mm:ss.fff} - LetterId: {state.LetterId} - Acking message...");

                if (state.ReceivedData.AckMessage())
                { state.AcknowledgeStepSuccess = true; }
            }
            else
            {
                _output
                    .WriteLine($"{DateTime.Now:yyyy/MM/dd hh:mm:ss.fff} - LetterId: {state.LetterId} - Nacking message...");

                if (state.ReceivedData.NackMessage(true))
                { state.AcknowledgeStepSuccess = true; }
            }

            return state;
        }

        private WorkState LogStep(WorkState state)
        {
            _output
                .WriteLine(
                    $"{DateTime.Now:yyyy/MM/dd hh:mm:ss.fff} - Logging - LetterId: {state.LetterId} - All Steps Success? {state.AllStepsSuccess}");

            return state;
        }

        public class Message
        {
            public string StringMessage { get; set; }
        }

        public class WorkState
        {
            public Message Message { get; set; }
            public ReceivedData ReceivedData { get; set; }
            public ulong LetterId { get; set; }
            public bool DeserializeStepSuccess { get; set; }
            public bool ProcessStepSuccess { get; set; }
            public bool AcknowledgeStepSuccess { get; set; }
            public bool AllStepsSuccess => DeserializeStepSuccess && ProcessStepSuccess && AcknowledgeStepSuccess;
        }
    }
}
