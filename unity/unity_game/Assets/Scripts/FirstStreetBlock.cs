using UnityEngine;

namespace HarareAfterHours
{
    /// <summary>Stable identity for the first reference-led replacement plot.</summary>
    [DisallowMultipleComponent]
    public sealed class FirstStreetBlock : MonoBehaviour
    {
        public const int LegacyGridX = 18;
        public const int LegacyGridZ = 18;
        public const string ResourcePath = "HarareEnvironment/FirstStreetCorner";
        public const string ReferenceImages = "10_First_Street_Day.png;11_First_Street_Night.png";

        // Original seeded plot centre and footprint; no road or mission relocation.
        public static readonly Vector3 PlotCentre = new(17.8968f, 0, 19.6303f);
        public const float Width = 14.789f;
        public const float Depth = 10.0182f;

        public static bool EnsurePresent(Transform parent)
        {
            if (FindFirstObjectByType<FirstStreetBlock>() != null) return true;
            GameObject prefab = Resources.Load<GameObject>(ResourcePath);
            if (prefab == null) return false; // Keep the old building if installation is absent.
            GameObject instance = Instantiate(prefab, PlotCentre, Quaternion.identity, parent);
            instance.name = "First Street - reference corner";
            return true;
        }
    }
}
