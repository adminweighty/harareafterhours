#if UNITY_EDITOR
using System;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
namespace HarareAfterHours.EditorTools
{
    [InitializeOnLoad]
    public static class TouchControlsValidation
    {
        const string Key="Harare.TouchControlsValidation",Folder="/private/tmp/harare-touch-controls/";
        static double deadline;static int stage;static float started;static Vector3 origin;
        static readonly string[] SavedInts={MissionDirector.SaveKey,JoinaCityBlock.IntroSaveKey,PlayerLoadout.StarterWeaponKey};
        static readonly string[] SavedStrings={PlayerLoadout.SaveKey,TouchGameplayControls.SettingsKey};
        static TouchControlsValidation(){EditorApplication.update+=Tick;}
        public static void Run()
        {
            Directory.CreateDirectory(Folder);stage=0;deadline=0;started=0;
            foreach(var save in SavedInts){SessionState.SetBool(Key+save+"exists",PlayerPrefs.HasKey(save));SessionState.SetInt(Key+save,PlayerPrefs.GetInt(save));}
            foreach(var save in SavedStrings){SessionState.SetBool(Key+save+"exists",PlayerPrefs.HasKey(save));SessionState.SetString(Key+save,PlayerPrefs.GetString(save));}
            PlayerPrefs.DeleteKey(PlayerLoadout.SaveKey);PlayerPrefs.DeleteKey(PlayerLoadout.StarterWeaponKey);PlayerPrefs.DeleteKey(TouchGameplayControls.SettingsKey);
            PlayerPrefs.SetInt(JoinaCityBlock.IntroSaveKey,1);
            PlayerPrefs.SetInt(MissionDirector.SaveKey,(int)MissionState.WalkToVehicle);PlayerPrefs.Save();
            EditorSceneManager.OpenScene("Assets/Scenes/SampleScene.unity");SessionState.SetBool(Key,true);EditorApplication.isPlaying=true;
        }
        static void Check(bool ok,string why){if(!ok)throw new Exception(why);}
        static void Place(ThirdPersonController player,Vector3 position)
        {
            var controller=player.GetComponent<CharacterController>();controller.enabled=false;player.transform.position=position;controller.enabled=true;Physics.SyncTransforms();
        }
        static void RestoreSaves()
        {
            foreach(var save in SavedInts)
            {
                if(SessionState.GetBool(Key+save+"exists",false))PlayerPrefs.SetInt(save,SessionState.GetInt(Key+save,0));
                else PlayerPrefs.DeleteKey(save);
            }
            foreach(var save in SavedStrings)
            {
                if(SessionState.GetBool(Key+save+"exists",false))PlayerPrefs.SetString(save,SessionState.GetString(Key+save,""));
                else PlayerPrefs.DeleteKey(save);
            }
            PlayerPrefs.Save();
        }
        static void Tick()
        {
            if(!SessionState.GetBool(Key,false)||!EditorApplication.isPlaying)return;
            if(deadline==0)deadline=EditorApplication.timeSinceStartup+100;
            if(Time.time<3&&EditorApplication.timeSinceStartup<deadline)return;
            try
            {
                Check(EditorApplication.timeSinceStartup<deadline,"Timeout");
                var game=HarareAfterHoursBootstrap.Instance;Check(game!=null&&game.Player!=null,"World loaded");
                var touch=TouchGameplayControls.Instance;Check(touch!=null,"Touch controls installed");
                if(stage==0)
                {
                    Check(game.Combat.EquippedWeapon=="Pulse pistol","Starter pistol restored for fresh and legacy saves");
                    Check(game.Player.GetComponent<PlayerEquipmentVisual>().IsHoldingWeapon,"Starter pistol is visible in the player's hand");
                    Check(TouchGameplayControls.Analog(new Vector2(3,3),62)==Vector2.zero,"Dead zone");
                    Check(TouchGameplayControls.Analog(Vector2.one*100,62).magnitude<=1.001f,"Diagonal normalization");
                    Check(TouchGameplayControls.Analog(Vector2.up*30,62).magnitude<.5f,"Analog walk speed");
                    foreach(var a in new[]{TouchGameplayControls.Action.Up,TouchGameplayControls.Action.Down,TouchGameplayControls.Action.Left,TouchGameplayControls.Action.Right,TouchGameplayControls.Action.Attack,TouchGameplayControls.Action.Fight,TouchGameplayControls.Action.Kick,TouchGameplayControls.Action.Jump,TouchGameplayControls.Action.Use,TouchGameplayControls.Action.Ride,TouchGameplayControls.Action.View,TouchGameplayControls.Action.Reload,TouchGameplayControls.Action.SwitchWeapon,TouchGameplayControls.Action.Perspective,TouchGameplayControls.Action.Crouch,TouchGameplayControls.Action.Sprint,TouchGameplayControls.Action.SettingsMenu})
                    {var rect=touch.ButtonRect(a);Check(touch.Safe.Contains(rect.min)&&touch.Safe.Contains(rect.max-Vector2.one),"Safe button "+a);Check(touch.HitTest(rect.center)==a,"Unique button hit region "+a);Check(touch.VisibleOpacity(a)>=.58f,"Readable button opacity "+a);}
                    var up=touch.ButtonRect(TouchGameplayControls.Action.Up).center;var left=touch.ButtonRect(TouchGameplayControls.Action.Left).center;
                    touch.ProcessPointer(-20,up,Vector2.zero,true,true);touch.ProcessPointer(-21,left,Vector2.zero,true,true);touch.ApplyHeld();
                    Check(MobileControlState.Move.y>.5f&&MobileControlState.Move.x<-.5f,"D-pad supports normalized diagonal movement");
                    touch.ProcessPointer(-20,Vector2.zero,Vector2.zero,false,false);touch.ProcessPointer(-21,Vector2.zero,Vector2.zero,false,false);touch.Cancel();
                    touch.Cancel();origin=game.Player.transform.position;
                    var home=touch.StickHome;touch.ProcessPointer(-10,home,Vector2.zero,true,true);
                    touch.ProcessPointer(-10,home+Vector2.up*(-touch.Radius*.7f),Vector2.zero,false,true);
                    var look=new Vector2(Screen.width*.60f,Screen.height*.48f);
                    Check(touch.HitTest(look)==TouchGameplayControls.Action.Look,"Free camera region");
                    touch.ProcessPointer(-11,look,new Vector2(25,0),true,true);
                    touch.ApplyHeld();Check(MobileControlState.Move.y>.5f&&!MobileControlState.Sprint,"Walk and camera together");
                    started=Time.time;stage++;return;
                }
                if(stage==1)
                {
                    if(Time.time-started<.8f)return;
                    Check(Vector3.Distance(game.Player.transform.position,origin)>.45f,"Live movement from held pointer");
                    touch.ProcessPointer(-10,touch.StickHome+Vector2.up*(-touch.Radius*1.3f),Vector2.zero,false,true);touch.ApplyHeld();
                    Check(MobileControlState.Sprint&&MobileControlState.Move.y>.95f,"Outer-ring sprint");
                    var jump=touch.ButtonRect(TouchGameplayControls.Action.Jump).center;
                    touch.ProcessPointer(-12,jump,Vector2.zero,true,true);Check(MobileControlState.ConsumeJump(),"Jump while moving and looking");
                    touch.ProcessPointer(-12,touch.ButtonRect(TouchGameplayControls.Action.Attack).center,Vector2.zero,false,true);
                    Check(!MobileControlState.ConsumePulse(),"Dragging between buttons cannot fire");
                    touch.ProcessPointer(-10,Vector2.zero,Vector2.zero,false,false);touch.ProcessPointer(-11,Vector2.zero,Vector2.zero,false,false);touch.ProcessPointer(-12,Vector2.zero,Vector2.zero,false,false);touch.ApplyHeld();
                    Check(MobileControlState.Move==Vector2.zero&&!MobileControlState.Sprint,"Immediate release stops movement");
                    touch.ProcessPointer(-10,touch.StickHome,Vector2.zero,true,true);touch.ProcessPointer(-10,touch.StickHome+Vector2.right*100,Vector2.zero,false,true);
                    Time.timeScale=0;touch.ApplyHeld();Check(MobileControlState.Move==Vector2.zero,"Pause cancels held controls");Time.timeScale=1;
                    touch.SendMessage("OnApplicationFocus",false);touch.ProcessPointer(-10,touch.StickHome,Vector2.zero,true,true);touch.ApplyHeld();Check(MobileControlState.Move==Vector2.zero,"Focus-loss input blocked");touch.SendMessage("OnApplicationFocus",true);
                    game.Player.InputLocked=true;touch.ProcessPointer(-12,jump,Vector2.zero,true,true);Check(!MobileControlState.ConsumeJump(),"Arrest input lock respected");game.Player.InputLocked=false;
                    touch.Cancel();
                    stage++;return;
                }
                if(stage==2)
                {
                    var car=game.FeaturedVehicle;Place(game.Player,car.transform.position-car.transform.right*1.8f+Vector3.up*.1f);
                    Check(game.Player.TryBoardNearest(false)&&game.Player.IsDriving,"Board mission car for touch-drive test");
                    // The live router intentionally clears touches during the
                    // on-foot-to-vehicle transition. Wait for that frame before
                    // injecting the new driver's two-finger input.
                    started=Time.time;stage++;return;
                }
                if(stage==3)
                {
                    if(Time.time-started<.15f)return;
                    Check(game.Player.IsDriving,"Driver remains seated after touch-transition reset");
                    foreach(var action in new[]{TouchGameplayControls.Action.Left,TouchGameplayControls.Action.Right,TouchGameplayControls.Action.Go,TouchGameplayControls.Action.Reverse,TouchGameplayControls.Action.Brake,TouchGameplayControls.Action.Use,TouchGameplayControls.Action.View})
                    {
                        var rect=touch.ButtonRect(action);
                        Check(touch.Safe.Contains(rect.min)&&touch.Safe.Contains(rect.max-Vector2.one),"Safe driver button "+action);
                        Check(touch.HitTest(rect.center)==action,"Unique driver button hit region "+action);
                    }
                    var accelerate=touch.ButtonRect(TouchGameplayControls.Action.Go).center;
                    var left=touch.ButtonRect(TouchGameplayControls.Action.Left).center;
                    touch.ProcessPointer(-30,accelerate,Vector2.zero,true,true);touch.ProcessPointer(-31,left,Vector2.zero,true,true);touch.ApplyHeld();
                    Check(MobileControlState.Move.y>.5f&&MobileControlState.Move.x<-.5f,"ACCEL and LEFT combine as throttle and steering");
                    origin=game.FeaturedVehicle.transform.position;started=Time.time;stage++;return;
                }
                if(stage==4)
                {
                    var car=game.FeaturedVehicle;touch.ApplyHeld();
                    if(Time.time-started<1.2f)return;
                    Check(Vector3.Distance(car.transform.position,origin)>.35f&&car.SpeedKph>2,"ACCEL touch moves the physical vehicle");
                    Check(Mathf.Abs(car.Wheels[0].steerAngle)>3,"LEFT touch reaches the front-wheel steering");
                    touch.ProcessPointer(-30,Vector2.zero,Vector2.zero,false,false);touch.ProcessPointer(-31,Vector2.zero,Vector2.zero,false,false);
                    touch.ProcessPointer(-32,touch.ButtonRect(TouchGameplayControls.Action.Reverse).center,Vector2.zero,true,true);touch.ApplyHeld();
                    Check(MobileControlState.Move.y<-.9f,"REV touch supplies reverse throttle");
                    touch.ProcessPointer(-32,Vector2.zero,Vector2.zero,false,false);
                    touch.ProcessPointer(-33,touch.ButtonRect(TouchGameplayControls.Action.Brake).center,Vector2.zero,true,true);touch.ApplyHeld();
                    Check(MobileControlState.Brake,"BRAKE touch reaches the vehicle controller");
                    touch.Cancel();RestoreSaves();
                    File.WriteAllText(Folder+"validation.txt","PASS: visible starter pistol plus FIRE, RELOAD, weapon switch, four-button D-pad and every on-foot action inside the safe area at readable opacity.\nPASS: live walking with camera finger; three-finger move/look/jump; sprint; no slide-to-fire; release, pause, focus and input-lock cancellation.\nPASS: separate driver ACCEL, REV and BRAKE buttons have unique safe hit regions; ACCEL + LEFT delivers simultaneous throttle and steering; the physical vehicle moves and turns; reverse and brake reach the driving input. Real saves restored after validation.\nPhysical-device comfort and live capacitive touch still require device testing.");
                    SessionState.EraseBool(Key);EditorApplication.isPlaying=false;EditorApplication.Exit(0);
                }
            }
            catch(Exception e){Time.timeScale=1;RestoreSaves();File.WriteAllText(Folder+"validation.txt",e.ToString());Debug.LogException(e);SessionState.EraseBool(Key);EditorApplication.isPlaying=false;EditorApplication.Exit(1);}
        }
    }
}
#endif
