using UnityEngine;
using System.Collections.Generic;

namespace HarareAfterHours
{
    /// <summary>Bounded animated knockdown; no dynamic bodies or per-frame allocation.</summary>
    [DefaultExecutionOrder(300)]
    public sealed class EnemyKnockdown : MonoBehaviour
    {
        Transform _visual; Animator _animator; SkinnedMeshRenderer[] _renderers;
        Mesh _baked;readonly List<Vector3> _vertices=new();
        Vector3 _restPosition, _startPosition, _landPosition;
        Quaternion _restRotation, _startRotation, _landRotation;
        float _started, _groundY;
        bool _animatorWasEnabled;
        public bool Active { get; private set; }
        public bool Recovering { get; private set; }
        public bool Settled => Active && !Recovering && Time.time-_started>=.65f;

        public void Initialize(Transform visual)
        {
            _visual=visual;_animator=visual.GetComponent<Animator>();
            _renderers=visual.GetComponentsInChildren<SkinnedMeshRenderer>();
            _baked=new Mesh {name="Reusable knockdown ground-fit mesh"};
            _restPosition=visual.localPosition;_restRotation=visual.localRotation;
        }
        public void Begin(Vector3 away)
        {
            if(Active)return;
            Active=true;Recovering=false;_started=Time.time;
            _animatorWasEnabled=_animator!=null&&_animator.enabled;
            if(_animator!=null)_animator.enabled=false;
            _startPosition=_visual.position;_startRotation=_visual.rotation;
            away=Vector3.ProjectOnPlane(away,Vector3.up).normalized;
            if(away.sqrMagnitude<.01f)away=-transform.forward;
            // Prefer away from the hit, but choose space beside a wall when needed.
            Vector3 best=away;float bestRoom=-1;
            for(int i=0;i<4;i++)
            {
                Vector3 direction=Quaternion.Euler(0,i*90,0)*away;float room=2.1f;
                foreach(var hit in Physics.SphereCastAll(transform.position+Vector3.up*.5f,.25f,direction,2.1f,~0,QueryTriggerInteraction.Ignore))
                    if(!hit.transform.IsChildOf(transform)&&!(hit.collider is CharacterController))room=Mathf.Min(room,hit.distance);
                if(room-i*.02f>bestRoom){bestRoom=room-i*.02f;best=direction;}
            }
            _groundY=transform.position.y;
            float nearestGround=float.PositiveInfinity;
            foreach(var hit in Physics.RaycastAll(transform.position+Vector3.up,Vector3.down,3,~0,QueryTriggerInteraction.Ignore))
                if(!hit.transform.IsChildOf(transform)&&!(hit.collider is CharacterController)&&hit.normal.y>.6f&&hit.distance<nearestGround)
                {nearestGround=hit.distance;_groundY=hit.point.y;}
            _landPosition=_startPosition+best*.18f;
            _landRotation=Quaternion.FromToRotation(Vector3.up,best)*_startRotation;
            // Measure the actual posed skin once; imported renderer bounds can
            // include standing-animation padding and leave a fallen body floating.
            _visual.SetPositionAndRotation(_landPosition,_landRotation);
            _visual.GetComponent<StreetActionPose>()?.ApplyPose();
            float bottom=float.PositiveInfinity;
            foreach(var renderer in _renderers)
            {
                if(renderer==null||!renderer.enabled)continue;
                renderer.BakeMesh(_baked);_baked.GetVertices(_vertices);
                foreach(var vertex in _vertices)bottom=Mathf.Min(bottom,renderer.transform.TransformPoint(vertex).y);
            }
            if(!float.IsPositiveInfinity(bottom))_landPosition.y+=_groundY+.025f-bottom;
            _visual.SetPositionAndRotation(_startPosition,_startRotation);
            _visual.GetComponent<StreetActionPose>()?.ApplyPose();
        }
        public void Recover()
        {
            if(!Active||Recovering)return;
            Recovering=true;_started=Time.time;
            _startPosition=_visual.position;_startRotation=_visual.rotation;
        }
        public void ResetPose()
        {
            if(_visual==null)return;
            _visual.localPosition=_restPosition;_visual.localRotation=_restRotation;
            if(Active&&_animator!=null)_animator.enabled=_animatorWasEnabled;
            Active=false;Recovering=false;
        }
        void LateUpdate()
        {
            if(!Active||Time.timeScale<=0)return;
            float t=Mathf.Clamp01((Time.time-_started)/(Recovering?.55f:.65f));
            float eased=t*t*(3-2*t);
            Vector3 target=Recovering?_visual.parent.TransformPoint(_restPosition):_landPosition;
            Quaternion rotation=Recovering?_visual.parent.rotation*_restRotation:_landRotation;
            _visual.SetPositionAndRotation(Vector3.Lerp(_startPosition,target,eased),Quaternion.Slerp(_startRotation,rotation,eased));
            if(!Recovering)_visual.position+=Vector3.up*Mathf.Sin(t*Mathf.PI)*.12f;
            if(Recovering&&t>=1)ResetPose();
        }
        void OnDestroy(){if(_baked!=null)Destroy(_baked);}
    }
}
