using System.Collections.Generic;
using UnityEngine;

namespace HarareAfterHours
{
    /// <summary>Small bone-bound cosmetics; no colliders or gameplay armour bonus.</summary>
    [DefaultExecutionOrder(200)]
    public sealed class PlayerEquipmentVisual : MonoBehaviour
    {
        private ThirdPersonController _player;
        private Transform _headSocket;
        private Transform _handSocket;
        private GameObject _headwear;
        private GameObject _eyewear;
        private GameObject _weapon;
        private Material _dark, _shell, _lens, _metal, _blaster, _wood;
        private Mesh _dome;
        private float _aimUntil;
        private string _weaponName;
        private bool _reduced;
        private WeaponFeedback _feedback;
        private Animator _animator;
        public Transform Muzzle { get; private set; }
        public bool IsRanged => _weaponName is "Pulse rifle" or "Pulse pistol";
        public bool IsHoldingWeapon => _weapon != null && !_player.IsInVehicle && !_player.InputLocked;
        public int HandPoseUpdates { get; private set; }

        public void Initialize(ThirdPersonController player)
        {
            if (_headSocket != null) return;
            _player = player;
            Animator animator = GetComponentInChildren<Animator>(true);
            _animator = animator;
            if (animator != null)
            {
                var ik = animator.GetComponent<VehicleSeatIK>();
                if (ik == null) ik = animator.gameObject.AddComponent<VehicleSeatIK>();
                ik.Configure(null, null);
            }
            _feedback = gameObject.AddComponent<WeaponFeedback>();
            Transform head = animator != null && animator.isHuman ? animator.GetBoneTransform(HumanBodyBones.Head) : null;
            Transform hand = animator != null && animator.isHuman ? animator.GetBoneTransform(HumanBodyBones.RightHand) : null;
            _headSocket = Socket("Head equipment socket", head, new Vector3(0, 1.65f, 0));
            // Character01's head joint sits above the eye line. Fit these
            // metre-sized accessories to its actual face, not a generic rig.
            if (head != null)
            {
                _headSocket.position -= transform.up * .085f;
                _headSocket.localScale *= .82f;
            }
            _handSocket = Socket("Weapon grip socket", hand, new Vector3(.35f, 1, 0));
            _dark = Material("Blued weapon steel", new Color(.075f, .09f, .115f), .48f);
            _shell = Material("Helmet ivory", new Color(.88f, .85f, .69f), .3f);
            _lens = Material("Sunglasses blue black", new Color(.025f, .12f, .18f), .85f);
            _metal = Material("Equipment trim", new Color(.82f, .54f, .19f), .65f);
            _wood = Material("Walnut firearm furniture", new Color(.34f,.13f,.05f), .05f);
            _blaster = Material("Kenney blaster palette", Color.white, .3f);
            var texture = Resources.Load<Texture2D>("HarareEquipment/colormap");
            _blaster.SetTexture("_BaseMap", texture);
            _blaster.mainTexture = texture;
            _blaster.EnableKeyword("_EMISSION");
            _blaster.SetTexture("_EmissionMap", texture);
            _blaster.SetColor("_EmissionColor", Color.white * .22f);
            _dome = Dome();
        }

        private Transform Socket(string label, Transform bone, Vector3 fallback)
        {
            var socket = new GameObject(label).transform;
            socket.position = bone != null ? bone.position : transform.TransformPoint(fallback);
            socket.rotation = transform.rotation;
            socket.SetParent(bone != null ? bone : transform, true);
            return socket;
        }

