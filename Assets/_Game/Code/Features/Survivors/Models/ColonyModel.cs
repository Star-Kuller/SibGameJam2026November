using System;
using R3;

namespace _Game.Code.Features.Survivors.Models
{
    public sealed class ColonyModel : IDisposable
    {
        private readonly int _startingResidents;
        private readonly int _capacity;
        private readonly ReactiveProperty<int> _revision = new(0);

        public int Inside { get; private set; }
        public int Outside { get; private set; }
        public int Total => Inside + Outside;
        public int Rescued { get; private set; }
        public int Lost { get; private set; }
        public int FriendlyFireDeaths { get; private set; }
        public bool CanDispatch => Inside > 0 && Outside == 0;
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
            Reset();
        }

        public void Reset()
        {
            Inside = _startingResidents;
            Outside = 0;
            Rescued = 0;
            Lost = 0;
            FriendlyFireDeaths = 0;
            Notify();
        }

        public bool Dispatch()
        {
            if (!CanDispatch)
            {
                return false;
            }

            Inside--;
            Outside++;
            Notify();
            return true;
        }

        public bool Admit(bool alreadyResident)
        {
            if (alreadyResident)
            {
                if (Outside == 0)
                {
                    return false;
                }

                Outside--;
            }
            else
            {
                if (Total >= _capacity)
                {
                    return false;
                }

                Rescued++;
            }

            Inside++;
            Notify();
            return true;
        }

        public void RecordDeath(bool resident, bool friendlyFire)
        {
            if (resident)
            {
                if (Outside == 0)
                {
                    return;
                }

                Outside--;
            }

            Lost++;

            if (friendlyFire)
            {
                FriendlyFireDeaths++;
            }

            Notify();
        }

        public void Dispose()
        {
            _revision.Dispose();
        }

        private void Notify()
        {
            _revision.Value++;
        }
    }
}
