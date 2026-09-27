using UnityEngine;
using UnityEngine.InputSystem;

namespace HarareAfterHours
{
    [RequireComponent(typeof(CharacterController))]
    [RequireComponent(typeof(AvatarCustomization))]
    public sealed class ThirdPersonController : MonoBehaviour
    {
        [SerializeField] private float walkSpeed = 1.8f;
        [SerializeField] private float sprintSpeed = 4.5f;
        [SerializeField] private float jumpVelocity = 5.4f;
        private PlayerSteering _steering;

        private CharacterController _characterController;
        private AvatarCustomization _avatar;
        private ThirdPersonCameraRig _cameraRig;
        private VehicleController _vehicle;
        private MissionDirector _mission;
        private FlutterGameBridge _bridge;
        private Vector3 _verticalVelocity;
        private Vector3 _smoothMovement;
        private readonly RaycastHit[] _wallHits = new RaycastHit[16];
        private Vector3 _lastWallNormal;
        private float _blockedSince=-1f,_lastUnstick=-100f;
        private float _lastGrounded=-100,_jumpQueued=-100;
        private float _standingHeight,_slideUntil,_slideReady;
        private Vector3 _standingCenter,_slideDirection;
        public bool IsCrouching {get;private set;}
        public bool IsSliding=>Time.time<_slideUntil;
        public float StanceOffset=>IsCrouching?-.55f:0;
        public void ToggleCrouchSlide()
        {
            if(InputLocked||IsInVehicle||Time.timeScale<=0)return;
            if(IsCrouching){TryStand();return;}
            if(!_characterController.isGrounded)return;
            IsCrouching=true;
            if(CurrentSpeed>3.5f&&Time.time>=_slideReady)
            {
                _slideDirection=_smoothMovement.normalized;_slideUntil=Time.time+.7f;_slideReady=Time.time+1.5f;
            }
            SetStance();
        }
        private void SetStance()
        {
            _characterController.height=IsCrouching?_standingHeight*.62f:_standingHeight;
            _characterController.center=_standingCenter-Vector3.up*(_standingHeight-_characterController.height)*.5f;
        }
        private bool TryStand()
        {
            if(!IsCrouching)return true;
            float radius=_characterController.radius*.95f;
            Vector3 top=transform.TransformPoint(_standingCenter)+Vector3.up*(_standingHeight*.5f-radius);
            Vector3 bottom=transform.TransformPoint(_characterController.center)+Vector3.up*(_characterController.height*.5f-radius);
            foreach(var c in Physics.OverlapCapsule(bottom,top,radius,~0,QueryTriggerInteraction.Ignore))
                if(!c.transform.IsChildOf(transform))return false;
            IsCrouching=false;_slideUntil=0;SetStance();return true;
        }

        public bool IsDriving => _vehicle != null && _vehicle.Driver == this;
        public bool IsPassenger => _vehicle != null && _vehicle.Passenger == this;
        public bool IsInVehicle => IsDriving || IsPassenger;
        public VehicleController CurrentVehicle => IsInVehicle ? _vehicle : null;
        public string VehicleHint { get; private set; }
        private Transform _onFootParent;
        public float CurrentSpeed { get; private set; }
        public AvatarCustomization Avatar => _avatar;
        public bool InputLocked { get; set; }
        public bool IsTurning { get; private set; }

        private void Awake()
        {
            _characterController = GetComponent<CharacterController>();
            _characterController.skinWidth=Mathf.Max(.05f,_characterController.skinWidth);
            _characterController.minMoveDistance=0f;
            _characterController.slopeLimit=Mathf.Max(50f,_characterController.slopeLimit);
            _standingHeight=_characterController.height;_standingCenter=_characterController.center;
            _avatar = GetComponent<AvatarCustomization>();
        }

        public void Configure(VehicleController vehicle, MissionDirector mission, FlutterGameBridge bridge)
        {
            _vehicle = vehicle;
            _mission = mission;
            _bridge = bridge;
            _cameraRig = Camera.main != null ? Camera.main.GetComponent<ThirdPersonCameraRig>() : FindFirstObjectByType<ThirdPersonCameraRig>();
        }

