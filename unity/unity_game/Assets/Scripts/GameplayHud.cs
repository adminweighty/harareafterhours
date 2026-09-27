using UnityEngine;
using UnityEngine.InputSystem;

namespace HarareAfterHours
{
    /// <summary>
    /// A compact gameplay HUD that intentionally lives in Unity: it remains
    /// visible whether the Flutter host uses the game full-screen or embedded.
    /// It also supplies touch controls until the production control skin lands.
    /// </summary>
    public sealed class GameplayHud : MonoBehaviour
    {
        private ThirdPersonController _player;
        private VehicleController _vehicle;
        private MissionDirector _mission;
        private PlayerProgression _progression;
        private PlayerCombat _combat;
        private GUIStyle _titleStyle;
        private GUIStyle _bodyStyle;
        private GUIStyle _buttonStyle;

        public void Configure(
            ThirdPersonController player,
            VehicleController vehicle,
            MissionDirector mission,
            PlayerProgression progression,
            PlayerCombat combat)
        {
            _player = player;
            _vehicle = vehicle;
            _mission = mission;
            _progression = progression;
            _combat = combat;
            var touch = GetComponent<TouchGameplayControls>();
            if(touch==null)touch=gameObject.AddComponent<TouchGameplayControls>();
            touch.Configure(player,mission,combat);
        }

        private void OnGUI()
        {
            if (_player == null || _mission == null) return;
            if(TouchGameplayControls.Instance?.Editing==true)return;
            EnsureStyles();
            if(TouchGameplayControls.Active)
            {
                DrawCompactHud();
                DrawShooterHud();
                DrawHostileLabels();
                DrawWeaponReticle(TouchGameplayControls.Instance.Scale);
                return;
            }

            float scale = Mathf.Max(1f, Mathf.Min(Screen.width, Screen.height) / 390f);
            Rect safe = Screen.safeArea;
            float left = safe.xMin + 12f * scale;
            float top = Screen.height - safe.yMax + 12f * scale;
            float width = Mathf.Min(380f * scale, safe.width - 84f * scale);
            _titleStyle.fontSize = Mathf.RoundToInt(24f * scale);
            _bodyStyle.fontSize = Mathf.RoundToInt(16f * scale);
            GUI.Box(new Rect(left, top, width, 112f * scale), GUIContent.none);
            string points = $"{(_progression != null ? _progression.Points : 0):N0} PTS";
            if (_progression != null && _progression.Combo > 1) points += $"   x{_progression.Combo}";
            GUI.Label(new Rect(left + 12f * scale, top + 4f * scale, width - 24f * scale, 32f * scale), points, _titleStyle);
            string objective = _mission.ObjectiveText;
            if (_mission.State != MissionState.Complete) objective += $"  ·  {_mission.DistanceMetres} m";
            GUI.Label(new Rect(left + 12f * scale, top + 38f * scale, width - 24f * scale, 46f * scale), objective, _bodyStyle);
            _bodyStyle.fontSize = Mathf.RoundToInt(14f * scale);
            string hint = _player.IsInVehicle
                ? $"{Mathf.RoundToInt(_player.CurrentVehicle.SpeedKph)} km/h  ·  {_mission.ControlHint}"
                : _combat.IsRanged ? $"{_combat.EquippedWeapon} · drag to aim" : $"{_combat.EquippedWeapon}  ·  {_mission.ControlHint}";
            hint = CityVenue.Hint(_player) ?? hint;
            GUI.Label(new Rect(left + 12f * scale, top + 84f * scale, width - 24f * scale, 24f * scale), hint, _bodyStyle);
            if (!TouchGameplayControls.Active && Button(CameraButton(), "VIEW")) Camera.main?.GetComponent<ThirdPersonCameraRig>()?.Recenter();

            var street=StreetActionDirector.Instance;
            if(street!=null)
            {
                Rect status=new(left,top+194*scale,Mathf.Min(390*scale,safe.width-24*scale),76*scale);
                GUI.Box(status,GUIContent.none);
                GUI.Label(new Rect(status.x+8*scale,status.y+4*scale,status.width-16*scale,24*scale),street.Status,_bodyStyle);
                string action=street.Capture>0?"ARREST "+Mathf.RoundToInt(street.Capture/3*100)+"% — RUN or FIGHT":street.Notice;
                GUI.Label(new Rect(status.x+8*scale,status.y+28*scale,status.width-16*scale,46*scale),action,_bodyStyle);
            }

            if (!string.IsNullOrEmpty(_mission.Notice))
            {
                float noticeWidth = Mathf.Min(500f * scale, safe.width - 24f * scale);
                Rect noticeRect = new(left, top + 122f * scale, noticeWidth, 66f * scale);
                GUI.Box(noticeRect, GUIContent.none);
                _bodyStyle.fontSize = Mathf.RoundToInt(16f * scale);
                GUI.Label(new Rect(noticeRect.x + 12f * scale, noticeRect.y + 8f * scale,
                    noticeRect.width - 24f * scale, noticeRect.height - 16f * scale), _mission.Notice, _bodyStyle);
            }

            if(!TouchGameplayControls.Active)DrawMobileControls();
            DrawWeaponReticle(scale);
        }

