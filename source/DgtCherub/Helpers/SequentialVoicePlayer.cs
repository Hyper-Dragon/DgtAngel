using Microsoft.Extensions.Logging;
using NAudio.Wave;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;

namespace DgtCherub.Helpers
{
    public interface ISequentialVoicePlayer
    {
        public float Volume { get; set; }
        void Speak(IEnumerable<UnmanagedMemoryStream> clipStreams);
        void Speak(UnmanagedMemoryStream clipStream);
    }

    public class SequentialVoicePlayer : ISequentialVoicePlayer
    {
        private readonly ILogger _logger;
        private readonly Channel<IEnumerable<UnmanagedMemoryStream>> playlistChannel;
        private volatile float volume = 0.7f;

        public float Volume
        {
            get => volume ; set
            {
                if (value < 0 || value > 1)
                {
                    throw new ArgumentOutOfRangeException("Volume", "Volume should be between 0-1");
                }
                else
                {
                    volume = value;
                }
            }
        }

        public SequentialVoicePlayer(ILogger<Form1> logger)
        {
            _logger = logger;

            BoundedChannelOptions playlistChannelOptions = new(1)
            {
                AllowSynchronousContinuations = true,
                FullMode = BoundedChannelFullMode.DropOldest,
                SingleReader = true,
                SingleWriter = false
            };

            // Init the channels and run the processor
            playlistChannel = Channel.CreateBounded<IEnumerable<UnmanagedMemoryStream>>(playlistChannelOptions);
            Task.Run(() => RunPlaylistProcessor());
        }

        private async void RunPlaylistProcessor()
        {
            while (true)
            {
                IEnumerable<UnmanagedMemoryStream> playlist = await playlistChannel.Reader.ReadAsync();

                foreach (UnmanagedMemoryStream sound in playlist)
                {
                    using WaveFileReader reader = new(sound);
                    using WaveOutEvent outputDevice = new();
                    outputDevice.Volume = (float)volume;
                    outputDevice.Init(reader);
                    outputDevice.Play();
                    while (outputDevice.PlaybackState == PlaybackState.Playing) { Thread.Sleep(50); };
                }
            }
        }

        public void Speak(IEnumerable<UnmanagedMemoryStream> clipStreams)
        {
            _ = playlistChannel.Writer.TryWrite(clipStreams);
        }

        public void Speak(UnmanagedMemoryStream clipStream)
        {
            List<UnmanagedMemoryStream> tmp = (new List<UnmanagedMemoryStream>());
            tmp.Add(clipStream);

            _ = playlistChannel.Writer.TryWrite(tmp);
        }

    }
}
