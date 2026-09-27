using UnityEngine;
using UnityEngine.InputSystem;

namespace HarareAfterHours
{
    /// <summary>
    /// Two-mode third-person camera with lightweight collision avoidance. It follows
    /// either the player or their current vehicle and intentionally avoids a
    /// Cinemachine dependency for the first mobile build.
    /// </summary>
    public sealed class ThirdPersonCameraRig : MonoBehaviour
    {
        [SerializeField] private Vector3 playerViewOffset = new(.25f, 1.55f, -7.2f);
        [SerializeField] private Vector3 currentViewOffset = new(.35f, 1.1f, -5.4f);
        [SerializeField] private Vector3 drivingOffset = new(0f, 2.3f, -8f);
        [SerializeField] private float followSmoothTime = 0.08f;
        [SerializeField] private float mouseSensitivity = 0.12f;
        [SerializeField] private float gamepadSensitivity = 130f;

        private Transform _target;
        private Vector3 _velocity;
        private float _yaw;
        private float _movementYaw;
        private bool _movementHeld;
        private float _pitch = 6f;
        private bool _driving;
        private PlayerCombat _combat;
        private ThirdPersonController _player;
        private int _lookFinger = -1;
        private int _touchLookFrame = -100;
        private bool _guiLook;
        private VehicleController _vehicle;
        private Transform _collisionRoot;
        private float _lastLook = -10;
        private Camera _camera;
        private bool _snap;
        private float _openingVista;
        private bool _openingVistaReleased;
        private bool _currentView;
        /// <summary>The default mode frames the complete playable character.</summary>
        public bool PlayerView => !_currentView || _driving;
        /// <summary>Option two preserves the previous, closer shoulder camera.</summary>
        public bool CurrentView => _currentView && !_driving;
        public string CameraModeLabel => CurrentView ? "CURRENT\nVIEW" : "PLAYER\nVIEW";
        public void TogglePerspective()
        {
            if (_driving) return;
            _currentView = !_currentView;
            _openingVista = 0;
            _openingVistaReleased = true;
            _snap = true;
        }
        public void FrameJoinaOpening() { _openingVista = 1; _openingVistaReleased = false; }
        private readonly RaycastHit[] _cameraHits = new RaycastHit[64];

        public void Recenter()
        {
            _openingVistaReleased=true;
            if (_target == null) return;
            _yaw = (_vehicle != null ? _vehicle.transform : _target).eulerAngles.y;
            _pitch = 6;
            _lastLook = -10;
            if (!_movementHeld) _movementYaw = _yaw;
        }

        public void FollowPlayerTurn(float heading)
        {
            if (_driving) return;
            _yaw = heading;
            _movementYaw = heading;
            _lastLook = -10;
            _openingVistaReleased = true;
        }

        // Freeze the movement reference during a held stick/key gesture. Automatic
        // camera yaw must not feed back into movement and create endless circles.
        public Vector3 MovementForward(Vector2 input)
        {
            bool held = input.sqrMagnitude > .001f;
            if (!held || !_movementHeld) _movementYaw = _yaw;
            _movementHeld = held;
            return Quaternion.Euler(0, _movementYaw, 0) * Vector3.forward;
        }

        private void ManualYaw(float delta)
        {
            _yaw += delta;
            _movementYaw += delta;
        }

        public Vector3 HeadingForward => Quaternion.Euler(0, _yaw, 0) * Vector3.forward;

        public Vector3 FlatForward
        {
            get
            {
                Vector3 forward = transform.forward;
                forward.y = 0f;
                return forward.sqrMagnitude > 0.001f ? forward.normalized : Vector3.forward;
            }
        }

        public void SetTarget(Transform target, bool driving)
        {
            _target = target;
            _driving = driving;
            _combat = !driving ? target.GetComponent<PlayerCombat>() : null;
            _player = !driving ? target.GetComponent<ThirdPersonController>() : null;
            _vehicle = driving ? target.GetComponentInParent<VehicleController>() : null;
            _collisionRoot = _vehicle != null ? _vehicle.transform : target;
            _camera = GetComponent<Camera>();
            _velocity = Vector3.zero;
            _snap = true;
            _movementHeld = false;
            Recenter();
        }