        private void DrawWeaponReticle(float scale)
        {
            if (!_combat.IsRanged || _player.IsInVehicle || _player.InputLocked || Time.timeScale <= 0) return;
            var feedback = _player.GetComponent<WeaponFeedback>();
            // A passive aim indicator, never an input zone or movement stick.
            bool hit = feedback != null && feedback.HitConfirmed;
            float kick = feedback == null ? 0 : Mathf.Clamp01(1-(Time.time-feedback.LastShotTime)/.18f);
            float gap = (hit ? 4 : 7 + kick*4) * scale;
            float x=Screen.width*.5f, y=Screen.height*.5f, length=6*scale, thickness=1.5f*scale;
            Color previous = GUI.color;
            GUI.color = hit ? new Color(1,.7f,.2f) : new Color(.9f,.95f,1,.85f);
            GUI.DrawTexture(new Rect(x-gap-length,y-thickness/2,length,thickness),Texture2D.whiteTexture);
            GUI.DrawTexture(new Rect(x+gap,y-thickness/2,length,thickness),Texture2D.whiteTexture);
            GUI.DrawTexture(new Rect(x-thickness/2,y-gap-length,thickness,length),Texture2D.whiteTexture);
            GUI.DrawTexture(new Rect(x-thickness/2,y+gap,thickness,length),Texture2D.whiteTexture);
            if (hit) GUI.DrawTexture(new Rect(x-2*scale,y-2*scale,4*scale,4*scale),Texture2D.whiteTexture);
            GUI.color = previous;
        }

        private void DrawHostileLabels()
        {
            var world=StreetActionDirector.Instance;var camera=Camera.main;
            if(world==null||camera==null||world.GameOver)return;
            float s=TouchGameplayControls.Instance.Scale;Rect safe=TouchGameplayControls.Instance.Safe;
            foreach(var actor in world.Actors)
            {
                if(actor.Officer||actor.Down||!actor.isActiveAndEnabled)continue;
                float distance=Vector3.Distance(_player.transform.position,actor.transform.position);
                Vector3 screen=camera.WorldToScreenPoint(actor.transform.position+Vector3.up*2.9f);
                bool behind=screen.z<=0;
                if(behind){screen.x=Screen.width-screen.x;screen.y=Screen.height-screen.y;}
                float halfWidth=78*s;
                float x=Mathf.Clamp(screen.x,safe.xMin+halfWidth,safe.xMax-halfWidth);
                float y=Mathf.Clamp(Screen.height-screen.y-14*s,safe.yMin+8*s,safe.yMax-38*s);
                bool edge=behind||Mathf.Abs(x-screen.x)>1||Mathf.Abs(y-(Screen.height-screen.y-14*s))>1;
                if (behind)
                {
                    x = Vector3.Dot(camera.transform.right, actor.transform.position-camera.transform.position) < 0
                        ? safe.xMin+halfWidth : safe.xMax-halfWidth;
                    y = safe.center.y;
                }
                bool covered = actor.GetComponent<HostileIndicator>()?.HasClearView == false;
                Rect label=new(x-halfWidth,y,156*s,28*s);
                Color previous=GUI.color;GUI.color=new Color(.3f,.04f,.025f,.9f);GUI.DrawTexture(label,Texture2D.whiteTexture);GUI.color=previous;
                var style=new GUIStyle(_bodyStyle){alignment=TextAnchor.MiddleCenter,fontSize=Mathf.RoundToInt(12*s)};
                string threat=actor.WindingUp||actor.GetComponent<HostileFirearm>()?.Aiming==true
                    ?"! INCOMING · TAKE COVER !"
                    :(edge ? "OFFSCREEN" : covered ? "BEHIND COVER" : distance > 80 ? "DISTANT THIEF" : actor.Armed ? "ARMED THIEF" : "THIEF")+" · "+Mathf.RoundToInt(distance)+" m";
                GUI.Label(label,threat,style);
            }
        }

