using System;
using System.Linq;
using System.Reflection;
using UnityEngine;

namespace HarareAfterHours
{
    /// <summary>
    /// Stable message contract for the Flutter host. A Unity-as-a-Library
    /// adapter can forward <see cref="LastEvent"/> and call
    /// <see cref="OnFlutterMessage"/> without coupling core gameplay to a
    /// particular Flutter plugin.
    /// </summary>
    [UnityEngine.Scripting.Preserve]
    public sealed class FlutterGameBridge : MonoBehaviour
    {
        [Serializable]
        private sealed class BridgeCommand
        {
            public string type;
            public string payload;
        }

        [Serializable]
        private sealed class BridgeEvent
        {
            public string type;
            public string payload;
        }

        public static FlutterGameBridge Instance { get; private set; }
        public event Action<string> EventPublished;
        public string LastEvent { get; private set; } = string.Empty;

        private ThirdPersonController _player;
        private bool _sessionEnded;
        private MissionDirector _mission;
        private PlayerProgression _progression;
        private PlayerLoadout _loadout;

        private void Awake()
        {
            Instance = this;
            gameObject.name = "FlutterBridge";
        }

        public void Bind(ThirdPersonController player, MissionDirector mission, PlayerProgression progression)
        {
            _player = player;
            _mission = mission;
            _progression = progression;
            _loadout = player.GetComponent<PlayerLoadout>();
            if (_loadout == null) _loadout = player.gameObject.AddComponent<PlayerLoadout>();
            _loadout.Initialize(player);
        }

        /// <summary>
        /// Entry point called by Flutter using the game object name
        /// <c>FlutterBridge</c>, method <c>OnFlutterMessage</c>.
        /// </summary>
        [UnityEngine.Scripting.Preserve]
        public void OnFlutterMessage(string message)
        {
            if (string.IsNullOrWhiteSpace(message)) return;

            try
            {
                BridgeCommand command = JsonUtility.FromJson<BridgeCommand>(message);
                if (command == null || string.IsNullOrWhiteSpace(command.type)) return;

                switch (command.type)
                {
                    case "set_character_profile":
                        if (_loadout != null && _loadout.ApplyJson(command.payload))
                            Publish("character_updated", "{\"status\":\"saved_locally\"}");
                        break;
                    case "request_character_profile":
                        if (_loadout != null) Publish("character_profile", _loadout.ProfileJson);
                        break;
                    case "set_character_photo":
                        _player?.ApplyPortrait(command.payload);
                        Publish("character_photo_updated", "{\"status\":\"applied_locally\"}");
                        break;
                    case "request_guidance":
                        PublishGuidance();
                        break;
                    case "request_mission_status":
                        PublishMissionStatus();
                        break;
                    case "set_active_mission":
                        _mission?.SetActiveMission(command.payload);
                        break;
                    case "request_player_status":
                        PublishPlayerStatus();
                        break;
                    case "request_world_ready":
                        PublishWorldReady();
                        if(StreetActionDirector.Instance?.GameOver==true)Publish("game_over","{}");
                        break;
                    case "restart_encounter":
                        StreetActionDirector.Instance?.RestartEncounter();
                        break;
                    case "pause":
                        Time.timeScale = 0f;
                        MobileControlState.Reset();
                        TouchGameplayControls.Instance?.Cancel();
                        PublishGuidance();
                        break;
                    case "end_session":
                        _sessionEnded=true;
                        MobileControlState.Reset();TouchGameplayControls.Instance?.Cancel();
                        _player?.GetComponent<PlayerCombat>()?.CancelPendingMelee();
                        PlayerPrefs.Save();Time.timeScale=0;AudioListener.pause=true;
                        Publish("session_ended","{}");
                        break;
                    case "restart_session":
                        MobileControlState.Reset();AudioListener.pause=false;Time.timeScale=1;
                        UnityEngine.SceneManagement.SceneManager.LoadScene(UnityEngine.SceneManagement.SceneManager.GetActiveScene().buildIndex);
                        break;
                    case "set_graphics":
                        CityGraphics.Instance?.Apply(command.payload);
                        break;
                    case "request_audio_state":
                        PublishAudioState();
                        break;
                    case "set_audio_muted":
                        if (bool.TryParse(command.payload, out bool muted))
                        {
                            HarareAfterHoursBootstrap.Instance?.ArtLibrary?.SetSoundMuted(muted);
                            PublishAudioState();
                        }
                        break;
                    case "set_audio_shuffle":
                        if (bool.TryParse(command.payload, out bool shuffle))
                        {
                            HarareAfterHoursBootstrap.Instance?.ArtLibrary?.SetShuffleEnabled(shuffle);
                            PublishAudioState();
                        }
                        break;
                    case "randomize_soundtrack":
                        HarareAfterHoursBootstrap.Instance?.ArtLibrary?.RandomizeTrack();
                        PublishAudioState();
                        break;
                    case "set_difficulty":
                        if(StreetActionDirector.Instance?.SetDifficulty(command.payload)==true)
                            Publish("difficulty_changed", "{\"difficulty\":\""+StreetActionDirector.Instance.DifficultyLevel+"\"}");
                        break;
                    case "edit_controls":
                        TouchGameplayControls.Instance?.BeginEdit();
                        break;
                    case "resume":
                        if(!_sessionEnded && StreetActionDirector.Instance?.GameOver!=true && TouchGameplayControls.Instance?.Editing!=true)Time.timeScale = 1f;
                        break;
                }
            }
            catch (Exception exception)
            {
                Debug.LogWarning($"Flutter bridge ignored an invalid message: {exception.Message}");
            }
        }

