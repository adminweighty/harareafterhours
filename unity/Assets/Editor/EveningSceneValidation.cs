#if UNITY_EDITOR
using System;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace HarareAfterHours.EditorTools
{
    [InitializeOnLoad]
    public static class EveningSceneValidation
    {
        const string Key = "Harare.EveningSceneValidation";
        const string Folder = "/private/tmp/harare-evening-validation/";
        static int _frame;
        static string _runtimeError;

        static EveningSceneValidation()
        {
            EditorApplication.update += Tick;
            Application.logMessageReceived += (message, stack, type) =>
            {
                if (SessionState.GetBool(Key, false) && (type == LogType.Error || type == LogType.Exception || type == LogType.Assert))
                    _runtimeError = message;
            };
        }

        public static void Run()
        {
            Directory.CreateDirectory(Folder);
            PreserveString("graphics", "Harare.Graphics.v1");
            PreserveInt("intro", JoinaCityBlock.IntroSaveKey);
            PlayerPrefs.SetString("Harare.Graphics.v1", "Balanced");
            PlayerPrefs.Save();
            EditorSceneManager.OpenScene("Assets/Scenes/SampleScene.unity");
            SessionState.SetBool(Key, true);
            EditorApplication.isPlaying = true;
        }

        static void Tick()
        {
            if (!SessionState.GetBool(Key, false) || !EditorApplication.isPlaying || _frame == Time.frameCount) return;
            _frame = Time.frameCount;
            if (Time.time < 4f) return;
            try
            {
                Require(string.IsNullOrEmpty(_runtimeError), "Runtime error: " + _runtimeError);
                var rig = EveningLightingRig.Instance;
                Require(rig != null, "Evening lighting rig was not created");
                Require(RenderSettings.skybox != null && RenderSettings.skybox.name.Contains("Black Night"), "Runtime black night sky is active");
                Require(RenderSettings.ambientMode == AmbientMode.Custom, "Explicit runtime ambient probe mode");
                Require(RenderSettings.ambientSkyColor.grayscale > .60f && RenderSettings.ambientGroundColor.grayscale > .30f, "Ambient light is bright enough to read while remaining dusk-weighted");
                Require(RenderSettings.fog && RenderSettings.fogMode == FogMode.Linear, "Linear evening distance fog");
                Require(RenderSettings.fogColor.b > RenderSettings.fogColor.r, "Fog carries the cool evening horizon color");
                Require(RenderSettings.sun == rig.Sun && rig.Sun.enabled && rig.Sun.intensity >= 1f, "Low warm evening sun is authoritative");
                Require(rig.Sun.useColorTemperature && rig.Sun.colorTemperature >= 4500f && rig.Sun.colorTemperature <= 5000f, "Sun uses warm evening color temperature");
                Require(rig.SkyFill.enabled && rig.SkyFill.shadows == LightShadows.None && rig.SkyFill.intensity >= .6f, "Cool fill stays within the mobile light budget");

                int shadowedDirectionals = 0;
                foreach (Light light in UnityEngine.Object.FindObjectsByType<Light>(FindObjectsSortMode.None))
                    if (light.enabled && light.type == LightType.Directional && light.shadows != LightShadows.None) shadowedDirectionals++;
                Require(shadowedDirectionals == 1, "Exactly one directional light casts shadows");
                Require(Camera.main != null && Camera.main.clearFlags == CameraClearFlags.Skybox, "Gameplay camera renders the evening sky");
                var cameraRig = Camera.main.GetComponent<ThirdPersonCameraRig>();
                Require(cameraRig != null && cameraRig.PlayerView, "PLAYER VIEW is the default camera mode");
                var game = HarareAfterHoursBootstrap.Instance;
                Vector3 feet = Camera.main.WorldToViewportPoint(game.Player.transform.position + Vector3.up * .08f);
                Vector3 head = Camera.main.WorldToViewportPoint(game.Player.transform.position + Vector3.up * 1.82f);
                Require(feet.z > 0 && head.z > 0 && feet.y > .03f && head.y < .97f, "Default camera keeps the complete player character in frame");
                var streets = AdaptiveStreetLighting.Instance;
                Require(streets != null && streets.FixtureCount >= 50, "Street lamps cover the CBD, service route and Joina approach");
                Require(streets.ActiveLightCount >= 6 && streets.ActiveLightCount <= AdaptiveStreetLighting.MobileLightBudget, "Nearby street lights are active inside the mobile light budget");
                foreach (Light light in UnityEngine.Object.FindObjectsByType<Light>(FindObjectsSortMode.None))
                    if (light.name.Contains("Lamp light") || light.name.Contains("street light")) Require(light.shadows == LightShadows.None, "Mobile street lights never cast real-time shadows");

                var graphics = CityGraphics.Instance;
                Require(graphics != null && graphics.Preset == "Balanced", "Balanced mobile graphics preset is active");
                ColorAdjustments colour = null;
                Require(graphics.RuntimeProfile != null && graphics.RuntimeProfile.TryGet(out colour), "Runtime color grade exists");
                Require(colour.postExposure.value >= .5f && colour.contrast.value == 0f, "Readable evening exposure and contrast are applied");
                Finish(0, "PASS: readable 18:30 lighting, 50+ street fixtures, capped nearby shadowless lights, evening sky/fog/reflections and mobile Balanced grade. Real preferences restored after validation.");
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
                Finish(1, "FAIL: " + exception);
            }
        }

        static void Require(bool condition, string message)
        {
            if (!condition) throw new Exception(message);
        }

        static void PreserveString(string slot, string pref)
        {
            SessionState.SetBool(Key + slot + "Has", PlayerPrefs.HasKey(pref));
            SessionState.SetString(Key + slot, PlayerPrefs.GetString(pref, ""));
        }

        static void PreserveInt(string slot, string pref)
        {
            SessionState.SetBool(Key + slot + "Has", PlayerPrefs.HasKey(pref));
            SessionState.SetInt(Key + slot, PlayerPrefs.GetInt(pref));
        }

        static void Finish(int code, string report)
        {
            if (SessionState.GetBool(Key + "graphicsHas", false)) PlayerPrefs.SetString("Harare.Graphics.v1", SessionState.GetString(Key + "graphics", "Balanced"));
            else PlayerPrefs.DeleteKey("Harare.Graphics.v1");
            if (SessionState.GetBool(Key + "introHas", false)) PlayerPrefs.SetInt(JoinaCityBlock.IntroSaveKey, SessionState.GetInt(Key + "intro", 0));
            else PlayerPrefs.DeleteKey(JoinaCityBlock.IntroSaveKey);
            PlayerPrefs.Save();
            File.WriteAllText(Folder + "validation.txt", report);
            Debug.Log(report);
            SessionState.EraseBool(Key);
            EditorApplication.isPlaying = false;
            EditorApplication.Exit(code);
        }
    }
}
#endif
