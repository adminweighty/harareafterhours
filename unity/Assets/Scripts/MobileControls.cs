using UnityEngine;

namespace HarareAfterHours
{
    /// <summary>
    /// Small, dependency-free input buffer used by the mobile HUD. The value
    /// expires quickly so controls never keep moving after a finger is lifted.
    /// </summary>
    public static class MobileControlState
    {
        private static Vector2 _move;
        private static int _moveFrame = -100;
        private static bool _brake;
        private static int _brakeFrame = -100;
        private static bool _interact;
        private static bool _pulse;
        private static bool _fight;
        private static bool _kick;
        public static void RequestKick() => _kick = true;
        public static bool ConsumeKick() { bool value=_kick; _kick=false; return value; }
        private static bool _ride;
        public static void RequestRide() => _ride = true;
        public static bool ConsumeRide() { bool value=_ride; _ride=false; return value; }
        private static int _sprintFrame = -100;
        private static bool _jump;

        public static void ClearHeld()
        {
            _move=Vector2.zero; _moveFrame=-100; _brake=false; _brakeFrame=-100; _sprintFrame=-100;
        }
        public static void Reset()
        {
            ClearHeld(); _interact=_pulse=_fight=_kick=_ride=_jump=false;
        }

        public static Vector2 Move => Time.frameCount - _moveFrame <= 2 ? _move : Vector2.zero;
        public static bool Brake => Time.frameCount - _brakeFrame <= 2 && _brake;
        public static bool Sprint => Time.frameCount - _sprintFrame <= 2;
        public static void SetSprint() => _sprintFrame = Time.frameCount;
        public static void RequestJump() => _jump = true;
        public static bool ConsumeJump()
        {
            bool requested = _jump;
            _jump = false;
            return requested;
        }

        public static void SetMove(Vector2 value)
        {
            _move = Vector2.ClampMagnitude(value, 1f);
            _moveFrame = Time.frameCount;
        }

        public static void SetBrake()
        {
            _brake = true;
            _brakeFrame = Time.frameCount;
        }

        public static void RequestInteract()
        {
            _interact = true;
        }

        public static void RequestPulse()
        {
            _pulse = true;
        }

        public static void RequestFight()
        {
            _fight = true;
        }

        public static bool ConsumeInteract()
        {
            if (!_interact) return false;
            _interact = false;
            return true;
        }

        public static bool ConsumePulse()
        {
            if (!_pulse) return false;
            _pulse = false;
            return true;
        }

        public static bool ConsumeFight()
        {
            if (!_fight) return false;
            _fight = false;
            return true;
        }
    }
}
