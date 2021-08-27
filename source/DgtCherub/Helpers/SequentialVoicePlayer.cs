using Microsoft.Extensions.Logging;
using NAudio.Wave;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using static DgtCherub.Helpers.ISequentialVoicePlayer;

namespace DgtCherub.Helpers
{
    public interface ISequentialVoicePlayer
    {
        public int Volume { get; set; }
        void Speak(IEnumerable<UnmanagedMemoryStream> clipStreams);
        void Speak(UnmanagedMemoryStream clipStream);
    }

    public class SequentialVoicePlayer : ISequentialVoicePlayer
    {
        private readonly ILogger _logger;
        private readonly Channel<IEnumerable<UnmanagedMemoryStream>> playlistChannel;
        private volatile int volume = 10;

        public int Volume
        {
            get => volume; set
            {
                if (value < 0 || value > 10)
                {
                    throw new ArgumentOutOfRangeException("Volume", "Volume should be 0-10");
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
                    outputDevice.Init(reader);
                    outputDevice.Play();
                    while (outputDevice.PlaybackState == PlaybackState.Playing) { Thread.Sleep(100); };
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
