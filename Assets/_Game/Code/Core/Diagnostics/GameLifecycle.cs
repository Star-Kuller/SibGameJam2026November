using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;
using VContainer.Unity;

namespace _Game.Code.Core.Diagnostics
{
    public sealed class GameLifecycle :
        IInitializable,
        IAsyncStartable,
        IDisposable
    {
        public void Initialize()
        {
            Debug.Log("[GameScope] Initialized");
        }

        public async UniTask StartAsync(
            CancellationToken cancellationToken)
        {
            await UniTask.NextFrame(cancellationToken);

            Debug.Log("[GameScope] UniTask works!");
        }

        public void Dispose()
        {
            Debug.Log("[GameScope] Disposed");
        }
    }
}