#if UNITY_EDITOR
using System;
using System.IO;
using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace HarareAfterHours.EditorTools
{
    /// <summary>Batch-safe validation for the imported humanoid and cast registry.</summary>
    public static class HarareAfterHoursCharacterValidation
    {
        private const string CharacterFolder = "Assets/Resources/HarareCharacters";
        private const string PrefabPath = CharacterFolder + "/Character01.prefab";

        [MenuItem("Harare After Hours/Validate Walking Stance")]
        public static void ValidateWalkingStance() => SharedCharacterValidation.Validate();

        [MenuItem("Harare After Hours/Validate Character Integration")]
        public static void Validate()
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
            Require(prefab != null, $"Missing cast prefab: {PrefabPath}");

            LODGroup lodGroup = prefab.GetComponent<LODGroup>();
            Require(lodGroup != null, "Character01 must contain an LODGroup.");
            Require(lodGroup.lodCount == 3, $"Expected 3 LODs, found {lodGroup.lodCount}.");

            int triangleCount = 0;
            for (int index = 0; index < 3; index++)
            {
                string path = $"{CharacterFolder}/Character01_LOD{index}.fbx";
                ModelImporter importer = AssetImporter.GetAtPath(path) as ModelImporter;
                Require(importer != null, $"Missing model importer: {path}");
                Require(importer.animationType == ModelImporterAnimationType.Human, $"{path} is not configured as Humanoid.");
                Require(importer.maxBonesPerVertex <= 4, $"{path} exceeds the four bone-weight mobile budget.");

                Avatar avatar = FindAvatar(path);
                Require(avatar != null && avatar.isValid && avatar.isHuman, $"{path} does not expose a valid Humanoid Avatar.");

                GameObject model = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                Require(model != null, $"Unable to load {path}.");
                SkinnedMeshRenderer[] renderers = model.GetComponentsInChildren<SkinnedMeshRenderer>(true);
                Require(renderers.Length == 1, $"{path} should contain one skinned renderer, found {renderers.Length}.");
                triangleCount += TriangleCount(renderers[0].sharedMesh);
            }

            GameObject testRoot = new("Character integration validation");
            try
            {
                CharacterRoster roster = testRoot.AddComponent<CharacterRoster>();
                roster.Initialize();
                Require(roster.IsReady, "Character roster failed to load the Resources prefab.");
                Require(roster.CastCount == 24, $"Expected 24 cast roles, found {roster.CastCount}.");

                GameObject playerHost = new("Player host");
                playerHost.transform.SetParent(testRoot.transform);
                AvatarCustomization avatar = playerHost.AddComponent<AvatarCustomization>();
                Require(roster.AttachPlayer(avatar), "The player could not receive the rigged visual.");

                for (int index = 0; index < roster.NpcCount; index++)
                {
                    GameObject npc = roster.SpawnNpc(testRoot.transform, index, new Vector3(index * 2f, 0f, 0f), 1f, 0.6f);
                    Require(npc != null, $"NPC {index + 1} could not be spawned.");
                }

                RuntimeCharacterVisual[] visuals = testRoot.GetComponentsInChildren<RuntimeCharacterVisual>(true);
                Require(visuals.Length == 24, $"Expected 24 instantiated rigged characters, found {visuals.Length}.");
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(testRoot);
            }

            Report report = new()
            {
                castCount = 24,
                npcCount = 23,
                lodCount = lodGroup.lodCount,
                totalTriangleBudget = triangleCount,
                avatar = "Humanoid",
                prefab = PrefabPath,
            };
            string outputPath = Path.Combine(Application.dataPath, "Resources/HarareCharacters/character_roster_validation.json");
            File.WriteAllText(outputPath, JsonUtility.ToJson(report, true));
            AssetDatabase.ImportAsset("Assets/Resources/HarareCharacters/character_roster_validation.json");
            Debug.Log($"[Characters] Validated {report.castCount} cast roles. Report: {outputPath}");
        }

        private static Avatar FindAvatar(string path)
        {
            foreach (UnityEngine.Object asset in AssetDatabase.LoadAllAssetsAtPath(path))
            {
                if (asset is Avatar avatar) return avatar;
            }
            return null;
        }

        private static int TriangleCount(Mesh mesh)
        {
            if (mesh == null) return 0;
            int triangles = 0;
            for (int index = 0; index < mesh.subMeshCount; index++)
            {
                triangles += (int)mesh.GetIndexCount(index) / 3;
            }
            return triangles;
        }

        private static void Require(bool condition, string message)
        {
            if (!condition) throw new InvalidOperationException(message);
        }

        [Serializable]
        private sealed class Report
        {
            public int castCount;
            public int npcCount;
            public int lodCount;
            public int totalTriangleBudget;
            public string avatar;
            public string prefab;
        }
    }
}
#endif
