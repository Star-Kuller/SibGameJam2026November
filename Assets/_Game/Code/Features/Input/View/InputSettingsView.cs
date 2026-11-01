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
        private Button _reset;
        private Button _cancel;
        private Label _message;
        private float _sensitivityValue = 0.12f;
        private float _volumeValue = 0.7f;
        private string _messageText = string.Empty;
        private bool _rebinding;

        public event Action<string> RebindRequested;
        public event Action CancelRequested;
        public event Action ResetRequested;
        public event Action<float> SensitivityChanged;
        public event Action<float> VolumeChanged;

        public void ShowValues(float sensitivity, float volume)
        {
            _sensitivityValue = sensitivity;
            _volumeValue = volume;
            _sensitivity?.SetValueWithoutNotify(sensitivity);
            _volume?.SetValueWithoutNotify(volume);
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
            _reset = Element<Button>("reset-controls");
            _cancel = Element<Button>("cancel-rebind");
            _message = Element<Label>("rebind-message");

            foreach (var action in Actions)
            {
                var button = Element<Button>("bind-" + action);
                button.userData = action;
                button.RegisterCallback<ClickEvent>(OnRebindClicked);
                _buttons.Add(action, button);
            }

            _sensitivity.RegisterValueChangedCallback(OnSensitivityChanged);
            _volume.RegisterValueChangedCallback(OnVolumeChanged);
            _reset.clicked += OnReset;
            _cancel.clicked += OnCancel;

            ShowValues(_sensitivityValue, _volumeValue);

            if (_labels != null)
            {
                ShowBindings(_labels);
            }

            RenderRebinding();
        }

        protected override void UnbindElements()
        {
            foreach (var button in _buttons.Values)
            {
                button.UnregisterCallback<ClickEvent>(OnRebindClicked);
            }

            _buttons.Clear();
            _sensitivity.UnregisterValueChangedCallback(OnSensitivityChanged);
            _volume.UnregisterValueChangedCallback(OnVolumeChanged);
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

        private void OnRebindClicked(ClickEvent evt)
        {
            var button = (Button)evt.currentTarget;
            RebindRequested?.Invoke((string)button.userData);
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

        private void OnCancel()
        {
            CancelRequested?.Invoke();
        }
    }
}
