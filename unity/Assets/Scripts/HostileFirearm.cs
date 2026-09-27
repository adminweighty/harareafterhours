using UnityEngine;
namespace HarareAfterHours
{
    // Telegraph -> sampled aim point -> collision-tested shot. Cover and dodging work.
    public sealed class HostileFirearm : MonoBehaviour
    {
        private StreetActor _actor;private StreetActionDirector _world;private WeaponFeedback _feedback;
        private Transform _gun,_muzzle,_hand;private float _fireAt,_nextShot;private Vector3 _aim;
        private static Material _steel;
        public bool Aiming=>_fireAt>0;
        public void Configure(StreetActor actor,StreetActionDirector world)
        {
            _actor=actor;_world=world;
            var animator=GetComponentInChildren<Animator>();_hand=animator.GetBoneTransform(HumanBodyBones.RightHand);
            _gun=new GameObject("Robber pistol").transform;_gun.SetParent(transform,false);
            if(_steel==null){_steel=RuntimeMaterialFactory.Create("Universal Render Pipeline/Lit","Standard");_steel.SetColor("_BaseColor",new Color(.08f,.085f,.09f));}
            Part("Slide",new Vector3(0,.04f,.1f),new Vector3(.05f,.06f,.24f));
            Part("Grip",new Vector3(0,-.04f,.015f),new Vector3(.045f,.12f,.055f));
            _muzzle=new GameObject("Pistol muzzle").transform;_muzzle.SetParent(_gun,false);_muzzle.localPosition=new Vector3(0,.04f,.23f);
            _feedback=gameObject.AddComponent<WeaponFeedback>();_feedback.Configure(false,false,false);
            var audio=GetComponent<AudioSource>();audio.spatialBlend=1;audio.minDistance=3;audio.maxDistance=45;audio.rolloffMode=AudioRolloffMode.Linear;
        }
        private void Part(string label,Vector3 pos,Vector3 size)
        {
            var part=GameObject.CreatePrimitive(PrimitiveType.Cube);part.name=label;part.transform.SetParent(_gun,false);part.transform.localPosition=pos;part.transform.localScale=size;
            var collider=part.GetComponent<Collider>();collider.enabled=false;Destroy(collider);part.GetComponent<Renderer>().sharedMaterial=_steel;
        }
        public void CancelShot(){_fireAt=0;_nextShot=Time.time+(_world!=null?_world.Difficulty.RangedCooldown:1.5f);}
        public bool Tick(bool engaged,float distance)
        {
            if(!engaged||_actor.Down||_actor.Stunned||_world.Player.IsInVehicle||distance>22||distance<3){CancelShot();return false;}
            Vector3 eye=transform.position+Vector3.up*1.4f;
            if(!_world.Visible(eye,_world.Player.transform,24)){CancelShot();return false;}
            Vector3 facing=_world.Player.transform.position-transform.position;facing.y=0;
            if(facing.sqrMagnitude>.01f)transform.rotation=Quaternion.Slerp(transform.rotation,Quaternion.LookRotation(facing),Time.deltaTime*10);
            if(_fireAt==0&&Time.time>=_nextShot)
            {
                _aim=_world.Player.transform.position+Vector3.up*(1.2f+_world.Player.StanceOffset);
                _fireAt=Time.time+_world.Difficulty.RangedWindup;_world.Notify("ARMED ROBBER · MOVE or TAKE COVER");
            }
            if(_fireAt>0&&Time.time>=_fireAt)
            {
                _fireAt=0;_nextShot=Time.time+_world.Difficulty.RangedCooldown;
                Vector3 direction=(_aim-eye).normalized,end=eye+direction*24;RaycastHit nearest=default;float range=24;
                foreach(var hit in Physics.RaycastAll(eye,direction,24,~0,QueryTriggerInteraction.Ignore))
                    if(!hit.transform.IsChildOf(transform)&&hit.distance<range){nearest=hit;range=hit.distance;end=hit.point;}
                bool playerHit=nearest.collider!=null&&nearest.transform.IsChildOf(_world.Player.transform);
                if(playerHit)_world.Damage(15);
                _feedback.Fire(_muzzle,eye,end,playerHit,playerHit,nearest.collider!=null);
            }
            return distance<14; // chase until within firing distance, then aim from a stable stance
        }
        private void LateUpdate()
        {
            if(_gun==null)return;
            _gun.gameObject.SetActive(!_actor.Down);
            if(_actor.Down||_hand==null)return;
            _gun.SetPositionAndRotation(_hand.position,transform.rotation);
            if(_actor.Stunned||_world.GameOver)CancelShot();
        }
    }
}
