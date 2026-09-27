#if UNITY_EDITOR
using System;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
namespace HarareAfterHours.EditorTools
{
    [InitializeOnLoad]
    public static class PracticeTargetValidation
    {
        const string Key="Harare.PracticeTargetValidation", Folder="/private/tmp/harare-practice-targets/";
        static int stage; static float next; static double deadline;
        static CombatTarget target; static int points; static Vector3 position;
        static PracticeTargetValidation(){EditorApplication.update+=Tick;}
        public static void Run()
        {
            Directory.CreateDirectory(Folder);
            SessionState.SetString(Key+"score",PlayerPrefs.GetString(PlayerProgression.SaveKey,""));
            EditorSceneManager.OpenScene("Assets/Scenes/SampleScene.unity");
            SessionState.SetBool(Key,true);EditorApplication.isPlaying=true;
        }
        static void Check(bool ok,string message){if(!ok)throw new Exception(message);}
        static void Tick()
        {
            if(!SessionState.GetBool(Key,false)||!EditorApplication.isPlaying)return;
            try
            {
                if(deadline==0)deadline=EditorApplication.timeSinceStartup+90;
                Check(EditorApplication.timeSinceStartup<deadline,"Timeout");
                if(Time.time<3||Time.time<next)return;
                var game=HarareAfterHoursBootstrap.Instance;
                if(stage==0)
                {
                    var targets=UnityEngine.Object.FindObjectsByType<CombatTarget>(FindObjectsSortMode.None);
                    Check(targets.Length==6,"Six gameplay targets retained");
                    foreach(var item in targets)
                    {
                        Check(item.transform.Find("Padded practice bag")!=null,"Practice mesh present");
                        Check(item.GetComponentsInChildren<Light>().Length==0,"No cyan point lights");
                        foreach(var r in item.GetComponentsInChildren<Renderer>())
                        {
                            Check(r.sharedMaterial!=null&&r.sharedMaterial.shader.isSupported,"Supported material");
                            Check(!r.sharedMaterial.IsKeywordEnabled("_EMISSION"),"No glowing placeholder materials");
                            var block=new MaterialPropertyBlock();r.GetPropertyBlock(block);Check(block.isEmpty,"No forced cyan override");
                        }
                    }
                    target=targets[0];position=target.transform.position;points=game.Progression.Points;
                    Check(target.ReceiveHit(CombatAction.Pulse)&&target.RemainingIntegrity==2,"Hit damage retained");
                    next=Time.time+.3f;stage++;return;
                }
                if(stage==1)
                {
                    Check(Vector3.Distance(position,target.transform.position)<.001f,"Target grounded, no bobbing");
                    foreach(var r in target.GetComponentsInChildren<Renderer>())
                    {var block=new MaterialPropertyBlock();r.GetPropertyBlock(block);Check(block.isEmpty,"Original material restored after hit");}
                    target.ReceiveHit(CombatAction.Fight);
                    Check(!target.GetComponent<Collider>().enabled,"Completed target collision disabled");
                    Check(game.Progression.Points>points,"Completion awards points");
                    Check(!target.ReceiveHit(CombatAction.Pulse),"No duplicate completion");
                    next=Time.time+5.3f;stage++;return;
                }
                Check(target.RemainingIntegrity==3&&target.GetComponent<Collider>().enabled,"Respawn restores target");
                foreach(var r in target.GetComponentsInChildren<Renderer>())
                {var block=new MaterialPropertyBlock();r.GetPropertyBlock(block);Check(r.enabled&&block.isEmpty,"Respawn preserves original appearance");}
                var camera=Camera.main;var rt=new RenderTexture(1170,2532,24);camera.targetTexture=rt;camera.Render();RenderTexture.active=rt;
                var image=new Texture2D(1170,2532,TextureFormat.RGB24,false);image.ReadPixels(new Rect(0,0,1170,2532),0,0);image.Apply();File.WriteAllBytes(Folder+"after.png",image.EncodeToPNG());
                camera.targetTexture=null;RenderTexture.active=null;UnityEngine.Object.DestroyImmediate(image);rt.Release();UnityEngine.Object.DestroyImmediate(rt);
                Finish(0,"PASS: six non-emissive practice targets, supported materials, grounded meshes, hit flash restores original materials, damage, scoring, duplicate-hit protection and respawn.");
            }
            catch(Exception e){Finish(1,e.ToString());}
        }
        static void Finish(int code,string report)
        {
            var saved=SessionState.GetString(Key+"score","");if(saved=="")PlayerPrefs.DeleteKey(PlayerProgression.SaveKey);else PlayerPrefs.SetString(PlayerProgression.SaveKey,saved);PlayerPrefs.Save();
            File.WriteAllText(Folder+"validation.txt",report);SessionState.EraseBool(Key);EditorApplication.isPlaying=false;EditorApplication.Exit(code);
        }
    }
}
#endif
