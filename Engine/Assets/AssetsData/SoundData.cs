using System;
using System.IO;
using System.Runtime.InteropServices;

using SharpDX;
using SharpDX.Multimedia;
using SharpDX.XAudio2;

namespace Engine.AssetsData
{
    [AssetData<Sound>]
    public class SoundData : AssetData
    {
        private const int PcmNativeSizeInBytes = 16;
        private const int NativeExtraSizeInBytes = 2;
        private const int AdpcmExtraSizeInBytes = 6;
        private const int ExtExtraSizeInBytes = 4 + 16;

        public byte[] AudioDataBuffer;
        public byte[] FormatBuffer;
        public byte[] DecodedPacketsInfo;

        public void SetDataBuffer(DataStream dataStream)
        {
            AudioDataBuffer = Utilities.ReadStream(dataStream);
        }

        public void SetWaveFormat(WaveFormat waveFormat)
        {
            int size = PcmNativeSizeInBytes;
            if (waveFormat.ExtraSize > 0)
                size += NativeExtraSizeInBytes + waveFormat.ExtraSize;
            FormatBuffer = new byte[size];

            nint formatSourcePointer = WaveFormat.MarshalToPtr(waveFormat);
            Utilities.Read(formatSourcePointer, FormatBuffer, 0, size);
            Marshal.FreeHGlobal(formatSourcePointer);
        }

        public void SetDecodedPacketsInfo(uint[] packetsInfo)
        {
            DecodedPacketsInfo = packetsInfo == null ? null : MemoryMarshal.Cast<uint, byte>(packetsInfo.AsSpan()).ToArray();
        }

        public override void Serialize(BinaryWriter writer)
        {
            writer.Write(AudioDataBuffer.Length);
            writer.Write(AudioDataBuffer);

            writer.Write(FormatBuffer.Length);
            writer.Write(FormatBuffer);

            if (DecodedPacketsInfo != null)
            {
                writer.Write(DecodedPacketsInfo.Length);
                writer.Write(DecodedPacketsInfo);
            }
            else
                writer.Write(-1);
        }

        public override void Deserialize(BinaryReader reader)
        {
            int dataLength = reader.ReadInt32();
            AudioDataBuffer = reader.ReadBytes(dataLength);

            int formatLength = reader.ReadInt32();
            FormatBuffer = reader.ReadBytes(formatLength);

            int packetsLength = reader.ReadInt32();
            if (packetsLength >= 0)
                DecodedPacketsInfo = reader.ReadBytes(packetsLength);
            else
                DecodedPacketsInfo = null;
        }

        public override Sound ToRealAsset(BaseAsset targetAsset = null)
        {
            Sound sound = targetAsset as Sound ?? new Sound();

            DataStream dataStream = DataStream.Create(AudioDataBuffer, true, true, pinBuffer: true);
            AudioBuffer audioBuffer = new AudioBuffer(dataStream);
            WaveFormat waveFormat = WaveFormat.MarshalFrom(FormatBuffer);
            uint[] packetsInfo = DecodedPacketsInfo == null ? null : MemoryMarshal.Cast<byte, uint>(DecodedPacketsInfo.AsSpan()).ToArray();

            return sound.UpdateSound(audioBuffer, waveFormat, packetsInfo);
        }
    }
}