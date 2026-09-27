#if UNITY_EDITOR
using System;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
namespace HarareAfterHours.EditorTools
{
    [InitializeOnLoad]
    public static class CameraVenueValidation
    {
        const string Key="Harare.CameraVenueValidation",Folder="/private/tmp/harare-camera-venues/";
        static double deadline;
        static CameraVenueValidation(){EditorApplication.update+=Tick;}
        public static void Run()
        {
            SessionState.SetString(Key+"score",PlayerPrefs.GetString(PlayerProgression.SaveKey,""));
            PlayerPrefs.DeleteKey(PlayerProgression.SaveKey);
            EditorSceneManager.OpenScene("Assets/Scenes/SampleScene.unity");
            SessionState.SetBool(Key,true);EditorApplication.isPlaying=true;
        }
        static void Check(bool ok,string message){if(!ok)throw new Exception(message);}
        static void Tick()
        {
            if(!SessionState.GetBool(Key,false)||!EditorApplication.isPlaying)return;
            if(deadline==0)deadline=EditorApplication.timeSinceStartup+100;
            if(Time.time<4&&EditorApplication.timeSinceStartup<deadline)return;
            int code=0;string report;
            try
            {
                var game=HarareAfterHoursBootstrap.Instance;Check(game!=null&&game.Player!=null,"World loaded");
                var player=game.Player;var cc=player.GetComponent<CharacterController>();var state=game.Mission.State;
                var camera=Camera.main;var rig=camera.GetComponent<ThirdPersonCameraRig>();
                var venues=UnityEngine.Object.FindObjectsByType<CityVenue>(FindObjectsSortMode.None);
                Check(venues.Length==4,"Four storefronts");
                int points=game.Progression.Points;
                foreach(var venue in venues)
                {
                    cc.enabled=false;player.transform.position=venue.Approach;cc.enabled=true;Physics.SyncTransforms();
                    Check(CityVenue.Nearest(player)==venue,"Approachable "+venue.DisplayName);
                    foreach(var hit in Physics.OverlapCapsule(venue.Approach+Vector3.up*.35f,venue.Approach+Vector3.up*1.4f,.3f,~0,QueryTriggerInteraction.Ignore))
                        Check(hit.transform.IsChildOf(player.transform)||hit.transform==player.transform,"Clear approach: "+venue.DisplayName+" / "+hit.name);
                    Check(CityVenue.TryInteract(player),"Check in");
                    CityVenue.TryInteract(player);
                    camera.transform.position=venue.transform.TransformPoint(new Vector3(0,2.1f,8));camera.transform.LookAt(venue.transform.position+Vector3.up*2.1f);
                    Capture(camera,venue.VenueId);
                }
                Check(game.Progression.Points==points+200,"Exactly one reward per venue");
                Check(game.Mission.State==state,"Main mission preserved");
                cc.enabled=false;player.transform.SetPositionAndRotation(new Vector3(120,.2f,120),Quaternion.identity);cc.enabled=true;
                rig.SetTarget(player.transform,false);rig.SendMessage("LateUpdate");
                Check(Vector3.Distance(camera.transform.position,player.transform.position)>4,"Walking distance");
                foreach(var input in new[]{Vector2.left,Vector2.right,Vector2.down,Vector2.up})
                {
                    cc.enabled=false;player.transform.SetPositionAndRotation(new Vector3(120,.2f,120),Quaternion.identity);cc.enabled=true;
                    rig.SetTarget(player.transform,false);MobileControlState.SetMove(input);
                    Vector3 origin=player.transform.position;
                    for(int step=0;step<240;step++){player.SendMessage("Update");rig.SendMessage("LateUpdate");}
                    Vector3 expected=new Vector3(input.x,0,input.y);
                    Vector3 displacement=player.transform.position-origin;displacement.y=0;
                    Check(Vector3.Dot(displacement.normalized,expected)>.99f,"Held movement stays straight: "+input);
                    Check(Vector3.Dot(rig.FlatForward,expected)>.95f,"Automatic camera follows turn: "+input);
                    MobileControlState.SetMove(Vector2.zero);player.SendMessage("Update");
                }
                rig.ApplyTouchLook(new Vector2(70,0));
                var yaw=typeof(ThirdPersonCameraRig).GetField("_yaw",System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.Instance);
                float manual=(float)yaw.GetValue(rig);
                MobileControlState.SetMove(Vector2.up);player.SendMessage("Update");rig.SendMessage("LateUpdate");
                Check(Mathf.Abs(Mathf.DeltaAngle(manual,(float)yaw.GetValue(rig)))<.01f,"Manual camera grace period");
                MobileControlState.SetMove(Vector2.zero);player.SendMessage("Update");
                cc.enabled=false;player.transform.SetPositionAndRotation(new Vector3(120,.2f,120),Quaternion.identity);cc.enabled=true;
                var wall=GameObject.CreatePrimitive(PrimitiveType.Cube);wall.transform.position=new Vector3(120,2,117);wall.transform.localScale=new Vector3(10,6,.4f);Physics.SyncTransforms();
                rig.SetTarget(player.transform,false);rig.SendMessage("LateUpdate");
                Check(camera.transform.position.z>117.3f,"Camera stays in front of wall");
                UnityEngine.Object.DestroyImmediate(wall);
                cc.enabled=false;player.transform.position=new Vector3(140,.2f,140);cc.enabled=true;
                var car=game.FeaturedVehicle;car.transform.SetPositionAndRotation(new Vector3(120,.2f,120),Quaternion.Euler(0,90,0));Physics.SyncTransforms();
                rig.SetTarget(car.CameraTarget,true);rig.SendMessage("LateUpdate");
                Check(Vector3.Dot(rig.FlatForward,car.transform.forward)>.95f,"Car heading on entry");
                Check(Vector3.Distance(camera.transform.position,car.CameraTarget.position)>6,"Own vehicle excluded from camera collision");
                cc.enabled=false;player.transform.SetPositionAndRotation(new Vector3(4.5f,.2f,-8.5f),Quaternion.identity);cc.enabled=true;
                rig.SetTarget(player.transform,false);rig.SendMessage("LateUpdate");Capture(camera,"walking-camera");
                report="PASS: four reachable storefronts, clear approaches, persistent one-time discovery rewards, main mission unchanged.\nPASS: automatic left/right/back/forward camera alignment, held movement stays straight, manual-look grace period.\nPASS: walking framing, obstacle collision, vehicle-heading entry and own-car exclusion.\nCaptured four storefronts and walking camera. Deterministic Update/LateUpdate fixtures; device performance and sustained driving require separate validation.";
            }
            catch(Exception e){code=1;report=e.ToString();Debug.LogException(e);}
            string saved=SessionState.GetString(Key+"score","");if(saved=="")PlayerPrefs.DeleteKey(PlayerProgression.SaveKey);else PlayerPrefs.SetString(PlayerProgression.SaveKey,saved);PlayerPrefs.Save();
            File.WriteAllText(Folder+"validation.txt",report);SessionState.EraseBool(Key);EditorApplication.isPlaying=false;EditorApplication.Exit(code);
        }
        static void Capture(Camera camera,string name)
        {
            var rt=new RenderTexture(1280,800,24);camera.targetTexture=rt;camera.Render();camera.Render();RenderTexture.active=rt;
            var image=new Texture2D(1280,800,TextureFormat.RGB24,false);image.ReadPixels(new Rect(0,0,1280,800),0,0);image.Apply();File.WriteAllBytes(Folder+name+".png",image.EncodeToPNG());
            camera.targetTexture=null;RenderTexture.active=null;UnityEngine.Object.DestroyImmediate(image);rt.Release();UnityEngine.Object.DestroyImmediate(rt);
        }
    }
}
#endif
