using UnityEngine;

namespace HarareAfterHours
{
    /// <summary>
    /// Creates procedural materials without allowing player-build shader
    /// stripping to abort world construction. Runtime Shader.Find is useful
    /// when available, but the Resources-backed material templates are the
    /// reliable fallback for an iOS player.
    /// </summary>
    internal static class RuntimeMaterialFactory
    {
        public static Material Create(string preferredShaderName, string fallbackShaderName)
        {
            string templatePath = fallbackShaderName == "Unlit/Texture" || fallbackShaderName == "Sprites/Default"
                ? "HarareRuntime/UnlitFallback"
                : "HarareRuntime/LitFallback";
            Material template = Resources.Load<Material>(templatePath);
            if (template != null) return new Material(template);

            Shader shader = Shader.Find(preferredShaderName) ?? Shader.Find(fallbackShaderName);
            if (shader != null) return new Material(shader);

            Debug.LogError($"Runtime material template is missing for {preferredShaderName}.");
            return new Material(Shader.Find("Hidden/InternalErrorShader"));
        }
    }
}
