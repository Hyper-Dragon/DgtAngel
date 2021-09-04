﻿using Microsoft.Extensions.Logging;
using NAudio.Wave;
using System.Threading.Channels;

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
            get => volume; set
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

        public SequentialVoicePlayer(ILogger<SequentialVoicePlayer> logger)
        {
            _logger = logger;

            _logger?.LogTrace($"Sequential Voice Player Created");

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
            _logger?.LogTrace($"Running Audio Process");

            while (true)
            {
                IEnumerable<UnmanagedMemoryStream> playlist = await playlistChannel.Reader.ReadAsync();

                foreach (UnmanagedMemoryStream sound in playlist)
                {
                    using WaveFileReader reader = new(sound);
                    using WaveOutEvent outputDevice = new();
                    outputDevice.Volume = volume;
                    outputDevice.Init(reader);
                    outputDevice.Play();
                    while (outputDevice.PlaybackState == PlaybackState.Playing) { Thread.Sleep(50); };
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

            List<UnmanagedMemoryStream> tmp = (new List<UnmanagedMemoryStream>());
            tmp.Add(clipStream);

            _ = playlistChannel.Writer.TryWrite(tmp);
        }

    }
}
