using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

namespace HarareAfterHours
{
    /// <summary>
    /// Arcade combat against training beacons and street encounter actors.
    /// </summary>
    [RequireComponent(typeof(ThirdPersonController))]
    public sealed class PlayerCombat : MonoBehaviour
    {
        [SerializeField] private float pulseCooldown = 0.32f;
        [SerializeField] private float fightCooldown = 0.68f;
        [SerializeField] private float pulseRange = 100f;
        public const float RifleRange = 250f;
        public const float PistolRange = 100f;
        [SerializeField] private float fightRange = 2.25f;

        private static Material _pulseMaterial;

        private ThirdPersonController _player;
        private PlayerProgression _progression;
        private float _nextPulseAt;
        private float _nextFightAt;
        private bool _aiming;
        private readonly System.Collections.Generic.Dictionary<string,int> _magazines = new();
        private float _reloadUntil;
        public int MagazineSize => IsAutomatic ? 30 : 12;
        public int Ammo => _magazines.TryGetValue(EquippedWeapon,out int ammo)?ammo:MagazineSize;
        public bool Reloading => _reloadUntil>0;
        public string WeaponLabel => IsAutomatic ? "AK RIFLE" : IsRanged ? "AGENT PISTOL" : EquippedWeapon.ToUpperInvariant();
        public void Reload()
        {
            if(!IsRanged||Reloading||Ammo>=MagazineSize||_player.InputLocked||_player.IsInVehicle||Time.timeScale<=0)return;
            _reloadUntil=Time.time+(IsAutomatic?2.1f:1.5f);
        }
        public bool IsAiming => IsRanged && _aiming;
        public void ToggleAim(){_aiming=IsRanged&&!_aiming;Camera.main?.GetComponent<ThirdPersonCameraRig>()?.Recenter();}
        public void ClearAim()=>_aiming=false;

        public string EquippedWeapon { get; private set; } = "Unarmed";
        public string AttackLabel => EquippedWeapon is "Unarmed" or "Baton" ? "HIT" : "FIRE";
        public bool IsRanged => EquippedWeapon is "Pulse pistol" or "Pulse rifle";
        public bool IsAutomatic => EquippedWeapon == "Pulse rifle";
        public float AttackRange => EquippedWeapon is "Unarmed" or "Baton" ? fightRange : pulseRange;
        public string ControlHint => $"{EquippedWeapon}  •  F / trigger";

        public void Equip(string weapon)
        {
            _aiming=false;
            _reloadUntil=0;
            EquippedWeapon = weapon is "Pulse pistol" or "Pulse rifle" or "Baton" ? weapon : "Unarmed";
            // World units are metres. The old 40/28m limits silently discarded
            // otherwise valid crosshair hits across the larger city streets.
            pulseRange = EquippedWeapon == "Pulse rifle" ? RifleRange : PistolRange;
            pulseCooldown = EquippedWeapon == "Pulse rifle" ? .18f : .38f;
            fightRange = EquippedWeapon == "Baton" ? 2.9f : 2.25f;
            // Swapping must not bypass the cooldown from the previous attack.
            _nextPulseAt = Mathf.Max(_nextPulseAt, Time.time + .18f);
            _nextFightAt = Mathf.Max(_nextFightAt, Time.time + .18f);
        }

        private void Awake()
        {
            _player = GetComponent<ThirdPersonController>();
        }

        public void Configure(PlayerProgression progression)
        {
            _progression = progression;
        }

        private void Update()
        {
            if (_player == null || _player.InputLocked || _player.IsInVehicle || Time.timeScale <= 0f) return;

            if(Reloading&&Time.time>=_reloadUntil){_magazines[EquippedWeapon]=MagazineSize;_reloadUntil=0;}

            if (WasPulseRequested()) TryAttack();
            if (WasFightRequested()) TryFight();
            if (MobileControlState.ConsumeKick() || (Keyboard.current!=null && Keyboard.current.kKey.wasPressedThisFrame) ||
                (Gamepad.current!=null && Gamepad.current.leftShoulder.wasPressedThisFrame)) TryKick();
        }