        private void DrawShooterHud()
        {
            float s=TouchGameplayControls.Instance.Scale;Rect safe=TouchGameplayControls.Instance.Safe;
            var street=StreetActionDirector.Instance;var previous=GUI.color;
            _bodyStyle.fontSize=Mathf.RoundToInt(11*s);
            var centered=new GUIStyle(_bodyStyle){alignment=TextAnchor.MiddleCenter};
            float width=Mathf.Min(safe.width*.5f,360*s);
            if(_player.IsDriving&&string.IsNullOrEmpty(street?.Notice))
            {
                var driving=new GUIStyle(centered){fontSize=Mathf.RoundToInt(9*s),normal={textColor=new Color(.92f,.96f,1,.96f)}};
                GUI.Label(new Rect(safe.center.x-width/2,safe.yMin+12*s,width,34*s),"HOLD ACCEL  ·  ◀  ▶ STEER\nBRAKE STOPS  ·  REV BACKS UP",driving);
            }
            if(street==null)return;
            float healthWidth=Mathf.Min(220*s,safe.width*.46f);
            var health=new Rect(safe.center.x-healthWidth/2,safe.yMax-11*s,healthWidth,5*s);
            GUI.color=new Color(.1f,.1f,.1f,.8f);GUI.DrawTexture(health,Texture2D.whiteTexture);
            GUI.color=street.Health<=25?new Color(1,.25f,.15f):Color.white;
            GUI.DrawTexture(new Rect(health.x,health.y,health.width*street.Health/100,health.height),Texture2D.whiteTexture);GUI.color=previous;
            GUI.Label(new Rect(health.x-48*s,health.y-8*s,48*s,18*s),$"HP {Mathf.CeilToInt(street.Health)}",_bodyStyle);
            if(street.RecoveringFromHit)
            {
                GUI.color=new Color(.7f,.015f,.015f,.23f);
                GUI.DrawTexture(new Rect(safe.xMin,safe.yMin,5*s,safe.height),Texture2D.whiteTexture);
                GUI.DrawTexture(new Rect(safe.xMax-5*s,safe.yMin,5*s,safe.height),Texture2D.whiteTexture);GUI.color=previous;
            }
            if(!string.IsNullOrEmpty(street.Notice))GUI.Label(new Rect(safe.center.x-width/2,safe.yMin+58*s,width,44*s),street.Notice,centered);
            if(street.TakedownActive)
            {
                float fade=Mathf.Clamp01((street.TakedownUntil-Time.time)/1.15f);
                Rect cue=new(safe.center.x-104*s,safe.yMin+safe.height*.29f,208*s,42*s);
                GUI.color=new Color(.42f,.01f,.018f,.76f*fade);GUI.DrawTexture(cue,Texture2D.whiteTexture);
                GUI.color=Color.white;
                var takedownStyle=new GUIStyle(centered){fontSize=Mathf.RoundToInt(17*s),fontStyle=FontStyle.Bold};
                GUI.Label(cue,street.TakedownText,takedownStyle);
                GUI.color=previous;
            }
        }

