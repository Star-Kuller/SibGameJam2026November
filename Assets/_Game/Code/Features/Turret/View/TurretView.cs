using _Game.Code.Features.Turret.Models;
using UnityEngine;

namespace _Game.Code.Features.Turret.View
{
    public sealed class TurretView : MonoBehaviour
    {
        [Header("Rig")]
        [SerializeField] private Transform yawPivot;
        [SerializeField] private Transform pitchPivot;
        [SerializeField] private Camera gameplayCamera;
        [SerializeField] private float horizontalFieldOfView = 65.81f;

        [Header("Shot feedback")]
        [SerializeField] private Transform barrel;
        [SerializeField] private Transform muzzle;
        [SerializeField] private GameObject muzzleFlash;
        [SerializeField] private LineRenderer tracer;

        private Vector3 _barrelOrigin;
        private bool _originCaptured;
        private float _flashRemaining;

        public Camera Camera => gameplayCamera;

        private void Awake()
        {
            CaptureBarrelOrigin();
            UpdateFraming();
        }

        private void LateUpdate()
        {
            UpdateFraming();
        }

        public void RenderAim(TurretModel model)
        {
            UpdateFraming();
            yawPivot.localRotation = Quaternion.Euler(0, model.Yaw, 0);
            pitchPivot.localRotation = Quaternion.Euler(model.Pitch, 0, 0);
        }

        private void UpdateFraming()
        {
            if (gameplayCamera != null && gameplayCamera.aspect > 0)
            {
                gameplayCamera.fieldOfView = Camera.HorizontalToVerticalFieldOfView(
                    horizontalFieldOfView, gameplayCamera.aspect);
            }
        }

        public void ShowShot(Vector3 endPoint)
        {
            _flashRemaining = 0.055f;
            muzzleFlash.SetActive(true);
            tracer.enabled = true;

            tracer.SetPosition(0, muzzle.position);
            tracer.SetPosition(1, endPoint);
            barrel.localPosition = _barrelOrigin - Vector3.forward * 0.08f;
        }

        public void RenderEffects(float seconds)
        {
            _flashRemaining = Mathf.Max(0, _flashRemaining - seconds);
            var visible = _flashRemaining > 0;

            muzzleFlash.SetActive(visible);
            tracer.enabled = visible;
            barrel.localPosition = Vector3.Lerp(barrel.localPosition, _barrelOrigin, 20 * seconds);
        }

        public void ResetEffects()
        {
            CaptureBarrelOrigin();
            _flashRemaining = 0;

            muzzleFlash.SetActive(false);
            tracer.enabled = false;
            barrel.localPosition = _barrelOrigin;
        }

        private void CaptureBarrelOrigin()
        {
            if (_originCaptured)
            {
                return;
            }

            // VContainer may initialize the encounter before this component's Awake.
            _barrelOrigin = barrel.localPosition;
            _originCaptured = true;
        }
    }
}
