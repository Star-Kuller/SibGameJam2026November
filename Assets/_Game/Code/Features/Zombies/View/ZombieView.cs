using _Game.Code.Features.Zombies.Models;
using _Game.Code.Features.Presentation.View;
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
        private string _animationState;
        private Vector3 _visualPosition;
        private SkinnedMeshRenderer _deathRenderer;

        public int EntityId => _model.EntityId;
        public ZombieModel Model => _model;

        public void Bind(ZombieModel model, float animationSpeed)
        {
            _model = model;
            _visualScale = visual.localScale;
            _visualRotation = visual.localRotation;
            _animationState = null;
            _visualPosition = visual.localPosition;
            _deathRenderer = visual.GetComponentInChildren<SkinnedMeshRenderer>();

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

            if (CharacterDeathPose.Render(animator, visual, _deathRenderer, _visualPosition, progress, Model.Position.y))
            {
                return;
            }

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

            var state = _model.State == ZombieState.Moving ? "Walk" : "Work";

            if (_animationState != state && animator.HasState(0, Animator.StringToHash(state)))
            {
                animator.Play(state);
                _animationState = state;
            }

            if (paused)
            {
                animator.speed = 0;
                return;
            }

            animator.speed = animationSpeed;
        }
    }
}
