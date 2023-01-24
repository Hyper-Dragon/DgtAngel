using Microsoft.Extensions.Logging;
using NAudio.Wave;
using System.Threading.Channels;

namespace DgtCherub.Helpers
{
    public interface ISequentialVoicePlayer
    {
        public float Volume { get; set; }
        void Speak(IEnumerable<UnmanagedMemoryStream> clipStreams);
        void Speak(UnmanagedMemoryStream clipStream);
        public void Start(int queueLength = 1);
    }

    public class SequentialVoicePlayer : ISequentialVoicePlayer
    {
        private readonly ILogger _logger;
        private Channel<IEnumerable<UnmanagedMemoryStream>> playlistChannel;
        private volatile float volume = 0.7f;
        private WaveOutEvent outputDevice = null;

        public float Volume
        {
            get => volume; set => volume = value is < 0 or > 1 ? throw new ArgumentOutOfRangeException("Volume", "Volume should be between 0-1") : value;
        }

        public SequentialVoicePlayer(ILogger<SequentialVoicePlayer> logger)
        {
            _logger = logger;
            _logger?.LogTrace($"Creating the Sequential Voice Player");
        }

        public void Start(int queueLength)
        {
            _logger?.LogTrace($"Starting the Sequential Voice Player");

            BoundedChannelOptions playlistChannelOptions = new(queueLength)
            {
                AllowSynchronousContinuations = true,
                FullMode = BoundedChannelFullMode.DropOldest,
                SingleReader = true,
                SingleWriter = false
            };

            playlistChannel = Channel.CreateBounded<IEnumerable<UnmanagedMemoryStream>>(playlistChannelOptions);

            _ = Task.Run(RunPlaylistProcessor);
        }

        private async void RunPlaylistProcessor()
        {
            _logger?.LogTrace($"Running Audio Process");

            // Init the channels and start the processor
            outputDevice = new() { Volume = volume };
            _logger?.LogTrace($"Sequential Voice Player Has Valid Audio Device");

            while (true)
            {
                try
                {
                    while (true)
                    {
                        IEnumerable<UnmanagedMemoryStream> playlist = await playlistChannel.Reader.ReadAsync();

                        foreach (UnmanagedMemoryStream sound in playlist)
                        {
                            using WaveFileReader reader = new(sound);
                            if (outputDevice.Volume != volume)
                            {
                                outputDevice.Volume = volume;
                            }

                            outputDevice.Init(reader);
                            outputDevice.Play();
                            while (outputDevice.PlaybackState == PlaybackState.Playing) { await Task.Delay(50); };
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger?.LogError($"Sequential Voice Player Failed (Retrying)", ex.Message);
                    await Task.Delay(5000);
                }
            }
        }

        public void Speak(IEnumerable<UnmanagedMemoryStream> clipStreams)
        {
            _logger?.LogTrace($"Speak Called");
            _ = playlistChannel.Writer.TryWrite(clipStreams);
        }

        public void Speak(UnmanagedMemoryStream clipStream)
        {
            _logger?.LogTrace($"Speak Called");

            List<UnmanagedMemoryStream> tmp = new()
            {
                clipStream
            };

            _ = playlistChannel.Writer.TryWrite(tmp);
        }

    }
}
