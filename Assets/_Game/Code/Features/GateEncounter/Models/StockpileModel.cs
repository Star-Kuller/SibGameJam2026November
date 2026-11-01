using System;
using R3;

namespace _Game.Code.Features.GateEncounter.Models
{
    public sealed class StockpileModel : IDisposable
    {
        private readonly int _initialMetal;
        private readonly int _initialRounds;
        private readonly ReactiveProperty<int> _revision = new(0);

        public int Metal { get; private set; }
        public int Rounds { get; private set; }
        public int DeliveredMetal { get; private set; }
        public int DeliveredRounds { get; private set; }
        public int Deliveries { get; private set; }
        public bool HasRounds => Rounds > 0;
        public Observable<int> Changed => _revision;

        public StockpileModel(int metal, int rounds)
        {
            _initialMetal = Math.Max(0, metal);
            _initialRounds = Math.Max(0, rounds);
            Reset();
        }

        public void Reset()
        {
            Metal = _initialMetal;
            Rounds = _initialRounds;
            DeliveredMetal = 0;
            DeliveredRounds = 0;
            Deliveries = 0;
            _revision.Value++;
        }

        public bool TryUseRound()
        {
            if (!HasRounds)
            {
                return false;
            }

            Rounds--;
            _revision.Value++;
            return true;
        }

        public void Deliver(int metal, int rounds)
        {
            metal = Math.Max(0, metal);
            rounds = Math.Max(0, rounds);

            if (metal == 0 && rounds == 0)
            {
                return;
            }

            Metal += metal;
            Rounds += rounds;
            DeliveredMetal += metal;
            DeliveredRounds += rounds;
            Deliveries++;
            _revision.Value++;
        }

        public void Dispose()
        {
            _revision.Dispose();
        }
    }
}
