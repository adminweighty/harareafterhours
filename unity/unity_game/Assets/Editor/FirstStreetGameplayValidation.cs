#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Profiling;

namespace HarareAfterHours.EditorTools
{
    [InitializeOnLoad]
    public static class FirstStreetGameplayValidation
    {
        const string Key = "Harare.FirstStreetGameplayValidation";
        static int stage;
        static float started;
        static double deadline;
        static Vector3 startPosition;
        static Vector3[] npcPositions, trafficPositions;
        static NpcWanderer[] npcs;
        static TrafficVehicle[] traffic;
        static readonly List<string> report = new();
        static readonly List<float> frameTimes = new();
        static string runtimeError;
        static int lastFrame;

        static FirstStreetGameplayValidation()
        {
            EditorApplication.update += Tick;
            Application.logMessageReceived += (message, stack, type) =>
            {
                if (SessionState.GetBool(Key, false) && (type == LogType.Exception || type == LogType.Error || type == LogType.Assert)) runtimeError = message;
            };
        }
        public static void Run()
        {
            EditorSceneManager.OpenScene("Assets/Scenes/SampleScene.unity");
            SessionState.SetBool(Key, true);
            EditorApplication.isPlaying = true;
        }
        static void Tick()
        {
            if (!SessionState.GetBool(Key, false) || !EditorApplication.isPlaying) return;
            try
            {
                if (deadline == 0) deadline = EditorApplication.timeSinceStartup + 100;
                Require(EditorApplication.timeSinceStartup < deadline, "Validation timeout");
                Require(runtimeError == null, "Runtime error: " + runtimeError);
                if (lastFrame == Time.frameCount) return;
                lastFrame = Time.frameCount;
                var game = UnityEngine.Object.FindFirstObjectByType<HarareAfterHoursBootstrap>();
                if (game == null || game.Player == null || Time.time < 2) return;
                switch (stage)
                {
                    case 0:
                        ValidateScene(game);
                        npcs = UnityEngine.Object.FindObjectsByType<NpcWanderer>(FindObjectsSortMode.InstanceID);
                        traffic = UnityEngine.Object.FindObjectsByType<TrafficVehicle>(FindObjectsSortMode.InstanceID).Where(t=>t.IsAmbient).ToArray();
                        npcPositions = npcs.Select(n => n.transform.position).ToArray();
                        trafficPositions = traffic.Select(t => t.transform.position).ToArray();
                        Camera.main.GetComponent<ThirdPersonCameraRig>().enabled = false;
                        Camera.main.transform.SetPositionAndRotation(new Vector3(8.8f, 2.1f, 17), Quaternion.identity);
                        PlacePlayer(game.Player, new Vector3(8.8f, .3f, 20));
                        startPosition = game.Player.transform.position;
                        Next();
                        break;
                    case 1:
                        MobileControlState.SetMove(Vector2.up);
                        if (Time.time - started < 1.2f) break;
                        Require(game.Player.transform.position.z - startPosition.z > 1.8f, "Player cannot walk along the patched pavement at the corrected 1.8m/s walking speed");
                        Require(game.Player.transform.position.y > .12f && game.Player.transform.position.y < .4f, "Pavement support/collision failed");
                        report.Add("PASS: player walks >1.8m on the new pavement with normal movement input (1.8m/s walking speed).");
                        Require(npcs.Where((npc, i) => Vector3.Distance(npc.transform.position, npcPositions[i]) > .1f).Count() >= 20, "NPC movement regression");
                        Require(traffic.Where((car, i) => Vector3.Distance(car.transform.position, trafficPositions[i]) > .2f).Count() == 3, "Traffic movement regression");
                        report.Add("PASS: all 23 NPCs retained; at least 20 moved during sample. All three traffic vehicles moved.");
                        MobileControlState.SetMove(Vector2.zero);
                        PlacePlayer(game.Player, game.FeaturedVehicle.transform.position - game.FeaturedVehicle.transform.right * 2.5f + Vector3.up * .1f);
                        MobileControlState.RequestInteract();
                        Next();
                        break;
                    case 2:
                        if (Time.time - started < .25f) break;
                        Require(game.Player.IsDriving && game.Mission.State == MissionState.DriveToMarket, "Vehicle entry or mission transition failed");
                        startPosition = game.FeaturedVehicle.transform.position;
                        report.Add("PASS: normal USE input enters the existing vehicle and advances the mission.");
                        Next();
                        break;
                    case 3:
                        MobileControlState.SetMove(Vector2.up);
                        if (Time.time - started < 1.2f) break;
                        Require(Vector3.Distance(startPosition, game.FeaturedVehicle.transform.position) > .5f, "Driving input did not move the vehicle");
                        report.Add("PASS: throttle input moves the existing vehicle.");
                        MobileControlState.SetMove(Vector2.zero);
                        MobileControlState.RequestInteract();
                        Next();
                        break;
                    case 4:
                        MobileControlState.SetBrake();
                        if(game.Player.IsDriving)
                        {
                            Require(Time.time-started<4,"Car could not brake and exit safely");
                            if(game.FeaturedVehicle.SpeedKph<2.5f) MobileControlState.RequestInteract();
                            break;
                        }
                        if (Time.time - started < .25f) break;
                        Require(!game.Player.IsDriving && game.Player.GetComponent<CharacterController>().enabled, "Vehicle exit failed");
                        report.Add("PASS: normal USE input exits the vehicle and restores the character controller.");
                        var body = game.FeaturedVehicle.GetComponent<Rigidbody>();
                        body.linearVelocity = Vector3.zero;
                        body.angularVelocity = Vector3.zero;
                        body.position = new Vector3(42, .3f, -27);
                        Physics.SyncTransforms();
                        Next();
                        break;
                    case 5:
                        if (Time.time - started < .4f) break;
                        Require(game.Mission.State == MissionState.Complete, "Existing checkpoint no longer completes the mission");
                        report.Add("PASS: vehicle overlapping the unchanged market trigger completes the mission (checkpoint teleported for this regression check).");
                        PlacePlayer(game.Player, new Vector3(8.8f, .3f, 20));
                        Camera.main.transform.position = new Vector3(5, 1.8f, 9);
                        Camera.main.transform.LookAt(new Vector3(12, 3, 28));
                        Next();
                        break;
                    case 6:
                        if (Time.time - started > 1 && frameTimes.Count < 180) frameTimes.Add(Time.unscaledDeltaTime * 1000);
                        if (frameTimes.Count < 180) break;
                        float[] times = frameTimes.OrderBy(t => t).ToArray();
                        report.Add($"Editor CPU/wall-frame sample, 180 frames: median {times[90]:F2}ms; p95 {times[171]:F2}ms. Not a physical mobile GPU benchmark.");
                        report.Add($"Unity total allocated memory: {Profiler.GetTotalAllocatedMemoryLong() / (1024 * 1024)} MiB (whole editor scene, not incremental block cost).");
                        report.Add("PASS: no runtime errors or exceptions during validation.");
                        Finish(0);
                        break;
                }
            }
            catch (Exception error)
            {
                report.Add("FAIL: " + error.Message);
                Debug.LogException(error);
                Finish(1);
            }
        }
        static void ValidateScene(HarareAfterHoursBootstrap game)
        {
            var blocks = UnityEngine.Object.FindObjectsByType<FirstStreetBlock>(FindObjectsSortMode.None);
            Require(blocks.Length == 1, "Block duplicated or missing");
            Require(game.Characters.IsReady && game.Characters.CastCount == 24, "Character roster changed");
            Require(UnityEngine.Object.FindObjectsByType<NpcWanderer>(FindObjectsSortMode.None).Length == 23, "NPC count changed");
            Require(UnityEngine.Object.FindObjectsByType<TrafficVehicle>(FindObjectsSortMode.None).Count(t=>t.IsAmbient) == 3, "Traffic count changed");
            Require(game.Combat != null && game.Progression != null && game.Bridge != null && game.ArtLibrary != null, "Gameplay services missing");
            Require(UnityEngine.Object.FindFirstObjectByType<GameplayHud>() != null, "HUD missing");
            Require(game.ArtLibrary.GetComponents<AudioSource>().Any(a => a.isPlaying), "Music stopped");
            Require(UnityEngine.Object.FindObjectsByType<AudioListener>(FindObjectsSortMode.None).Count(a => a.enabled) == 1, "Audio listener changed");
            Require(game.Mission.State == MissionState.WalkToVehicle, "Initial mission state changed");
            var block = blocks[0];
            Require(Vector3.Distance(block.transform.position, FirstStreetBlock.PlotCentre) < .001f, "Plot centre moved");
            Require(block.GetComponentsInChildren<MeshCollider>().Length == 0, "Complex collisions added");
            Require(block.GetComponentsInChildren<Light>().Length == 0, "Realtime lights added");
            foreach (Renderer renderer in block.GetComponentsInChildren<Renderer>())
                Require(renderer.sharedMaterials.All(m => m != null && m.shader != null && !m.shader.name.Contains("Error")), "Missing material/shader");
            foreach (LODGroup group in block.GetComponentsInChildren<LODGroup>())
                foreach (LOD lod in group.GetLODs()) Require(lod.renderers.All(r => r != null), "LOD references missing");
            foreach (Collider c in block.GetComponentsInChildren<Collider>())
                Require(c.bounds.min.x >= 6.05f && c.bounds.min.z >= 6.05f, "Block collision obstructs carriageway");
            Require(Physics.Raycast(new Vector3(11.7f, 1.3f, 21), Vector3.right, out RaycastHit hit, 4) && hit.collider.GetComponentInParent<FirstStreetBlock>() != null, "Closed shop wall has no collision");
            var lamps = UnityEngine.Object.FindObjectsByType<Transform>(FindObjectsSortMode.None).Where(t => t.name == "Street light").ToArray();
            Require(lamps.Length == 21 && lamps.Select(t => t.position).Distinct().Count() == 21, "Streetlights overlap at origin");
            report.Add("PASS: single reference block at original plot centre; all LOD/material/collision checks pass; carriageways clear.");
            report.Add("PASS: 24 cast roles, 23 NPCs, three traffic vehicles, combat, progression, mission, HUD, audio and bridge present.");
            report.Add("PASS: 21 existing streetlights retain their intended distinct street positions.");
        }
        static void PlacePlayer(ThirdPersonController player, Vector3 p)
        {
            var controller = player.GetComponent<CharacterController>();
            controller.enabled = false;
            player.transform.position = p;
            controller.enabled = true;
            Physics.SyncTransforms();
        }
        static void Next() { stage++; started = Time.time; }
        static void Require(bool condition, string message) { if (!condition) throw new InvalidOperationException(message); }
        static void Finish(int code)
        {
            File.WriteAllLines("Assets/Environment/Reports/FirstStreet-gameplay-validation.txt", report);
            Debug.Log("[FirstStreetGameplay] " + (code == 0 ? "PASS" : "FAIL") + "\n" + string.Join("\n", report));
            SessionState.EraseBool(Key);
            EditorApplication.isPlaying = false;
            EditorApplication.Exit(code);
        }
    }
}
#endif
