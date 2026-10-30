using _Game.Code.Features.Counter.Controllers;
using _Game.Code.Features.Counter.MonoBehaviours;
using _Game.Code.Features.Counter.Systems;
using VContainer;
using VContainer.Unity;

namespace _Game.Code.Features.Counter
{
    public sealed class CounterSceneScope : LifetimeScope
    {
        protected override void Configure(IContainerBuilder builder)
        {
            builder.Register<CounterSystem>(Lifetime.Scoped);
            builder.Register<CounterController>(Lifetime.Scoped);

            builder.RegisterComponentInHierarchy<CounterButton>();
            builder.RegisterComponentInHierarchy<CounterDisplay>();
        }
    }
}