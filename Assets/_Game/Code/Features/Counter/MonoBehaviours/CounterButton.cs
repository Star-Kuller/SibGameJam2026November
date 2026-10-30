using _Game.Code.Features.Counter.Controllers;
using UnityEngine;
using UnityEngine.UI;
using VContainer;

namespace _Game.Code.Features.Counter.MonoBehaviours
{
    [RequireComponent(typeof(Button))]
    [DisallowMultipleComponent]
    public sealed class CounterButton : MonoBehaviour
    {
        private Button _button;
        private CounterController _controller;

        [Inject]
        public void Construct(CounterController controller)
        {
            _controller = controller;
        }

        private void Awake()
        {
            _button = GetComponent<Button>();
        }

        private void Start()
        {
            _button.onClick.AddListener(OnClick);
        }

        private void OnDestroy()
        {
            if (_button != null)
                _button.onClick.RemoveListener(OnClick);
        }

        private void OnClick()
        {
            _controller.Increment();
        }
    }
}