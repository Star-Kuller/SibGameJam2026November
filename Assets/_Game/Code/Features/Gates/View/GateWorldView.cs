using UnityEngine;

namespace _Game.Code.Features.Gates.View
{
    public sealed class GateWorldView : MonoBehaviour
    {
        [SerializeField] private Transform leftHinge;
        [SerializeField] private Transform rightHinge;
        [SerializeField] private Collider[] doorColliders;

        public void Render(float openness, bool passable)
        {
            leftHinge.localRotation = Quaternion.Euler(0, 95 * openness, 0);
            rightHinge.localRotation = Quaternion.Euler(0, -95 * openness, 0);

            foreach (var door in doorColliders)
            {
                door.enabled = !passable;
            }
        }
    }
}
