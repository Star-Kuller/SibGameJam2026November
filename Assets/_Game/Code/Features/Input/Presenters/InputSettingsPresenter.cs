using System;
using System.Collections.Generic;
using _Game.Code.Features.Audio;
using _Game.Code.Features.GateEncounter;
using _Game.Code.Features.GateEncounter.View;
using _Game.Code.Features.Input.Models;
using _Game.Code.Features.Input.View;
using UnityEngine;
using UnityEngine.InputSystem;
using VContainer.Unity;

namespace _Game.Code.Features.Input.Presenters
{
    public sealed class InputSettingsPresenter : IInitializable, IDisposable
    {
        private const string BindingsKey = "LastShift.Bindings.v1";
        private const string SensitivityKey = "LastShift.MouseSensitivity";
        private const string VolumeKey = "LastShift.MasterVolume";
        private const string CctvKey = "LastShift.CctvEnabled";

        private static readonly string[] Actions = { "Attack", "Pause", "Gate", "Recall", "CommandCursor" };

        private readonly GatePrototypeSettings _defaults;
        private readonly PlayerSettingsModel _model;
        private readonly PlayerInputView _input;
        private readonly InputSettingsView _view;
        private readonly SessionMenuView _menu;
        private readonly GateAudio _audio;

        private InputActionRebindingExtensions.RebindingOperation _operation;
        private InputAction _reboundAction;
        private bool _wasEnabled;
        private int _bindingIndex;
        private string _previousOverride;

        public InputSettingsPresenter(
            GatePrototypeSettings defaults,
            PlayerSettingsModel model,
            PlayerInputView input,
            InputSettingsView view,
            SessionMenuView menu,
            GateAudio audio)
        {
            _defaults = defaults;
            _model = model;
            _input = input;
            _view = view;
            _menu = menu;
            _audio = audio;
        }

        public void Initialize()
        {
            LoadPreferences();

            _view.RebindRequested += BeginRebind;
            _view.CancelRequested += CancelRebind;
            _view.ResetRequested += Reset;
            _view.SensitivityChanged += SetSensitivity;
            _view.VolumeChanged += SetVolume;
            _view.CctvChanged += SetCctv;
            _menu.SettingsClosed += CancelRebind;
            _input.BindingsChanged += RefreshBindings;
            _input.FocusChanged += OnFocusChanged;

            RefreshBindings();
            _view.ShowValues(_model.MouseSensitivity, _model.Volume, _model.CctvEnabled);
            _audio.SetVolume(_model.Volume);
        }

        public void Dispose()
        {
            _view.RebindRequested -= BeginRebind;
            _view.CancelRequested -= CancelRebind;
            _view.ResetRequested -= Reset;
            _view.SensitivityChanged -= SetSensitivity;
            _view.VolumeChanged -= SetVolume;
            _view.CctvChanged -= SetCctv;
            _menu.SettingsClosed -= CancelRebind;
            _input.BindingsChanged -= RefreshBindings;
            _input.FocusChanged -= OnFocusChanged;
            CancelRebind();
        }

        private void LoadPreferences()
        {
            _model.SetSensitivity(PlayerPrefs.GetFloat(SensitivityKey, _defaults.mouseSensitivity));
            _model.SetVolume(PlayerPrefs.GetFloat(VolumeKey, 0.7f));
            _model.SetCctvEnabled(PlayerPrefs.GetInt(CctvKey, 1) != 0);

            if (!PlayerPrefs.HasKey(BindingsKey))
            {
                return;
            }

            try
            {
                _input.Asset.LoadBindingOverridesFromJson(PlayerPrefs.GetString(BindingsKey));
            }
            catch (Exception exception)
            {
                Debug.LogWarning("Saved control bindings could not be read: " + exception.Message);
                PlayerPrefs.DeleteKey(BindingsKey);
            }
        }

