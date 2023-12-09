using System.IO;

using Engine;
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

        protected override SoundImportSettings GetDefaultSettings()
        {
            return new SoundImportSettings();
        }

        protected override SoundData OnImportAsset(string assetPath, AssetMeta assetMeta)
        {
            using SoundStream stream = new SoundStream(File.OpenRead(assetPath));

            SoundData soundData = new SoundData();

            soundData.SetDataBuffer(stream.ToDataStream());
            soundData.SetWaveFormat(stream.Format);
            soundData.SetDecodedPacketsInfo(stream.DecodedPacketsInfo);

            return soundData;
        }
    }
}