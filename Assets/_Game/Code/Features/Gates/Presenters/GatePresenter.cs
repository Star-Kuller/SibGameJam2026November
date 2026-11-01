using System;
using _Game.Code.Features.Audio;
using _Game.Code.Features.GateEncounter;
using _Game.Code.Features.GateEncounter.Models;
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

        private bool _toggleRequested;

        public GatePresenter(
            GatePrototypeSettings settings,
            GateEncounterModel session,
            GateModel model,
            GateWorldView world,
            GateControlsView controls,
            GateAudio audio,
            GateAudioEvents sounds)
        {
            _settings = settings;
            _session = session;
            _model = model;
            _world = world;
            _controls = controls;
            _audio = audio;
            _sounds = sounds;
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
                    var sound = _model.State == GateState.Opening
                        ? _sounds.gateOpen
                        : _sounds.gateClose;

                    _audio.Play(sound, new Vector3(0, 2, _settings.gatePlane));
                }
            }

            _model.AdvanceTransition(seconds / Mathf.Max(0.01f, _settings.gateTransitionDuration));
            _world.Render(_model.Openness, _model.CanPass);
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
