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
        public int WaitingForExit { get; private set; }
        public int Total => Inside + Outside + WaitingForExit;
        public int Rescued { get; private set; }
        public int Lost { get; private set; }
        public int FriendlyFireDeaths { get; private set; }
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
            Reset();
        }

        public void Reset()
        {
            Inside = _startingResidents;
            Outside = 0;
            WaitingForExit = 0;
            Rescued = 0;
            Lost = 0;
            FriendlyFireDeaths = 0;
            Notify();
        }

        public bool Dispatch(bool waitForExit = false)
        {
            if (!CanDispatch)
            {
                return false;
            }

            Inside--;
            if (waitForExit)
            {
                WaitingForExit++;
            }
            else
            {
                Outside++;
            }
            Notify();
            return true;
        }

        public void BeginExit()
        {
            if (WaitingForExit == 0)
            {
                return;
            }

            WaitingForExit--;
            Outside++;
            Notify();
        }

        public bool Admit(bool alreadyResident)
        {
            if (alreadyResident)
            {
                if (Outside == 0 && WaitingForExit == 0)
                {
                    return false;
                }

                if (WaitingForExit > 0)
                {
                    WaitingForExit--;
                }
                else
                {
                    Outside--;
                }
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
                if (Outside == 0 && WaitingForExit == 0)
                {
                    return;
                }

                if (WaitingForExit > 0)
                {
                    WaitingForExit--;
                }
                else
                {
                    Outside--;
                }
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
