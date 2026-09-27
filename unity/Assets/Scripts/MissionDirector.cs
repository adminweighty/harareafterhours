using System;
using UnityEngine;

namespace HarareAfterHours
{
    [Serializable]
    public sealed class CampaignMissionSelection
    {
        public string missionId;
        public int level;
        public bool isReplay;
        public bool unlocked;
        public string title;
        public string district;
        public string activity;
        public string vehicle;
        public int coinReward;
        public int xpReward;
        public string[] objectives;
        public string primaryLabel;
        public string secondaryLabel;
        public int primaryTrust;
        public int primaryCommunity;
        public int secondaryTrust;
        public int secondaryCommunity;
    }

    public enum MissionState
    {
        // Retained so older editor helpers and serialized values still compile.
        WalkToVehicle = 0,
        DriveToMarket = 1,
        Complete = 2,
        MeetTino = 3,
        MeetRudo = 10,
        ChaseRunner = 11,
        YardRecovery = 12,
        ReturnToChipo = 13,
    }

    /// <summary>
    /// Version 3.1 opening mission: City Wakes / Wrong Delivery. This owns the
    /// live Unity objective, checkpoint and score state. Flutter receives one
    /// versioned result and remains in the city after completion.
    /// </summary>
    public sealed class MissionDirector : MonoBehaviour
    {
        public const string MissionId = "M01";
        public const int DefinitionVersion = 31;
        public const string SaveKey = "Harare.M01.Runtime.v31";
        public const string RewardKey = "Harare.M01.RewardCommitted.v31";
        public const string ValidationSessionKey = "Harare.M01CampaignValidation";
        public static string ActiveSaveKey
        {
            get
            {
#if UNITY_EDITOR
                if (UnityEditor.SessionState.GetBool(ValidationSessionKey, false)) return SaveKey + ".validation";
#endif
                return SaveKey;
            }
        }
        public static string ActiveRewardKey
        {
            get
            {
#if UNITY_EDITOR
                if (UnityEditor.SessionState.GetBool(ValidationSessionKey, false)) return RewardKey + ".validation";
#endif
                return RewardKey;
            }
        }

        public static readonly Vector3 OpeningSpawn = new(4.5f, .2f, -8.5f);
        public static readonly Vector3 RudoPosition = new(7.5f, .18f, -3.5f);
        public static readonly Vector3 MarketRoutePosition = new(18f, .18f, -13f);
        public static readonly Vector3 ServiceYardPosition = new(33f, .18f, -26f);
        public static readonly Vector3 GaragePosition = ServiceYardPosition + new Vector3(2.5f, 0, 1.5f);
        public static readonly Vector3 ChipoPosition = new(28.5f, .18f, -34f);
        public static readonly Vector3 PosterPosition = new(22f, .18f, -18f);

        [Serializable]
        private sealed class RuntimeSave
        {
            public int definitionVersion = DefinitionVersion;
            public int state = (int)MissionState.MeetRudo;
            public string checkpointId = "m01_start";
            public string openingRoute = "none";
            public string recoveryMethod = "none";
            public bool posterSeen;
            public bool cleanRun = true;
            public float elapsedSeconds;
        }

        [Serializable]
        private sealed class MissionResultPayload
        {
            public string missionId = MissionDirector.MissionId;
            public int definitionVersion = DefinitionVersion;
            public int sequence;
            public int score;
            public int stars;
            public int coins;
            public int xp;
            public int timeSeconds;
            public string openingRoute;
            public string recoveryMethod;
            public bool posterSeen;
            public bool clean;
            public bool firstCompletion;
        }

        private ThirdPersonController _player;
        private VehicleController _vehicle;
        private FlutterGameBridge _bridge;
        private PlayerProgression _progression;
        private Transform _marker;
        private Transform _checkpointVisual;
        private Transform _runner;
        private Transform _runnerThreatIndicator;
        private CharacterController _runnerBody;
        private GameObject _satchel;
        private Transform _rudo;
        private Transform _tino;
        private int _runnerWaypoint;
        private float _runnerSpeed;
        private float _runnerVerticalVelocity;
        private float _runnerAvoidUntil;
        private float _runnerAvoidSign = 1f;
        private float _elapsed;
        private bool _cleanRun = true;
        private bool _posterSeen;
        private bool _isReplay;
        private string _openingRoute = "none";
        private string _recoveryMethod = "none";
        private string _checkpointId = "m01_start";
        private string _notice;
        private float _noticeUntil;
        private int _resultSequence;
        private ActOneCampaign _actOne;
        private static readonly Vector3[] RunnerWaypoints =
        {
            new(13f, .18f, -7f),
            new(20f, .18f, -14f),
            new(27f, .18f, -20f),
            ServiceYardPosition,
        };

