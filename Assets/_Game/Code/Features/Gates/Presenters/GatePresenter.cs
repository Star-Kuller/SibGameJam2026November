using System;
using _Game.Code.Features.Audio;
using _Game.Code.Features.GateEncounter;
using _Game.Code.Features.GateEncounter.Models;
using _Game.Code.Features.GateEncounter.View;
using _Game.Code.Features.Gates.Models;
using _Game.Code.Features.Gates.View;
using UnityEngine;
using VContainer.Unity;

namespace _Game.Code.Features.Gates.Presenters
{
    public sealed class GatePresenter : IInitializable, IDisposable
    {
        private readonly GatePrototypeSettings _settings;
        private readonly GateEncounterModel _session;
        private readonly GateModel _model;
        private readonly GateWorldView _world;
        private readonly GateControlsView _controls;
        private readonly GateAudio _audio;
        private readonly GateAudioEvents _sounds;
        private readonly NotificationView _notifications;

        private bool _toggleRequested;
        private bool _criticalNotified;

        public GatePresenter(
            GatePrototypeSettings settings,
            GateEncounterModel session,
            GateModel model,
            GateWorldView world,
            GateControlsView controls,
            GateAudio audio,
            GateAudioEvents sounds,
            NotificationView notifications)
        {
            _settings = settings;
            _session = session;
            _model = model;
            _world = world;
            _controls = controls;
            _audio = audio;
            _sounds = sounds;
            _notifications = notifications;
        }

        public void Initialize()
        {
            _controls.Bind(_model);
            _controls.ToggleRequested += RequestToggle;
            Reset();
        }

        public void Reset()
        {
            _toggleRequested = false;
            _criticalNotified = false;
            _model.Reset();
            _world.Render(0, false);
        }

        public void Tick(float seconds, bool pressed)
        {
            if (_toggleRequested || pressed)
            {
                _toggleRequested = false;

                if (_model.Toggle())
                {
                    Debug.Log($"[Last Shift] GateState={_model.State}; Time={_session.ElapsedTime:F2}");

                    var sound = _model.State == GateState.Opening
                        ? _sounds.gateOpen
                        : _sounds.gateClose;

                    _audio.Play(sound, new Vector3(0, 2, _settings.gatePlane));
                }
            }

            _model.AdvanceTransition(seconds / Mathf.Max(0.01f, _settings.gateTransitionDuration));
            _world.Render(_model.Openness, _model.CanPass);

            if (_model.IsCritical && !_criticalNotified)
            {
                _criticalNotified = true;
                _notifications.Show("Ворота повреждены! Прочность ниже 30%.", true);
                _audio.Play(_sounds.gateCritical, Vector3.zero, true);
            }
        }

        public void Dispose()
        {
            _controls.ToggleRequested -= RequestToggle;
        }

        private void RequestToggle()
        {
            if (_session.State == SessionState.Running)
            {
                _toggleRequested = true;
            }
        }
    }
}
