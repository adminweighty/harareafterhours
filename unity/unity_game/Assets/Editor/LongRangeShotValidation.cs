#if UNITY_EDITOR
using System;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace HarareAfterHours.EditorTools
{
    public static class LongRangeShotValidation
    {
        public static void Run()
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            try
            {
                var player = new GameObject("Range test player");
                var controller = player.AddComponent<ThirdPersonController>();
                var combat = player.AddComponent<PlayerCombat>();
                Set(combat,"_player",controller);
                var camera = new GameObject("Range test camera").AddComponent<Camera>();
                camera.tag="MainCamera";
                camera.transform.position=new Vector3(.7f,1.4f,-3);
                var body = GameObject.CreatePrimitive(PrimitiveType.Capsule);
                var target=body.AddComponent<CombatTarget>();
                var wall=GameObject.CreatePrimitive(PrimitiveType.Cube);
                wall.transform.localScale=new Vector3(10,10,1);
                wall.SetActive(false);
                foreach(var weapon in new[]{"Pulse rifle","Pulse pistol"})
                {
                    combat.Equip(weapon);
                    float limit=weapon=="Pulse rifle"?250:100;
                    if(combat.AttackRange!=limit)throw new Exception("Unexpected range");
                    foreach(float range in new[]{60f,limit-2,limit+5})
                    {
                        body.transform.position=new Vector3(0,1.4f,range);
                        target.Configure(null,10);
                        camera.transform.LookAt(body.transform.position);
                        Physics.SyncTransforms();
                        Set(combat,"_nextPulseAt",-100f);combat.TryAttack();
                        int expected=range<limit?9:10;
                        if(target.RemainingIntegrity!=expected)throw new Exception(weapon+" range "+range+" failed");
                    }
                    body.transform.position=new Vector3(0,1.4f,60);
                    target.Configure(null,10);camera.transform.LookAt(body.transform.position);
                    wall.transform.position=new Vector3(0,1.4f,30);wall.SetActive(true);
                    Physics.SyncTransforms();Set(combat,"_nextPulseAt",-100f);combat.TryAttack();
                    if(target.RemainingIntegrity!=10)throw new Exception("Shot penetrated wall");
                    wall.SetActive(false);
                }
                Debug.Log("PASS: actual TryAttack hits 60m and near maximum rifle/pistol ranges; beyond-range misses; walls block both weapons; offset shoulder camera converges on target.");
                EditorApplication.Exit(0);
            }
            catch(Exception ex){Debug.LogException(ex);EditorApplication.Exit(1);}
        }
        static void Set(object instance,string field,object value)=>instance.GetType().GetField(field,BindingFlags.NonPublic|BindingFlags.Instance).SetValue(instance,value);
    }
}
#endif
