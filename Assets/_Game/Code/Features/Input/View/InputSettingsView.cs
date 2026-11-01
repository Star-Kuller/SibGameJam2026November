using System;
using System.Collections.Generic;
using _Game.Code.Features.GateEncounter.View;
using UnityEngine.UIElements;

namespace _Game.Code.Features.Input.View
{
    public sealed class InputSettingsView : DocumentSectionView
    {
        private static readonly string[] Actions = { "Attack", "Pause", "Gate", "Recall", "CommandCursor" };

        private readonly Dictionary<string, Button> _buttons = new();
        private IReadOnlyDictionary<string, string> _labels;
        private Slider _sensitivity;
        private Slider _volume;
        private Toggle _cctv;
        private Button _reset;
        private Button _cancel;
        private Label _message;
        private float _sensitivityValue = 0.12f;
        private float _volumeValue = 0.7f;
        private string _messageText = string.Empty;
        private bool _rebinding;
        private bool _cctvValue = true;

        public event Action<string> RebindRequested;
        public event Action CancelRequested;
        public event Action ResetRequested;
        public event Action<float> SensitivityChanged;
        public event Action<float> VolumeChanged;
        public event Action<bool> CctvChanged;

        public void ShowValues(float sensitivity, float volume, bool cctv)
        {
            _sensitivityValue = sensitivity;
            _volumeValue = volume;
            _cctvValue = cctv;
            _sensitivity?.SetValueWithoutNotify(sensitivity);
            _volume?.SetValueWithoutNotify(volume);
            _cctv?.SetValueWithoutNotify(cctv);
        }

        public void ShowBindings(IReadOnlyDictionary<string, string> labels)
        {
            _labels = labels;

            foreach (var pair in _buttons)
            {
                pair.Value.text = labels[pair.Key];
            }
        }

        public void ShowRebinding(bool active, string message)
        {
            _rebinding = active;
            _messageText = message;
            RenderRebinding();
        }

        protected override void BindElements()
        {
            _sensitivity = Element<Slider>("mouse-sensitivity");
            _volume = Element<Slider>("master-volume");
            _cctv = Element<Toggle>("cctv-enabled");
            _reset = Element<Button>("reset-controls");
            _cancel = Element<Button>("cancel-rebind");
            _message = Element<Label>("rebind-message");

            foreach (var action in Actions)
            {
                var button = Element<Button>("bind-" + action);
                _buttons.Add(action, button);
            }

            _buttons["Attack"].clicked += OnFireRebind;
            _buttons["Pause"].clicked += OnPauseRebind;
            _buttons["Gate"].clicked += OnGateRebind;
            _buttons["Recall"].clicked += OnRecallRebind;
            _buttons["CommandCursor"].clicked += OnCursorRebind;
            _sensitivity.RegisterValueChangedCallback(OnSensitivityChanged);
            _volume.RegisterValueChangedCallback(OnVolumeChanged);
            _cctv.RegisterValueChangedCallback(OnCctvChanged);
            _reset.clicked += OnReset;
            _cancel.clicked += OnCancel;

            ShowValues(_sensitivityValue, _volumeValue, _cctvValue);

            if (_labels != null)
            {
                ShowBindings(_labels);
            }

            RenderRebinding();
        }

        protected override void UnbindElements()
        {
            _buttons["Attack"].clicked -= OnFireRebind;
            _buttons["Pause"].clicked -= OnPauseRebind;
            _buttons["Gate"].clicked -= OnGateRebind;
            _buttons["Recall"].clicked -= OnRecallRebind;
            _buttons["CommandCursor"].clicked -= OnCursorRebind;
            _buttons.Clear();
            _sensitivity.UnregisterValueChangedCallback(OnSensitivityChanged);
            _volume.UnregisterValueChangedCallback(OnVolumeChanged);
            _cctv.UnregisterValueChangedCallback(OnCctvChanged);
            _reset.clicked -= OnReset;
            _cancel.clicked -= OnCancel;

            _message = null;
            _cancel = null;
        }

        private void RenderRebinding()
        {
            if (_message == null)
            {
                return;
            }

            _message.text = _messageText;
            _cancel.style.display = _rebinding ? DisplayStyle.Flex : DisplayStyle.None;
            _reset.SetEnabled(!_rebinding);

            foreach (var button in _buttons.Values)
            {
                button.SetEnabled(!_rebinding);
            }
        }

        private void OnFireRebind()
        {
            RebindRequested?.Invoke("Attack");
        }

        private void OnPauseRebind()
        {
            RebindRequested?.Invoke("Pause");
        }

        private void OnGateRebind()
        {
            RebindRequested?.Invoke("Gate");
        }

        private void OnRecallRebind()
        {
            RebindRequested?.Invoke("Recall");
        }

        private void OnCursorRebind()
        {
            RebindRequested?.Invoke("CommandCursor");
        }

        private void OnSensitivityChanged(ChangeEvent<float> evt)
        {
            SensitivityChanged?.Invoke(evt.newValue);
        }

        private void OnVolumeChanged(ChangeEvent<float> evt)
        {
            VolumeChanged?.Invoke(evt.newValue);
        }

        private void OnReset()
        {
            ResetRequested?.Invoke();
        }

        private void OnCctvChanged(ChangeEvent<bool> evt)
        {
            CctvChanged?.Invoke(evt.newValue);
        }

        private void OnCancel()
        {
            CancelRequested?.Invoke();
        }
    }
}
