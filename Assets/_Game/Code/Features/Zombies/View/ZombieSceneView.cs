using UnityEngine;

namespace _Game.Code.Features.Zombies.View
{
    public sealed class ZombieSceneView : MonoBehaviour
    {
        [SerializeField] private ZombieView prefab;
        [SerializeField] private Transform runtime;
        [SerializeField] private ZombieRoute[] routes;

        public ZombieRoute[] Routes => routes;

        public ZombieView Create()
        {
            return Instantiate(prefab, runtime);
        }

        public void Remove(ZombieView view)
        {
            view.gameObject.SetActive(false);
            Destroy(view.gameObject);
        }
    }
}