        private void DrawCompactHud()
        {
            float s=TouchGameplayControls.Instance.Scale;
            Rect safe=TouchGameplayControls.Instance.Safe;
            float x=safe.xMin+12*s,y=safe.yMin+10*s,w=Mathf.Min(340*s,safe.width*.68f);
            Color previous=GUI.color;
            GUI.color=new Color(.035f,.06f,.09f,.83f);GUI.DrawTexture(new Rect(x,y,w,84*s),Texture2D.whiteTexture);GUI.color=previous;
            _titleStyle.fontSize=Mathf.RoundToInt(16*s);_bodyStyle.fontSize=Mathf.RoundToInt(12*s);
            string status=_player.IsInVehicle?$"{Mathf.RoundToInt(_player.CurrentVehicle.SpeedKph)} km/h":_combat.IsRanged?$"{_combat.WeaponLabel} · {_combat.Ammo}/{_combat.MagazineSize}":_combat.WeaponLabel;
            GUI.Label(new Rect(x+10*s,y+5*s,w-20*s,22*s),$"{_progression.Points:N0} PTS     {status}",_titleStyle);
            string objective=_mission.ObjectiveText+(_mission.JoinaWelcome||_mission.State!=MissionState.Complete?$" · {_mission.DistanceMetres} m":"");
            objective=CityTimeTrial.Instance?.Objective??objective;
            objective=VenueInterior.Active?.Objective??objective;
            GUI.Label(new Rect(x+10*s,y+29*s,w-20*s,32*s),objective,_bodyStyle);
            string hint=CityVenue.Hint(_player)??(_mission.CanInteract?$"TAP {_mission.ActionLabel} · {_mission.ControlHint}":_player.IsPassenger?"STOP requests a safe drop-off":_player.IsDriving?"◀ / ▶ steer · hold ACCEL or REV":"▲ walk · ▼ back · ◀ ▶ turn · hold SPRINT to run");
            hint=CityTimeTrial.Instance?.Hint??hint;
            if(StreetActionDirector.Instance!=null&&!_player.IsInVehicle)hint=StreetActionDirector.Instance.CombatHint;
            if(_mission.JoinaWelcome && !_player.IsInVehicle && (StreetActionDirector.Instance?.Wanted ?? 0)==0)
                hint=_mission.CanTalkAtJoina?"TAP TALK · meet your city contact":"Move to the gold marker · swipe to look";
            hint=VenueInterior.Active?.Hint??hint;
            if(VenueInterior.Active==null&&CityVenue.Nearest(_player)!=null)hint=CityVenue.Hint(_player);
            GUI.Label(new Rect(x+10*s,y+61*s,w-20*s,22*s),hint,_bodyStyle);
            var street=StreetActionDirector.Instance;
            if(street!=null)
            {
                GUI.color=new Color(.035f,.06f,.09f,.75f);GUI.DrawTexture(new Rect(x,y+88*s,w,45*s),Texture2D.whiteTexture);GUI.color=previous;
                string lifeState=street.GameOver?"GAME OVER":street.Arrested?"CUFFED":street.RecoveringFromHit?"HIT BACK NOW":street.Wanted>0?"ZRP "+new string('*',street.Wanted):"FREE ROAM";
                GUI.Label(new Rect(x+10*s,y+89*s,w-20*s,24*s),$"LIFE {Mathf.CeilToInt(street.Health)}/100 · {lifeState}",_bodyStyle);
                Rect life=new(x+10*s,y+116*s,w-20*s,9*s);
                GUI.color=new Color(.18f,.2f,.22f);GUI.DrawTexture(life,Texture2D.whiteTexture);
                GUI.color=street.Health<=25?new Color(.95f,.35f,.22f):new Color(.25f,.8f,.48f);
                GUI.DrawTexture(new Rect(life.x,life.y,life.width*Mathf.Clamp01(street.Health/100),life.height),Texture2D.whiteTexture);GUI.color=previous;
            }
            string notice=street!=null&&street.Capture>0?"ARREST · RUN or FIGHT to break free":!string.IsNullOrEmpty(street?.Notice)?street.Notice:_mission.Notice;
            if(!string.IsNullOrEmpty(notice))
            {
                GUI.color=new Color(.035f,.06f,.09f,.75f);GUI.DrawTexture(new Rect(x,y+137*s,w,36*s),Texture2D.whiteTexture);GUI.color=previous;
                GUI.Label(new Rect(x+10*s,y+139*s,w-20*s,32*s),notice,_bodyStyle);
            }
        }

        private void DrawMobileControls()
        {
            float size = Mathf.Clamp(Screen.width * 0.12f, 58f, 160f);
            float margin = 18f;
            float bottom = Screen.height - size - margin - Screen.safeArea.y;
            float top = bottom - size;
            float left = margin;
            float right = Screen.width - margin - size;

            if (Repeat(new Rect(left + size, top, size, size), "▲")) MobileControlState.SetMove(Vector2.up);
            if (Repeat(new Rect(left, bottom, size, size), "◀")) MobileControlState.SetMove(Vector2.left);
            if (Repeat(new Rect(left + size * 2f, bottom, size, size), "▶")) MobileControlState.SetMove(Vector2.right);
            if (Repeat(new Rect(left + size, bottom, size, size), "▼")) MobileControlState.SetMove(Vector2.down);

            if (_player.IsInVehicle)
            {
                if (Repeat(new Rect(right - size, top, size, size), _player.IsPassenger?"STOP":"BRAKE"))
                { if(_player.IsPassenger)_player.CurrentVehicle.GetComponent<TrafficVehicle>()?.RequestStop(); else MobileControlState.SetBrake(); }
                if (_player.IsDriving && Repeat(new Rect(right, top, size, size), "GO")) MobileControlState.SetMove(Vector2.up);
            }
            else
            {
                Rect fire = new(right-size,top,size,size);
                if (_combat.IsAutomatic ? Repeat(fire, "HOLD\nFIRE") : Button(fire, _combat.AttackLabel)) MobileControlState.RequestPulse();
                if (Button(new Rect(right, top, size, size), "PUNCH")) MobileControlState.RequestFight();
                if (Button(new Rect(right - size, bottom, size, size), "KICK")) MobileControlState.RequestKick();
                if (Button(new Rect(right, top - size - 6, size, size), "JUMP")) MobileControlState.RequestJump();
                if (Button(new Rect(right-size, top-size-6, size, size), "RIDE")) MobileControlState.RequestRide();
            }
            if (Button(new Rect(right, bottom, size, size), _mission.CanInteract ? _mission.ActionLabel : "USE")) MobileControlState.RequestInteract();
        }

