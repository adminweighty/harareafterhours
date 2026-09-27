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
    public static class CharacterCameraValidation
    {
        const string Key = "Harare.CharacterCameraQA", Folder = "/private/tmp/harare-character-camera/";
        static readonly string[] Strings = {"Harare.Graphics.v1", PlayerLoadout.SaveKey, TouchGameplayControls.SettingsKey, MissionDirector.SaveKey, PlayerProgression.SaveKey};
        static readonly string[] Ints = {JoinaCityBlock.IntroSaveKey, PlayerLoadout.StarterWeaponKey, MissionDirector.RewardKey};
        static int stage;
        static double deadline;
        static string error;
        static CharacterCameraValidation()
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
                    CaptureCastGallery(game);
                    File.WriteAllText(Folder+"validation.txt",$"PASS: {characters.Length} characters / {meshes} skinned meshes; supported materials; no forcibly hidden bodies; distant LOD retained; six-direction ambient minimum {samples.Min(c=>c.grayscale):F3}. Armed cardinal movement turns body/camera; held input stays straight; aim strafe preserved; practice bags absent.\n");
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
        static void CaptureCastGallery(HarareAfterHoursBootstrap game)
        {
            var root=new GameObject("QA cast gallery");
            var cameraObject=new GameObject("QA gallery camera");var camera=cameraObject.AddComponent<Camera>();
            camera.enabled=false;camera.orthographic=true;camera.orthographicSize=4.8f;camera.aspect=1600f/900;
            camera.transform.position=new Vector3(0,4,982);camera.transform.rotation=Quaternion.identity;
            camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=new Color(.02f,.025f,.035f);camera.cullingMask=1<<31;
            var rt=new RenderTexture(1600,900,24);var previous=RenderTexture.active;camera.targetTexture=rt;
            var models=new System.Collections.Generic.List<GameObject>();
            try
            {
                for(int i=0;i<game.Characters.Cast.Count;i++)
                {
                    var model=UnityEngine.Object.Instantiate(Resources.Load<GameObject>("HarareCharacters/Character01"),root.transform);
                    model.transform.SetPositionAndRotation(new Vector3(-7+(i%8)*2,(i/8)*3,1000),Quaternion.Euler(0,180,0));
                    model.AddComponent<RuntimeCharacterVisual>().Configure(game.Characters.Cast[i],false);
                    foreach(var child in model.GetComponentsInChildren<Transform>(true))child.gameObject.layer=31;
                    model.GetComponentInChildren<Animator>().Update(0);models.Add(model);
                }
                for(int lod=0;lod<3;lod++)
                {
                    foreach(var model in models)foreach(var group in model.GetComponentsInChildren<LODGroup>())group.ForceLOD(lod);
                    camera.Render();camera.Render();RenderTexture.active=rt;
                    var texture=new Texture2D(1600,900,TextureFormat.RGB24,false);texture.ReadPixels(new Rect(0,0,1600,900),0,0);texture.Apply();
                    File.WriteAllBytes(Folder+"cast-lod-"+lod+".png",texture.EncodeToPNG());
                    var pixels=texture.GetPixels32();
                    foreach(var model in models)
                    {
                        Vector3 feet=camera.WorldToScreenPoint(model.transform.position);
                        Vector3 head=camera.WorldToScreenPoint(model.transform.position+Vector3.up*1.85f);
                        int lit=0;
                        for(int y=(int)feet.y;y<(int)head.y;y++)for(int x=(int)feet.x-40;x<(int)feet.x+40;x++)
                        {var c=pixels[y*1600+x];if(Mathf.Max(c.r,c.g,c.b)>90)lit++;}
                        Check(lit>100,"Visible rasterized character at LOD "+lod+": "+model.name+" pixels "+lit);
                    }
                    UnityEngine.Object.DestroyImmediate(texture);
                }
            }
            finally
            {
                camera.targetTexture=null;RenderTexture.active=previous;rt.Release();UnityEngine.Object.DestroyImmediate(rt);
                UnityEngine.Object.DestroyImmediate(root);UnityEngine.Object.DestroyImmediate(cameraObject);
            }
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
