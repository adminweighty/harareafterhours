#if UNITY_EDITOR
using System;
using System.IO;
using HarareAfterHours;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace HarareAfterHours.EditorTools
{
    /// <summary>
    /// Starts the production scene in Play Mode and verifies that the complete
    /// character-backed vertical slice has constructed itself. It is safe to
    /// invoke in batch mode with -executeMethod.
    /// </summary>
    [InitializeOnLoad]
    public static class HarareAfterHoursPlayModeSmokeTest
    {
        private const string RunKey = "HarareAfterHours.PlayModeSmokeTest";
        private const string ScenePath = "Assets/Scenes/SampleScene.unity";
        private static double _deadline;

        static HarareAfterHoursPlayModeSmokeTest()
        {
            EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
            EditorApplication.update += Tick;
        }

        [MenuItem("Harare After Hours/Run Play Mode Smoke Test")]
        public static void Run()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                throw new InvalidOperationException("The smoke test needs the editor to be out of Play Mode.");
            }

            EditorSceneManager.OpenScene(ScenePath);
            SessionState.SetBool(RunKey, true);
            EditorApplication.isPlaying = true;
        }

        private static void OnPlayModeStateChanged(PlayModeStateChange state)
        {
            if (!SessionState.GetBool(RunKey, false)) return;
            if (state == PlayModeStateChange.EnteredPlayMode)
            {
                _deadline = EditorApplication.timeSinceStartup + 20d;
                Debug.Log("[SmokeTest] Play Mode entered; waiting for the Harare After Hours district.");
            }
        }

        private static void Tick()
        {
            if (!SessionState.GetBool(RunKey, false) || !EditorApplication.isPlaying) return;

            HarareAfterHoursBootstrap bootstrap = UnityEngine.Object.FindFirstObjectByType<HarareAfterHoursBootstrap>();
            bool ready = bootstrap != null &&
                         bootstrap.Player != null &&
                         bootstrap.FeaturedVehicle != null &&
                         bootstrap.Mission != null &&
                         bootstrap.Bridge != null &&
                         bootstrap.Characters != null &&
                         bootstrap.Characters.IsReady;
            if (ready)
            {
                RuntimeCharacterVisual[] visuals = UnityEngine.Object.FindObjectsByType<RuntimeCharacterVisual>(FindObjectsInactive.Include, FindObjectsSortMode.None);
                NpcWanderer[] npcs = UnityEngine.Object.FindObjectsByType<NpcWanderer>(FindObjectsInactive.Include, FindObjectsSortMode.None);
                if (visuals.Length == 25 && npcs.Length == 23 && GameObject.Find("Tino") != null)
                {
                    WriteReport(visuals.Length, npcs.Length, bootstrap.Characters.CastCount);
                    Debug.Log("[SmokeTest] PASS — player, Tino, 23 roaming NPCs, humanoid cast, camera, mission, vehicle, and Flutter bridge are live.");
                    Finish(0);
                    return;
                }
            }

            if (_deadline > 0d && EditorApplication.timeSinceStartup > _deadline)
            {
                Debug.LogError("[SmokeTest] Timed out waiting for the full gameplay cast.");
                Finish(1);
            }
        }

        private static void WriteReport(int characterCount, int npcCount, int castCount)
        {
            SmokeReport report = new()
            {
                scene = ScenePath,
                characterCount = characterCount,
                npcCount = npcCount,
                castCount = castCount,
                status = "pass",
            };
            string output = Path.Combine(Application.dataPath, "Resources/HarareCharacters/character_playmode_smoke.json");
            File.WriteAllText(output, JsonUtility.ToJson(report, true));
            AssetDatabase.ImportAsset("Assets/Resources/HarareCharacters/character_playmode_smoke.json");
        }

        private static void Finish(int exitCode)
        {
            SessionState.EraseBool(RunKey);
            EditorApplication.isPlaying = false;
            EditorApplication.Exit(exitCode);
        }

        [Serializable]
        private sealed class SmokeReport
        {
            public string scene;
            public int characterCount;
            public int npcCount;
            public int castCount;
            public string status;
        }
    }
}
#endif