        public void TryAttack()
        {
            if (_player == null || _player.InputLocked || _player.IsInVehicle || Time.timeScale <= 0) return;
            if (EquippedWeapon is "Unarmed" or "Baton") { TryFight(); return; }
            if(Reloading)return;
            if(Ammo<=0){Reload();return;}
            if (Time.time < _nextPulseAt) return;
            _magazines[EquippedWeapon]=Ammo-1;
            _nextPulseAt = Time.time + pulseCooldown;
            Camera camera = Camera.main;
            Vector3 aimDirection = camera != null ? camera.transform.forward : transform.forward;
            Vector3 flatAim = Vector3.ProjectOnPlane(aimDirection, Vector3.up);
            if (IsAiming && flatAim.sqrMagnitude > .01f) transform.rotation = Quaternion.LookRotation(flatAim);
            var visual = GetComponent<PlayerEquipmentVisual>();
            visual?.ShowAttack();
            Vector3 chest = transform.position + Vector3.up * 1.4f;
            Vector3 origin = visual != null && visual.Muzzle != null ? visual.Muzzle.position : chest;
            Vector3 cameraOrigin = camera != null ? camera.transform.position : chest;
            float aimRange = pulseRange + Vector3.Distance(cameraOrigin, origin);
            Vector3 aimPoint = cameraOrigin + aimDirection * aimRange;
            bool direct = NearestHit(cameraOrigin, aimDirection, aimRange, out RaycastHit aimHit);
            if (direct) aimPoint = aimHit.point;
            // Keep precise direct hits. Assist only a visible, live hostile near the
            // reticle; both the camera and muzzle paths must be clear.
            if ((!direct || aimHit.collider.GetComponentInParent<StreetActor>() == null) &&
                TryAssistedAim(cameraOrigin, aimDirection, origin, out Vector3 assisted)) aimPoint = assisted;
            Vector3 shot = aimPoint - origin;
            // Cross the sampled surface slightly: an exact endpoint can round
            // outside a capsule and miss the character under the reticle.
            float distance = Mathf.Min(pulseRange, shot.magnitude + .025f);
            Vector3 end = origin + shot.normalized * distance;
            bool hit = false;
            bool characterHit = false;
            bool surfaceHit = false;
            // A muzzle beyond a nearby wall must not allow shooting through it.
            bool obstructed = NearestHit(chest, (origin-chest).normalized, Vector3.Distance(chest,origin), out RaycastHit impact);
            if (obstructed || NearestHit(origin, shot.normalized, distance, out impact))
            {
                surfaceHit = true;
                end = impact.point;
                var target = impact.collider.GetComponentInParent<CombatTarget>();
                if (!obstructed && target != null) hit = target.ReceiveHit(CombatAction.Pulse);
                else if (!obstructed)
                {
                    var actor = impact.collider.GetComponentInParent<StreetActor>();
                    if (actor != null) characterHit = hit = actor.ReceiveShot(impact.point, shot.normalized);
                }
            }
            if (!hit) _progression?.RegisterMiss();
            GetComponent<WeaponFeedback>()?.Fire(visual?.Muzzle, origin, end, hit, characterHit, surfaceHit);
        }

        private bool TryAssistedAim(Vector3 eye, Vector3 forward, Vector3 muzzle, out Vector3 point)
        {
            point = default;
            var world = StreetActionDirector.Instance;
            if (world == null) return false;
            float best = Mathf.Cos((IsAiming ? 3f : 10f) * Mathf.Deg2Rad);
            bool found = false;
            foreach (var actor in world.Actors)
            {
                if (actor == null || actor.Officer || actor.Down || !actor.gameObject.activeInHierarchy) continue;
                Vector3 target = actor.transform.position + Vector3.up * 1.2f;
                Vector3 fromEye = target - eye, fromMuzzle = target - muzzle;
                if (fromMuzzle.magnitude > pulseRange || fromEye.sqrMagnitude < .01f) continue;
                float alignment = Vector3.Dot(forward, fromEye.normalized);
                if (alignment <= best) continue;
                if (!NearestHit(eye, fromEye.normalized, fromEye.magnitude + .1f, out var cameraHit) ||
                    cameraHit.collider.GetComponentInParent<StreetActor>() != actor) continue;
                if (!NearestHit(muzzle, fromMuzzle.normalized, fromMuzzle.magnitude + .1f, out var muzzleHit) ||
                    muzzleHit.collider.GetComponentInParent<StreetActor>() != actor) continue;
                best = alignment; point = target; found = true;
            }
            return found;
        }

        private void TryFight()
        {
            if(_player==null||_player.InputLocked||_player.IsInVehicle||Time.timeScale<=0)return;
            if (Time.time < _nextFightAt) return;
            _nextFightAt = Time.time + fightCooldown;
            GetComponent<PlayerEquipmentVisual>()?.ShowAttack();
            GetComponentInChildren<StreetActionPose>()?.Punch();
            StartCoroutine(ResolveFightContact(false));
        }

