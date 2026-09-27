using System.Collections.Generic;
using UnityEngine;

namespace HarareAfterHours
{
    [RequireComponent(typeof(Rigidbody))]
    public sealed class VehicleController : MonoBehaviour
    {
        [SerializeField] private WheelCollider[] wheels;
        [SerializeField] private Transform[] wheelVisuals;
        [SerializeField] private Transform driverSeat, passengerSeat;
        [SerializeField] private float maxForwardSpeed=22, maxReverseSpeed=5, motorTorque=650, brakeTorque=2600;
        private Rigidbody _body;
        private Vector2 _input;
        private bool _brake;
        private Transform _cameraAnchor;
        private float _steering, _trafficSpeed, _spin;
        private Vector3 _lastPosition;
        private MaterialPropertyBlock _block;
        private Renderer[] _renderers;
        public ThirdPersonController Driver {get;private set;}
        public ThirdPersonController Passenger {get;private set;}
        public Transform DriverSeat=>driverSeat;
        public Transform PassengerSeat=>passengerSeat;
        public Transform CameraTarget=>_cameraAnchor!=null?_cameraAnchor:transform;
        public WheelCollider[] Wheels=>wheels;
        public float SpeedKph=>_body==null?0:(_body.isKinematic?Mathf.Abs(_trafficSpeed):Vector3.ProjectOnPlane(_body.linearVelocity,Vector3.up).magnitude)*3.6f;
        public bool CanBoard=>SpeedKph<4;
        private void Awake()
        {
            _body=GetComponent<Rigidbody>();
            _body.centerOfMass=new Vector3(0,.58f,0);
            _body.constraints=RigidbodyConstraints.None;
            _lastPosition=transform.position;
            _block=new MaterialPropertyBlock();
            _renderers=GetComponentsInChildren<Renderer>();
            CreateCameraAnchor();
        }
        public void ConfigureRig(WheelCollider[] colliders,Transform[] visuals,Transform driver,Transform passenger)
        { wheels=colliders; wheelVisuals=visuals; driverSeat=driver; passengerSeat=passenger; }
        public void CreateCameraAnchor()
        {
            if(_cameraAnchor!=null)return;
            _cameraAnchor=transform.Find("Camera target");
            if(_cameraAnchor==null){_cameraAnchor=new GameObject("Camera target").transform;_cameraAnchor.SetParent(transform,false);_cameraAnchor.localPosition=new Vector3(0,1.2f,-.3f);}
        }
        public bool TryEnter(ThirdPersonController person,bool passenger)
        {
            if(GetComponent<PolicePatrol>()!=null)return false;
            if(person==null || !CanBoard || (passenger?Passenger!=null:Driver!=null))return false;
            if(passenger) Passenger=person;
            else { Driver=person; _body.isKinematic=false; _body.WakeUp(); _input=Vector2.zero; _brake=false; }
            return true;
        }
        public void Enter(ThirdPersonController person)=>TryEnter(person,false);
        public void Exit(ThirdPersonController person)
        {
            if(Driver==person){Driver=null;_input=Vector2.zero;_brake=true;}
            if(Passenger==person)Passenger=null;
        }
        public void SetDrivingInput(Vector2 input,bool brake){_input=Vector2.ClampMagnitude(input,1);_brake=brake;}
        public void SetTrafficMotion(float speed,float steering)
        {
            _trafficSpeed=speed;
            if(Driver==null)_steering=steering;
        }
        public bool TryGetExitPosition(out Vector3 position)
        {
            position=transform.position;
            if(SpeedKph>3)return false;
            foreach(var offset in new[]{new Vector3(1.75f,0,.3f),new Vector3(-1.75f,0,.3f),new Vector3(0,0,-3.35f)})
            {
                Vector3 p=transform.TransformPoint(offset);
                if(!Physics.Raycast(p+Vector3.up*2,Vector3.down,out var hit,4,~0,QueryTriggerInteraction.Ignore) || hit.normal.y<.65f || hit.point.y-transform.position.y>.45f)continue;
                p.y=hit.point.y+.04f;
                bool blocked=false;
                foreach(var c in Physics.OverlapCapsule(p+Vector3.up*.36f,p+Vector3.up*1.48f,.32f,~0,QueryTriggerInteraction.Ignore))
                    if(c!=null && !(c is WheelCollider)) {blocked=true;break;}
                if(!blocked){position=p;return true;}
            }
            return false;
        }
        public Vector3 GetExitPosition()=>TryGetExitPosition(out var p)?p:transform.position+transform.right*1.75f;
        private void FixedUpdate()
        {
            if(wheels==null || wheels.Length!=4 || _body.isKinematic)return;
            float speed=Vector3.Dot(_body.linearVelocity,transform.forward);
            float throttle=Driver==null?0:_input.y;
            float limit=throttle>=0?maxForwardSpeed:maxReverseSpeed;
            bool reversing=throttle*speed<-.6f;
            float braking=Driver==null || _brake || reversing?brakeTorque:0;
            float torque=braking>0?0:throttle*motorTorque*Mathf.Clamp01(1-Mathf.Abs(speed)/limit);
            float maxAngle=Mathf.Lerp(34,9,Mathf.Clamp01(Mathf.Abs(speed)/maxForwardSpeed));
            _steering=Mathf.MoveTowards(_steering,_input.x*maxAngle,90*Time.fixedDeltaTime);
            for(int i=0;i<4;i++)
            {
                wheels[i].motorTorque=torque;
                wheels[i].brakeTorque=braking;
                wheels[i].steerAngle=i<2?_steering:0;
            }
            _body.AddForce(-transform.up*Mathf.Min(speed*speed*2.5f,1800),ForceMode.Force);
        }
        private void LateUpdate()
        {
            if(wheels==null || wheelVisuals==null)return;
            if(_body.isKinematic)
            {
                float distance=Vector3.Dot(transform.position-_lastPosition,transform.forward);
                if(Mathf.Abs(distance)<3)_spin+=distance/wheels[0].radius*Mathf.Rad2Deg;
                for(int i=0;i<4;i++)wheelVisuals[i].localRotation=Quaternion.Euler(0,i<2?_steering:0,0)*Quaternion.Euler(_spin,0,0);
            }
            else for(int i=0;i<4;i++)
            {
                wheels[i].GetWorldPose(out var p,out var q);wheelVisuals[i].SetPositionAndRotation(p,q);
            }
            _lastPosition=transform.position;
            bool braking=_brake || (Driver!=null && _input.y*Vector3.Dot(_body.linearVelocity,transform.forward)<-.6f);
            foreach(var renderer in _renderers)
            {
                var mats=renderer.sharedMaterials;
                for(int i=0;i<mats.Length;i++)if(mats[i]!=null && mats[i].name.Contains("TailLight"))
                {
                    renderer.GetPropertyBlock(_block,i);
                    _block.SetColor("_EmissionColor",new Color(braking?2.4f:.2f,.002f,.002f));
                    renderer.SetPropertyBlock(_block,i);
                }
            }
        }
        private void OnCollisionEnter(Collision collision)
        {
            if(Driver!=null && collision.relativeVelocity.magnitude>4)HarareAfterHoursBootstrap.Instance?.Mission?.RegisterImpact(collision.relativeVelocity.magnitude);
        }
    }
}