        private void BeginRebind(string name)
        {
            CancelRebind();

            var action = _input.GetAction(name);
            var bindingIndex = FindKeyboardBinding(action);

            if (bindingIndex < 0)
            {
                _view.ShowRebinding(false, "У этого действия нет назначения клавиатуры и мыши.");
                return;
            }

            _reboundAction = action;
            _bindingIndex = bindingIndex;
            _previousOverride = action.bindings[bindingIndex].overridePath;
            _wasEnabled = action.enabled;
            action.Disable();

            _view.ShowRebinding(true, "Нажмите клавишу или кнопку мыши. Esc — отмена.");

            _operation = action.PerformInteractiveRebinding(bindingIndex)
                .WithExpectedControlType("Button")
                .WithControlsExcluding("<Pointer>/position")
                .WithControlsExcluding("<Pointer>/delta")
                .WithControlsExcluding("<Gamepad>")
                .WithCancelingThrough("<Keyboard>/escape")
                .OnComplete(OnRebindCompleted)
                .OnCancel(OnRebindCancelled);

            _operation.Start();
        }

        private void OnRebindCompleted(InputActionRebindingExtensions.RebindingOperation operation)
        {
            if (HasConflict())
            {
                if (_previousOverride == null)
                {
                    _reboundAction.RemoveBindingOverride(_bindingIndex);
                }
                else
                {
                    _reboundAction.ApplyBindingOverride(_bindingIndex, _previousOverride);
                }

                ReleaseOperation();
                RefreshBindings();
                _view.ShowRebinding(false, "Эта клавиша уже занята другим действием. Выберите другую.");
                return;
            }

            ReleaseOperation();
            PlayerPrefs.SetString(BindingsKey, _input.Asset.SaveBindingOverridesAsJson());
            PlayerPrefs.Save();
            RefreshBindings();
            _view.ShowRebinding(false, "Назначение сохранено.");
        }

        private void OnRebindCancelled(InputActionRebindingExtensions.RebindingOperation operation)
        {
            ReleaseOperation();
            _view.ShowRebinding(false, "Переназначение отменено.");
        }

        private void CancelRebind()
        {
            if (_operation != null)
            {
                _operation.Cancel();
            }
        }

        private void OnFocusChanged(bool focused)
        {
            if (!focused)
            {
                CancelRebind();
            }
        }

        private bool HasConflict()
        {
            var path = _reboundAction.bindings[_bindingIndex].effectivePath;

            foreach (var name in Actions)
            {
                var action = _input.GetAction(name);

                if (action == _reboundAction)
                {
                    continue;
                }

                foreach (var binding in action.bindings)
                {
                    if (string.Equals(binding.effectivePath, path, StringComparison.OrdinalIgnoreCase))
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        private void ReleaseOperation()
        {
            var operation = _operation;
            _operation = null;
            operation?.Dispose();

            if (_wasEnabled)
            {
                _reboundAction?.Enable();
            }

            _reboundAction = null;
            _wasEnabled = false;
        }

        private void RefreshBindings()
        {
            var labels = new Dictionary<string, string>();

            foreach (var action in Actions)
            {
                labels.Add(action, _input.BindingLabel(action));
            }

            _view.ShowBindings(labels);
        }

        private void Reset()
        {
            CancelRebind();
            _input.Asset.RemoveAllBindingOverrides();
            PlayerPrefs.DeleteKey(BindingsKey);

            SetSensitivity(_defaults.mouseSensitivity);
            SetVolume(0.7f);
            SetCctv(true);
            _view.ShowValues(_model.MouseSensitivity, _model.Volume, _model.CctvEnabled);
            _view.ShowRebinding(false, "Стандартные назначения восстановлены.");
            RefreshBindings();
        }

        private void SetSensitivity(float value)
        {
            _model.SetSensitivity(value);
            PlayerPrefs.SetFloat(SensitivityKey, _model.MouseSensitivity);
            PlayerPrefs.Save();
        }

        private void SetVolume(float value)
        {
            _model.SetVolume(value);
            _audio.SetVolume(_model.Volume);
            PlayerPrefs.SetFloat(VolumeKey, _model.Volume);
            PlayerPrefs.Save();
        }

        private void SetCctv(bool enabled)
        {
            _model.SetCctvEnabled(enabled);
            PlayerPrefs.SetInt(CctvKey, enabled ? 1 : 0);
            PlayerPrefs.Save();
        }

        private static int FindKeyboardBinding(InputAction action)
        {
            for (var index = 0; index < action.bindings.Count; index++)
            {
                var binding = action.bindings[index];

                if (!binding.isComposite && !binding.isPartOfComposite
                    && binding.groups.Contains("Keyboard&Mouse"))
                {
                    return index;
                }
            }

            return -1;
        }
    }
}
