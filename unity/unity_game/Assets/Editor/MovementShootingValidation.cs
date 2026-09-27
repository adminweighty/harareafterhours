#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace HarareAfterHours.EditorTools
{
    [InitializeOnLoad]
    public static class MovementShootingValidation
    {
        const string Key = "Harare.MovementShootingQA", Folder = "/private/tmp/harare-movement-shooting/";
        static readonly string[] Strings = {"Harare.Graphics.v1", PlayerLoadout.SaveKey, TouchGameplayControls.SettingsKey, MissionDirector.SaveKey, PlayerProgression.SaveKey};
        static readonly string[] Ints = {JoinaCityBlock.IntroSaveKey, PlayerLoadout.StarterWeaponKey, MissionDirector.RewardKey};
        static int stage;
        static double deadline;
        static string error;
        static MovementShootingValidation()
        {
            EditorApplication.update += Tick;
            Application.logMessageReceived += (message, stack, kind) =>
            { if(SessionState.GetBool(Key,false) && (kind==LogType.Error || kind==LogType.Exception)) error=message; };
        }
        public static void Run() => Start(false);
        public static void Capture() => Start(true);
        static void Start(bool capture)
        {
            Directory.CreateDirectory(Folder);
            File.Delete(Folder+"gameplay.png");File.Delete(Folder+"character.png");
            File.WriteAllText(Folder+"validation.txt", "Starting visibility validation\n");
            foreach(var key in Strings){SessionState.SetBool(Key+key+"Exists",PlayerPrefs.HasKey(key));SessionState.SetString(Key+key,PlayerPrefs.GetString(key,""));}
            foreach(var key in Ints){SessionState.SetBool(Key+key+"Exists",PlayerPrefs.HasKey(key));SessionState.SetInt(Key+key,PlayerPrefs.GetInt(key));}
            PlayerPrefs.DeleteKey(MissionDirector.ActiveSaveKey);PlayerPrefs.DeleteKey(MissionDirector.ActiveRewardKey);PlayerPrefs.DeleteKey(PlayerProgression.ActiveSaveKey);
            PlayerPrefs.SetString("Harare.Graphics.v1","Balanced");PlayerPrefs.Save();
            SessionState.SetBool(Key+"Capture",capture);
            EditorSceneManager.OpenScene("Assets/Scenes/SampleScene.unity");
            if(capture)
            {
                var view=EditorWindow.GetWindow(Type.GetType("UnityEditor.GameView,UnityEditor"));
                view.position=new Rect(30,50,1280,780);view.Show();view.Focus();
            }
            SessionState.SetBool(Key,true);EditorApplication.isPlaying=true;
        }
        static void Check(bool condition,string message){if(!condition)throw new Exception(message);}
        static void Tick()
        {
            if(!SessionState.GetBool(Key,false)||!EditorApplication.isPlaying)return;
            if(deadline==0)deadline=EditorApplication.timeSinceStartup+90;
            try
            {
                Check(EditorApplication.timeSinceStartup<deadline,"Visibility validation timeout at stage "+stage+" frame "+Time.frameCount+" time "+Time.time);
                Check(error==null,error);
                Application.runInBackground=true;
                EditorApplication.isPaused=false;
                if(Time.time<4)return;
                var game=HarareAfterHoursBootstrap.Instance;
                if(stage==0)
                {
                    Check(game!=null&&game.Player!=null,"World/player loaded");
                    var normals=new[]{Vector3.up,Vector3.down,Vector3.forward,Vector3.back,Vector3.left,Vector3.right};
                    var samples=new Color[normals.Length];RenderSettings.ambientProbe.Evaluate(normals,samples);
                    File.AppendAllText(Folder+"validation.txt","Ambient: "+string.Join(", ",samples.Select(c=>c.grayscale.ToString("F3")))+"\n");
                    Check(samples.All(c=>c.grayscale>.30f),"Diffuse light reaches all six sides, including faces away from the sun");
                    var characters=UnityEngine.Object.FindObjectsByType<RuntimeCharacterVisual>(FindObjectsSortMode.None);
                    Check(characters.Length>=25,"Player, mission cast and street NPCs instantiated");
                    int meshes=0;
                    foreach(var character in characters)
                    {
                        var renderers=character.GetComponentsInChildren<SkinnedMeshRenderer>(true);
                        Check(renderers.Length>0,"Character has skinned geometry: "+character.name);
                        foreach(var renderer in renderers)
                        {
                            Check(!renderer.forceRenderingOff,"Character not forcibly hidden: "+character.name);
                            Check(renderer.sharedMesh!=null&&renderer.sharedMesh.vertexCount>0,"Character mesh exists");
                            foreach(var material in renderer.sharedMaterials)
                                Check(material!=null&&material.shader!=null&&material.shader.isSupported&&material.shader.name!="Hidden/InternalErrorShader","Character shader supported: "+character.name);
                            meshes++;
                        }
                        foreach(var group in character.GetComponentsInChildren<LODGroup>())
                            Check(group.GetLODs().Last().screenRelativeTransitionHeight==0f,"No character LOD distance cutoff");
                    }
                    Check(UnityEngine.Object.FindObjectsByType<CombatTarget>(FindObjectsSortMode.None).Length==0,"No brown practice bags on streets");
                    ValidateMovement(game);
                    ValidateWalkingAndShots(game);
                    File.WriteAllText(Folder+"validation.txt",$"PASS: {characters.Length} characters / {meshes} skinned meshes; supported materials; no forcibly hidden bodies; distant LOD retained; six-direction ambient minimum {samples.Min(c=>c.grayscale):F3}. Armed cardinal movement/camera, walking speed, held sprint, release-to-walk, stop distance, actual hostile hits at 2/5/50m and weapon limits, wall blocking and 50m hip-fire assistance passed.\n");
                    if(!SessionState.GetBool(Key+"Capture",false)){Finish(0);return;}
                    ScreenCapture.CaptureScreenshot(Folder+"gameplay.png");stage=1;
                }
                else if(stage==1 && File.Exists(Folder+"gameplay.png"))
                {
                    // A close front view makes skin, clothing and held equipment reviewable.
                    var camera=Camera.main;camera.GetComponent<ThirdPersonCameraRig>().enabled=false;
                    camera.transform.position=game.Player.transform.position+new Vector3(1.8f,1.5f,3.5f);
                    camera.transform.LookAt(game.Player.transform.position+Vector3.up*.95f);
                    camera.fieldOfView=48;stage=2;
                }
                else if(stage==2){ScreenCapture.CaptureScreenshot(Folder+"character.png");stage=3;}
                else if(stage==3&&File.Exists(Folder+"character.png"))Finish(0);
            }
            catch(Exception e){File.AppendAllText(Folder+"validation.txt","FAIL: "+e);Finish(1);}
        }
        static void ValidateMovement(HarareAfterHoursBootstrap game)
        {
            var player=game.Player;var body=player.GetComponent<CharacterController>();
            var combat=player.GetComponent<PlayerCombat>();var rig=Camera.main.GetComponent<ThirdPersonCameraRig>();
            var position=player.transform.position;var rotation=player.transform.rotation;string weapon=combat.EquippedWeapon;
            var floor=GameObject.CreatePrimitive(PrimitiveType.Cube);
            floor.transform.position=new Vector3(1000,-.5f,1000);floor.transform.localScale=new Vector3(100,1,100);
            try
            {
                foreach(string equipped in new[]{"Pulse pistol","Pulse rifle"})
                foreach(Vector2 input in new[]{Vector2.right,Vector2.left,Vector2.down,Vector2.up})
                {
                    combat.Equip(equipped);body.enabled=false;
                    player.transform.SetPositionAndRotation(new Vector3(1000,.1f,1000),Quaternion.identity);body.enabled=true;
                    rig.SetTarget(player.transform,false);MobileControlState.SetMove(Vector2.zero);player.SendMessage("Update");
                    Physics.SyncTransforms();Vector3 origin=player.transform.position;
                    for(int i=0;i<240;i++){MobileControlState.SetMove(input);player.SendMessage("Update");rig.SendMessage("LateUpdate");}
                    Vector3 expected=new Vector3(input.x,0,input.y), travelled=player.transform.position-origin;travelled.y=0;
                    Check(Vector3.Dot(travelled.normalized,expected)>.97f,"Held armed input stays straight: "+equipped+input);
                    Check(Vector3.Dot(player.transform.forward,expected)>.98f,"Body turns toward movement: "+equipped+input);
                    Check(Vector3.Dot(rig.FlatForward,expected)>.95f,"Camera follows armed player: "+equipped+input);
                }
                combat.ToggleAim();rig.SetTarget(player.transform,false);Vector3 facing=rig.FlatForward;
                for(int i=0;i<60;i++){MobileControlState.SetMove(Vector2.right);player.SendMessage("Update");rig.SendMessage("LateUpdate");}
                Check(Vector3.Dot(player.transform.forward,facing)>.95f,"Aiming retains camera-facing strafe");
            }
            finally
            {
                MobileControlState.Reset();combat.Equip(weapon);body.enabled=false;player.transform.SetPositionAndRotation(position,rotation);body.enabled=true;
                rig.SetTarget(player.transform,false);rig.SendMessage("LateUpdate");UnityEngine.Object.DestroyImmediate(floor);Physics.SyncTransforms();
            }
        }

        static void Set(object obj,string field,object value)=>obj.GetType().GetField(field,System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.Instance).SetValue(obj,value);
        static void ValidateWalkingAndShots(HarareAfterHoursBootstrap game)
        {
            var player=game.Player;var body=player.GetComponent<CharacterController>();var combat=game.Combat;
            var rig=Camera.main.GetComponent<ThirdPersonCameraRig>();var camera=Camera.main;var touch=TouchGameplayControls.Instance;
            Vector3 saved=player.transform.position;Quaternion savedRotation=player.transform.rotation;string weapon=combat.EquippedWeapon;
            var actor=StreetActionDirector.Instance.Actors.Find(a=>!a.Officer);bool actorEnabled=actor.enabled;
            Vector3 actorPosition=actor.transform.position;Quaternion actorRotation=actor.transform.rotation;
            var floor=GameObject.CreatePrimitive(PrimitiveType.Cube);floor.transform.position=new Vector3(1000,-.5f,1130);floor.transform.localScale=new Vector3(100,1,400);
            var wall=GameObject.CreatePrimitive(PrimitiveType.Cube);wall.transform.localScale=new Vector3(10,10,1);wall.SetActive(false);
            try
            {
                actor.enabled=false;combat.Equip("Pulse pistol");
                body.enabled=false;player.transform.SetPositionAndRotation(new Vector3(1000,.1f,1000),Quaternion.identity);body.enabled=true;rig.SetTarget(player.transform,false);Physics.SyncTransforms();
                touch.Cancel();var up=touch.ButtonRect(TouchGameplayControls.Action.Up).center;var sprint=touch.ButtonRect(TouchGameplayControls.Action.Sprint).center;
                touch.ProcessPointer(-90,up,Vector2.zero,true,true);touch.ApplyHeld();Check(!MobileControlState.Sprint,"Direction alone does not sprint");
                for(int i=0;i<120;i++){touch.ApplyHeld();player.SendMessage("Update");}
                Check(Mathf.Abs(player.CurrentSpeed-1.8f)<.12f,"Normal movement walks at 1.8 m/s: "+player.CurrentSpeed);
                touch.ProcessPointer(-91,sprint,Vector2.zero,true,true);touch.ApplyHeld();Check(MobileControlState.Sprint,"Held sprint works with D-pad");
                for(int i=0;i<120;i++){touch.ApplyHeld();player.SendMessage("Update");}
                Check(Mathf.Abs(player.CurrentSpeed-4.5f)<.12f,"Explicit sprint speed");
                touch.ProcessPointer(-91,sprint,Vector2.zero,false,false);touch.ApplyHeld();Check(!MobileControlState.Sprint,"Sprint release does not latch");
                for(int i=0;i<120;i++){touch.ApplyHeld();player.SendMessage("Update");}
                Check(Mathf.Abs(player.CurrentSpeed-1.8f)<.12f,"Release returns to walk");
                Vector3 stop=player.transform.position;touch.Cancel();
                for(int i=0;i<30;i++)player.SendMessage("Update");
                Check(Vector3.Distance(stop,player.transform.position)<.25f,"Movement stops promptly");
                foreach(string equipped in new[]{"Pulse pistol","Pulse rifle"})
                {
                    combat.Equip(equipped);float limit=combat.AttackRange;
                    foreach(float distance in new[]{2f,5f,50f,limit-2,limit+5})
                    {
                        actor.ResetEncounter();Place(player,new Vector3(1000,.1f,1000));Place(actor,new Vector3(1000,.1f,1000+distance));
                        camera.transform.position=player.transform.position+new Vector3(.7f,2.6f,-7.2f);camera.transform.LookAt(actor.transform.position+Vector3.up*1.2f);
                        Physics.SyncTransforms();Set(combat,"_nextPulseAt",Time.time-1);combat.TryAttack();
                        Check(actor.HitsRemaining==(distance<limit?2:3),equipped+" actual hostile hit at "+distance+"m");
                        if(distance>=50 && distance<limit)
                        {
                            var feedback=player.GetComponent<WeaponFeedback>();
                            var flags=System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic;
                            float length=(float)typeof(WeaponFeedback).GetField("_shotDistance",flags).GetValue(feedback);
                            float expires=(float)typeof(WeaponFeedback).GetField("_traceUntil",flags).GetValue(feedback);
                            Check(expires-feedback.LastShotTime>length/300f,"Tracer lives until impact at "+distance+"m");
                            Set(feedback,"<LastShotTime>k__BackingField",Time.time-length/300f-.001f);
                            feedback.SendMessage("LateUpdate");
                            var tracer=player.GetComponentInChildren<LineRenderer>();
                            var end=(Vector3)typeof(WeaponFeedback).GetField("_shotEnd",flags).GetValue(feedback);
                            Check(tracer.enabled&&Vector3.Distance(tracer.GetPosition(1),end)<.02f,"Animated tracer reaches distant hit point");
                        }
                    }
                    actor.ResetEncounter();Place(actor,new Vector3(1000,.1f,1050));camera.transform.LookAt(actor.transform.position+Vector3.up*1.2f);
                    wall.transform.position=new Vector3(1000,2,1025);wall.SetActive(true);Physics.SyncTransforms();Set(combat,"_nextPulseAt",Time.time-1);combat.TryAttack();
                    Check(actor.HitsRemaining==3,"Cover blocks direct and assisted "+equipped);wall.SetActive(false);
                    actor.ResetEncounter();Place(actor,new Vector3(1000,.1f,1050));player.transform.rotation=Quaternion.identity;rig.SetTarget(player.transform,false);rig.SendMessage("LateUpdate");Physics.SyncTransforms();
                    Set(combat,"_nextPulseAt",Time.time-1);combat.TryAttack();Check(actor.HitsRemaining==2,"Default camera can hit visible hostile at 50m with "+equipped);
                    actor.ResetEncounter();Place(actor,new Vector3(1030,.1f,1050));Set(combat,"_nextPulseAt",Time.time-1);combat.TryAttack();Check(actor.HitsRemaining==3,"Aim assistance never selects outside cone");
                }
            }
            finally
            {
                touch.Cancel();combat.Equip(weapon);Place(player,saved);player.transform.rotation=savedRotation;
                actor.ResetEncounter();Place(actor,actorPosition);actor.transform.rotation=actorRotation;actor.enabled=actorEnabled;
                rig.SetTarget(player.transform,false);rig.SendMessage("LateUpdate");UnityEngine.Object.DestroyImmediate(floor);UnityEngine.Object.DestroyImmediate(wall);Physics.SyncTransforms();
            }
        }
        static void Place(Component item,Vector3 position)
        {var body=item.GetComponent<CharacterController>();body.enabled=false;item.transform.position=position;body.enabled=true;Physics.SyncTransforms();}

        static void Finish(int code)
        {
            foreach(var key in Strings){if(SessionState.GetBool(Key+key+"Exists",false))PlayerPrefs.SetString(key,SessionState.GetString(Key+key,""));else PlayerPrefs.DeleteKey(key);}
            foreach(var key in Ints){if(SessionState.GetBool(Key+key+"Exists",false))PlayerPrefs.SetInt(key,SessionState.GetInt(Key+key,0));else PlayerPrefs.DeleteKey(key);}
            PlayerPrefs.Save();SessionState.EraseBool(Key);
            EditorApplication.isPlaying=false;EditorApplication.Exit(code);
        }
    }
}
#endif
