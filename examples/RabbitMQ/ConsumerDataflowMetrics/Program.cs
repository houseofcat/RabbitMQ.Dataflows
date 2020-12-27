using ConsumerDataflowMetrics.Services;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System.Threading.Tasks;

namespace ConsumerDataflowMetrics
{
    public static class Program
    {
        public static async Task Main(string[] args)
        {
            var hostBuilder = CreateHostBuilder(args).Build();
            var webStartAsync = hostBuilder.RunAsync();

            using var scope = hostBuilder.Services.CreateScope();
            var workflowService = scope.ServiceProvider.GetRequiredService<ConsumerDataflowService>();

            await workflowService
                .BuildAndStartDataflowAsync()
                .ConfigureAwait(false);

            await webStartAsync.ConfigureAwait(false);
        }

        public static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
                .ConfigureWebHostDefaults(webBuilder =>
                {
                    webBuilder.UseStartup<Startup>();
                });
    }
}
