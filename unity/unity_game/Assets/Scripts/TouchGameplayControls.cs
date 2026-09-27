using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using EnhancedTouch = UnityEngine.InputSystem.EnhancedTouch.Touch;
using EnhancedTouchSupport = UnityEngine.InputSystem.EnhancedTouch.EnhancedTouchSupport;

namespace HarareAfterHours
{
    /// <summary>Finger-owned analog movement, camera gestures and contextual actions.</summary>
    [DefaultExecutionOrder(-100)]
    public sealed partial class TouchGameplayControls : MonoBehaviour
    {
        public static TouchGameplayControls Instance { get; private set; }
        public static bool Active => Instance != null && Instance.isActiveAndEnabled;
        public enum Action { None, Move, Up, Down, Left, Right, Look, Attack, Fight, Jump, Use, Ride, View, Go, Brake, Reverse, Stop, Kick, Reload, SwitchWeapon, Perspective, Crouch, Sprint, SettingsMenu }
        private sealed class Pointer { public Action action; public Vector2 position, origin; }
        private readonly Dictionary<int,Pointer> _pointers = new();
        private readonly List<int> _released = new();
        private ThirdPersonController _player;
        private MissionDirector _mission;
        private PlayerCombat _combat;
        private GUIStyle _label;
        private int _touchFrame=-100;
        private bool _wasVehicle, _wasPassenger;
        private Vector2 _stick;
        private Vector2 _origin;
        private bool _sprinting;
        private Vector2 _screen;
        private Rect _safe;
        private bool _focused=true;
        private bool _sentHeld;
        public float Scale => Mathf.Clamp(Mathf.Min(Screen.width,Screen.height)/540f,.85f,2.4f);
        public Rect Safe => new(Screen.safeArea.xMin,Screen.height-Screen.safeArea.yMax,Screen.safeArea.width,Screen.safeArea.height);
        // Four fixed direction buttons are much easier to control than a drag
        // stick. The center remains a small legacy analog zone for saved layouts.
        private float DpadTile => 52*Scale*ControlSize(Action.Move);
        public float Radius => DpadTile*1.5f;
        public Vector2 StickHome => PositionFor(Action.Move,new Vector2(Safe.xMin+Radius+24*Scale,Safe.yMax-Radius-70*Scale));
        public Rect MoveArea => new(StickHome.x-Radius,StickHome.y-Radius,Radius*2,Radius*2);
        public Rect ButtonRect(Action action)
        {
            if(IsDirection(action))return DirectionRect(action);
            Rect rect=DefaultButtonRect(action);var center=PositionFor(action,rect.center);rect.size*=ControlSize(action);rect.center=center;return rect;
        }
        private Rect DirectionRect(Action action)
        {
            float tile=DpadTile;Vector2 home=StickHome;
            return action switch
            {
                Action.Up => new Rect(home.x-tile*.5f,home.y-tile*1.5f,tile,tile),
                Action.Down => new Rect(home.x-tile*.5f,home.y+tile*.5f,tile,tile),
                Action.Left => new Rect(home.x-tile*1.5f,home.y-tile*.5f,tile,tile),
                Action.Right => new Rect(home.x+tile*.5f,home.y-tile*.5f,tile,tile),
                _ => new Rect(),
            };
        }
        private static bool IsDirection(Action action)=>action is Action.Up or Action.Down or Action.Left or Action.Right;
        private static Vector2 DirectionFor(Action action)=>action switch
        {
            Action.Up => Vector2.up,Action.Down => Vector2.down,Action.Left => Vector2.left,Action.Right => Vector2.right,_ => Vector2.zero,
        };
        private Rect DefaultButtonRect(Action action)
        {
            if(!_player.IsInVehicle)
            {
                float u=Scale,tile=46*u,g=10*u,x=Safe.xMax-12*u-tile,y=Safe.yMax-54*u-tile;
                if(action==Action.SettingsMenu)return new Rect(Safe.center.x-24*u,Safe.yMin+8*u,48*u,36*u);
                if(action==Action.Perspective)return new Rect(Safe.xMin+12*u,Safe.yMin+60*u,64*u,32*u);
                if(action==Action.SwitchWeapon)return new Rect(Safe.center.x-65*u,Safe.yMax-48*u,130*u,32*u);
                if(action==Action.Sprint)return new Rect(StickHome.x-tile*.5f,StickHome.y-Radius-tile-12*u,tile,tile);
                int c=action is Action.Attack or Action.Reload or Action.Ride or Action.Use?1:0;
                int r=action switch{Action.Crouch=>0,Action.Kick=>1,Action.Fight=>2,Action.Jump=>3,Action.View=>4,Action.Reload=>0,Action.Attack=>1,Action.Ride=>2,_=>3};
                return new Rect(x-c*(tile+g),y-r*(tile+g),tile,tile);
            }
            float s=Scale,b=60*s,gap=12*s,right=Safe.xMax-16*s-b,bottom=Safe.yMax-16*s-b;
            // Leave the Flutter host's top-right pause button clear on Retina displays.
            if(action==Action.View)return new Rect(Safe.xMax-76*s,Safe.yMin+88*s,60*s,44*s);
            if(action==Action.Reload)return new Rect(right,bottom-3*(b+gap),b,b);
            if(action==Action.Perspective)return new Rect(Safe.xMin+16*s,Safe.yMax-285*s,64*s,44*s);
            if(action==Action.SwitchWeapon)return new Rect(Safe.center.x-64*s,Safe.yMax-75*s,128*s,48*s);
            int col=action is Action.Attack or Action.Go or Action.Ride or Action.Reverse or Action.Kick ? 1:0;
            int row=action is Action.Jump or Action.Reverse or Action.Ride ? 2:action is Action.Attack or Action.Fight or Action.Brake or Action.Go or Action.Stop ? 1:0;
            return new Rect(right-col*(b+gap),bottom-row*(b+gap),b,b);
        }
        private readonly Action[] _foot={Action.Up,Action.Down,Action.Left,Action.Right,Action.Attack,Action.Fight,Action.Kick,Action.Jump,Action.Use,Action.Ride,Action.View,Action.Reload,Action.SwitchWeapon,Action.Perspective,Action.Crouch,Action.Sprint,Action.SettingsMenu};
        // Keep the D-pad for familiar steering, but show dedicated pedals so
        // a driver can steer and accelerate with separate thumbs.
        private readonly Action[] _driver={Action.Up,Action.Down,Action.Left,Action.Right,Action.Go,Action.Reverse,Action.Brake,Action.Use,Action.View};
        private readonly Action[] _passenger={Action.Stop,Action.Use,Action.View};
        private Action[] Actions => _player.IsPassenger?_passenger:_player.IsDriving?_driver:_foot;

