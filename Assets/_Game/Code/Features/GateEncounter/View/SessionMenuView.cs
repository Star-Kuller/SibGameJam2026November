using System;
using System.Text;
using _Game.Code.Features.GateEncounter.Models;
using _Game.Code.Features.Survivors.Models;
using R3;
using _Game.Code.Features.Input.View;
using UnityEngine.UIElements;

namespace _Game.Code.Features.GateEncounter.View
{
    public sealed class SessionMenuView : DocumentSectionView, ISettingsPanel
    {
        private GateEncounterModel _session;
        private CombatStatisticsModel _statistics;
        private ColonyModel _colony;
        private StockpileModel _stockpile;
        private IDisposable _binding;

        private VisualElement _overlay;
        private VisualElement _home;
        private VisualElement _settings;
        private Label _title;
        private Label _subtitle;
        private Label _results;
        private Button _start;
        private Button _resume;
        private Button _restart;
        private Button _main;
        private Button _settingsButton;
        private Button _quit;
        private Button _settingsBack;

        public bool IsSettingsOpen { get; private set; }

        public event Action StartRequested;
        public event Action ResumeRequested;
        public event Action RestartRequested;
        public event Action MainMenuRequested;
        public event Action QuitRequested;
        public event Action SettingsClosed;

        public void Bind(
            GateEncounterModel session,
            CombatStatisticsModel statistics,
            ColonyModel colony,
            StockpileModel stockpile)
        {
            _session = session;
            _statistics = statistics;
            _colony = colony;
            _stockpile = stockpile;

            if (Root != null)
            {
                Subscribe();
            }
        }

        public void CloseSettings()
        {
            if (!IsSettingsOpen)
            {
                return;
            }

            IsSettingsOpen = false;
            _settings.style.display = DisplayStyle.None;
            _home.style.display = DisplayStyle.Flex;
            SettingsClosed?.Invoke();
            _settingsButton.Focus();
        }

        protected override void BindElements()
        {
            _overlay = Element<VisualElement>("menu-overlay");
            _home = Element<VisualElement>("menu-home");
            _settings = Element<VisualElement>("settings-panel");
            _title = Element<Label>("menu-title");
            _subtitle = Element<Label>("menu-subtitle");
            _results = Element<Label>("run-results");
            _start = Element<Button>("start");
            _resume = Element<Button>("resume");
            _restart = Element<Button>("restart");
            _main = Element<Button>("back-main");
            _settingsButton = Element<Button>("open-settings");
            _quit = Element<Button>("quit");
            _settingsBack = Element<Button>("settings-back");

            _start.clicked += OnStart;
            _resume.clicked += OnResume;
            _restart.clicked += OnRestart;
            _main.clicked += OnMainMenu;
            _settingsButton.clicked += OpenSettings;
            _quit.clicked += OnQuit;
            _settingsBack.clicked += CloseSettings;
            Subscribe();
        }

        protected override void UnbindElements()
        {
            _binding?.Dispose();
            _binding = null;

            _start.clicked -= OnStart;
            _resume.clicked -= OnResume;
            _restart.clicked -= OnRestart;
            _main.clicked -= OnMainMenu;
            _settingsButton.clicked -= OpenSettings;
            _quit.clicked -= OnQuit;
            _settingsBack.clicked -= CloseSettings;
        }

        private void Subscribe()
        {
            _binding?.Dispose();

            if (_session != null)
            {
                _binding = _session.StateChanged.Subscribe(ShowState);
            }
        }

        private void ShowState(SessionState state)
        {
            CloseSettings();
            var main = state == SessionState.MainMenu;
            var paused = state == SessionState.Paused;
            var running = state == SessionState.Running;
            _title.EnableInClassList("main-title", main);

            _overlay.style.display = main || running ? DisplayStyle.None : DisplayStyle.Flex;
            Element<VisualElement>("hud").style.display = main ? DisplayStyle.None : DisplayStyle.Flex;
            _start.style.display = main ? DisplayStyle.Flex : DisplayStyle.None;
            _resume.style.display = paused ? DisplayStyle.Flex : DisplayStyle.None;
            _restart.style.display = main || running ? DisplayStyle.None : DisplayStyle.Flex;
            _main.style.display = main || running ? DisplayStyle.None : DisplayStyle.Flex;
            _quit.style.display = paused ? DisplayStyle.None : DisplayStyle.Flex;
            _results.style.display = main || running || paused ? DisplayStyle.None : DisplayStyle.Flex;

            switch (state)
            {
                case SessionState.MainMenu:
                    _title.text = "ПОСЛЕДНЯЯ СМЕНА";
                    _subtitle.text = string.Empty;
                    break;

                case SessionState.Paused:
                    _title.text = "Пауза";
                    _subtitle.text = string.Empty;
                    break;

                case SessionState.Won:
                    _title.text = "Смена завершена";
                    _subtitle.text = _session.ResultReason;
                    break;

                case SessionState.Lost:
                    _title.text = "ПОЗИЦИЯ ПОТЕРЯНА";
                    _subtitle.text = _session.ResultReason;
                    break;
            }

            _subtitle.style.display = string.IsNullOrEmpty(_subtitle.text)
                ? DisplayStyle.None
                : DisplayStyle.Flex;

            if (!main && !running)
            {
                var time = TimeSpan.FromSeconds(_session.ElapsedTime);
                _results.text = $"Время  {time:mm\\:ss}     Уничтожено  {_statistics.Kills}\n"
                    + $"Живы  {_colony.Total}     Спасено  {_colony.Rescued}     Погибло  {_colony.Lost}\n"
                    + $"Дружественный огонь  {_colony.FriendlyFireDeaths}     Выстрелов  {_statistics.Shots}\n"
                    + $"Доставлено: {_stockpile.DeliveredMetal} металла, {_stockpile.DeliveredRounds} патронов";

                if (!paused)
                {
                    var losses = new StringBuilder();

                    foreach (var person in _colony.People)
                    {
                        if (person.State != SurvivorState.Dead)
                        {
                            continue;
                        }

                        var cause = person.DeathCause == SurvivorDeathCause.FriendlyFire
                            ? "ваш выстрел"
                            : "атака зомби";
                        losses.Append('\n').Append(person.Name).Append(" — ").Append(cause);
                    }

                    _results.text += losses.ToString();
                }
            }

            if (main)
            {
                _start.schedule.Execute(_start.Focus);
            }
            else if (paused)
            {
                _resume.schedule.Execute(_resume.Focus);
            }
            else if (!running)
            {
                _restart.schedule.Execute(_restart.Focus);
            }
        }

        private void OpenSettings()
        {
            IsSettingsOpen = true;
            _home.style.display = DisplayStyle.None;
            _settings.style.display = DisplayStyle.Flex;
            _settingsBack.Focus();
        }

        private void OnStart()
        {
            StartRequested?.Invoke();
        }

        private void OnResume()
        {
            ResumeRequested?.Invoke();
        }

        private void OnRestart()
        {
            RestartRequested?.Invoke();
        }

        private void OnMainMenu()
        {
            MainMenuRequested?.Invoke();
        }

        private void OnQuit()
        {
            QuitRequested?.Invoke();
        }
    }
}
