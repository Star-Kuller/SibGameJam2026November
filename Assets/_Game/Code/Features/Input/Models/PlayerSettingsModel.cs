using System;

namespace _Game.Code.Features.Input.Models
{
    public sealed class PlayerSettingsModel
    {
        public float MouseSensitivity { get; private set; }
        public float Volume { get; private set; } = 0.7f;

        public PlayerSettingsModel(float sensitivity)
        {
            SetSensitivity(sensitivity);
        }

        public void SetSensitivity(float value)
        {
            MouseSensitivity = Math.Clamp(value, 0.02f, 1);
        }

        public void SetVolume(float value)
        {
            Volume = Math.Clamp(value, 0, 1);
        }
    }
}
