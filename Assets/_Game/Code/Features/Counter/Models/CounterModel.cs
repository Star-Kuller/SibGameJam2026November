using System;

namespace _Game.Code.Features.Counter.Models
{
    public sealed class CounterModel
    {
        public int Count { get; private set; }

        public event Action<int> CountChanged;

        public void Increment()
        {
            Count++;
            CountChanged?.Invoke(Count);
        }
    }
}
