using System;
using System.Collections.Generic;
using System.Text;
using _Game.Code.Features.GateEncounter.View;
using _Game.Code.Features.Gates.Models;
using _Game.Code.Features.Survivors.Models;
using R3;
using UnityEngine.UIElements;

namespace _Game.Code.Features.Survivors.View
{
    public sealed class PopulationHudView : DocumentSectionView
    {
        private ColonyModel _colony;
        private GateModel _gate;
        private IDisposable _populationBinding;
        private IDisposable _gateBinding;

        private Label _population;
        private Label _people;
        private Button _dispatch;
        private Button _recall;
        private string _peopleText = "За воротами никого нет.";

        public event Action DispatchRequested;
        public event Action RecallRequested;

        public void Bind(ColonyModel colony, GateModel gate)
        {
            _colony = colony;
            _gate = gate;

            if (Root != null)
            {
                Subscribe();
            }
        }

        public void ShowPeople(IReadOnlyList<SurvivorView> people, float patience)
        {
            if (Root != null)
            {
                Element<VisualElement>("people-panel").style.display = people.Count > 0
                    ? DisplayStyle.Flex
                    : DisplayStyle.None;
            }

            var text = new StringBuilder();

            foreach (var view in people)
            {
                var person = view.Model;
                text.Append(person.Name).Append("  ·  ");

                switch (person.State)
                {
                    case SurvivorState.WaitingToLeave:
                        text.Append("Ждёт открытия ворот внутри");
                        break;
                    case SurvivorState.GoingOut:
                        text.Append("Идёт за ресурсами");
                        break;

                    case SurvivorState.Working:
                        text.Append("Собирает груз");
                        break;

                    case SurvivorState.Returning:
                        text.Append("Возвращается");
                        break;

                    case SurvivorState.Waiting:
                        text.Append("Ждёт открытия ворот");

                        if (!person.IsResident)
                        {
                            var remaining = Math.Max(0, (int)Math.Ceiling(patience - person.WaitingTime));
                            text.Append(" · ").Append(remaining).Append(" с");
                        }

                        break;

                    case SurvivorState.Entering:
                        text.Append("Входит в общину");
                        break;
                }

                if (person.IsResident && person.CargoMetal + person.CargoRounds > 0)
                {
                    text.Append(" · ").Append(person.CargoMetal).Append(" мет. / ")
                        .Append(person.CargoRounds).Append(" патр.");
                }

                text.AppendLine();
            }

            var next = text.Length == 0 ? "За воротами никого нет." : text.ToString().TrimEnd();

            if (_peopleText == next)
            {
                return;
            }

            _peopleText = next;

            if (_people != null)
            {
                _people.text = next;
            }
        }

        protected override void BindElements()
        {
            _population = Element<Label>("population");
            _people = Element<Label>("people-list");
            _dispatch = Element<Button>("dispatch-person");
            _recall = Element<Button>("recall-people");
            _people.text = _peopleText;

            _dispatch.clicked += OnDispatch;
            _recall.clicked += OnRecall;
            Subscribe();
        }

        protected override void UnbindElements()
        {
            Unsubscribe();
            _dispatch.clicked -= OnDispatch;
            _recall.clicked -= OnRecall;
            _people = null;
        }

        private void Subscribe()
        {
            Unsubscribe();

            if (_colony != null && _gate != null)
            {
                _populationBinding = _colony.Changed.Subscribe(ShowPopulation);
                _gateBinding = _gate.StateChanged.Subscribe(ShowGateState);
            }
        }

        private void Unsubscribe()
        {
            _populationBinding?.Dispose();
            _gateBinding?.Dispose();
            _populationBinding = null;
            _gateBinding = null;
        }

        private void ShowPopulation(int revision)
        {
            _population.text = $"ЛЮДИ  {_colony.Total} / СВОБОДНЫ  {_colony.Inside} / СНАРУЖИ  {_colony.Outside}";
            RefreshButtons();
        }

        private void ShowGateState(GateState state)
        {
            RefreshButtons();
        }

        private void RefreshButtons()
        {
            _dispatch.SetEnabled(_colony.CanDispatch);
            _dispatch.tooltip = "1 житель, груз 12 металла и 30 патронов. При закрытых воротах ждёт выхода внутри.";
            _recall.SetEnabled(_colony.Outside + _colony.WaitingForExit > 0);
        }

        private void OnDispatch()
        {
            DispatchRequested?.Invoke();
        }

        private void OnRecall()
        {
            RecallRequested?.Invoke();
        }
    }
}
