using UnityEngine;
using System.Collections.Generic;
namespace HarareAfterHours
{
    // Additive humanoid posing keeps the existing shared locomotion rig intact.
    [DefaultExecutionOrder(100)]
    public sealed class StreetActionPose : MonoBehaviour
    {
        Animator _animator;float _punchUntil,_flinchUntil;bool _cuffed,_down;Transform _cuffs;
        readonly Dictionary<Transform,Quaternion> _baseRotations=new();
        bool _leftPunch;
        float _kickUntil;
        public bool Kicking => Time.time < _kickUntil;
        public void Kick(){_kickUntil=Time.time+.65f;_punchUntil=0;}
        public bool Cuffed=>_cuffed;
        void Awake(){_animator=GetComponent<Animator>();}
        public void Punch(){_leftPunch=!_leftPunch;_punchUntil=Time.time+.42f;}
        public void Flinch()=>_flinchUntil=Time.time+.25f;
        public void SetDown(bool down){_down=down;if(down){_kickUntil=0;_punchUntil=0;}}
        public void SetCuffed(bool cuffed){_cuffed=cuffed;}
        void RestorePose(){foreach(var pair in _baseRotations)if(pair.Key!=null)pair.Key.localRotation=pair.Value;_baseRotations.Clear();}
        void Remember(Transform bone){if(bone!=null&&!_baseRotations.ContainsKey(bone))_baseRotations.Add(bone,bone.localRotation);}
        void Update()=>RestorePose();
        void OnDisable()=>RestorePose();
        void LateUpdate()=>ApplyPose();
        public void ApplyPose()
        {
            RestorePose();
            if(_animator==null||!_animator.isHuman)return;
            if(GetComponentInParent<HostileFirearm>()?.Aiming==true)
            {
                Arm(HumanBodyBones.RightUpperArm,HumanBodyBones.RightLowerArm,transform.TransformPoint(new Vector3(.15f,1.4f,.65f)));
                Arm(HumanBodyBones.LeftUpperArm,HumanBodyBones.LeftLowerArm,transform.TransformPoint(new Vector3(.05f,1.35f,.55f)));
            }
            var player=GetComponentInParent<ThirdPersonController>();
            if(player!=null&&!player.IsInVehicle&&!player.IsCrouching&&player.GetComponent<PlayerCombat>()?.IsRanged==true)
            {
                var body=player.GetComponent<CharacterController>();
                Vector3 motion=player.transform.InverseTransformDirection(body.velocity);
                if(body.isGrounded&&new Vector2(motion.x,motion.z).magnitude>.2f)
                {
                    float yaw=Mathf.Clamp(Mathf.Atan2(motion.x,Mathf.Abs(motion.z))*Mathf.Rad2Deg,-65,65);
                    var hips=_animator.GetBoneTransform(HumanBodyBones.Hips);var spine=_animator.GetBoneTransform(HumanBodyBones.Spine);
                    if(hips!=null&&spine!=null){Remember(hips);Remember(spine);hips.rotation=Quaternion.AngleAxis(yaw,Vector3.up)*hips.rotation;spine.rotation=Quaternion.AngleAxis(-yaw,Vector3.up)*spine.rotation;}
                }
            }
            if(player!=null&&!player.IsInVehicle&&player.IsSliding)
            {
                Bend(HumanBodyBones.LeftUpperLeg,-50);Bend(HumanBodyBones.RightUpperLeg,player.IsSliding?-80:-50);
                Bend(HumanBodyBones.LeftLowerLeg,85);Bend(HumanBodyBones.RightLowerLeg,player.IsSliding?25:85);
                Bend(HumanBodyBones.Spine,player.IsSliding?-18:15);
            }
            if(_cuffed)
            {
                Arm(HumanBodyBones.LeftUpperArm,HumanBodyBones.LeftLowerArm,transform.TransformPoint(new Vector3(-.11f,1.05f,.34f)));
                Arm(HumanBodyBones.RightUpperArm,HumanBodyBones.RightLowerArm,transform.TransformPoint(new Vector3(.11f,1.05f,.34f)));
                if(_cuffs==null)MakeCuffs();_cuffs.gameObject.SetActive(true);
                var left=_animator.GetBoneTransform(HumanBodyBones.LeftHand);var right=_animator.GetBoneTransform(HumanBodyBones.RightHand);
                _cuffs.position=(left.position+right.position)*.5f;_cuffs.rotation=transform.rotation;
            }
            else if(_cuffs!=null)_cuffs.gameObject.SetActive(false);
            if(!_cuffed&&Time.time<_punchUntil)
            {
                float progress=1-(_punchUntil-Time.time)/.42f;
                float reach=progress<.3f?progress/.3f:Mathf.Clamp01((1-progress)/.7f);
                Arm(_leftPunch?HumanBodyBones.LeftUpperArm:HumanBodyBones.RightUpperArm,
                    _leftPunch?HumanBodyBones.LeftLowerArm:HumanBodyBones.RightLowerArm,
                    transform.TransformPoint(new Vector3(_leftPunch?-.12f:.12f,1.35f,.3f+reach*.55f)));
                Arm(_leftPunch?HumanBodyBones.RightUpperArm:HumanBodyBones.LeftUpperArm,
                    _leftPunch?HumanBodyBones.RightLowerArm:HumanBodyBones.LeftLowerArm,
                    transform.TransformPoint(new Vector3(_leftPunch?.22f:-.22f,1.3f,.3f)));
            }
            if(!_cuffed&&!_down&&Kicking)
            {
                float t=1-(_kickUntil-Time.time)/.65f;
                float reach=t<.34f?t/.34f:Mathf.Clamp01((1-t)/.66f);
                var upper=_animator.GetBoneTransform(HumanBodyBones.RightUpperLeg);
                var lower=_animator.GetBoneTransform(HumanBodyBones.RightLowerLeg);
                var foot=_animator.GetBoneTransform(HumanBodyBones.RightFoot);
                if(upper!=null&&lower!=null&&foot!=null)
                {
                    Remember(upper);Remember(lower);
                    Vector3 target=Vector3.Lerp(foot.position,transform.TransformPoint(new Vector3(.15f,.9f,.85f)),reach);
                    for(int i=0;i<2;i++)
                    {
                        lower.rotation=Quaternion.FromToRotation(foot.position-lower.position,target-lower.position)*lower.rotation;
                        upper.rotation=Quaternion.FromToRotation(foot.position-upper.position,target-upper.position)*upper.rotation;
                    }
                }
            }
            if(_down || Time.time<_flinchUntil)
            {
                if(_down)
                {
                    Arm(HumanBodyBones.LeftUpperArm,HumanBodyBones.LeftLowerArm,transform.TransformPoint(new Vector3(-.28f,1.0f,.02f)));
                    Arm(HumanBodyBones.RightUpperArm,HumanBodyBones.RightLowerArm,transform.TransformPoint(new Vector3(.28f,1.0f,.02f)));
                }
                var spine=_animator.GetBoneTransform(HumanBodyBones.Spine);
                if(spine!=null){Remember(spine);spine.localRotation*=Quaternion.Euler(_down?8:-12,0,0);}
            }
        }
        void Bend(HumanBodyBones name,float angle)
        {
            var bone=_animator.GetBoneTransform(name);if(bone==null)return;
            Remember(bone);bone.localRotation*=Quaternion.Euler(angle,0,0);
        }
        void Arm(HumanBodyBones upperBone,HumanBodyBones lowerBone,Vector3 target)
        {
            Transform upper=_animator.GetBoneTransform(upperBone),lower=_animator.GetBoneTransform(lowerBone);
            Transform hand=_animator.GetBoneTransform(upperBone==HumanBodyBones.LeftUpperArm?HumanBodyBones.LeftHand:HumanBodyBones.RightHand);
            if(upper==null||lower==null||hand==null)return;
            Remember(upper);Remember(lower);
            // Bounded two-pass CCD, affecting only this arm after locomotion.
            for(int i=0;i<2;i++)
            {lower.rotation=Quaternion.FromToRotation(hand.position-lower.position,target-lower.position)*lower.rotation;
             upper.rotation=Quaternion.FromToRotation(hand.position-upper.position,target-upper.position)*upper.rotation;}
        }
        void MakeCuffs()
        {
            _cuffs=new GameObject("Visible handcuffs").transform;_cuffs.SetParent(transform,false);
            var material=RuntimeMaterialFactory.Create("Universal Render Pipeline/Lit","Standard");material.SetColor("_BaseColor",new Color(.55f,.58f,.6f));material.SetFloat("_Metallic",.8f);
            foreach(float x in new[]{-.11f,.11f})
            {
                var loop=new GameObject("Wrist cuff").AddComponent<LineRenderer>();loop.transform.SetParent(_cuffs,false);loop.useWorldSpace=false;loop.loop=true;loop.positionCount=12;loop.startWidth=loop.endWidth=.015f;loop.sharedMaterial=material;
                for(int i=0;i<12;i++){float a=i*Mathf.PI/6;loop.SetPosition(i,new Vector3(x+Mathf.Cos(a)*.055f,Mathf.Sin(a)*.055f,0));}
            }
            var chain=new GameObject("Cuff link").AddComponent<LineRenderer>();chain.transform.SetParent(_cuffs,false);chain.useWorldSpace=false;chain.positionCount=2;chain.startWidth=chain.endWidth=.012f;chain.sharedMaterial=material;chain.SetPositions(new[]{new Vector3(-.06f,0,0),new Vector3(.06f,0,0)});
        }
    }
}
