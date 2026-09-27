using System.Collections.Generic;
using UnityEngine;

namespace HarareAfterHours
{
    [RequireComponent(typeof(VehicleController))]
    public sealed class PolicePatrol : MonoBehaviour
    {
        StreetActionDirector _world;VehicleController _car;Rigidbody _body;StreetActor _officer;
        readonly Queue<Vector3> _route=new();Vector3 _goal;float _speed,_repath,_crimeCooldown;int _patrolIndex;
        Renderer _beacon;Material _blue,_lamp,_lettering;AudioSource _siren;AudioClip _clip;
        static readonly Vector3[] Circuit={new(-44,.16f,-44),new(40,.16f,-44),new(40,.16f,40),new(-44,.16f,40)};
        public void Configure(StreetActionDirector world,int index)
        {
            _world=world;_car=GetComponent<VehicleController>();_body=GetComponent<Rigidbody>();_body.isKinematic=true;
            _patrolIndex=index*2;_goal=Circuit[_patrolIndex];
            GetComponent<VehicleAppearance>()?.SetAppearance(new Color(.86f,.89f,.9f));
            var blue=Material(new Color(.035f,.12f,.42f));_blue=blue;
            foreach(float side in new[]{-1f,1f})
            {
                Part("ZRP blue stripe",new Vector3(side*.99f,1.02f,0),new Vector3(.018f,.18f,3.5f),blue);
                var sign=new GameObject("ZRP POLICE lettering");sign.transform.SetParent(transform,false);
                sign.transform.localPosition=new Vector3(side*1.03f,.88f,.1f);
                sign.transform.localRotation=Quaternion.Euler(0,side>0?-90:90,0);
                var text=sign.AddComponent<TextMesh>();text.text="ZRP  POLICE";text.fontSize=48;text.characterSize=.035f;text.anchor=TextAnchor.MiddleCenter;text.color=new Color(.025f,.08f,.25f);
                if(_lettering==null){_lettering=new Material(Resources.Load<Shader>("HarareWorldText"));_lettering.mainTexture=text.font.material.mainTexture;}
                text.GetComponent<Renderer>().sharedMaterial=_lettering;
            }
            _beacon=Part("Blue roof lightbar",new Vector3(0,1.98f,-.2f),new Vector3(.95f,.13f,.27f),blue);
            _lamp=Material(new Color(.06f,.25f,1));_lamp.EnableKeyword("_EMISSION");_lamp.SetColor("_EmissionColor",new Color(.03f,.2f,1.5f));_beacon.sharedMaterial=_lamp;
            _officer=world.SpawnActor(true,transform.position+transform.right*2.4f,"ZRP patrol officer "+(index+1));_officer.gameObject.SetActive(false);
            var driver=Instantiate(Resources.Load<GameObject>("HarareCharacters/Character01"),_car.DriverSeat);
            var visual=driver.AddComponent<RuntimeCharacterVisual>();visual.Configure(new CharacterCastEntry{DisplayName="ZRP driver",SkinTone="dark",Outfit="navy",Accent="silver"},false);visual.SetSeated(true,true);
            _siren=gameObject.AddComponent<AudioSource>();_siren.spatialBlend=1;_siren.minDistance=4;_siren.maxDistance=50;_siren.rolloffMode=AudioRolloffMode.Linear;_siren.volume=.12f;_siren.loop=true;
            const int rate=22050;float[] samples=new float[rate*2];float phase=0;
            for(int i=0;i<samples.Length;i++){float t=i/(float)rate;phase+=2*Mathf.PI*(570+170*Mathf.Sin(t*Mathf.PI))/rate;samples[i]=Mathf.Sin(phase)*.3f;}
            _clip=AudioClip.Create("Fictional patrol wail",samples.Length,1,rate,false);_clip.SetData(samples,0);_siren.clip=_clip;
        }
        static Material Material(Color color){var m=RuntimeMaterialFactory.Create("Universal Render Pipeline/Lit","Standard");m.SetColor("_BaseColor",color);m.enableInstancing=true;return m;}
        Renderer Part(string label,Vector3 position,Vector3 scale,Material material)
        {
            var part=GameObject.CreatePrimitive(PrimitiveType.Cube);part.name=label;part.transform.SetParent(transform,false);part.transform.localPosition=position;part.transform.localScale=scale;
            Destroy(part.GetComponent<Collider>());part.GetComponent<Renderer>().sharedMaterial=material;return part.GetComponent<Renderer>();
        }
        public static Vector3 RoadPoint(Vector3 p)
        {
            float x=Nearest(p.x),z=Nearest(p.z);
            return Mathf.Abs(p.x-x)<Mathf.Abs(p.z-z)?new Vector3(x,.16f,Mathf.Clamp(p.z,-70,70)):new Vector3(Mathf.Clamp(p.x,-70,70),.16f,z);
        }
        static float Nearest(float value)=>Mathf.Abs(value+44)<Mathf.Abs(value+2)?-44:Mathf.Abs(value+2)<Mathf.Abs(value-40)?-2:40;
        void Plan(Vector3 destination)
        {
            _route.Clear();Vector3 start=new(Nearest(transform.position.x),.16f,Nearest(transform.position.z));
            Vector3 end=new(Nearest(destination.x),.16f,Nearest(destination.z));
            // Join the nearest junction along the current road, then traverse
            // orthogonal junctions; never steer diagonally through a city block.
            _route.Enqueue(start);
            while(Mathf.Abs(start.x-end.x)>1){start.x+=Mathf.Sign(end.x-start.x)*42;_route.Enqueue(start);}
            while(Mathf.Abs(start.z-end.z)>1){start.z+=Mathf.Sign(end.z-start.z)*42;_route.Enqueue(start);}
            _route.Enqueue(destination);_goal=destination;
        }
        void FixedUpdate()
        {
            if(_world==null)return;
            bool chase=_world.Wanted>0&&!_world.Arrested;
            if(chase&&!_siren.isPlaying)_siren.Play();if(!chase&&_siren.isPlaying)_siren.Stop();
            _beacon.enabled=!chase || Mathf.Sin(Time.time*4)>-.2f;
            float playerDistance=Vector3.Distance(transform.position,_world.Player.transform.position);
            if(chase&&playerDistance<23&&!_officer.gameObject.activeSelf)
            {
                var position=transform.position+transform.right*2.2f;position.y=.3f;
                if(!Physics.CheckCapsule(position+Vector3.up*.4f,position+Vector3.up*1.4f,.3f,~0,QueryTriggerInteraction.Ignore))
                {_officer.transform.position=position;_officer.gameObject.SetActive(true);}
            }
            Vector3 destination=chase?RoadPoint(_world.LastKnownPosition):Circuit[_patrolIndex];
            if(!chase&&Vector3.Distance(transform.position,destination)<3){_patrolIndex=(_patrolIndex+1)%Circuit.Length;destination=Circuit[_patrolIndex];}
            // Finish each road segment before replanning; avoids mid-road U-turn oscillation.
            if(_route.Count==0 || (Time.time>_repath&&Vector3.Distance(transform.position,new Vector3(Nearest(transform.position.x),.16f,Nearest(transform.position.z)))<2&&Vector3.Distance(_goal,destination)>8))
            {Plan(destination);_repath=Time.time+2;}
            while(_route.Count>0&&Vector3.Distance(transform.position,_route.Peek())<1.3f)_route.Dequeue();
            Vector3 direction=_route.Count>0?_route.Peek()-transform.position:Vector3.zero;direction.y=0;
            float desired=chase?9+_world.Wanted*2:5;
            if(direction.sqrMagnitude<.2f || (chase&&playerDistance<9))desired=0;
            float angle=direction.sqrMagnitude>.01f?Vector3.SignedAngle(transform.forward,direction,Vector3.up):0;
            if(Mathf.Abs(angle)>25)desired=Mathf.Min(desired,2.5f);
            if(Physics.SphereCast(transform.position+Vector3.up*.8f+transform.forward*2.7f,.65f,transform.forward,out var hit,Mathf.Max(2,_speed*.65f),~0,QueryTriggerInteraction.Ignore)&&!hit.transform.IsChildOf(transform))desired=0;
            _speed=Mathf.MoveTowards(_speed,desired,Time.fixedDeltaTime*(desired<_speed?14:3));
            Quaternion rotation=direction.sqrMagnitude>.01f?Quaternion.RotateTowards(_body.rotation,Quaternion.LookRotation(direction),Mathf.Max(15,_speed*24)*Time.fixedDeltaTime):_body.rotation;
            _body.MoveRotation(rotation);_body.MovePosition(_body.position+rotation*Vector3.forward*(_speed*Time.fixedDeltaTime));
            _car.SetTrafficMotion(_speed,Mathf.Clamp(angle,-32,32));
        }
        void OnCollisionEnter(Collision collision)
        {
            var other=collision.gameObject.GetComponentInParent<VehicleController>();
            if(other!=null&&other.Driver==_world.Player&&collision.relativeVelocity.magnitude>4&&Time.time>_crimeCooldown)
            {_crimeCooldown=Time.time+3;_world.ReportCrime();}
        }
        void OnDestroy(){if(_clip!=null)Destroy(_clip);if(_blue!=null)Destroy(_blue);if(_lamp!=null)Destroy(_lamp);if(_lettering!=null)Destroy(_lettering);}
    }
}
