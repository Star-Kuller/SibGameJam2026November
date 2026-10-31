using UnityEngine;

namespace _Game.Code.Core.Diagnostics
{
    public sealed class SceneGreeting : MonoBehaviour
    {
        private void Start()
        {
            Debug.Log("Привет из тестовой сцены!", this);
        }
    }
}
