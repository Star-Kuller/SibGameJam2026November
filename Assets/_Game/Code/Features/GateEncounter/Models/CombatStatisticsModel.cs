using System;
using R3;

namespace _Game.Code.Features.GateEncounter.Models
{
    public sealed class CombatStatisticsModel : IDisposable
    {
        private readonly ReactiveProperty<int> _kills = new(0);

        public int Kills => _kills.Value;
        public int Shots { get; private set; }
        public Observable<int> KillsChanged => _kills;

        public void Reset()
        {
            Shots = 0;
            _kills.Value = 0;
        }

        public void RecordShot()
        {
            Shots++;
        }

        public void RecordKill()
        {
            _kills.Value++;
        }

        public void Dispose()
        {
            _kills.Dispose();
        }
    }
}
