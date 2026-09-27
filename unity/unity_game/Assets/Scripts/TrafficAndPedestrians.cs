using UnityEngine;

namespace HarareAfterHours
{
    /// <summary>Shared-car autopilot. Suspended immediately when a player takes the driving seat.</summary>
    public sealed class TrafficVehicle : MonoBehaviour
    {
        private Vector3[] _route;
        private int _nextIndex;
        private float _cruise, _speed, _stopUntil;
        private bool _parked;
        private VehicleController _vehicle;
        private Rigidbody _body;
        private GameObject _autopilotDriver;
        private readonly RaycastHit[] _groundHits=new RaycastHit[12];
        public bool IsAmbient => !_parked;
        public void Configure(Vector3[] route,int startIndex,float speed)
        {
            _route=route;_nextIndex=(startIndex+1)%route.Length;_cruise=speed;
            _vehicle=GetComponent<VehicleController>();_body=GetComponent<Rigidbody>();
            transform.position=route[startIndex]; transform.LookAt(route[_nextIndex]);
            if(_body!=null)_body.isKinematic=true;
        }
        public void ConfigureParked(Vector3[] route)
        {
            _route=route;_nextIndex=0;_cruise=6;_parked=true;
            _vehicle=GetComponent<VehicleController>();_body=GetComponent<Rigidbody>();
        }
        public void RequestStop()=>_stopUntil=Time.time+6;
        private void FixedUpdate()
        {
            if(_route==null || _body==null || _vehicle==null)return;
            bool showDriver=_vehicle.Driver==null && (!_parked || _vehicle.Passenger!=null);
            if(showDriver && _autopilotDriver==null)
            {
                var prefab=Resources.Load<GameObject>("HarareCharacters/Character01");
                if(prefab!=null)
                {
                    _autopilotDriver=Instantiate(prefab,_vehicle.DriverSeat);
                    _autopilotDriver.name="Autopilot driver";
                    var visual=_autopilotDriver.AddComponent<RuntimeCharacterVisual>();
                    visual.Configure(new CharacterCastEntry{DisplayName="Driver",SkinTone="dark",Outfit="navy",Accent="silver"},false);
                    visual.SetSeated(true,true);
                }
            }
            if(_autopilotDriver!=null)_autopilotDriver.SetActive(showDriver);
            if(_vehicle.Driver!=null){_speed=0;return;}
            if(_parked && _vehicle.Passenger==null){_body.isKinematic=false;_speed=0;return;}
            _body.isKinematic=true;
            Vector3 direction=_route[_nextIndex]-transform.position;direction.y=0;
            if(direction.magnitude<1.5f){_nextIndex=(_nextIndex+1)%_route.Length;direction=_route[_nextIndex]-transform.position;direction.y=0;}
            float angle=Vector3.SignedAngle(transform.forward,direction,Vector3.up);
            float desired=Mathf.Abs(angle)>30?2.2f:_cruise;
            var player=HarareAfterHoursBootstrap.Instance?.Player;
            if(Time.time<_stopUntil || (player!=null && !player.IsInVehicle && Vector3.Distance(player.transform.position,transform.position)<5)) desired=0;
            Vector3 front=transform.position+transform.forward*2.8f+Vector3.up*.8f;
            if(Physics.SphereCast(front,.6f,transform.forward,out var hit,Mathf.Max(2,_speed*.65f),~0,QueryTriggerInteraction.Ignore)
                && !hit.transform.IsChildOf(transform)) desired=0;
            _speed=Mathf.MoveTowards(_speed,desired,Time.fixedDeltaTime*(desired<_speed?12:2.5f));
            Quaternion rotation=Quaternion.RotateTowards(_body.rotation,Quaternion.LookRotation(direction),Mathf.Abs(_speed)*24*Time.fixedDeltaTime);
            Vector3 position=_body.position+rotation*Vector3.forward*(_speed*Time.fixedDeltaTime);
            int count=Physics.RaycastNonAlloc(position+Vector3.up*2,Vector3.down,_groundHits,4,~0,QueryTriggerInteraction.Ignore);
            float nearest=100;
            for(int i=0;i<count;i++)
                if(!_groundHits[i].transform.IsChildOf(transform) && _groundHits[i].normal.y>.7f && _groundHits[i].distance<nearest)
                {nearest=_groundHits[i].distance;position.y=Mathf.Lerp(position.y,_groundHits[i].point.y+.025f,Time.fixedDeltaTime*8);}
            _body.MoveRotation(rotation);_body.MovePosition(position);
            _vehicle.SetTrafficMotion(_speed,Mathf.Clamp(angle,-32,32));
        }
    }

    /// <summary>Simple sidewalk animation used until navmesh NPCs are added.</summary>
    public sealed class NpcWanderer : MonoBehaviour
    {
        private Vector3 _center;
        private Vector3 _target;
        private float _radius;
        private float _speed;

        public float Speed => _speed;

        public void Configure(Vector3 center, float radius, float speed)
        {
            _center = center;
            _radius = radius;
            _speed = speed;
            PickTarget();
        }

        private void Update()
        {
            Vector3 toTarget = _target - transform.position;
            toTarget.y = 0f;
            if (toTarget.sqrMagnitude < 0.16f)
            {
                PickTarget();
                return;
            }

            transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(toTarget.normalized), Time.deltaTime * 3.5f);
            transform.position += transform.forward * (_speed * Time.deltaTime);
        }

        private void PickTarget()
        {
            Vector2 offset = Random.insideUnitCircle * _radius;
            _target = _center + new Vector3(offset.x, 0f, offset.y);
        }
    }
}
