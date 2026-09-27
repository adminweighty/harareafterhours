using System.Collections.Generic;
using UnityEngine;

namespace HarareAfterHours
{
    /// <summary>
    /// Keeps emissive fixtures across the city while enabling only nearby real-time
    /// lights. This stays below URP's mobile per-camera light limit and avoids all
    /// additional-light shadow maps.
    /// </summary>
    public sealed class AdaptiveStreetLighting : MonoBehaviour
    {
        public const int MobileLightBudget = 22;
        public const float ActivationRange = 92f;
        public static AdaptiveStreetLighting Instance { get; private set; }
        readonly List<Light> _fixtures = new();
        Vector3 _observer;
        float _nextRefresh;

        public int FixtureCount => _fixtures.Count;
        public int ActiveLightCount { get; private set; }

        void Awake() => Instance = this;

        public void Register(Light light)
        {
            if (light == null || _fixtures.Contains(light)) return;
            light.shadows = LightShadows.None;
            light.enabled = false;
            _fixtures.Add(light);
            _nextRefresh = 0;
        }

        void LateUpdate()
        {
            if (Time.unscaledTime < _nextRefresh) return;
            _nextRefresh = Time.unscaledTime + .2f;
            var camera = Camera.main;
            if (camera == null) return;
            _observer = camera.transform.position;
            _fixtures.RemoveAll(light => light == null);
            _fixtures.Sort(CompareDistance);
            float maximumSqr = ActivationRange * ActivationRange;
            ActiveLightCount = 0;
            for (int index = 0; index < _fixtures.Count; index++)
            {
                Light light = _fixtures[index];
                bool active = index < MobileLightBudget && (light.transform.position - _observer).sqrMagnitude <= maximumSqr;
                if (light.enabled != active) light.enabled = active;
                if (active) ActiveLightCount++;
            }
        }

        int CompareDistance(Light a, Light b) =>
            (a.transform.position - _observer).sqrMagnitude.CompareTo((b.transform.position - _observer).sqrMagnitude);

        void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }
    }
}
