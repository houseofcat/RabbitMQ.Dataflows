using HouseofCat.Compression;
using HouseofCat.Encryption;
using HouseofCat.Hashing;
using HouseofCat.RabbitMQ.Services;
using HouseofCat.Serialization;
using HouseofCat.Tests.IntegrationTests.RabbitMQ;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace HouseofCat.Tests.IntegrationTests.RabbitMQ
{
    public class RabbitServiceTests : IClassFixture<RabbitFixture>
    {
        private readonly RabbitFixture _fixture;

        public RabbitServiceTests(RabbitFixture fixture)
        {
            _fixture = fixture;
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
