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
    public static class SharedVehicleValidation
    {
        const string Key="Harare.SharedVehicleValidation";
        static int stage,frame;
        static float started;
        static double deadline;
        static Vector3 origin;
        static Quaternion rotation;
        static VehicleController ambient;
        static readonly List<string> Report=new();
        static string runtimeError;
        static SharedVehicleValidation()
        {
            EditorApplication.update+=Tick;
            Application.logMessageReceived+=(m,s,t)=>{if(SessionState.GetBool(Key,false)&&(t==LogType.Error||t==LogType.Exception||t==LogType.Assert))runtimeError=m;};
        }
        public static void Run()
        {
            SessionState.SetInt(Key+"Stage",PlayerPrefs.GetInt(MissionDirector.SaveKey,-1));
            SessionState.SetInt(Key+"Clean",PlayerPrefs.GetInt(MissionDirector.SaveKey+".clean",-1));
            SessionState.SetString(Key+"Points",PlayerPrefs.GetString(PlayerProgression.SaveKey,""));
            PlayerPrefs.SetInt(MissionDirector.SaveKey,(int)MissionState.WalkToVehicle);
            EditorSceneManager.OpenScene("Assets/Scenes/SampleScene.unity");
            SessionState.SetBool(Key,true);EditorApplication.isPlaying=true;
        }
        static void Tick()
        {
            if(!SessionState.GetBool(Key,false)||!EditorApplication.isPlaying||frame==Time.frameCount)return;
            frame=Time.frameCount;
            try
            {
                if(deadline==0)deadline=EditorApplication.timeSinceStartup+150;
                Require(EditorApplication.timeSinceStartup<deadline,"Timeout at stage "+stage);
                Require(runtimeError==null,"Runtime error: "+runtimeError);
                var game=UnityEngine.Object.FindFirstObjectByType<HarareAfterHoursBootstrap>();
                if(game==null||game.Player==null||Time.time<3)return;
                var player=game.Player;var car=game.FeaturedVehicle;
                switch(stage)
                {
                    case 0:
                        var cars=UnityEngine.Object.FindObjectsByType<VehicleController>(FindObjectsSortMode.None);
                        Require(cars.Count(c=>c.GetComponent<PolicePatrol>()==null)==4,"Expected delivery car plus three ambient cars, excluding added patrols");
                        var mesh=car.GetComponentsInChildren<MeshFilter>().First(m=>m.name.StartsWith("Body_LOD0")).sharedMesh;
                        foreach(var c in cars)
                        {
                            Require(c.GetComponentsInChildren<MeshFilter>().First(m=>m.name.StartsWith("Body_LOD0")).sharedMesh==mesh,"Cars must share base mesh");
                            Require(c.Wheels.Length==4,"Four suspension wheels on each car");
                            Require(c.GetComponent<LODGroup>().lodCount==3,"Three LODs on each car");
                        }
                        Debug.Log("[VehicleCheck] ground="+car.Wheels.Count(w=>w.isGrounded)+" y="+car.transform.position.y);
                        Require(car.Wheels.Count(w=>w.isGrounded)>=3,"Suspension tyres must contact ground");
                        Require(Vector3.Dot(car.transform.up,Vector3.up)>.95f,"Stable parked car");
                        Place(player,car.transform.position-car.transform.right*1.8f+Vector3.up*.1f);
                        MobileControlState.RequestInteract();Report.Add("PASS: four shared cars, material-separated mesh, LODs and grounded suspension");Next();break;
                    case 1:
                        if(Elapsed<.35f)break;
                        Require(player.IsDriving,"USE boards driving seat");
                        var animator=player.GetComponentInChildren<Animator>();
                        Require(animator.GetBool("Seated")&&animator.GetBool("DrivingSeat"),"Driving animation active");
                        Require(player.GetComponentsInChildren<SkinnedMeshRenderer>().All(r=>!r.forceRenderingOff),"Driver stays visible");
                        Require(Vector3.Distance(animator.GetBoneTransform(HumanBodyBones.Hips).position,car.DriverSeat.position)<.12f,"Driver pelvis aligned to seat");
                        Require(Vector3.Dot(animator.GetBoneTransform(HumanBodyBones.Head).forward,car.transform.forward)>.5f,"Driver faces forward");
                        Require(animator.GetBoneTransform(HumanBodyBones.Head).position.y-car.transform.position.y<1.7f,"Head fits inside roof");
                        Report.Add("PASS: visible forward-facing seated driver with roof clearance");origin=car.transform.position;Next();break;
                    case 2:
                        MobileControlState.SetMove(Vector2.up);
                        if(Elapsed<1.3f)break;
                        Require(Vector3.Distance(car.transform.position,origin)>.7f&&car.SpeedKph>3,"Throttle accelerates car");
                        Require(car.Wheels.Any(w=>Mathf.Abs(w.rpm)>5),"Wheels roll under torque");
                        Report.Add("PASS: acceleration and physical wheel rotation, "+car.SpeedKph.ToString("F1")+"km/h");rotation=car.transform.rotation;Next();break;
                    case 3:
                        MobileControlState.SetMove(new Vector2(.7f,.7f));
                        if(Elapsed<.7f)break;
                        Require(Mathf.Abs(car.Wheels[0].steerAngle)>5&&Mathf.Abs(car.Wheels[2].steerAngle)<.1f,"Only front wheels steer");
                        Require(Quaternion.Angle(rotation,car.transform.rotation)>2,"Steering changes heading");
                        Require(Vector3.Dot(car.transform.up,Vector3.up)>.85f,"Turning stability");
                        Require(!player.TryExitVehicle(),"No unsafe moving exit");
                        Report.Add("PASS: speed-sensitive front steering, stability and moving-exit rejection");Next();break;
                    case 4:
                        MobileControlState.SetMove(Vector2.down);
                        if(Elapsed<2.2f)break;
                        Require(Vector3.Dot(car.GetComponent<Rigidbody>().linearVelocity,car.transform.forward)<-.15f,"Brake-before-reverse transitions to reverse");
                        Report.Add("PASS: reverse input brakes before reversing");MobileControlState.SetMove(Vector2.zero);Next();break;
                    case 5:
                        MobileControlState.SetBrake();
                        if(car.SpeedKph>1){Require(Elapsed<4,"Braking timeout");break;}
                        Require(BlockedExitRejected(player,car),"Blocked exits rejected");
                        MobileControlState.RequestInteract();Next();break;
                    case 6:
                        if(Elapsed<.25f)break;
                        Require(!player.IsInVehicle&&player.GetComponent<CharacterController>().enabled,"Stopped exit restores walking");
                        Require(!player.transform.IsChildOf(car.transform),"Player unparented from vehicle");
                        Require(!player.GetComponentInChildren<Animator>().GetBool("Seated"),"On-foot animation restored");
                        Report.Add("PASS: stopped safe exit, obstruction checks and walking restoration");
                        Place(player,car.transform.position+car.transform.right*1.8f+Vector3.up*.1f);
                        MobileControlState.RequestRide();Next();break;
                    case 7:
                        if(Elapsed<.25f)break;
                        Require(player.IsPassenger&&!player.IsDriving,"RIDE selects passenger seat");
                        Require(player.GetComponentInChildren<Animator>().GetBool("Seated")&&!player.GetComponentInChildren<Animator>().GetBool("DrivingSeat"),"Passenger animation active");
                        origin=car.transform.position;Next();break;
                    case 8:
                        if(Elapsed<1.5f)break;
                        Require(Vector3.Distance(origin,car.transform.position)>.4f,"Passenger autopilot actually moves");
                        Require(car.transform.Find("Driver seat/Autopilot driver")!=null,"Passenger has visible AI driver");
                        Report.Add("PASS: visible passenger and AI driver ride together");MobileControlState.RequestInteract();Next();break;
                    case 9:
                        if(player.IsPassenger)
                        {
                            Require(Elapsed<7,"Passenger stop/exit timeout");
                            if(car.SpeedKph<2.5f)MobileControlState.RequestInteract();
                            break;
                        }
                        Report.Add("PASS: passenger requests stop and exits safely");
                        ambient=UnityEngine.Object.FindObjectsByType<TrafficVehicle>(FindObjectsSortMode.None).First(t=>t.IsAmbient).GetComponent<VehicleController>();
                        ambient.GetComponent<TrafficVehicle>().RequestStop();Next();break;
                    case 10:
                        if(ambient.SpeedKph>1){Require(Elapsed<6,"Ambient stop timeout");break;}
                        Place(player,ambient.transform.position+ambient.transform.right*1.8f+Vector3.up*.1f);
                        MobileControlState.RequestInteract();Next();break;
                    case 11:
                        if(Elapsed<.3f)break;
                        Require(player.IsDriving&&player.CurrentVehicle==ambient,"Ambient car supports driver takeover");
                        Report.Add("PASS: ambient car drive takeover");
                        Require(player.TryExitVehicle(),"Leave stopped ambient car");
                        Place(player,car.transform.position+car.transform.right*1.8f+Vector3.up*.1f);
                        Require(player.TryBoardNearest(false)&&player.CurrentVehicle==car,"Return to mission car before delivery");
                        var rb=car.GetComponent<Rigidbody>();rb.position=new Vector3(42,.25f,-27);rb.linearVelocity=Vector3.zero;Physics.SyncTransforms();Next();break;
                    case 12:
                        if(Elapsed<.6f)break;
                        Require(game.Mission.State==MissionState.Complete,"Existing delivery trigger still completes mission");
                        Require(UnityEngine.Object.FindObjectsByType<NpcWanderer>(FindObjectsSortMode.None).Length==23,"Pedestrian count preserved");
                        Report.Add("PASS: delivery mission, 23 pedestrians, and no runtime errors");Finish(0);break;
                }
            }
            catch(Exception error){Report.Add("FAIL stage "+stage+": "+error.Message);Debug.LogException(error);Finish(1);}
        }
        static bool BlockedExitRejected(ThirdPersonController player,VehicleController car)
        {
            var blockers=new List<GameObject>();
            foreach(var p in new[]{new Vector3(1.75f,0,.3f),new Vector3(-1.75f,0,.3f),new Vector3(0,0,-3.35f)})
            {var obj=GameObject.CreatePrimitive(PrimitiveType.Cube);obj.transform.position=car.transform.TransformPoint(p)+Vector3.up;obj.transform.localScale=new Vector3(1,2.5f,1);blockers.Add(obj);}
            Physics.SyncTransforms();bool rejected=!car.TryGetExitPosition(out _);
            foreach(var obj in blockers)UnityEngine.Object.DestroyImmediate(obj);
            Physics.SyncTransforms();return rejected;
        }
        static void Place(ThirdPersonController p,Vector3 position){var c=p.GetComponent<CharacterController>();c.enabled=false;p.transform.position=position;c.enabled=true;Physics.SyncTransforms();}
        static float Elapsed=>Time.time-started;
        static void Next(){stage++;started=Time.time;}
        static void Require(bool condition,string message){if(!condition)throw new InvalidOperationException(message);}
        static void Finish(int code)
        {
            foreach(var pair in new[]{(Key+"Stage",MissionDirector.SaveKey),(Key+"Clean",MissionDirector.SaveKey+".clean")})
            {int value=SessionState.GetInt(pair.Item1,-1);if(value<0)PlayerPrefs.DeleteKey(pair.Item2);else PlayerPrefs.SetInt(pair.Item2,value);}
            string points=SessionState.GetString(Key+"Points","");if(points=="")PlayerPrefs.DeleteKey(PlayerProgression.SaveKey);else PlayerPrefs.SetString(PlayerProgression.SaveKey,points);PlayerPrefs.Save();
            File.WriteAllLines("/private/tmp/harare-vehicle-repair/gameplay-validation.txt",Report);Debug.Log("[SharedVehicleValidation] "+string.Join("\n",Report));SessionState.EraseBool(Key);EditorApplication.isPlaying=false;EditorApplication.Exit(code);
        }
    }
}
#endif