        public MissionState State { get; private set; } = MissionState.MeetRudo;
        public float ElapsedSeconds => _elapsed;
        public bool CleanRun => _cleanRun;
        public string OpeningRoute => _openingRoute;
        public bool PosterSeen => _posterSeen;
        public string CheckpointId => _checkpointId;
        public string SatchelCarrier => _satchel != null && _satchel.transform.parent != null
            ? _satchel.transform.parent.name
            : "none";
        public string Notice => Time.time < _noticeUntil ? _notice : string.Empty;
        public bool JoinaWelcome => false;
        public bool CanTalkAtJoina => false;
        // Compatibility name used by older HUD code; it now means any M01 interaction.
        public bool CanTalkToTino => CanInteract;

        public int ActiveMissionLevel => _actOne?.Active == true ? _actOne.Level : 1;
        public string ActiveMissionId => _actOne?.Active == true ? _actOne.MissionId : MissionId;
        public bool ExtendedMissionComplete => _actOne?.Completed == true;
        public ActOneCampaign ExtendedCampaign => _actOne;

        public bool CanInteract
        {
            get
            {
                if (_actOne?.Active == true) return _actOne.CanInteract;
                if (_player == null || _player.IsInVehicle) return false;
                return State switch
                {
                    MissionState.MeetRudo => Near(RudoPosition, 3f),
                    MissionState.ChaseRunner => _runner != null && Near(_runner.position, 3f),
                    MissionState.YardRecovery => (_runner != null && Near(_runner.position, 3.2f)) ||
                                                   (_tino != null && Near(_tino.position, 3.2f)),
                    MissionState.ReturnToChipo => Near(ChipoPosition, 3.2f),
                    _ => false,
                };
            }
        }

        public string ActionLabel
        {
            get
            {
                if (_actOne?.Active == true) return _actOne.ActionLabel;
                if (!CanInteract) return "USE";
                return State switch
                {
                    MissionState.MeetRudo => "TALK",
                    MissionState.ChaseRunner => "RECOVER",
                    MissionState.YardRecovery => _tino != null && Near(_tino.position, 3.2f) ? "COMMAND" : "RECOVER",
                    MissionState.ReturnToChipo => "RETURN",
                    _ => "USE",
                };
            }
        }

        public Vector3 ObjectivePosition => _actOne?.Active == true ? _actOne.ObjectivePosition : State switch
        {
            MissionState.MeetRudo => RudoPosition,
            MissionState.ChaseRunner => _runner != null ? _runner.position : RunnerWaypoints[0],
            MissionState.YardRecovery => _runner != null ? _runner.position : ServiceYardPosition,
            MissionState.ReturnToChipo => ChipoPosition,
            _ => _player != null ? _player.transform.position : OpeningSpawn,
        };

        public int DistanceMetres => _actOne?.Active == true ? _actOne.DistanceMetres : _player == null || State == MissionState.Complete
            ? 0
            : Mathf.CeilToInt(Vector3.Distance(_player.transform.position, ObjectivePosition));

        public string ObjectiveText => _actOne?.Active == true ? _actOne.ObjectiveText : State switch
        {
            MissionState.MeetRudo => CanInteract ? "Talk to Rudo" : "Meet Rudo for the delivery",
            MissionState.ChaseRunner => CanInteract ? "Recover the stolen satchel" :
                (_openingRoute == "service" ? "Cut off the runner at the service yard" : "Chase the satchel runner"),
            MissionState.YardRecovery => CanInteract ? "Recover the satchel or command Tino" : "Reach the service yard",
            MissionState.ReturnToChipo => CanInteract ? "Return the satchel to Chipo" : "Take the satchel to Chipo",
            MissionState.Complete => "M01 complete · Menu for next steps",
            _ => "City Wakes",
        };