        public void Apply(AvatarProfilePayload profile)
        {
            _weaponName = profile.weapon;
            _reduced = profile.reducedWeaponEffects;
            _aimUntil = 0;
            Muzzle = null;
            _feedback.Configure(profile.muteWeaponAudio, _reduced, profile.weapon == "Pulse rifle");
            Remove(ref _headwear); Remove(ref _eyewear); Remove(ref _weapon);
            if (profile.headwear != "None")
            {
                _headwear = Root(profile.headwear, _headSocket);
                var root = _headwear.transform;
                if (profile.headwear is "Helmet" or "Cap")
                {
                    var dome = new GameObject("Open-face shell");
                    dome.transform.SetParent(root, false);
                    dome.transform.localPosition = new Vector3(0, .175f, .02f);
                    dome.transform.localScale = new Vector3(.13f, .17f, .16f);
                    dome.AddComponent<MeshFilter>().sharedMesh = _dome;
                    dome.AddComponent<MeshRenderer>().sharedMaterial = profile.headwear == "Helmet" ? _shell : _dark;
                    if (profile.headwear == "Cap")
                        Part(root, "Cap peak", new Vector3(0,.15f,.12f), new Vector3(.2f,.018f,.16f), _dark);
                    else
                    {
                        Part(root, "Left helmet guard", new Vector3(-.105f,.12f,0), new Vector3(.045f,.10f,.12f), _shell, PrimitiveType.Sphere);
                        Part(root, "Right helmet guard", new Vector3(.105f,.12f,0), new Vector3(.045f,.10f,.12f), _shell, PrimitiveType.Sphere);
                        Part(root, "Helmet stripe", new Vector3(0,.344f,.02f), new Vector3(.025f,.006f,.1f), _metal);
                    }
                }
                else
                {
                    Part(root, "Headphone band", new Vector3(0,.265f,0), new Vector3(.25f,.024f,.045f), _dark);
                    foreach (float side in new[] {-1f, 1f})
                        Part(root, "Headphone cup", new Vector3(side*.119f,.11f,0), new Vector3(.044f,.12f,.085f), _dark);
                }
            }
            if (profile.eyewear == "Sunglasses")
            {
                _eyewear = Root("Sunglasses", _headSocket);
                foreach (float side in new[] {-1f, 1f})
                {
                    Part(_eyewear.transform, "Lens frame", new Vector3(side*.049f,.132f,.10f), new Vector3(.088f,.047f,.018f), _metal);
                    Part(_eyewear.transform, "Dark lens", new Vector3(side*.049f,.132f,.112f), new Vector3(.075f,.034f,.009f), _lens);
                    Part(_eyewear.transform, "Temple arm", new Vector3(side*.095f,.134f,.018f), new Vector3(.008f,.012f,.18f), _dark);
                }
                Part(_eyewear.transform, "Nose bridge", new Vector3(0,.139f,.11f), new Vector3(.022f,.008f,.014f), _metal);
            }
            if (profile.weapon != "Unarmed")
            {
                _weapon = Root(profile.weapon, _handSocket);
                _weapon.transform.localScale=Vector3.one*1.08f;
                if (profile.weapon == "Baton")
                {
                    Part(_weapon.transform, "Baton shaft", new Vector3(0,.04f,.20f), new Vector3(.038f,.038f,.52f), _dark);
                    Part(_weapon.transform, "Baton grip", new Vector3(0,.04f,-.055f), new Vector3(.052f,.052f,.15f), _metal);
                }
                else
                {
                    bool rifle = profile.weapon == "Pulse rifle";
                    BuildFirearm(_weapon.transform, rifle);
                }
            }
            LateUpdate();
        }

        public void ShowAttack() { _aimUntil = Time.time + .25f; }

        // Original game-ready silhouettes, not extracted franchise assets.
        private void BuildFirearm(Transform root, bool rifle)
        {
            Part(root,"Dark steel receiver",new Vector3(0,.10f,.08f),new Vector3(.055f,.075f,rifle?.30f:.19f),_dark);
            Part(root,"Grip",new Vector3(0,.025f,0),new Vector3(.047f,.13f,.065f),_dark);
            if(rifle)
            {
                Part(root,"Walnut stock",new Vector3(0,.065f,-.23f),new Vector3(.055f,.10f,.25f),_wood);
                Part(root,"Wood fore-end",new Vector3(0,.09f,.28f),new Vector3(.065f,.065f,.16f),_wood);
                Part(root,"Barrel",new Vector3(0,.12f,.44f),new Vector3(.025f,.025f,.22f),_dark);
                Part(root,"Gas tube",new Vector3(0,.148f,.35f),new Vector3(.02f,.02f,.22f),_dark);
                for(int i=0;i<5;i++)
                    Part(root,"Curved magazine segment",new Vector3(0,.025f-i*.033f,.13f+i*i*.003f),new Vector3(.038f,.044f,.085f),_dark);
                Part(root,"Front sight",new Vector3(0,.16f,.50f),new Vector3(.028f,.055f,.025f),_dark);
            }
            else
            {
                Part(root,"Compact slide",new Vector3(0,.145f,.08f),new Vector3(.052f,.04f,.22f),_dark);
                Part(root,"Rear sight",new Vector3(0,.17f,-.005f),new Vector3(.035f,.014f,.016f),_metal);
                Part(root,"Front sight",new Vector3(0,.17f,.175f),new Vector3(.012f,.014f,.012f),_metal);
            }
            Part(root,"Trigger guard base",new Vector3(0,.015f,.07f),new Vector3(.018f,.012f,.07f),_dark);
            Muzzle=Root("Muzzle",root).transform;
            Muzzle.localPosition=new Vector3(0,rifle?.12f:.145f,rifle?.55f:.195f);
        }

