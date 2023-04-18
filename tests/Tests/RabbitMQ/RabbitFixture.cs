using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using HouseofCat.Compression;
using HouseofCat.Encryption.Providers;
using HouseofCat.Hashing;
using HouseofCat.Hashing.Argon;
using HouseofCat.RabbitMQ;
using HouseofCat.RabbitMQ.Pools;
using HouseofCat.RabbitMQ.Services;
using HouseofCat.Serialization;
using HouseofCat.Utilities.File;
using Microsoft.Extensions.Logging;
using Nito.AsyncEx;
using Xunit.Abstractions;

namespace RabbitMQ
{
    public class RabbitFixture
    {
        private static readonly string EnvironmentRabbitHost = Environment.GetEnvironmentVariable("RABBITMQ_HOST");
        
        private readonly AsyncLazy<bool> _lazyConnectionCheck;
        
        public ITestOutputHelper Output;
        public readonly ISerializationProvider SerializationProvider;
        public readonly IHashingProvider HashingProvider;
        public readonly IEncryptionProvider EncryptionProvider;
        public readonly ICompressionProvider CompressionProvider;

        public static readonly string RabbitHost = 
            string.IsNullOrEmpty(EnvironmentRabbitHost) ? "localhost" : EnvironmentRabbitHost;
        public const int RabbitPort = 5672;
        public const string Passphrase = "SuperNintendoHadTheBestZelda";
        public const string Salt = "SegaGenesisIsTheBestConsole";
        public readonly byte[] HashKey;
        
        public Task<bool> RabbitConnectionCheckAsync => _lazyConnectionCheck.Task;

        public RabbitFixture()
        {
            CompressionProvider = new GzipProvider();
            HashingProvider = new Argon2ID_HashingProvider();
            HashKey = HashingProvider.GetHashKey(Passphrase, Salt, 32);
            EncryptionProvider = new AesGcmEncryptionProvider(HashKey);
            SerializationProvider = new Utf8JsonProvider();
            
            _lazyConnectionCheck = new AsyncLazy<bool>(
                () => CheckRabbitHostConnection().AsTask(), AsyncLazyFlags.ExecuteOnCallingThread);
        }

        public async ValueTask<RabbitOptions> GetOptionsAsync()
        {
            var options =
                await JsonFileReader.ReadFileAsync<RabbitOptions>(Path.Combine("RabbitMQ", "TestConfig.json"))
                    .ConfigureAwait(false);
            await CheckRabbitHostConnectionAndUpdateFactoryOptions(options).ConfigureAwait(false);
            return options;
        }

        public async ValueTask<IChannelPool> GetChannelPoolAsync() =>
            new ChannelPool(await GetOptionsAsync().ConfigureAwait(false));

        public async ValueTask<IRabbitService> GetRabbitServiceAsync() =>
            new RabbitService(
                await GetChannelPoolAsync().ConfigureAwait(false),
                SerializationProvider,
                EncryptionProvider,
                CompressionProvider,
                LoggerFactory
                    .Create(
                        builder => builder.AddConsole().SetMinimumLevel(LogLevel.Information)));

        public async ValueTask<IRabbitService> GetRecoverableRabbitServiceAsync() =>
            new HouseofCat.RabbitMQ.Services.Recoverable.RabbitService(
                await GetOptionsAsync().ConfigureAwait(false),
                SerializationProvider,
                EncryptionProvider,
                CompressionProvider,
                LoggerFactory
                    .Create(
                        builder => builder.AddConsole().SetMinimumLevel(LogLevel.Information)));

        public async ValueTask<bool> CheckRabbitHostConnectionAndUpdateFactoryOptions(RabbitOptions options)
        {
            if (!await RabbitConnectionCheckAsync.ConfigureAwait(false))
            {
                Output?.WriteLine($"Could not connect to RabbitMQ at {RabbitHost}:{RabbitPort}");
				return false;
            }

            if (RabbitHost != "localhost")
            {
                UpdateFactoryOptionsWithHost(options.FactoryOptions);
            }
            Output?.WriteLine($"RabbitMQ listening on {RabbitHost}:{RabbitPort}");
            return true;
        }

        private async ValueTask<bool> CheckRabbitHostConnection()
        {
            try
            {
                using var cts = new CancellationTokenSource(1000);
                using var socket = new Socket(SocketType.Stream, ProtocolType.Tcp);
                await socket.ConnectAsync(new DnsEndPoint(RabbitHost, RabbitPort), cts.Token).ConfigureAwait(false);
                return true;
            }
            catch (Exception ex)
            {
                Output?.WriteLine(
                    $"Exception occurred trying to connect to RabbitMQ at {RabbitHost}:{RabbitPort}; {ex.Message}");
                return false;
            }
        }

        private static void UpdateFactoryOptionsWithHost(FactoryOptions factoryOptions)
        {
            if (factoryOptions.Uri is not null)
            {
                var uri = factoryOptions.Uri;
                var vhost = uri.AbsolutePath == "/" ? uri.AbsolutePath : uri.AbsolutePath[1..];
                var userInfo = uri.UserInfo.Split(':');

                factoryOptions.Uri = null;
                factoryOptions.VirtualHost = vhost;
                factoryOptions.UserName = userInfo[0];
                factoryOptions.Password = userInfo[1];
            }
            else
            {
                factoryOptions.VirtualHost = "/";
                factoryOptions.UserName = "guest";
                factoryOptions.Password = "guest";
            }
            
            factoryOptions.HostName = RabbitHost;
            factoryOptions.Port = RabbitPort;
        }
    }
}
