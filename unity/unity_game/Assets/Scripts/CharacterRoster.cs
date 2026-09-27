using System;
using System.Collections.Generic;
using UnityEngine;

namespace HarareAfterHours
{
    /// <summary>
    /// A compact cast system built around the approved Character01 humanoid.
    /// Every in-world role shares the validated rig and switches through the
    /// prefab's LODGroup, while texture and material variants
    /// make each cast member readable at mobile-game distances.
    /// </summary>
    public sealed class CharacterRoster : MonoBehaviour
    {
        private const string CharacterPrefabPath = "HarareCharacters/Character01";

        private readonly List<CharacterCastEntry> _cast = new();
        private GameObject _characterPrefab;
        private bool _initialized;

        public static CharacterRoster Instance { get; private set; }
        public bool IsReady => _characterPrefab != null && _cast.Count > 0;
        public int CastCount => _cast.Count;
        public int NpcCount => Mathf.Max(0, _cast.Count - 1);
        public IReadOnlyList<CharacterCastEntry> Cast => _cast;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
        }

        public void Initialize()
        {
            if (_initialized) return;
            _initialized = true;
            if (Instance == null) Instance = this;

            _characterPrefab = Resources.Load<GameObject>(CharacterPrefabPath);
            BuildCast();
            if (_characterPrefab == null)
            {
                Debug.LogWarning($"[Characters] Missing Resources prefab at {CharacterPrefabPath}. The primitive avatar fallback will be used.");
            }
        }

        public bool AttachPlayer(AvatarCustomization avatar)
        {
            Initialize();
            if (avatar == null || !IsReady) return false;

            RuntimeCharacterVisual visual = CreateVisual(avatar.transform, _cast[0], true);
            if (visual == null) return false;

            avatar.UseRiggedVisual(visual);
            return true;
        }

        public GameObject SpawnNpc(Transform parent, int npcIndex, Vector3 position, float wanderRadius, float speed)
        {
            Initialize();
            if (!IsReady || npcIndex < 0 || npcIndex >= NpcCount) return null;

            CharacterCastEntry entry = _cast[npcIndex + 1];
            GameObject npc = new($"{entry.DisplayName} — {entry.Role}");
            npc.transform.SetParent(parent);
            npc.transform.position = position;

            RuntimeCharacterVisual visual = CreateVisual(npc.transform, entry, false);
            if (visual == null)
            {
                Destroy(npc);
                return null;
            }

            NpcWanderer wanderer = npc.AddComponent<NpcWanderer>();
            wanderer.Configure(position, wanderRadius, speed);
            return npc;
        }

        private RuntimeCharacterVisual CreateVisual(Transform parent, CharacterCastEntry entry, bool isPlayer)
        {
            if (_characterPrefab == null) return null;

            GameObject instance = Instantiate(_characterPrefab, parent);
            instance.name = entry.DisplayName;
            instance.transform.localPosition = Vector3.zero;
            instance.transform.localRotation = Quaternion.identity;
            instance.transform.localScale = Vector3.one;

            RuntimeCharacterVisual visual = instance.AddComponent<RuntimeCharacterVisual>();
            visual.Configure(entry, isPlayer);
            return visual;
        }

