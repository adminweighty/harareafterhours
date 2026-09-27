using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace HarareAfterHours
{
    // Keeps the black night sky independent of readable street and character lighting.
    public sealed class EveningLightingRig : MonoBehaviour
    {
        public static EveningLightingRig Instance { get; private set; }
        public Light Sun { get; private set; }
        public Light SkyFill { get; private set; }

        Material _sky;
        Material _sourceSky;
        string _preset;

        public void Configure()
        {
            Instance = this;
            foreach (Light light in FindObjectsByType<Light>(FindObjectsSortMode.None))
            {
                if (light.type == LightType.Directional) light.enabled = false;
            }

            Sun = CreateDirectional("Evening sun", Color.white, 1.25f, new Vector3(18f, -110f, 0f));
            Sun.useColorTemperature = true;
            Sun.colorTemperature = 4800f;
            Sun.shadows = LightShadows.Hard;
            Sun.shadowStrength = .40f;
            Sun.shadowBias = .07f;
            Sun.shadowNormalBias = .32f;
            RenderSettings.sun = Sun;

            SkyFill = CreateDirectional("Evening sky fill", new Color(.78f, .85f, 1f), .65f, new Vector3(38f, 128f, 0f));
            SkyFill.shadows = LightShadows.None;

            _sourceSky = RenderSettings.skybox;
            var night = Resources.Load<Material>("HarareEnvironment/NightSky");
            if (night != null) _sky = new Material(night) { name = "Harare Black Night Sky (Runtime)" };
            Apply("Balanced", true);
        }

        Light CreateDirectional(string objectName, Color color, float intensity, Vector3 euler)
        {
            var child = new GameObject(objectName);
            child.transform.SetParent(transform, false);
            child.transform.rotation = Quaternion.Euler(euler);
            var light = child.AddComponent<Light>();
            light.type = LightType.Directional;
            light.color = color;
            light.intensity = intensity;
            return light;
        }

        public void Apply(string preset, bool force = false)
        {
            preset = preset == "High" ? "High" : "Balanced";
            if (!force && _preset == preset && RenderSettings.skybox == _sky) return;
            _preset = preset;

            if (_sky != null)
            {
                RenderSettings.skybox = _sky;
                // Retain the reflection environment for readable cars and equipment;
                // visible sky colour never controls the diffuse character lighting.
                var reflections = Resources.Load<Material>("HarareEnvironment/CityExpansion/PhotographicSky");
                RenderSettings.defaultReflectionMode = DefaultReflectionMode.Custom;
                if (reflections != null) RenderSettings.customReflectionTexture = reflections.GetTexture("_Tex");
            }

            RenderSettings.ambientMode = AmbientMode.Custom;
            RenderSettings.ambientSkyColor = new Color(.65f, .69f, .80f);
            RenderSettings.ambientEquatorColor = new Color(.59f, .60f, .66f);
            RenderSettings.ambientGroundColor = new Color(.39f, .37f, .40f);
            RenderSettings.ambientIntensity = 1f;
            RenderSettings.reflectionIntensity = preset == "High" ? .55f : .48f;
            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.Linear;
            RenderSettings.fogColor = new Color(.012f, .017f, .025f);
            RenderSettings.fogStartDistance = preset == "High" ? 310f : 205f;
            RenderSettings.fogEndDistance = preset == "High" ? 650f : 470f;

            var camera = Camera.main;
            if (camera != null)
            {
                camera.clearFlags = _sky != null ? CameraClearFlags.Skybox : CameraClearFlags.SolidColor;
                camera.backgroundColor = RenderSettings.fogColor;
            }
            // The entire city is assembled at runtime and has no baked probes.
            // Supply its diffuse ambient probe directly so a dark sky capture or
            // deferred GPU readback cannot leave skinned characters black on iOS.
            var ambient = new SphericalHarmonicsL2();
            ambient.AddAmbientLight(new Color(.38f, .40f, .46f));
            RenderSettings.ambientProbe = ambient;
        }

        void OnDestroy()
        {
            if (RenderSettings.skybox == _sky) RenderSettings.skybox = _sourceSky;
            if (_sky != null) Destroy(_sky);
            if (Instance == this) Instance = null;
        }
    }

    public sealed class CityGraphics : MonoBehaviour
    {
        public static CityGraphics Instance { get; private set; }
        public string Preset { get; private set; }
        public VolumeProfile RuntimeProfile => _profile;

        RenderPipelineAsset _previous;
        VolumeProfile _profile;

        public void Configure()
        {
            Instance = this;
            _previous = QualitySettings.renderPipeline;
            var volume = gameObject.AddComponent<Volume>();
            volume.isGlobal = true;
            volume.priority = 80;
            var source = Resources.Load<VolumeProfile>("HarareEnvironment/CityExpansion/CityGrade");
            if (source != null)
            {
                volume.sharedProfile = source;
                _profile = volume.profile;
            }
            Apply(PlayerPrefs.GetString("Harare.Graphics.v1", "Balanced"));
        }

        public void Apply(string value)
        {
            Preset = value == "High" ? "High" : "Balanced";
            var pipeline = Resources.Load<UniversalRenderPipelineAsset>("HarareEnvironment/CityExpansion/" + Preset);
            if (pipeline != null) QualitySettings.renderPipeline = pipeline;

            var camera = Camera.main;
            if (camera != null)
            {
                camera.farClipPlane = Preset == "High" ? 700 : 520;
                camera.allowMSAA = false;
                camera.clearFlags = RenderSettings.skybox != null ? CameraClearFlags.Skybox : CameraClearFlags.SolidColor;
                camera.backgroundColor = RenderSettings.fogColor;
                var data = camera.GetUniversalAdditionalCameraData();
                data.renderPostProcessing = true;
                data.antialiasing = AntialiasingMode.FastApproximateAntialiasing;
            }

            if (_profile != null && _profile.TryGet(out ColorAdjustments colour))
            {
                colour.postExposure.Override(.55f);
                colour.contrast.Override(0f);
                colour.saturation.Override(-2f);
                colour.colorFilter.Override(Color.white);
            }
            if (_profile != null && _profile.TryGet(out Tonemapping tonemapping))
                tonemapping.mode.Override(TonemappingMode.Neutral);
            EveningLightingRig.Instance?.Apply(Preset);
            PlayerPrefs.SetString("Harare.Graphics.v1", Preset);
            PlayerPrefs.Save();
        }

        void OnDestroy()
        {
            if (Instance != this) return;
            QualitySettings.renderPipeline = _previous;
            Instance = null;
        }
    }
}
