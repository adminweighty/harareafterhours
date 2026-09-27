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
    public static class GroundGameplayValidation
    {
        const string Key="Harare.GroundValidation";
        static readonly List<string> Report=new();
        static readonly List<float> Times=new();
        static int stage,frame;static float started;static double deadline;
        static string error;static Vector3 start;
        static GroundGameplayValidation()
        {
            EditorApplication.update+=Tick;
            Application.logMessageReceived+=(m,s,t)=>{if(SessionState.GetBool(Key,false)&&(t==LogType.Error||t==LogType.Exception||t==LogType.Assert))error=m;};
        }
        public static void Run()
        {
            Directory.CreateDirectory("/private/tmp/harare-ground-repair");
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
                Require(EditorApplication.timeSinceStartup<deadline,"Timeout");Require(error==null,error);
                var game=UnityEngine.Object.FindFirstObjectByType<HarareAfterHoursBootstrap>();
                if(game==null||game.Player==null||Time.time<3)return;
                switch(stage)
                {
                    case 0:
                        var floor=GameObject.Find("City ground").GetComponent<BoxCollider>();
                        Require(floor.bounds.size.x>=400&&floor.bounds.size.z>=400,"400m continuous ground");
                        foreach(float x in new[]{-190f,-90f,90f,190f})foreach(float z in new[]{-190f,-90f,90f,190f})
                            Require(Physics.Raycast(new Vector3(x,2,z),Vector3.down,out var hit,3,~0,QueryTriggerInteraction.Ignore)&&Mathf.Abs(hit.point.y)<.02f,"Ground support at "+x+","+z);
                        foreach(float d in new[]{-7.3f,7.3f})
                        {
                            Require(Physics.Raycast(new Vector3(0,2,d),Vector3.down,out var hit,3)&&hit.collider.name=="Road north-south","No raised sidewalk across road");
                        }
                        Require(Physics.Raycast(new Vector3(GroundEnvironment.BoundaryHalf-2,2,120),Vector3.right,out var boundary,4)&&boundary.collider.name=="Distant ground boundary","Safe far boundary");
                        Report.Add("PASS: 400m collision ground, 16 outer support samples, clear intersections and far boundary");
                        foreach(string name in new[]{"Asphalt","Paving","Soil","Concrete","WallWarm","WallGrey","WallCream","WallStone"})
                        {
                            var material=GroundEnvironment.Surface(name);
                            Require(material!=null&&material.shader.isSupported&&!ShaderUtil.ShaderHasError(material.shader),"Working surface shader: "+name);
                            var texture=material.GetTexture("_Surfaces") as Texture2DArray;
                            Require(texture!=null&&texture.depth==4&&texture.mipmapCount>1,"Four independently mipmapped atlas layers");
                        }
                        Require(!UnityEngine.Object.FindObjectsByType<Transform>(FindObjectsSortMode.None).Any(t=>t.name=="Photographic building facade"),"No street-photo billboards posing as facades");
                        var asphalt=GroundEnvironment.Surface("Asphalt");
                        Require(asphalt.IsKeywordEnabled("_PHOTO_SURFACE"),"Photographic road variant enabled");
                        Require(Mathf.Abs(asphalt.GetFloat("_MetresPerTile")-2.05f)<.001f,"Photographic asphalt physical scale");
                        foreach(string map in new[]{"_PhotoColor","_PhotoNormal","_PhotoRoughness"})
                        {
                            var photo=asphalt.GetTexture(map) as Texture2D;
                            Require(photo!=null&&photo.width==1024&&photo.mipmapCount>1&&photo.wrapMode==TextureWrapMode.Repeat,"Mobile-ready photographic map: "+map);
                        }
                        Report.Add("PASS: photographic asphalt colour/normal/roughness, 1K mipmaps, repeat, 2.05m scale");
                        Require(UnityEngine.Object.FindObjectsByType<VehicleController>(FindObjectsSortMode.None).Count(c=>c.GetComponent<PolicePatrol>()==null)==4,"Original cars retained alongside police");
                        Require(UnityEngine.Object.FindObjectsByType<NpcWanderer>(FindObjectsSortMode.None).Length==23,"Pedestrians retained");
                        Report.Add("PASS: shared textured materials, mipmaps, supported shader, four cars and 23 pedestrians");
                        if(UnityEngine.Object.FindFirstObjectByType<ReferenceCityPresentation>()!=null)
                        {
                            Require(RenderSettings.skybox!=null&&RenderSettings.skybox.shader.isSupported,"Working reference daylight sky");
                            var probe=UnityEngine.Object.FindObjectsByType<UnityEngine.ReflectionProbe>(FindObjectsSortMode.None).FirstOrDefault(p=>p.name=="First Street 128px reflection");
                            Require(probe!=null&&probe.resolution==128,"Bounded street reflection probe");
                            Require(GameObject.Find("Reference street detail pass")!=null,"Reference street props installed");
                            foreach(float z in new[]{9f,12f,18f,22f})
                                Require(!Physics.OverlapCapsule(new Vector3(7.4f,.6f,z),new Vector3(7.4f,1.6f,z),.32f,~0,QueryTriggerInteraction.Ignore).Any(c=>!(c is CharacterController)&&c.GetComponentInParent<VehicleController>()==null),"Clear First Street walking lane "+z);
                            Report.Add("PASS: daylight sky, single 128px street probe, detail prefab and clear walking lane");
                        }
                        Camera.main.GetComponent<ThirdPersonCameraRig>().enabled=false;
                        Camera.main.transform.SetPositionAndRotation(new Vector3(120,2,116),Quaternion.identity);
                        Place(game.Player,new Vector3(120,.3f,120));start=game.Player.transform.position;Next();break;
                    case 1:
                        MobileControlState.SetMove(Vector2.up);
                        if(Time.time-started<1.2f)break;
                        Require(game.Player.transform.position.z-start.z>1.7f,"Walking beyond old ground edge");
                        Require(game.Player.GetComponent<CharacterController>().isGrounded,"Player remains grounded outside old city plane");
                        Report.Add("PASS: normal walking on extended ground beyond original 75m edge");
                        MobileControlState.SetMove(Vector2.zero);MobileControlState.RequestJump();Next();break;
                    case 2:
                        if(Time.time-started<.25f)break;
                        Require(!game.Player.GetComponent<CharacterController>().isGrounded,"Jump leaves extended ground");Next();break;
                    case 3:
                        if(Time.time-started<1.3f)break;
                        Require(game.Player.GetComponent<CharacterController>().isGrounded,"Lands on extended ground");
                        Report.Add("PASS: jump and landing on new ground");
                        Place(game.Player,new Vector3(4.5f,.3f,-8.5f));Next();break;
                    case 4:
                        Times.Add(Time.unscaledDeltaTime*1000);
                        if(Times.Count<180)break;
                        Times.Sort();Report.Add("Editor frame sample median "+Times[90].ToString("F2")+"ms; p95 "+Times[171].ToString("F2")+"ms (not a physical-device GPU benchmark)");
                        Capture(Camera.main,new Vector3(5,1.8f,9),new Vector3(12,3,28),"after-pavement");
                        Capture(Camera.main,new Vector3(4.5f,2.8f,-13),new Vector3(0,.5f,8),"after-start");
                        Capture(Camera.main,new Vector3(100,7,95),new Vector3(25,0,20),"after-ground");
                        Report.Add("PASS: no runtime errors; three review views captured");Finish(0);break;
                }
            }
            catch(Exception e){Report.Add("FAIL: "+e.Message);Debug.LogException(e);Finish(1);}
        }
        static void Place(ThirdPersonController player,Vector3 p){var c=player.GetComponent<CharacterController>();c.enabled=false;player.transform.position=p;c.enabled=true;Physics.SyncTransforms();}
        static void Next(){stage++;started=Time.time;}
        static void Require(bool ok,string message){if(!ok)throw new Exception(message);}
        static void Capture(Camera camera,Vector3 p,Vector3 target,string name)
        {
            camera.transform.position=p;camera.transform.LookAt(target);camera.fieldOfView=65;
            var rt=new RenderTexture(1280,800,24);camera.targetTexture=rt;camera.Render();camera.Render();RenderTexture.active=rt;
            var image=new Texture2D(1280,800,TextureFormat.RGB24,false);image.ReadPixels(new Rect(0,0,1280,800),0,0);image.Apply();
            File.WriteAllBytes("/private/tmp/harare-ground-repair/"+name+".png",image.EncodeToPNG());
            camera.targetTexture=null;RenderTexture.active=null;UnityEngine.Object.DestroyImmediate(image);rt.Release();UnityEngine.Object.DestroyImmediate(rt);
        }
        static void Finish(int code)
        {
            File.WriteAllLines("/private/tmp/harare-ground-repair/validation.txt",Report);
            Debug.Log("[GroundValidation] "+string.Join("\n",Report));SessionState.EraseBool(Key);EditorApplication.isPlaying=false;EditorApplication.Exit(code);
        }
    }
}
#endif