        private void Update()
        {
            IsTurning=false;
            if(Time.timeScale<=0){_steering.Reset();return;}
            if(InputLocked)
            {
                _steering.Reset();
                _smoothMovement=Vector3.zero;_jumpQueued=-100;
                CurrentSpeed=0;
                MobileControlState.ConsumeFight();MobileControlState.ConsumePulse();MobileControlState.ConsumeKick();
                MobileControlState.ConsumeInteract();MobileControlState.ConsumeRide();MobileControlState.ConsumeJump();
                return;
            }
            if (_cameraRig == null)
            {
                _cameraRig = Camera.main != null ? Camera.main.GetComponent<ThirdPersonCameraRig>() : FindFirstObjectByType<ThirdPersonCameraRig>();
                if (_cameraRig == null) return;
            }

            if (IsInVehicle)
            {
                _steering.Reset();
                _smoothMovement=Vector3.zero;_jumpQueued=-100;
                UpdateDriving();
                return;
            }

            UpdateOnFoot();
        }

        private void UpdateOnFoot()
        {
            if(Keyboard.current?.cKey.wasPressedThisFrame==true)ToggleCrouchSlide();
            bool aiming=GetComponent<PlayerCombat>()?.IsAiming==true;
            Vector2 input = ReadMoveInput();
            Vector3 start = transform.position;
            // Forward/back follow the body heading. Horizontal input only steers;
            // the camera can never feed sideways velocity back into locomotion.
            if (aiming && !IsSliding)
                transform.rotation = Quaternion.LookRotation(_cameraRig.HeadingForward, Vector3.up);
            float yaw = _steering.Step(IsSliding ? 0 : input.x, aiming, Time.deltaTime);
            IsTurning = Mathf.Abs(yaw) > .00001f;
            if (IsTurning)
            {
                transform.Rotate(0, yaw, 0, Space.World);
                _cameraRig.FollowPlayerTurn(transform.eulerAngles.y);
            }
            float throttle = Mathf.Abs(input.y) > .1f ? input.y : 0f;
            float speed = IsCrouching ? 1.25f : aiming ? 1.5f : IsSprintHeld() && throttle > 0 ? sprintSpeed : walkSpeed;
            if (throttle < 0) speed = Mathf.Min(speed, 1.25f);
            Vector3 movement = transform.forward * (throttle * speed);

            if (_characterController.isGrounded && _verticalVelocity.y < 0f)
            {
                _verticalVelocity.y = -2f;
            }
            if(_characterController.isGrounded)_lastGrounded=Time.time;
            if(IsJumpPressed())_jumpQueued=Time.time;
            if (Time.time-_jumpQueued<.12f && Time.time-_lastGrounded<.10f && _verticalVelocity.y<=0 && TryStand())
            {
                _verticalVelocity.y = jumpVelocity;
                _jumpQueued=-100;_lastGrounded=-100;
            }
            float forwardSpeed = Mathf.MoveTowards(Vector3.Dot(_smoothMovement, transform.forward),
                Vector3.Dot(movement, transform.forward), (movement.sqrMagnitude > .01f ? 9 : 24) * Time.deltaTime);
            _smoothMovement = transform.forward * forwardSpeed;
            if(IsSliding)_smoothMovement=_slideDirection*Mathf.Lerp(2.4f,7f,Mathf.Clamp01((_slideUntil-Time.time)/.7f));
            _verticalVelocity.y += Physics.gravity.y * Time.deltaTime;
            _lastWallNormal=Vector3.zero;
            Vector3 horizontalStep=ResolveWallSlide(_smoothMovement*Time.deltaTime);
            var collisions=_characterController.Move(horizontalStep + _verticalVelocity*Time.deltaTime);
            if((collisions&CollisionFlags.Above)!=0&&_verticalVelocity.y>0)_verticalVelocity.y=0;
            if(IsSliding&&(collisions&CollisionFlags.Sides)!=0)_slideUntil=0;
            Vector3 travelled = transform.position - start;
            RecoverFromWallIfBlocked(input,collisions,travelled);
            travelled = transform.position - start;
            CurrentSpeed = new Vector2(travelled.x, travelled.z).magnitude / Mathf.Max(Time.deltaTime, .0001f);

            if (WasInteractPressed() && !VenueInterior.TryInteract() && !(_mission?.TryInteract() ?? false) && !CityVenue.TryInteract(this)) TryBoardNearest(false);
            if (MobileControlState.ConsumeRide() || (Keyboard.current != null && Keyboard.current.rKey.wasPressedThisFrame)) TryBoardNearest(true);
        }

