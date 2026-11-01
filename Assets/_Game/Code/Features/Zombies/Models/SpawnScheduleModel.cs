using System;

namespace _Game.Code.Features.Zombies.Models
{
    public sealed class SpawnScheduleModel
    {
        private readonly double _firstSpawn;
        private readonly double _interval;
        private readonly double _duration;
        private readonly double[] _waveTimes;

        private double _nextRegularSpawn;
        private int _nextWave;

        public int Pending { get; private set; }
        public int WaveNumber { get; private set; }
        public double NextWaveTime => _nextWave < _waveTimes.Length
            ? _waveTimes[_nextWave]
            : double.PositiveInfinity;

        public SpawnScheduleModel(double firstSpawn, double interval, double duration)
        {
            _firstSpawn = firstSpawn;
            _interval = Math.Max(0.1, interval);
            _duration = duration;
            _waveTimes = new[] { duration / 3, duration * 2 / 3, duration * 5 / 6 };
            Reset();
        }

        public void Reset()
        {
            _nextRegularSpawn = _firstSpawn;
            _nextWave = 0;
            Pending = 0;
            WaveNumber = 0;
        }

        public bool Advance(double time, bool hasCapacity)
        {
            if (time >= _duration)
            {
                Pending = 0;
                return false;
            }

            if (_nextWave < _waveTimes.Length && time >= _waveTimes[_nextWave])
            {
                Pending += 4 + _nextWave;
                _nextWave++;
                WaveNumber = _nextWave;
                _nextRegularSpawn = time + _interval;
                return true;
            }

            if (time >= _nextRegularSpawn)
            {
                _nextRegularSpawn = time + _interval;

                if (hasCapacity && Pending == 0)
                {
                    Pending = 1;
                }
            }

            return false;
        }

        public void ConsumeSpawn()
        {
            Pending = Math.Max(0, Pending - 1);
        }
    }
}
