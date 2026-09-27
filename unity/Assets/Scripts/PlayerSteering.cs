using UnityEngine;

namespace HarareAfterHours
{
    /// <summary>Precision taps, gentle held turns, and no rotation after release.</summary>
    public struct PlayerSteering
    {
        private float _heldSeconds, _direction;
        private const float RampSeconds = .4f;

        public void Reset() { _heldSeconds = 0; _direction = 0; }

        public float Step(float input, bool aiming, float deltaTime)
        {
            if (Mathf.Abs(input) <= .1f || deltaTime <= 0) { Reset(); return 0; }
            float direction = Mathf.Sign(input);
            if (direction != _direction) _heldSeconds = 0;
            _direction = direction;
            // Integrate the speed ramp so 30, 60 and 120 fps turn equally far.
            float next = _heldSeconds + deltaTime;
            float degrees = Distance(next) - Distance(_heldSeconds);
            _heldSeconds = Mathf.Min(next, RampSeconds);
            return degrees * Mathf.Clamp(input, -1, 1) * (aiming ? .55f : 1f);
        }

        private static float Distance(float seconds)
        {
            float ramp = Mathf.Min(seconds, RampSeconds);
            return 20f * ramp + 31.25f * ramp * ramp + 45f * Mathf.Max(0, seconds - RampSeconds);
        }
    }
}
