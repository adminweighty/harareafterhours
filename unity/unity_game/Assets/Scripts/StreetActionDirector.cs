using System.Collections.Generic;
using UnityEngine;

namespace HarareAfterHours
{
    // Fictional arcade encounters, not a simulation of real police procedures.
    public sealed class StreetActionDirector : MonoBehaviour
    {
        public static StreetActionDirector Instance { get; private set; }
        public readonly List<StreetActor> Actors = new();
        public readonly List<PolicePatrol> Patrols = new();
        readonly List<StreetActor> _streetThreats = new();
        public ThirdPersonController Player { get; private set; }
        public int Wanted { get; private set; }
        public float Health { get; private set; } = 100;
        public GameDifficulty DifficultyLevel => GameDifficultySettings.Current;
        public DifficultyProfile Difficulty => GameDifficultySettings.Profile;
        public bool RecoveringFromHit => Time.time < _lastDamage + 1.5f;
        public bool GameOver { get; private set; }
        public string CombatHint
        {
            get
            {
                if(GameOver)return "GAME OVER · restart required";
                foreach(var actor in Actors)
                    if(!actor.Officer&&!actor.Down&&Vector3.Distance(actor.transform.position,Player.transform.position)<16)
                        return actor.WindingUp?"INCOMING · PUNCH, KICK or MOVE AWAY":"HOSTILE · PUNCH is fast · KICK hits harder";
                return DifficultyLevelComplete
                    ? DifficultyLevel.ToString().ToUpperInvariant()+" LEVEL CLEAR · choose another level in Menu"
                    : DifficultyLevel.ToString().ToUpperInvariant()+" · stop "+StreetThreatsRemaining+" marked threats";
            }
        }
        public int StreetThreatTarget => Difficulty.StreetEnemyCount;
        public int StreetThreatsDefeated { get; private set; }
        public int StreetThreatsRemaining => Mathf.Max(0,StreetThreatTarget-StreetThreatsDefeated);
        public bool DifficultyLevelComplete { get; private set; }
        public float Capture { get; private set; }
        public float HiddenSeconds { get; private set; }
        public Vector3 LastKnownPosition { get; private set; }
        public bool Arrested { get; private set; }
        public float ReleaseAt { get; private set; }
        public string Notice => Time.time < _noticeUntil ? _notice : "";
        public float TakedownUntil { get; private set; }
        public string TakedownText { get; private set; } = "";
        public bool TakedownActive => Time.time < TakedownUntil;
        public float GraceUntil { get; private set; }
        float _noticeUntil, _lastDamage = -100, _lastCrime, _nextCrime, _nextSense, _tickAt;
        string _notice;
        PlayerProgression _score;
        FlutterGameBridge _bridge;
        public string Status => (GameOver ? "GAME OVER" : Arrested ? "CUFFED · release in " + Mathf.CeilToInt(ReleaseAt-Time.time)+"s"
            : Wanted>0 ? "ZRP " + new string('*',Wanted) + (HiddenSeconds>0?" · SEARCH "+Mathf.CeilToInt(12-HiddenSeconds)+"s":" · PURSUIT")
            : "HEALTH "+Mathf.CeilToInt(Health)+" · FREE ROAM")+
            $" · {DifficultyLevel.ToString().ToUpperInvariant()} {StreetThreatsDefeated}/{StreetThreatTarget}";

