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
    public static class JoinaCityValidation
    {
        const string Key="Harare.Joina.QA";
        static readonly string[] IntKeys={JoinaCityBlock.IntroSaveKey,MissionDirector.SaveKey};
        static double deadline;
        static JoinaCityValidation(){EditorApplication.update+=Tick;}
        public static void RunFresh(){SessionState.SetBool(Key+"fresh",true);Run();}
        public static void Run()
        {
            foreach(var key in IntKeys){SessionState.SetBool(Key+key+"exists",PlayerPrefs.HasKey(key));SessionState.SetInt(Key+key,PlayerPrefs.GetInt(key));}
            SessionState.SetBool(Key+"pointsExists",PlayerPrefs.HasKey(PlayerProgression.SaveKey));SessionState.SetString(Key+"points",PlayerPrefs.GetString(PlayerProgression.SaveKey,""));
            PlayerPrefs.DeleteKey(JoinaCityBlock.IntroSaveKey);PlayerPrefs.DeleteKey(PlayerProgression.SaveKey);
            PlayerPrefs.SetInt(MissionDirector.SaveKey,(int)(SessionState.GetBool(Key+"fresh",false)?MissionState.MeetTino:MissionState.Complete));
            EditorSceneManager.OpenScene("Assets/Scenes/SampleScene.unity");SessionState.SetBool(Key,true);EditorApplication.isPlaying=true;
        }
        static void Check(bool value,string message){if(!value)throw new Exception(message);}
        static void Tick()
        {
            if(!SessionState.GetBool(Key,false)||!EditorApplication.isPlaying)return;
            try
            {
                if(deadline==0)deadline=EditorApplication.timeSinceStartup+100;
                Check(EditorApplication.timeSinceStartup<deadline,"Joina validation timed out");
                var game=HarareAfterHoursBootstrap.Instance;if(game==null||Time.time<4)return;
                var block=Object.FindFirstObjectByType<JoinaCityBlock>();Check(block!=null,"Landmark exists");
                Check(Vector3.Distance(game.Player.transform.position,JoinaCityBlock.Spawn)<1,"Player starts on Joina pavement");
                Check(game.Mission.JoinaWelcome&&game.Mission.ObjectiveText.Contains("Joina City"),"Joina is first objective even with legacy save");
                Check(!game.Mission.TryInteract(),"Cannot complete contact remotely");
                Check(!game.Mission.CanEnterVehicle(game.FeaturedVehicle),"Campaign car stays gated until welcome");
                Vector3[] walk={JoinaCityBlock.Spawn,new(7.5f,.2f,200),new(7.5f,.2f,219),JoinaCityBlock.Contact};
                for(int i=1;i<walk.Length;i++)
                {
                    Vector3 delta=walk[i]-walk[i-1];
                    foreach(var hit in Physics.CapsuleCastAll(walk[i-1]+Vector3.up*.6f,walk[i-1]+Vector3.up*1.5f,.32f,delta.normalized,delta.magnitude,~0,QueryTriggerInteraction.Ignore))
                        Check(hit.transform.IsChildOf(game.Player.transform),"Walkable pavement route blocked by "+hit.transform.name);
                }
                foreach(var r in block.GetComponentsInChildren<Renderer>())foreach(var m in r.sharedMaterials)Check(m!=null&&m.shader!=null&&m.shader.isSupported,"All landmark shaders supported");
                Check(block.GetComponentsInChildren<Light>().Length==0,"No additional realtime lights");
                Check(block.GetComponentsInChildren<LODGroup>().Length>=2,"Facade and silhouette have LOD culling");
                Capture(Camera.main,"/private/tmp/harare-joina-opening.png",1280,720);
                var camera=new GameObject("Joina QA overview").AddComponent<Camera>();camera.CopyFrom(Camera.main);camera.enabled=false;
                camera.transform.position=new Vector3(-42,25,172);camera.transform.LookAt(new Vector3(44,47,252));camera.fieldOfView=65;
                Capture(camera,"/private/tmp/harare-joina-overview.png",1280,900);Object.Destroy(camera.gameObject);
                var cc=game.Player.GetComponent<CharacterController>();cc.enabled=false;game.Player.transform.position=JoinaCityBlock.Contact+Vector3.back;cc.enabled=true;Physics.SyncTransforms();
                Check(game.Mission.CanTalkAtJoina&&game.Mission.TryInteract(),"Nearby contact completes opening");
                Check(!JoinaCityBlock.WelcomePending&&!game.Mission.JoinaWelcome,"Welcome persists");
                var expected=SessionState.GetBool(Key+"fresh",false)?MissionState.MeetTino:MissionState.Complete;
                Check(game.Mission.State==expected&&PlayerPrefs.GetInt(MissionDirector.SaveKey)==(int)expected,"Legacy mission preserved");
                Check(game.Progression.Points==25,"Welcome reward is 25 points");game.Mission.TryInteract();Check(game.Progression.Points==25,"Cannot repeat welcome reward");
                if(expected==MissionState.MeetTino)
                {
                    cc.enabled=false;game.Player.transform.position=MissionDirector.GaragePosition+Vector3.back;cc.enabled=true;
                    Check(game.Mission.TryInteract()&&game.Mission.State==MissionState.WalkToVehicle&&game.Progression.Points==125,"Fresh welcome continues into Tino keys mission");
                }
                Check(CityTimeTrial.LengthMetres>3900,"Existing connected race preserved");
                Finish(0,"PASS: Joina spawn, first objective, contact distance gate, unobstructed walking route, campaign vehicle gate, supported materials, no added real-time lights, LOD groups, persisted welcome, legacy stage "+expected+" preserved, +25 once, original race retained. Fresh-start mode also verifies Tino keys hand-off. Renders captured. Device frame-time profiling still required.");
            }
            catch(Exception ex){Finish(1,ex.ToString());}
        }
        static void Capture(Camera camera,string path,int width,int height)
        {
            var old=camera.targetTexture;var target=new RenderTexture(width,height,24);camera.targetTexture=target;camera.Render();
            var active=RenderTexture.active;RenderTexture.active=target;var image=new Texture2D(width,height,TextureFormat.RGB24,false);image.ReadPixels(new Rect(0,0,width,height),0,0);image.Apply();File.WriteAllBytes(path,image.EncodeToPNG());
            RenderTexture.active=active;camera.targetTexture=old;Object.Destroy(image);Object.Destroy(target);
        }
        static void Finish(int code,string report)
        {
            foreach(var key in IntKeys){if(SessionState.GetBool(Key+key+"exists",false))PlayerPrefs.SetInt(key,SessionState.GetInt(Key+key,0));else PlayerPrefs.DeleteKey(key);}
            if(SessionState.GetBool(Key+"pointsExists",false))PlayerPrefs.SetString(PlayerProgression.SaveKey,SessionState.GetString(Key+"points",""));else PlayerPrefs.DeleteKey(PlayerProgression.SaveKey);
            PlayerPrefs.Save();File.WriteAllText("/private/tmp/harare-joina-validation.txt",report);Debug.Log(report);SessionState.EraseBool(Key);SessionState.EraseBool(Key+"fresh");EditorApplication.isPlaying=false;EditorApplication.Exit(code);
        }
    }
}
#endif
