#if UNITY_EDITOR
using System;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace HarareAfterHours.EditorTools
{
    /// <summary>Run only in a disposable project copy; uses isolated save data.</summary>
    [InitializeOnLoad]
    public static class CityStartupValidation
    {
        private const string RunKey = "Harare.CityStartupValidation";
        private static double _deadline;
        private static int _stage;
        static CityStartupValidation() { EditorApplication.update += Tick; }

        public static void Run()
        {
            if (!Application.dataPath.StartsWith("/private/tmp/harare-city-start."))
                throw new InvalidOperationException("Use a disposable city-start project copy for this test.");
            PlayerSettings.companyName = "CodexCityValidation";
            PlayerSettings.productName = "CityStartup-" + Guid.NewGuid().ToString("N");
            EditorSceneManager.OpenScene("Assets/Scenes/SampleScene.unity");
            SessionState.SetBool(RunKey, true);
            EditorApplication.isPlaying = true;
        }

        private static void Tick()
        {
            if (!SessionState.GetBool(RunKey, false) || !EditorApplication.isPlaying) return;
            try
            {
                if (_deadline == 0) _deadline = EditorApplication.timeSinceStartup + 90;
                Require(EditorApplication.timeSinceStartup < _deadline, "Startup timed out");
                var game = UnityEngine.Object.FindFirstObjectByType<HarareAfterHoursBootstrap>();
                if (game == null || game.Mission == null || Time.time < 3) return;
                if (_stage == 0)
                {
                    Require(game.Player != null && Camera.main != null && GameObject.Find("Tino") != null, "City, player, camera and Tino exist");
                    Require(game.Mission.State == MissionState.MeetTino, "First objective must be Tino, not delivery");
                    Require(!game.Mission.TryInteract(), "Cannot collect keys from across the city");
                    Require(!game.Mission.CanEnterVehicle(game.FeaturedVehicle), "Keys required for mission car");
                    Place(game.Player, MissionDirector.GaragePosition + Vector3.back);
                    Require(game.Mission.TryInteract(), "Talk to Tino at garage");
                    Require(game.Progression.Points == 100 && game.Mission.State == MissionState.WalkToVehicle, "Keys reward and next stage");
                    Require(!game.Mission.TryInteract(), "Dialogue reward cannot repeat");
                    Place(game.Player, game.FeaturedVehicle.transform.position + Vector3.left * 2);
                    Require(game.Player.TryBoardNearest(false), "Enter unlocked car");
                    Require(game.Mission.State == MissionState.DriveToMarket && game.Progression.Points == 250, "Driving stage and points");
                    game.FeaturedVehicle.GetComponent<Rigidbody>().linearVelocity = Vector3.forward * 10;
                    game.Mission.TryComplete(game.FeaturedVehicle.GetComponentInChildren<Collider>());
                    Require(game.Mission.State == MissionState.DriveToMarket, "Must slow down at the finish");
                    _stage++;
                    return;
                }
                if (_stage == 1)
                {
                    var body = game.FeaturedVehicle.GetComponent<Rigidbody>();
                    body.linearVelocity = Vector3.zero;
                    body.angularVelocity = Vector3.zero;
                    body.position = game.Mission.ObjectivePosition + Vector3.up * .2f;
                    Physics.SyncTransforms();
                    _stage++;
                    return;
                }
                if (game.Mission.State != MissionState.Complete) return;
                Require(game.Progression.Points == 1000, "Clean mission pays exactly 1000 total points");
                game.Mission.TryComplete(game.FeaturedVehicle.GetComponentInChildren<Collider>());
                game.Progression.AwardMissionStep("city_wakes_complete", 750);
                Require(game.Progression.Points == 1000, "Repeated completion cannot duplicate points");
                Require(Time.timeScale == 1 && game.Player.enabled, "Completion keeps city playable");
                Require(!GameObject.Find("Current objective marker"), "Finished objective marker hidden");
                Require(PlayerPrefs.GetInt(MissionDirector.SaveKey) == (int)MissionState.Complete, "Mission saved");
                var check = new GameObject("Score reload check");
                var loaded = check.AddComponent<PlayerProgression>();
                loaded.Configure(null);
                Require(loaded.Points == 1000, "Points reload from local save");
                var restoredMission = new GameObject("Mission reload check").AddComponent<MissionDirector>();
                restoredMission.Configure(game.Player, game.FeaturedVehicle, game.Bridge, new Vector3(42, .1f, -27));
                Require(restoredMission.State == MissionState.Complete, "Completed mission reloads into free roam");
                Debug.Log("[CityStartup] PASS: direct city, Tino, keys, car, checkpoint trigger, 1000 points, duplicate protection, save reload, free roam.");
                Finish(0);
            }
            catch (Exception exception) { Debug.LogException(exception); Finish(1); }
        }

        private static void Place(ThirdPersonController player, Vector3 position)
        {
            var controller = player.GetComponent<CharacterController>();
            controller.enabled = false;
            player.transform.position = position;
            controller.enabled = true;
            Physics.SyncTransforms();
        }
        private static void Require(bool ok, string message) { if (!ok) throw new Exception(message); }
        private static void Finish(int code)
        {
            SessionState.EraseBool(RunKey);
            EditorApplication.isPlaying = false;
            EditorApplication.Exit(code);
        }
    }
}
#endif