        public void Configure(ThirdPersonController player, PlayerProgression score, FlutterGameBridge bridge)
        {
            Instance=this;Player=player;_score=score;_bridge=bridge;GraceUntil=Time.time+18;
            _tickAt=Time.time;
            for(int i=0;i<2;i++)
            {
                var prefab=Resources.Load<GameObject>("HarareVehicles/Prefabs/SharedSUV");
                if(prefab==null)throw new System.InvalidOperationException("Shared police car base missing");
                var car=Instantiate(prefab,transform);car.name="ZRP patrol "+(i+1);
                car.transform.position=new Vector3(i==0?-42:42,.16f,i==0?-2:40);
                var traffic=car.GetComponent<TrafficVehicle>();if(traffic!=null)Destroy(traffic);
                var patrol=car.AddComponent<PolicePatrol>();patrol.Configure(this,i);Patrols.Add(patrol);
            }
            EnsureStreetThreatPopulation();
            SpawnActor(true,new Vector3(-7.8f,.2f,3),"ZRP foot patrol");
        }
        void EnsureStreetThreatPopulation()
        {
            int target=StreetThreatTarget;
            while(_streetThreats.Count>target)
            {
                var actor=_streetThreats[_streetThreats.Count-1];
                _streetThreats.RemoveAt(_streetThreats.Count-1);Actors.Remove(actor);
                if(actor!=null)Destroy(actor.gameObject);
            }
            while(_streetThreats.Count<target)
            {
                int index=_streetThreats.Count;
                var actor=SpawnActor(false,StreetThreatPosition(index),StreetThreatName(index));
                if(StreetThreatIsArmed(index))actor.Arm();
                _streetThreats.Add(actor);
            }
        }
        static Vector3 StreetThreatPosition(int index)=>index switch
        {
            0 => new Vector3(7.8f,.2f,18),
            1 => new Vector3(-7.8f,.2f,-25),
            2 => new Vector3(34,.2f,7.8f),
            3 => JoinaCityBlock.Spawn+new Vector3(0,0,35),
            4 => new Vector3(13,.2f,24),
            5 => new Vector3(-33,.2f,-7.8f),
            6 => new Vector3(7.8f,.2f,45),
            7 => new Vector3(-7.8f,.2f,43),
            8 => new Vector3(49,.2f,7.8f),
            9 => new Vector3(-47,.2f,-7.8f),
            10 => new Vector3(7.8f,.2f,-54),
            11 => new Vector3(-7.8f,.2f,60),
            12 => new Vector3(58,.2f,-7.8f),
            _ => new Vector3(-58,.2f,7.8f),
        };
        static string StreetThreatName(int index)=>index switch
        {
            0 => "Street robber — First Street",
            1 => "Street robber — south block",
            2 => "Armed robber — east block",
            3 => "Armed robber — Joina approach",
            4 => "Street robber — trader block",
            5 => "Street robber — market approach",
            _ => (StreetThreatIsArmed(index)?"Armed robber":"Street robber")+" — patrol "+(index+1),
        };
        static bool StreetThreatIsArmed(int index)=>index==2||index==3||index==6||index==9||index>=12;
        public StreetActor SpawnActor(bool officer,Vector3 position,string label)
        {
            var root=new GameObject(label);root.transform.SetParent(transform);root.transform.position=position;
            var cc=root.AddComponent<CharacterController>();cc.height=1.78f;cc.radius=.3f;cc.center=Vector3.up*.91f;cc.stepOffset=.25f;
            var model=Instantiate(Resources.Load<GameObject>("HarareCharacters/Character01"),root.transform);
            model.AddComponent<RuntimeCharacterVisual>().Configure(new CharacterCastEntry{DisplayName=label,SkinTone="dark",Outfit=officer?"navy":"plum",Accent=officer?"silver":"gold"},false);
            var actor=root.AddComponent<StreetActor>();actor.Configure(this,officer,position);Actors.Add(actor);
            return actor;
        }
        public void Notify(string text){_notice=text;_noticeUntil=Time.time+4;}
        public void ReportCrime(int severity=1)
        {
            if(Arrested || Time.time<_nextCrime)return;
            _nextCrime=Time.time+1;_lastCrime=Time.time;HiddenSeconds=0;
            LastKnownPosition=Player.transform.position;
            Wanted=Mathf.Clamp(Wanted+severity,1,3);Notify("ZRP pursuit — run, drive away or break line of sight");
            _bridge?.Publish("wanted_changed","{\"level\":"+Wanted+"}");
        }
        public bool Visible(Vector3 eye,Transform target,float range)
        {
            Vector3 destination=target.position+Vector3.up;
            Vector3 delta=destination-eye;if(delta.magnitude>range)return false;
            foreach(var hit in Physics.RaycastAll(eye,delta.normalized,delta.magnitude,~0,QueryTriggerInteraction.Ignore))
            {
                if(hit.transform.IsChildOf(target) || target.IsChildOf(hit.transform))continue;
                if(hit.collider is WheelCollider)continue;
                return false;
            }
            return true;
        }
        void Update()
        {
            if(Player==null || Time.timeScale<=0 || GameOver)return;
            if(Arrested){if(Time.time>=ReleaseAt)Release();return;}
            if(Time.time-_lastDamage>12 && !Actors.Exists(a=>a!=null && !a.Officer && !a.Down &&
                a.gameObject.activeInHierarchy && Vector3.Distance(a.transform.position,Player.transform.position)<16))
                Health=Mathf.Min(100,Health+Time.deltaTime*Difficulty.HealthRecovery);
            if(Time.time<_nextSense)return;
            float dt=Mathf.Min(.5f,Time.time-_tickAt);_tickAt=Time.time;_nextSense=Time.time+.15f;
            if(Wanted==0){Capture=0;HiddenSeconds=0;return;}
            bool seen=false,close=false;
            foreach(var actor in Actors)
                if(actor.Officer && actor.gameObject.activeInHierarchy && !actor.Down)
                {
                    bool sight=Visible(actor.transform.position+Vector3.up*1.5f,Player.transform,35);
                    seen|=sight;
                    close|=sight && !Player.IsInVehicle && Vector3.Distance(actor.transform.position,Player.transform.position)<1.65f && !actor.Stunned;
                }
            foreach(var patrol in Patrols)seen|=Visible(patrol.transform.position+Vector3.up*2.2f,Player.transform,45);
            HiddenSeconds=seen?0:HiddenSeconds+dt;
            if(seen)LastKnownPosition=Player.transform.position;
            Capture=close?Mathf.Min(3,Capture+dt):Mathf.Max(0,Capture-dt*2);
            if(Capture>=3)BeginArrest();
            else if(HiddenSeconds>=12 && Time.time-_lastCrime>=12)
            {Wanted=0;HiddenSeconds=0;Notify("Escaped — wanted level cleared");_bridge?.Publish("wanted_changed","{\"level\":0}");}
        }
        public void BeginArrest()
        {
            if(Arrested || GameOver || Player.IsInVehicle)return;
            Arrested=true;ReleaseAt=Time.time+6;Capture=0;
            Player.InputLocked=true;Player.GetComponentInChildren<StreetActionPose>()?.SetCuffed(true);
            Notify("Arrested · handcuffed · release shortly");_bridge?.Publish("player_arrested","{}");
        }
        void Release()
        {
            Arrested=false;Wanted=0;Health=100;HiddenSeconds=0;Capture=0;
            Player.InputLocked=false;Player.GetComponentInChildren<StreetActionPose>()?.SetCuffed(false);
            Relocate(new Vector3(-7.8f,.3f,-12));GraceUntil=Time.time+20;
            int fine=_score.SpendPoints(50,"arrest_fine");Notify("Released · "+fine+" point fine · mission retained");
            _bridge?.Publish("wanted_changed","{\"level\":0}");
        }
        void Relocate(Vector3 position)
        {
            var cc=Player.GetComponent<CharacterController>();cc.enabled=false;Player.transform.position=position;cc.enabled=true;
        }
        public void Damage(float amount)
        {
            if(GameOver || Time.timeScale<=0 || Arrested || Player.IsInVehicle || Time.time<GraceUntil)return;
            if(amount<=0 || float.IsNaN(amount) || float.IsInfinity(amount) || RecoveringFromHit)return;
            BloodHitEffects.Instance?.Emit(Player.transform,Player.transform.position+Vector3.up*1.25f-Player.transform.forward*.2f,Player.transform.forward);
            _lastDamage=Time.time;Health=Mathf.Max(0,Health-amount*Difficulty.IncomingDamage);
            Player.GetComponentInChildren<StreetActionPose>()?.Flinch();
            if(Health>0)
            {
                Notify("HIT · "+Mathf.CeilToInt(Health)+" / 100 LIFE · PUNCH or KICK back!");
                return;
            }
            GameOver=true;Player.InputLocked=true;
            Player.GetComponent<PlayerCombat>()?.CancelPendingMelee();
            MobileControlState.Reset();Player.GetComponentInChildren<StreetActionPose>()?.SetDown(true);
            Time.timeScale=0;
            _bridge?.Publish("game_over","{\"reason\":\"Your life reached zero. Punch or kick to interrupt attacks, or retreat to recover.\"}");
        }
        public void RestartEncounter()
        {
            if(!GameOver)return;
            GameOver=false;Health=100;Wanted=0;Capture=0;HiddenSeconds=0;Arrested=false;
            _lastDamage=-100;
            TakedownUntil=0;TakedownText="";
            StreetThreatsDefeated=0;DifficultyLevelComplete=false;
            foreach(var actor in Actors)actor.ResetEncounter();
            Relocate(JoinaCityBlock.WelcomePending ? JoinaCityBlock.Spawn : new Vector3(4.5f,.3f,-8.5f));
            Player.InputLocked=false;Player.GetComponentInChildren<StreetActionPose>()?.SetDown(false);
            MobileControlState.Reset();GraceUntil=Time.time+8;
            Camera.main?.GetComponent<ThirdPersonCameraRig>()?.SetTarget(Player.transform,false);
            Time.timeScale=1;Notify("RESTARTED · 8 seconds to prepare · mission and earned points retained");
            _bridge?.Publish("encounter_restarted","{}");
        }
        public int Rob()=>_score.SpendPoints(40,"robbed");
        public bool SetDifficulty(string value)
        {
            GameDifficulty previous=DifficultyLevel;
            if(!GameDifficultySettings.Apply(value))return false;
            if(previous!=DifficultyLevel)
            {
                EnsureStreetThreatPopulation();
                StreetThreatsDefeated=0;DifficultyLevelComplete=false;
                foreach(var actor in _streetThreats)if(actor!=null)actor.ResetEncounter();
            }
            Notify("LEVEL · "+DifficultyLevel.ToString().ToUpperInvariant()+" · "+StreetThreatTarget+" marked threats");
            return true;
        }
        public void Defeated(StreetActor actor,int recovered)
        {
            if(actor.Officer)return;
            int before=_score.Points;
            _score.AwardMissionStep("hostile_defeated_v1_"+actor.name,50);
            int earned=_score.Points-before;
            if(_streetThreats.Contains(actor)&&!DifficultyLevelComplete)
            {
                StreetThreatsDefeated=Mathf.Min(StreetThreatTarget,StreetThreatsDefeated+1);
                DifficultyLevelComplete=StreetThreatsDefeated>=StreetThreatTarget;
                if(DifficultyLevelComplete)
                {
                    Notify(DifficultyLevel.ToString().ToUpperInvariant()+" LEVEL CLEAR · all marked threats defeated");
                    _bridge?.Publish("difficulty_level_complete","{\"difficulty\":\""+DifficultyLevel+"\",\"defeated\":"+StreetThreatsDefeated+"}");
                    return;
                }
            }
            string progress=_streetThreats.Contains(actor)?$" · {StreetThreatsDefeated}/{StreetThreatTarget}":"";
            Notify(earned>0?"HOSTILE DEFEATED · +50 POINTS"+progress:"HOSTILE DEFEATED · reward already collected"+progress);
        }
        public void RegisterShotTakedown(StreetActor actor)
        {
            TakedownUntil=Time.time+1.15f;
            TakedownText=actor.Officer?"TAKEDOWN · ZRP ALERT":"TAKEDOWN · +50 PTS";
            Notify(TakedownText);
        }
        public bool Strike(float range, bool animate = true, int damage = 1)
        {
            if(GameOver || Arrested || Player.IsInVehicle)return false;
            StreetActor best=null;float distance=range;
            foreach(var actor in Actors)
            {
                if(!actor.gameObject.activeInHierarchy || actor.Down)continue;
                Vector3 delta=actor.transform.position-Player.transform.position;delta.y=0;
                if(delta.magnitude>distance || Vector3.Dot(Player.transform.forward,delta.normalized)<.05f)continue;
                if(!Visible(Player.transform.position+Vector3.up*1.4f,actor.transform,range+.5f))continue;
                best=actor;distance=delta.magnitude;
            }
            if(animate)Player.GetComponentInChildren<StreetActionPose>()?.Punch();
            if(best==null)return false;
            Player.transform.rotation=Quaternion.LookRotation(Vector3.ProjectOnPlane(best.transform.position-Player.transform.position,Vector3.up));
            best.Hit(damage);
            if(!best.Down)Notify("HIT · enemy life "+best.HitsRemaining+" / "+Difficulty.EnemyHits+" · keep fighting");
            return true;
        }
        void OnDestroy(){if(Instance==this)Instance=null;}
    }

