using System;
using System.Collections.Generic;
using UnityEngine;

namespace HarareAfterHours
{
    [Serializable]
    public sealed class AvatarProfilePayload
    {
        public string name;
        public string skinTone;
        public string hairStyle;
        public string outfit;
        public string accessory;
        public string weapon;
        public string headwear;
        public string eyewear;
        public bool muteWeaponAudio;
        public bool reducedWeaponEffects;
        public bool hideBloodEffects;
    }

    /// <summary>
    /// A lightweight, in-scene avatar. It exposes the same choices as the
    /// Flutter character studio and can receive an opted-in portrait directly
    /// from the local app, without sending the image to a server.
    /// </summary>
    public sealed class AvatarCustomization : MonoBehaviour
    {
        private readonly List<Renderer> _renderers = new();
        private Renderer _skinRenderer;
        private Renderer _hairRenderer;
        private Renderer _outfitRenderer;
        private Renderer _photoRenderer;
        private Renderer _capRenderer;
        private Renderer _glassesRenderer;
        private Renderer _headphonesRenderer;
        private string _accessory = "No accessory";
        private bool _isVisible = true;
        private bool _hasPhoto;
        private bool _built;
        private RuntimeCharacterVisual _riggedVisual;

        public string DisplayName { get; private set; } = "Tari";

        public void BuildDefaultVisual()
        {
            if (_built) return;

            // CharacterRoster is initialized before the player is created in
            // the gameplay bootstrap. Keep the primitive construction below as
            // a safe fallback for scenes that do not include the character pack.
            if (CharacterRoster.Instance != null && CharacterRoster.Instance.AttachPlayer(this))
            {
                _built = true;
                return;
            }

            _built = true;

            Material skin = CreateMaterial(new Color(0.32f, 0.145f, 0.075f), 0.1f, 0.35f);
            Material hair = CreateMaterial(new Color(0.025f, 0.018f, 0.014f), 0.05f, 0.42f);
            Material outfit = CreateMaterial(new Color(0.92f, 0.2f, 0.09f), 0.12f, 0.42f);
            Material trousers = CreateMaterial(new Color(0.04f, 0.09f, 0.14f), 0.08f, 0.42f);
            Material photo = CreateMaterial(new Color(0.21f, 0.4f, 0.54f), 0f, 0.7f);

            _outfitRenderer = CreatePart(PrimitiveType.Capsule, "Delivery jacket", new Vector3(0f, 0.9f, 0f), new Vector3(0.72f, 0.72f, 0.72f), outfit).GetComponent<Renderer>();
            _skinRenderer = CreatePart(PrimitiveType.Sphere, "Head", new Vector3(0f, 1.7f, 0f), new Vector3(0.62f, 0.68f, 0.62f), skin).GetComponent<Renderer>();
            _hairRenderer = CreatePart(PrimitiveType.Sphere, "Hair", new Vector3(0f, 1.91f, -0.02f), new Vector3(0.64f, 0.35f, 0.64f), hair).GetComponent<Renderer>();
            _photoRenderer = CreatePart(PrimitiveType.Cube, "Optional portrait panel", new Vector3(0f, 1.7f, 0.32f), new Vector3(0.4f, 0.42f, 0.025f), photo).GetComponent<Renderer>();

            // Simple physical accessory silhouettes make the Flutter studio's
            // choices visible in the playable prototype. Production meshes
            // can replace these primitives without changing the profile API.
            _capRenderer = CreatePart(PrimitiveType.Sphere, "Cap", new Vector3(0f, 2.04f, 0f), new Vector3(0.68f, 0.16f, 0.68f), hair).GetComponent<Renderer>();
            _glassesRenderer = CreatePart(PrimitiveType.Cube, "Glasses", new Vector3(0f, 1.72f, 0.33f), new Vector3(0.55f, 0.11f, 0.045f), CreateMaterial(new Color(0.05f, 0.55f, 0.68f), 0.7f, 0.92f)).GetComponent<Renderer>();
            _headphonesRenderer = CreatePart(PrimitiveType.Capsule, "Headphones", new Vector3(0f, 1.92f, 0f), new Vector3(0.72f, 0.23f, 0.72f), CreateMaterial(new Color(0.15f, 0.04f, 0.22f), 0.45f, 0.7f)).GetComponent<Renderer>();

            CreatePart(PrimitiveType.Cube, "Left leg", new Vector3(-0.22f, 0.28f, 0f), new Vector3(0.23f, 0.66f, 0.27f), trousers);
            CreatePart(PrimitiveType.Cube, "Right leg", new Vector3(0.22f, 0.28f, 0f), new Vector3(0.23f, 0.66f, 0.27f), trousers);
            CreatePart(PrimitiveType.Capsule, "Left arm", new Vector3(-0.52f, 1.03f, 0f), new Vector3(0.17f, 0.45f, 0.17f), outfit);
            CreatePart(PrimitiveType.Capsule, "Right arm", new Vector3(0.52f, 1.03f, 0f), new Vector3(0.17f, 0.45f, 0.17f), outfit);
            RefreshAccessoryVisibility();
            RefreshPhotoVisibility();
        }