        public string ControlHint => _actOne?.Active == true ? _actOne.ControlHint : State switch
        {
            MissionState.MeetRudo => CanInteract ? "TALK / E — accept the envelope" : "Follow the gold marker",
            MissionState.ChaseRunner => _player != null && _player.IsDriving
                ? "HOLD ACCEL · ◀ / ▶ steer · BRAKE stops"
                : CanInteract ? "RECOVER / E — take the satchel" : "Follow on foot, or use the hatchback",
            MissionState.YardRecovery => CanInteract ? $"{ActionLabel} / E" : "Reach the marked yard",
            MissionState.ReturnToChipo => CanInteract ? "RETURN / E — finish the delivery" : "Follow the marker",
            _ => _player != null && _player.IsInVehicle ? "Stop, then USE / E to exit" : "Explore Harare",
        };

        public void Configure(ThirdPersonController player, VehicleController vehicle, FlutterGameBridge bridge, Vector3 unusedDestination)
        {
            _player = player;
            _vehicle = vehicle;
            _bridge = bridge;
            _progression = player.GetComponent<PlayerProgression>();
            _actOne = new ActOneCampaign(this, player, bridge, _progression);
            Load();
            BuildMissionCast();
            BuildMarker();
            RestoreCheckpointPosition();
            UpdateWorldState();
            ShowNotice(State == MissionState.Complete
                ? "City Wakes complete · continue exploring Harare"
                : "M01 · City Wakes / Wrong Delivery", 6f);
        }

        public void SetActiveMission(string payload)
        {
            CampaignMissionSelection selection = null;
            try { selection = JsonUtility.FromJson<CampaignMissionSelection>(payload); }
            catch (ArgumentException) { }
            if (selection != null && selection.level >= 2 && selection.level <= 30)
            {
                if (_actOne.TryStart(selection))
                {
                    UpdateWorldState();
                    UpdateMarker();
                    return;
                }
                _bridge?.Publish("mission_unavailable", "{\"missionId\":\"" +
                    selection.missionId + "\",\"reason\":\"previous_mission_required\"}");
                return;
            }
            if (selection == null || (selection.level != 1 && selection.missionId != MissionId))
            {
                _bridge?.Publish("mission_unavailable", "{\"missionId\":\"" +
                    (selection?.missionId ?? "unknown") + "\",\"reason\":\"not_implemented_yet\"}");
                return;
            }

            _isReplay = selection.isReplay;
            if (_isReplay && State == MissionState.Complete)
            {
                ResetForReplay();
                ShowNotice("M01 replay started · first-completion rewards remain committed", 6f);
            }
            else if (State == MissionState.Complete)
            {
                // Reconcile a Unity completion into a fresh Flutter view model.
                PublishCompletion(false);
            }
            PublishStatus();
        }

        private void BuildMissionCast()
        {
            Transform root = new GameObject("M01 Wrong Delivery cast").transform;
            root.SetParent(transform);
            _rudo = SpawnNpc("Rudo", RudoPosition, "teal", "gold", root);
            _tino = SpawnNpc("Tino", GaragePosition, "olive", "silver", root);
            SpawnNpc("Chipo", ChipoPosition, "orange", "gold", root);

            GameObject runnerRoot = new("Satchel runner");
            runnerRoot.transform.SetParent(root);
            runnerRoot.transform.position = RudoPosition + new Vector3(1.8f, 0, 1.5f);
            _runner = runnerRoot.transform;
            _runnerBody = runnerRoot.AddComponent<CharacterController>();
            _runnerBody.height = 1.78f;
            _runnerBody.radius = .3f;
            _runnerBody.center = Vector3.up * .91f;
            _runnerBody.stepOffset = .25f;
            GameObject runnerModel = Instantiate(Resources.Load<GameObject>("HarareCharacters/Character01"), _runner);
            runnerModel.name = "Runner visual";
            runnerModel.AddComponent<RuntimeCharacterVisual>().Configure(new CharacterCastEntry
            {
                Id = "m01_runner", DisplayName = "Satchel runner", SkinTone = "dark", HairStyle = "close cut",
                Outfit = "plum", Accessory = "none", Wardrobe = "street", Accent = "red", Height = 1, Build = 1,
            }, false);
            BuildSatchel();
            BuildThreatIndicator(_runner);
            BuildPoster(root);
        }

