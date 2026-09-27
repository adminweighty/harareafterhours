#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace HarareAfterHours.Editor
{
    /// <summary>
    /// Configures the supplied mobile vehicle FBXs and turns each LOD set into
    /// a Resources prefab that the runtime city can instantiate for traffic.
    /// </summary>
    public static class VehicleAssetInstaller
    {
        private const string VehicleRoot = "Assets/Resources/HarareVehicles";
        private const string ModelsRoot = VehicleRoot + "/Models";
        private const string MaterialsRoot = VehicleRoot + "/Materials";
        private const string PrefabsRoot = VehicleRoot + "/Prefabs";

        private static readonly float[] LodTransitions = { 0.58f, 0.24f, 0.07f };

        private sealed class VehicleSpec
        {
            public readonly string Name;
            public readonly Color Paint;
            public readonly Vector3 ColliderCenter;
            public readonly Vector3 ColliderSize;

            public VehicleSpec(string name, Color paint, Vector3 colliderCenter, Vector3 colliderSize)
            {
                Name = name;
                Paint = paint;
                ColliderCenter = colliderCenter;
                ColliderSize = colliderSize;
            }

            public string ModelPath(int lod) => $"{ModelsRoot}/{Name}/{Name}_LOD{lod}.fbx";
            public string PrefabPath => $"{PrefabsRoot}/{Name}.prefab";
        }

        private static readonly VehicleSpec[] Vehicles =
        {
            new("Mercedes_GLS_580_2020", new Color(0.82f, 0.84f, 0.88f), new Vector3(0f, 0.91f, 0f), new Vector3(2.16f, 1.82f, 5.21f)),
            new("McLaren_Senna", new Color(0.96f, 0.19f, 0.035f), new Vector3(0f, 0.62f, 0f), new Vector3(2.16f, 1.24f, 4.75f)),
        };

        [MenuItem("Harare After Hours/Install Vehicle Assets")]
        public static void InstallOrUpdateAll()
        {
            EnsureFolder(MaterialsRoot);
            EnsureFolder(PrefabsRoot);
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);

            foreach (VehicleSpec vehicle in Vehicles)
            {
                for (int lod = 0; lod < 3; lod++) ConfigureModelImporter(vehicle.ModelPath(lod));
            }

            foreach (VehicleSpec vehicle in Vehicles) BuildPrefab(vehicle);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
            ValidateInstallation();
            Debug.Log("[Harare Vehicles] Installed two mobile LOD vehicle prefabs for runtime traffic.");
        }

        [MenuItem("Harare After Hours/Validate Vehicle Assets")]
        public static void ValidateInstallation()
        {
            List<string> failures = new();
            foreach (VehicleSpec vehicle in Vehicles)
            {
                GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(vehicle.PrefabPath);
                if (prefab == null)
                {
                    failures.Add($"Missing prefab: {vehicle.PrefabPath}");
                    continue;
                }

                LODGroup group = prefab.GetComponent<LODGroup>();
                if (group == null)
                {
                    failures.Add($"{vehicle.Name} has no LODGroup.");
                    continue;
                }

                LOD[] lods = group.GetLODs();
                if (lods.Length != 3)
                {
                    failures.Add($"{vehicle.Name} has {lods.Length} LODs instead of 3.");
                    continue;
                }

                for (int index = 0; index < lods.Length; index++)
                {
                    if (lods[index].renderers == null || lods[index].renderers.Length == 0)
                    {
                        failures.Add($"{vehicle.Name} LOD{index} has no renderers.");
                    }
                }

                if (vehicle.Name == "McLaren_Senna")
                {
                    int pivots = 0;
                    foreach (Transform child in prefab.GetComponentsInChildren<Transform>(true))
                    {
                        if (child.name.StartsWith("Wheel_") && child.childCount > 0 && child.GetComponent<Renderer>() == null) pivots++;
                    }
                    if (pivots < 4) failures.Add("McLaren wheel pivots were not preserved.");
                }
            }

            if (failures.Count > 0)
            {
                throw new InvalidOperationException("Vehicle installation validation failed:\n- " + string.Join("\n- ", failures));
            }

            string projectRoot = Path.GetDirectoryName(Application.dataPath) ?? throw new InvalidOperationException("Unity project root could not be resolved.");
            string reportPath = Path.Combine(projectRoot, "VehicleIntegrationReport.txt");
            File.WriteAllText(
                reportPath,
                $"Unity {Application.unityVersion}\nVehicle prefabs: {Vehicles.Length}\nLOD variants per vehicle: 3\nMcLaren wheel pivots: preserved\nStatus: passed\n"
            );
            Debug.Log($"[Harare Vehicles] Validation passed. Report: {reportPath}");
        }

        private static void ConfigureModelImporter(string assetPath)
        {
            ModelImporter importer = AssetImporter.GetAtPath(assetPath) as ModelImporter;
            if (importer == null) throw new InvalidOperationException($"Vehicle FBX was not found: {assetPath}");

            importer.globalScale = 1f;
            importer.useFileUnits = true;
            importer.importCameras = false;
            importer.importLights = false;
            importer.importAnimation = false;
            importer.importBlendShapes = false;
            importer.isReadable = false;
            importer.meshCompression = ModelImporterMeshCompression.Medium;
            importer.optimizeMeshVertices = true;
            importer.optimizeMeshPolygons = true;
            // The model exports use empty wheel pivots. Do not optimize those
            // transforms away; the motion scripts address them at runtime.
            importer.preserveHierarchy = true;
            importer.optimizeGameObjects = false;
            importer.materialImportMode = ModelImporterMaterialImportMode.ImportStandard;
            importer.SaveAndReimport();
        }

        private static void BuildPrefab(VehicleSpec vehicle)
        {
            GameObject root = new(vehicle.Name);
            List<LOD> lods = new();

            try
            {
                for (int lod = 0; lod < 3; lod++)
                {
                    GameObject model = AssetDatabase.LoadAssetAtPath<GameObject>(vehicle.ModelPath(lod));
                    if (model == null) throw new InvalidOperationException($"Unable to load {vehicle.ModelPath(lod)}");

                    GameObject instance = PrefabUtility.InstantiatePrefab(model) as GameObject;
                    if (instance == null) throw new InvalidOperationException($"Unable to instantiate {vehicle.ModelPath(lod)}");

                    instance.name = $"LOD{lod}";
                    instance.transform.SetParent(root.transform, false);
                    Renderer[] renderers = instance.GetComponentsInChildren<Renderer>(true);
                    AssignMobileMaterials(vehicle, renderers);
                    lods.Add(new LOD(LodTransitions[lod], renderers));
                }

                LODGroup group = root.AddComponent<LODGroup>();
                group.SetLODs(lods.ToArray());
                group.RecalculateBounds();

                BoxCollider collider = root.AddComponent<BoxCollider>();
                collider.center = vehicle.ColliderCenter;
                collider.size = vehicle.ColliderSize;

                PrefabUtility.SaveAsPrefabAsset(root, vehicle.PrefabPath);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(root);
            }
        }

        private static void AssignMobileMaterials(VehicleSpec vehicle, Renderer[] renderers)
        {
            foreach (Renderer renderer in renderers)
            {
                Material[] materials = renderer.sharedMaterials;
                for (int index = 0; index < materials.Length; index++)
                {
                    // The chassis is one fused mesh in these optimized FBXs.
                    // Give it a deliberate paint finish even when an importer
                    // collapses its source material slots to one default slot.
                    materials[index] = renderer.name.StartsWith("Chassis_", StringComparison.Ordinal)
                        ? GetOrCreateMobileMaterial(vehicle, "BodyPaint")
                        : GetOrCreateMobileMaterial(vehicle, materials[index]);
                }
                renderer.sharedMaterials = materials;
            }
        }

        private static Material GetOrCreateMobileMaterial(VehicleSpec vehicle, Material source)
        {
            return GetOrCreateMobileMaterial(vehicle, source != null ? source.name : "Trim");
        }

        private static Material GetOrCreateMobileMaterial(VehicleSpec vehicle, string sourceName)
        {
            string category = MaterialCategory(sourceName);
            string materialName = $"{vehicle.Name}_{Sanitize(sourceName)}";
            string assetPath = $"{MaterialsRoot}/{materialName}.mat";
            Material material = AssetDatabase.LoadAssetAtPath<Material>(assetPath);
            if (material != null) return material;

            Shader shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
            if (shader == null) throw new InvalidOperationException("Neither URP/Lit nor Standard shader is available.");

            material = new Material(shader) { name = materialName };
            ConfigureMaterial(material, vehicle, category);
            AssetDatabase.CreateAsset(material, assetPath);
            return material;
        }

        private static void ConfigureMaterial(Material material, VehicleSpec vehicle, string category)
        {
            Color color;
            float metallic;
            float smoothness;
            bool emission = false;

            switch (category)
            {
                case "paint":
                    color = vehicle.Paint;
                    metallic = 0.64f;
                    smoothness = 0.78f;
                    break;
                case "glass":
                    color = new Color(0.035f, 0.09f, 0.12f);
                    metallic = 0.12f;
                    smoothness = 0.9f;
                    break;
                case "rubber":
                    color = new Color(0.018f, 0.02f, 0.025f);
                    metallic = 0f;
                    smoothness = 0.44f;
                    break;
                case "lights":
                    color = new Color(1f, 0.78f, 0.38f);
                    metallic = 0.1f;
                    smoothness = 0.82f;
                    emission = true;
                    break;
                case "metal":
                    color = new Color(0.18f, 0.2f, 0.23f);
                    metallic = 0.9f;
                    smoothness = 0.7f;
                    break;
                case "interior":
                    color = new Color(0.045f, 0.052f, 0.06f);
                    metallic = 0f;
                    smoothness = 0.45f;
                    break;
                default:
                    color = new Color(0.1f, 0.12f, 0.14f);
                    metallic = 0.22f;
                    smoothness = 0.52f;
                    break;
            }

            if (material.HasProperty("_BaseColor")) material.SetColor("_BaseColor", color);
            if (material.HasProperty("_Color")) material.color = color;
            if (material.HasProperty("_Metallic")) material.SetFloat("_Metallic", metallic);
            if (material.HasProperty("_Smoothness")) material.SetFloat("_Smoothness", smoothness);
            if (emission)
            {
                material.EnableKeyword("_EMISSION");
                if (material.HasProperty("_EmissionColor")) material.SetColor("_EmissionColor", color * 1.45f);
            }
        }

        private static string MaterialCategory(string materialName)
        {
            string value = materialName.ToLowerInvariant();
            if (value.Contains("tyre") || value.Contains("tire") || value.Contains("rubber") || value.StartsWith("wire_162")) return "rubber";
            if (value.Contains("glass") || value.Contains("window")) return "glass";
            if (value.Contains("light") || value.Contains("lamp")) return "lights";
            if (value.StartsWith("wire_135") || value.Contains("chrome") || value.Contains("metal") || value.Contains("grille")) return "metal";
            if (value.Contains("interior") || value.Contains("seat") || value.Contains("dashboard")) return "interior";
            if (value.Contains("paint") || value.Contains("polar") || value.Contains("orange") || value.Contains("body") || value.Contains("color")) return "paint";
            return "trim";
        }

        private static string Sanitize(string value)
        {
            char[] characters = value.ToCharArray();
            for (int index = 0; index < characters.Length; index++)
            {
                if (!char.IsLetterOrDigit(characters[index])) characters[index] = '_';
            }
            return new string(characters);
        }

        private static void EnsureFolder(string assetPath)
        {
            if (AssetDatabase.IsValidFolder(assetPath)) return;
            string parent = Path.GetDirectoryName(assetPath)?.Replace('\\', '/');
            string name = Path.GetFileName(assetPath);
            if (string.IsNullOrEmpty(parent)) throw new InvalidOperationException($"Invalid asset folder: {assetPath}");
            EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, name);
        }
    }
}
#endif
