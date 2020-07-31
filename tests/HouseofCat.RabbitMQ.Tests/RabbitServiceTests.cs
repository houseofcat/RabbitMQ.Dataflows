using CookedRabbit.Core.Service;
using HouseofCat.RabbitMQ.Service;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace CookedRabbit.Core.Tests
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
                .StartConsumerAsync(false, true)
                .ConfigureAwait(false);
        }
    }
}
