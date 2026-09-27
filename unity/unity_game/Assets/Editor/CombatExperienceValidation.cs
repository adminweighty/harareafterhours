#if UNITY_EDITOR
using System;
using System.IO;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
namespace HarareAfterHours.EditorTools
{
    [InitializeOnLoad]
    public static class CombatExperienceValidation
    {
        const string Key="Harare.CombatExperienceTest";
        static double deadline;
        static CombatExperienceValidation(){EditorApplication.update+=Tick;}
        public static void Run(){SessionState.SetString(Key+"save",PlayerPrefs.GetString(PlayerProgression.SaveKey,""));PlayerPrefs.DeleteKey(PlayerProgression.SaveKey);EditorSceneManager.OpenScene("Assets/Scenes/SampleScene.unity");SessionState.SetBool(Key,true);EditorApplication.isPlaying=true;}
        static void Check(bool ok,string message){if(!ok)throw new Exception(message);}
        static void Place(Component item,Vector3 p){var cc=item.GetComponent<CharacterController>();cc.enabled=false;item.transform.position=p;cc.enabled=true;Physics.SyncTransforms();}
        static void Arm(StreetActor actor){typeof(StreetActor).GetField("_attackAt",BindingFlags.Instance|BindingFlags.NonPublic).SetValue(actor,Time.time-.01f);}
        static void TickActor(StreetActor actor)=>typeof(StreetActor).GetMethod("Update",BindingFlags.Instance|BindingFlags.NonPublic).Invoke(actor,null);
        static void ExpireProtection(StreetActionDirector world)=>typeof(StreetActionDirector).GetField("_lastDamage",BindingFlags.Instance|BindingFlags.NonPublic).SetValue(world,Time.time-2);
        static void Tick()
        {
            if(!SessionState.GetBool(Key,false)||!EditorApplication.isPlaying)return;
            if(deadline==0)deadline=EditorApplication.timeSinceStartup+90;
            if(Time.time<3&&EditorApplication.timeSinceStartup<deadline)return;
            int code=0;string report;
            try
            {
                var world=StreetActionDirector.Instance;var game=HarareAfterHoursBootstrap.Instance;
                Check(world!=null,"World available");foreach(var a in world.Actors)a.enabled=false;
                var actor=world.Actors.Find(a=>!a.Officer);var mission=game.Mission.State;int points=game.Progression.Points;
                actor.Hit();actor.Hit();actor.Hit();Check(actor.Down&&game.Progression.Points==points+50,"Marked hostile +50");
                actor.ResetEncounter();actor.Hit();actor.Hit();actor.Hit();Check(game.Progression.Points==points+50,"Restart cannot farm rewards");actor.ResetEncounter();
                var officer=world.Actors.Find(a=>a.Officer);officer.Hit();officer.Hit();officer.Hit();Check(game.Progression.Points==points+50,"No officer combat reward");
                Place(game.Player,new Vector3(250,.2f,250));Place(actor,new Vector3(250,.2f,251.4f));
                typeof(StreetActionDirector).GetProperty("GraceUntil").SetValue(world,Time.time-1);
                typeof(StreetActor).GetField("_nextAttack",BindingFlags.Instance|BindingFlags.NonPublic).SetValue(actor,0f);
                TickActor(actor);Check(actor.WindingUp&&!world.GameOver,"Attack is telegraphed before damage");
                Place(game.Player,new Vector3(250,.2f,246));Arm(actor);TickActor(actor);Check(world.Health==100&&!world.GameOver,"Moving away evades hit");
                Place(game.Player,new Vector3(250,.2f,250));Place(actor,new Vector3(250,.2f,251.4f));actor.transform.rotation=Quaternion.Euler(0,180,0);
                var wall=GameObject.CreatePrimitive(PrimitiveType.Cube);wall.transform.position=new Vector3(250,1,250.7f);wall.transform.localScale=new Vector3(3,3,.2f);Physics.SyncTransforms();
                Arm(actor);TickActor(actor);Check(world.Health==100&&!world.GameOver,"Wall blocks hostile hit");UnityEngine.Object.DestroyImmediate(wall);Physics.SyncTransforms();
                Arm(actor);TickActor(actor);Check(!world.GameOver&&world.Health==80&&!game.Player.InputLocked&&Time.timeScale==1,"First hit leaves player able to counterattack");
                world.Damage(20);Check(world.Health==80&&world.RecoveringFromHit,"Repeated or simultaneous hits blocked by protection");
                for(int i=0;i<4;i++){ExpireProtection(world);world.Damage(20);}
                Check(world.GameOver&&world.Health==0&&game.Player.InputLocked&&Time.timeScale==0,"Five separated ordinary hits deplete life");
                game.Bridge.OnFlutterMessage("{\"type\":\"resume\"}");Check(Time.timeScale==0,"Resume cannot bypass defeat");
                game.Bridge.OnFlutterMessage("{\"type\":\"restart_encounter\"}");Check(!world.GameOver&&Time.timeScale==1&&world.Health==100&&!game.Player.InputLocked,"Restart restores play");
                Check(game.Mission.State==mission&&game.Progression.Points==points+50,"Mission and earned points retained");
                world.Damage(100);Check(!world.GameOver,"Restart grace prevents instant repeat death");
                report="PASS: telegraphed attack; movement evasion; wall obstruction; first hit leaves 80 life and controls active; duplicate hit protection; five separated hits cause Game Over; resume blocked; explicit restart restores 100 life; restart grace; +50 hostile-only reward; no repeat farming; mission and points retained.";
            }
            catch(Exception e){code=1;report=e.ToString();}
            Time.timeScale=1;var saved=SessionState.GetString(Key+"save","");if(saved=="")PlayerPrefs.DeleteKey(PlayerProgression.SaveKey);else PlayerPrefs.SetString(PlayerProgression.SaveKey,saved);PlayerPrefs.Save();
            Directory.CreateDirectory("/private/tmp/harare-combat-experience");File.WriteAllText("/private/tmp/harare-combat-experience/validation.txt",report);SessionState.EraseBool(Key);EditorApplication.isPlaying=false;EditorApplication.Exit(code);
        }
    }
}
#endif
