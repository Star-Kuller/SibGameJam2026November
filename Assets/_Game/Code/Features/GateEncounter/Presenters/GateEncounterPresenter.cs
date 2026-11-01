using System;
using _Game.Code.Features.Audio;
using _Game.Code.Features.GateEncounter.Models;
using _Game.Code.Features.GateEncounter.View;
using _Game.Code.Features.Gates.Models;
using _Game.Code.Features.Gates.Presenters;
using _Game.Code.Features.Input.View;
using _Game.Code.Features.Survivors.Models;
using _Game.Code.Features.Survivors.Presenters;
using _Game.Code.Features.Turret.Presenters;
using _Game.Code.Features.Zombies.Presenters;
using R3;
using UnityEngine;
using VContainer.Unity;

namespace _Game.Code.Features.GateEncounter.Presenters
{
    // Owns session lifecycle and the ordered simulation loop, not individual game rules.
    public sealed class GateEncounterPresenter : IInitializable, ITickable, IDisposable
    {
        private readonly GatePrototypeSettings _settings;
        private readonly GateEncounterModel _session;
        private readonly GateModel _gate;
        private readonly ColonyModel _colony;
        private readonly CombatStatisticsModel _statistics;
        private readonly StockpileModel _stockpile;
        private readonly PlayerInputView _input;
        private readonly SessionMenuView _menu;
        private readonly GateHudView _hud;
        private readonly NotificationView _notifications;
        private readonly GatePresenter _gates;
        private readonly SurvivorPresenter _survivors;
        private readonly ZombiePresenter _zombies;
        private readonly TurretPresenter _turret;
        private readonly OutcomePresenter _outcome;
        private readonly GateAudio _audio;
        private readonly GateAudioEvents _sounds;

        private IDisposable _stateBinding;
        private bool _startRequested;
        private bool _mainMenuRequested;
        private bool _commandMode;
        private bool _hadFocus;
        private bool _disposed;

        public GateEncounterModel Session => _session;

        public GateEncounterPresenter(
            GatePrototypeSettings settings,
            GateEncounterModel session,
            GateModel gate,
            ColonyModel colony,
            CombatStatisticsModel statistics,
            StockpileModel stockpile,
            PlayerInputView input,
            SessionMenuView menu,
            GateHudView hud,
            NotificationView notifications,
            GatePresenter gates,
            SurvivorPresenter survivors,
            ZombiePresenter zombies,
            TurretPresenter turret,
            OutcomePresenter outcome,
            GateAudio audio,
            GateAudioEvents sounds)
        {
            _settings = settings;
            _session = session;
            _gate = gate;
            _colony = colony;
            _statistics = statistics;
            _stockpile = stockpile;
            _input = input;
            _menu = menu;
            _hud = hud;
            _notifications = notifications;
            _gates = gates;
            _survivors = survivors;
            _zombies = zombies;
            _turret = turret;
            _outcome = outcome;
            _audio = audio;
            _sounds = sounds;
        }

        public void Initialize()
        {
            _menu.Bind(_session, _statistics, _colony, _stockpile);
            _hud.Bind(_gate, _statistics);

            _menu.StartRequested += RequestStart;
            _menu.RestartRequested += RequestStart;
            _menu.ResumeRequested += Resume;
            _menu.MainMenuRequested += RequestMainMenu;
            _menu.QuitRequested += Quit;
            _input.FocusChanged += OnFocusChanged;

            _hadFocus = _input.HasFocus;
            _stateBinding = _session.StateChanged.Subscribe(OnStateChanged);
        }

        public void Tick()
        {
            if (_disposed)
            {
                return;
            }

            HandleMenuRequests();
            HandleFocus();

            var input = _input.Read();
            HandlePause(input.PausePressed);
            _audio.ReleaseFinished();

            if (_session.State != SessionState.Running || !_input.HasFocus)
            {
                return;
            }

            if (input.CursorPressed)
            {
                _commandMode = !_commandMode;
                _turret.ResetInputSafety();
                UpdateCursor();
            }

            var seconds = Mathf.Max(0, Time.deltaTime);
            _session.AdvanceTime(seconds);

            _gates.Tick(seconds, input.GatePressed);

            if (input.RecallPressed)
            {
                _survivors.RecallAll();
            }

            _survivors.Tick(seconds, _session.ElapsedTime);
            _zombies.Move(seconds);

            if (_session.State == SessionState.Running)
            {
                // Movement precedes raycasts; shots precede pending enemy attacks.
                _turret.Tick(seconds, input, !_commandMode);
                _zombies.ResolveAttacks();
                _zombies.Cleanup(seconds);
                _outcome.Evaluate();
            }

            _hud.ShowTime(_settings.shiftDuration - _session.ElapsedTime, _zombies.LivingCount);
        }

