using HouseofCat.Windows;
using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Windows.KeyRecorder
{
    public partial class MainForm : Form
    {
        private readonly SynchronizationContext syncContext;
        private readonly KernelKeyRecorder Recorder = new KernelKeyRecorder();
        private readonly KernelKeyPlayer Player = new KernelKeyPlayer();

        private bool Recording { get; set; }

        public Task WriteKeysToFormTask { get; set; }
        public Task WriteLogsToFormTask { get; set; }

        public MainForm()
        {
            InitializeComponent();
            syncContext = SynchronizationContext.Current;

            SetupKeyWriterTask();
            SetupLogWriterTask();
        }

        private const string Record = "Record";
        private const string Stop = "Stop";

        private void ButtonEventHandler(object sender, EventArgs e)
        {
            if (sender.Equals(RecordButton))
            {
                if (Recording)
                {
                    Recorder.Stop();
                    RecordButton.Text = Record;
                    Recording = false;
                    PlayButton.Enabled = true;
                }
                else
                {
                    Recorder.Start();
                    RecordButton.Text = Stop;
                    Recording = true;
                    PlayButton.Enabled = false;
                }
            }
            else if (sender.Equals(PlayButton))
            {
                if (TimeSyncCbx.Checked)
                {
                    Player.PlayWithTimeSynchronization(Recorder.KeyPlaybackBuffer);
                }
                else
                { Player.Play(Recorder.KeyPlaybackBuffer, 50); }
            }
            else if (sender.Equals(ClearButton))
            {
                KeyBufferTbx.Text = string.Empty;
                LogBufferTbx.Text = string.Empty;

                KeyCounter = 0;
                LogCounter = 0;

                Recorder.Clear();
                RecordButton.Text = Record;
                Recording = false;
                PlayButton.Enabled = true;
            }
        }

        private void SetupKeyWriterTask()
        {
            WriteKeysToFormTask = Task.Run(
                async () =>
                {
                    while (true)
                    {
                        try
                        {
                            var key = await Recorder.KeyChannelReader.ReadAsync();

                            if (ShowKBBufferCbx.Checked)
                            {
                                UpdateKeyPressesForUI(key);
                            }
                        }
                        catch { }
                    }
                });
        }

        private long KeyCounter = 0;
        private const string KeyMessageFormat = "KeyEvent {0}: {1}\r\n\r\n";
        private void UpdateKeyPressesForUI(string keySequence)
        {
            syncContext.Post(
                new SendOrPostCallback(
                    obj =>
                    {
                        KeyBufferTbx.AppendText(string.Format(KeyMessageFormat, Interlocked.Increment(ref KeyCounter), obj));
                    }), keySequence);
        }

        private void SetupLogWriterTask()
        {
            WriteLogsToFormTask = Task.Run(
                async () =>
                {
                    while (true)
                    {
                        try
                        {
                            var message = await Player.KeyEventMessageChannelReader.ReadAsync();

                            if (ShowLogBufferCbx.Checked)
                            {
                                UpdateLogsForUI(message);
                            }
                        }
                        catch { }
                    }
                });
        }

        private long LogCounter = 0;
        private const string LogMessageFormat = "Play Log {0}: {1}\r\n\r\n";
        private void UpdateLogsForUI(string message)
        {
            syncContext.Post(
                new SendOrPostCallback(
                    obj =>
                    {
                        LogBufferTbx.AppendText(string.Format(LogMessageFormat, Interlocked.Increment(ref LogCounter), message));
                    }), message);
        }

        private const string HomePage = "https://houseofcat.io";
        private void LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            Process.Start(HomePage);
        }
    }
}
