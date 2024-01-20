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

        public Sound UpdateSound(AudioBuffer buffer, WaveFormat format, uint[] decodedPacketsInfo)
        {
            Buffer?.Stream?.Dispose();

            Buffer = buffer;
            Format = format;
            DecodedPacketsInfo = decodedPacketsInfo;
            return this;
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