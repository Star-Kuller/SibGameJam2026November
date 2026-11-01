using UnityEngine;

namespace _Game.Code.Features.Gates.View
{
    public sealed class GateWorldView : MonoBehaviour
    {
        [SerializeField] private Transform leftHinge;
        [SerializeField] private Transform rightHinge;
        [SerializeField] private Collider[] doorColliders;
        [SerializeField] private AnimationCurve openingMotion = AnimationCurve.Linear(0, 0, 1, 1);

        public void Render(float openness, bool passable)
        {
            var progress = openingMotion.Evaluate(openness);
            leftHinge.localRotation = Quaternion.Euler(0, 95 * progress, 0);
            rightHinge.localRotation = Quaternion.Euler(0, -95 * progress, 0);

            foreach (var door in doorColliders)
            {
                door.enabled = !passable;
            }
        }
    }
}
