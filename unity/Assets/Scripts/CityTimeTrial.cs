using UnityEngine;
namespace HarareAfterHours
{
    /// <summary>Opt-in ordered-checkpoint time trial, independent of campaign progression.</summary>
    public sealed class CityTimeTrial : MonoBehaviour
    {
        public static CityTimeTrial Instance { get; private set; }
        public static readonly Vector3[] Route={new(0,.2f,110),new(0,.2f,420),new(420,.2f,420),new(420,.2f,-420),new(-420,.2f,-420),new(-420,.2f,420),new(0,.2f,420),new(0,.2f,110)};
        public static float LengthMetres { get {float length=0;for(int i=1;i<Route.Length;i++)length+=Vector3.Distance(Route[i-1],Route[i]);return length;} }
        public bool Running { get; private set; }
        public int Checkpoint { get; private set; }
        public float Elapsed => Running?Time.time-_started:_finished;
        public float Best => PlayerPrefs.GetFloat("Harare.AvenueTrial.Best.v1",0);
        public bool AtStart => _player!=null&&Vector3.Distance(_player.transform.position,Route[0])<12;
        public string ActionLabel => Running?"END\nRACE":AtStart&&_player.IsDriving?"RACE":null;
        public string Objective => Running?$"CIRCUIT {Checkpoint}/{Route.Length-1} · {Mathf.RoundToInt(Vector3.Distance(_player.transform.position,Route[Checkpoint]))} m":null;
        public string Hint
        {
            get
            {
                if(Time.time<_noticeUntil)return _notice;
                if(!Running)return AtStart?"Stop at the line · tap RACE · 3.98 km time trial":null;
                float angle=Vector3.SignedAngle(_player.CurrentVehicle.transform.forward,Route[Checkpoint]-_player.transform.position,Vector3.up);
                string turn=Mathf.Abs(angle)<25?"AHEAD":angle>0?"RIGHT":"LEFT";
                return $"{Elapsed:0.0}s · {turn} · END RACE cancels";
            }
        }
        ThirdPersonController _player;PlayerProgression _score;VehicleController _car;
        float _started,_finished,_offRoad,_noticeUntil;string _notice;Vector3 _lastPosition;
        LineRenderer _gate;
        Material _lettering;
        public void Configure(ThirdPersonController player,PlayerProgression score)
        {
            Instance=this;_player=player;_score=score;
            var sign=GameObject.CreatePrimitive(PrimitiveType.Cube);sign.name="Avenue circuit start sign";sign.transform.SetParent(transform,false);sign.transform.position=new Vector3(9,3,106);sign.transform.localScale=new Vector3(3.5f,1.3f,.14f);sign.GetComponent<Renderer>().sharedMaterial=GroundEnvironment.Surface("Concrete");sign.GetComponent<Collider>().enabled=false;Destroy(sign.GetComponent<Collider>());
            foreach(float x in new[]{7.6f,10.4f}){var post=GameObject.CreatePrimitive(PrimitiveType.Cube);post.name="Circuit sign post";post.transform.SetParent(transform,false);post.transform.position=new Vector3(x,1.25f,106);post.transform.localScale=new Vector3(.06f,2.5f,.06f);post.GetComponent<Renderer>().sharedMaterial=GroundEnvironment.Surface("Asphalt");post.GetComponent<Collider>().enabled=false;Destroy(post.GetComponent<Collider>());}
            var label=new GameObject("Circuit instructions");label.transform.SetParent(transform,false);label.transform.position=new Vector3(9,3,105.9f);
            var text=label.AddComponent<TextMesh>();text.text="AVENUE CIRCUIT\n3.98 km TIME TRIAL\nSTOP · TAP RACE";text.anchor=TextAnchor.MiddleCenter;text.alignment=TextAlignment.Center;text.fontSize=48;text.characterSize=.07f;text.color=new Color(.07f,.12f,.12f);
            _lettering=new Material(Resources.Load<Shader>("HarareWorldText"));_lettering.mainTexture=text.font.material.mainTexture;label.GetComponent<Renderer>().sharedMaterial=_lettering;
            for(int i=0;i<12;i++)
            {
                var tile=GameObject.CreatePrimitive(PrimitiveType.Cube);tile.name="Circuit start paint";tile.transform.SetParent(transform,false);
                tile.transform.position=Route[0]+new Vector3(i-5.5f,-.072f,0);tile.transform.localScale=new Vector3(.8f,.008f,1.4f);
                tile.GetComponent<Renderer>().sharedMaterial=GroundEnvironment.Surface(i%2==0?"RoadPaint":"Asphalt");tile.GetComponent<Collider>().enabled=false;Destroy(tile.GetComponent<Collider>());
            }
            var go=new GameObject("Ordered circuit checkpoint gate");go.transform.SetParent(transform,false);_gate=go.AddComponent<LineRenderer>();
            _gate.sharedMaterial=GroundEnvironment.Surface("RoadPaint");_gate.widthMultiplier=.13f;_gate.positionCount=4;_gate.useWorldSpace=true;PositionGate(Route[0],Vector3.forward);
        }
        public bool TryInteract(ThirdPersonController player)
        {
            if(player!=_player||!player.IsDriving)return false;
            if(Running){End("Race ended · free roam resumed");return true;}
            if(!AtStart)return false;
            if(player.CurrentVehicle.SpeedKph>3){Notice("Stop at the start line to begin");return true;}
            if(StreetActionDirector.Instance!=null&&StreetActionDirector.Instance.Wanted>0){Notice("Lose the police before starting a race");return true;}
            _car=player.CurrentVehicle;_started=Time.time;_finished=0;_offRoad=0;Checkpoint=1;Running=true;_lastPosition=player.transform.position;
            PositionGate(Route[1],Route[1]-Route[0]);Notice("GO · follow the checkpoint gates");return true;
        }
        void Update()
        {
            if(!Running||Time.timeScale<=0)return;
            if(!_player.IsDriving||_player.CurrentVehicle!=_car||_player.InputLocked){End("Race ended · vehicle changed");return;}
            Vector3 p=_player.transform.position;
            if(Vector3.Distance(_lastPosition,p)>Mathf.Max(50,80*Time.deltaTime)){End("Race ended · route reset");return;}_lastPosition=p;
            Vector3 a=Route[Checkpoint-1],b=Route[Checkpoint];
            float t=Mathf.Clamp01(Vector3.Dot(p-a,b-a)/(b-a).sqrMagnitude);
            float distance=Vector3.Distance(p,Vector3.Lerp(a,b,t));
            _offRoad=distance>24?_offRoad+Time.deltaTime:0;
            if(_offRoad>5){End("Race ended · return to the marked roads");return;}
            if(Vector3.Distance(p,b)>12)return;
            Checkpoint++;
            if(Checkpoint==Route.Length)
            {
                _finished=Time.time-_started;Running=false;
                bool record=Best<=0||_finished<Best;if(record){PlayerPrefs.SetFloat("Harare.AvenueTrial.Best.v1",_finished);PlayerPrefs.Save();}
                _score.AwardMissionStep("avenue_time_trial_first_finish",500);
                Notice($"FINISH {_finished:0.0}s · "+(record?"NEW BEST":"BEST "+Best.ToString("0.0")+"s"));PositionGate(Route[0],Vector3.forward);return;
            }
            PositionGate(Route[Checkpoint],Route[Checkpoint]-Route[Checkpoint-1]);
        }
        void End(string message){_finished=Elapsed;Running=false;Notice(message);PositionGate(Route[0],Vector3.forward);}
        void Notice(string value){_notice=value;_noticeUntil=Time.time+5;}
        void PositionGate(Vector3 p,Vector3 direction)
        {
            Vector3 side=Vector3.Cross(Vector3.up,direction.normalized)*5.5f;
            _gate.SetPositions(new[]{p-side,p-side+Vector3.up*4.5f,p+side+Vector3.up*4.5f,p+side});
        }
        void OnDestroy(){if(_lettering!=null)Destroy(_lettering);if(Instance==this)Instance=null;}
    }
}