        private int _touchFrame=-100;
        private Rect CameraButton() => new Rect(Screen.safeArea.xMax-82,Screen.height-Screen.safeArea.yMax+76,72,38);
        private bool Button(Rect rect,string label)=>GUI.Button(rect,label,_buttonStyle) && Time.frameCount-_touchFrame>2;
        private bool Repeat(Rect rect,string label)=>GUI.RepeatButton(rect,label,_buttonStyle) && Time.frameCount-_touchFrame>2;

        private void Update()
        {
            if(TouchGameplayControls.Active)return;
            // Track each finger separately; IMGUI's single hot control cannot
            // steer + accelerate or move + jump at the same time on a phone.
            var screen=Touchscreen.current;
            if(screen==null || _player==null || Time.timeScale <= 0)return;
            float size=Mathf.Clamp(Screen.width*.12f,58,160), margin=18;
            float bottom=Screen.height-size-margin-Screen.safeArea.y, top=bottom-size;
            float left=margin,right=Screen.width-margin-size;
            Vector2 direction=Vector2.zero;bool go=false,brake=false;
            foreach(var touch in screen.touches)
            {
                if(!touch.press.isPressed)continue;
                _touchFrame=Time.frameCount;
                Vector2 p=touch.position.ReadValue();p.y=Screen.height-p.y;
                bool began=touch.press.wasPressedThisFrame;
                if(began && CameraButton().Contains(p)) Camera.main?.GetComponent<ThirdPersonCameraRig>()?.Recenter();
                if(new Rect(left+size,top,size,size).Contains(p))direction+=Vector2.up;
                if(new Rect(left,bottom,size,size).Contains(p))direction+=Vector2.left;
                if(new Rect(left+size*2,bottom,size,size).Contains(p))direction+=Vector2.right;
                if(new Rect(left+size,bottom,size,size).Contains(p))direction+=Vector2.down;
                if(new Rect(right,bottom,size,size).Contains(p)&&began)MobileControlState.RequestInteract();
                if(_player.IsInVehicle)
                {
                    if(new Rect(right,top,size,size).Contains(p)&&_player.IsDriving)go=true;
                    if(new Rect(right-size,top,size,size).Contains(p))brake=true;
                }
                else
                {
                    if(new Rect(right-size,bottom,size,size).Contains(p))go=true;
                    if(new Rect(right,top-size-6,size,size).Contains(p)&&began)MobileControlState.RequestJump();
                    if(new Rect(right-size,top-size-6,size,size).Contains(p)&&began)MobileControlState.RequestRide();
                    if(new Rect(right-size,top,size,size).Contains(p)&&(began||_combat.IsAutomatic))MobileControlState.RequestPulse();
                    if(new Rect(right,top,size,size).Contains(p)&&began)MobileControlState.RequestFight();
                }
            }
            if(go)
            {
                if(_player.IsDriving)direction.y=1;
                else if(!_player.IsInVehicle){if(direction==Vector2.zero)direction=Vector2.up;MobileControlState.SetSprint();}
            }
            if(direction!=Vector2.zero)MobileControlState.SetMove(direction);
            if(brake)
            {
                if(_player.IsPassenger)_player.CurrentVehicle.GetComponent<TrafficVehicle>()?.RequestStop();
                else MobileControlState.SetBrake();
            }
        }

        private void EnsureStyles()
        {
            if (_titleStyle != null) return;

            _titleStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 17,
                fontStyle = FontStyle.Bold,
                normal = { textColor = new Color(1f, 0.54f, 0.26f) },
            };
            _bodyStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 13,
                wordWrap = true,
                normal = { textColor = new Color(0.93f, 0.98f, 0.95f) },
            };
            _buttonStyle = new GUIStyle(GUI.skin.button)
            {
                fontSize = Mathf.RoundToInt(Mathf.Clamp(Screen.width * .03f, 14, 40)),
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter,
            };
        }
    }
}
