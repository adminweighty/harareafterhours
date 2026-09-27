#if UNITY_EDITOR
using System;
using System.IO;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering.Universal;
namespace HarareAfterHours.EditorTools
{
    [InitializeOnLoad]
    public static class CityExpansionValidation
    {
        const string Key="Harare.ExpansionValidation",Folder="/private/tmp/harare-city-expansion/";
        static int stage,frame,points;static double deadline;static MissionState mission;static string runtimeError;
        static readonly List<string> Report=new();static readonly List<float> Times=new();
        static CityExpansionValidation()
        {
            EditorApplication.update+=Tick;
            Application.logMessageReceived+=(m,s,t)=>{if(SessionState.GetBool(Key,false)&&(t==LogType.Error||t==LogType.Exception||t==LogType.Assert))runtimeError=m;};
        }
        public static void Run()
        {
            SessionState.SetInt(Key+"stage",PlayerPrefs.GetInt(MissionDirector.SaveKey,-1));SessionState.SetInt(Key+"clean",PlayerPrefs.GetInt(MissionDirector.SaveKey+".clean",-1));
            SessionState.SetString(Key+"score",PlayerPrefs.GetString(PlayerProgression.SaveKey,""));SessionState.SetString(Key+"graphics",PlayerPrefs.GetString("Harare.Graphics.v1",""));SessionState.SetFloat(Key+"best",PlayerPrefs.GetFloat("Harare.AvenueTrial.Best.v1",-1));
            PlayerPrefs.SetInt(MissionDirector.SaveKey,(int)MissionState.WalkToVehicle);PlayerPrefs.SetString("Harare.Graphics.v1","Balanced");PlayerPrefs.DeleteKey(PlayerProgression.SaveKey);PlayerPrefs.DeleteKey("Harare.AvenueTrial.Best.v1");
            EditorSceneManager.OpenScene("Assets/Scenes/SampleScene.unity");SessionState.SetBool(Key,true);EditorApplication.isPlaying=true;
        }
        static void Check(bool ok,string why){if(!ok)throw new Exception(why);}
        static void Tick()
        {
            if(!SessionState.GetBool(Key,false)||!EditorApplication.isPlaying||frame==Time.frameCount)return;frame=Time.frameCount;
            try
            {
                if(deadline==0)deadline=EditorApplication.timeSinceStartup+180;
                Check(EditorApplication.timeSinceStartup<deadline,"Timeout stage "+stage);Check(runtimeError==null,runtimeError);
                var game=HarareAfterHoursBootstrap.Instance;if(game==null||game.Player==null||Time.time<4)return;
                var player=game.Player;var car=game.FeaturedVehicle;var race=CityTimeTrial.Instance;
                if(stage==0)
                {
                    var world=UnityEngine.Object.FindFirstObjectByType<ExpandedCity>();Check(world!=null&&world.BuildingCount>=180,"Reference modules instantiated");
                    Check(GameObject.Find("City ground").GetComponent<Collider>().bounds.size.x==1200,"1.2 km supported ground");
                    Check(Mathf.Abs(CityTimeTrial.LengthMetres-3980)<.1f,"3.98 km ordered circuit");
                    int samples=0;
                    for(int leg=1;leg<CityTimeTrial.Route.Length;leg++)
                    {
                        Vector3 a=CityTimeTrial.Route[leg-1],b=CityTimeTrial.Route[leg];int count=Mathf.CeilToInt(Vector3.Distance(a,b)/5);
                        for(int i=0;i<=count;i++)
                        {
                            Vector3 p=Vector3.Lerp(a,b,(float)i/count);
                            Check(Physics.Raycast(p+Vector3.up*2,Vector3.down,out var ground,3)&&ground.collider.name=="Expanded avenue asphalt","Continuous circuit asphalt at "+p);
                            Check(!Physics.CheckCapsule(p+Vector3.up*.8f,p+Vector3.up*1.6f,.65f,~0,QueryTriggerInteraction.Ignore),"Circuit vehicle clearance at "+p);samples++;
                        }
                    }
                    Report.Add($"PASS: 1200m ground, {world.BuildingCount} reference-derived modular buildings, 3980m circuit; {samples} asphalt and clearance samples");
                    Check(GameObject.Find("Avondale reference shopping entrance")!=null,"Avondale landmark");
                    foreach(string name in new[]{"Balanced","High"})
                    {
                        CityGraphics.Instance.Apply(name);var pipeline=(UniversalRenderPipelineAsset)QualitySettings.renderPipeline;
                        Check(pipeline.msaaSampleCount==1&&!Camera.main.allowMSAA,"Single-sample embedded target "+name);
                        Check(Camera.main.GetUniversalAdditionalCameraData().antialiasing==AntialiasingMode.FastApproximateAntialiasing,"FXAA "+name);Check(Camera.main.GetUniversalAdditionalCameraData().renderPostProcessing,"Post-processing enabled");
                    }
                    CityGraphics.Instance.Apply("Balanced");
                    Camera.main.GetComponent<ThirdPersonCameraRig>().enabled=false;
                    Capture(new Vector3(4,2.7f,100),new Vector3(0,10,190),"avenue");
                    Capture(new Vector3(300,4,216),new Vector3(300,6.5f,260),"avondale");
                    Capture(new Vector3(490,220,510),new Vector3(100,0,100),"district-overview");
                    Camera.main.GetComponent<ThirdPersonCameraRig>().enabled=true;
                    var cc=player.GetComponent<CharacterController>();cc.enabled=false;player.transform.position=car.transform.position-car.transform.right*1.8f+Vector3.up*.1f;cc.enabled=true;Physics.SyncTransforms();
                    Check(player.TryBoardNearest(false),"Can still enter campaign car");mission=game.Mission.State;points=game.Progression.Points;
                    car.GetComponent<Rigidbody>().isKinematic=true;car.transform.SetPositionAndRotation(CityTimeTrial.Route[0],Quaternion.identity);Physics.SyncTransforms();
                    Check(race.TryInteract(player)&&race.Running,"Opt-in race start");stage++;return;
                }
                if(stage==1)
                {
                    if(race.Running)
                    {
                        // Deterministic routing fixture, not a physical-driving benchmark.
                        Vector3 target=CityTimeTrial.Route[race.Checkpoint];Vector3 direction=target-car.transform.position;
                        if(direction.sqrMagnitude>.01f)car.transform.rotation=Quaternion.LookRotation(direction);
                        car.transform.position=Vector3.MoveTowards(car.transform.position,target,25);Physics.SyncTransforms();return;
                    }
                    Check(race.Checkpoint==CityTimeTrial.Route.Length,"All checkpoints passed in order");
                    Check(race.Best>0&&game.Progression.Points==points+500,"Best time and one-time completion award");Check(game.Mission.State==mission,"Campaign state preserved");
                    Check(race.TryInteract(player)&&race.Running,"Race can restart");Check(race.TryInteract(player)&&!race.Running,"Race can cancel without exiting");Check(player.IsDriving,"Still in car after cancel");
                    Report.Add("PASS: ordered checkpoint completion, best-time persistence, first-finish reward, retry/cancel, campaign state preserved (synthetic route fixture)");
                    car.transform.position=new Vector3(3,.2f,28);Camera.main.GetComponent<ThirdPersonCameraRig>().Recenter();stage++;return;
                }
                Times.Add(Time.unscaledDeltaTime*1000);if(Times.Count<180)return;Times.Sort();
                Report.Add($"Editor frame sample median {Times[90]:0.00} ms; p95 {Times[171]:0.00} ms. Not a physical-phone performance guarantee.");
                Report.Add("PASS: Balanced/High presets, Avondale landmark, no runtime errors. Three review views captured.");Finish(0);
            }
            catch(Exception e){Report.Add("FAIL: "+e);Debug.LogException(e);Finish(1);}
        }
        static void Capture(Vector3 p,Vector3 target,string name)
        {
            var camera=Camera.main;camera.transform.SetPositionAndRotation(p,Quaternion.LookRotation(target-p));camera.fieldOfView=60;
            var rt=new RenderTexture(1280,800,24);camera.targetTexture=rt;camera.Render();camera.Render();RenderTexture.active=rt;
            var texture=new Texture2D(1280,800,TextureFormat.RGB24,false);texture.ReadPixels(new Rect(0,0,1280,800),0,0);texture.Apply();File.WriteAllBytes(Folder+name+".png",texture.EncodeToPNG());
            camera.targetTexture=null;RenderTexture.active=null;UnityEngine.Object.DestroyImmediate(texture);rt.Release();UnityEngine.Object.DestroyImmediate(rt);
        }
        static void Finish(int code)
        {
            foreach(var pair in new[]{("stage",MissionDirector.SaveKey),("clean",MissionDirector.SaveKey+".clean")}){int v=SessionState.GetInt(Key+pair.Item1,-1);if(v<0)PlayerPrefs.DeleteKey(pair.Item2);else PlayerPrefs.SetInt(pair.Item2,v);}
            foreach(var pair in new[]{("score",PlayerProgression.SaveKey),("graphics","Harare.Graphics.v1")}){string v=SessionState.GetString(Key+pair.Item1,"");if(v=="")PlayerPrefs.DeleteKey(pair.Item2);else PlayerPrefs.SetString(pair.Item2,v);}
            float best=SessionState.GetFloat(Key+"best",-1);if(best<0)PlayerPrefs.DeleteKey("Harare.AvenueTrial.Best.v1");else PlayerPrefs.SetFloat("Harare.AvenueTrial.Best.v1",best);PlayerPrefs.Save();
            File.WriteAllLines(Folder+"validation.txt",Report);SessionState.EraseBool(Key);EditorApplication.isPlaying=false;EditorApplication.Exit(code);
        }
    }
}
#endif
