#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
namespace HarareAfterHours.EditorTools
{
    [InitializeOnLoad]
    public static class VenueInteriorValidation
    {
        const string Key="Harare.VenueQA";static double deadline;static int stage;
        static ThirdPersonController player;static CityVenue grocer;static MissionState mission;
        static readonly string[] Strings = {"Harare.Graphics.v1", PlayerLoadout.SaveKey, TouchGameplayControls.SettingsKey, MissionDirector.SaveKey, PlayerProgression.SaveKey};
        static readonly string[] Ints = {JoinaCityBlock.IntroSaveKey, PlayerLoadout.StarterWeaponKey, MissionDirector.RewardKey};
        static int points;static Vector3 outside;
        static VenueInteriorValidation(){EditorApplication.update+=Tick;}
        public static void Run()
        {
            foreach(var key in Strings){SessionState.SetBool(Key+key+"Exists",PlayerPrefs.HasKey(key));SessionState.SetString(Key+key,PlayerPrefs.GetString(key,""));}
            foreach(var key in Ints){SessionState.SetBool(Key+key+"Exists",PlayerPrefs.HasKey(key));SessionState.SetInt(Key+key,PlayerPrefs.GetInt(key));}
            PlayerPrefs.DeleteKey(PlayerProgression.SaveKey);
            EditorSceneManager.OpenScene("Assets/Scenes/SampleScene.unity");SessionState.SetBool(Key,true);EditorApplication.isPlaying=true;
        }
        static void Check(bool ok,string message){if(!ok)throw new Exception(message);}
        static void Place(Vector3 at){var cc=player.GetComponent<CharacterController>();cc.enabled=false;player.transform.position=at;cc.enabled=true;Physics.SyncTransforms();}
        static void Tick()
        {
            if(!SessionState.GetBool(Key,false)||!EditorApplication.isPlaying)return;
            try
            {
                if(deadline==0)deadline=EditorApplication.timeSinceStartup+90;
                Check(EditorApplication.timeSinceStartup<deadline,"Interior test timeout");
                var game=HarareAfterHoursBootstrap.Instance;if(game==null||Time.time<3)return;
                if(stage==0)
                {
                    player=game.Player;mission=game.Mission.State;
                    var venues=UnityEngine.Object.FindObjectsByType<CityVenue>(FindObjectsSortMode.None);
                    Check(venues.Length==4,"Four existing venues retained");grocer=venues.Single(v=>v.VenueId=="city_grocer");
                    Place(grocer.Approach);outside=player.transform.position;
                    Check(CityVenue.TryInteract(player),"Enter from real exterior approach");
                    Check(VenueInterior.Active!=null&&VenueInterior.Active.NearExit,"Enter lands at interior exit mat");
                    Check(!VenueInterior.Active.RobbersStopped,"Robbers alive on entry");
                    CaptureRoom();
                    var shopkeeper=GameObject.Find("Shopkeeper · RESCUE");
                    Place(shopkeeper.transform.position+Vector3.back);
                    VenueInterior.Active.Interact();Check(!VenueInterior.Active.Escorting,"Cannot rescue until robbers stopped");
                    var robbers=StreetActionDirector.Instance.Actors.Where(a=>a.name=="Robber · City Grocer").ToArray();
                    Check(robbers.Length==2,"Two encounter enemies");
                    foreach(var actor in robbers){actor.Hit(2);actor.Hit(1);}
                    Check(VenueInterior.Active.RobbersStopped,"Existing combat defeats both robbers");
                    VenueInterior.Active.Interact();Check(VenueInterior.Active.Escorting,"Shopkeeper follows after RESCUE");
                    var beforeFollow=shopkeeper.transform.position;
                    Place(beforeFollow+Vector3.back*4);
                    var update=typeof(VenueInterior).GetMethod("Update",BindingFlags.Instance|BindingFlags.NonPublic);
                    update.Invoke(VenueInterior.Active,null);
                    Check(Vector3.Distance(shopkeeper.transform.position,beforeFollow)>.001f,"Escort actually walks toward player");
                    var beforePause=shopkeeper.transform.position;Time.timeScale=0;update.Invoke(VenueInterior.Active,null);
                    Check(shopkeeper.transform.position==beforePause,"Escort respects pause");Time.timeScale=1;
                    points=game.Progression.Points;Place(VenueInterior.Active.ExitPoint);
                    VenueInterior.Active.Interact();Check(VenueInterior.Active!=null,"Exit waits for distant escort");
                    var body=shopkeeper.GetComponent<CharacterController>();body.enabled=false;shopkeeper.transform.position=player.transform.position+Vector3.forward;body.enabled=true;
                    VenueInterior.Active.Interact();
                    Check(VenueInterior.Active==null&&Vector3.Distance(player.transform.position,outside)<.1f,"Exit returns to exterior");
                    Check(game.Progression.Points==points+200&&game.Progression.HasMissionAward(VenueInterior.RescueAward),"One-time rescue reward persisted");
                    Check(game.Mission.State==mission,"Main mission unchanged");
                    stage=1;return;
                }
                if(stage==1)
                {
                    Check(!StreetActionDirector.Instance.Actors.Any(a=>a!=null&&a.name=="Robber · City Grocer"),"Interior actors removed from director");
                    VenueInterior.Enter(grocer,player);
                    Check(!VenueInterior.Active.Objective.Contains("robbers"),"Completed rescue does not respawn");
                    VenueInterior.Active.Interact();Check(game.Progression.Points==points+200,"Repeat visit cannot award rescue twice");
                    foreach(var venue in UnityEngine.Object.FindObjectsByType<CityVenue>(FindObjectsSortMode.None))
                    {
                        VenueInterior.Enter(venue,player);Check(VenueInterior.Active!=null,"Every venue enters");
                        VenueInterior.Active.Interact();Check(VenueInterior.Active==null,"Every venue exits");
                    }
                    VenueInterior.Enter(grocer,player);Place(outside);
                    typeof(VenueInterior).GetMethod("Update",BindingFlags.Instance|BindingFlags.NonPublic).Invoke(VenueInterior.Active,null);
                    Check(VenueInterior.Active==null,"External restart relocation clears interior");
                    stage=2;return;

                }
                if(stage==2)
                {
                    foreach(var state in new[]{MissionState.MeetRudo,MissionState.ChaseRunner,MissionState.YardRecovery,MissionState.ReturnToChipo,MissionState.Complete})
                    {
                        typeof(MissionDirector).GetProperty("State").SetValue(game.Mission,state);
                        var guide=PlayerGuidance.For(game.Mission);
                        string expected=state==MissionState.MeetRudo?"Rudo":state==MissionState.ReturnToChipo?"Chipo":state==MissionState.Complete?"no M02 trigger":"satchel";
                        Check(guide.instruction.Contains(expected),"Actionable guidance for "+state);
                        Check(guide.availability.Contains("M02–M30"),"Unimplemented story status explicit");
                    }
                    typeof(MissionDirector).GetProperty("State").SetValue(game.Mission,mission);
                    var velvet=UnityEngine.Object.FindObjectsByType<CityVenue>(FindObjectsSortMode.None).Single(v=>v.VenueId=="big_apple");
                    Check(velvet.DisplayName=="The Velvet Room","Public venue name follows v3.1; legacy ID preserved");
                    VenueInterior.Enter(velvet,player);stage=3;return;
                }
                if(stage==3)
                {
                    var room=VenueInterior.Active;
                    Check(room.GetComponentsInChildren<RuntimeCharacterVisual>().Length==4,"Host, staff and two guests visible in lounge");
                    foreach(var renderer in room.GetComponentsInChildren<SkinnedMeshRenderer>())
                    {
                        Check(renderer.enabled&&renderer.sharedMesh!=null&&!renderer.forceRenderingOff,"Populated venue has visible body meshes");
                        Check(renderer.sharedMaterials.All(m=>m!=null&&m.shader.isSupported),"Supported character materials");
                    }
                    Check(room.Objective.Contains("host"),"Entry gives host objective");
                    Check(PlayerGuidance.For(game.Mission).instruction.Contains("M16–M17"),"Preview status is disclosed");
                    CaptureRoom("/private/tmp/harare-velvet-preview.png");
                    Place(room.HostPoint+Vector3.back*.8f);
                    Check(room.ActionLabel=="TALK","Approaching host exposes TALK action");
                    int before=game.Progression.Points;room.Interact();
                    Check(game.Bridge.LastEvent.Contains("open_guidance"),"Host opens next-step menu");
                    Check(room.Objective.Contains("EXIT"),"After host, next action is EXIT");
                    Check(game.Mission.State==mission&&game.Progression.Points==before,"Host cannot advance story or grant rewards");
                    Place(room.ExitPoint);room.Interact();Check(VenueInterior.Active==null,"Lounge exits safely");
                    Check(game.Bridge.LastEvent.Contains("player_guidance"),"Exit refreshes street objective");
                    Finish(0,"PASS: all four venues enter/exit; rescue combat, escort, reward and cleanup regressions passed; all M01 stages and completed-state next steps; four rendered lounge NPCs; canonical public name with legacy ID; TALK opens guidance; EXIT refreshes street objective; no invented story progress or duplicate rewards. Real preferences restored.");
                }
            }
            catch(Exception ex){Finish(1,ex.ToString());}
        }
        static void Finish(int code,string report)
        {
            foreach(var key in Strings){if(SessionState.GetBool(Key+key+"Exists",false))PlayerPrefs.SetString(key,SessionState.GetString(Key+key,""));else PlayerPrefs.DeleteKey(key);}
            foreach(var key in Ints){if(SessionState.GetBool(Key+key+"Exists",false))PlayerPrefs.SetInt(key,SessionState.GetInt(Key+key,0));else PlayerPrefs.DeleteKey(key);}
            PlayerPrefs.Save();
            File.WriteAllText("/private/tmp/harare-venue-validation.txt",report);Debug.Log(report);SessionState.EraseBool(Key);EditorApplication.isPlaying=false;EditorApplication.Exit(code);
        }
        static void CaptureRoom(string path="/private/tmp/harare-grocer-interior.png")
        {
            var obj=new GameObject("Interior validation camera");var camera=obj.AddComponent<Camera>();
            camera.transform.position=VenueInterior.Origin+new Vector3(0,2.3f,-6.4f);
            camera.transform.LookAt(VenueInterior.Origin+new Vector3(0,1.4f,2));camera.fieldOfView=75;
            var rt=new RenderTexture(1280,720,24);camera.targetTexture=rt;camera.Render();
            var previous=RenderTexture.active;RenderTexture.active=rt;var image=new Texture2D(1280,720,TextureFormat.RGB24,false);
            image.ReadPixels(new Rect(0,0,1280,720),0,0);image.Apply();File.WriteAllBytes(path,image.EncodeToPNG());
            RenderTexture.active=previous;camera.targetTexture=null;rt.Release();UnityEngine.Object.DestroyImmediate(rt);UnityEngine.Object.DestroyImmediate(image);UnityEngine.Object.DestroyImmediate(obj);
        }
    }
}
#endif