        public void Configure(ThirdPersonController player,MissionDirector mission,PlayerCombat combat)
        { Instance=this;_player=player;_mission=mission;_combat=combat;LoadSettings();_origin=StickHome; }
        public static Vector2 Analog(Vector2 drag,float radius)
        {
            Vector2 v=drag/Mathf.Max(1,radius);float length=v.magnitude;
            if(length<=.13f)return Vector2.zero;
            return v.normalized*Mathf.Clamp01((length-.13f)/.87f);
        }
        public Action HitTest(Vector2 point)
        {
            foreach(var action in Actions)if(ButtonRect(action).Contains(point))return action;
            if(!_player.IsPassenger && MoveArea.Contains(point))return Action.Move;
            if(Safe.Contains(point)&&!MenuArea.Contains(point))return Action.Look;
            return Action.None;
        }
        public void ProcessPointer(int id,Vector2 position,Vector2 delta,bool began,bool held)
        {
            if(Editing||_player==null||_player.InputLocked||Time.timeScale<=0||!_focused){Cancel();return;}
            if(!held){_pointers.Remove(id);return;}
            if(began && !_pointers.ContainsKey(id))
            {
                Action action=HitTest(position);
                if(action==Action.Look)
                    foreach(var owned in _pointers.Values)if(owned.action==action)return;
                Vector2 origin=StickHome;
                _pointers[id]=new Pointer{action=action,position=position,origin=origin};
                if(action==Action.Move)_origin=origin;
                if(action is not Action.Move and not Action.Look)Trigger(action);
            }
            if(!_pointers.TryGetValue(id,out var pointer))return;
            if(!began&&delta.sqrMagnitude<.0001f)delta=position-pointer.position;
            // A thumb can glide between movement arrows without lifting. It cannot
            // cross into fire/use controls: only direction ownership is transferable.
            if (IsDirection(pointer.action))
            {
                Action next = HitTest(position);
                if (IsDirection(next)) pointer.action = next;
            }
            pointer.position=position;
            // Aim/fire drag belongs to this finger, never the movement stick.
            if(pointer.action==Action.Look || (pointer.action==Action.Attack && _combat.IsRanged))
                Camera.main?.GetComponent<ThirdPersonCameraRig>()?.ApplyTouchLook(Vector2.Scale(delta,new Vector2(1,_settings.invertY?-1:1))*(_settings.sensitivity*(_combat.IsAiming?70f:120f)/Mathf.Min(Screen.width,Screen.height)));
        }
        private void Trigger(Action action)
        {
            switch(action)
            {
                case Action.Attack:
                    if(_settings.aimOnFire && _combat.IsRanged && !_combat.IsAiming)_combat.ToggleAim();
                    MobileControlState.RequestPulse();break;
                case Action.Crouch:_player.ToggleCrouchSlide();break;
                case Action.Sprint:break; // Sprint is held, never latched.
                case Action.SettingsMenu:BeginEdit();break;
                case Action.Fight:MobileControlState.RequestFight();break;
                case Action.Kick:MobileControlState.RequestKick();break;
                case Action.Jump:MobileControlState.RequestJump();break;
                case Action.Use:MobileControlState.RequestInteract();break;
                case Action.Ride:MobileControlState.RequestRide();break;
                case Action.Reload:_combat.Reload();break;
                case Action.SwitchWeapon:_player.GetComponent<PlayerLoadout>()?.CycleWeapon();break;
                case Action.Perspective:Camera.main?.GetComponent<ThirdPersonCameraRig>()?.TogglePerspective();break;
                case Action.View:
                    if(!_player.IsInVehicle&&_combat.IsRanged)_combat.ToggleAim();
                    else Camera.main?.GetComponent<ThirdPersonCameraRig>()?.Recenter();
                    break;
                case Action.Stop:_player.CurrentVehicle?.GetComponent<TrafficVehicle>()?.RequestStop();break;
            }
        }
        public void ApplyHeld()
        {
            if(_player==null||_player.InputLocked||Time.timeScale<=0||!_focused){Cancel();return;}
            if(_pointers.Count==0)
            {
                if(_sentHeld)MobileControlState.ClearHeld();
                _sentHeld=false;_stick=Vector2.zero;_sprinting=false;_origin=StickHome;return;
            }
            _sentHeld=true;
            MobileControlState.ClearHeld();_stick=Vector2.zero;_sprinting=false;
            bool go=false,reverse=false,brake=false,hasStick=false,hasDpad=false,sprintHeld=false;Vector2 dpad=Vector2.zero;
            foreach(var pointer in _pointers.Values)
            {
                if(pointer.action==Action.Move)
                {
                    hasStick=true;_origin=pointer.origin;_stick=Analog(pointer.position-pointer.origin,Radius);
                    _stick.y=-_stick.y;_sprinting=!_player.IsInVehicle&&_stick.y>.35f&&(_settings.joystickSprint&&Vector2.Distance(pointer.position,pointer.origin)>Radius*1.05f);
                }
                if(IsDirection(pointer.action)&&ButtonRect(pointer.action).Contains(pointer.position))
                {
                    dpad+=DirectionFor(pointer.action);hasDpad=true;
                }
                // Fingers retain ownership; sliding into another button never fires it.
                if(pointer.action==Action.Attack && _combat.IsAutomatic)MobileControlState.RequestPulse();
                if(!ButtonRect(pointer.action).Contains(pointer.position))continue;
                sprintHeld|=pointer.action==Action.Sprint;
                go|=pointer.action==Action.Go;reverse|=pointer.action==Action.Reverse;brake|=pointer.action==Action.Brake;
            }
            if(!hasStick)_origin=StickHome;
            Vector2 move=hasDpad?Vector2.ClampMagnitude(dpad,1):_stick;
            if(_player.IsDriving&&go!=reverse)move.y=go?1:-1;
            if(_player.IsPassenger)move=Vector2.zero;
            _sprinting = !_player.IsInVehicle && move.sqrMagnitude > .01f && (_sprinting || sprintHeld);
            MobileControlState.SetMove(move);
            if(_sprinting)MobileControlState.SetSprint();
            if(brake)MobileControlState.SetBrake();
        }
        private void Update()
        {
            if(_player==null)return;
            if(Editing){Cancel();UpdateEditorTouch();return;}
            Vector2 size=new(Screen.width,Screen.height);
            if(size!=_screen||_safe!=Screen.safeArea||_wasVehicle!=_player.IsInVehicle||_wasPassenger!=_player.IsPassenger)
            {Cancel();_screen=size;_safe=Screen.safeArea;_wasVehicle=_player.IsInVehicle;_wasPassenger=_player.IsPassenger;}
            if(!_focused||_player.InputLocked||Time.timeScale<=0){Cancel();return;}
            // Enhanced Touch retains each active contact across input updates.
            // Direct Touchscreen polling can miss a held iOS contact between
            // updates, which left the car without a sustained throttle/steer.
            _released.Clear();foreach(var pair in _pointers)if(pair.Key>=0)_released.Add(pair.Key);
            foreach(var touch in EnhancedTouch.activeTouches)
            {
                if(!touch.inProgress)continue;
                _released.Remove(touch.touchId);
                _touchFrame=Time.frameCount;
                Vector2 p=touch.screenPosition,d=touch.delta;p.y=Screen.height-p.y;d.y=-d.y;
                ProcessPointer(touch.touchId,p,d,touch.began,true);
            }
            foreach(int id in _released)_pointers.Remove(id);
            ApplyHeld();
        }
        public void Cancel(){_pointers.Clear();_stick=Vector2.zero;_sprinting=false;_sentHeld=false;MobileControlState.Reset();_combat?.ClearAim();}
        private void OnApplicationFocus(bool focus){_focused=focus;if(!focus)Cancel();}
        private void OnApplicationPause(bool pause){if(pause)Cancel();}
        private void OnEnable(){Instance=this;EnhancedTouchSupport.Enable();}
        private void OnDisable(){Cancel();EnhancedTouchSupport.Disable();if(Instance==this)Instance=null;}
        private string Label(Action action)=>action switch
        {
            Action.Up=>"▲\nWALK",Action.Down=>"▼\nBACK",Action.Left=>"◀\nTURN",Action.Right=>"▶\nTURN",
            Action.Attack=>_combat.IsAutomatic?"HOLD\nFIRE":_combat.AttackLabel,
            Action.Fight=>"PUNCH",
            Action.Crouch=>_player.IsSliding?"SLIDE":_player.IsCrouching?"STAND":"CROUCH",
            Action.Sprint=>_sprinting?"RUNNING":"HOLD\nSPRINT",
            Action.SettingsMenu=>"SETUP",
            Action.Reload=>_combat.Reloading?"LOADING": "RELOAD",
            Action.SwitchWeapon=>_combat.IsRanged?$"{_combat.WeaponLabel}\n{_combat.Ammo}/{_combat.MagazineSize} · TAP":$"{_combat.WeaponLabel}\nTAP FOR GUN",
            Action.Perspective=>Camera.main?.GetComponent<ThirdPersonCameraRig>()?.CameraModeLabel??"PLAYER\nVIEW",
            Action.View=>!_player.IsInVehicle&&_combat.IsRanged?(_combat.IsAiming?"AIM ON":"AIM"):"CAMERA",
            Action.Use=>VenueInterior.Active?.ActionLabel??CityTimeTrial.Instance?.ActionLabel??(_player.IsInVehicle?"EXIT":_mission.CanInteract?_mission.ActionLabel:CityVenue.Nearest(_player)!=null?"ENTER":"USE"),
            Action.Go=>"ACCEL",Action.Reverse=>"REV",_=>action.ToString().ToUpperInvariant()
        };
        private void OnGUI()
        {
            if(Editing){DrawEditor();return;}
            if(_player==null||_player.InputLocked||Time.timeScale<=0)return;
            _label??=new GUIStyle(GUI.skin.label){alignment=TextAnchor.MiddleCenter,fontStyle=FontStyle.Bold,normal={textColor=Color.white}};
            _label.fontSize=Mathf.RoundToInt(12*Scale);
            var e=Event.current;
            if(Time.frameCount-_touchFrame>2 && e.button==0)
            {
                int control=GUIUtility.GetControlID(FocusType.Passive);
                if(e.type==EventType.MouseDown)
                {
                    ProcessPointer(-1,e.mousePosition,Vector2.zero,true,true);
                    if(_pointers.TryGetValue(-1,out var pointer)&&pointer.action!=Action.None){GUIUtility.hotControl=control;e.Use();}
                }
                if(e.type==EventType.MouseDrag&&GUIUtility.hotControl==control){ProcessPointer(-1,e.mousePosition,e.delta,false,true);e.Use();}
                if(e.type==EventType.MouseUp){ProcessPointer(-1,e.mousePosition,Vector2.zero,false,false);if(GUIUtility.hotControl==control){GUIUtility.hotControl=0;e.Use();}}
            }
            DrawControlBackdrop();
            foreach(var action in Actions)
            {
                Rect rect=ButtonRect(action);bool pressed=false;
                foreach(var pointer in _pointers.Values)pressed|=pointer.action==action&&rect.Contains(pointer.position);
                DrawControl(action,rect,pressed);
            }
        }
        private static void Draw(Rect rect,Color color,float radius)=>GUI.DrawTexture(rect,Texture2D.whiteTexture,ScaleMode.StretchToFill,true,0,color,0,radius);
    }
}