        private Transform SpawnNpc(string displayName, Vector3 position, string outfit, string accent, Transform parent)
        {
            GameObject prefab = Resources.Load<GameObject>("HarareCharacters/Character01");
            if (prefab == null) throw new InvalidOperationException("Shared character prefab is required for M01.");
            GameObject npc = Instantiate(prefab, position, Quaternion.Euler(0, 180, 0), parent);
            npc.name = displayName;
            npc.AddComponent<RuntimeCharacterVisual>().Configure(new CharacterCastEntry
            {
                Id = displayName.ToLowerInvariant(), DisplayName = displayName, SkinTone = "dark", HairStyle = "natural",
                Outfit = outfit, Accessory = "none", Wardrobe = "street", Accent = accent, Height = 1, Build = 1,
            }, false);
            npc.AddComponent<MissionNpcPresence>().Configure(_player.transform);
            return npc.transform;
        }

        private void BuildSatchel()
        {
            _satchel = new GameObject("Delivery satchel");
            Material leather = MakeMaterial(new Color(.13f, .045f, .018f), false);
            Material edge = MakeMaterial(new Color(.045f, .018f, .009f), false);
            SatchelPart("Leather bag", new Vector3(0, 0, 0), new Vector3(.48f, .38f, .18f), leather);
            SatchelPart("Satchel flap", new Vector3(0, .12f, -.105f), new Vector3(.46f, .16f, .035f), leather);
            SatchelPart("Brass clasp", new Vector3(0, .06f, -.132f), new Vector3(.07f, .08f, .025f),
                MakeMaterial(new Color(.66f, .39f, .08f), true));
            LineRenderer strap = _satchel.AddComponent<LineRenderer>();
            strap.name = "Leather shoulder strap";
            strap.useWorldSpace = false;
            strap.positionCount = 4;
            strap.SetPositions(new[]
            {
                new Vector3(-.20f, .12f, 0), new Vector3(-.24f, .66f, .03f),
                new Vector3(.24f, .66f, .03f), new Vector3(.20f, .12f, 0),
            });
            strap.startWidth = strap.endWidth = .035f;
            strap.numCornerVertices = 3;
            strap.sharedMaterial = edge;
        }

        private void SatchelPart(string label, Vector3 localPosition, Vector3 scale, Material material)
        {
            GameObject part = GameObject.CreatePrimitive(PrimitiveType.Cube);
            part.name = label;
            part.transform.SetParent(_satchel.transform, false);
            part.transform.localPosition = localPosition;
            part.transform.localScale = scale;
            DisableCollider(part);
            part.GetComponent<Renderer>().sharedMaterial = material;
        }

        private void AttachSatchel(Transform carrier)
        {
            if (_satchel == null || carrier == null) return;
            if (_satchel.transform.parent != carrier) _satchel.transform.SetParent(carrier, false);
            _satchel.transform.localPosition = new Vector3(0, 1.05f, -.24f);
            _satchel.transform.localRotation = Quaternion.Euler(3f, 0, 0);
            _satchel.transform.localScale = Vector3.one;
            _satchel.SetActive(true);
        }

        private void BuildThreatIndicator(Transform target)
        {
            _runnerThreatIndicator = HostileIndicator.CreateMarkerVisual(target, "Mission thief red indicator");
            _runnerThreatIndicator.localPosition = new Vector3(0, 2.65f, 0);
            _runnerThreatIndicator.localScale = Vector3.one * 1.2f;
        }

        private void BuildPoster(Transform parent)
        {
            GameObject board = GameObject.CreatePrimitive(PrimitiveType.Cube);
            board.name = "Farai venue schedule poster";
            board.transform.SetParent(parent);
            board.transform.position = PosterPosition + Vector3.up * 1.2f;
            board.transform.localScale = new Vector3(1.2f, 1.8f, .08f);
            board.transform.rotation = Quaternion.Euler(0, 145f, 0);
            DisableCollider(board);
            board.GetComponent<Renderer>().sharedMaterial = MakeMaterial(new Color(.42f, .025f, .075f), false);

            GameObject copy = new("Farai poster readable copy");
            copy.transform.SetParent(board.transform, false);
            copy.transform.localPosition = new Vector3(0, 0, -.56f);
            copy.transform.localRotation = Quaternion.Euler(0, 180f, 0);
            copy.transform.localScale = new Vector3(.06f, .04f, .04f);
            TextMesh text = copy.AddComponent<TextMesh>();
            text.text = "FARAI\nFRIDAY 20:00\nLIVE VENUE";
            text.anchor = TextAnchor.MiddleCenter;
            text.alignment = TextAlignment.Center;
            text.fontSize = 42;
            text.color = new Color(1f, .83f, .48f);
        }

