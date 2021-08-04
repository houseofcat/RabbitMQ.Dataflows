using ConsumerDataflowMetrics.Services;
using HouseofCat.Compression;
using HouseofCat.Encryption;
using HouseofCat.Hashing;
using HouseofCat.Metrics;
using HouseofCat.RabbitMQ.Services;
using HouseofCat.Serialization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Prometheus;

namespace ConsumerDataflowMetrics
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddControllers();

            var loggerFactory = LoggerFactory.Create(builder => builder.AddConsole().SetMinimumLevel(LogLevel.Information));

            var metricsProvider = new PrometheusMetricsProvider();

            var serializationProvider = new Utf8JsonProvider();
            var hashingProvider = new Argon2IDHasher();
            var hashKey = hashingProvider
                .GetHashKeyAsync("passwordforencryption", "saltforencryption", 32)
                .GetAwaiter()
                .GetResult();

            var encryptionProvider = new AesGcmEncryptionProvider(hashKey, hashingProvider.Type);
            var compressionProvider = new LZ4PickleProvider();

            var rabbitService = new RabbitService(
                "Config.json",
                serializationProvider,
                encryptionProvider,
                compressionProvider,
                loggerFactory);

            services.AddSingleton(
                s =>
                {
                    return new ConsumerDataflowService(
                        s.GetRequiredService<IConfiguration>(),
                        loggerFactory,
                        rabbitService,
                        serializationProvider,
                        compressionProvider,
                        encryptionProvider,
                        metricsProvider);
                });
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            app.UseRouting();
            app.UseHttpMetrics();

            app.UseAuthorization();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers();
                endpoints.MapMetrics();
            });
        }
    }
}
