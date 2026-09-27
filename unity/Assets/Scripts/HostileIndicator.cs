using UnityEngine;
using UnityEngine.Rendering;

namespace HarareAfterHours
{
    /// <summary>World-space threat marker for every live robber. Shared mesh/material keep it cheap on mobile.</summary>
    [DefaultExecutionOrder(320)]
    public sealed class HostileIndicator : MonoBehaviour
    {
        private StreetActor _actor;
        private Transform _visual;
        private Transform _head;
        private readonly RaycastHit[] _sightHits = new RaycastHit[24];
        private float _nextSight;
        public bool HasClearView { get; private set; }
        private static Mesh _chevron;
        private static Material _material;

        public bool IsVisible => _visual != null && _visual.gameObject.activeInHierarchy;
        public Vector3 IndicatorPosition => _visual != null ? _visual.position : Vector3.zero;

        public void Configure(StreetActor actor)
        {
            _actor = actor;
            _visual = CreateMarkerVisual(transform, "Hostile red indicator");

            var animator = GetComponentInChildren<Animator>();
            _head = animator != null && animator.isHuman ? animator.GetBoneTransform(HumanBodyBones.Head) : null;
            _visual.gameObject.SetActive(false);
        }

        private void LateUpdate() => Refresh();

        public void Refresh()
        {
            var world = StreetActionDirector.Instance;
            Camera camera = Camera.main;
            bool shouldShow = _actor != null && _actor.isActiveAndEnabled && !_actor.Officer && !_actor.Down &&
                              world != null && world.Player != null && !world.GameOver && camera != null;
            if (_visual == null) return;
            if (_visual.gameObject.activeSelf != shouldShow) _visual.gameObject.SetActive(shouldShow);
            if (!shouldShow) return;

            if (Time.unscaledTime >= _nextSight)
            {
                _nextSight = Time.unscaledTime + .15f;
                HasClearView = ClearView(camera, transform.position + Vector3.up) ||
                    ClearView(camera, _head != null ? _head.position : transform.position + Vector3.up * 1.7f);
            }
            Transform anchor = _head != null ? _head : transform;
            // Keep the chevron clear of hair, hats and the HUD label on varied rigs.
            _visual.position = anchor.position + Vector3.up * (_head != null ? .72f : 2.45f);
            Vector3 toCamera = camera.transform.position - _visual.position;
            toCamera.y = 0f;
            if (toCamera.sqrMagnitude > .001f) _visual.rotation = Quaternion.LookRotation(toCamera.normalized, Vector3.up);
            float distance = Vector3.Distance(camera.transform.position, _visual.position);
            float distanceScale = Mathf.Clamp(distance * .027f, .9f, 1.8f);
            float pulse = .92f + Mathf.PingPong(Time.unscaledTime * 1.8f, .15f);
            _visual.localScale = Vector3.one * (distanceScale * pulse);
        }

        private bool ClearView(Camera camera, Vector3 point)
        {
            var delta = point - camera.transform.position;
            int count = Physics.RaycastNonAlloc(camera.transform.position, delta.normalized,
                _sightHits, delta.magnitude, ~0, QueryTriggerInteraction.Ignore);
            if (count == _sightHits.Length) return false;
            var player = StreetActionDirector.Instance?.Player;
            for (int i = 0; i < count; i++)
            {
                var hit = _sightHits[i];
                if (hit.transform.IsChildOf(transform) || hit.collider is WheelCollider) continue;
                if (player != null && hit.transform.IsChildOf(player.transform)) continue;
                return false;
            }
            return true;
        }

        /// <summary>Creates the same cheap red chevron for mission thieves and encounter hostiles.</summary>
        public static Transform CreateMarkerVisual(Transform parent, string label)
        {
            var indicator = new GameObject(label);
            indicator.transform.SetParent(parent, false);
            indicator.AddComponent<MeshFilter>().sharedMesh = ChevronMesh;
            var renderer = indicator.AddComponent<MeshRenderer>();
            renderer.sharedMaterial = MarkerMaterial;
            renderer.shadowCastingMode = ShadowCastingMode.Off;
            renderer.receiveShadows = false;
            renderer.lightProbeUsage = LightProbeUsage.Off;
            renderer.reflectionProbeUsage = ReflectionProbeUsage.Off;
            return indicator.transform;
        }

        private static Mesh ChevronMesh
        {
            get
            {
                if (_chevron != null) return _chevron;
                _chevron = new Mesh { name = "Shared hostile red chevron" };
                _chevron.vertices = new[]
                {
                    new Vector3(-.23f, .16f, 0f),
                    new Vector3(.23f, .16f, 0f),
                    new Vector3(0f, -.22f, 0f),
                };
                // Render from both sides: the player can circle a hostile.
                _chevron.triangles = new[] { 0, 1, 2, 2, 1, 0 };
                _chevron.RecalculateBounds();
                return _chevron;
            }
        }

        private static Material MarkerMaterial
        {
            get
            {
                if (_material != null) return _material;
                _material = RuntimeMaterialFactory.Create("Universal Render Pipeline/Unlit", "Sprites/Default");
                Color red = new(1f, .035f, .055f, 1f);
                if (_material.HasProperty("_BaseColor")) _material.SetColor("_BaseColor", red);
                if (_material.HasProperty("_Color")) _material.SetColor("_Color", red);
                if (_material.HasProperty("_Cull")) _material.SetFloat("_Cull", 0f);
                return _material;
            }
        }
    }
}
