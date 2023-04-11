using System;
using System.Threading.Tasks;
using Xunit.Abstractions;

namespace Utilities;

public class Wait
{
    private readonly TimeSpan _timeout;
    private readonly TimeSpan _interval;
    private const int DefaultTimeout = 15000;
    private const int DefaultInterval = 50;

    public Wait(TimeSpan? timeout = null, TimeSpan? interval = null)
    {
        _timeout = timeout ?? TimeSpan.FromMilliseconds(DefaultTimeout);
        _interval = interval ?? TimeSpan.FromMilliseconds(DefaultInterval);
    }

    public async ValueTask<bool> UntilAsync(
        Func<ValueTask<bool>> condition, Func<double, ValueTask> success, Func<ValueTask> failure)
    {
        await Task.Yield();

        var startTime = DateTime.UtcNow;
        var timeIsUp = startTime.Add(_timeout);
        while (DateTime.UtcNow.CompareTo(timeIsUp) < 0)
        {
            if (await condition().ConfigureAwait(false))
            {
                var timeTaken = DateTime.UtcNow - startTime;
                await success(timeTaken.TotalSeconds).ConfigureAwait(false);
                return true;
            }

            await Task.Delay(_interval).ConfigureAwait(false);
        }

        await failure().ConfigureAwait(false);
        return false;
    }

    public ValueTask<bool> UntilAsync(
        Func<ValueTask<int>> current, int expected, string log, ITestOutputHelper writer, bool throwOnTimeout = true) =>
        UntilAsync(
            async () => await current().ConfigureAwait(false) == expected,
            secondsTaken =>
            {
                writer.WriteLine($"{DateTime.UtcNow:T}: {expected} {log} in {secondsTaken:f4}s");
                return ValueTask.CompletedTask;
            },
            async () =>
            {
                var actual = await current().ConfigureAwait(false);
                var errorMessage =
                    $"{DateTime.UtcNow:T}: {actual} {log} (expected {expected}) after {_timeout.TotalSeconds:f4}s";
                if (throwOnTimeout)
                {
                    throw new TimeoutException(errorMessage);
                }

                writer.WriteLine(errorMessage);
            });
}