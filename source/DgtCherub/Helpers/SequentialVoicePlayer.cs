using Microsoft.Extensions.Logging;
using System.Collections.Concurrent;
using System.Media;
using System.Reflection;
using System.Threading;
using static DgtCherub.Helpers.ISequentialVoicePlayer;

namespace DgtCherub.Helpers
{
    public interface ISequentialVoicePlayer
    {
        public enum AudioClip { MISMATCH = 0, MATCH, DGT_LC_CONNECTED, DGT_LC_DISCONNECTED, DGT_CONNECTED, DGT_DISCONNECTED, CDC_WATCHING, CDC_NOTWATCHING, DGT_CANT_FIND };
        public bool IsMuted { get; set; }
        public uint Volume { get; set; }
        void Speak(AudioClip clipName);
    }

    public class SequentialVoicePlayer : ISequentialVoicePlayer
    {
        private const string RESOURCE_VOICE_ROOT = "DgtCherub.Assets.Audio";
        private const string RESOURCE_VOICE_NAME = "Speech_en_01";

        private readonly string[] AudioFiles = { "Mismatch.wav" ,
                                                 "Match.wav" ,
                                                 "DgtLcConnected.wav" ,
                                                 "DgtLcDisconnected.wav" ,
                                                 "DgtConnected.wav" ,
                                                 "DgtDisconnected.wav" ,
                                                 "CdcWatching.wav" ,
                                                 "CdcStoppedWatching.wav" ,
                                                 "DgtCantFindBoard.wav"
                                                };

        private readonly ILogger _logger;
        private readonly SoundPlayer _soundPlayer;
        private readonly ConcurrentQueue<AudioClip> playList = new();

        public bool IsMuted { get; set; } = false;
        public uint Volume { get; set; } = 10;

        public SequentialVoicePlayer(ILogger<Form1> logger, SoundPlayer soundPlayer)
        {
            _logger = logger;
            _soundPlayer = soundPlayer;
        }

        public void Speak(AudioClip clipName)
        {
            _logger.LogDebug($"Speaking {clipName} [vol. {Volume} Mute. {IsMuted}]]");

            if (playList.IsEmpty)
            {
                if (!IsMuted)
                {
                    playList.Enqueue(clipName);

                    Thread playListPlayer = new(() =>
                    {
                        while (!playList.IsEmpty)
                        {
                            if (playList.TryDequeue(out AudioClip result))
                            {
                                if (!IsMuted)
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
                if (!IsMuted)
                {
                    playList.Enqueue(clipName);
                }
            }
        }
    }
}
