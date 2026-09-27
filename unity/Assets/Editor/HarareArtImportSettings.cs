#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

namespace HarareAfterHours.Editor
{
    /// <summary>
    /// Applies practical iOS/Android defaults as the supplied art library is
    /// imported. Full-resolution originals remain in Downloads; Unity stores
    /// compressed runtime copies suited to the embedded mobile build.
    /// </summary>
    public sealed class HarareArtImportSettings : AssetPostprocessor
    {
        private const string ArtRoot = "Assets/Resources/HarareArt/";

        private void OnPreprocessTexture()
        {
            if (!assetPath.StartsWith(ArtRoot)) return;

            TextureImporter importer = (TextureImporter)assetImporter;
            bool isEnvironment = assetPath.Contains("/Environment/");
            importer.textureType = TextureImporterType.Default;
            importer.sRGBTexture = true;
            importer.alphaIsTransparency = true;
            importer.mipmapEnabled = isEnvironment;
            importer.maxTextureSize = isEnvironment ? 2048 : 1024;
            importer.textureCompression = TextureImporterCompression.Compressed;
            importer.compressionQuality = 70;
        }

        private void OnPreprocessAudio()
        {
            if (!assetPath.StartsWith(ArtRoot + "Music/")) return;

            AudioImporter importer = (AudioImporter)assetImporter;
            AudioImporterSampleSettings settings = importer.defaultSampleSettings;
            settings.loadType = AudioClipLoadType.Streaming;
            settings.compressionFormat = AudioCompressionFormat.Vorbis;
            settings.quality = 0.68f;
            settings.preloadAudioData = false;
            importer.defaultSampleSettings = settings;
        }
    }
}
#endif
