using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace HarareAfterHours
{
    /// <summary>
    /// Playable M02-M30 campaign chain. M02-M05 have bespoke Act I routes;
    /// later missions are composed from the script-authored Flutter mission
    /// contract. Each phase owns a world action, a
    /// checkpoint, and a saved consequence. It reuses the live city systems;
    /// no menu button can advance a mission.
    /// </summary>
    public sealed class ActOneCampaign
    {
        const int Version = 31;
        public const string SaveKey = "Harare.ActOne.Runtime.v31";
        public const string ValidationSessionKey = "Harare.ActOneCampaignValidation";
        const string ChoicePrimary = "primary";
        const string ChoiceSecondary = "secondary";

        [Serializable]
        sealed class SaveData
        {
            public int version = Version;
            public int mission;
            public int phase;
            public int waypoint;
            public string choice = "none";
            public string branch = "none";
            public bool optional;
            public bool clean = true;
            public float elapsed;
        }

        [Serializable]
        sealed class ResultPayload
        {
            public string missionId;
            public int definitionVersion = Version;
            public int sequence;
            public int score;
            public int stars;
            public int coins;
            public int xp;
            public int timeSeconds;
            public string choiceLabel;
            public int crewTrust;
            public int communitySupport;
            public bool firstCompletion;
        }

        enum PhaseKind { Interact, Choice, Route, Drive, Combat }

        sealed class Phase
        {
            public PhaseKind Kind;
            public string Objective;
            public string Hint;
            public string Action;
            public Vector3[] Points;
            public string Primary;
            public string Secondary;
            public int Enemies;
            public bool Armed;
            public bool ResultChoice;
        }

        sealed class Definition
        {
            public int Level;
            public string Title;
            public int Coins;
            public int Xp;
            public int PrimaryTrust;
            public int PrimaryCommunity;
            public int SecondaryTrust;
            public int SecondaryCommunity;
            public Phase[] Phases;
        }

        static readonly Vector3 Chipo = MissionDirector.ChipoPosition;
        static readonly Vector3 Market = new(20, .18f, -28);
        static readonly Vector3 MarketNorth = new(19, .18f, -13);
        static readonly Vector3 Yard = MissionDirector.ServiceYardPosition;
        static readonly Vector3 Garage = MissionDirector.GaragePosition;
        static readonly Vector3 StopA = new(0, .18f, 54);
        static readonly Vector3 StopB = new(48, .18f, 54);
        static readonly Vector3 StopC = new(48, .18f, 3);
        static readonly Vector3 Sana = new(-8, .18f, -20);

        static readonly Dictionary<int, Definition> Definitions = new()
        {
            [2] = new Definition
            {
                Level=2, Title="Change for a Note", Coins=600, Xp=125,
                PrimaryCommunity=1,
                Phases=new[]
                {
                    new Phase { Kind=PhaseKind.Choice, ResultChoice=true, Objective="M02 · Resolve the customer's overpayment", Hint="Choose who returns the overpayment", Action="CHOOSE",
                        Points=new[]{Chipo+new Vector3(-2.2f,0,0),Chipo+new Vector3(2.2f,0,0)}, Primary="Return it directly", Secondary="Ask Chipo to resolve it" },
                    new Phase { Kind=PhaseKind.Route, Objective="Follow the collection through the market", Hint="Keep the courier route in view · reach each gold checkpoint",
                        Points=new[]{Market,MarketNorth,new Vector3(12,.18f,-8)} },
                    new Phase { Kind=PhaseKind.Choice, Objective="Let Chipo confront the contractor", Hint="Choose a public or private confrontation", Action="CHOOSE",
                        Points=new[]{Market+new Vector3(-2.5f,0,3),Market+new Vector3(2.5f,0,3)}, Primary="Public confrontation", Secondary="Private confrontation" },
                    new Phase { Kind=PhaseKind.Interact, Objective="Clear the overturned cart and recover the note", Hint="Reach the blocked lane · tap CLEAR", Action="CLEAR", Points=new[]{Market+new Vector3(0,0,7)} },
                }
            },
            [3] = new Definition
            {
                Level=3, Title="Three Stops", Coins=700, Xp=150,
                PrimaryTrust=1,
                Phases=new[]
                {
                    new Phase { Kind=PhaseKind.Choice, Objective="M03 · Choose Tino's delivery setup", Hint="GRIP is steadier · ACCELERATION is quicker", Action="CHOOSE",
                        Points=new[]{Garage+new Vector3(-2.5f,0,0),Garage+new Vector3(2.5f,0,0)}, Primary="Grip setup", Secondary="Acceleration setup" },
                    new Phase { Kind=PhaseKind.Drive, Objective="Deliver the first two client orders", Hint="Enter the hatchback · stop inside each delivery marker · tap DELIVER", Action="DELIVER", Points=new[]{StopA,StopB} },
                    new Phase { Kind=PhaseKind.Choice, ResultChoice=true, Objective="Answer the client's extra request", Hint="Choose whether to secure the fragile parcel", Action="CHOOSE",
                        Points=new[]{StopB+new Vector3(-2.5f,0,3),StopB+new Vector3(2.5f,0,3)}, Primary="Secure the fragile parcel", Secondary="Keep the booked load" },
                    new Phase { Kind=PhaseKind.Drive, Objective="Finish at the tight loading zone", Hint="Drive cleanly · stop in the final bay · tap FINISH", Action="FINISH", Points=new[]{StopC} },
                }
            },
            [4] = new Definition
            {
                Level=4, Title="The Missing Phone", Coins=800, Xp=175,
                Phases=new[]
                {
                    new Phase { Kind=PhaseKind.Route, Objective="M04 · Track the runner's dropped markers", Hint="Follow the gold checkpoints to the service yard", Points=new[]{Sana,new Vector3(4,.18f,-20),Yard+new Vector3(-8,0,0)} },
                    new Phase { Kind=PhaseKind.Combat, Objective="Stop the brawler blocking the trail", Hint="PUNCH or KICK · defeat the marked brawler", Points=new[]{Yard+new Vector3(-5,0,3)}, Enemies=1, Armed=false },
                    new Phase { Kind=PhaseKind.Combat, Objective="Win the loading-yard firefight", Hint="Use cover · AIM and FIRE · reload when empty", Points=new[]{Yard+new Vector3(3,0,4)}, Enemies=2, Armed=true },
                    new Phase { Kind=PhaseKind.Choice, ResultChoice=true, Objective="Recover the dispatch phone", Hint="Choose what evidence to prioritise", Action="RECOVER",
                        Points=new[]{Yard+new Vector3(-2.5f,0,8),Yard+new Vector3(2.5f,0,8)}, Primary="Return the courier's belongings", Secondary="Take the contractor photograph" },
                }
            },
            [5] = new Definition
            {
                Level=5, Title="Last Kombi", Coins=900, Xp=200,
                PrimaryTrust=1,
                Phases=new[]
                {
                    new Phase { Kind=PhaseKind.Interact, Objective="M05 · Read the final route card", Hint="Meet Chipo at the route board · tap START", Action="START", Points=new[]{Chipo} },
                    new Phase { Kind=PhaseKind.Drive, Objective="Complete the first two passenger stops", Hint="Drive smoothly · stop at each marker · tap STOP", Action="STOP", Points=new[]{StopA,StopB} },
                    new Phase { Kind=PhaseKind.Choice, ResultChoice=true, Objective="Plan the stranded-worker pickup", Hint="Choose one combined route or ask Tino to split the pickup", Action="CHOOSE",
                        Points=new[]{StopB+new Vector3(-2.5f,0,3),StopB+new Vector3(2.5f,0,3)}, Primary="Combined route", Secondary="Split pickup" },
                    new Phase { Kind=PhaseKind.Drive, Objective="Bring everyone safely to the depot", Hint="Follow the final marker · stop · tap COMPLETE", Action="COMPLETE", Points=new[]{Garage} },
                }
            },
        };

        readonly MissionDirector _owner;
        readonly ThirdPersonController _player;
        readonly FlutterGameBridge _bridge;
        readonly PlayerProgression _progression;
        Definition _definition;
        SaveData _save;
        readonly List<StreetActor> _missionEnemies = new();
        readonly List<GameObject> _choiceMarkers = new();
        bool _combatSpawned;
        int _sequence;

        public ActOneCampaign(MissionDirector owner, ThirdPersonController player,
            FlutterGameBridge bridge, PlayerProgression progression)
        { _owner=owner; _player=player; _bridge=bridge; _progression=progression; }

        public bool Active => _definition != null;
        public bool Completed => Active && _save.phase >= 4;
        public int Level => _definition?.Level ?? 0;
        public int PhaseIndex => Active ? _save.phase : -1;
        public int WaypointIndex => Active ? _save.waypoint : -1;
        public int ActiveEnemyCount => _missionEnemies.Count(actor=>actor!=null&&!actor.Down);
        public int CombatEnemyTarget => Active && !Completed && Current.Kind==PhaseKind.Combat
            ? Current.Enemies+GameDifficultySettings.Profile.MissionEnemyBonus : 0;
        Phase Current => Active ? _definition.Phases[Mathf.Clamp(_save.phase,0,3)] : null;
        Vector3 CurrentPoint => Current.Points[Mathf.Clamp(_save.waypoint,0,Current.Points.Length-1)];
        public Vector3 PrimaryChoicePosition => Active && !Completed && Current.Kind==PhaseKind.Choice ? Current.Points[0] : ObjectivePosition;
        public Vector3 SecondaryChoicePosition => Active && !Completed && Current.Kind==PhaseKind.Choice ? Current.Points[1] : ObjectivePosition;
        public string MissionId => Active ? $"M{Level:00}" : MissionDirector.MissionId;
        public string CheckpointId => Active ? $"m{Level:00}_phase_{_save.phase}_{_save.waypoint}" : "";
        public Vector3 ObjectivePosition => Active && !Completed ? CurrentPoint : _player.transform.position;
        public int DistanceMetres => Active && !Completed ? Mathf.CeilToInt(Vector3.Distance(_player.transform.position,CurrentPoint)) : 0;
        public string ObjectiveText
        {
            get
            {
                if(Completed)return $"M{Level:00} complete · next mission pending";
                if(!Active)return "";
                string objective=Current.Kind==PhaseKind.Choice
                    ? $"{Current.Objective} · A {Current.Primary} / B {Current.Secondary}"
                    : Current.Objective+(Current.Points.Length>1?$" · {_save.waypoint+1}/{Current.Points.Length}":"");
                if(Current.Kind==PhaseKind.Combat&&_combatSpawned)
                    objective+=$" · {ActiveEnemyCount}/{CombatEnemyTarget} threats remain";
                return objective;
            }
        }
        public string ControlHint => Completed ? "Mission saved · open Menu for campaign status" : !Active ? "" : Current.Kind switch
        {
            PhaseKind.Drive when !_player.IsDriving => "Stand beside a stopped car · tap USE to drive",
            PhaseKind.Drive => "Follow the gold marker · brake below 5 km/h",
            PhaseKind.Combat when !_combatSpawned => "Reach the marked encounter boundary",
            PhaseKind.Combat => Current.Hint+$" · {ActiveEnemyCount} remaining",
            PhaseKind.Choice => $"A: {Current.Primary} · B: {Current.Secondary} · stand in a ring and tap {Current.Action}",
            _ => Current.Hint,
        };
        public string ActionLabel => CanInteract ? Current.Action ?? "USE" : "USE";
        public bool CanInteract
        {
            get
            {
                if(!Active||Completed||_player.InputLocked)return false;
                if(Current.Kind==PhaseKind.Combat)
                    return _combatSpawned && _missionEnemies.Count>0 && _missionEnemies.All(a=>a==null||a.Down) && Near(CurrentPoint,3.2f);
                if(Current.Kind==PhaseKind.Drive)
                    return _player.IsDriving && _player.CurrentVehicle.SpeedKph<5 && Near(CurrentPoint,8f);
                if(Current.Kind==PhaseKind.Route)return false;
                if(Current.Kind==PhaseKind.Choice)
                    return !_player.IsInVehicle && (Near(Current.Points[0],2.8f)||Near(Current.Points[1],2.8f));
                return !_player.IsInVehicle && Near(CurrentPoint,3f);
            }
        }

        public bool TryStart(CampaignMissionSelection selection)
        {
            if(selection==null||selection.level<2||selection.level>30)return false;
            int level=selection.level;
            bool replay=selection.isReplay;
            if(!Definitions.TryGetValue(level,out var definition))definition=BuildStoryDefinition(selection);
            if(level>2 && !selection.unlocked && PlayerPrefs.GetInt(RewardKey(level-1),0)==0)return false;
            _definition=definition;
            _save=Load(level);
            if(replay || _save.mission!=level || _save.phase>=4)
                _save=new SaveData{mission=level};
            CleanupEnemies();
            _combatSpawned=false;
            Save();
            RefreshChoiceMarkers();
            PublishStatus();
            _bridge?.PublishGuidance();
            StreetActionDirector.Instance?.Notify($"M{level:00} · {definition.Title}");
            return true;
        }

        public void Tick()
        {
            if(!Active||Completed||Time.timeScale<=0)return;
            _save.elapsed+=Time.deltaTime;
            var phase=Current;
            if(phase.Kind==PhaseKind.Route)
            {
                if(Near(CurrentPoint,4f))AdvanceWaypoint();
            }
            else if(phase.Kind==PhaseKind.Combat)
            {
                if(!_combatSpawned&&Near(CurrentPoint,18f))SpawnCombat(phase);
            }
        }

        public bool TryInteract()
        {
            if(!CanInteract)return false;
            if(Current.Kind==PhaseKind.Choice)
            {
                bool primary=Vector3.Distance(_player.transform.position,Current.Points[0])<=
                    Vector3.Distance(_player.transform.position,Current.Points[1]);
                if(Current.ResultChoice)_save.choice=primary?ChoicePrimary:ChoiceSecondary;
                else _save.branch=primary?ChoicePrimary:ChoiceSecondary;
                StreetActionDirector.Instance?.Notify(primary?Current.Primary:Current.Secondary);
            }
            AdvanceWaypoint();
            return true;
        }

        void AdvanceWaypoint()
        {
            if(Current.Kind!=PhaseKind.Choice && _save.waypoint+1<Current.Points.Length)
            {
                _save.waypoint++;
                Save();PublishStatus();_bridge?.PublishGuidance();return;
            }
            _save.waypoint=0;_save.phase++;
            CleanupEnemies();_combatSpawned=false;
            if(_save.phase>=4){Complete();return;}
            RefreshChoiceMarkers();
            Save();PublishStatus();_bridge?.PublishGuidance();
            StreetActionDirector.Instance?.Notify(Current.Objective);
        }

        void SpawnCombat(Phase phase)
        {
            _combatSpawned=true;
            var director=StreetActionDirector.Instance;
            int enemyCount=phase.Enemies+GameDifficultySettings.Profile.MissionEnemyBonus;
            for(int i=0;i<enemyCount;i++)
            {
                float angle=i*Mathf.PI*2/Mathf.Max(1,enemyCount);
                var at=CurrentPoint+new Vector3(Mathf.Cos(angle)*4,0,Mathf.Sin(angle)*4);
                var actor=director.SpawnActor(false,at,$"M{Level:00} active threat {i+1}");
                if(phase.Armed)actor.Arm();
                _missionEnemies.Add(actor);
            }
            StreetActionDirector.Instance?.Notify((phase.Armed?"ARMED THREATS · use cover":"BRAWLERS · punch or kick")+$" · defeat all {enemyCount}");
        }

        void Complete()
        {
            bool first=PlayerPrefs.GetInt(RewardKey(Level),0)==0;
            if(first)
            {
                PlayerPrefs.SetInt(RewardKey(Level),1);
                _progression?.AwardMissionStep($"m{Level:00}_result_committed_v31",Score());
            }
            _save.phase=4;Save();PlayerPrefs.Save();
            RefreshChoiceMarkers();
            _bridge?.PublishGuidance();
            string choice=_save.choice==ChoicePrimary ? FirstChoice().Primary : FirstChoice().Secondary;
            int trust=_save.choice==ChoicePrimary?_definition.PrimaryTrust:_definition.SecondaryTrust;
            int community=_save.choice==ChoicePrimary?_definition.PrimaryCommunity:_definition.SecondaryCommunity;
            int score=Score();
            _bridge?.Publish("mission_complete",JsonUtility.ToJson(new ResultPayload
            {
                missionId=MissionId,sequence=200000+(Level*1000)+(++_sequence),score=score,
                stars=score>=850?3:score>=700?2:1,coins=first?_definition.Coins:0,
                xp=first?_definition.Xp:0,timeSeconds=Mathf.RoundToInt(_save.elapsed),choiceLabel=choice,
                crewTrust=first?trust:0,communitySupport=first?community:0,firstCompletion=first,
            }));
            StreetActionDirector.Instance?.Notify($"M{Level:00} complete · {score} score · "+(first?$"+{_definition.Coins} coins":"reward already paid"));
        }

        Phase FirstChoice()=>_definition.Phases.First(p=>p.ResultChoice);
        int Score()=>Mathf.Clamp(600+(_save.clean?100:0)+(_save.optional?100:0)+(_save.elapsed<=420?200:100),0,1000);
        bool Near(Vector3 point,float range)=>Vector3.Distance(_player.transform.position,point)<=range;
        public static string ActiveSaveKey
        {
            get
            {
#if UNITY_EDITOR
                if(UnityEditor.SessionState.GetBool(ValidationSessionKey,false))return SaveKey+".validation";
#endif
                return SaveKey;
            }
        }
        public static string ActiveRewardKey(int level)=>RewardKey(level);
        static string RewardKey(int level)
        {
            string key=$"Harare.M{level:00}.RewardCommitted.v31";
#if UNITY_EDITOR
            if(UnityEditor.SessionState.GetBool(ValidationSessionKey,false))return key+".validation";
#endif
            return key;
        }
        SaveData Load(int level)
        {
            try
            {
                var data=JsonUtility.FromJson<SaveData>(PlayerPrefs.GetString(ActiveSaveKey,"{}"));
                return data!=null&&data.version==Version&&data.mission==level?data:new SaveData{mission=level};
            }
            catch(ArgumentException){return new SaveData{mission=level};}
        }
        void Save(){PlayerPrefs.SetString(ActiveSaveKey,JsonUtility.ToJson(_save));PlayerPrefs.Save();}
        void PublishStatus()=>_bridge?.Publish("mission_stage",$"{{\"missionId\":\"{MissionId}\",\"definitionVersion\":31,\"stage\":{_save.phase},\"checkpointId\":\"{CheckpointId}\"}}");
        void CleanupEnemies()
        {
            foreach(var actor in _missionEnemies)
                if(actor!=null){StreetActionDirector.Instance?.Actors.Remove(actor);UnityEngine.Object.Destroy(actor.gameObject);}
            _missionEnemies.Clear();
        }

        static Definition BuildStoryDefinition(CampaignMissionSelection selection)
        {
            int lane=(selection.level-6)%5;
            Vector3 start=new(-28+lane*14,.18f,-8+((selection.level/5)%3)*13);
            Vector3 middle=start+new Vector3(10,0,12);
            Vector3 finish=middle+new Vector3(11,0,-7);
            string[] objectives=selection.objectives??Array.Empty<string>();
            string Objective(int index,string fallback)=>index<objectives.Length&&!string.IsNullOrWhiteSpace(objectives[index])?objectives[index]:fallback;
            string activity=(selection.activity??string.Empty).ToLowerInvariant();
            string vehicle=(selection.vehicle??string.Empty).ToLowerInvariant();
            bool drive=vehicle.Length>0&&vehicle!="on foot"&&vehicle!="none" ||
                activity.Contains("driv")||activity.Contains("race")||activity.Contains("delivery")||activity.Contains("transport");
            bool armed=activity.Contains("shoot")||activity.Contains("raid")||activity.Contains("armed")||activity.Contains("takedown");
            bool combat=armed||activity.Contains("combat")||activity.Contains("fight")||activity.Contains("escape")||activity.Contains("stealth");
            string primary=string.IsNullOrWhiteSpace(selection.primaryLabel)?"Back the crew":selection.primaryLabel;
            string secondary=string.IsNullOrWhiteSpace(selection.secondaryLabel)?"Protect the community":selection.secondaryLabel;
            Phase travel=drive
                ? new Phase{Kind=PhaseKind.Drive,Objective=Objective(1,"Reach the operation zone"),Hint="Enter a vehicle · follow the gold route · stop in each marker",Action="CONTINUE",Points=new[]{middle,finish}}
                : new Phase{Kind=PhaseKind.Route,Objective=Objective(1,"Reach the operation zone"),Hint="Follow every gold checkpoint through the district",Points=new[]{middle,finish}};
            Phase pressure=combat
                ? new Phase{Kind=PhaseKind.Combat,Objective=Objective(2,"Clear the immediate threat"),Hint=armed?"Use cover · AIM and FIRE · stop every marked threat":"PUNCH or KICK · stop every marked threat",Points=new[]{finish+new Vector3(4,0,5)},Enemies=armed?3:2,Armed=armed}
                : new Phase{Kind=PhaseKind.Interact,Objective=Objective(2,"Secure the mission evidence"),Hint="Reach the evidence marker · tap SECURE",Action="SECURE",Points=new[]{finish+new Vector3(4,0,5)}};
            return new Definition
            {
                Level=selection.level,Title=string.IsNullOrWhiteSpace(selection.title)?$"Mission {selection.level:00}":selection.title,
                Coins=Mathf.Max(0,selection.coinReward),Xp=Mathf.Max(0,selection.xpReward),
                PrimaryTrust=selection.primaryTrust,PrimaryCommunity=selection.primaryCommunity,
                SecondaryTrust=selection.secondaryTrust,SecondaryCommunity=selection.secondaryCommunity,
                Phases=new[]
                {
                    new Phase{Kind=PhaseKind.Interact,Objective=$"M{selection.level:00} · {Objective(0,"Meet the mission contact")}",Hint=$"Reach the {selection.district} briefing marker · tap START",Action="START",Points=new[]{start}},
                    travel,
                    pressure,
                    new Phase{Kind=PhaseKind.Choice,ResultChoice=true,Objective=Objective(3,"Make the decision that closes the mission"),Hint="Choose the consequence you want to carry forward",Action="CHOOSE",Points=new[]{finish+new Vector3(-2.6f,0,10),finish+new Vector3(2.6f,0,10)},Primary=primary,Secondary=secondary},
                }
            };
        }

        void RefreshChoiceMarkers()
        {
            foreach(var marker in _choiceMarkers)if(marker!=null)UnityEngine.Object.Destroy(marker);
            _choiceMarkers.Clear();
            if(!Active||Completed||Current.Kind!=PhaseKind.Choice)return;
            BuildChoiceMarker(Current.Points[0],"A",Current.Primary,new Color(.08f,.75f,.72f));
            BuildChoiceMarker(Current.Points[1],"B",Current.Secondary,new Color(.72f,.25f,.9f));
        }

        void BuildChoiceMarker(Vector3 position,string code,string label,Color color)
        {
            GameObject root=new($"Choice {code} · {label}");
            root.transform.SetParent(_owner.transform);
            root.transform.position=position;
            _choiceMarkers.Add(root);
            Material material=RuntimeMaterialFactory.Create("Universal Render Pipeline/Unlit","Sprites/Default");
            if(material.HasProperty("_BaseColor"))material.SetColor("_BaseColor",color);
            if(material.HasProperty("_Color"))material.SetColor("_Color",color);
            GameObject ring=GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            ring.name=$"Choice {code} floor marker";
            ring.transform.SetParent(root.transform,false);
            ring.transform.localPosition=new Vector3(0,.05f,0);
            ring.transform.localScale=new Vector3(1.35f,.025f,1.35f);
            Collider collider=ring.GetComponent<Collider>();if(collider!=null)collider.enabled=false;
            ring.GetComponent<Renderer>().sharedMaterial=material;
            GameObject copy=new($"Choice {code} label");
            copy.transform.SetParent(root.transform,false);
            copy.transform.localPosition=new Vector3(0,2.1f,0);
            copy.transform.localScale=Vector3.one*.055f;
            copy.AddComponent<CameraFacingBillboard>();
            TextMesh text=copy.AddComponent<TextMesh>();
            text.text=$"{code}\n{label.ToUpperInvariant()}";
            text.anchor=TextAnchor.MiddleCenter;
            text.alignment=TextAlignment.Center;
            text.fontSize=52;
            text.color=Color.white;
        }
    }
}
