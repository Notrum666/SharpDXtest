using SharpDX.Multimedia;
using SharpDX.XAudio2;

namespace Engine
{
    public class Sound : BaseAsset
    {
        public AudioBuffer Buffer { get; private set; }
        public WaveFormat Format { get; private set; }
        public uint[] DecodedPacketsInfo { get; private set; }

        private bool disposed = false;

        public Sound(AudioBuffer buffer, WaveFormat format, uint[] decodedPacketsInfo)
        {
            Buffer = buffer;
            Format = format;
            DecodedPacketsInfo = decodedPacketsInfo;
        }

        protected override void Dispose(bool disposing)
        {
            if (disposed)
                return;

            if (disposing)
            {
                Buffer.Stream.Dispose();
                Format = null;
                DecodedPacketsInfo = null;
            }
            disposed = true;

            base.Dispose(disposing);
        }
    }
}