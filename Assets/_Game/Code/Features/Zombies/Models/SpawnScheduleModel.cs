using System;

namespace _Game.Code.Features.Zombies.Models
{
    public sealed class SpawnScheduleModel
    {
        private readonly double _firstSpawn;
        private readonly double _interval;
        private readonly double _duration;
        private readonly double[] _waveTimes = { 180, 390, 630 };
        private readonly int[] _waveSizes = { 12, 16, 20 };

        private double _nextRegularSpawn;
        private int _nextWave;
        private int _regularGroups;

        public int Pending { get; private set; }
        public int WaveNumber { get; private set; }
        public bool WaveActive { get; private set; }
        public bool PendingIsWave { get; private set; }
        public bool AllWavesSpawned => _nextWave == _waveTimes.Length && Pending == 0;
        public double NextWaveTime => _nextWave < _waveTimes.Length
            ? _waveTimes[_nextWave]
            : double.PositiveInfinity;

        public SpawnScheduleModel(double firstSpawn, double interval, double duration)
        {
            _firstSpawn = firstSpawn;
            _interval = Math.Max(0.1, interval);
            _duration = duration;
            Reset();
        }

        public void Reset()
        {
            _nextRegularSpawn = _firstSpawn;
            _nextWave = 0;
            _regularGroups = 0;
            Pending = 0;
            WaveNumber = 0;
            WaveActive = false;
            PendingIsWave = false;
        }

        public bool Advance(double time, bool hasCapacity, int livingWaveZombies = 0)
        {
            if (WaveActive && Pending == 0 && livingWaveZombies == 0)
            {
                WaveActive = false;
                PendingIsWave = false;
                _nextRegularSpawn = time + _interval;
            }

            if (_nextWave < _waveTimes.Length && time >= _waveTimes[_nextWave])
            {
                // Scheduled waves keep their start time even if an earlier wave is still alive.
                // Unspawned wave enemies stay committed; only pending regular groups are replaced.
                Pending = (PendingIsWave ? Pending : 0) + _waveSizes[_nextWave];
                _nextWave++;
                WaveNumber = _nextWave;
                WaveActive = true;
                PendingIsWave = true;
                return true;
            }

            if (time >= _duration)
            {
                if (!PendingIsWave)
                {
                    Pending = 0;
                }

                return false;
            }

            if (WaveActive || Pending > 0 || time < _nextRegularSpawn || !hasCapacity)
            {
                return false;
            }

            PendingIsWave = false;
            Pending = _regularGroups == 0 ? 2 : 3 + (_regularGroups - 1) % 3;
            _regularGroups++;
            _nextRegularSpawn = _regularGroups == 1 ? Math.Max(60, time + _interval) : time + _interval;
            return false;
        }

        public void ConsumeSpawn()
        {
            Pending = Math.Max(0, Pending - 1);
        }
    }
}