        public void ApplyProfileJson(string json)
        {
            if (string.IsNullOrWhiteSpace(json)) return;
            try
            {
                AvatarProfilePayload profile = JsonUtility.FromJson<AvatarProfilePayload>(json);
                ApplyProfile(profile);
            }
            catch (Exception exception)
            {
                Debug.LogWarning($"Unable to read avatar profile: {exception.Message}");
            }
        }

        public void ApplyProfile(AvatarProfilePayload profile)
        {
            if (profile == null) return;
            if (!string.IsNullOrWhiteSpace(profile.name)) DisplayName = profile.name.Trim();
            if (_riggedVisual != null)
            {
                _riggedVisual.ApplyProfile(profile);
                return;
            }

            SetColor(_skinRenderer, SkinColor(profile.skinTone));
            SetColor(_hairRenderer, HairColor(profile.hairStyle));
            SetColor(_outfitRenderer, OutfitColor(profile.outfit));
            SetAccessory(profile.accessory);
        }

        public void ApplyPhotoBase64(string encodedPhoto)
        {
            if (_riggedVisual != null)
            {
                _riggedVisual.ApplyPhotoBase64(encodedPhoto);
                return;
            }
            if (string.IsNullOrWhiteSpace(encodedPhoto) || _photoRenderer == null) return;

            try
            {
                int commaIndex = encodedPhoto.IndexOf(',');
                if (encodedPhoto.StartsWith("data:", StringComparison.OrdinalIgnoreCase) && commaIndex >= 0)
                {
                    encodedPhoto = encodedPhoto[(commaIndex + 1)..];
                }

                byte[] bytes = Convert.FromBase64String(encodedPhoto);
                Texture2D texture = new(2, 2, TextureFormat.RGBA32, false);
                if (!texture.LoadImage(bytes, false))
                {
                    Destroy(texture);
                    return;
                }

                Material material = new(_photoRenderer.sharedMaterial);
                material.mainTexture = texture;
                if (material.HasProperty("_BaseMap")) material.SetTexture("_BaseMap", texture);
                _photoRenderer.material = material;
                _hasPhoto = true;
                RefreshPhotoVisibility();
            }
            catch (Exception exception)
            {
                Debug.LogWarning($"Unable to apply the local player portrait: {exception.Message}");
            }
        }

        public void SetVisible(bool visible)
        {
            _isVisible = visible;
            if (_riggedVisual != null)
            {
                _riggedVisual.SetVisible(visible);
                return;
            }

            foreach (Renderer renderer in _renderers)
            {
                if (renderer != null) renderer.enabled = visible;
            }
            RefreshAccessoryVisibility();
            RefreshPhotoVisibility();
        }

        /// <summary>
        /// Called by CharacterRoster after it instantiates the imported humanoid
        /// beneath this player. The existing Flutter profile API remains the
        /// source of truth, while its presentation moves from primitives to the
        /// rigged, LOD-enabled character.
        /// </summary>
        public void UseRiggedVisual(RuntimeCharacterVisual visual)
        {
            _riggedVisual = visual;
            _built = true;
            if (_riggedVisual != null) _riggedVisual.SetVisible(_isVisible);
        }

