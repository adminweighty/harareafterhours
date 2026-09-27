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
    public static class BulletFeedbackValidation
    {
        const string Key="Harare.BulletFeedbackQA";
        static double deadline;
        static readonly string[] Saves={PlayerLoadout.SaveKey,PlayerProgression.SaveKey};
        static BulletFeedbackValidation(){EditorApplication.update+=Tick;}
        public static void Run()
        {
            foreach(var key in Saves){SessionState.SetBool(Key+key+"exists",PlayerPrefs.HasKey(key));SessionState.SetString(Key+key,PlayerPrefs.GetString(key,""));}
            EditorSceneManager.OpenScene("Assets/Scenes/SampleScene.unity");
            SessionState.SetBool(Key,true);EditorApplication.isPlaying=true;
        }
        static void Check(bool value,string message){if(!value)throw new Exception(message);}
        static void Tick()
        {
            if(!SessionState.GetBool(Key,false)||!EditorApplication.isPlaying)return;
            if(deadline==0)deadline=EditorApplication.timeSinceStartup+90;
            if(Time.time<3&&EditorApplication.timeSinceStartup<deadline)return;
            int code=0;string report;
            try
            {
                var game=HarareAfterHoursBootstrap.Instance;
                Check(game!=null,"World loads");
                foreach(var actor in StreetActionDirector.Instance.Actors)actor.enabled=false;
                // Keep the fixture clear of live traffic and practice targets.
                var body=game.Player.GetComponent<CharacterController>();body.enabled=false;
                game.Player.transform.SetPositionAndRotation(new Vector3(1000,.2f,1000),Quaternion.identity);
                body.enabled=true;Physics.SyncTransforms();
                BloodCombatValidation.Check(game);
                game.Player.GetComponent<PlayerLoadout>().ApplyJson("{\"weapon\":\"Pulse rifle\"}",false);
                var feedback=game.Player.GetComponent<WeaponFeedback>();
                var muzzle=game.Player.GetComponent<PlayerEquipmentVisual>().Muzzle;
                feedback.Configure(false,false,true);
                feedback.Fire(muzzle,muzzle.position,muzzle.position+muzzle.forward*35,true);
                Check(feedback.ShotColor.r>feedback.ShotColor.b*2,"Rifle tracer is warm, not blue");
                Check(feedback.VisibleTracerLength>0&&feedback.VisibleTracerLength<=.651f,"Tracer is a short bullet streak, never a full beam");
                Check(Vector3.Distance(feedback.TracerOrigin,muzzle.position)<.001f,"Shot anchored to muzzle");
                typeof(WeaponFeedback).GetProperty("LastShotTime").SetValue(feedback,Time.time-.04f);
                feedback.SendMessage("LateUpdate");
                Check(feedback.VisibleTracerLength<=.651f,"Travelling streak stays short");
                feedback.Configure(true,true,true);
                feedback.Fire(muzzle,muzzle.position,muzzle.position+muzzle.forward*35,true);
                Check(feedback.VisibleTracerLength==0&&!feedback.FlashVisible,"Reduced effects hides tracer and flash");
                Check(!game.Player.GetComponent<AudioSource>().isPlaying,"Mute remains respected");
                report="PASS: actual ranged character hit; wall obstruction; blood burst and pool cap 72; blood toggle clears effects; reduced blood; downed guard; wanted consequence; warm short tracer <=0.65m; muzzle origin; reduced-effects flash/tracer suppression; mute.";
            }
            catch(Exception ex){code=1;report=ex.ToString();}
            foreach(var key in Saves){if(SessionState.GetBool(Key+key+"exists",false))PlayerPrefs.SetString(key,SessionState.GetString(Key+key,""));else PlayerPrefs.DeleteKey(key);}
            PlayerPrefs.Save();Time.timeScale=1;
            File.WriteAllText("/private/tmp/harare-bullet-validation.txt",report);
            Debug.Log(report);SessionState.EraseBool(Key);EditorApplication.isPlaying=false;EditorApplication.Exit(code);
        }
    }
}
#endif
