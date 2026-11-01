using System;
using _Game.Code.Features.GateEncounter.View;
using _Game.Code.Features.Gates.Models;
using R3;
using UnityEngine.UIElements;

namespace _Game.Code.Features.Gates.View
{
    public sealed class GateControlsView : DocumentSectionView
    {
        private GateModel _model;
        private IDisposable _binding;
        private Button _toggle;
        private Label _state;

        public event Action ToggleRequested;

        public void Bind(GateModel model)
        {
            _model = model;

            if (Root != null)
            {
                Subscribe();
            }
        }

        protected override void BindElements()
        {
            _toggle = Element<Button>("toggle-gate");
            _state = Element<Label>("gate-state");
            _toggle.clicked += OnToggle;
            Subscribe();
        }

        protected override void UnbindElements()
        {
            _binding?.Dispose();
            _binding = null;
            _toggle.clicked -= OnToggle;
        }

        private void Subscribe()
        {
            _binding?.Dispose();

            if (_model != null)
            {
                _binding = _model.StateChanged.Subscribe(ShowState);
            }
        }

        private void ShowState(GateState state)
        {
            switch (state)
            {
                case GateState.Closed:
                    _state.text = "ЗАКРЫТЫ";
                    _toggle.text = "Открыть ворота";
                    break;

                case GateState.Opening:
                    _state.text = "ОТКРЫВАЮТСЯ";
                    _toggle.text = "Открытие…";
                    break;

                case GateState.Open:
                    _state.text = "ОТКРЫТЫ · РИСК ПРОРЫВА";
                    _toggle.text = "Закрыть ворота";
                    break;

                case GateState.Closing:
                    _state.text = "ЗАКРЫВАЮТСЯ";
                    _toggle.text = "Закрытие…";
                    break;
            }

            _state.EnableInClassList("danger-text", state == GateState.Open);
            _toggle.SetEnabled(!_model.IsMoving);
        }

        private void OnToggle()
        {
            ToggleRequested?.Invoke();
        }
    }
}
