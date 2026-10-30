using System;
using _Game.Code.Features.Counter.Systems;
using UnityEngine;

namespace _Game.Code.Features.Counter.Controllers
{
    public sealed class CounterController
    {
        private readonly CounterSystem _system;

        public event Action<int> CountChanged;

        public CounterController(CounterSystem system)
        {
            _system = system;
        }

        public int Count => _system.Count;

        public void Increment()
        {
            _system.Increment();
            CountChanged?.Invoke(_system.Count);
            
            Debug.Log($"[demo] {_system.Count} increment");
        }
    }
}