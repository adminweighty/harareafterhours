#if UNITY_EDITOR
using System;
using System.IO;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
namespace HarareAfterHours.EditorTools
{
    [InitializeOnLoad]
    public static class InputQuitValidation
    {
        const string Key="Harare.InputQuitQA";
        static double deadline;static int stage;static HarareAfterHoursBootstrap original;
        static Mouse mouse;
        static InputQuitValidation(){EditorApplication.update+=Tick;}
        public static void Run()
        {
            SessionState.SetBool(Key+"SettingsExist",PlayerPrefs.HasKey(TouchGameplayControls.SettingsKey));
            SessionState.SetString(Key+"Settings",PlayerPrefs.GetString(TouchGameplayControls.SettingsKey,""));
            PlayerPrefs.DeleteKey(TouchGameplayControls.SettingsKey);PlayerPrefs.Save();
            EditorSceneManager.OpenScene("Assets/Scenes/SampleScene.unity");SessionState.SetBool(Key,true);EditorApplication.isPlaying=true;
        }
        static void Check(bool ok,string label){if(!ok)throw new Exception(label);}
        static bool Fire(PlayerCombat combat)=>(bool)typeof(PlayerCombat).GetMethod("WasPulseRequested",BindingFlags.Instance|BindingFlags.NonPublic).Invoke(combat,null);
        static void Tick()
        {
            if(!SessionState.GetBool(Key,false)||!EditorApplication.isPlaying)return;
            try
            {
                if(deadline==0)deadline=EditorApplication.timeSinceStartup+90;
                Check(EditorApplication.timeSinceStartup<deadline,"Input/session test timeout");
                var game=HarareAfterHoursBootstrap.Instance;if(game==null||Time.time<3)return;
                if(stage==0)
                {
                    original=game;var touch=TouchGameplayControls.Instance;game.Combat.Equip("Pulse rifle");
                    var rig=Camera.main.GetComponent<ThirdPersonCameraRig>();
                    Check(rig.PlayerView&&!rig.CurrentView,"Full-character PLAYER VIEW is the default");rig.TogglePerspective();
                    Check(rig.CurrentView&&!rig.PlayerView,"Option two selects the previous CURRENT VIEW");rig.TogglePerspective();
                    Check(rig.PlayerView,"Camera toggle restores PLAYER VIEW");
                    var yaw=typeof(ThirdPersonCameraRig).GetField("_yaw",BindingFlags.Instance|BindingFlags.NonPublic);
                    float originalYaw=(float)yaw.GetValue(rig);
                    var free=touch.Safe.center;
                    Check(touch.HitTest(free)==TouchGameplayControls.Action.Look,"Unused screen belongs to look");
                    touch.ProcessPointer(990,free,Vector2.zero,true,true);
                    touch.ProcessPointer(990,free+Vector2.right*40,Vector2.right*40,false,true);
                    Check((float)yaw.GetValue(rig)>originalYaw,"Free-area swipe rotates third-person camera");
                    rig.TogglePerspective();originalYaw=(float)yaw.GetValue(rig);
                    touch.ProcessPointer(990,free+Vector2.right*80,Vector2.right*40,false,true);
                    Check((float)yaw.GetValue(rig)>originalYaw,"Same swipe rotates CURRENT VIEW camera");
                    touch.Cancel();rig.TogglePerspective();
                    touch.BeginEdit();Check(touch.Editing&&Time.timeScale==0,"Control editor pauses gameplay");
                    var editTouch=InputSystem.AddDevice<Touchscreen>();
                    var oldReload=touch.ButtonRect(TouchGameplayControls.Action.Reload).center;
                    var editorUpdate=typeof(TouchGameplayControls).GetMethod("UpdateEditorTouch",BindingFlags.Instance|BindingFlags.NonPublic);
                    InputSystem.QueueStateEvent(editTouch,new TouchState{touchId=1,phase=UnityEngine.InputSystem.TouchPhase.Began,position=new Vector2(oldReload.x,Screen.height-oldReload.y)});InputSystem.Update();editorUpdate.Invoke(touch,null);
                    var newReload=oldReload+Vector2.left*35;
                    InputSystem.QueueStateEvent(editTouch,new TouchState{touchId=1,phase=UnityEngine.InputSystem.TouchPhase.Moved,position=new Vector2(newReload.x,Screen.height-newReload.y),delta=Vector2.left*35});InputSystem.Update();editorUpdate.Invoke(touch,null);
                    Check(Vector2.Distance(touch.ButtonRect(TouchGameplayControls.Action.Reload).center,oldReload)>20,$"Direct touch repositions reload control: {oldReload} -> {touch.ButtonRect(TouchGameplayControls.Action.Reload).center}; touch {editTouch.primaryTouch.position.ReadValue()} delta {editTouch.primaryTouch.delta.ReadValue()}");
                    InputSystem.RemoveDevice(editTouch);
                    game.Bridge.OnFlutterMessage("{\"type\":\"resume\"}");Check(Time.timeScale==0,"Resume cannot unpause control editor");
                    Check(touch.FinishEdit(false)&&!touch.Editing&&Time.timeScale==1,"Cancel control editor resumes play");
                    Check(game.Combat.Ammo==30,"Rifle magazine starts full");
                    typeof(PlayerCombat).GetField("_nextPulseAt",BindingFlags.Instance|BindingFlags.NonPublic).SetValue(game.Combat,0f);
                    game.Combat.TryAttack();
                    Check(game.Combat.Ammo==29,"Shot consumes one round");game.Combat.Reload();
                    Check(game.Combat.Reloading,"Reload starts");game.Combat.TryAttack();
                    Check(game.Combat.Ammo==29,"Reload blocks fire");game.Combat.Equip("Pulse pistol");game.Combat.Equip("Pulse rifle");
                    Check(game.Combat.Ammo==29&&!game.Combat.Reloading,"Switch cancels reload without replenishing ammunition");
                    Check(!game.Combat.IsAiming,"No aiming on initial movement screen");
                    mouse=InputSystem.AddDevice<Mouse>();InputSystem.QueueStateEvent(mouse,new MouseState().WithButton(MouseButton.Left));InputSystem.Update();
                    touch.Cancel();var p=touch.StickHome;
                    touch.ProcessPointer(901,p,Vector2.zero,true,true);touch.ProcessPointer(901,p+Vector2.up*35,Vector2.up*35,false,true);touch.ApplyHeld();
                    Check(MobileControlState.Move.sqrMagnitude>0,"Joystick still moves");Check(!Fire(game.Combat),"Joystick mouse click cannot shoot");
                    touch.ProcessPointer(902,touch.ButtonRect(TouchGameplayControls.Action.Attack).center,Vector2.zero,true,true);
                    Check(Fire(game.Combat),"Separate FIRE works concurrently with movement");
                    Check(!game.Combat.IsAiming,"Default FIRE remains hip-fire unless Aim when firing is enabled");
                    touch.ProcessPointer(902,touch.ButtonRect(TouchGameplayControls.Action.Attack).center+Vector2.up*160,Vector2.up*160,false,true);
                    touch.ApplyHeld();Check(Fire(game.Combat),"Rifle keeps firing during aim drag outside button");
                    touch.ProcessPointer(902,Vector2.zero,Vector2.zero,false,false);
                    touch.ApplyHeld();Check(!Fire(game.Combat),"Releasing firing finger stops automatic shots");
                    touch.Cancel();
                    touch.ProcessPointer(903,touch.ButtonRect(TouchGameplayControls.Action.View).center,Vector2.zero,true,true);
                    Check(game.Combat.IsAiming&&!Fire(game.Combat),"AIM toggles without firing");
                    touch.ProcessPointer(903,touch.ButtonRect(TouchGameplayControls.Action.Attack).center,Vector2.zero,false,true);
                    Check(!Fire(game.Combat),"Dragging AIM finger onto FIRE does not change ownership");
                    game.Bridge.OnFlutterMessage("{\"type\":\"end_session\"}");
                    Check(Time.timeScale==0&&AudioListener.pause&&MobileControlState.Move==Vector2.zero&&!game.Combat.IsAiming,"Quit clears input/aim and stops gameplay/audio");
                    Check(game.Bridge.LastEvent.Contains("session_ended"),"Save/quit acknowledgment emitted");
                    game.Bridge.OnFlutterMessage("{\"type\":\"resume\"}");Check(Time.timeScale==0,"Late resume cannot reopen a closed session");
                    game.Bridge.OnFlutterMessage("{\"type\":\"restart_session\"}");stage=1;
                }
                else
                {
                    if(game==original)return; // Scene reload completes on the next player frame.
                    Check(game!=original&&Time.timeScale==1&&!AudioListener.pause,"Start game creates a fresh running session");
                    Finish(0,"PASS: movement never shoots from mouse fallback; FIRE works alongside movement; AIM toggle does not fire; pointer ownership survives dragging across buttons; quit clears input and aim, stops time/audio and acknowledges save; late resume blocked; start reloads a fresh scene.");
                }
            }
            catch(Exception ex){Finish(1,ex.ToString());}
        }
        static void Finish(int code,string report)
        {
            if(mouse!=null)InputSystem.RemoveDevice(mouse);Time.timeScale=1;AudioListener.pause=false;
            if(SessionState.GetBool(Key+"SettingsExist",false))PlayerPrefs.SetString(TouchGameplayControls.SettingsKey,SessionState.GetString(Key+"Settings",""));
            else PlayerPrefs.DeleteKey(TouchGameplayControls.SettingsKey);PlayerPrefs.Save();
            File.WriteAllText("/private/tmp/harare-input-quit-validation.txt",report);Debug.Log(report);SessionState.EraseBool(Key);EditorApplication.isPlaying=false;EditorApplication.Exit(code);
        }
    }
}
#endif
