#if UNITY_EDITOR
using System;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace HarareAfterHours.EditorTools
{
    [InitializeOnLoad]
    public static class M01CampaignValidation
    {
        private const string Key = MissionDirector.ValidationSessionKey;
        private static int _stage;
        private static int _frame;
        private static double _deadline;
        private static HarareAfterHoursBootstrap _game;
        private static int _pointsAfterCompletion;
        private static float _stageStarted;
        private static Transform _runner;
        private static Vector3 _runnerStart;

        static M01CampaignValidation() => EditorApplication.update += Tick;

        public static void Run()
        {
            Directory.CreateDirectory("/private/tmp/harare-m01-validation");
            SessionState.SetBool(Key, true);
            PlayerPrefs.DeleteKey(MissionDirector.ActiveSaveKey);
            PlayerPrefs.DeleteKey(MissionDirector.ActiveRewardKey);
            PlayerPrefs.DeleteKey(PlayerProgression.ActiveSaveKey);
            PlayerPrefs.Save();
            EditorSceneManager.OpenScene("Assets/Scenes/SampleScene.unity");
            _stage = 0;
            _deadline = 0;
            EditorApplication.isPlaying = true;
        }

        private static void Tick()
        {
            if (!SessionState.GetBool(Key, false) || !EditorApplication.isPlaying || _frame == Time.frameCount) return;
            _frame = Time.frameCount;
            try
            {
                if (_deadline == 0) _deadline = EditorApplication.timeSinceStartup + 120;
                Require(EditorApplication.timeSinceStartup < _deadline, "M01 validation timed out");
                _game = HarareAfterHoursBootstrap.Instance;
                if (_game == null || _game.Player == null || _game.Mission == null || Time.time < 2f) return;

                switch (_stage)
                {
                    case 0:
                        Require(_game.Mission.State == MissionState.MeetRudo, "M01 starts with Rudo");
                        Require(_game.Mission.ObjectiveText.Contains("Rudo"), "M01 exposes the Rudo objective");
                        Require(_game.Mission.SatchelCarrier == "Rudo",
                            "Rudo visibly carries the satchel before the theft; carrier=" + _game.Mission.SatchelCarrier);
                        Require(GameObject.Find("Farai poster readable copy")?.GetComponent<TextMesh>()?.text.Contains("FARAI") == true,
                            "the optional clue is physically readable");
                        MovePlayer(MissionDirector.RudoPosition);
                        Require(_game.Mission.TryInteract(), "Rudo interaction starts the theft");
                        Require(_game.Mission.State == MissionState.ChaseRunner, "runner chase begins");
                        Require(_game.Mission.CheckpointId == "m01_collection", "collection checkpoint saved");
                        Require(PlayerPrefs.GetString(MissionDirector.ActiveSaveKey).Contains("m01_collection"), "collection checkpoint persisted");
                        _game.Mission.OnVehicleEntered();
                        Require(_game.Mission.OpeningRoute == "service", "vehicle selects the service route");
                        _runner = GameObject.Find("Satchel runner").transform;
                        _runnerStart = _runner.position;
                        Require(_game.Mission.SatchelCarrier == _runner.name, "the runner visibly takes the satchel");
                        Next();
                        break;
                    case 1:
                        if (Time.time - _stageStarted < .45f) return;
                        Require(Vector3.Distance(_runner.position, _runnerStart) > .08f, "runner accelerates through the world");
                        MovePlayer(_game.Mission.ObjectivePosition);
                        Require(_game.Mission.TryInteract(), "runner can be intercepted");
                        Require(_game.Mission.State == MissionState.ReturnToChipo, "satchel recovery advances to Chipo");
                        Require(_game.Mission.SatchelCarrier == _game.Player.transform.name,
                            "the recovered satchel remains visible on the player");
                        Require(_game.Mission.CheckpointId == "m01_yard", "yard checkpoint saved");
                        MovePlayer(MissionDirector.ChipoPosition);
                        Require(_game.Mission.TryInteract(), "Chipo accepts the recovered satchel");
                        Require(_game.Mission.State == MissionState.Complete, "M01 completes in the live city");
                        Require(PlayerPrefs.GetInt(MissionDirector.ActiveRewardKey) == 1, "first result is committed");
                        Require(_game.Bridge.LastEvent.Contains("mission_complete"), "completion is published to Flutter");
                        Require(_game.Bridge.LastEvent.Contains("\\\"coins\\\":500"), "first result pays 500 coins");
                        Require(_game.Bridge.LastEvent.Contains("\\\"xp\\\":100"), "first result pays 100 XP");
                        _pointsAfterCompletion = _game.Progression.Points;
                        Next();
                        break;
                    case 2:
                        _game.Mission.SetActiveMission("{\"missionId\":\"M01\",\"level\":1,\"isReplay\":false}");
                        Require(_game.Progression.Points == _pointsAfterCompletion, "status reconciliation never duplicates score");
                        Require(_game.Bridge.LastEvent.Contains("mission_stage"), "mission status follows reconciliation");
                        PlayerPrefs.SetString(MissionDirector.ActiveSaveKey,
                            "{\"definitionVersion\":31,\"state\":12,\"checkpointId\":\"m01_yard\",\"openingRoute\":\"market\",\"recoveryMethod\":\"none\",\"posterSeen\":true,\"cleanRun\":true,\"elapsedSeconds\":42}");
                        PlayerPrefs.Save();
                        MissionDirector restored = new GameObject("M01 checkpoint restore validation").AddComponent<MissionDirector>();
                        restored.Configure(_game.Player, _game.FeaturedVehicle, _game.Bridge, MissionDirector.ServiceYardPosition);
                        Require(restored.State == MissionState.YardRecovery, "yard checkpoint restores its stage");
                        Require(restored.CheckpointId == "m01_yard", "yard checkpoint id restores");
                        Require(Vector3.Distance(_game.Player.transform.position, MissionDirector.ServiceYardPosition) < 6f,
                            "yard checkpoint restores the player near the yard");
                        Finish("PASS: M01 runs Rudo → theft → service route → recovery → Chipo, commits one result, publishes Flutter rewards, and restores the yard checkpoint.", 0);
                        break;
                }
            }
            catch (Exception error)
            {
                Finish("FAIL: " + error, 1);
            }
        }

        private static void MovePlayer(Vector3 position)
        {
            CharacterController body = _game.Player.GetComponent<CharacterController>();
            body.enabled = false;
            _game.Player.transform.position = position;
            body.enabled = true;
            Physics.SyncTransforms();
        }

        private static void Next()
        {
            _stage++;
            _stageStarted = Time.time;
        }

        private static void Require(bool condition, string message)
        {
            if (!condition) throw new Exception(message);
        }

        private static void Finish(string report, int exitCode)
        {
            File.WriteAllText("/private/tmp/harare-m01-validation/validation.txt", report);
            Debug.Log("[M01CampaignValidation] " + report);
            SessionState.EraseBool(Key);
            EditorApplication.isPlaying = false;
            EditorApplication.Exit(exitCode);
        }
    }
}
#endif