        public void TryKick()
        {
            if(_player==null||_player.InputLocked||_player.IsInVehicle||Time.timeScale<=0||Time.time<_nextFightAt)return;
            _nextFightAt=Time.time+.95f;
            GetComponentInChildren<StreetActionPose>()?.Kick();
            StartCoroutine(ResolveFightContact(true));
        }

        public void CancelPendingMelee()
        {
            StopAllCoroutines();
            _nextFightAt=Time.time+fightCooldown;
        }

        private IEnumerator ResolveFightContact(bool kick)
        {
            yield return new WaitForSeconds(kick ? .22f : .12f);
            if(_player==null||_player.InputLocked||_player.IsInVehicle||StreetActionDirector.Instance?.GameOver==true)yield break;
            float range=kick?2.6f:fightRange;
            if(StreetActionDirector.Instance!=null && StreetActionDirector.Instance.Strike(range,false,kick?2:1))yield break;

            Vector3 center = transform.position + Vector3.up * 1.05f + transform.forward * 1.15f;
            Collider[] nearby = Physics.OverlapSphere(center, range, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore);
            CombatTarget bestTarget = null;
            float bestAlignment = 0.2f;

            foreach (Collider collider in nearby)
            {
                CombatTarget target = collider.GetComponentInParent<CombatTarget>();
                if (target == null) continue;

                Vector3 targetDirection = (target.transform.position - transform.position).normalized;
                float alignment = Vector3.Dot(transform.forward, targetDirection);
                Vector3 chest = transform.position + Vector3.up * 1.05f;
                Vector3 toTarget = collider.bounds.center - chest;
                if (FindTargetAlongRay(chest, toTarget.normalized, toTarget.magnitude + .1f, out _) != target) continue;
                if (alignment > bestAlignment)
                {
                    bestAlignment = alignment;
                    bestTarget = target;
                }
            }

            if (bestTarget != null)
            {
                bestTarget.ReceiveHit(CombatAction.Fight, kick || EquippedWeapon == "Baton" ? 3 : 2);
            }
            else
            {
                _progression?.RegisterMiss();
            }
        }

        private CombatTarget FindTargetAlongRay(Vector3 origin, Vector3 direction, float range, out RaycastHit targetHit)
        {
            targetHit = default;
            float nearestTargetDistance = float.MaxValue;
            float nearestObstacleDistance = float.MaxValue;
            RaycastHit[] hits = Physics.RaycastAll(origin, direction, range, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore);

            foreach (RaycastHit hit in hits)
            {
                if (hit.collider.transform.IsChildOf(transform)) continue;

                CombatTarget target = hit.collider.GetComponentInParent<CombatTarget>();
                if (target != null)
                {
                    if (hit.distance < nearestTargetDistance)
                    {
                        nearestTargetDistance = hit.distance;
                        targetHit = hit;
                    }
                }
                else if (hit.distance < nearestObstacleDistance)
                {
                    nearestObstacleDistance = hit.distance;
                }
            }

            if (nearestTargetDistance <= nearestObstacleDistance)
            {
                return targetHit.collider != null ? targetHit.collider.GetComponentInParent<CombatTarget>() : null;
            }

            return null;
        }

        private bool NearestHit(Vector3 origin, Vector3 direction, float range, out RaycastHit nearest)
        {
            nearest = default;
            float distance = float.PositiveInfinity;
            foreach (var hit in Physics.RaycastAll(origin, direction, range, ~0, QueryTriggerInteraction.Ignore))
            {
                if (hit.transform.IsChildOf(transform) || hit.distance >= distance) continue;
                nearest = hit; distance = hit.distance;
            }
            return nearest.collider != null;
        }

        private static void CreatePulseTrail(Vector3 start, Vector3 end, Color color)
        {
            GameObject beam = new("Pulse trail");
            LineRenderer line = beam.AddComponent<LineRenderer>();
            line.positionCount = 2;
            line.SetPositions(new[] { start, end });
            line.startWidth = 0.055f;
            line.endWidth = 0.025f;
            line.alignment = LineAlignment.View;
            line.sharedMaterial = PulseMaterial;
            line.startColor = color;
            line.endColor = new Color(color.r, color.g, color.b, 0.15f);
            Destroy(beam, 0.12f);
        }

        private static Material PulseMaterial
        {
            get
            {
                if (_pulseMaterial != null) return _pulseMaterial;

                _pulseMaterial = RuntimeMaterialFactory.Create("Universal Render Pipeline/Unlit", "Sprites/Default");
                if (_pulseMaterial.HasProperty("_BaseColor")) _pulseMaterial.SetColor("_BaseColor", Color.white);
                if (_pulseMaterial.HasProperty("_Color")) _pulseMaterial.color = Color.white;
                return _pulseMaterial;
            }
        }