        private void BuildMarker()
        {
            GameObject marker = new("Current M01 objective marker");
            marker.transform.SetParent(transform);
            _marker = marker.transform;
            Material material = MakeMarkerMaterial(new Color(1f, .55f, .12f));
            GameObject ring = new("Objective ring");
            ring.transform.SetParent(_marker, false);
            ring.transform.localPosition = new Vector3(0, .06f, 0);
            LineRenderer line = ring.AddComponent<LineRenderer>();
            line.useWorldSpace = false;
            line.loop = true;
            line.positionCount = 48;
            line.startWidth = line.endWidth = .12f;
            line.numCornerVertices = 2;
            line.sharedMaterial = material;
            for (int i = 0; i < line.positionCount; i++)
            {
                float angle = i * Mathf.PI * 2f / line.positionCount;
                line.SetPosition(i, new Vector3(Mathf.Cos(angle) * 2.25f, 0, Mathf.Sin(angle) * 2.25f));
            }
            _checkpointVisual = MarkerPart("Objective beacon", PrimitiveType.Cylinder,
                new Vector3(0, 2.5f, 0), new Vector3(.18f, 2.5f, .18f), material).transform;
        }

        private GameObject MarkerPart(string label, PrimitiveType type, Vector3 position, Vector3 scale, Material material)
        {
            GameObject part = GameObject.CreatePrimitive(type);
            part.name = label;
            part.transform.SetParent(_marker, false);
            part.transform.localPosition = position;
            part.transform.localScale = scale;
            DisableCollider(part);
            part.GetComponent<Renderer>().sharedMaterial = material;
            return part;
        }

        private void Update()
        {
            if (_actOne?.Active == true)
            {
                _actOne.Tick();
                UpdateMarker();
                return;
            }
            if (State != MissionState.Complete && !(StreetActionDirector.Instance?.Arrested ?? false)) _elapsed += Time.deltaTime;
            if (State == MissionState.ChaseRunner) UpdateRunner();
            if (!_posterSeen && State is MissionState.ChaseRunner or MissionState.YardRecovery && Near(PosterPosition, 3f))
            {
                _posterSeen = true;
                Save();
                ShowNotice("Optional clue found · Farai's venue schedule", 5f);
            }
            UpdateMarker();
            UpdateRunnerThreatIndicator();
        }

        private void UpdateRunnerThreatIndicator()
        {
            if (_runnerThreatIndicator == null) return;
            bool active = State is MissionState.ChaseRunner or MissionState.YardRecovery;
            if (_runnerThreatIndicator.gameObject.activeSelf != active) _runnerThreatIndicator.gameObject.SetActive(active);
            if (!active || Camera.main == null) return;
            Vector3 toCamera = Camera.main.transform.position - _runnerThreatIndicator.position;
            toCamera.y = 0;
            if (toCamera.sqrMagnitude > .001f)
                _runnerThreatIndicator.rotation = Quaternion.LookRotation(toCamera.normalized, Vector3.up);
            float distanceScale = Mathf.Clamp(Vector3.Distance(Camera.main.transform.position, _runnerThreatIndicator.position) * .027f, .9f, 1.8f);
            float pulse = .92f + Mathf.PingPong(Time.unscaledTime * 1.8f, .15f);
            _runnerThreatIndicator.localScale = Vector3.one * (distanceScale * pulse);
        }

