using System;
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace HarareAfterHours.Editor
{
    /// <summary>
    /// Keeps direct material references in Resources so player builds retain
    /// the shaders used by the procedural city. Shader.Find alone is not a
    /// build dependency and therefore is unsafe on iOS after stripping.
    /// </summary>
    public static class RuntimeMaterialBuildSupport
    {
        private const string LitTemplatePath = "Assets/Resources/HarareRuntime/LitFallback.mat";
        private const string UnlitTemplatePath = "Assets/Resources/HarareRuntime/UnlitFallback.mat";

        public static void EnsureTemplates()
        {
            // iOS selects its renderer through the active quality tier. The
            // project intentionally leaves GraphicsSettings' global pipeline
            // empty, so checking only that field would incorrectly generate
            // built-in Standard materials for a URP player.
            bool usesUniversalRenderPipeline = GraphicsSettings.currentRenderPipeline != null ||
                                               GraphicsSettings.defaultRenderPipeline != null ||
                                               QualitySettings.renderPipeline != null;
            CreateOrRefreshTemplate(
                LitTemplatePath,
                usesUniversalRenderPipeline ? "Universal Render Pipeline/Lit" : "Standard",
                "Harare runtime lit material"
            );
            CreateOrRefreshTemplate(
                UnlitTemplatePath,
                usesUniversalRenderPipeline ? "Universal Render Pipeline/Unlit" : "Unlit/Texture",
                "Harare runtime unlit material"
            );
            AssetDatabase.SaveAssets();
        }

        private static void CreateOrRefreshTemplate(string assetPath, string shaderName, string materialName)
        {
            Shader shader = Shader.Find(shaderName);
            if (shader == null)
            {
                throw new InvalidOperationException($"Required runtime shader was not found: {shaderName}");
            }

            Material material = AssetDatabase.LoadAssetAtPath<Material>(assetPath);
            if (material == null)
            {
                string directory = Path.GetDirectoryName(assetPath);
                if (!string.IsNullOrEmpty(directory)) Directory.CreateDirectory(directory);
                material = new Material(shader) { name = materialName };
                AssetDatabase.CreateAsset(material, assetPath);
            }
            else
            {
                material.shader = shader;
                material.name = materialName;
                EditorUtility.SetDirty(material);
            }
        }
    }
}
