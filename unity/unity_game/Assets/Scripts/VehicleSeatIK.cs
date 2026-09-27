using UnityEngine;
namespace HarareAfterHours
{
    // Lives beside the Animator so Unity invokes the Humanoid IK callback.
    public sealed class VehicleSeatIK : MonoBehaviour
    {
        private Animator _animator;
        private Transform _left,_right;
        public void Configure(Transform left,Transform right){_animator=GetComponent<Animator>();_left=left;_right=right;}
        private void OnAnimatorIK(int layer)
        {
            if(_animator==null)return;
            bool active=_left!=null && _right!=null && _animator.GetBool("Seated") && _animator.GetBool("DrivingSeat");
            _animator.SetIKPositionWeight(AvatarIKGoal.LeftHand,active?1:0);
            _animator.SetIKPositionWeight(AvatarIKGoal.RightHand,active?1:0);
            if(active){_animator.SetIKPosition(AvatarIKGoal.LeftHand,_left.position);_animator.SetIKPosition(AvatarIKGoal.RightHand,_right.position);}
            else GetComponentInParent<PlayerEquipmentVisual>()?.ApplyHandIK(_animator);
        }
    }
}