        public void ApplyHandIK(Animator animator)
        {
            if (!IsHoldingWeapon || !IsRanged || animator.GetBool("Seated")) return;
            HandPoseUpdates++;
            float kick = _reduced ? 0 : Mathf.Clamp01((_aimUntil - Time.time) / .25f);
            float bob = _reduced ? 0 : Mathf.Sin(Time.time * 5) * Mathf.Min(_player.CurrentSpeed, 1) * .012f;
            Vector3 right = transform.TransformPoint(new Vector3(.28f, 1.30f + bob, .36f - kick*.045f));
            animator.SetIKPositionWeight(AvatarIKGoal.RightHand, 1);
            animator.SetIKPosition(AvatarIKGoal.RightHand, right);
            animator.SetIKHintPositionWeight(AvatarIKHint.RightElbow, .8f);
            animator.SetIKHintPosition(AvatarIKHint.RightElbow, transform.TransformPoint(new Vector3(.6f,1.08f,.04f)));
            if (_weaponName == "Pulse rifle")
            {
                animator.SetIKPositionWeight(AvatarIKGoal.LeftHand, 1);
                animator.SetIKPosition(AvatarIKGoal.LeftHand, right + transform.TransformVector(new Vector3(-.035f,.025f,.28f)));
                animator.SetIKHintPositionWeight(AvatarIKHint.LeftElbow, .8f);
                animator.SetIKHintPosition(AvatarIKHint.LeftElbow, transform.TransformPoint(new Vector3(-.4f,1.05f,.12f)));
            }
        }
        private void LateUpdate()
        {
            if (_weapon == null) return;
            _weapon.SetActive(!_player.IsInVehicle && !_player.InputLocked);
            // Grip IK must continue even when the shoulder view briefly culls
            // the body's renderer (or a preview camera is rendering it).
            if (_animator != null && IsRanged && IsHoldingWeapon)
                _animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
            // The grip follows the animated hand; keep the barrel oriented away
            // from the body rather than inheriting arbitrary imported bone axes.
            float kick = _reduced ? 0 : Mathf.Clamp01((_aimUntil-Time.time)/.25f);
            _handSocket.rotation = transform.rotation * Quaternion.Euler(IsRanged ? 8 - kick*7 : 65 - kick*85, 0, 0);
        }

        private static GameObject Root(string name, Transform parent)
        {
            var root = new GameObject(name);
            root.transform.SetParent(parent, false);
            return root;
        }
        private static void Remove(ref GameObject item)
        {
            if (item != null) { item.SetActive(false); Destroy(item); }
            item = null;
        }
        private static void Part(Transform parent, string name, Vector3 position, Vector3 scale, Material material, PrimitiveType shape = PrimitiveType.Cube)
        {
            var part = GameObject.CreatePrimitive(shape);
            part.name = name;
            part.transform.SetParent(parent, false);
            part.transform.localPosition = position;
            part.transform.localScale = scale;
            var collider = part.GetComponent<Collider>();
            collider.enabled = false;
            Destroy(collider);
            part.GetComponent<Renderer>().sharedMaterial = material;
        }
        private static Material Material(string name, Color color, float metallic)
        {
            var material = RuntimeMaterialFactory.Create("Universal Render Pipeline/Lit", "Standard");
            material.name = name;
            material.color = color;
            material.SetColor("_BaseColor", color);
            material.SetFloat("_Metallic", metallic);
            material.SetFloat("_Smoothness", .5f);
            return material;
        }
        private static Mesh Dome()
        {
            const int slices = 24, rings = 8;
            var vertices = new List<Vector3>();
            var triangles = new List<int>();
            for (int y = 0; y <= rings; y++)
            {
                float theta = y * Mathf.PI / (2 * rings);
                for (int x = 0; x <= slices; x++)
                {
                    float phi = x * 2 * Mathf.PI / slices;
                    vertices.Add(new Vector3(Mathf.Sin(theta)*Mathf.Cos(phi), Mathf.Cos(theta), Mathf.Sin(theta)*Mathf.Sin(phi)));
                    if (y == rings || x == slices) continue;
                    int a = y*(slices+1)+x, b = a+slices+1;
                    triangles.AddRange(new[] {a,a+1,b, a+1,b+1,b});
                }
            }
            var mesh = new Mesh { name = "Open-face helmet hemisphere" };
            mesh.SetVertices(vertices); mesh.SetTriangles(triangles, 0); mesh.RecalculateNormals(); mesh.RecalculateBounds();
            return mesh;
        }
        private void OnDestroy()
        {
            foreach (var material in new[] {_dark,_shell,_lens,_metal,_blaster,_wood}) if (material != null) Destroy(material);
            if (_dome != null) Destroy(_dome);
        }
    }
}
