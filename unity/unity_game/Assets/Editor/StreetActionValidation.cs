#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
namespace HarareAfterHours.EditorTools
{
    [InitializeOnLoad]
    public static class StreetActionValidation
    {
        const string Key="Harare.StreetActionTest",Folder="/private/tmp/harare-street-action/";
        static int stage,frame,points;static float started;static double deadline;
        static readonly List<string> report=new();static string error;static Vector3 patrolStart;static MissionState mission;
        static StreetActor thug,officer;
        static StreetActionValidation(){EditorApplication.update+=Tick;Application.logMessageReceived+=(m,s,t)=>{if(SessionState.GetBool(Key,false)&&(t==LogType.Error||t==LogType.Exception))error=m;};}
        public static void Run()
        {
            Directory.CreateDirectory(Folder);SessionState.SetString(Key+"Save",PlayerPrefs.GetString(PlayerProgression.SaveKey,""));
            PlayerPrefs.DeleteKey(PlayerProgression.SaveKey);
            EditorSceneManager.OpenScene("Assets/Scenes/SampleScene.unity");SessionState.SetBool(Key,true);EditorApplication.isPlaying=true;
        }
        static void Tick()
        {
            if(!SessionState.GetBool(Key,false)||!EditorApplication.isPlaying||frame==Time.frameCount)return;
            frame=Time.frameCount;
            try
            {
                if(deadline==0)deadline=EditorApplication.timeSinceStartup+180;
                Check(EditorApplication.timeSinceStartup<deadline,"Test timeout");Check(error==null,error);
                var world=StreetActionDirector.Instance;var game=HarareAfterHoursBootstrap.Instance;
                if(world==null||game==null||Time.time<3)return;
                switch(stage)
                {
                    case 0:
                        Check(world.Patrols.Count==2&&world.Actors.Count(a=>!a.Officer)==world.StreetThreatTarget,
                            "Two patrols and the selected level's full robber population");
                        Check(world.StreetThreatTarget>=8&&world.StreetThreatsRemaining==world.StreetThreatTarget,
                            "Level starts with at least eight marked threats and a complete clear target");
                        Check(UnityEngine.Object.FindObjectsByType<VehicleController>(FindObjectsSortMode.None).Length>=6,"Core city vehicle fleet retained");
                        Check(!world.Patrols[0].GetComponent<VehicleController>().TryEnter(game.Player,false),"Patrol seat protected");
                        var indicatorActor=world.SpawnActor(false,new Vector3(120,.2f,121.6f),"Indicator validation robber");
                        var indicator=indicatorActor.GetComponent<HostileIndicator>();
                        Check(indicator!=null,"Every robber receives a red overhead indicator");
                        Place(game.Player,new Vector3(120,.2f,120));
                        var camera=Camera.main;var rig=camera.GetComponent<ThirdPersonCameraRig>();bool rigWasEnabled=rig!=null&&rig.enabled;
                        if(rig!=null)rig.enabled=false;
                        camera.transform.SetPositionAndRotation(new Vector3(120,1.6f,116),Quaternion.LookRotation(new Vector3(0,-.12f,1)));
                        Physics.SyncTransforms();indicator.Refresh();
                        Check(indicator.IsVisible&&indicator.IndicatorPosition.y>indicatorActor.transform.position.y+2.0f,"Visible robber has clear space below its red marker");
                        var markerRenderer=indicatorActor.GetComponentsInChildren<MeshRenderer>().FirstOrDefault(r=>r.gameObject.name=="Hostile red indicator");
                        Check(markerRenderer!=null&&markerRenderer.sharedMaterial!=null&&markerRenderer.sharedMaterial.shader.name!="Hidden/InternalErrorShader","Robber indicator uses a supported red material");
                        Color markerColor=markerRenderer.sharedMaterial.HasProperty("_BaseColor")?markerRenderer.sharedMaterial.GetColor("_BaseColor"):markerRenderer.sharedMaterial.color;
                        Check(markerColor.r>.9f&&markerColor.g<.15f&&markerColor.b<.15f,"Robber indicator is visibly red");
                        var cover=GameObject.CreatePrimitive(PrimitiveType.Cube);cover.transform.position=new Vector3(120,1.5f,120.8f);cover.transform.localScale=new Vector3(3,3,.25f);Physics.SyncTransforms();
                        indicator.Refresh();Check(indicator.IsVisible,"Robber indicator remains visible when the thief is behind cover");cover.SetActive(false);UnityEngine.Object.Destroy(cover);
                        indicatorActor.Hit();indicatorActor.Hit();indicatorActor.Hit();indicator.Refresh();
                        Check(!indicator.IsVisible,"Downed robber marker hides");
                        if(rig!=null)rig.enabled=rigWasEnabled;
                        foreach(var actor in world.Actors)actor.enabled=false;
                        thug=world.Actors.First(a=>!a.Officer);officer=world.Actors.Last(a=>a.Officer);
                        Place(game.Player,new Vector3(120,.2f,120));Place(thug,new Vector3(120,.2f,121.6f));game.Player.transform.rotation=Quaternion.identity;
                        points=game.Progression.Points;
                        for(int i=0;i<3;i++)Check(world.Strike(2.25f),"Melee reaches robber");
                        Check(thug.Down&&game.Progression.Points==points+50,"Defeat awards 50");
                        Check(world.StreetThreatsDefeated==1&&world.StreetThreatsRemaining==world.StreetThreatTarget-1,
                            "Street-level clear progress advances once per roaming threat");world.Strike(2.25f);
                        Check(game.Progression.Points==points+50,"No repeat reward while down");
                        Check(world.Wanted==0,"Defending against robber does not cause wanted");
                        var wall=GameObject.CreatePrimitive(PrimitiveType.Cube);wall.transform.position=new Vector3(120,1,120.8f);wall.transform.localScale=new Vector3(3,3,.2f);Physics.SyncTransforms();
                        Check(!world.Visible(game.Player.transform.position+Vector3.up,thug.transform,5),"Walls block sight");UnityEngine.Object.Destroy(wall);
                        report.Add("PASS: every robber has a supported red overhead indicator, cover does not hide it, downed marker hides, police seats protected, reward-once and self-defence work");
                        Place(game.Player,new Vector3(0,.2f,-15));Place(officer,new Vector3(0,.2f,-13.4f));game.Player.transform.rotation=Quaternion.identity;
                        Check(world.Strike(2.25f)&&world.Wanted==1,"Assault triggers wanted");
                        patrolStart=world.Patrols[0].transform.position;mission=game.Mission.State;Next();break;
                    case 1:
                        if(Time.time-started<2)break;
                        Check(Vector3.Distance(patrolStart,world.Patrols[0].transform.position)>.2f,"Police car moves during pursuit");
                        Place(officer,game.Player.transform.position+Vector3.forward*1.2f);
                        Next();break;
                    case 2:
                        if(!world.Arrested){Check(Time.time-started<7,"Officer must complete capture");break;}
                        Check(game.Player.InputLocked,"Arrest locks movement");
                        Check(game.Player.GetComponentInChildren<StreetActionPose>().Cuffed,"Handcuff pose active");
                        Check(!game.Player.TryBoardNearest(false),"Cuffed player cannot board");
                        Capture(game.Player.transform.position+new Vector3(2,1.6f,3.3f),game.Player.transform.position+Vector3.up,"cuffed.png");
                        points=game.Progression.Points;report.Add("PASS: assault triggers wanted, patrol moves, officer captures, handcuffs lock movement/boarding");Next();break;
                    case 3:
                        if(world.Arrested)break;
                        Check(!game.Player.InputLocked&&world.Wanted==0,"Release restores controls and clears wanted");
                        Check(game.Progression.Points==Mathf.Max(0,points-50),"Capped arrest fine");Check(game.Mission.State==mission,"Mission preserved");
                        Place(game.Player,new Vector3(170,.2f,170));world.ReportCrime(3);Check(world.Wanted==3,"Three-level wanted cap");
                        report.Add("PASS: release, capped fine, mission retained, wanted escalation");Next();break;
                    case 4:
                        if(world.Wanted>0){Check(Time.time-started<16,"Escape timer clears wanted");break;}
                        report.Add("PASS: break sight/range for 12 seconds clears pursuit");
                        Place(thug,new Vector3(-120,.2f,120));Place(game.Player,new Vector3(120,.2f,120));
                        // A fresh actor ensures no prior defeat cooldown affects the robbery test.
                        thug=world.SpawnActor(false,new Vector3(120,.2f,121.2f),"Validation robber");
                        points=game.Progression.Points;Next();break;
                    case 5:
                        if(!world.GameOver)break;
                        Check(world.Health==0&&game.Player.InputLocked&&Time.timeScale==0,"Repeated hostile hits deplete life and end the run");
                        Check(game.Progression.Points==points,"Defeat retains earned points");
                        world.RestartEncounter();
                        Check(!world.GameOver&&!game.Player.InputLocked&&world.Health==100,"Explicit restart restores control");
                        report.Add("PASS: live hostile attack causes Game Over; explicit restart restores controls and retains points");
                        foreach(var target in world.Actors.Where(a=>!a.Officer&&a.name.Contains(" — ")).ToArray())
                            for(int hit=0;hit<world.Difficulty.EnemyHits;hit++)target.Hit();
                        Check(world.DifficultyLevelComplete&&world.StreetThreatsRemaining==0,
                            "Selected difficulty completes only after every roaming threat is defeated");
                        report.Add("PASS: the selected difficulty clear target reaches completion after every roaming threat is down");
                        var car=world.Patrols[0];Capture(car.transform.position+car.transform.right*4+car.transform.forward*4+Vector3.up*2,car.transform.position+Vector3.up,"police-car.png");
                        Finish(0);break;
                }
            }
            catch(Exception e){report.Add("FAIL: "+e.Message);Debug.LogException(e);Finish(1);}
        }
        static void Place(Component item,Vector3 position){var cc=item.GetComponent<CharacterController>();if(cc!=null)cc.enabled=false;item.transform.position=position;if(cc!=null)cc.enabled=true;Physics.SyncTransforms();}
        static void Next(){stage++;started=Time.time;}
        static void Check(bool condition,string message){if(!condition)throw new Exception(message);}
        static void Capture(Vector3 position,Vector3 target,string file)
        {
            // Unity's Camera.Render path can crash the headless Metal renderer.
            // The production build still renders normally; batch validation covers gameplay only.
            if(Application.isBatchMode)return;
            var cam=Camera.main;cam.GetComponent<ThirdPersonCameraRig>().enabled=false;cam.transform.position=position;cam.transform.LookAt(target);
            foreach(var pose in UnityEngine.Object.FindObjectsByType<StreetActionPose>(FindObjectsSortMode.None))pose.ApplyPose();
            // Editor camera.Render can reuse pre-LateUpdate GPU skinning. Bake
            // only for this review image, then restore all live renderers.
            var skins=UnityEngine.Object.FindObjectsByType<SkinnedMeshRenderer>(FindObjectsSortMode.None);
            var visibility=skins.Select(s=>s.forceRenderingOff).ToArray();var objects=new List<GameObject>();var meshes=new List<Mesh>();
            foreach(var skin in skins)
            {
                var lod=skin.GetComponentInParent<LODGroup>();
                if(!skin.forceRenderingOff&&(lod==null||lod.GetLODs()[0].renderers.Contains(skin)))
                {
                    var mesh=new Mesh();skin.BakeMesh(mesh);meshes.Add(mesh);
                    var obj=new GameObject("Temporary posed capture");objects.Add(obj);obj.transform.SetPositionAndRotation(skin.transform.position,skin.transform.rotation);obj.transform.localScale=skin.transform.lossyScale;
                    obj.AddComponent<MeshFilter>().sharedMesh=mesh;var renderer=obj.AddComponent<MeshRenderer>();renderer.sharedMaterials=skin.sharedMaterials;
                    for(int i=0;i<skin.sharedMaterials.Length;i++){var block=new MaterialPropertyBlock();skin.GetPropertyBlock(block,i);renderer.SetPropertyBlock(block,i);}
                }
                skin.forceRenderingOff=true;
            }
            var rt=new RenderTexture(1280,800,24);cam.targetTexture=rt;cam.Render();RenderTexture.active=rt;
            var image=new Texture2D(1280,800,TextureFormat.RGB24,false);image.ReadPixels(new Rect(0,0,1280,800),0,0);image.Apply();File.WriteAllBytes(Folder+file,image.EncodeToPNG());
            cam.targetTexture=null;RenderTexture.active=null;UnityEngine.Object.DestroyImmediate(image);rt.Release();UnityEngine.Object.DestroyImmediate(rt);
            for(int i=0;i<skins.Length;i++)skins[i].forceRenderingOff=visibility[i];
            foreach(var obj in objects)UnityEngine.Object.DestroyImmediate(obj);foreach(var mesh in meshes)UnityEngine.Object.DestroyImmediate(mesh);
        }
        static void Finish(int code)
        {
            string save=SessionState.GetString(Key+"Save","");if(save=="")PlayerPrefs.DeleteKey(PlayerProgression.SaveKey);else PlayerPrefs.SetString(PlayerProgression.SaveKey,save);PlayerPrefs.Save();
            File.WriteAllLines(Folder+"validation.txt",report);SessionState.EraseBool(Key);EditorApplication.isPlaying=false;EditorApplication.Exit(code);
        }
    }
}
#endif
