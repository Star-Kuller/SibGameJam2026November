using System;
using _Game.Code.Features.Audio;
using _Game.Code.Features.GateEncounter;
using _Game.Code.Features.GateEncounter.Models;
using _Game.Code.Features.GateEncounter.View;
using _Game.Code.Features.Input.Models;
using _Game.Code.Features.Input.View;
using _Game.Code.Features.Survivors.Presenters;
using _Game.Code.Features.Turret.Models;
using _Game.Code.Features.Turret.View;
using _Game.Code.Features.Zombies.Presenters;
using UnityEngine;
using VContainer.Unity;

namespace _Game.Code.Features.Turret.Presenters
{
    public sealed class TurretPresenter : IInitializable, IDisposable
    {
        private const int AimWarmupFrames = 2;

        private readonly GatePrototypeSettings _settings;
        private readonly PlayerSettingsModel _preferences;
        private readonly GateEncounterModel _session;
        private readonly CombatStatisticsModel _statistics;
        private readonly PlayerInputView _input;
        private readonly TurretView _view;
        private readonly GateHudView _hud;
        private readonly GateEncounterView _queries;
        private readonly ZombiePresenter _zombies;
        private readonly SurvivorPresenter _survivors;
        private readonly GateAudio _audio;
        private readonly GateAudioEvents _sounds;

        private TurretModel _model;
        private bool _waitForRelease = true;
        private int _warmupFrames;
        private float _hitMarkerRemaining;
        private bool _friendlyHit;

        public TurretModel Model => _model;

        public TurretPresenter(
            GatePrototypeSettings settings,
            PlayerSettingsModel preferences,
            GateEncounterModel session,
            CombatStatisticsModel statistics,
            PlayerInputView input,
            TurretView view,
            GateHudView hud,
            GateEncounterView queries,
            ZombiePresenter zombies,
            SurvivorPresenter survivors,
            GateAudio audio,
            GateAudioEvents sounds)
        {
            _settings = settings;
            _preferences = preferences;
            _session = session;
            _statistics = statistics;
            _input = input;
            _view = view;
            _hud = hud;
            _queries = queries;
            _zombies = zombies;
            _survivors = survivors;
            _audio = audio;
            _sounds = sounds;
        }

        public void Initialize()
        {
            _input.BindingsChanged += UpdateControlsHint;
            Reset();
            UpdateControlsHint();
        }

        public void Reset()
        {
            _model = new TurretModel(
                _settings.yawLimit,
                _settings.minPitch,
                _settings.maxPitch,
                _settings.shotsPerSecond,
                _settings.initialPitch);

            _view.ResetEffects();
            _view.RenderAim(_model);
            _hitMarkerRemaining = 0;
            _hud.SetHitMarker(false);
            ResetInputSafety();
        }

        public void ResetInputSafety()
        {
            _waitForRelease = true;
            _warmupFrames = AimWarmupFrames;
        }

        public void Tick(float seconds, PlayerInputFrame input, bool aiming)
        {
            _view.RenderEffects(seconds);
            _hitMarkerRemaining = Mathf.Max(0, _hitMarkerRemaining - seconds);
            _hud.SetHitMarker(_hitMarkerRemaining > 0, _friendlyHit);
            _hud.SetAimMode(aiming);

            if (!aiming)
            {
                ResetInputSafety();
                return;
            }

            if (!input.FireHeld)
            {
                _waitForRelease = false;
            }

            UpdateAiming(input, seconds);
            Physics.SyncTransforms();

            var target = _queries.QueryShot(_settings.range);
            ShowTarget(target);

            if (_waitForRelease || !input.FireHeld || !_model.TryFire(_session.ElapsedTime))
            {
                return;
            }

            Fire(target);
        }

        public void Dispose()
        {
            _input.BindingsChanged -= UpdateControlsHint;
        }

        private void UpdateAiming(PlayerInputFrame input, float seconds)
        {
            if (_warmupFrames > 0)
            {
                _warmupFrames--;
            }
            else
            {
                var sensitivity = input.LookIsRate
                    ? _settings.lookSpeed * seconds
                    : _preferences.MouseSensitivity;

                _model.Aim(input.Look * sensitivity);
            }

            _view.RenderAim(_model);
        }

        private void Fire(ShotTarget target)
        {
            _statistics.RecordShot();
            _view.ShowShot(target.Point);
            _audio.Play(_sounds.shot, _view.Camera.transform.position);

            var hit = false;
            _friendlyHit = target.Survivor != null;

            if (target.Survivor != null)
            {
                hit = _survivors.Kill(target.Survivor, true);
            }
            else if (target.Zombie != null)
            {
                hit = _zombies.ApplyShot(target.Zombie, target.Point);
            }

            if (hit)
            {
                _hitMarkerRemaining = 0.12f;
                _hud.SetHitMarker(true, _friendlyHit);
            }
        }

        private void ShowTarget(ShotTarget target)
        {
            if (target.Survivor != null)
            {
                _hud.ShowTarget("СВОЙ · " + target.Survivor.Model.Name, true);
            }
            else if (target.Zombie != null)
            {
                _hud.ShowTarget("ЗОМБИ", false);
            }
            else
            {
                _hud.ShowTarget(string.Empty, false);
            }
        }

        private void UpdateControlsHint()
        {
            _hud.SetControlsHint(_input.ControlsHint(), _input.BindingLabel("CommandCursor"));
        }
    }
}