        public void Dispose()
        {
            if (_disposed)
            {
                return;
            }

            _disposed = true;
            _stateBinding?.Dispose();

            _menu.StartRequested -= RequestStart;
            _menu.RestartRequested -= RequestStart;
            _menu.ResumeRequested -= Resume;
            _menu.MainMenuRequested -= RequestMainMenu;
            _menu.QuitRequested -= Quit;
            _input.FocusChanged -= OnFocusChanged;

            _audio.Dispose();
            _survivors.Clear();
            _zombies.Clear();
            _input.CaptureCursor(false);

            _session.Dispose();
            _gate.Dispose();
            _colony.Dispose();
            _statistics.Dispose();
            _stockpile.Dispose();
        }

        private void HandleMenuRequests()
        {
            if (_mainMenuRequested)
            {
                _mainMenuRequested = false;
                _startRequested = false;
                _session.ReturnToMenu();
                _survivors.Clear();
                _zombies.Clear();
                _audio.Dispose();
                _notifications.Clear();
            }

            if (_startRequested)
            {
                _startRequested = false;
                StartSession();
            }
        }

        private void StartSession()
        {
            _session.ReturnToMenu();
            _audio.Dispose();
            _commandMode = false;

            _statistics.Reset();
            _stockpile.Reset();
            _gates.Reset();
            _survivors.Reset();
            _zombies.Reset();
            _turret.Reset();
            _notifications.Clear();
            _hud.ShowTime(_settings.shiftDuration, 0);

            _session.Start();
            _audio.Play(_sounds.ambience, _turret.CameraPosition);

            if (!_input.HasFocus)
            {
                _session.Pause();
            }

            _notifications.Show("Вылазка: отправьте жителя за металлом и патронами. Груз нужно вернуть за ворота.");
        }

        private void HandleFocus()
        {
            if (_input.HasFocus != _hadFocus)
            {
                OnFocusChanged(_input.HasFocus);
            }
        }

        private void OnFocusChanged(bool focused)
        {
            _hadFocus = focused;
            _turret.ResetInputSafety();

            if (!focused)
            {
                _session.Pause();
            }

            UpdateCursor();
        }

        private void HandlePause(bool pressed)
        {
            if (!pressed || !_input.HasFocus)
            {
                return;
            }

            if (_menu.IsSettingsOpen)
            {
                _menu.CloseSettings();
                return;
            }

            if (_session.State == SessionState.Running)
            {
                _session.Pause();
            }
            else if (_session.State == SessionState.Paused)
            {
                Resume();
            }
        }

        private void Resume()
        {
            if (!_input.HasFocus)
            {
                return;
            }

            _commandMode = false;
            _session.Resume();
            PlayButton();
        }

        private void OnStateChanged(SessionState state)
        {
            Debug.Log($"[Last Shift] State={state}; Time={_session.ElapsedTime:F2}; "
                + $"GateHP={_gate.Health}; Residents={_colony.Total}; "
                + $"Kills={_statistics.Kills}; Shots={_statistics.Shots}; Reason={_session.ResultReason}");

            var running = state == SessionState.Running;

            _turret.ResetInputSafety();
            _survivors.SetPaused(!running);
            _zombies.SetPaused(!running);
            _audio.SetPaused(!running);
            _hud.SetAimMode(running && !_commandMode);
            UpdateCursor();

            if (state == SessionState.Won || state == SessionState.Lost)
            {
                _audio.Dispose();
                _audio.Play(state == SessionState.Won ? _sounds.win : _sounds.lose, Vector3.zero, true);

                if (state == SessionState.Lost && _gate.Health == 0)
                {
                    _audio.Play(_sounds.destruction, Vector3.zero, true);
                }
            }
        }

        private void UpdateCursor()
        {
            var capture = _session.State == SessionState.Running && _input.HasFocus && !_commandMode;
            _input.CaptureCursor(capture);
        }

        private void RequestStart()
        {
            PlayButton();
            _startRequested = true;
        }

        private void RequestMainMenu()
        {
            PlayButton();
            _mainMenuRequested = true;
        }

        private void PlayButton()
        {
            _audio.Play(_sounds.button, Vector3.zero, true);
        }

        private static void Quit()
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }
    }
}