        private void BuildCast()
        {
            if (_cast.Count > 0) return;

            // The first role is the playable character. The remaining 23
            // entries correspond one-for-one with the supplied cast references.
            _cast.Add(Entry("tari", "Tari", "dark", "fade", "charcoal", "gold", "none", "street", 1.00f, 1.00f));
            _cast.Add(Entry("nia", "Nia", "rich", "braids", "black", "gold", "glasses", "dress", 0.96f, 0.92f));
            _cast.Add(Entry("kuda", "Kuda", "warm", "close cut", "olive", "rust", "none", "field", 1.03f, 1.03f));
            _cast.Add(Entry("asha", "Asha", "deep", "locs", "plum", "gold", "none", "dress", 0.98f, 0.94f));
            _cast.Add(Entry("officer_chipo", "Officer Chipo", "dark", "close cut", "navy", "silver", "cap", "uniform", 1.02f, 1.02f));
            _cast.Add(Entry("munya", "Munya", "rich", "fade", "black", "red", "hood", "hood", 1.05f, 1.04f));
            _cast.Add(Entry("mbuya_rudo", "Mbuya Rudo", "warm", "headwrap", "ochre", "teal", "headwrap", "apron", 0.92f, 1.00f));
            _cast.Add(Entry("tendai", "Tendai", "dark", "close cut", "black", "gold", "none", "suit", 1.04f, 1.03f));
            _cast.Add(Entry("elliot", "Elliot", "light", "curls", "navy", "cream", "none", "blazer", 1.01f, 0.98f));
            _cast.Add(Entry("sienna", "Sienna", "light", "long hair", "black", "silver", "glasses", "dress", 0.98f, 0.91f));
            _cast.Add(Entry("zara", "Zara", "medium", "ponytail", "graphite", "silver", "headphones", "suit", 0.96f, 0.90f));
            _cast.Add(Entry("max", "Max", "medium", "fade", "navy", "cream", "none", "blazer", 1.02f, 1.02f));
            _cast.Add(Entry("maris", "Maris", "light", "long hair", "black", "silver", "none", "dress", 0.98f, 0.91f));
            _cast.Add(Entry("claire", "Claire", "light", "ponytail", "white", "black", "glasses", "suit", 0.97f, 0.90f));
            _cast.Add(Entry("elena", "Elena", "light", "long hair", "black", "gold", "none", "suit", 0.99f, 0.92f));
            _cast.Add(Entry("ivy", "Ivy", "light", "ponytail", "cream", "gold", "none", "dress", 0.98f, 0.91f));
            _cast.Add(Entry("samira", "Samira", "warm", "long hair", "black", "silver", "headphones", "leather", 0.98f, 0.93f));
            _cast.Add(Entry("lena", "Lena", "medium", "ponytail", "black", "gold", "glasses", "suit", 0.97f, 0.91f));
            _cast.Add(Entry("maya", "Maya", "golden", "long hair", "brown", "gold", "none", "dress", 0.99f, 0.92f));
            _cast.Add(Entry("rafael", "Rafael", "warm", "curls", "charcoal", "cream", "none", "vest", 1.02f, 1.00f));
            _cast.Add(Entry("imani", "Imani", "rich", "locs", "black", "gold", "none", "dress", 0.99f, 0.94f));
            _cast.Add(Entry("thandi", "Thandi", "deep", "braids", "black", "silver", "headphones", "leather", 0.99f, 0.95f));
            _cast.Add(Entry("laila", "Laila", "warm", "headwrap", "cream", "gold", "headwrap", "suit", 0.97f, 0.92f));
            _cast.Add(Entry("zola", "Zola", "rich", "locs", "black", "gold", "none", "dress", 1.00f, 0.96f));
        }

