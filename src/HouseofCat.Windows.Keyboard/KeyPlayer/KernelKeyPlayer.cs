using HouseofCat.Extensions;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Threading.Channels;
using System.Threading.Tasks;
using static HouseofCat.Windows.InputStructs;

namespace HouseofCat.Windows
{
    public class KernelKeyPlayer
    {
        private Stopwatch SW;

        private Channel<string> KeyEventMessageChannel { get; }
        public ChannelReader<string> KeyEventMessageChannelReader { get; }
        private ChannelWriter<string> KeyEventMessageChannelWriter { get; }

        private bool Playing;
        private Task KeyPlayingTask;

        public KernelKeyPlayer()
        {
            KeyEventMessageChannel = Channel.CreateUnbounded<string>();
            KeyEventMessageChannelReader = KeyEventMessageChannel.Reader;
            KeyEventMessageChannelWriter = KeyEventMessageChannel.Writer;
        }

        // Make this configurable.
        private const long SleepAccuracyAdjustmentInMicroseconds = 1;
        private const int ThresholdAwaitTaskDelayInMilliseconds = 50;

        private const string KeySequenceInputLog = "Key Sequence:\r\n{0}\r\nSimulated Timestamp: {1} μs";
        private const string UknownError = "Unknown error occurred. Exception: {0}\r\nStack: {1}\r\n";
        private const string SendInputError = "Error returned with SendInput. ErrorCode: {0}\r\n";

        public void Play(Dictionary<long, List<KeyEvent>> keyPlaybackBuffer, int interludeDelay)
        {
            if (!Playing)
            {
                KeyPlayingTask = Task.Run(
                    async () =>
                    {
                        var currentTimestamp = 0L;
                        var keyInputSequences = KeyInputSequencer.BuildSequence(keyPlaybackBuffer);
                        var enumerator = keyInputSequences.GetEnumerator();

                        SW = new Stopwatch();
                        SW.Start();

                        while (enumerator.MoveNext())
                        {
                            try
                            {
                                await Task.Delay(interludeDelay);

                                currentTimestamp = SW.ElapsedMicroseconds();
                                var err = HouseofCat.Windows.NativeMethods.SendInput(
                                    (uint)enumerator.Current.InputSequence.Length,
                                    enumerator.Current.InputSequence,
                                    Marshal.SizeOf(typeof(INPUT)));

                                if (err > 1)
                                {
                                    await KeyEventMessageChannelWriter
                                        .WriteAsync(string.Format(SendInputError, err));
                                }
                                else
                                {
                                    await KeyEventMessageChannelWriter
                                        .WriteAsync(
                                            string.Format(KeySequenceInputLog, enumerator.Current.KeySequence, currentTimestamp));
                                }
                            }
                            catch (Exception ex)
                            {
                                await KeyEventMessageChannelWriter
                                    .WriteAsync(
                                        string.Format(UknownError, ex.Message, ex.StackTrace));
                            }
                        }

                        Playing = false;
                    });
            }
        }

        public void PlayWithTimeSynchronization(Dictionary<long, List<KeyEvent>> keyPlaybackBuffer)
        {
            if (!Playing)
            {
                KeyPlayingTask = Task.Run(
                    async () =>
                    {
                        var currentTimestamp = 0L;
                        var keyInputSequences = KeyInputSequencer.BuildSequence(keyPlaybackBuffer);
                        var enumerator = keyInputSequences.GetEnumerator();

                        SW = new Stopwatch();
                        SW.Start();

                        while (enumerator.MoveNext())
                        {
                            try
                            {
                                // Sleep - Long Wait - Low CPU
                                currentTimestamp = SW.ElapsedMicroseconds();
                                var millisecondsToSleep = (enumerator.Current.FrameTimestamp - currentTimestamp) / 1_000.0 - ThresholdAwaitTaskDelayInMilliseconds;
                                if (millisecondsToSleep > SleepAccuracyAdjustmentInMicroseconds)
                                { await Task.Delay((int)millisecondsToSleep); }

                                // NO-OP - Short Wait - High CPU
                                while (SW.ElapsedMicroseconds() < enumerator.Current.FrameTimestamp - SleepAccuracyAdjustmentInMicroseconds) { }

                                currentTimestamp = SW.ElapsedMicroseconds();
                                var err = NativeMethods.SendInput(
                                    (uint)enumerator.Current.InputSequence.Length,
                                    enumerator.Current.InputSequence,
                                    Marshal.SizeOf(typeof(INPUT)));

                                if (err > 1)
                                {
                                    await KeyEventMessageChannelWriter
                                    .WriteAsync(string.Format(SendInputError, err));
                                }
                                else
                                {
                                    await KeyEventMessageChannelWriter
                                    .WriteAsync(
                                        string.Format(KeySequenceInputLog, enumerator.Current.KeySequence, currentTimestamp));
                                }
                            }
                            catch (Exception ex)
                            {
                                await KeyEventMessageChannelWriter
                                    .WriteAsync(
                                        string.Format(UknownError, ex.Message, ex.StackTrace));
                            }
                        }

                        Playing = false;
                    });
            }
        }
    }
}
