#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace HarareAfterHours.EditorTools
{
    [InitializeOnLoad]
    public static class FirstStreetReview
    {
        const string Key = "Harare.FirstStreetReview";
        static int frames;
        static double deadline;
        static FirstStreetReview()
        {
            EditorApplication.update += Tick;
            EditorApplication.playModeStateChanged += state =>
            {
                if (state == PlayModeStateChange.EnteredPlayMode)
                {
                    frames = 0;
                    deadline = EditorApplication.timeSinceStartup + 90;
                }
            };
        }
        public static void Baseline() => Run("before");
        public static void After() => Run("after");
        static void Run(string stage)
        {
            EditorSceneManager.OpenScene("Assets/Scenes/SampleScene.unity");
            SessionState.SetString(Key, stage);
            EditorApplication.isPlaying = true;
        }
        static void Tick()
        {
            string stage = SessionState.GetString(Key, "");
            if (stage == "" || !EditorApplication.isPlaying) return;
            try
            {
                var game = UnityEngine.Object.FindFirstObjectByType<HarareAfterHoursBootstrap>();
                if (game == null || game.Player == null || Camera.main == null)
                {
                    if (deadline > 0 && EditorApplication.timeSinceStartup > deadline)
                        throw new Exception("Gameplay did not initialise.");
                    return;
                }
                if (++frames < 100) return;
                var buildings = UnityEngine.Object.FindObjectsByType<Transform>(FindObjectsSortMode.None)
                    .Where(t => t.name == "Harare building")
                    .OrderBy(t => t.position.x).ThenBy(t => t.position.z)
                    .Select(t => $"{t.position.x:F4},{t.position.y:F4},{t.position.z:F4}|{t.localScale.x:F4},{t.localScale.y:F4},{t.localScale.z:F4}");
                File.WriteAllLines("/private/tmp/harare-first-street-review/" + stage + "-buildings.txt", buildings);
                Camera camera = Camera.main;
                camera.GetComponent<ThirdPersonCameraRig>().enabled = false;
                Capture(camera, new Vector3(-3, 2.0f, -3), new Vector3(21, 8, 22), stage + "-corner");
                Capture(camera, new Vector3(5, 1.8f, 9), new Vector3(12, 3, 28), stage + "-pavement");
                Debug.Log("[FirstStreetReview] Captured " + stage);
                SessionState.EraseString(Key);
                EditorApplication.isPlaying = false;
                EditorApplication.Exit(0);
            }
            catch (Exception error)
            {
                Debug.LogException(error);
                SessionState.EraseString(Key);
                EditorApplication.Exit(1);
            }
        }
        static void Capture(Camera camera, Vector3 position, Vector3 target, string name)
        {
            camera.transform.position = position;
            camera.transform.LookAt(target);
            camera.fieldOfView = 65;
            RenderTexture rt = new(1280, 800, 24);
            camera.targetTexture = rt;
            // Warm the off-screen render after changing camera/target state.
            // The first manual SRP render can carry stale per-object buffers.
            camera.Render();
            camera.Render();
            RenderTexture.active = rt;
            Texture2D image = new(1280, 800, TextureFormat.RGB24, false);
            image.ReadPixels(new Rect(0, 0, 1280, 800), 0, 0);
            image.Apply();
            Directory.CreateDirectory("/private/tmp/harare-first-street-review");
            File.WriteAllBytes("/private/tmp/harare-first-street-review/" + name + ".png", image.EncodeToPNG());
            camera.targetTexture = null;
            RenderTexture.active = null;
            UnityEngine.Object.DestroyImmediate(image);
            rt.Release();
            UnityEngine.Object.DestroyImmediate(rt);
        }
    }
}
#endif
