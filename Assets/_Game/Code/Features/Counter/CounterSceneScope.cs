using _Game.Code.Features.Counter.View;
using _Game.Code.Features.Counter.Models;
using VContainer;
using VContainer.Unity;

namespace _Game.Code.Features.Counter
{
    public sealed class CounterSceneScope : LifetimeScope
    {
        protected override void Configure(IContainerBuilder builder)
        {
            builder.Register<CounterModel>(Lifetime.Scoped);

            builder.RegisterComponentInHierarchy<CounterButton>();
            builder.RegisterComponentInHierarchy<CounterDisplay>();
        }
    }
}
