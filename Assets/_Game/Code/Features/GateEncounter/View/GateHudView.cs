using System;
using _Game.Code.Features.GateEncounter.Models;
using _Game.Code.Features.Gates.Models;
using R3;
using UnityEngine;
using UnityEngine.UIElements;

namespace _Game.Code.Features.GateEncounter.View
{
    public sealed class GateHudView : DocumentSectionView
    {
        private GateModel _gate;
        private CombatStatisticsModel _statistics;
        private IDisposable _healthBinding;
        private IDisposable _killsBinding;

        private Label _health;
        private VisualElement _healthFill;
        private Label _kills;
        private Label _clock;
        private Label _enemies;
        private Label _target;
        private Label _controls;
        private VisualElement _crosshair;
        private VisualElement _hitMarker;
        private string _controlsText = string.Empty;
        private string _commandHintText = string.Empty;
        private SessionState _sessionState;

        public void ShowSessionState(SessionState state)
        {
            _sessionState = state;
        }

        public void Bind(GateModel gate, CombatStatisticsModel statistics)
        {
            Unsubscribe();
            _gate = gate;
            _statistics = statistics;

            if (Root != null)
            {
                Subscribe();
            }
        }

        public void ShowTime(double remaining, int enemies)
        {
            if (_clock == null)
            {
                return;
            }

            var time = TimeSpan.FromSeconds(Math.Max(0, remaining));
            _clock.text = remaining > 0 ? $"{time:mm\\:ss}" : "ЗАЧИСТКА";
            _enemies.text = $"УГРОЗЫ  {enemies:D2}";
        }

        public void SetControlsHint(string text, string cursorBinding)
        {
            _controlsText = text;
            _commandHintText = cursorBinding + " — курсор для команд · мир продолжает жить";

            if (_controls != null)
            {
                _controls.text = text;
            }

            if (Root != null)
            {
                Element<Label>("command-caption").text = _commandHintText;
            }
        }

        public void SetAimMode(bool aiming)
        {
            if (Root != null)
            {
                Element<Label>("control-mode").text = _sessionState == SessionState.Paused
                    ? "ПАУЗА"
                    : aiming ? "ТУРЕЛЬ" : "КУРСОР";
            }

            if (_crosshair != null)
            {
                _crosshair.style.display = aiming ? DisplayStyle.Flex : DisplayStyle.None;
            }

            if (!aiming)
            {
                ShowTarget(string.Empty, false);
            }
        }

        public void SetHitMarker(bool visible, bool friendly = false)
        {
            if (_hitMarker == null)
            {
                return;
            }

            _hitMarker.style.display = visible ? DisplayStyle.Flex : DisplayStyle.None;
            _hitMarker.style.color = friendly
                ? new Color(0.95f, 0.36f, 0.29f)
                : new Color(0.9f, 0.76f, 0.44f);
        }

        public void ShowTarget(string name, bool friendly)
        {
            if (_target == null)
            {
                return;
            }

            _target.text = name;
            _target.style.color = friendly
                ? new Color(0.35f, 0.9f, 0.81f)
                : new Color(0.93f, 0.86f, 0.72f);
        }

        protected override void BindElements()
        {
            _health = Element<Label>("health");
            _healthFill = Element<VisualElement>("health-fill");
            _kills = Element<Label>("kills");
            _clock = Element<Label>("clock");
            _enemies = Element<Label>("enemies");
            _target = Element<Label>("target-name");
            _controls = Element<Label>("controls-hint");
            _crosshair = Element<VisualElement>("crosshair");
            _hitMarker = Element<VisualElement>("hit-marker");
            _controls.text = _controlsText;
            Element<Label>("command-caption").text = _commandHintText;
            Subscribe();
        }

        protected override void UnbindElements()
        {
            Unsubscribe();
        }

        private void Subscribe()
        {
            if (_gate == null || _statistics == null)
            {
                return;
            }

            Unsubscribe();
            _healthBinding = _gate.HealthChanged.Subscribe(ShowHealth);
            _killsBinding = _statistics.KillsChanged.Subscribe(ShowKills);
        }

        private void ShowHealth(int value)
        {
            _health.text = $"{value} / {_gate.MaximumHealth}";
            _healthFill.style.width = Length.Percent(100f * value / _gate.MaximumHealth);
            _healthFill.EnableInClassList("critical", _gate.IsCritical);
            Element<VisualElement>("gate-damage-icon").style.display = _gate.IsCritical
                ? DisplayStyle.Flex
                : DisplayStyle.None;
        }

        private void ShowKills(int value)
        {
            _kills.text = value.ToString("D3");
        }

        private void Unsubscribe()
        {
            _healthBinding?.Dispose();
            _killsBinding?.Dispose();
            _healthBinding = null;
            _killsBinding = null;
        }
    }
}
