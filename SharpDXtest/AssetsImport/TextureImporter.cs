using System.IO;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Engine.AssetsData;
using SharpDX.DXGI;

namespace Editor.AssetsImport
{
    [AssetImporter("jpg", "png", "bmp")]
    public class TextureImporter : AssetImporter
    {
        public override int LatestVersion => 1;

        public class TextureImportSettings : BaseImportSettings
        {
            public bool ApplyGammaCorrection = false;
        }

        protected override void OnImportAsset(AssetImportContext importContext)
        {
            TextureImportSettings textureSettings = importContext.GetImportSettings<TextureImportSettings>();

            TextureData textureData = DecodeData(importContext.DataStream, textureSettings);

            importContext.AddMainAsset(textureData);
        }

        public static TextureData DecodeData(Stream dataStream, TextureImportSettings textureSettings)
        {
            TextureData textureData = new TextureData();
            
            BitmapDecoder decoder = BitmapDecoder.Create(dataStream, BitmapCreateOptions.None, BitmapCacheOption.Default);
            FormatConvertedBitmap convertedBitmap = new FormatConvertedBitmap(decoder.Frames[0], PixelFormats.Bgra32, decoder.Palette, 0);

            textureData.SetTextureSize(convertedBitmap.PixelWidth, convertedBitmap.PixelHeight);
            textureData.SetPixelFormat(textureSettings.ApplyGammaCorrection ? Format.B8G8R8A8_UNorm_SRgb : Format.B8G8R8A8_UNorm);
            textureData.FillPixelBuffer(convertedBitmap);
            
            return textureData;
        }
    }
}