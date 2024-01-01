using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Windows.Media.Imaging;
using SharpDX;
using SharpDX.Direct3D11;
using SharpDX.DXGI;

namespace Engine.AssetsData
{
    [AssetData<Texture>]
    public class TextureData : AssetData
    {
        public int PixelWidth;
        public int PixelHeight;
        public Format PixelFormat;
        public byte[] PixelBuffer;

        public int PixelBufferSize => PixelWidth * PixelHeight * PixelFormat.SizeOfInBytes();

        public void SetTextureSize(int pixelWidth, int pixelHeight)
        {
            PixelWidth = pixelWidth;
            PixelHeight = pixelHeight;
        }

        public void SetPixelFormat(Format pixelFormat)
        {
            PixelFormat = pixelFormat;
        }

        public void FillPixelBuffer(BitmapSource source)
        {
            BitmapFrame frame = BitmapFrame.Create(source);
            PngBitmapEncoder pngEncoder = new PngBitmapEncoder() { Frames = new[] { frame } };

            using MemoryStream stream = new MemoryStream();
            pngEncoder.Save(stream);
            PixelBuffer = stream.ToArray();
        }

        public override void Serialize(BinaryWriter writer)
        {
            writer.Write(PixelWidth);
            writer.Write(PixelHeight);
            writer.Write((int)PixelFormat);

            writer.Write(PixelBuffer.Length);
            writer.Write(PixelBuffer);
        }

        public override void Deserialize(BinaryReader reader)
        {
            PixelWidth = reader.ReadInt32();
            PixelHeight = reader.ReadInt32();
            PixelFormat = (Format)reader.ReadInt32();

            int bufferLength = reader.ReadInt32();
            PixelBuffer = reader.ReadBytes(bufferLength);
        }

        public override Texture ToRealAsset()
        {
            Texture2DDescription description = new Texture2DDescription()
            {
                Width = PixelWidth,
                Height = PixelHeight,
                MipLevels = 1,
                ArraySize = 1,
                Format = PixelFormat,
                SampleDescription = new SampleDescription(1, 0),
                Usage = ResourceUsage.Immutable,
                BindFlags = BindFlags.ShaderResource,
                CpuAccessFlags = CpuAccessFlags.None,
                OptionFlags = ResourceOptionFlags.Shared
            };

            GCHandle handle = GCHandle.Alloc(PixelBuffer, GCHandleType.Pinned);
            DataRectangle dataRectangle = new DataRectangle(handle.AddrOfPinnedObject(), PixelWidth * PixelFormat.SizeOfInBytes());
            handle.Free();

            return new Texture(description, dataRectangle);
        }
    }
}