    public sealed class StreetActor : MonoBehaviour
    {
        StreetActionDirector _world;CharacterController _body;Vector3 _home;StreetActionPose _pose;
        float _stunUntil,_downUntil,_nextAttack,_resetAt,_attackAt;int _hits,_stolen;bool _engaged,_defeated;
        Transform _visual;Vector3 _visualPosition;Quaternion _visualRotation;
        EnemyKnockdown _knockdown;
        private HostileFirearm _firearm;
        public void Arm(){if(!Officer&&_firearm==null){_firearm=gameObject.AddComponent<HostileFirearm>();_firearm.Configure(this,_world);}}
        public bool Armed=>_firearm!=null;
        public bool WindingUp=>_attackAt>0;
        public int HitsRemaining=>Mathf.Max(0,_world.Difficulty.EnemyHits-_hits);
        public bool Officer {get;private set;}
        public bool Down=>_defeated||Time.time<_downUntil||(_knockdown!=null&&_knockdown.Active);
        public bool Stunned=>Time.time<_stunUntil;
        public void Configure(StreetActionDirector world,bool officer,Vector3 home)
        {
            _world=world;Officer=officer;_home=home;_body=GetComponent<CharacterController>();
            _pose=GetComponentInChildren<Animator>().gameObject.AddComponent<StreetActionPose>();
            _visual=_pose.transform;_visualPosition=_visual.localPosition;_visualRotation=_visual.localRotation;
            _knockdown=gameObject.AddComponent<EnemyKnockdown>();_knockdown.Initialize(_visual);
            if(!officer)gameObject.AddComponent<HostileIndicator>().Configure(this);
        }
        public void ResetEncounter()
        {
            _engaged=false;_defeated=false;_hits=0;_stolen=0;_attackAt=0;_stunUntil=0;_downUntil=0;_resetAt=0;_nextAttack=Time.time+_world.Difficulty.MeleeCooldown;
            _body.enabled=false;transform.position=_home;_body.enabled=true;
            _knockdown.ResetPose();_visual.localPosition=_visualPosition;_visual.localRotation=_visualRotation;_pose.SetDown(false);
            _firearm?.CancelShot();
        }
        public void Hit(int damage = 1)
        {
            if(Down||_world.GameOver)return;
            _attackAt=0;
            _firearm?.CancelShot();
            if(Officer)_world.ReportCrime();else _engaged=true;
            _stunUntil=Time.time+(damage>1?1.1f:.85f);_hits+=Mathf.Clamp(damage,1,2);_pose.Flinch();
            if(_hits<_world.Difficulty.EnemyHits)return;
            _hits=0;_downUntil=Time.time+(Officer?7:30);_resetAt=Time.time+(Officer?8:90);
            _pose.SetDown(true);_world.Defeated(this,_stolen);_stolen=0;_engaged=false;
            _body.enabled=false;
            _knockdown.Begin(transform.position-_world.Player.transform.position);
            if(!Officer)
            {
                _defeated=true;
            }
        }
        public bool ReceiveShot(Vector3 point, Vector3 direction)
        {
            if(Down || _world==null || !gameObject.activeInHierarchy)return false;
            bool wasDown=Down;
            Hit();
            var blood=BloodHitEffects.Instance;
            blood?.Emit(transform,point,direction);
            if(!wasDown && Down)
            {
                blood?.EmitFatality(transform,direction);
                _world.RegisterShotTakedown(this);
            }
            return true;
        }
        void Update()
        {
            if(_world==null || Time.timeScale<=0 || _world.GameOver)return;
            if(_knockdown.Active)
            {
                if(Officer&&Time.time>=_downUntil&&!_knockdown.Recovering){_pose.SetDown(false);_knockdown.Recover();}
                return;
            }
            if(!_body.enabled)_body.enabled=true;
            if(Down)return;_pose.SetDown(false);
            if(Stunned)return;
            Vector3 player=_world.Player.transform.position;
            float distance=Vector3.Distance(player,transform.position);
            bool canEngage=!_world.Arrested && Time.time>=_world.GraceUntil;
            if(Officer)_engaged=_world.Wanted>0&&!_world.Arrested;
            else if(canEngage && Time.time>=_resetAt && !_world.Player.IsInVehicle && distance<_world.Difficulty.EngageRange && _world.Visible(transform.position+Vector3.up*1.5f,_world.Player.transform,_world.Difficulty.EngageRange+1))
            {if(!_engaged)_world.Notify("HOSTILE · "+_world.Difficulty.EnemyHits+" hits to defeat · +50 points · RUN to escape");_engaged=true;}
            if(!canEngage && !Officer)_engaged=false;
            Vector3 target=_engaged?(Officer?_world.LastKnownPosition:player):_home;
            if(!Officer && _stolen>0)target=_home+(transform.position-player).normalized*12;
            Vector3 direction=target-transform.position;direction.y=0;
            if(_firearm!=null&&_firearm.Tick(_engaged&&canEngage,distance))return;
            if(_attackAt>0)
            {
                if(Time.time<_attackAt)return;
                _attackAt=0;_nextAttack=Time.time+_world.Difficulty.MeleeCooldown;_pose.Punch();
                if(canEngage&&distance<1.65f&&Vector3.Dot(transform.forward,(player-transform.position).normalized)>.2f&&_world.Visible(transform.position+Vector3.up*1.4f,_world.Player.transform,2.2f))_world.Damage(20);
                if(_world.GameOver)return;
            }
            if(_engaged && distance<1.65f && !Officer && Time.time>=_nextAttack && !_world.Player.IsInVehicle && _world.Visible(transform.position+Vector3.up*1.4f,_world.Player.transform,2.2f))
            {
                Vector3 facing=player-transform.position;facing.y=0;if(facing.sqrMagnitude>.001f)transform.rotation=Quaternion.LookRotation(facing);
                _attackAt=Time.time+_world.Difficulty.MeleeWindup;_pose.Punch();_world.Notify("ATTACK WARNING · MOVE AWAY or FIGHT to interrupt");return;
            }
            if(!Officer && _engaged && distance>_world.Difficulty.DisengageRange){_engaged=false;_stolen=0;_resetAt=Time.time+45;}
            float speed=_engaged?(Officer?3.7f:_world.Difficulty.EnemySpeed):1.4f;
            Vector3 move=Vector3.zero;
            if(direction.magnitude>(Officer&&_engaged?1.2f:.65f))
            {
                var heading=direction.normalized;
                if(Physics.SphereCast(transform.position+Vector3.up*.9f,.28f,heading,out var hit,.9f,~0,QueryTriggerInteraction.Ignore)&&!hit.transform.IsChildOf(transform)&&!hit.transform.IsChildOf(_world.Player.transform))
                    heading=Vector3.Cross(Vector3.up,hit.normal).normalized;
                if(heading.sqrMagnitude>.1f){transform.rotation=Quaternion.Slerp(transform.rotation,Quaternion.LookRotation(heading),Time.deltaTime*9);move=heading*speed;}
            }
            _body.Move((move+Vector3.down*4)*Time.deltaTime);
        }
    }
}
