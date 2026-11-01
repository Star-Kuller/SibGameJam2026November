using _Game.Code.Features.Survivors.Models;
using UnityEngine;

namespace _Game.Code.Features.Survivors.View
{
    public sealed class SurvivorView : MonoBehaviour
    {
        [SerializeField] private Transform visual;
        [SerializeField] private Collider hitbox;
        [SerializeField] private Animator animator;

        private Vector3 _originalScale;
        private Quaternion _originalRotation;

        public SurvivorModel Model { get; private set; }

        public void Bind(SurvivorModel model)
        {
            Model = model;
            _originalScale = visual.localScale;
            _originalRotation = visual.localRotation;

            if (animator != null)
            {
                animator.applyRootMotion = false;
            }

            Render(false, 1);
        }

        public void Render(bool paused, float animationSpeed)
        {
            transform.position = Model.Position;
            var heading = Vector3.ProjectOnPlane(Model.Forward, Vector3.up);

            if (heading.sqrMagnitude > 0.001f)
            {
                transform.rotation = Quaternion.LookRotation(heading);
            }

            hitbox.enabled = Model.IsVisible;

            if (animator != null)
            {
                animator.speed = !paused && Model.IsWalking ? animationSpeed : 0;
            }
        }

        public void ShowDeath(float progress)
        {
            hitbox.enabled = false;
            visual.localRotation = _originalRotation * Quaternion.Euler(75 * progress, 0, 0);
            visual.localScale = _originalScale * (1 - 0.8f * progress);
        }
    }
}
