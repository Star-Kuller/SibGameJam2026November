using System;
using UnityEngine;

namespace _Game.Code.Features.Turret.Models
{
    public sealed class TurretModel
    {
        private readonly float _yawLimit;
        private readonly float _minPitch;
        private readonly float _maxPitch;
        private readonly double _shotInterval;

        private double _nextShotAt;

        public float Yaw { get; private set; }
        public float Pitch { get; private set; }

        public TurretModel(
            float yawLimit,
            float minPitch,
            float maxPitch,
            float shotsPerSecond,
            float initialPitch = 12)
        {
            if (shotsPerSecond <= 0 || minPitch > maxPitch)
            {
                throw new ArgumentOutOfRangeException(nameof(shotsPerSecond));
            }

            _yawLimit = Mathf.Abs(yawLimit);
            _minPitch = minPitch;
            _maxPitch = maxPitch;
            _shotInterval = 1.0 / shotsPerSecond;

            Pitch = Mathf.Clamp(initialPitch, minPitch, maxPitch);
        }

        public void Aim(Vector2 angleDelta)
        {
            Yaw = Mathf.Clamp(Yaw + angleDelta.x, -_yawLimit, _yawLimit);
            Pitch = Mathf.Clamp(Pitch - angleDelta.y, _minPitch, _maxPitch);
        }

        public bool TryFire(double sessionTime)
        {
            if (sessionTime < _nextShotAt)
            {
                return false;
            }

            _nextShotAt = sessionTime + _shotInterval;
            return true;
        }
    }
}