        private void UpdateRunner()
        {
            if (_runner == null || _runnerWaypoint >= RunnerWaypoints.Length) return;
            Vector3 target = RunnerWaypoints[_runnerWaypoint];
            Vector3 delta = target - _runner.position;
            delta.y = 0;
            if (delta.magnitude < .65f)
            {
                _runnerWaypoint++;
                if (_runnerWaypoint >= RunnerWaypoints.Length)
                {
                    State = MissionState.YardRecovery;
                    _checkpointId = "m01_yard";
                    _runner.position = ServiceYardPosition;
                    Save();
                    ShowNotice("Runner reached the yard · intercept him or command Tino to block the exit", 7f);
                    PublishStatus();
                }
                return;
            }
            float distanceToPlayer = _player == null ? 8f : Vector3.Distance(_runner.position, _player.transform.position);
            float desiredSpeed = Mathf.Lerp(3.45f, 4.65f, Mathf.Clamp01((distanceToPlayer - 2f) / 12f));
            desiredSpeed *= Mathf.Lerp(.72f, 1f, Mathf.Clamp01(delta.magnitude / 2.5f));
            _runnerSpeed = Mathf.MoveTowards(_runnerSpeed, desiredSpeed, Time.deltaTime * 3.2f);
            Vector3 direction = delta.normalized;
            if (Time.time < _runnerAvoidUntil)
            {
                Vector3 side = Vector3.Cross(Vector3.up, direction) * _runnerAvoidSign;
                direction = Vector3.Slerp(direction, side, .58f).normalized;
            }
            _runner.rotation = Quaternion.RotateTowards(_runner.rotation, Quaternion.LookRotation(direction), 420f * Time.deltaTime);
            if (_runnerBody != null && _runnerBody.enabled)
            {
                if (_runnerBody.isGrounded && _runnerVerticalVelocity < 0) _runnerVerticalVelocity = -2f;
                else _runnerVerticalVelocity += Physics.gravity.y * Time.deltaTime;
                CollisionFlags collision = _runnerBody.Move((direction * _runnerSpeed + Vector3.up * _runnerVerticalVelocity) * Time.deltaTime);
                if ((collision & CollisionFlags.Sides) != 0)
                {
                    _runnerAvoidSign *= -1f;
                    _runnerAvoidUntil = Time.time + .65f;
                    _runnerSpeed = Mathf.Min(_runnerSpeed, 2.5f);
                }
            }
            else _runner.position += direction * (_runnerSpeed * Time.deltaTime);
            if (_openingRoute == "none" && _player != null && !_player.IsInVehicle && Near(MarketRoutePosition, 5f))
                SetRoute("market");
        }

        private void UpdateMarker()
        {
            if (_marker == null) return;
            if (_actOne?.Active == true)
            {
                _marker.gameObject.SetActive(!_actOne.Completed);
                if (!_actOne.Completed) _marker.position = _actOne.ObjectivePosition;
                return;
            }
            _marker.gameObject.SetActive(State != MissionState.Complete);
            if (State == MissionState.Complete) return;
            _marker.position = ObjectivePosition;
            float pulse = 1f + Mathf.Sin(Time.time * 3.4f) * .1f;
            _checkpointVisual.localScale = new Vector3(.18f * pulse, 2.5f, .18f * pulse);
        }

        public bool TryInteract()
        {
            if (_actOne?.Active == true) return _actOne.TryInteract();
            if (!CanInteract) return false;
            switch (State)
            {
                case MissionState.MeetRudo:
                    State = MissionState.ChaseRunner;
                    _checkpointId = "m01_collection";
                    _runnerWaypoint = 0;
                    Save();
                    UpdateWorldState();
                    ShowNotice("Rudo handed over the envelope — the runner stole the satchel! Chase him.", 7f);
                    PublishStatus();
                    return true;
                case MissionState.ChaseRunner:
                    RecoverSatchel("intercepted");
                    return true;
                case MissionState.YardRecovery:
                    RecoverSatchel(_tino != null && Near(_tino.position, 3.2f) ? "tino_block" : "yard_intercept");
                    return true;
                case MissionState.ReturnToChipo:
                    CompleteMission();
                    return true;
                default:
                    return false;
            }
        }

        private void RecoverSatchel(string method)
        {
            if (_openingRoute == "none") SetRoute(_player.IsInVehicle ? "service" : "market");
            _recoveryMethod = method;
            State = MissionState.ReturnToChipo;
            _checkpointId = "m01_yard";
            Save();
            UpdateWorldState();
            ShowNotice(method == "tino_block"
                ? "Tino blocked the exit · satchel recovered. Return it to Chipo."
                : "Satchel recovered · return it to Chipo.", 7f);
            PublishStatus();
        }

        private void CompleteMission()
        {
            bool firstCompletion = PlayerPrefs.GetInt(ActiveRewardKey, 0) == 0;
            State = MissionState.Complete;
            _checkpointId = "m01_complete";
            if (firstCompletion)
            {
                PlayerPrefs.SetInt(ActiveRewardKey, 1);
                _progression?.AwardMissionStep("m01_result_committed_v31", CalculateScore());
            }
            Save();
            UpdateWorldState();
            int score = CalculateScore();
            ShowNotice($"M01 complete · {score} score · +{(firstCompletion ? 500 : 0)} coins · +{(firstCompletion ? 100 : 0)} XP", 9f);
            PublishCompletion(firstCompletion);
        }

