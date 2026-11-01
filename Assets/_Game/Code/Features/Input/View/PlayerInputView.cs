using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;

namespace _Game.Code.Features.Input.View
{
    public readonly struct PlayerInputFrame
    {
        public readonly Vector2 Look;
        public readonly bool LookIsRate;
        public readonly bool FireHeld;
        public readonly bool PausePressed;
        public readonly bool GatePressed;
        public readonly bool RecallPressed;
        public readonly bool CursorPressed;

        public PlayerInputFrame(
            Vector2 look,
            bool lookIsRate,
            bool fireHeld,
            bool pausePressed,
            bool gatePressed,
            bool recallPressed,
            bool cursorPressed)
        {
            Look = look;
            LookIsRate = lookIsRate;
            FireHeld = fireHeld;
            PausePressed = pausePressed;
            GatePressed = gatePressed;
            RecallPressed = recallPressed;
            CursorPressed = cursorPressed;
        }
    }

    public sealed class PlayerInputView : MonoBehaviour
    {
        [SerializeField] private InputActionReference lookAction;
        [SerializeField] private InputActionReference fireAction;
        [SerializeField] private InputActionReference pauseAction;
        [SerializeField] private InputActionReference gateAction;
        [SerializeField] private InputActionReference recallAction;
        [SerializeField] private InputActionReference cursorAction;

        private readonly List<InputAction> _ownedActions = new();
        private InputAction _look;
        private InputAction _fire;
        private InputAction _pause;
        private InputAction _gate;
        private InputAction _recall;
        private InputAction _cursor;

        public bool HasFocus => Application.isFocused;
        public InputActionAsset Asset => Require(lookAction).actionMap.asset;

        public event Action<bool> FocusChanged;
        public event Action BindingsChanged;

        private void OnEnable()
        {
            _look = Require(lookAction);
            _fire = Require(fireAction);
            _pause = Require(pauseAction);
            _gate = Require(gateAction);
            _recall = Require(recallAction);
            _cursor = Require(cursorAction);

            EnableIfNeeded(_look);
            EnableIfNeeded(_fire);
            EnableIfNeeded(_pause);
            EnableIfNeeded(_gate);
            EnableIfNeeded(_recall);
            EnableIfNeeded(_cursor);

            InputSystem.onActionChange += OnActionChange;
        }

        private void OnDisable()
        {
            InputSystem.onActionChange -= OnActionChange;

            foreach (var action in _ownedActions)
            {
                action.Disable();
            }

            _ownedActions.Clear();
        }

        private void OnApplicationFocus(bool focused)
        {
            FocusChanged?.Invoke(focused);
        }

        public PlayerInputFrame Read()
        {
            if (!isActiveAndEnabled)
            {
                return default;
            }

            var look = _look.ReadValue<Vector2>();
            var control = _look.activeControl;
            var rate = control != null && control is not DeltaControl;

            return new PlayerInputFrame(
                look,
                rate,
                _fire.IsPressed(),
                _pause.WasPressedThisFrame(),
                _gate.WasPressedThisFrame(),
                _recall.WasPressedThisFrame(),
                _cursor.WasPressedThisFrame());
        }

        public InputAction GetAction(string name)
        {
            return Asset.FindAction("Player/" + name, true);
        }

        public string BindingLabel(string name)
        {
            var action = GetAction(name);
            var options = InputBinding.DisplayStringOptions.DontIncludeInteractions;
            var label = action.GetBindingDisplayString(options, "Keyboard&Mouse");

            return string.IsNullOrEmpty(label)
                ? action.GetBindingDisplayString(options)
                : label;
        }

        public string ControlsHint()
        {
            return $"Огонь: {BindingLabel("Attack")}    Ворота: {BindingLabel("Gate")}    "
                + $"Отзыв: {BindingLabel("Recall")}    Курсор: {BindingLabel("CommandCursor")}    "
                + $"Пауза: {BindingLabel("Pause")}";
        }

        public void CaptureCursor(bool captured)
        {
            Cursor.lockState = captured ? CursorLockMode.Locked : CursorLockMode.None;
            Cursor.visible = !captured;
        }

        private static InputAction Require(InputActionReference reference)
        {
            if (reference == null || reference.action == null)
            {
                throw new InvalidOperationException("Assign every PlayerInputView action reference.");
            }

            return reference.action;
        }

        private void EnableIfNeeded(InputAction action)
        {
            if (!action.enabled)
            {
                action.Enable();
                _ownedActions.Add(action);
            }
        }

        private void OnActionChange(object source, InputActionChange change)
        {
            if (change != InputActionChange.BoundControlsChanged)
            {
                return;
            }

            var asset = Asset;
            var matchesAsset = source is InputActionAsset changedAsset && changedAsset == asset;
            var matchesAction = source is InputAction action && action.actionMap?.asset == asset;
            var matchesMap = source is InputActionMap map && map.asset == asset;

            if (matchesAsset || matchesAction || matchesMap)
            {
                BindingsChanged?.Invoke();
            }
        }
    }
}
