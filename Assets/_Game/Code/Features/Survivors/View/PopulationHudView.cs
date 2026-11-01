using System;
using System.Collections.Generic;
using _Game.Code.Features.GateEncounter.View;
using _Game.Code.Features.Gates.Models;
using _Game.Code.Features.Survivors.Models;
using R3;
using UnityEngine.UIElements;

namespace _Game.Code.Features.Survivors.View
{
    public sealed class PopulationHudView : DocumentSectionView
    {
        private sealed class PersonRow
        {
            public VisualElement Root;
            public Label Status;
            public Button Recall;
        }

        private ColonyModel _colony;
        private GateModel _gate;
        private IDisposable _populationBinding;
        private IDisposable _gateBinding;
        private readonly Dictionary<int, PersonRow> _rows = new();

        private Label _population;
        private VisualElement _people;
        private VisualElement _picker;
        private VisualElement _choices;
        private Label _selection;
        private Button _dispatch;
        private Button _recall;
        private Button _confirm;
        private Button _cancel;
        private int _selectedId;

        public event Action<int> DispatchRequested;
        public event Action RecallRequested;
        public event Action<int> RecallPersonRequested;

        public void Bind(ColonyModel colony, GateModel gate)
        {
            _colony = colony;
            _gate = gate;

            if (Root != null)
            {
                Subscribe();
            }
        }

        public void ShowPeople(float patience)
        {
            if (Root == null || _colony == null)
            {
                return;
            }

            foreach (var row in _rows.Values)
            {
                row.Root.style.display = DisplayStyle.None;
            }

            var visibleCount = 0;

            foreach (var person in _colony.People)
            {
                if (!person.IsVisible)
                {
                    continue;
                }

                visibleCount++;
                var row = GetRow(person.EntityId);
                row.Root.style.display = DisplayStyle.Flex;
                row.Status.text = Describe(person, patience);
                row.Recall.SetEnabled(person.IsResident && CanRecall(person.State));
                row.Recall.style.display = person.IsResident ? DisplayStyle.Flex : DisplayStyle.None;
            }

            Element<VisualElement>("people-panel").style.display = visibleCount > 0
                ? DisplayStyle.Flex
                : DisplayStyle.None;
        }

        public void CancelSelection()
        {
            _selectedId = 0;

            if (_picker != null)
            {
                _picker.style.display = DisplayStyle.None;
            }
        }

        protected override void BindElements()
        {
            _population = Element<Label>("population");
            _people = Element<VisualElement>("people-list");
            _dispatch = Element<Button>("dispatch-person");
            _recall = Element<Button>("recall-people");
            _picker = Element<VisualElement>("resident-picker");
            _choices = Element<VisualElement>("resident-choices");
            _selection = Element<Label>("resident-selection");
            _confirm = Element<Button>("confirm-expedition");
            _cancel = Element<Button>("cancel-expedition");

            _rows.Clear();
            _dispatch.clicked += ShowPicker;
            _recall.clicked += OnRecall;
            _confirm.clicked += ConfirmSelection;
            _cancel.clicked += CancelSelection;
            Root.RegisterCallback<PointerDownEvent>(OnPointerDown);
            Root.RegisterCallback<KeyDownEvent>(OnKeyDown);
            CancelSelection();
            Subscribe();
        }

        protected override void UnbindElements()
        {
            Unsubscribe();
            _dispatch.clicked -= ShowPicker;
            _recall.clicked -= OnRecall;
            _confirm.clicked -= ConfirmSelection;
            _cancel.clicked -= CancelSelection;
            Root.UnregisterCallback<PointerDownEvent>(OnPointerDown);
            Root.UnregisterCallback<KeyDownEvent>(OnKeyDown);
            _rows.Clear();
        }

        private void Subscribe()
        {
            Unsubscribe();

            if (_colony != null && _gate != null)
            {
                _populationBinding = _colony.Changed.Subscribe(ShowPopulation);
                _gateBinding = _gate.StateChanged.Subscribe(_ => RefreshButtons());
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

        private void RefreshButtons()
        {
            _dispatch.SetEnabled(_colony.CanDispatch);
            _dispatch.tooltip = _colony.CanDispatch
                ? "1 житель · 62 с без ожидания ворот · 12 металла и 30 патронов после входа."
                : "Нужен свободный житель. Одновременно доступна одна вылазка.";
            _recall.SetEnabled(_colony.Outside + _colony.WaitingForExit > 0);

            var person = _colony.Find(_selectedId);
            _confirm.SetEnabled(_colony.CanDispatch && person?.State == SurvivorState.Safe);
        }

        private void ShowPicker()
        {
            _choices.Clear();
            _selectedId = 0;
            _selection.text = "Выберите свободного жителя";

            foreach (var person in _colony.People)
            {
                if (!person.IsResident || person.State != SurvivorState.Safe)
                {
                    continue;
                }

                var id = person.EntityId;
                var choice = new Button(() => SelectPerson(id)) { text = person.Name + " · свободен" };
                choice.AddToClassList("resident-choice");
                _choices.Add(choice);
            }

            _picker.style.display = DisplayStyle.Flex;
            RefreshButtons();
        }

        private void SelectPerson(int id)
        {
            _selectedId = id;
            _selection.text = _colony.Find(id).Name + " · вылазка к месту сбора";
            RefreshButtons();
        }

        private void ConfirmSelection()
        {
            var id = _selectedId;
            CancelSelection();
            DispatchRequested?.Invoke(id);
        }

        private PersonRow GetRow(int id)
        {
            if (_rows.TryGetValue(id, out var row))
            {
                return row;
            }

            var root = new VisualElement();
            root.AddToClassList("person-row");
            var status = new Label();
            status.AddToClassList("person-status");
            var recall = new Button(() => RecallPersonRequested?.Invoke(id)) { text = "Отозвать" };
            root.Add(status);
            root.Add(recall);
            _people.Add(root);
            row = new PersonRow { Root = root, Status = status, Recall = recall };
            _rows.Add(id, row);
            return row;
        }

        private static bool CanRecall(SurvivorState state)
        {
            return state == SurvivorState.WaitingToLeave
                || state == SurvivorState.GoingOut
                || state == SurvivorState.Working;
        }

        private static string Describe(SurvivorModel person, float patience)
        {
            var state = person.State switch
            {
                SurvivorState.WaitingToLeave => "Ждёт выхода внутри",
                SurvivorState.GoingOut => "Идёт за ресурсами",
                SurvivorState.Working => "Собирает груз",
                SurvivorState.Returning => person.IsResident ? "Возвращается" : "Идёт к воротам",
                SurvivorState.Waiting => "Ждёт у ворот",
                SurvivorState.Entering => "Входит в общину",
                _ => string.Empty
            };

            var text = person.Name + " · " + state;

            if (!person.IsResident && person.State == SurvivorState.Waiting)
            {
                text += $" · {Math.Max(0, (int)Math.Ceiling(patience - person.WaitingTime))} с";
            }

            if (person.CargoMetal + person.CargoRounds > 0)
            {
                text += $"\nГруз: {person.CargoMetal} металла / {person.CargoRounds} патронов";
            }

            return text;
        }

        private void OnPointerDown(PointerDownEvent evt)
        {
            if (evt.button == 1)
            {
                CancelSelection();
            }
        }

        private void OnKeyDown(KeyDownEvent evt)
        {
            if (evt.keyCode == UnityEngine.KeyCode.Escape)
            {
                CancelSelection();
            }
        }

        private void OnRecall()
        {
            RecallRequested?.Invoke();
        }
    }
}
