using System;
using System.Collections.Generic;
using System.Linq;
using R3;
using UnityEngine;

namespace _Game.Code.Features.Survivors.Models
{
    public sealed class ColonyModel : IDisposable
    {
        private static readonly string[] Names =
        {
            "Михаил", "Илья", "Лев", "Павел", "Иван", "Борис",
            "Виктор", "Роман", "Марк", "Алексей", "Сергей", "Денис"
        };

        private readonly int _startingResidents;
        private readonly int _capacity;
        private readonly List<SurvivorModel> _people = new();
        private readonly IReadOnlyList<SurvivorModel> _readOnlyPeople;
        private readonly ReactiveProperty<int> _revision = new(0);
        private int _nextId;

        public IReadOnlyList<SurvivorModel> People => _readOnlyPeople;
        public int Inside => _people.Count(person => person.IsResident && person.State == SurvivorState.Safe);
        public int WaitingForExit => _people.Count(person => person.State == SurvivorState.WaitingToLeave);
        public int Total => _people.Count(person => person.IsResident && person.IsAlive);
        public int Outside => Total - Inside - WaitingForExit;
        public int Rescued { get; private set; }
        public int Lost => _people.Count(person => person.State == SurvivorState.Dead);
        public int FriendlyFireDeaths => _people.Count(person => person.DeathCause == SurvivorDeathCause.FriendlyFire);
        public bool CanDispatch => Inside > 0 && Outside == 0 && WaitingForExit == 0;
        public bool HasCapacity => Total < _capacity;
        public Observable<int> Changed => _revision;

        public ColonyModel(int startingResidents, int capacity)
        {
            if (startingResidents <= 0 || capacity < startingResidents)
            {
                throw new ArgumentOutOfRangeException(nameof(startingResidents));
            }

            _startingResidents = startingResidents;
            _capacity = capacity;
            _readOnlyPeople = _people.AsReadOnly();
            Reset();
        }

        public void Reset()
        {
            UnsubscribePeople();
            _people.Clear();
            _nextId = 0;
            Rescued = 0;

            for (var index = 0; index < _startingResidents; index++)
            {
                var id = ++_nextId;
                Add(new SurvivorModel(id, NameFor(id)));
            }

            Notify();
        }

        public SurvivorModel CreateRefugee(Vector3[] route)
        {
            var id = ++_nextId;
            var person = new SurvivorModel(id, NameFor(id), false, route);
            Add(person);
            Notify();
            return person;
        }

        public SurvivorModel Find(int id)
        {
            return _people.Find(person => person.EntityId == id);
        }

        public bool Dispatch(int id, Vector3[] route, bool waitForExit)
        {
            return CanDispatch && Find(id)?.StartExpedition(route, waitForExit) == true;
        }

        public bool Admit(SurvivorModel person)
        {
            if (!_people.Contains(person) || !person.IsResident && !HasCapacity)
            {
                return false;
            }

            var refugee = !person.IsResident;

            if (!person.ConfirmArrival())
            {
                return false;
            }

            if (refugee)
            {
                Rescued++;
            }

            Notify();
            return true;
        }

        public void Dispose()
        {
            UnsubscribePeople();
            _revision.Dispose();
        }

        private void Add(SurvivorModel person)
        {
            _people.Add(person);
            person.StateChanged += Notify;
        }

        private void UnsubscribePeople()
        {
            foreach (var person in _people)
            {
                person.StateChanged -= Notify;
            }
        }

        private static string NameFor(int id)
        {
            var name = Names[(id - 1) % Names.Length];
            return id <= Names.Length ? name : name + " " + id;
        }

        private void Notify()
        {
            _revision.Value++;
        }
    }
}