        private void UpdateDriving()
        {
            if(IsDriving) _vehicle.SetDrivingInput(ReadMoveInput(), IsBrakeHeld());
            CurrentSpeed=_vehicle.SpeedKph/3.6f;
            if(WasInteractPressed() && !(CityTimeTrial.Instance?.TryInteract(this)??false)) ExitVehicle();
        }

        public bool TryBoardNearest(bool passenger)
        {
            if(IsInVehicle || InputLocked || !TryStand())return false;
            VehicleController nearest=null;
            float distance=3.6f;
            foreach(var candidate in FindObjectsByType<VehicleController>(FindObjectsSortMode.None))
            {
                float d=Vector3.Distance(transform.position,candidate.transform.position);
                if(d<distance){distance=d;nearest=candidate;}
            }
            if(nearest==null){VehicleHint="Move beside a car: USE drives, RIDE is passenger";return false;}
            if (_mission != null && !_mission.CanEnterVehicle(nearest)) return false;
            if(!nearest.TryEnter(this,passenger)){VehicleHint="Wait for the car to stop or choose a free seat";return false;}
            _vehicle=nearest;
            CurrentSpeed=0; _verticalVelocity=Vector3.zero; MobileControlState.ConsumeJump();
            _characterController.enabled=false;
            _onFootParent=transform.parent;
            transform.SetParent(passenger?nearest.PassengerSeat:nearest.DriverSeat,false);
            transform.localPosition=Vector3.zero; transform.localRotation=Quaternion.identity;
            _avatar.SetVisible(true);
            GetComponentInChildren<RuntimeCharacterVisual>()?.SetSeated(true,!passenger);
            _cameraRig.SetTarget(nearest.CameraTarget,true);
            if(!passenger && nearest==HarareAfterHoursBootstrap.Instance?.FeaturedVehicle) _mission?.OnVehicleEntered();
            VehicleHint=passenger?"Passenger: USE requests a stop / exits":"Drive: hold ACCEL, use ◀ / ▶ to steer, BRAKE stops, USE exits";
            _bridge?.Publish("vehicle_entered",passenger?"{\"seat\":\"passenger\"}":"{\"seat\":\"driver\"}");
            return true;
        }

        private void EnterVehicle()=>TryBoardNearest(false);

        public bool TryExitVehicle()
        {
            if(!IsInVehicle)return false;
            if(!_vehicle.TryGetExitPosition(out var exitPosition))
            {
                if(IsPassenger)_vehicle.GetComponent<TrafficVehicle>()?.RequestStop();
                VehicleHint=_vehicle.SpeedKph>3?"Stop before exiting — passenger stop requested":"Doorway blocked; move to a clear area";
                return false;
            }
            CurrentSpeed=0; _verticalVelocity=Vector3.zero; MobileControlState.ConsumeJump();
            _vehicle.Exit(this);
            transform.SetParent(_onFootParent,true);
            transform.position=exitPosition;
            transform.rotation=Quaternion.Euler(0,transform.eulerAngles.y,0);
            GetComponentInChildren<RuntimeCharacterVisual>()?.SetSeated(false,false);
            _characterController.enabled=true; _avatar.SetVisible(true);
            _cameraRig.SetTarget(transform,false);
            VehicleHint="USE drives; RIDE enters as passenger";
            _bridge?.Publish("vehicle_exited","{}");
            return true;
        }
        private void ExitVehicle()=>TryExitVehicle();

        public void ApplyProfile(string profileJson)
        {
            _avatar.ApplyProfileJson(profileJson);
        }

        public void ApplyPortrait(string encodedPhoto)
        {
            _avatar.ApplyPhotoBase64(encodedPhoto);
        }

