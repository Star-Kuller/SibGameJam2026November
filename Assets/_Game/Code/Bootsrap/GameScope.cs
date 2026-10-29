using _Game.Code.Core.Diagnostics;
using VContainer;
using VContainer.Unity;

namespace _Game.Code.Bootsrap
{
    public sealed class GameScope : LifetimeScope
    {
        protected override void Configure(IContainerBuilder builder)
        {
            builder.RegisterEntryPoint<GameLifecycle>();
        }
    }
}