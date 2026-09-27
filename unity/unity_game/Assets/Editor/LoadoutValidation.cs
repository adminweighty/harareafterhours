#if UNITY_EDITOR
using System;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Object = UnityEngine.Object;

namespace HarareAfterHours.EditorTools
{
    [InitializeOnLoad]
    public static class LoadoutValidation
    {
        private const string RunKey = "Harare.LoadoutValidation";
        private static double _deadline;
        private static float _next;
        private static int _stage;
        private static CombatTarget _target;
        private static string _saved;
        static LoadoutValidation() { EditorApplication.update += Tick; }
        public static void Run()
        {
            if (!Application.dataPath.StartsWith("/private/tmp/harare-loadout-test."))
                throw new InvalidOperationException("Run in a disposable loadout-test project only.");
            PlayerSettings.companyName = "CodexLoadoutValidation";
            PlayerSettings.productName = "Loadout-" + Guid.NewGuid().ToString("N");
            EditorSceneManager.OpenScene("Assets/Scenes/SampleScene.unity");
            SessionState.SetBool(RunKey, true);
            EditorApplication.isPlaying = true;
        }
        private static void Tick()
        {
            if (!SessionState.GetBool(RunKey, false) || !EditorApplication.isPlaying) return;
            try
            {
                if (_deadline == 0) _deadline = EditorApplication.timeSinceStartup + 120;
                Require(EditorApplication.timeSinceStartup < _deadline, "Loadout validation timeout");
                var game = Object.FindFirstObjectByType<HarareAfterHoursBootstrap>();
                if (game == null || game.Combat == null || Time.time < 3 || Time.time < _next) return;
                var loadout = game.Player.GetComponent<PlayerLoadout>();
                var animator = game.Player.GetComponentInChildren<Animator>();
                switch (_stage)
                {
                    case 0:
                        Require(loadout != null && game.Combat.EquippedWeapon == "Unarmed", "Default is unarmed");
                        foreach (string item in new[] {"blaster-a", "blaster-g"})
                        {
                            var model = Resources.Load<GameObject>("HarareEquipment/"+item);
                            Require(model != null, "Licensed blaster imported");
                            Debug.Log("[Loadout] Model " + item + " scale=" + model.transform.localScale + " bounds=" + model.GetComponentInChildren<MeshFilter>().sharedMesh.bounds);
                        }
                        Require(loadout.ApplyJson("{\"headwear\":\"Helmet\",\"eyewear\":\"Sunglasses\",\"weapon\":\"Pulse rifle\",\"outfit\":\"Festival ready\"}"), "Equip four categories");
                        _saved = loadout.ProfileJson;
                        Require(PlayerPrefs.GetString(PlayerLoadout.SaveKey) == _saved, "Local save contains selection");
                        var head = animator.GetBoneTransform(HumanBodyBones.Head);
                        Require(head.Find("Head equipment socket/Helmet") != null, "Helmet follows head");
                        Require(head.Find("Head equipment socket/Sunglasses") != null, "Sunglasses coexist with helmet");
                        Require(animator.GetBoneTransform(HumanBodyBones.RightHand).Find("Weapon grip socket/Pulse rifle") != null, "Rifle follows hand");
                        var renderer = game.Player.GetComponentInChildren<SkinnedMeshRenderer>();
                        int slot = Array.FindIndex(renderer.sharedMaterials, m => m.name.ToLowerInvariant().Contains("casualsuit"));
                        Require(slot >= 0, "Suit material exists");
                        var colors = new System.Collections.Generic.HashSet<Color>();
                        foreach (var outfit in new[] {"Delivery fit", "Street classic", "Night shift", "Festival ready"})
                        {
                            var profile = JsonUtility.FromJson<AvatarProfilePayload>(_saved);
                            profile.outfit = outfit;
                            game.Player.Avatar.ApplyProfile(profile);
                            var block = new MaterialPropertyBlock();
                            renderer.GetPropertyBlock(block, slot);
                            colors.Add(block.GetColor("_BaseColor"));
                        }
                        Require(colors.Count == 4, "All four clothes options have different material tints");
                        loadout.ApplyJson("{\"weapon\":\"Unarmed\"}", false);
                        loadout.Initialize(game.Player);
                        Require(loadout.ProfileJson == _saved, "Reinitialization restores save, not defaults");
                        game.Bridge.OnFlutterMessage("{\"type\":\"request_character_profile\"}");
                        Require(game.Bridge.LastEvent.Contains("character_profile") && game.Bridge.LastEvent.Contains("Helmet"), "Bridge returns saved loadout");
                        Require(!loadout.ApplyJson("broken JSON"), "Corrupt save rejected");
                        _next = Time.time + .4f;
                        _stage++;
                        break;
                    case 1:
                        Debug.Log("[Weapon pose] controller="+animator.runtimeAnimatorController.name+" updates="+game.Player.GetComponent<PlayerEquipmentVisual>().HandPoseUpdates+" hand="+game.Player.transform.InverseTransformPoint(animator.GetBoneTransform(HumanBodyBones.RightHand).position));
                        Capture(game.Player, "front", new Vector3(0,1.45f,2.3f), new Vector3(0,1.3f,0));
                        Capture(game.Player, "side", new Vector3(2,1.5f,1.2f), new Vector3(0,1.25f,0));
                        Capture(game.Player, "shoulder", game.Player.transform.InverseTransformPoint(Camera.main.transform.position), game.Player.transform.InverseTransformPoint(Camera.main.transform.position + Camera.main.transform.forward*8));
                        Require(game.Player.GetComponent<PlayerEquipmentVisual>().HandPoseUpdates > 0 && game.Player.transform.InverseTransformPoint(animator.GetBoneTransform(HumanBodyBones.RightHand).position).y > 1.20f, "Armed hand is raised by humanoid IK");
                        Place(game.Player, new Vector3(300, 1, 300));
                        game.Player.enabled = false;
                        var targetObject = GameObject.CreatePrimitive(PrimitiveType.Cube);
                        targetObject.transform.position = new Vector3(300, 2.05f, 335);
                        _target = targetObject.AddComponent<CombatTarget>();
                        _target.Configure(null, 6);
                        _target.enabled = false;
                        game.Combat.Equip("Pulse pistol");
                        _next = Time.time + .5f;
                        _stage++;
                        break;
                    case 2:
                        Aim(game.Player);
                        game.Combat.TryAttack();
                        Require(_target.RemainingIntegrity == 6, "Pistol cannot hit a 35m target");
                        game.Combat.Equip("Pulse rifle");
                        _next = Time.time + .5f;
                        _stage++;
                        break;
                    case 3:
                        Aim(game.Player);
                        game.Combat.TryAttack();
                        Require(_target.RemainingIntegrity == 5, "Rifle hits a target beyond pistol range");
                        var feedback = game.Player.GetComponent<WeaponFeedback>();
                        Require(feedback.HitConfirmed && feedback.FlashVisible, "Hit confirmation and muzzle flash appear on a successful shot");
                        Require(Vector3.Distance(feedback.TracerOrigin,game.Player.GetComponent<PlayerEquipmentVisual>().Muzzle.position)<.01f, "Tracer starts at muzzle, not camera");
                        game.Combat.TryAttack();
                        Require(_target.RemainingIntegrity == 5, "Cooldown prevents repeated attack in same frame");
                        _next = Time.time + .5f;
                        _stage++;
                        break;
                    case 4:
                        Aim(game.Player);
                        Time.timeScale = 0;
                        game.Combat.TryAttack();
                        Require(_target.RemainingIntegrity == 5, "Pause prevents attack");
                        Time.timeScale = 1;
                        game.Player.InputLocked = true;
                        game.Combat.TryAttack();
                        Require(_target.RemainingIntegrity == 5, "Locked input prevents attack");
                        game.Player.InputLocked = false;
                        var wall = GameObject.CreatePrimitive(PrimitiveType.Cube);
                        wall.transform.position = game.Player.transform.position + new Vector3(0,1,1.5f);
                        wall.transform.localScale = new Vector3(4,4,.2f);
                        Physics.SyncTransforms();
                        game.Combat.TryAttack();
                        Require(_target.RemainingIntegrity == 5, "Nearby wall blocks the muzzle shot");
                        wall.SetActive(false); Object.Destroy(wall);
                        var reduced = game.Player.GetComponent<WeaponFeedback>();
                        reduced.Configure(true,true,true);
                        reduced.Fire(game.Player.GetComponent<PlayerEquipmentVisual>().Muzzle,Vector3.zero,Vector3.one,true);
                        Require(!reduced.FlashVisible && !game.Player.GetComponent<AudioSource>().isPlaying, "Reduced effects and mute disable flash/audio");
                        _target.transform.position = game.Player.transform.position + new Vector3(0,1.05f,2);
                        game.Combat.Equip("Baton");
                        _next = Time.time + .8f;
                        _stage++;
                        break;
                    case 5:
                        Physics.SyncTransforms();
                        game.Combat.TryAttack();
                        Require(_target.RemainingIntegrity == 2, "Baton deals three integrity damage in melee range");
                        game.Combat.Equip("Unarmed");
                        _next = Time.time + .8f;
                        _stage++;
                        break;
                    case 6:
                        game.Combat.TryAttack();
                        Require(_target.RemainingIntegrity == 0, "Unarmed uses melee rather than firing a hidden weapon");
                        BloodCombatValidation.Check(game);
                        Require(loadout.ApplyJson("{\"weapon\":\"invalid\",\"headwear\":\"invalid\",\"eyewear\":\"invalid\"}",false), "Normalize unsupported values");
                        Require(game.Combat.EquippedWeapon == "Unarmed" && loadout.ProfileJson.Contains("None"), "Unsupported slots fall back safely");
                        loadout.ApplyJson(_saved, false);
                        Place(game.Player, MissionDirector.GaragePosition + Vector3.back);
                        Require(game.Mission.TryInteract(), "Collect car keys for equipment seat test");
                        Place(game.Player, game.FeaturedVehicle.transform.position + Vector3.left*2);
                        Require(game.Player.TryBoardNearest(false), "Board car with weapon equipped");
                        _next = Time.time + .5f;
                        _stage++;
                        break;
                    case 7:
                        var grip = animator.GetBoneTransform(HumanBodyBones.RightHand).Find("Weapon grip socket/Pulse rifle");
                        Require(grip != null && !grip.gameObject.activeSelf, "Weapon hidden while seated");
                        _target.Configure(null, 6);
                        _target.transform.position = game.Player.transform.position + Vector3.forward*10 + Vector3.up*1.05f;
                        Aim(game.Player);
                        game.Combat.TryAttack();
                        Require(_target.RemainingIntegrity == 6, "Cannot fire from car");
                        Debug.Log("[Loadout] PASS: import, bones, IK grip pose, simultaneous accessories, four clothes, save/restore, bridge, range, cooldown, muzzle tracer, hit/flash, wall blocking, reduced effects/mute, pause/input guards, melee, vehicle holster/fire guard.");
                        Finish(0);
                        break;
                }
            }
            catch (Exception exception) { Debug.LogException(exception); Finish(1); }
        }
        private static void Aim(ThirdPersonController player)
        {
            Camera.main.transform.SetPositionAndRotation(player.transform.position + Vector3.up*1.05f, Quaternion.identity);
            Physics.SyncTransforms();
        }
        private static void Place(ThirdPersonController player, Vector3 position)
        {
            var body = player.GetComponent<CharacterController>();
            body.enabled = false;
            player.transform.SetPositionAndRotation(position, Quaternion.identity);
            body.enabled = true;
            Physics.SyncTransforms();
        }
        private static void Capture(ThirdPersonController player, string name, Vector3 offset, Vector3 look)
        {
            var camera = new GameObject("Loadout QA camera").AddComponent<Camera>();
            camera.CopyFrom(Camera.main);
            camera.enabled = false;
            camera.transform.position = player.transform.TransformPoint(offset);
            camera.transform.LookAt(player.transform.TransformPoint(look));
            camera.fieldOfView = 42;
            var target = new RenderTexture(720,900,24);
            camera.targetTexture = target;
            camera.Render();
            var previous = RenderTexture.active;
            RenderTexture.active = target;
            var image = new Texture2D(720,900,TextureFormat.RGB24,false);
            image.ReadPixels(new Rect(0,0,720,900),0,0); image.Apply();
            File.WriteAllBytes("/private/tmp/harare-loadout-"+name+".png",image.EncodeToPNG());
            RenderTexture.active = previous;
            camera.targetTexture = null;
            Object.Destroy(image); Object.Destroy(target); Object.Destroy(camera.gameObject);
        }
        private static void Require(bool okay, string message) { if (!okay) throw new Exception(message); }
        private static void Finish(int code)
        {
            Time.timeScale = 1;
            SessionState.EraseBool(RunKey);
            EditorApplication.isPlaying = false;
            EditorApplication.Exit(code);
        }
    }
}
#endif
