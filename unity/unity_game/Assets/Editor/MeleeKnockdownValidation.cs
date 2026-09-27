#if UNITY_EDITOR
using System;
using System.IO;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Object=UnityEngine.Object;
namespace HarareAfterHours.EditorTools
{
    [InitializeOnLoad]
    public static class MeleeKnockdownValidation
    {
        const string Key="Harare.MeleeKnockdownQA";
        static readonly string[] Saves={PlayerLoadout.SaveKey,PlayerProgression.SaveKey};
        static double deadline;static float next;static int stage;
        static StreetActor enemy,officer;static GameObject floor,wall;
        static MeleeKnockdownValidation(){EditorApplication.update+=Tick;}
        public static void Run()
        {
            foreach(var key in Saves){SessionState.SetBool(Key+key+"exists",PlayerPrefs.HasKey(key));SessionState.SetString(Key+key,PlayerPrefs.GetString(key,""));}
            PlayerPrefs.DeleteKey(PlayerProgression.SaveKey);
            EditorSceneManager.OpenScene("Assets/Scenes/SampleScene.unity");SessionState.SetBool(Key,true);EditorApplication.isPlaying=true;
        }
        static void Check(bool ok,string label){if(!ok)throw new Exception(label);}
        static void Place(Component item,Vector3 point){var cc=item.GetComponent<CharacterController>();cc.enabled=false;item.transform.SetPositionAndRotation(point,Quaternion.identity);cc.enabled=true;Physics.SyncTransforms();}
        static void Punch(HarareAfterHoursBootstrap game){typeof(PlayerCombat).GetField("_nextFightAt",BindingFlags.Instance|BindingFlags.NonPublic).SetValue(game.Combat,0f);game.Combat.TryAttack();}
        static void Tick()
        {
            if(!SessionState.GetBool(Key,false)||!EditorApplication.isPlaying)return;
            try
            {
                if(deadline==0)deadline=EditorApplication.timeSinceStartup+90;
                Check(EditorApplication.timeSinceStartup<deadline,"Melee test timeout");
                var game=HarareAfterHoursBootstrap.Instance;
                if(game==null||Time.time<3||Time.time<next)return;
                switch(stage)
                {
                    case 0:
                        foreach(var actor in StreetActionDirector.Instance.Actors)actor.enabled=false;
                        enemy=StreetActionDirector.Instance.Actors.Find(a=>!a.Officer);
                        officer=StreetActionDirector.Instance.Actors.Find(a=>a.Officer&&a.gameObject.activeInHierarchy);
                        floor=GameObject.CreatePrimitive(PrimitiveType.Cube);floor.name="Melee QA floor";floor.transform.position=new Vector3(1000,-.1f,1000);floor.transform.localScale=new Vector3(20,.2f,20);
                        Place(game.Player,new Vector3(1000,.02f,1000));Place(enemy,new Vector3(1000,.02f,1001.4f));
                        game.Player.GetComponent<PlayerLoadout>().ApplyJson("{\"weapon\":\"Unarmed\"}",false);
                        Punch(game);Check(enemy.HitsRemaining==3,"Damage waits for punch contact");next=Time.time+.22f;break;
                    case 1:
                        Check(enemy.HitsRemaining==2&&enemy.Stunned,"Punch contact damages and interrupts enemy");
                        wall=GameObject.CreatePrimitive(PrimitiveType.Cube);wall.transform.position=new Vector3(1000,1,1000.7f);wall.transform.localScale=new Vector3(3,3,.15f);Physics.SyncTransforms();
                        Punch(game);next=Time.time+.22f;break;
                    case 2:
                        Check(enemy.HitsRemaining==2,"Wall blocks melee contact");wall.SetActive(false);Object.Destroy(wall);Physics.SyncTransforms();
                        Punch(game);next=Time.time+.22f;break;
                    case 3:
                        Check(enemy.HitsRemaining==1,"Second punch lands");Punch(game);next=Time.time+.22f;break;
                    case 4:
                        Check(enemy.Down&&enemy.GetComponent<EnemyKnockdown>().Active&&!enemy.GetComponent<EnemyKnockdown>().Settled,"Final punch starts a visible fall, not an instant snap");
                        Check(!enemy.GetComponent<CharacterController>().enabled,"Standing capsule disabled while down");
                        Check(game.Progression.Points==50,"Defeat awards exactly 50 points");next=Time.time+.85f;break;
                    case 5:
                        Check(enemy.GetComponent<EnemyKnockdown>().Settled,"Enemy finishes fall and remains down");
                        enemy.Hit();Check(game.Progression.Points==50,"Downed enemy cannot farm points");
                        Capture(enemy.transform);
                        enemy.ResetEncounter();Check(!enemy.Down&&enemy.GetComponent<CharacterController>().enabled&&enemy.GetComponentInChildren<Animator>().enabled,"Restart restores rig and collision");
                        Place(officer,new Vector3(1004,.02f,1000));officer.Hit();officer.Hit();officer.Hit();
                        Check(officer.Down,"Officer also visibly falls");
                        typeof(StreetActor).GetField("_downUntil",BindingFlags.Instance|BindingFlags.NonPublic).SetValue(officer,Time.time-.01f);
                        typeof(StreetActor).GetMethod("Update",BindingFlags.Instance|BindingFlags.NonPublic).Invoke(officer,null);Check(officer.GetComponent<EnemyKnockdown>().Recovering,"Officer recovery starts");next=Time.time+.7f;break;
                    case 6:
                        typeof(StreetActor).GetMethod("Update",BindingFlags.Instance|BindingFlags.NonPublic).Invoke(officer,null);Check(!officer.Down&&officer.GetComponent<CharacterController>().enabled,"Officer recovers with controller restored");
                        enemy.ResetEncounter();Place(enemy,new Vector3(1000,.02f,1002.45f));
                        wall=GameObject.CreatePrimitive(PrimitiveType.Cube);wall.transform.position=new Vector3(1000,1,1001);wall.transform.localScale=new Vector3(3,3,.15f);Physics.SyncTransforms();
                        game.Combat.TryKick();Check(enemy.HitsRemaining==3&&game.Player.GetComponentInChildren<StreetActionPose>().Kicking,"Kick animates before contact");next=Time.time+.3f;break;
                    case 7:
                        Check(enemy.HitsRemaining==3,"Wall blocks kick");wall.SetActive(false);Object.Destroy(wall);Physics.SyncTransforms();
                        typeof(PlayerCombat).GetField("_nextFightAt",BindingFlags.Instance|BindingFlags.NonPublic).SetValue(game.Combat,0f);
                        game.Combat.TryKick();game.Combat.TryKick();next=Time.time+.3f;break;
                    case 8:
                        Check(enemy.HitsRemaining==1&&enemy.Stunned,"Kick has longer reach, two damage and shared anti-spam cooldown");
                        Place(enemy,new Vector3(1000,.02f,1001.4f));Punch(game);next=Time.time+.3f;break;
                    case 9:
                        Check(enemy.Down&&game.Progression.Points==50,"Kick plus punch defeats; previous reward cannot be farmed");
                        MobileControlState.RequestKick();MobileControlState.Reset();Check(!MobileControlState.ConsumeKick(),"Pause clears buffered kick");
                        var touch=TouchGameplayControls.Instance;
                        Check(!touch.ButtonRect(TouchGameplayControls.Action.Kick).Overlaps(touch.ButtonRect(TouchGameplayControls.Action.Fight)),"Punch and kick touch targets do not overlap");
                        Check(touch.HitTest(touch.ButtonRect(TouchGameplayControls.Action.Kick).center)==TouchGameplayControls.Action.Kick,"Kick button hit test");
                        enemy.ResetEncounter();Place(enemy,new Vector3(1000,.02f,1001.4f));
                        typeof(PlayerCombat).GetField("_nextFightAt",BindingFlags.Instance|BindingFlags.NonPublic).SetValue(game.Combat,0f);
                        game.Combat.TryKick();game.Combat.CancelPendingMelee();next=Time.time+.3f;break;
                    case 10:
                        Check(enemy.HitsRemaining==3,"Defeat cancellation prevents delayed attack carrying into restart");
                        Finish(0,"PASS: punch contact delay, flinch, wall occlusion, three-punch fall, ground pose, +50 once, reset, officer recovery; kick animation/contact delay, wall occlusion, longer reach, two damage, shared cooldown, kick+punch defeat, kick buffer reset, separate touch hit target and pending-attack cancellation.");return;
                }
                stage++;
            }
            catch(Exception ex){Finish(1,ex.ToString());}
        }
        static void Capture(Transform actor)
        {
            var camera=new GameObject("Knockdown QA camera").AddComponent<Camera>();camera.CopyFrom(Camera.main);camera.enabled=false;
            camera.transform.position=actor.position+new Vector3(3,2.5f,-2);camera.transform.LookAt(actor.position+Vector3.forward*.7f);camera.fieldOfView=45;
            var target=new RenderTexture(800,600,24);camera.targetTexture=target;camera.Render();var previous=RenderTexture.active;RenderTexture.active=target;
            var image=new Texture2D(800,600,TextureFormat.RGB24,false);image.ReadPixels(new Rect(0,0,800,600),0,0);image.Apply();File.WriteAllBytes("/private/tmp/harare-knockdown-preview.png",image.EncodeToPNG());
            RenderTexture.active=previous;Object.Destroy(image);Object.Destroy(target);Object.Destroy(camera.gameObject);
        }
        static void Finish(int code,string report)
        {
            foreach(var key in Saves){if(SessionState.GetBool(Key+key+"exists",false))PlayerPrefs.SetString(key,SessionState.GetString(Key+key,""));else PlayerPrefs.DeleteKey(key);}
            PlayerPrefs.Save();Time.timeScale=1;File.WriteAllText("/private/tmp/harare-melee-validation.txt",report);Debug.Log(report);
            SessionState.EraseBool(Key);EditorApplication.isPlaying=false;EditorApplication.Exit(code);
        }
    }
}
#endif