        private void Update()
        {
            if (Time.timeScale <= 0 || (_player != null && _player.InputLocked)) return;
            Vector2 look = Vector2.zero;
            if (Mouse.current != null && Mouse.current.rightButton.isPressed)
            {
                look += Mouse.current.delta.ReadValue() * mouseSensitivity;
            }

            if (Gamepad.current != null)
            {
                look += Gamepad.current.rightStick.ReadValue() * gamepadSensitivity * Time.deltaTime;
            }

            // Drag the clear middle-right area; exclude HUD, Pause and controls.
            var screen = Touchscreen.current;
            if (screen != null && !TouchGameplayControls.Active)
            {
                bool held = false;
                foreach (var touch in screen.touches)
                {
                    Vector2 p = touch.position.ReadValue();
                    if (!touch.press.isPressed) continue;
                    int finger = touch.touchId.ReadValue();
                    if (_lookFinger < 0 && touch.press.wasPressedThisFrame &&
                        p.x > Screen.width*.45f && p.y > Screen.height*.26f && p.y < Screen.height*.60f)
                        _lookFinger = finger;
                    if (finger != _lookFinger) continue;
                    held = true;
                    _touchLookFrame = Time.frameCount;
                    look += touch.delta.ReadValue() * (120f / Mathf.Min(Screen.width,Screen.height));
                }
                if (!held) _lookFinger = -1;
            }

            if (Keyboard.current != null && Keyboard.current.vKey.wasPressedThisFrame) Recenter();
            if (look.sqrMagnitude > .0001f) _lastLook = Time.time;
            ManualYaw(look.x);
            _pitch = Mathf.Clamp(_pitch - look.y, -65f, 75f);
        }

        private void OnGUI()
        {
            if(TouchGameplayControls.Active)return;
            // Some embedded/simulator views deliver IMGUI pointer events but
            // no Input System touch delta. Keep a single-pointer fallback,
            // without applying the same physical drag twice on real phones.
            var pointer = Event.current;
            if (pointer.type == EventType.MouseUp) _guiLook = false;
            if (Time.timeScale <= 0 || (_player != null && _player.InputLocked))
            { _guiLook = false; return; }
            var area = new Rect(Screen.width*.45f, Screen.height*.40f, Screen.width*.55f, Screen.height*.34f);
            if (pointer.type == EventType.MouseDown && pointer.button == 0)
                _guiLook = area.Contains(pointer.mousePosition);
            if (_guiLook && pointer.type == EventType.MouseDrag && Time.frameCount-_touchLookFrame > 1)
            {
                float sensitivity = 120f/Mathf.Min(Screen.width,Screen.height);
                ManualYaw(pointer.delta.x*sensitivity);
                _lastLook = Time.time;
                _pitch = Mathf.Clamp(_pitch+pointer.delta.y*sensitivity,-18,58);
                pointer.Use();
            }
        }

        public void ApplyTouchLook(Vector2 guiDelta)
        {
            if(Time.timeScale<=0||(_player!=null&&_player.InputLocked))return;
            ManualYaw(guiDelta.x);_pitch=Mathf.Clamp(_pitch+guiDelta.y,-65,75);_lastLook=Time.time;
        }