        public void Publish(string type, string payload)
        {
            LastEvent = JsonUtility.ToJson(new BridgeEvent { type = type, payload = payload });
            Debug.Log($"[FlutterBridge] {LastEvent}");
            EventPublished?.Invoke(LastEvent);
            TryForwardToFlutter(LastEvent);
        }

        public void PublishGuidance()
        {
            if (_mission != null) Publish("player_guidance", JsonUtility.ToJson(PlayerGuidance.For(_mission)));
        }

        private void PublishAudioState()
        {
            HarareArtLibrary library = HarareAfterHoursBootstrap.Instance?.ArtLibrary;
            if (library != null) Publish("audio_state", library.AudioStateJson);
        }

        private void PublishMissionStatus()
        {
            if (_mission == null) return;
            Publish(
                "mission_status",
                $"{{\"missionId\":\"{MissionDirector.MissionId}\",\"definitionVersion\":{MissionDirector.DefinitionVersion},\"state\":\"{_mission.State}\",\"checkpointId\":\"{_mission.CheckpointId}\",\"openingRoute\":\"{_mission.OpeningRoute}\",\"posterSeen\":{_mission.PosterSeen.ToString().ToLowerInvariant()},\"clean\":{_mission.CleanRun.ToString().ToLowerInvariant()},\"seconds\":{Mathf.RoundToInt(_mission.ElapsedSeconds)}}}"
            );
        }

        private void PublishPlayerStatus()
        {
            if (_player == null) return;
            int points = _progression != null ? _progression.Points : 0;
            int combo = _progression != null ? _progression.Combo : 0;
            Publish(
                "player_status",
                $"{{\"character\":\"{_player.Avatar.DisplayName}\",\"driving\":{_player.IsDriving.ToString().ToLowerInvariant()},\"points\":{points},\"combo\":{combo}}}"
            );
        }

        private void PublishWorldReady()
        {
            if (_player == null || _mission == null) return;
            string difficulty=StreetActionDirector.Instance?.DifficultyLevel.ToString() ?? GameDifficultySettings.Current.ToString();
            Publish("world_ready", "{\"district\":\"Harare CBD\",\"mode\":\"campaign\",\"missionId\":\"M01\",\"definitionVersion\":31,\"time\":\"18:30\",\"difficulty\":\""+difficulty+"\"}");
        }

        private static void TryForwardToFlutter(string message)
        {
#if (UNITY_IOS || UNITY_ANDROID) && !UNITY_EDITOR
            // A direct reference also keeps the native message path alive in
            // stripped IL2CPP builds; Instance is inherited from the singleton.
            FlutterUnityIntegration.UnityMessageManager.Instance.SendMessageToFlutter(message);
#else
            // The gameplay assembly deliberately stays buildable without a
            // Flutter plugin. Once FlutterUnityIntegration is imported, this
            // reflection adapter discovers its message manager at runtime.
            try
            {
                Type managerType = AppDomain.CurrentDomain
                    .GetAssemblies()
                    .Select(assembly => assembly.GetType("FlutterUnityIntegration.UnityMessageManager"))
                    .FirstOrDefault(type => type != null);
                if (managerType == null) return;

                PropertyInfo instanceProperty = managerType.GetProperty("Instance", BindingFlags.Public | BindingFlags.Static | BindingFlags.FlattenHierarchy);
                MethodInfo sendMethod = managerType.GetMethod("SendMessageToFlutter", new[] { typeof(string) });
                object instance = instanceProperty?.GetValue(null);
                if (instance != null && sendMethod != null)
                {
                    sendMethod.Invoke(instance, new object[] { message });
                }
            }
            catch (Exception exception)
            {
                Debug.LogWarning($"Flutter event forwarding is unavailable: {exception.Message}");
            }
#endif
        }
    }
}
