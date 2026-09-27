#if UNITY_EDITOR
using System;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
namespace HarareAfterHours.EditorTools
{
    [InitializeOnLoad]
    public static class ShooterExperienceValidation
    {
        const string Key="Harare.ShooterExperienceValidation";
        static int stage,frame,ammo;static float started,height,health;static double deadline;
        static string saved;static StreetActor enemy;static GameObject cover;
        static Quaternion gaze;
        static ShooterExperienceValidation(){EditorApplication.update+=Tick;}
        public static void Run()
        {
            Directory.CreateDirectory("/private/tmp/harare-shooter-tests");
            SessionState.SetString(Key+"Prefs",PlayerPrefs.GetString(TouchGameplayControls.SettingsKey,""));
            EditorSceneManager.OpenScene("Assets/Scenes/SampleScene.unity");SessionState.SetBool(Key,true);EditorApplication.isPlaying=true;
        }
        public static void Preview()
        {
            EditorSceneManager.OpenScene("Assets/Scenes/SampleScene.unity");EditorApplication.isPlaying=true;
        }
        static void Check(bool ok,string why){if(!ok)throw new Exception(why);Debug.Log("SHOOTER PASS: "+why);}
        static void Next(){stage++;started=Time.time;}
        static void Tick()
        {
            if(!SessionState.GetBool(Key,false)||!EditorApplication.isPlaying||frame==Time.frameCount)return;
            frame=Time.frameCount;if(deadline==0)deadline=EditorApplication.timeSinceStartup+180;
            try
            {
                if(EditorApplication.timeSinceStartup>deadline)throw new Exception("Test timeout stage "+stage);
                var game=HarareAfterHoursBootstrap.Instance;if(game?.Player==null||Time.time<3)return;
                var player=game.Player;var touch=TouchGameplayControls.Instance;var cc=player.GetComponent<CharacterController>();
                var combat=player.GetComponent<PlayerCombat>();var world=StreetActionDirector.Instance;
                switch(stage)
                {
                    case 0:
                        touch.BeginEdit();touch.ResetLayout();Check(touch.LayoutValid(),"Default layout has no collisions at "+Screen.width+"x"+Screen.height);
                        Check(touch.FinishEdit(true),"Layout save succeeds");saved=PlayerPrefs.GetString(TouchGameplayControls.SettingsKey);
                        touch.BeginEdit();touch.ResetLayout();Check(touch.FinishEdit(false),"Cancel edit succeeds");Check(PlayerPrefs.GetString(TouchGameplayControls.SettingsKey)==saved,"Cancel preserves saved layout");
                        foreach(var actor in world.Actors)actor.gameObject.SetActive(false);
                        var floor=GameObject.CreatePrimitive(PrimitiveType.Cube);floor.transform.position=new Vector3(10000,-.5f,10000);floor.transform.localScale=new Vector3(100,1,100);
                        cc.enabled=false;player.transform.position=new Vector3(10000,.1f,10000);cc.enabled=true;
                        combat.Equip("Pulse rifle");height=cc.height;Next();break;
                    case 1:
                        if(Time.time-started<.4f)return;
                        var home=touch.StickHome;touch.ProcessPointer(-31,home,Vector2.zero,true,true);touch.ProcessPointer(-31,home+Vector2.up*(-touch.Radius*1.3f),Vector2.zero,false,true);touch.ApplyHeld();Next();break;
                    case 2:
                        if(Time.time-started<.7f)return;
                        Check(player.CurrentSpeed>4,"Sprint accelerates beyond walking speed");ammo=combat.Ammo;
                        gaze=Camera.main.transform.rotation;
                        touch.ProcessPointer(-32,touch.ButtonRect(TouchGameplayControls.Action.Attack).center,Vector2.zero,true,true);
                        touch.ProcessPointer(-33,new Vector2(touch.Safe.xMin+5,touch.Safe.center.y),new Vector2(4,0),true,true);touch.ApplyHeld();Next();break;
                    case 3:
                        if(Time.time-started<.35f)return;
                        Check(Quaternion.Angle(gaze,Camera.main.transform.rotation)>.1f,"Free-space drag changes the camera while sprinting and firing");
                        Check(!combat.IsAiming,"Default fire does not force ADS");Check(combat.Ammo<ammo,"Automatic fire consumes ammunition");Check(player.CurrentSpeed>4,"Run and fire stay simultaneous");
                        touch.Cancel();player.ToggleCrouchSlide();Check(player.IsSliding,"Sprint to slide starts");Check(cc.height<height*.7f,"Slide lowers collision capsule");Next();break;
                    case 4:
                        if(Time.time-started<.8f)return;
                        Check(player.GetComponentInChildren<Animator>().GetBool("Crouched"),"Crouch drives humanoid stance animation");
                        Check(!player.IsSliding&&player.IsCrouching,"Slide finishes crouched");player.ToggleCrouchSlide();Check(!player.IsCrouching,"Stand restores stance");Check(Mathf.Abs(cc.height-height)<.001f,"Standing capsule restored");
                        MobileControlState.RequestJump();Next();break;
                    case 5:
                        if(Time.time-started<.25f)return;
                        Check(!cc.isGrounded,"Jump becomes airborne");Check(!player.GetComponentInChildren<Animator>().GetBool("Grounded"),"Jump drives humanoid airborne animation");Next();break;
                    case 6:
                        if(Time.time-started<1.3f)return;
                        Check(cc.isGrounded,"Jump lands");
                        var camera=Camera.main.GetComponent<ThirdPersonCameraRig>();Check(camera.PlayerView,"PLAYER VIEW is default");camera.TogglePerspective();Check(camera.CurrentView,"CURRENT VIEW is option two");camera.TogglePerspective();
                        typeof(StreetActionDirector).GetProperty("GraceUntil").SetValue(world,0f);
                        enemy=world.SpawnActor(false,player.transform.position+Vector3.forward*8,"Validation armed robber");enemy.Arm();health=world.Health;Next();break;
                    case 7:
                        if(Time.time-started<3)return;
                        Check(world.Health<health&&world.Health>0,"Armed NPC hits without instant game over");health=world.Health;
                        cover=GameObject.CreatePrimitive(PrimitiveType.Cube);cover.transform.position=(player.transform.position+enemy.transform.position)*.5f+Vector3.up;cover.transform.localScale=new Vector3(5,4,1);Physics.SyncTransforms();Next();break;
                    case 8:
                        if(Time.time-started<3)return;
                        Check(world.Health==health,"Solid cover blocks NPC fire");enemy.Hit(2);enemy.Hit(2);Check(enemy.Down,"Hostile can be defeated");
                        UnityEngine.Object.Destroy(cover);
                        Next();break;
                    case 9:
                        if(Time.time-started<.5f)return;
                        MobileControlState.SetMove(Vector2.down);Next();break;
                    case 10:
                        MobileControlState.SetMove(Vector2.down);
                        if(Time.time-started<.4f)return;
                        Check(player.GetComponentInChildren<Animator>().GetFloat("GaitDirection")<0,"Backpedal reverses the grounded gait: speed="+player.CurrentSpeed+" dot="+Vector3.Dot(cc.velocity,player.transform.forward));
                        Finish("PASS: layout save/cancel; sprint + fire + look; slide capsule; crouch/stand; jump/landing; PLAYER/CURRENT camera modes; NPC damage, cover and defeat.",0);break;
                }
            }
            catch(Exception e){Debug.LogException(e);Finish("FAIL stage "+stage+": "+e,1);}
        }
        static void Finish(string report,int code)
        {
            var old=SessionState.GetString(Key+"Prefs","");if(old=="")PlayerPrefs.DeleteKey(TouchGameplayControls.SettingsKey);else PlayerPrefs.SetString(TouchGameplayControls.SettingsKey,old);PlayerPrefs.Save();
            File.WriteAllText("/private/tmp/harare-shooter-tests/result.txt",report);SessionState.EraseBool(Key);Time.timeScale=1;EditorApplication.isPlaying=false;EditorApplication.Exit(code);
        }
    }
}
#endif