        private GameObject CreatePart(PrimitiveType type, string objectName, Vector3 localPosition, Vector3 localScale, Material material)
        {
            GameObject part = GameObject.CreatePrimitive(type);
            part.name = objectName;
            part.transform.SetParent(transform);
            part.transform.localPosition = localPosition;
            part.transform.localRotation = Quaternion.identity;
            part.transform.localScale = localScale;
            if (part.TryGetComponent(out Collider collider)) collider.enabled = false;
            Renderer renderer = part.GetComponent<Renderer>();
            renderer.sharedMaterial = material;
            _renderers.Add(renderer);
            return part;
        }

        private static Material CreateMaterial(Color color, float metallic, float smoothness)
        {
            Material material = RuntimeMaterialFactory.Create("Universal Render Pipeline/Lit", "Standard");
            if (material.HasProperty("_BaseColor")) material.SetColor("_BaseColor", color);
            if (material.HasProperty("_Color")) material.color = color;
            if (material.HasProperty("_Metallic")) material.SetFloat("_Metallic", metallic);
            if (material.HasProperty("_Smoothness")) material.SetFloat("_Smoothness", smoothness);
            return material;
        }

        private static void SetColor(Renderer renderer, Color color)
        {
            if (renderer == null) return;
            Material material = renderer.material;
            if (material.HasProperty("_BaseColor")) material.SetColor("_BaseColor", color);
            if (material.HasProperty("_Color")) material.color = color;
        }

        private static Color SkinColor(string skinTone)
        {
            return skinTone?.ToLowerInvariant() switch
            {
                "deep" => new Color(0.24f, 0.095f, 0.04f),
                "rich" => new Color(0.31f, 0.125f, 0.052f),
                "warm" => new Color(0.47f, 0.21f, 0.09f),
                "golden" => new Color(0.64f, 0.34f, 0.16f),
                "cool" => new Color(0.72f, 0.44f, 0.3f),
                "dark" => new Color(0.33f, 0.14f, 0.065f),
                "medium" => new Color(0.52f, 0.27f, 0.13f),
                "light" => new Color(0.78f, 0.51f, 0.34f),
                _ => new Color(0.32f, 0.145f, 0.075f),
            };
        }

        private static Color HairColor(string hairStyle)
        {
            return hairStyle?.ToLowerInvariant() switch
            {
                "braids" => new Color(0.07f, 0.025f, 0.012f),
                "locs" => new Color(0.11f, 0.045f, 0.018f),
                "close cut" => new Color(0.04f, 0.024f, 0.014f),
                "curls" => new Color(0.075f, 0.032f, 0.018f),
                "headwrap" => new Color(0.66f, 0.13f, 0.12f),
                "fade" => new Color(0.035f, 0.028f, 0.024f),
                _ => new Color(0.025f, 0.018f, 0.014f),
            };
        }

        private static Color OutfitColor(string outfit)
        {
            return outfit?.ToLowerInvariant() switch
            {
                "delivery fit" => new Color(0.92f, 0.2f, 0.09f),
                "street classic" => new Color(0.11f, 0.2f, 0.52f),
                "night shift" => new Color(0.14f, 0.72f, 0.52f),
                "festival ready" => new Color(0.74f, 0.2f, 0.76f),
                "campus casual" => new Color(0.13f, 0.46f, 0.83f),
                "night runner" => new Color(0.28f, 0.74f, 0.36f),
                "creative fit" => new Color(0.68f, 0.2f, 0.78f),
                _ => new Color(0.92f, 0.2f, 0.09f),
            };
        }

        private void SetAccessory(string accessory)
        {
            _accessory = string.IsNullOrWhiteSpace(accessory) ? "No accessory" : accessory.Trim();
            RefreshAccessoryVisibility();
        }

        private void RefreshAccessoryVisibility()
        {
            string accessory = _accessory.ToLowerInvariant();
            if (_capRenderer != null) _capRenderer.enabled = _isVisible && accessory == "cap";
            if (_glassesRenderer != null) _glassesRenderer.enabled = _isVisible && accessory == "glasses";
            if (_headphonesRenderer != null) _headphonesRenderer.enabled = _isVisible && accessory == "headphones";
        }

        private void RefreshPhotoVisibility()
        {
            if (_photoRenderer != null) _photoRenderer.enabled = _isVisible && _hasPhoto;
        }
    }
}
