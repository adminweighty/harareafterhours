using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace HarareAfterHours
{
    /// <summary>
    /// Turns the supplied photographic art library into physical, illuminated
    /// parts of the playable 3D district. The source images remain images
    /// rather than being presented as animated 3D meshes: they become city
    /// facades, horizon backdrops, showroom displays, and cast projections.
    /// </summary>
    public sealed class HarareArtLibrary : MonoBehaviour
    {
        private const string EnvironmentPath = "HarareArt/Environment";
        private const string VehiclePath = "HarareArt/Vehicles";
        private const string ReferencePath = "HarareArt/References";
        private const string MusicPath = "HarareArt/Music";

        private readonly List<Material> _materials = new();
        private AudioSource _musicSource;
        private AudioClip[] _musicTracks = Array.Empty<AudioClip>();
        private int _trackIndex = -1;
        private bool _built;

        public int RenderedImageCount { get; private set; }
        public string CurrentTrackName => _trackIndex >= 0 && _trackIndex < _musicTracks.Length
            ? _musicTracks[_trackIndex].name
            : string.Empty;

        public void Build(bool showReferencePanels = false)
        {
            if (_built) return;
            _built = true;

            // These source boards are an art-review aid, not real buildings.
            // Keep them opt-in without disabling the existing music service.
            if (!showReferencePanels)
            {
                ConfigureMusic();
                Debug.Log($"[HarareArt] Reference boards retained for review; playable city uses 3D surfaces. Tracks: {_musicTracks.Length}.");
                return;
            }

            Texture2D[] cityLocations = LoadTextures(EnvironmentPath);
            Texture2D[] vehicleArtwork = LoadTextures(VehiclePath);
            Texture2D[] suppliedReferences = LoadTextures(ReferencePath);

            Texture2D[] panoramicReferences = suppliedReferences
                .Where(IsLandscape)
                .ToArray();
            Texture2D[] characterReferences = suppliedReferences
                .Where(texture => !IsLandscape(texture))
                .ToArray();

            Texture2D[] locationGallery = cityLocations.Concat(panoramicReferences).ToArray();
            // These are named and positioned as navigable city districts, not
            // as a menu/gallery screen. Every supplied image gets a material
            // and a physical panel somewhere in the same gameplay scene.
            CreateDistrict("Harare Central — location facades", locationGallery, new Vector3(-47f, 0f, 67f), 6, 5.4f, 0.48f);
            CreateDistrict("Borrowdale — vehicle showroom", vehicleArtwork, new Vector3(20f, 0f, 67f), 5, 3.8f, 0.48f);
            CreateDistrict("Private Lounge — cast projections", characterReferences, new Vector3(64f, 0f, 50f), 4, 5.2f, 0.42f);
            CreateHorizonBackdrops(locationGallery);

            RenderedImageCount = locationGallery.Length + vehicleArtwork.Length + characterReferences.Length;
            ConfigureMusic();
            Debug.Log($"[HarareArt] Rendered {RenderedImageCount} supplied images across 3D city districts. Tracks: {_musicTracks.Length}.");
        }

        private void Update()
        {
            if (_musicSource != null && _musicTracks.Length > 0 && !_musicSource.isPlaying)
            {
                PlayNextTrack();
            }
        }

        private void OnDestroy()
        {
            foreach (Material material in _materials)
            {
                if (material != null) Destroy(material);
            }
        }

        private static Texture2D[] LoadTextures(string path)
        {
            return Resources.LoadAll<Texture2D>(path)
                .Where(texture => texture != null)
                .OrderBy(texture => texture.name, StringComparer.Ordinal)
                .ToArray();
        }

        private static bool IsLandscape(Texture2D texture)
        {
            return texture != null && texture.width > texture.height * 1.25f;
        }

        private void CreateDistrict(string districtName, Texture2D[] images, Vector3 origin, int columns, float panelHeight, float gap)
        {
            if (images.Length == 0) return;

            GameObject district = new(districtName);
            district.transform.SetParent(transform);
            district.transform.position = origin;

            float maxWidth = images.Select(texture => Mathf.Clamp(panelHeight * texture.width / Mathf.Max(1f, texture.height), 1.35f, 6.2f)).Max();
            float xSpacing = maxWidth + gap;
            float zSpacing = panelHeight + 0.72f;

            for (int index = 0; index < images.Length; index++)
            {
                int row = index / columns;
                int column = index % columns;
                float x = (column - (columns - 1) * 0.5f) * xSpacing;
                float z = -row * zSpacing;
                CreateImagePanel(district.transform, images[index], new Vector3(x, panelHeight * 0.5f + 0.3f, z), panelHeight);
            }

            CreateDistrictFloor(district.transform, columns * xSpacing + 1.5f, Mathf.CeilToInt(images.Length / (float)columns) * zSpacing + 1.2f);
        }

        private void CreateHorizonBackdrops(Texture2D[] locationImages)
        {
            if (locationImages.Length == 0) return;

            GameObject horizon = new("Harare Central — photographic horizon");
            horizon.transform.SetParent(transform);
            horizon.transform.position = new Vector3(0f, 0f, 75f);

            int backdropCount = Mathf.Min(4, locationImages.Length);
            for (int index = 0; index < backdropCount; index++)
            {
                float x = (index - (backdropCount - 1) * 0.5f) * 19f;
                CreateImagePanel(horizon.transform, locationImages[index], new Vector3(x, 8.2f, 0f), 15.5f);
            }
        }

        private void CreateImagePanel(Transform parent, Texture2D image, Vector3 localPosition, float panelHeight)
        {
            float aspect = image.width / Mathf.Max(1f, image.height);
            float panelWidth = Mathf.Clamp(panelHeight * aspect, 1.35f, 6.2f);

            GameObject panel = new(image.name);
            panel.transform.SetParent(parent);
            panel.transform.localPosition = localPosition;
            panel.transform.localRotation = Quaternion.Euler(0f, 180f, 0f);

            GameObject backing = CreatePrimitive(PrimitiveType.Cube, "Backing", panel.transform, new Vector3(0f, 0f, 0.08f), new Vector3(panelWidth + 0.24f, panelHeight + 0.24f, 0.12f), GetWallMaterial());
            DisableCollider(backing);

            GameObject imageQuad = CreatePrimitive(PrimitiveType.Quad, "Artwork", panel.transform, Vector3.zero, new Vector3(panelWidth, panelHeight, 1f), CreateImageMaterial(image));
            DisableCollider(imageQuad);
            CreateFrame(panel.transform, panelWidth, panelHeight);
        }

        private void CreateDistrictFloor(Transform parent, float width, float depth)
        {
            GameObject floor = CreatePrimitive(PrimitiveType.Cube, "District deck", parent, new Vector3(0f, 0.12f, -depth * 0.5f + 0.18f), new Vector3(width, 0.24f, depth), GetFloorMaterial());
            DisableCollider(floor);
        }

        private void CreateFrame(Transform parent, float width, float height)
        {
            Material frame = GetFrameMaterial();
            float thickness = 0.09f;
            CreateFramePiece(parent, "Frame top", new Vector3(0f, height * 0.5f + thickness * 0.5f, -0.03f), new Vector3(width + thickness * 2f, thickness, thickness), frame);
            CreateFramePiece(parent, "Frame bottom", new Vector3(0f, -height * 0.5f - thickness * 0.5f, -0.03f), new Vector3(width + thickness * 2f, thickness, thickness), frame);
            CreateFramePiece(parent, "Frame left", new Vector3(-width * 0.5f - thickness * 0.5f, 0f, -0.03f), new Vector3(thickness, height, thickness), frame);
            CreateFramePiece(parent, "Frame right", new Vector3(width * 0.5f + thickness * 0.5f, 0f, -0.03f), new Vector3(thickness, height, thickness), frame);
        }

        private static void CreateFramePiece(Transform parent, string name, Vector3 position, Vector3 scale, Material material)
        {
            GameObject piece = CreatePrimitive(PrimitiveType.Cube, name, parent, position, scale, material);
            DisableCollider(piece);
        }

        private void ConfigureMusic()
        {
            _musicTracks = Resources.LoadAll<AudioClip>(MusicPath)
                .Where(track => track != null)
                .OrderBy(track => track.name, StringComparer.Ordinal)
                .ToArray();

            if (_musicTracks.Length == 0) return;

            _musicSource = gameObject.AddComponent<AudioSource>();
            _musicSource.playOnAwake = false;
            _musicSource.loop = false;
            _musicSource.spatialBlend = 0f;
            _musicSource.volume = 0.24f;
            PlayNextTrack();
        }

        private void PlayNextTrack()
        {
            if (_musicSource == null || _musicTracks.Length == 0) return;
            _trackIndex = (_trackIndex + 1) % _musicTracks.Length;
            _musicSource.clip = _musicTracks[_trackIndex];
            _musicSource.Play();
        }

        private Material CreateImageMaterial(Texture2D image)
        {
            Material material = RuntimeMaterialFactory.Create("Universal Render Pipeline/Unlit", "Unlit/Texture");
            material.mainTexture = image;
            if (material.HasProperty("_BaseMap")) material.SetTexture("_BaseMap", image);
            if (material.HasProperty("_BaseColor")) material.SetColor("_BaseColor", Color.white);
            _materials.Add(material);
            return material;
        }

        private Material GetWallMaterial()
        {
            Material material = _materials.FirstOrDefault(candidate => candidate != null && candidate.name == "District wall material");
            if (material != null) return material;
            material = CreateLitMaterial("District wall material", new Color(0.015f, 0.018f, 0.03f), 0.52f, 0.65f, false);
            return material;
        }

        private Material GetFloorMaterial()
        {
            Material material = _materials.FirstOrDefault(candidate => candidate != null && candidate.name == "District floor material");
            if (material != null) return material;
            material = CreateLitMaterial("District floor material", new Color(0.04f, 0.055f, 0.075f), 0.42f, 0.58f, false);
            return material;
        }

        private Material GetFrameMaterial()
        {
            Material material = _materials.FirstOrDefault(candidate => candidate != null && candidate.name == "District frame material");
            if (material != null) return material;
            material = CreateLitMaterial("District frame material", new Color(1f, 0.18f, 0.46f), 0.24f, 0.72f, true);
            return material;
        }

        private Material CreateLitMaterial(string materialName, Color color, float metallic, float smoothness, bool emissive)
        {
            Material material = RuntimeMaterialFactory.Create("Universal Render Pipeline/Lit", "Standard");
            material.name = materialName;
            if (material.HasProperty("_BaseColor")) material.SetColor("_BaseColor", color);
            if (material.HasProperty("_Color")) material.color = color;
            if (material.HasProperty("_Metallic")) material.SetFloat("_Metallic", metallic);
            if (material.HasProperty("_Smoothness")) material.SetFloat("_Smoothness", smoothness);
            if (emissive)
            {
                material.EnableKeyword("_EMISSION");
                material.SetColor("_EmissionColor", color * 1.5f);
            }

            _materials.Add(material);
            return material;
        }

        private static GameObject CreatePrimitive(PrimitiveType type, string name, Transform parent, Vector3 localPosition, Vector3 localScale, Material material)
        {
            GameObject gameObject = GameObject.CreatePrimitive(type);
            gameObject.name = name;
            gameObject.transform.SetParent(parent);
            gameObject.transform.localPosition = localPosition;
            gameObject.transform.localRotation = Quaternion.identity;
            gameObject.transform.localScale = localScale;
            if (gameObject.TryGetComponent(out Renderer renderer)) renderer.sharedMaterial = material;
            return gameObject;
        }

        private static void DisableCollider(GameObject gameObject)
        {
            if (gameObject.TryGetComponent(out Collider collider)) collider.enabled = false;
        }
    }
}