        private void LateUpdate()
        {
            if (_target == null || Time.timeScale <= 0) return;

            if(_camera!=null)_camera.nearClipPlane=.3f;

            float speed = _vehicle != null ? _vehicle.SpeedKph / 3.6f : (_player != null ? _player.CurrentSpeed : 0);
            if (speed > .1f || _lastLook >= 0 || _driving) _openingVistaReleased = true;
            if (_openingVistaReleased) _openingVista = Mathf.MoveTowards(_openingVista, 0, Time.deltaTime * 2);
            // Follow turns without requiring VIEW, including walking/running.
            // Manual look gets a brief grace period; paused/locked actors never steer it.
            bool follow = _driving ? _vehicle != null && speed > .8f
                : _player != null && !_player.InputLocked && (speed > .1f || _player.IsTurning);
            if (follow && (_combat == null || !_combat.IsAiming) && Time.time - _lastLook > (_driving ? 1.8f : .65f))
            {
                float heading = (_vehicle != null ? _vehicle.transform : _target).eulerAngles.y;
                _yaw = Mathf.LerpAngle(_yaw, heading, 1-Mathf.Exp(-4.5f*Time.deltaTime));
                _pitch = Mathf.Lerp(_pitch, 6, 1-Mathf.Exp(-2*Time.deltaTime));
            }

            bool armed = !_driving && _combat != null && _combat.IsAiming && (_player == null || !_player.InputLocked);
            Vector3 focus = _target.position + Vector3.up * (_driving ? .35f : armed ? 1.48f : CurrentView ? 1.35f : 1.12f);
            if(!_driving&&_player!=null)focus+=Vector3.up*_player.StanceOffset;
            Quaternion orbit = Quaternion.Euler(_pitch, _yaw, 0f);
            Vector3 desired = focus + orbit * (_driving ? drivingOffset : armed ? new Vector3(.78f,.28f,-3.4f) : CurrentView ? currentViewOffset : playerViewOffset);
            if (_openingVista > 0) desired = Vector3.Lerp(desired, focus + Quaternion.Euler(0,_yaw,0) * new Vector3(.7f,1.2f,-11), _openingVista);
            Vector3 candidate = _snap ? desired : Vector3.SmoothDamp(transform.position, desired, ref _velocity, followSmoothTime);
            // Test the smoothed position too: smoothing before collision prevents
            // the camera easing through a wall. Ignore the entire occupied car.
            Vector3 direction = candidate - focus;
            float distance = direction.magnitude;
            int count = Physics.SphereCastNonAlloc(focus, .28f, direction.normalized, _cameraHits, distance, ~0, QueryTriggerInteraction.Ignore);
            // Overflow is rare but must not silently miss an obstacle in a dense block.
            RaycastHit[] hits = count == _cameraHits.Length
                ? Physics.SphereCastAll(focus, .28f, direction.normalized, distance, ~0, QueryTriggerInteraction.Ignore) : _cameraHits;
            if (hits != _cameraHits) count = hits.Length;
            for (int index = 0; index < count; index++)
            {
                var hit = hits[index];
                if (hit.transform == _collisionRoot || hit.transform.IsChildOf(_collisionRoot)) continue;
                distance = Mathf.Min(distance, Mathf.Max(.1f, hit.distance-.08f));
            }
            transform.position = focus + direction.normalized * distance;
            _snap = false;
            Vector3 ahead = Quaternion.Euler(0,_yaw,0)*Vector3.forward;
            Vector3 lookAt = armed ? focus + orbit * new Vector3(.55f,0,8) : focus + ahead * (_driving ? Mathf.Lerp(2,5,Mathf.Clamp01(speed/22)) : 10f);
            if (_openingVista > 0) lookAt = Vector3.Lerp(lookAt, focus + ahead * 70 + Vector3.up * 20, _openingVista);
            transform.rotation = Quaternion.LookRotation((lookAt - transform.position).normalized, Vector3.up);
            if (_camera != null)
            {
                float gameplayFov = armed ? 60 : _driving ? Mathf.Lerp(65,72,Mathf.Clamp01(speed/22)) : CurrentView ? 65 : 62;
                float openingFov = _camera.aspect < 1 ? 85 : 72;
                _camera.fieldOfView = Mathf.Lerp(_camera.fieldOfView, Mathf.Lerp(gameplayFov,openingFov,_openingVista), 1-Mathf.Exp(-4*Time.deltaTime));
            }
        }
    }
}
