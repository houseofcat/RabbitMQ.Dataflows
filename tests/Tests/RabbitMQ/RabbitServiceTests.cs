using HouseofCat.RabbitMQ.Services;
using System.IO;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace RabbitMQ
{
    public class RabbitServiceTests : IClassFixture<RabbitFixture>
    {
        private readonly RabbitFixture _fixture;

        public RabbitServiceTests(RabbitFixture fixture, ITestOutputHelper output)
        {
            _fixture = fixture;
            _fixture.Output = output;
        }

        [Fact(Skip = "only manual")]
        public async Task BuildRabbitService_AndTopology()
        {
            if (!await _fixture.RabbitConnectionCheckAsync)
            {
                return;
            }

            var rabbitService = new RabbitService(
                Path.Combine("RabbitMQ", "TestConfig.json"),
                _fixture.SerializationProvider,
                _fixture.EncryptionProvider,
                _fixture.CompressionProvider);

            await rabbitService
                .Topologer
                .CreateTopologyFromFileAsync("TestTopologyConfig.json")
                .ConfigureAwait(false);

            var consumer = rabbitService.GetConsumer("TestMessageConsumer");
            await consumer
                .StartConsumerAsync()
                .ConfigureAwait(false);
        }
    }
}
