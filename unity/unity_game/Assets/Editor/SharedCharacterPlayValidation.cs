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
    public static class SharedCharacterPlayValidation
    {
        const string Key="Harare.SharedCharacterPlayValidation";
        static int stage, frame;
        static float started, groundY, apex, walkingSpeed;
        static double deadline;
        static Vector3 origin;
        static readonly List<string> Report=new();
        static string error;
        static SharedCharacterPlayValidation()
        {
            EditorApplication.update+=Tick;
            Application.logMessageReceived+=(message,stack,type)=>{
                if(SessionState.GetBool(Key,false) && (type==LogType.Error || type==LogType.Exception || type==LogType.Assert)) error=message;
            };
        }
        public static void Run()
        {
            EditorSceneManager.OpenScene("Assets/Scenes/SampleScene.unity");
            SessionState.SetBool(Key,true);
            EditorApplication.isPlaying=true;
        }
        static void Tick()
        {
            if(!SessionState.GetBool(Key,false) || !EditorApplication.isPlaying || frame==Time.frameCount) return;
            frame=Time.frameCount;
            try
            {
                if(deadline==0) deadline=EditorApplication.timeSinceStartup+100;
                Require(EditorApplication.timeSinceStartup<deadline,"Test completes before timeout");
                Require(error==null,"No runtime exception: "+error);
                var game=UnityEngine.Object.FindFirstObjectByType<HarareAfterHoursBootstrap>();
                if(game==null || game.Player==null || Time.time<2) return;
                var player=game.Player;
                var controller=player.GetComponent<CharacterController>();
                var animator=player.GetComponentInChildren<Animator>();
                switch(stage)
                {
                    case 0:
                        Require(game.Characters.CastCount==24,"24 cast roles");
                        Require(UnityEngine.Object.FindObjectsByType<NpcWanderer>(FindObjectsSortMode.None).Length>=23,"Original cast remains alongside expanded city population");
                        Require(animator!=null && animator.enabled && animator.isHuman,"Player uses enabled shared Humanoid Animator");
                        Camera.main.GetComponent<ThirdPersonCameraRig>().enabled=false;
                        Camera.main.transform.SetPositionAndRotation(new Vector3(8.8f,2.1f,17),Quaternion.identity);
                        controller.enabled=false; player.transform.position=new Vector3(8.8f,.3f,20); controller.enabled=true;
                        Physics.SyncTransforms();
                        Next();
                        break;
                    case 1:
                        if(Time.time-started<.3f) break;
                        origin=player.transform.position; Next(); break;
                    case 2:
                        MobileControlState.SetMove(Vector2.up);
                        if(Time.time-started<1.2f) break;
                        walkingSpeed=player.CurrentSpeed;
                        Require(walkingSpeed>1.5f && walkingSpeed<2.1f,"Walking is approximately 1.8m/s");
                        Require(player.transform.position.z-origin.z>1.7f,"Walk input actually moves on pavement");
                        Require(animator.GetFloat("Speed")>1.3f,"Walk speed reaches Animator");
                        Report.Add("PASS walking: "+walkingSpeed.ToString("F2")+"m/s");
                        origin=player.transform.position; Next(); break;
                    case 3:
                        MobileControlState.SetMove(Vector2.up); MobileControlState.SetSprint();
                        if(Time.time-started<.65f) break;
                        Require(player.CurrentSpeed>walkingSpeed*2.4f,"Mobile RUN genuinely sprints");
                        Require(animator.GetFloat("Speed")>4.5f,"Run reaches sprint blend");
                        Report.Add("PASS sprint: "+player.CurrentSpeed.ToString("F2")+"m/s");
                        MobileControlState.SetMove(Vector2.zero); Next(); break;
                    case 4:
                        if(Time.time-started<.3f) break;
                        Require(player.CurrentSpeed<.1f,"Input release stops movement");
                        Require(controller.isGrounded,"Ground contact before jump");
                        groundY=player.transform.position.y; apex=groundY;
                        MobileControlState.RequestJump(); Next(); break;
                    case 5:
                        apex=Mathf.Max(apex,player.transform.position.y);
                        if(Time.time-started<.25f) break;
                        Require(!controller.isGrounded,"Touch jump leaves ground");
                        Require(!animator.GetBool("Grounded"),"Airborne state reaches Animator");
                        // A second jump request in midair must not double-jump.
                        MobileControlState.RequestJump();
                        Report.Add("PASS touch jump / airborne animation");
                        Next(); break;
                    case 6:
                        apex=Mathf.Max(apex,player.transform.position.y);
                        if(Time.time-started<1.2f) break;
                        Require(controller.isGrounded,"Jump lands");
                        Require(apex-groundY>.8f && apex-groundY<1.7f,"Jump height sane and no unintended double-jump");
                        Require(Mathf.Abs(player.transform.position.y-groundY)<.08f,"Lands back on pavement");
                        Require(animator.GetBool("Grounded"),"Landing reaches Animator");
                        Report.Add("PASS landing; apex "+(apex-groundY).ToString("F2")+"m");
                        Next(); break;
                    case 7:
                        MobileControlState.SetMove(Vector2.down);
                        if(Time.time-started<.45f) break;
                        Vector3 reverse=-Camera.main.GetComponent<ThirdPersonCameraRig>().MovementForward(Vector2.down);
                        Require(Vector3.Dot(player.transform.forward,reverse)>.95f,"Player turns toward camera-relative reverse movement");
                        Require(Vector3.Dot(animator.GetBoneTransform(HumanBodyBones.Head).forward,player.transform.forward)>.6f,"Animated face follows controller facing");
                        Report.Add("PASS direction change and face alignment");
                        MobileControlState.SetMove(Vector2.zero);
                        Next(); break;
                    case 8:
                        if(Time.time-started<.3f) break;
                        Require(player.CurrentSpeed<.1f,"Movement buffer expires");
                        Report.Add("PASS 24 shared characters and no runtime errors");
                        Finish(0); break;
                }
            }
            catch(Exception e) { Report.Add("FAIL: "+e.Message); Debug.LogException(e); Finish(1); }
        }
        static void Next() { stage++; started=Time.time; }
        static void Require(bool condition,string message) {if(!condition)throw new InvalidOperationException(message);}
        static void Finish(int code)
        {
            File.WriteAllLines("/private/tmp/harare-character-repair/play-validation.txt",Report);
            Debug.Log("[SharedCharacterPlayValidation] "+(code==0?"PASS":"FAIL")+"\n"+string.Join("\n",Report));
            SessionState.EraseBool(Key); EditorApplication.isPlaying=false; EditorApplication.Exit(code);
        }
    }
}
#endif
