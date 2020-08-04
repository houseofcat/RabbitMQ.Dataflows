using HouseofCat.RabbitMQ.Services;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace HouseofCat.RabbitMQ.Tests
{
    public class RabbitServiceTests
    {
        //private readonly ITestOutputHelper output;
        private readonly RabbitService rabbitService;

        public RabbitServiceTests(ITestOutputHelper output)
        {
            rabbitService = new RabbitService("TestConfig.json", "passwordforencryption", "saltforencryption");
        }

        [Fact]
        public async Task ProductionBug_CantFindConsumer_WhenStartingMessageConsumers()
        {
            var rabbitService = new RabbitService("TestConfig.json", "passwordforencryption", "saltforencryption");

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