        private Vector3 ResolveWallSlide(Vector3 displacement)
        {
            if(displacement.sqrMagnitude<.000001f)return displacement;
            float radius=Mathf.Max(.02f,_characterController.radius-_characterController.skinWidth*.3f);
            Vector3 center=transform.TransformPoint(_characterController.center);
            float halfHeight=Mathf.Max(0f,_characterController.height*.5f-radius);
            Vector3 bottom=center-Vector3.up*halfHeight,top=center+Vector3.up*halfHeight;
            float distance=displacement.magnitude;
            int count=Physics.CapsuleCastNonAlloc(bottom,top,radius,displacement/distance,_wallHits,distance+.04f,~0,QueryTriggerInteraction.Ignore);
            float nearest=float.MaxValue;Vector3 normal=Vector3.zero;
            for(int index=0;index<count;index++)
            {
                RaycastHit hit=_wallHits[index];
                if(hit.collider==null||hit.collider==_characterController||hit.transform.IsChildOf(transform)||hit.normal.y>.55f)continue;
                if(Vector3.Dot(displacement,hit.normal)>=0||hit.distance>=nearest)continue;
                nearest=hit.distance;normal=Vector3.ProjectOnPlane(hit.normal,Vector3.up).normalized;
            }
            if(normal.sqrMagnitude<.001f)return displacement;
            _lastWallNormal=normal;
            return Vector3.ProjectOnPlane(displacement,normal);
        }

        private void OnControllerColliderHit(ControllerColliderHit hit)
        {
            Vector3 normal=Vector3.ProjectOnPlane(hit.normal,Vector3.up);
            if(normal.sqrMagnitude>.001f&&Vector3.Dot(hit.moveDirection,normal)<0)_lastWallNormal=normal.normalized;
        }

        private void RecoverFromWallIfBlocked(Vector2 input,CollisionFlags collisions,Vector3 travelled)
        {
            if((collisions&CollisionFlags.Sides)==0||input.sqrMagnitude<.01f||_lastWallNormal.sqrMagnitude<.001f)
            {
                _blockedSince=-1f;return;
            }
            if(Vector3.Dot(_smoothMovement,_lastWallNormal)<0)_smoothMovement=Vector3.ProjectOnPlane(_smoothMovement,_lastWallNormal);
            float planarTravel=new Vector2(travelled.x,travelled.z).magnitude;
            if(planarTravel>.015f){_blockedSince=-1f;return;}
            if(_blockedSince<0){_blockedSince=Time.time;return;}
            if(Time.time-_blockedSince<.28f||Time.time-_lastUnstick<.45f)return;
            _characterController.Move(_lastWallNormal*.08f);
            _lastUnstick=Time.time;_blockedSince=Time.time;
        }

        private static Vector2 ReadMoveInput()
        {
            Vector2 arrows=Vector2.zero;
            if (Keyboard.current != null)
            {
                if(Keyboard.current.upArrowKey.isPressed)arrows.y+=1f;
                if(Keyboard.current.downArrowKey.isPressed)arrows.y-=1f;
                if(Keyboard.current.leftArrowKey.isPressed)arrows.x-=1f;
                if(Keyboard.current.rightArrowKey.isPressed)arrows.x+=1f;
            }
            // A keyboard direction always wins over a stale touch/gamepad value.
            if(arrows.sqrMagnitude>.001f)return Vector2.ClampMagnitude(arrows,1f);
            Vector2 input = MobileControlState.Move;
            if (Gamepad.current != null) input += Gamepad.current.leftStick.ReadValue();
            return Vector2.ClampMagnitude(input, 1f);
        }

        private static bool IsSprintHeld()
        {
            return MobileControlState.Sprint || (Keyboard.current != null && Keyboard.current.leftShiftKey.isPressed) ||
                   (Gamepad.current != null && Gamepad.current.leftStickButton.isPressed);
        }

        private static bool IsJumpPressed()
        {
            return MobileControlState.ConsumeJump() ||
                   (Keyboard.current != null && Keyboard.current.spaceKey.wasPressedThisFrame) ||
                   (Gamepad.current != null && Gamepad.current.buttonNorth.wasPressedThisFrame);
        }

        private static bool WasInteractPressed()
        {
            return MobileControlState.ConsumeInteract() ||
                   (Keyboard.current != null && Keyboard.current.eKey.wasPressedThisFrame) ||
                   (Gamepad.current != null && Gamepad.current.buttonSouth.wasPressedThisFrame);
        }

        private static bool IsBrakeHeld()
        {
            return MobileControlState.Brake ||
                   (Keyboard.current != null && Keyboard.current.spaceKey.isPressed) ||
                   (Gamepad.current != null && Gamepad.current.leftTrigger.isPressed);
        }
    }
}
