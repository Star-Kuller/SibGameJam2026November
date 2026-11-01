using _Game.Code.Features.Zombies.Models;
using UnityEngine;

namespace _Game.Code.Features.Zombies.View
{
    [DisallowMultipleComponent]
    public sealed class ZombieView : MonoBehaviour
    {
        [SerializeField] private Transform visual;
        [SerializeField] private Collider hitbox;
        [SerializeField] private Animator animator;

        private ZombieModel _model;
        private Vector3 _visualScale;
        private Quaternion _visualRotation;

        public int EntityId => _model.EntityId;
        public ZombieModel Model => _model;

        public void Bind(ZombieModel model, float animationSpeed)
        {
            _model = model;
            _visualScale = visual.localScale;
            _visualRotation = visual.localRotation;

            if (animator != null)
            {
                animator.applyRootMotion = false;
            }

            RenderPose(animationSpeed);
        }

        public void RenderPose(float animationSpeed)
        {
            transform.position = _model.Position;

            var heading = Vector3.ProjectOnPlane(_model.Forward, Vector3.up);

            if (heading.sqrMagnitude > 0.001f)
            {
                transform.rotation = Quaternion.LookRotation(heading);
            }

            hitbox.enabled = _model.State != ZombieState.Dead;
            SetAnimationPaused(false, animationSpeed);
        }

        public void ShowDeath(float progress)
        {
            hitbox.enabled = false;

            var rotation = Quaternion.Euler(75 * progress, 0, 0);
            var scale = 1 - 0.8f * progress;

            visual.localRotation = _visualRotation * rotation;
            visual.localScale = _visualScale * scale;
        }

        public void SetAnimationPaused(bool paused, float animationSpeed)
        {
            if (animator == null)
            {
                return;
            }

            if (paused || _model.State != ZombieState.Moving)
            {
                animator.speed = 0;
                return;
            }

            animator.speed = animationSpeed;
        }
    }
}
