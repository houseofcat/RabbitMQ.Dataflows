using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using static HouseofCat.RabbitMQ.LogMessages.ChannelPools;

namespace HouseofCat.RabbitMQ.Pools.Extensions;

public static class ConnectionPoolExtensions
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static async ValueTask<IChannelHost> CreateChannelAsync(
        this IConnectionPool connectionPool, ulong channelId, bool ackable, ILogger logger = null)
    {
        var sleep = connectionPool.Options.PoolOptions.SleepOnErrorInterval;
        IConnectionHost connHost = null;

        while (true)
        {
            logger?.LogTrace(CreateChannel, channelId);

            // Get ConnectionHost
            try
            {
                connHost = await connectionPool.GetConnectionAsync().ConfigureAwait(false);
            }
            catch
            {
                logger?.LogTrace(CreateChannelFailedConnection, channelId);
                await ReturnConnectionWithOptionalSleep(connectionPool, connHost, channelId, sleep, logger)
                    .ConfigureAwait(false);
                continue;
            }

            // Create a Channel Host
            try
            {
                var chanHost =
                    connHost is IRecoveryAwareConnectionHost recoveryAwareConnectionHost
                        ? new RecoveryAwareChannelHost(recoveryAwareConnectionHost, ackable)
                        : new ChannelHost(0, connHost, ackable);
                await ReturnConnectionWithOptionalSleep(connectionPool, connHost, channelId).ConfigureAwait(false);
                logger?.LogDebug(CreateChannelSuccess, channelId);

                return chanHost;
            }
            catch
            {
                logger?.LogTrace(CreateChannelFailedConstruction, channelId);
                await ReturnConnectionWithOptionalSleep(connectionPool, connHost, channelId, sleep, logger)
                    .ConfigureAwait(false);
            }
        }
    }

    public static async ValueTask ReturnConnectionWithOptionalSleep(
        IConnectionPool connectionPool, IConnectionHost connHost, ulong channelId, int sleep = 0, ILogger logger = null)
    {
        if (connHost != null)
        {
            // Return Connection (or lose them.)
            await connectionPool.ReturnConnectionAsync(connHost).ConfigureAwait(false);
        }

        if (sleep > 0)
        {
            logger?.LogDebug(CreateChannelSleep, channelId);

            await Task.Delay(sleep).ConfigureAwait(false);
        }
    }
}