        private static CharacterCastEntry Entry(
            string id,
            string displayName,
            string skinTone,
            string hairStyle,
            string outfit,
            string accent,
            string accessory,
            string wardrobe,
            float height,
            float build)
        {
            return new CharacterCastEntry
            {
                Id = id,
                DisplayName = displayName,
                Role = wardrobe,
                SkinTone = skinTone,
                HairStyle = hairStyle,
                Outfit = outfit,
                Accent = accent,
                Accessory = accessory,
                Wardrobe = wardrobe,
                Height = height,
                Build = build,
            };
        }
    }

    [Serializable]
    public sealed class CharacterCastEntry
    {
        public string Id;
        public string DisplayName;
        public string Role;
        public string SkinTone;
        public string HairStyle;
        public string Outfit;
        public string Accent;
        public string Accessory;
        public string Wardrobe;
        public float Height;
        public float Build;

        public CharacterCastEntry Copy()
        {
            return (CharacterCastEntry)MemberwiseClone();
        }
    }

    // One shared rig, mesh set and authored controller for every cast member.
    // Variants affect material inputs only; never bone proportions or rigid costumes.
    [DefaultExecutionOrder(100)]
    public sealed class RuntimeCharacterVisual : MonoBehaviour
    {
        private CharacterCastEntry _entry;
        private Animator _animator;
        private ThirdPersonController _player;
        private CharacterController _body;
        private MaterialPropertyBlock _block;
        private Vector3 _lastPosition;
        private bool _isPlayer;
        private bool _visible = true;
        private GameObject _portrait;
        private Material _portraitMaterial;
        private Texture2D _portraitTexture;
        private static readonly int Speed = Animator.StringToHash("Speed");
        private static readonly int Grounded = Animator.StringToHash("Grounded");
        private static readonly int Vertical = Animator.StringToHash("VerticalSpeed");
        public string DisplayName => _entry?.DisplayName ?? gameObject.name;

        public void Configure(CharacterCastEntry entry, bool isPlayer)
        {
            _entry = entry.Copy();
            _isPlayer = isPlayer;
            _player = GetComponentInParent<ThirdPersonController>();
            _body = GetComponentInParent<CharacterController>();
            _block = new MaterialPropertyBlock();
            _animator = GetComponentInChildren<Animator>(true);
            if (_animator != null)
            {
                _animator.enabled = true;
                _animator.applyRootMotion = false;
                _animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
                _animator.SetBool(Grounded, true);
            }
            transform.localScale = Vector3.one;
            _lastPosition = transform.position;
            // Runtime IK, combat and seated poses extend beyond imported animation
            // bounds. Pad once instead of recomputing skinned bounds every frame.
            foreach (var renderer in GetComponentsInChildren<SkinnedMeshRenderer>(true))
            {
                var bounds = renderer.localBounds;
                bounds.Expand(2f);
                renderer.localBounds = bounds;
                renderer.allowOcclusionWhenDynamic = false;
            }
            foreach (LODGroup group in GetComponentsInChildren<LODGroup>(true))
            {
                var lods = group.GetLODs();
                if (lods.Length == 0) continue;
                // Keep the cheapest mesh until the camera far plane, including wide
                // portrait views. Never show a threat marker without a mesh due to LOD.
                lods[lods.Length - 1].screenRelativeTransitionHeight = 0f;
                group.SetLODs(lods);
                group.RecalculateBounds();
                if (isPlayer) group.ForceLOD(0);
            }
            ApplyPresentation();
        }

        private void Update()
        {
            // Bootstrap builds the visual before adding ThirdPersonController.
            // Resolve the owner once it exists so stance and gait are not stuck in fallback mode.
            if(_isPlayer&&_player==null)_player=GetComponentInParent<ThirdPersonController>();
            if (_player != null && _player.IsInVehicle)
            {
                _lastPosition=transform.position;
                if (_animator != null) { _animator.SetFloat(Speed,0); _animator.SetBool(Grounded,true); _animator.SetFloat(Vertical,0); }
                return;
            }
            Vector3 delta = transform.position - _lastPosition;
            _lastPosition = transform.position;
            if (_animator == null || !_visible) return;
            float speed = _isPlayer && _player != null
                ? (_player.IsDriving ? 0 : _player.CurrentSpeed)
                : new Vector2(delta.x, delta.z).magnitude / Mathf.Max(Time.deltaTime, .0001f);
            // CharacterController is the single source of truth for collision and gravity.
            bool grounded = !_isPlayer || _body == null || _body.isGrounded;
            float vertical = _body != null && _body.enabled ? _body.velocity.y : 0;
            _animator.SetFloat(Speed, Mathf.Min(speed, 6), .10f, Time.deltaTime);
            _animator.SetBool(Grounded, grounded);
            _animator.SetFloat(Vertical, vertical);
            _animator.SetBool("Crouched",_isPlayer&&_player!=null&&_player.IsCrouching);
            // Reverse the grounded cycle when backpedalling; do not reverse takeoff/landing.
            _animator.SetFloat("GaitDirection",_isPlayer&&_player!=null&&_body!=null&&Vector3.Dot(_body.velocity,_player.transform.forward)<-.2f?-1:1);
            if(_isPlayer&&_player!=null&&_player.IsSliding)_animator.SetFloat(Speed,0);
        }

        public void SetSeated(bool seated, bool driver)
        {
            if(_animator==null)return;
            var ik=_animator.GetComponent<VehicleSeatIK>();
            if(ik==null)ik=_animator.gameObject.AddComponent<VehicleSeatIK>();
            var car=GetComponentInParent<VehicleController>();
            ik.Configure(seated && driver && car!=null?car.transform.Find("Steering grip left"):null,
                seated && driver && car!=null?car.transform.Find("Steering grip right"):null);
            _animator.cullingMode=AnimatorCullingMode.AlwaysAnimate;
            _animator.SetBool("Seated",seated);
            _animator.SetBool("DrivingSeat",driver);
            _animator.SetBool(Grounded,true);
            _animator.SetFloat(Speed,0);
            _animator.Play(seated?(driver?"Driving":"Passenger"):"Locomotion",0,0);
            _animator.Update(0);
            transform.localPosition=Vector3.zero;
            if(seated)
            {
                Transform hip=_animator.GetBoneTransform(HumanBodyBones.Hips);
                transform.localPosition=-transform.parent.InverseTransformPoint(hip.position);
            }
            else _animator.cullingMode=AnimatorCullingMode.AlwaysAnimate;
        }

        public void ApplyProfile(AvatarProfilePayload profile)
        {
            if (profile == null || _entry == null) return;
            if (!string.IsNullOrWhiteSpace(profile.name)) _entry.DisplayName = profile.name.Trim();
            if (!string.IsNullOrWhiteSpace(profile.skinTone)) _entry.SkinTone = profile.skinTone.Trim();
            if (!string.IsNullOrWhiteSpace(profile.hairStyle)) _entry.HairStyle = profile.hairStyle.Trim();
            if (!string.IsNullOrWhiteSpace(profile.outfit)) _entry.Outfit = profile.outfit.Trim();
            if (!string.IsNullOrWhiteSpace(profile.accessory)) _entry.Accessory = profile.accessory.Trim();
            gameObject.name = _entry.DisplayName;
            ApplyPresentation();
        }

        private void ApplyPresentation()
        {
            foreach (SkinnedMeshRenderer renderer in GetComponentsInChildren<SkinnedMeshRenderer>(true))
            {
                var materials = renderer.sharedMaterials;
                for (int i = 0; i < materials.Length; i++)
                {
                    var material = materials[i];
                    if (material == null) continue;
                    string slot = material.name.ToLowerInvariant();
                    Color tint = material.HasProperty("_BaseColor") ? material.GetColor("_BaseColor") : Color.white;
                    // Ratios are relative to the original diffuse, not a second dark skin layer.
                    if (slot.Contains("skin"))
                    {
                        Color tone = CharacterVisualPalette.Skin(_entry.SkinTone);
                        Color baseline = CharacterVisualPalette.Skin("dark");
                        tint = new Color(Mathf.Sqrt(tone.r / baseline.r),
                            Mathf.Sqrt(tone.g / baseline.g), Mathf.Sqrt(tone.b / baseline.b), 1);
                    }
                    else if (slot.Contains("casualsuit") || slot.Contains("outfit"))
                    {
                        Color color = CharacterVisualPalette.Outfit(_entry.Outfit);
                        float peak = Mathf.Max(color.r, color.g, color.b, .01f);
                        // Preserve the shirt, seams and jacket texture, while varying fabric hue.
                        tint = Color.Lerp(Color.white, color / peak, .72f);
                        tint *= Mathf.Lerp(1f, 1.35f, Mathf.Clamp01(peak));
                        tint.a = 1;
                    }
                    else if (slot.Contains("gold")) tint = CharacterVisualPalette.Accent(_entry.Accent);
                    _block.Clear();
                    _block.SetColor("_BaseColor", tint);
                    renderer.SetPropertyBlock(_block, i);
                }
            }
        }

        public void SetVisible(bool visible)
        {
            _visible = visible;
            // forceRenderingOff respects the LODGroup's renderer selection.
            foreach (Renderer renderer in GetComponentsInChildren<Renderer>(true))
                renderer.forceRenderingOff = !visible;
            if (_animator != null) _animator.enabled = visible;
            _lastPosition = transform.position;
        }

        public void ApplyPhotoBase64(string encodedPhoto)
        {
            if (string.IsNullOrWhiteSpace(encodedPhoto)) return;
            Texture2D texture = null;
            try
            {
                int comma = encodedPhoto.IndexOf(',');
                if (encodedPhoto.StartsWith("data:", StringComparison.OrdinalIgnoreCase) && comma >= 0)
                    encodedPhoto = encodedPhoto[(comma + 1)..];
                byte[] bytes = Convert.FromBase64String(encodedPhoto);
                if (bytes.Length > 8 * 1024 * 1024) throw new ArgumentException("Portrait exceeds 8 MB.");
                texture = new Texture2D(2, 2, TextureFormat.RGBA32, false);
                if (!texture.LoadImage(bytes, true)) throw new ArgumentException("Invalid portrait image.");
                if (_portrait == null)
                {
                    // An opt-in portrait badge follows the chest, never a floating face billboard.
                    _portrait = GameObject.CreatePrimitive(PrimitiveType.Quad);
                    _portrait.name = "Personal portrait badge";
                    Destroy(_portrait.GetComponent<Collider>());
                    Transform chest = _animator != null && _animator.isHuman
                        ? _animator.GetBoneTransform(HumanBodyBones.Chest) : null;
                    _portrait.transform.SetParent(transform, false);
                    _portrait.transform.localPosition = new Vector3(-.12f, 1.40f, .145f);
                    _portrait.transform.localRotation = Quaternion.Euler(0, 180, 0);
                    _portrait.transform.localScale = new Vector3(.10f, .125f, 1);
                    if (chest != null) _portrait.transform.SetParent(chest, true);
                    _portraitMaterial = RuntimeMaterialFactory.Create("Universal Render Pipeline/Unlit", "Unlit/Texture");
                    _portrait.GetComponent<Renderer>().sharedMaterial = _portraitMaterial;
                }
                if (_portraitTexture != null) Destroy(_portraitTexture);
                _portraitTexture = texture;
                _portraitMaterial.mainTexture = texture;
                if (_portraitMaterial.HasProperty("_BaseMap")) _portraitMaterial.SetTexture("_BaseMap", texture);
                _portrait.GetComponent<Renderer>().forceRenderingOff = !_visible;
            }
            catch (Exception exception)
            {
                if (texture != null && texture != _portraitTexture) Destroy(texture);
                Debug.LogWarning($"Unable to apply the local player portrait: {exception.Message}");
            }
        }

        private void OnDestroy()
        {
            if (_portraitTexture != null) Destroy(_portraitTexture);
            if (_portraitMaterial != null) Destroy(_portraitMaterial);
        }
    }

    internal static class CharacterVisualPalette
    {
        public static Color Skin(string tone)
        {
            return tone?.ToLowerInvariant() switch
            {
                "deep" => new Color(0.29f, 0.10f, 0.045f),
                "rich" => new Color(0.38f, 0.15f, 0.065f),
                "warm" => new Color(0.54f, 0.25f, 0.105f),
                "golden" => new Color(0.69f, 0.38f, 0.18f),
                "cool" => new Color(0.75f, 0.48f, 0.34f),
                "dark" => new Color(0.34f, 0.14f, 0.065f),
                "medium" => new Color(0.55f, 0.29f, 0.15f),
                "light" => new Color(0.83f, 0.58f, 0.42f),
                _ => new Color(0.34f, 0.14f, 0.065f),
            };
        }

        public static Color Hair(string style)
        {
            return style?.ToLowerInvariant() switch
            {
                "headwrap" => new Color(0.68f, 0.12f, 0.13f),
                "locs" => new Color(0.11f, 0.042f, 0.018f),
                "braids" => new Color(0.075f, 0.026f, 0.012f),
                "curls" => new Color(0.10f, 0.04f, 0.018f),
                "long hair" => new Color(0.09f, 0.03f, 0.016f),
                "ponytail" => new Color(0.16f, 0.075f, 0.035f),
                _ => new Color(0.035f, 0.024f, 0.015f),
            };
        }

        public static Color Outfit(string outfit)
        {
            return outfit?.ToLowerInvariant() switch
            {
                "delivery fit" => new Color(.95f, .30f, .08f),
                "street classic" => new Color(.18f, .42f, .78f),
                "night shift" => new Color(.045f, .06f, .075f),
                "festival ready" => new Color(.72f, .18f, .58f),
                "black" => new Color(0.035f, 0.04f, 0.055f),
                "charcoal" => new Color(0.085f, 0.095f, 0.11f),
                "olive" => new Color(0.24f, 0.28f, 0.18f),
                "navy" => new Color(0.065f, 0.14f, 0.30f),
                "ochre" => new Color(0.74f, 0.43f, 0.11f),
                "plum" => new Color(0.28f, 0.06f, 0.23f),
                "cream" => new Color(0.81f, 0.74f, 0.62f),
                "white" => new Color(0.86f, 0.85f, 0.81f),
                "brown" => new Color(0.31f, 0.12f, 0.075f),
                "graphite" => new Color(0.12f, 0.13f, 0.16f),
                _ => new Color(0.10f, 0.11f, 0.14f),
            };
        }

        public static Color Accent(string accent)
        {
            return accent?.ToLowerInvariant() switch
            {
                "gold" => new Color(0.82f, 0.56f, 0.14f),
                "silver" => new Color(0.54f, 0.62f, 0.70f),
                "red" => new Color(0.68f, 0.08f, 0.08f),
                "teal" => new Color(0.04f, 0.50f, 0.42f),
                "cream" => new Color(0.86f, 0.79f, 0.68f),
                "black" => new Color(0.05f, 0.055f, 0.065f),
                _ => new Color(0.72f, 0.38f, 0.12f),
            };
        }
    }
}
