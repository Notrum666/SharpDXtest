using Engine.AssetsData;
using SharpDX.Multimedia;
using System.IO;

namespace Editor.AssetsImport
{
    [AssetImporter("wav")]
    public class SoundImporter : AssetImporter
    {
        public override int LatestVersion => 2;

        public class SoundImportSettings : BaseImportSettings
        {
            public string Name = "SoundName";
        }

        protected override void OnImportAsset(AssetImportContext importContext)
        {
            SoundImportSettings soundSettings = importContext.GetImportSettings<SoundImportSettings>();
            soundSettings.Name = Path.GetFileNameWithoutExtension(importContext.AssetContentPath);

            using SoundStream soundStream = new SoundStream(importContext.DataStream);

            SoundData soundData = new SoundData();

            soundData.SetDataBuffer(soundStream.ToDataStream());
            soundData.SetWaveFormat(soundStream.Format);
            soundData.SetDecodedPacketsInfo(soundStream.DecodedPacketsInfo);

            importContext.AddMainAsset(soundData);
        }
    }
}