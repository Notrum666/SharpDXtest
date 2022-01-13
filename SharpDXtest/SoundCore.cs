using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using SharpDX.XAudio2;

namespace SharpDXtest
{
    public static class SoundCore
    {
        private static XAudio2 device;
        private static MasteringVoice masteringVoice;
        private static bool disposed = false;
        public static XAudio2 CurrentDevice { get => device; }
        public static void Init()
        {
            device = new XAudio2();
            masteringVoice = new MasteringVoice(device, 2, 44100);
        }
        public static void Play(Sound sound)
        {
            SourceVoice source = new SourceVoice(device, sound.Format, true);
            source.BufferEnd += (context) =>
            {
                source.DestroyVoice();
                //source.Dispose();
            };
            source.SubmitSourceBuffer(sound.Buffer, sound.DecodedPacketsInfo);
            source.Start();
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
