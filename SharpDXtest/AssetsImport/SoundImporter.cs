using Engine.AssetsData;
using SharpDX.Multimedia;

namespace Editor.AssetsImport
{
    [AssetImporter("wav")]
    public class SoundImporter : AssetImporter
    {
        public class SoundImportSettings : BaseImportSettings
        {
            public string Name = "SoundShit";
        }

        protected override void OnImportAsset(AssetImportContext importContext)
        {
            SoundImportSettings soundSettings = importContext.GetImportSettings<SoundImportSettings>();
            using SoundStream soundStream = new SoundStream(importContext.DataStream);

            SoundData soundData = new SoundData();

            soundData.SetDataBuffer(soundStream.ToDataStream());
            soundData.SetWaveFormat(soundStream.Format);
            soundData.SetDecodedPacketsInfo(soundStream.DecodedPacketsInfo);

            importContext.AddMainAsset(soundData);
        }
    }
}