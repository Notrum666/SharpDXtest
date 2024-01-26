using System;
using System.Collections.Generic;
using System.Linq;

using Engine.BaseAssets.Components;

using SharpDX.Multimedia;
using SharpDX.X3DAudio;
using SharpDX.XAudio2;

namespace Engine
{
    public class PlayingSound : IDisposable
    {
        public SourceVoice voice;
        public Emitter source;
        public readonly int ChannelCount;

        private bool isFinished = false;
        public bool IsFinished => isFinished;
        private bool isPaused = false;
        public bool IsPaused
        {
            get => isPaused;
            set
            {
                if (value)
                {
                    isPaused = value;
                    if (!EngineCore.IsPaused)
                        voice.Start();
                }
                else
                {
                    isPaused = value;
                    if (!EngineCore.IsPaused)
                        voice.Stop();
                }
            }
        }

        private bool disposed;

        public PlayingSound(SourceVoice voice, int channelCount, Emitter source = null)
        {
            this.voice = voice;
            voice.BufferEnd += onBufferEnd;
            ChannelCount = channelCount;
            this.source = source;
        }

        private void onBufferEnd(nint context)
        {
            isFinished = true;
            voice.BufferEnd -= onBufferEnd;
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!disposed)
            {
                if (disposing) { }
                voice.DestroyVoice();
                //voice.Dispose();
                disposed = true;
            }
        }

        ~PlayingSound()
        {
            Dispose(false);
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
    }
    public static class SoundCore
    {
        private static XAudio2 device;
        private static X3DAudio device3d;
        private static MasteringVoice masteringVoice;
        private static bool disposed = false;
        public static XAudio2 CurrentDevice => device;
        public static SoundListener CurrentListener { get; set; }

        private static List<PlayingSound> playingSounds = new List<PlayingSound>();
        public static IReadOnlyCollection<PlayingSound> PlayingSounds => playingSounds.AsReadOnly();

        public static void Init()
        {
            device = new XAudio2();
            device3d = new X3DAudio(Speakers.FrontLeft | Speakers.FrontRight | Speakers.FrontCenter | Speakers.LowFrequency |
                                    Speakers.BackLeft | Speakers.BackRight | Speakers.SideLeft | Speakers.SideRight);
            masteringVoice = new MasteringVoice(device, 8, 44100);

            EngineCore.OnPaused += OnPaused;
            EngineCore.OnResumed += OnResumed;
            SceneManager.OnSceneUnloading += _ => ClearPlayingSounds();
        }

        private static void OnPaused()
        {
            foreach (PlayingSound sound in playingSounds)
            {
                if (!sound.IsPaused)
                    sound.voice.Stop(1);
            }
            device.CommitChanges(1);
        }

        private static void OnResumed()
        {
            foreach (PlayingSound sound in playingSounds)
            {
                if (!sound.IsPaused)
                    sound.voice.Start(1);
            }
            device.CommitChanges(1);
        }

        private static void ClearPlayingSounds()
        {
            List<PlayingSound> sounds = playingSounds.ToList();

            playingSounds.Clear();
            foreach (PlayingSound playingSound in sounds)
            {
                playingSound.Dispose();
            }
        }

        public static PlayingSound PlayFrom(Sound sound, SoundSource source)
        {
            SourceVoice voice = new SourceVoice(device, sound.Format, VoiceFlags.None, 1024f, true);
            voice.SetOutputVoices(new VoiceSendDescriptor(masteringVoice));
            //float[] volumes = new float[16] { 1f, 1f, 1f, 1f, 1f, 1f, 1f, 1f,
            //                                  1f, 1f, 1f, 1f, 1f, 1f, 1f, 1f };
            //source.SetOutputMatrix(2, 8, volumes);
            //source.SetVolume(0.5f);
            //source.GetOutputMatrix(masteringVoice, 8, 8, volumes);
            //float[] volumes = new float[16];
            //source.GetOutputMatrix(masteringVoice, 2, 8, volumes);
            //source.SetEffectChain(new EffectDescriptor(new Reverb(device), 2));
            //source.SetEffectParameters<ReverbParameters>(0, new ReverbParameters() { RoomSize = 1f, Diffusion = 0f });
            PlayingSound playingSound = new PlayingSound(voice, sound.Format.Channels, source == null ? null : source.Source);
            playingSounds.Add(playingSound);
            voice.SubmitSourceBuffer(sound.Buffer, sound.DecodedPacketsInfo);
            voice.Start();

            return playingSound;
        }

        public static PlayingSound Play(Sound sound)
        {
            return PlayFrom(sound, null);
        }

        internal static void Update()
        {
            for (int i = 0; i < playingSounds.Count; i++)
            {
                PlayingSound sound = playingSounds[i];
                if (sound.IsFinished)
                {
                    playingSounds.RemoveAt(i);
                    i--;
                    continue;
                }
                if (CurrentListener != null && sound.source is {} emitter)
                {
                    DspSettings settings = device3d.Calculate(CurrentListener.Listener, emitter, CalculateFlags.Matrix |
                                                                                                                 CalculateFlags.Doppler /*|
                                                                                                                 CalculateFlags.Reverb*/, sound.ChannelCount, 8);

                    sound.voice.SetFrequencyRatio(settings.DopplerFactor, 2);
                    sound.voice.SetOutputMatrix(sound.ChannelCount, 8, settings.MatrixCoefficients, 2);
                    device.CommitChanges(2);
                }
            }
        }

        public static void Dispose()
        {
            if (!disposed)
            {
                masteringVoice.Dispose();
                device.Dispose();
                disposed = true;
            }
        }
    }
}