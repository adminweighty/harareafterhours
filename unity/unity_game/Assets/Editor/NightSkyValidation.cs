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
    public static class NightSkyValidation
    {
        const string Key = "Harare.NightSkyQA", Folder = "/private/tmp/harare-night-sky/";
        static readonly string[] Strings = {"Harare.Graphics.v1", PlayerLoadout.SaveKey, TouchGameplayControls.SettingsKey, MissionDirector.SaveKey, PlayerProgression.SaveKey};
        static readonly string[] Ints = {JoinaCityBlock.IntroSaveKey, PlayerLoadout.StarterWeaponKey, MissionDirector.RewardKey};
        static int stage;
        static double deadline;
        static string error;
        static NightSkyValidation()
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
            foreach(var file in new[]{"gameplay.png","meteor-start.png","meteor-end.png"}) File.Delete(Folder+file);
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
                if(Time.realtimeSinceStartup<5)return;
                var game=HarareAfterHoursBootstrap.Instance;
                if(stage==0)
                {
                    Check(game!=null&&game.Player!=null,"World/player loaded");
                    Check(RenderSettings.skybox != null && RenderSettings.skybox.shader.name == "Harare/Night Sky" && RenderSettings.skybox.shader.isSupported,"Night shader is active and supported");
                    Check(!ShaderUtil.ShaderHasError(RenderSettings.skybox.shader),"Night shader compiles without errors");
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
                            Check(group.GetLODs().Last().screenRelativeTransitionHeight<=.0031f,"Distant NPC keeps final LOD");
                    }
                    File.WriteAllText(Folder+"validation.txt",$"PASS: {characters.Length} characters / {meshes} skinned meshes; supported materials; no forcibly hidden bodies; distant LOD retained; six-direction ambient minimum {samples.Min(c=>c.grayscale):F3}.\n");
                    if(!SessionState.GetBool(Key+"Capture",false)){Finish(0);return;}
                    ScreenCapture.CaptureScreenshot(Folder+"gameplay.png");stage=1;
                }
                else if(stage==1 && File.Exists(Folder+"gameplay.png"))
                {
                    var camera=Camera.main;camera.GetComponent<ThirdPersonCameraRig>().enabled=false;
                    camera.transform.rotation=Quaternion.LookRotation(new Vector3(0,.4f,1));
                    camera.fieldOfView=65;
                    RenderSettings.skybox.SetFloat("_PreviewTime",3.25f);stage=2;
                }
                else if(stage==2){ScreenCapture.CaptureScreenshot(Folder+"meteor-start.png");stage=3;}
                else if(stage==3&&File.Exists(Folder+"meteor-start.png"))
                {RenderSettings.skybox.SetFloat("_PreviewTime",3.70f);stage=4;}
                else if(stage==4){ScreenCapture.CaptureScreenshot(Folder+"meteor-end.png");stage=5;}
                else if(stage==5&&File.Exists(Folder+"meteor-end.png"))Finish(0);
            }
            catch(Exception e){File.AppendAllText(Folder+"validation.txt","FAIL: "+e);Finish(1);}
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
