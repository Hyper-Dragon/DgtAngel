using Microsoft.Extensions.Logging;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Media;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using static DgtCherub.Helpers.ISequentialVoicePlayer;

namespace DgtCherub.Helpers
{
    public interface ISequentialVoicePlayer
    {
        public enum AudioClip { MISMATCH = 0, MATCH, DGT_LC_CONNECTED, DGT_LC_DISCONNECTED, DGT_CONNECTED, DGT_DISCONNECTED, CDC_WATCHING, CDC_NOTWATCHING, DGT_CANT_FIND, BAT_OK, BAT_LOW, BAT_CRIT };
        public int Volume { get; set; }
        void Speak(AudioClip clipName);
        void Speak(UnmanagedMemoryStream clipStream);
        void Speak(IEnumerable<UnmanagedMemoryStream> clipStream);
    }

    public class SequentialVoicePlayer : ISequentialVoicePlayer
    {
        private const string RESOURCE_VOICE_ROOT = "DgtCherub.Assets.Audio";
        private const string RESOURCE_VOICE_NAME = "Speech_en_01";

        private readonly string[] AudioFiles = { "Mismatch-AP.wav" ,
                                                 "Match-AP.wav" ,
                                                 "DgtLcConnected-AP.wav" ,
                                                 "DgtLcDisconnected-AP.wav" ,
                                                 "DgtConnected-AP.wav" ,
                                                 "DgtDisconnected-AP.wav" ,
                                                 "CdcWatching-AP.wav" ,
                                                 "CdcStoppedWatching-AP.wav" ,
                                                 "DgtCantFindBoard-AP.wav",
                                                 "BatteryOk-AP.wav",
                                                 "BatteryLow-AP.wav",
                                                 "BatteryCritical-AP.wav"
                                                };


        private readonly ILogger _logger;
        private readonly SoundPlayer _soundPlayer;
        private readonly SoundPlayer _soundPlayerTime;

        private volatile int volume = 10;

        private readonly ConcurrentQueue<AudioClip> playList = new();

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

        public SequentialVoicePlayer(ILogger<Form1> logger, SoundPlayer soundPlayer, SoundPlayer soundPlayerTime)
        {
            _logger = logger;
            _soundPlayer = soundPlayer;
            _soundPlayerTime = soundPlayerTime;
        }

        public void Speak(IEnumerable<UnmanagedMemoryStream> clipStreams)
        {
            Task.Run(() =>
            {
                foreach (UnmanagedMemoryStream clipStream in clipStreams)
                {
                    if (volume > 0)
                    {
                        _soundPlayerTime.Stream = clipStream;
                        _soundPlayerTime.PlaySync();
                    }
                }
            });
        }

        public void Speak(UnmanagedMemoryStream clipStream)
        {
            _soundPlayerTime.Stream = clipStream;
            _soundPlayerTime.Play();
        }

        public void Speak(AudioClip clipName)
        {
            _logger.LogDebug($"Speaking {clipName} [vol. {Volume}]");

            if (playList.IsEmpty)
            {
                if (volume > 0)
                {
                    playList.Enqueue(clipName);

                    Thread playListPlayer = new(() =>
                    {
                        while (!playList.IsEmpty)
                        {
                            if (playList.TryDequeue(out AudioClip result))
                            {
                                if (Volume > 0)
                                {
                                    using System.IO.Stream audioStream = Assembly.GetExecutingAssembly().GetManifestResourceStream($"{RESOURCE_VOICE_ROOT}.{RESOURCE_VOICE_NAME}.{AudioFiles[((int)result)]}");
                                    _soundPlayer.Stream = audioStream;
                                    _soundPlayer.PlaySync();
                                    _soundPlayer.Stream = null;
                                }
                            }
                        }
                    });

                    playListPlayer.Start();
                }
            }
            else
            {
                if (Volume > 0)
                {
                    playList.Enqueue(clipName);
                }
            }
        }
    }
}