        private int CalculateScore()
        {
            int efficiency = (_elapsed <= 120f ? 100 : 50) + (_recoveryMethod == "intercepted" ? 100 : 75);
            return Mathf.Clamp(600 + efficiency + (_cleanRun ? 100 : 0) + (_posterSeen ? 100 : 0), 0, 1000);
        }

        private void PublishCompletion(bool firstCompletion)
        {
            int score = CalculateScore();
            _bridge?.PublishGuidance();
            _bridge?.Publish("mission_complete", JsonUtility.ToJson(new MissionResultPayload
            {
                sequence = ++_resultSequence,
                score = score,
                stars = score >= 850 ? 3 : score >= 700 ? 2 : 1,
                coins = firstCompletion ? 500 : 0,
                xp = firstCompletion ? 100 : 0,
                timeSeconds = Mathf.RoundToInt(_elapsed),
                openingRoute = _openingRoute,
                recoveryMethod = _recoveryMethod,
                posterSeen = _posterSeen,
                clean = _cleanRun,
                firstCompletion = firstCompletion,
            }));
        }

        private void PublishStatus()
        {
            _bridge?.PublishGuidance();
            _bridge?.Publish("mission_stage", "{\"missionId\":\"M01\",\"definitionVersion\":31,\"stage\":\"" +
                State + "\",\"checkpointId\":\"" + _checkpointId + "\",\"openingRoute\":\"" + _openingRoute + "\"}");
        }

        public bool CanEnterVehicle(VehicleController vehicle) => true;

        public void OnVehicleEntered()
        {
            if (State == MissionState.ChaseRunner) SetRoute("service");
        }

        public void TryComplete(Collider actor) { }

        public void RegisterImpact(float impact)
        {
            if (State is not (MissionState.ChaseRunner or MissionState.YardRecovery) || impact < 9f || !_cleanRun) return;
            _cleanRun = false;
            Save();
            ShowNotice("Collision · clean-run score bonus lost", 4f);
            _bridge?.Publish("mission_penalty", "{\"missionId\":\"M01\",\"reason\":\"collision\"}");
        }

        private void SetRoute(string route)
        {
            if (_openingRoute != "none") return;
            _openingRoute = route;
            Save();
            ShowNotice(route == "service" ? "Service route chosen · use the hatchback to cut him off" : "Market route chosen · stay close on foot", 5f);
            PublishStatus();
        }

        private void Load()
        {
            RuntimeSave save = null;
            try { save = JsonUtility.FromJson<RuntimeSave>(PlayerPrefs.GetString(ActiveSaveKey, string.Empty)); }
            catch (ArgumentException) { }
            if (save == null || save.definitionVersion != DefinitionVersion)
            {
                State = MissionState.MeetRudo;
                return;
            }
            State = Enum.IsDefined(typeof(MissionState), save.state) ? (MissionState)save.state : MissionState.MeetRudo;
            if (State is MissionState.WalkToVehicle or MissionState.DriveToMarket or MissionState.MeetTino) State = MissionState.MeetRudo;
            _checkpointId = string.IsNullOrEmpty(save.checkpointId) ? "m01_start" : save.checkpointId;
            _openingRoute = string.IsNullOrEmpty(save.openingRoute) ? "none" : save.openingRoute;
            _recoveryMethod = string.IsNullOrEmpty(save.recoveryMethod) ? "none" : save.recoveryMethod;
            _posterSeen = save.posterSeen;
            _cleanRun = save.cleanRun;
            _elapsed = Mathf.Max(0, save.elapsedSeconds);
            _runnerWaypoint = State == MissionState.YardRecovery || State == MissionState.ReturnToChipo || State == MissionState.Complete
                ? RunnerWaypoints.Length
                : 0;
        }

        private void Save()
        {
            PlayerPrefs.SetString(ActiveSaveKey, JsonUtility.ToJson(new RuntimeSave
            {
                state = (int)State,
                checkpointId = _checkpointId,
                openingRoute = _openingRoute,
                recoveryMethod = _recoveryMethod,
                posterSeen = _posterSeen,
                cleanRun = _cleanRun,
                elapsedSeconds = _elapsed,
            }));
            PlayerPrefs.Save();
        }

