#if UNITY_EDITOR
using System;
using System.Reflection;
using UnityEngine;
using Object=UnityEngine.Object;

namespace HarareAfterHours.EditorTools
{
    public static class BloodCombatValidation
    {
        public static void Check(HarareAfterHoursBootstrap game)
        {
            var blood=game.Player.GetComponent<BloodHitEffects>();
            Require(blood!=null,"Blood effects configured from loadout");
            var actor=StreetActionDirector.Instance.Actors.Find(item=>item.Officer && item.gameObject.activeInHierarchy);
            actor.enabled=false;
            var body=actor.GetComponent<CharacterController>();
            body.enabled=false;
            actor.transform.position=game.Player.transform.position+Vector3.forward*8;
            body.enabled=true;
            var floor=GameObject.CreatePrimitive(PrimitiveType.Cube);
            floor.name="Blood QA ground";
            floor.transform.position=actor.transform.position+Vector3.down*.1f;
            floor.transform.localScale=new Vector3(8,.1f,8);
            game.Player.GetComponent<PlayerLoadout>().ApplyJson("{\"weapon\":\"Pulse rifle\"}",false);
            var camera=Camera.main;
            camera.transform.SetPositionAndRotation(game.Player.transform.position+Vector3.up*1.2f,Quaternion.identity);
            Physics.SyncTransforms();
            var wall=GameObject.CreatePrimitive(PrimitiveType.Cube);
            wall.transform.position=game.Player.transform.position+new Vector3(0,1,4);
            wall.transform.localScale=new Vector3(4,4,.2f);
            Physics.SyncTransforms();
            int emissions=blood.EmissionCount;
            Fire(game);
            Require(blood.EmissionCount==emissions && !actor.Stunned,"Wall blocks character hits and blood");
            wall.SetActive(false);Object.Destroy(wall);Physics.SyncTransforms();
            Fire(game);
            Require(actor.Stunned && blood.EmissionCount==emissions+1 && blood.ActiveCount>0,$"Raycast hits character and creates blood (stunned={actor.Stunned}, emissions={blood.EmissionCount-emissions}, effects={blood.ActiveCount})");
            Require(StreetActionDirector.Instance.Wanted>0,"Shooting officer preserves wanted consequences");
            Require(game.Player.GetComponent<WeaponFeedback>().HitConfirmed,"Character hit confirms reticle");
            Require(game.Player.GetComponent<AudioSource>().isPlaying,"Unmuted shot starts audio playback");
            Require(camera.GetComponent<AudioListener>().enabled,"Gameplay audio listener enabled");
            Capture(actor.transform);
            Fire(game);Fire(game);
            Require(actor.Down,"Three shots down a combat actor");
            Require(blood.FatalityCount==1 && StreetActionDirector.Instance.TakedownActive && StreetActionDirector.Instance.TakedownText.Contains("TAKEDOWN"),"Final shot creates a single visible takedown blood burst and cue");
            emissions=blood.EmissionCount;Fire(game);
            Require(blood.EmissionCount==emissions && blood.FatalityCount==1,"Downed actors do not repeat blood, takedowns or rewards");
            for(int i=0;i<30;i++)blood.Emit(actor.transform,actor.transform.position+Vector3.up,Vector3.forward);
            Require(blood.ActiveCount<=72,"Blood pool bounded under repeated hits");
            game.Player.GetComponent<PlayerLoadout>().ApplyJson("{\"hideBloodEffects\":true}");
            Require(blood.ActiveCount==0 && PlayerPrefs.GetString(PlayerLoadout.SaveKey).Contains("\"hideBloodEffects\":true"),"Hiding blood clears existing effects and saves choice");
            blood.Emit(actor.transform,actor.transform.position+Vector3.up,Vector3.forward);
            Require(blood.ActiveCount==0,"Hidden blood prevents future effects");
            blood.Configure(false,true);
            blood.Emit(actor.transform,actor.transform.position+Vector3.up,Vector3.forward);
            Require(blood.ActiveCount<=7,"Reduced effects limits droplets");
            blood.Clear();
            Object.Destroy(floor);
            Debug.Log("[BloodCombat] PASS: wall, actual ranged actor hit, reticle, audio playback/listener, downed guard, pool cap, saved toggle, immediate clear, reduced effects.");
        }
        private static void Fire(HarareAfterHoursBootstrap game)
        {
            typeof(PlayerCombat).GetField("_nextPulseAt",BindingFlags.Instance|BindingFlags.NonPublic).SetValue(game.Combat,0f);
            game.Combat.TryAttack();
        }
        private static void Capture(Transform actor)
        {
            var camera=new GameObject("Blood QA camera").AddComponent<Camera>();
            camera.CopyFrom(Camera.main);camera.enabled=false;
            camera.transform.position=actor.position+new Vector3(1.8f,1.7f,-3);
            camera.transform.LookAt(actor.position+Vector3.up*.9f);camera.fieldOfView=42;
            var texture=new RenderTexture(600,800,24);camera.targetTexture=texture;camera.Render();
            var previous=RenderTexture.active;RenderTexture.active=texture;
            var image=new Texture2D(600,800,TextureFormat.RGB24,false);
            image.ReadPixels(new Rect(0,0,600,800),0,0);image.Apply();
            System.IO.File.WriteAllBytes("/private/tmp/harare-blood-preview.png",image.EncodeToPNG());
            RenderTexture.active=previous;camera.targetTexture=null;
            Object.Destroy(image);Object.Destroy(texture);Object.Destroy(camera.gameObject);
        }
        private static void Require(bool condition,string message){if(!condition)throw new Exception(message);}
    }
}
#endif