        private bool WasPulseRequested()
        {
            return MobileControlState.ConsumePulse() ||
                   (Keyboard.current != null && (IsAutomatic ? Keyboard.current.fKey.isPressed : Keyboard.current.fKey.wasPressedThisFrame)) ||
                   (!TouchGameplayControls.Active && !Application.isMobilePlatform && Mouse.current != null && (IsAutomatic ? Mouse.current.leftButton.isPressed : Mouse.current.leftButton.wasPressedThisFrame)) ||
                   (Gamepad.current != null && (IsAutomatic ? Gamepad.current.rightTrigger.isPressed : Gamepad.current.rightTrigger.wasPressedThisFrame));
        }

        private static bool WasFightRequested()
        {
            return MobileControlState.ConsumeFight() ||
                   (Keyboard.current != null && Keyboard.current.qKey.wasPressedThisFrame) ||
                   (Gamepad.current != null && Gamepad.current.rightShoulder.wasPressedThisFrame);
        }
    }

    /// <summary>
    /// A reusable practice target that absorbs non-graphic challenge actions,
    /// awards a score on completion, then resets so the city stays explorable.
    /// </summary>
    public sealed class CombatTarget : MonoBehaviour
    {
        [SerializeField] private float respawnDelay = 5f;

        private PlayerProgression _progression;
        private Renderer[] _renderers;
        private MaterialPropertyBlock[] _originalBlocks;
        private MaterialPropertyBlock _hitBlock;
        private Light[] _lights;
        private Collider _collider;
        private int _maxIntegrity = 3;
        private int _integrity;
        private bool _available = true;
        private Vector3 _basePosition;
        private float _flashUntil;

        private void Awake()
        {
            _basePosition = transform.position;
        }

        public void Configure(PlayerProgression progression, int integrity = 3)
        {
            _hitBlock = new MaterialPropertyBlock();
            _progression = progression;
            _maxIntegrity = Mathf.Max(1, integrity);
            _integrity = _maxIntegrity;
            _collider = GetComponent<Collider>();
            _renderers = GetComponentsInChildren<Renderer>();
            _originalBlocks = new MaterialPropertyBlock[_renderers.Length];
            for(int i=0;i<_renderers.Length;i++)
            {
                _originalBlocks[i]=new MaterialPropertyBlock();
                _renderers[i].GetPropertyBlock(_originalBlocks[i]);
            }
            _lights = GetComponentsInChildren<Light>();
        }

        private void Update()
        {
            if (_available)
            {
                if (_flashUntil > 0f && Time.time >= _flashUntil)
                {
                    _flashUntil = 0f;
                    RestoreMaterials();
                }
            }
        }

        public int RemainingIntegrity => _integrity;

        public bool ReceiveHit(CombatAction action, int damage = 0)
        {
            if (!_available) return false;

            _integrity -= damage > 0 ? damage : action == CombatAction.Fight ? 2 : 1;
            if (_integrity > 0)
            {
                _flashUntil = Time.time + 0.16f;
                SetTint(new Color(1f, 0.55f, 0.16f));
                return true;
            }

            _available = false;
            _collider.enabled = false;
            foreach (Renderer renderer in _renderers) renderer.enabled = false;
            foreach (Light light in _lights) light.enabled = false;
            _progression?.AwardChallenge(action);
            StartCoroutine(RespawnRoutine());
            return true;
        }

        private IEnumerator RespawnRoutine()
        {
            yield return new WaitForSeconds(respawnDelay);
            _integrity = _maxIntegrity;
            _available = true;
            _collider.enabled = true;
            foreach (Renderer renderer in _renderers) renderer.enabled = true;
            foreach (Light light in _lights) light.enabled = true;
            _flashUntil=0;
            RestoreMaterials();
        }

        private void RestoreMaterials()
        {
            for(int i=0;i<_renderers.Length;i++)_renderers[i].SetPropertyBlock(_originalBlocks[i]);
        }

        private void SetTint(Color color)
        {
            if (_renderers == null) return;
            foreach (Renderer renderer in _renderers)
            {
                renderer.GetPropertyBlock(_hitBlock);
                _hitBlock.SetColor("_BaseColor", color);
                _hitBlock.SetColor("_Color", color);
                renderer.SetPropertyBlock(_hitBlock);
            }
        }
    }
}