        private void ResetForReplay()
        {
            State = MissionState.MeetRudo;
            _checkpointId = "m01_start";
            _openingRoute = "none";
            _recoveryMethod = "none";
            _posterSeen = false;
            _cleanRun = true;
            _elapsed = 0;
            _runnerWaypoint = 0;
            _runnerSpeed = 0;
            _runnerVerticalVelocity = 0;
            RelocatePlayer(OpeningSpawn);
            if (_runner != null) _runner.position = RudoPosition + new Vector3(1.8f, 0, 1.5f);
            Save();
            UpdateWorldState();
        }

        private void RestoreCheckpointPosition()
        {
            if (State is MissionState.YardRecovery or MissionState.ReturnToChipo) RelocatePlayer(ServiceYardPosition + new Vector3(-4f, 0, 0));
            else if (State != MissionState.Complete) RelocatePlayer(OpeningSpawn);
        }

        private void RelocatePlayer(Vector3 position)
        {
            CharacterController body = _player.GetComponent<CharacterController>();
            if (body != null) body.enabled = false;
            _player.transform.position = position;
            if (body != null) body.enabled = true;
        }

        private void UpdateWorldState()
        {
            if (_runner != null)
            {
                _runner.gameObject.SetActive(State is MissionState.MeetRudo or MissionState.ChaseRunner or MissionState.YardRecovery);
                if (State == MissionState.YardRecovery) _runner.position = ServiceYardPosition;
            }
            if (_satchel != null)
            {
                switch (State)
                {
                    case MissionState.MeetRudo:
                        AttachSatchel(_rudo);
                        break;
                    case MissionState.ChaseRunner:
                    case MissionState.YardRecovery:
                        AttachSatchel(_runner);
                        break;
                    case MissionState.ReturnToChipo:
                        AttachSatchel(_player.transform);
                        break;
                    default:
                        _satchel.SetActive(false);
                        break;
                }
            }
            UpdateMarker();
        }

        private bool Near(Vector3 position, float range) => _player != null &&
            Vector3.Distance(_player.transform.position, position) <= range;

        private void ShowNotice(string text, float seconds)
        {
            _notice = text;
            _noticeUntil = Time.time + seconds;
        }

        private static Material MakeMaterial(Color color, bool emission)
        {
            Material material = RuntimeMaterialFactory.Create("Universal Render Pipeline/Lit", "Standard");
            if (material.HasProperty("_BaseColor")) material.SetColor("_BaseColor", color);
            if (material.HasProperty("_Color")) material.color = color;
            if (emission)
            {
                material.EnableKeyword("_EMISSION");
                material.SetColor("_EmissionColor", color * 1.6f);
            }
            return material;
        }

        private static Material MakeMarkerMaterial(Color color)
        {
            Material material = RuntimeMaterialFactory.Create("Universal Render Pipeline/Unlit", "Sprites/Default");
            if (material.HasProperty("_BaseColor")) material.SetColor("_BaseColor", color);
            if (material.HasProperty("_Color")) material.SetColor("_Color", color);
            if (material.HasProperty("_Cull")) material.SetFloat("_Cull", 0f);
            return material;
        }

        private static void DisableCollider(GameObject item)
        {
            Collider collider = item != null ? item.GetComponent<Collider>() : null;
            if (collider != null) collider.enabled = false;
        }
    }

    // Kept for compatibility with scenes and old editor helpers. M01 v3.1
    // completes by returning the recovered satchel to Chipo.
    public sealed class MissionCheckpoint : MonoBehaviour
    {
        private MissionDirector _director;
        public void Configure(MissionDirector director) => _director = director;
        private void OnTriggerEnter(Collider other) => _director?.TryComplete(other);
        private void OnTriggerStay(Collider other) => _director?.TryComplete(other);
    }

    /// <summary>Nearby mission characters turn naturally to acknowledge the player.</summary>
    public sealed class MissionNpcPresence : MonoBehaviour
    {
        private Transform _player;
        private Quaternion _restingRotation;

        public void Configure(Transform player)
        {
            _player = player;
            _restingRotation = transform.rotation;
        }

        private void Update()
        {
            if (_player == null || Time.timeScale <= 0) return;
            Vector3 offset = _player.position - transform.position;
            offset.y = 0;
            Quaternion target = _restingRotation;
            if (offset.sqrMagnitude < 42.25f && offset.sqrMagnitude > 1f)
                target = Quaternion.LookRotation(offset.normalized);
            transform.rotation = Quaternion.RotateTowards(transform.rotation, target, 150f * Time.deltaTime);
        }
    }
}
