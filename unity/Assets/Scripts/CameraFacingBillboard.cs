using UnityEngine;

namespace HarareAfterHours
{
    /// <summary>
    /// Keeps photo-backed NPC and vehicle details readable while they move
    /// through the 3D world. It is deliberately a presentation layer: the
    /// underlying player, traffic, and pedestrian objects remain real 3D
    /// colliders and movers.
    /// </summary>
    public sealed class CameraFacingBillboard : MonoBehaviour
    {
        private void LateUpdate()
        {
            Camera activeCamera = Camera.main;
            if (activeCamera == null) return;

            Vector3 direction = activeCamera.transform.position - transform.position;
            direction.y = 0f;
            if (direction.sqrMagnitude < 0.001f) return;
            transform.rotation = Quaternion.LookRotation(direction.normalized, Vector3.up);
        }
    }
}
