namespace _Game.Code.Features.Counter.Systems
{
    public sealed class CounterSystem
    {
        public int Count { get; private set; }

        public void Increment()
        {
            Count++;
        }
    }
}