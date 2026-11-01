using _Game.Code.Features.Survivors.Models;
using _Game.Code.Features.Presentation.View;
using UnityEngine;

namespace _Game.Code.Features.Survivors.View
{
    public sealed class SurvivorView : MonoBehaviour
    {
        [SerializeField] private Transform visual;
        [SerializeField] private Collider hitbox;
        [SerializeField] private Animator animator;
        [SerializeField] private SkinnedMeshRenderer clothesRenderer;
        [SerializeField] private Material[] clothesVariants;
        [SerializeField] private GameObject cargoVisual;
        [SerializeField] private GameObject toolVisual;

        private Vector3 _originalScale;
        private Quaternion _originalRotation;
        private string _animationState;
        private Vector3 _visualPosition;
        private SkinnedMeshRenderer _deathRenderer;
        private Transform _friendlyMarker;

        public SurvivorModel Model { get; private set; }

        public void Bind(SurvivorModel model)
        {
            Model = model;
            _originalScale = visual.localScale;
            _originalRotation = visual.localRotation;
            _animationState = null;
            _visualPosition = visual.localPosition;
            _deathRenderer = visual.GetComponentInChildren<SkinnedMeshRenderer>();
            _friendlyMarker = transform.Find("FriendlyMarker");

            if (clothesRenderer != null && clothesVariants != null && clothesVariants.Length > 0)
            {
                clothesRenderer.sharedMaterial = clothesVariants[(model.EntityId - 1) % clothesVariants.Length];
            }

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
                var state = AnimationState();

                if (_animationState != state && animator.HasState(0, Animator.StringToHash(state)))
                {
                    animator.Play(state);
                    _animationState = state;
                }

                animator.speed = paused ? 0 : animationSpeed;
            }

            if (cargoVisual != null)
            {
                cargoVisual.SetActive(Model.IsVisible && Model.CargoMetal + Model.CargoRounds > 0);
            }

            if (toolVisual != null)
            {
                toolVisual.SetActive(Model.State == SurvivorState.Working);
            }
        }

        public void ShowDeath(float progress)
        {
            hitbox.enabled = false;

            if (_friendlyMarker != null)
            {
                _friendlyMarker.gameObject.SetActive(false);
            }

            if (cargoVisual != null)
            {
                cargoVisual.SetActive(false);
            }

            if (toolVisual != null)
            {
                toolVisual.SetActive(false);
            }

            if (CharacterDeathPose.Render(animator, visual, _deathRenderer, _visualPosition, progress, Model.Position.y))
            {
                return;
            }

            visual.localRotation = _originalRotation * Quaternion.Euler(75 * progress, 0, 0);
            visual.localScale = _originalScale * (1 - 0.8f * progress);
        }

        private string AnimationState()
        {
            if (Model.IsWalking)
            {
                return Model.CargoMetal + Model.CargoRounds > 0 ? "Carry" : "Walk";
            }

            return Model.State == SurvivorState.Working ? "Work" : "Idle";
        }
    }
}
