using HouseofCat.RabbitMQ.Services;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace HouseofCat.RabbitMQ.IntegrationTests
{
    public class RabbitServiceTests : IClassFixture<RabbitFixture>
    {
        private readonly RabbitFixture _fixture;

        public RabbitServiceTests(RabbitFixture fixture, ITestOutputHelper output)
        {
            _fixture = fixture;
            _fixture.Output = output;
        }

        [Fact]
        public async Task BuildRabbitService_AndTopology()
        {
            var rabbitService = new RabbitService(
                "TestConfig.json",
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
