#if UNITY_EDITOR
using System;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace HarareAfterHours.EditorTools
{
    [InitializeOnLoad]
    public static class NavigationControlsValidation
    {
        const string Key="Harare.NavigationControlsValidation";
        static int _stage,_frame;
        static float _started;
        static double _deadline;
        static ThirdPersonController _player;
        static CharacterController _body;
        static GameObject _floor,_wall;
        static Vector3 _wallStart;

        static NavigationControlsValidation()=>EditorApplication.update+=Tick;

        public static void Run()
        {
            Directory.CreateDirectory("/private/tmp/harare-navigation-controls");
            EditorSceneManager.OpenScene("Assets/Scenes/SampleScene.unity");
            SessionState.SetBool(Key,true);
            EditorApplication.isPlaying=true;
        }

        static void Tick()
        {
            if(!SessionState.GetBool(Key,false)||!EditorApplication.isPlaying||_frame==Time.frameCount)return;
            _frame=Time.frameCount;
            try
            {
                if(_deadline==0)_deadline=EditorApplication.timeSinceStartup+90;
                Require(EditorApplication.timeSinceStartup<_deadline,"navigation validation timed out");
                var game=HarareAfterHoursBootstrap.Instance;
                if(game==null||game.Player==null||Time.time<3)return;
                _player=game.Player;_body=_player.GetComponent<CharacterController>();
                switch(_stage)
                {
                    case 0:
                        _floor=GameObject.CreatePrimitive(PrimitiveType.Cube);_floor.name="Navigation validation ground";
                        _floor.transform.position=new Vector3(1000,-.5f,1000);_floor.transform.localScale=new Vector3(40,1,40);
                        _wall=GameObject.CreatePrimitive(PrimitiveType.Cube);_wall.name="Navigation validation wall";
                        _wall.transform.position=new Vector3(1000,2,1003);_wall.transform.localScale=new Vector3(8,4,.3f);
                        _body.enabled=false;_player.transform.SetPositionAndRotation(new Vector3(1000,.1f,1000),Quaternion.identity);_body.enabled=true;
                        Camera.main.GetComponent<ThirdPersonCameraRig>()?.Recenter();
                        Physics.SyncTransforms();MobileControlState.Reset();Next();break;
                    case 1:
                        MobileControlState.SetMove(Vector2.up);
                        if(Time.time-_started<1.2f)return;
                        Require(_player.transform.position.z>1001f,"up moves the player toward a wall");
                        Require(_player.transform.position.z<1002.7f,"wall blocks forward movement");
                        _wallStart=_player.transform.position;Next();break;
                    case 2:
                        MobileControlState.SetMove(new Vector2(1,1));
                        if(Time.time-_started<.8f)return;
                        Require(_player.transform.position.x>_wallStart.x+.45f,"diagonal input slides along a wall");
                        Require(_player.transform.position.z<1002.7f,"wall slide never passes through the wall");
                        _wallStart=_player.transform.position;Next();break;
                    case 3:
                        MobileControlState.SetMove(Vector2.right);
                        if(Time.time-_started<.45f)return;
                        Require(_player.transform.position.x>_wallStart.x+.4f,"player recovers from a wall and keeps moving sideways");
                        Finish("PASS: D-pad-style cardinal movement reaches a wall, slides along it and recovers without trapping the player.",0);break;
                }
            }
            catch(Exception error){Finish("FAIL: "+error,1);}
        }

        static void Next(){_stage++;_started=Time.time;}
        static void Require(bool condition,string message){if(!condition)throw new Exception(message);}
        static void Finish(string report,int code)
        {
            MobileControlState.Reset();
            if(_floor!=null)UnityEngine.Object.Destroy(_floor);
            if(_wall!=null)UnityEngine.Object.Destroy(_wall);
            File.WriteAllText("/private/tmp/harare-navigation-controls/validation.txt",report);
            Debug.Log("[NavigationControlsValidation] "+report);
            SessionState.EraseBool(Key);EditorApplication.isPlaying=false;EditorApplication.Exit(code);
        }
    }
}
#endif
