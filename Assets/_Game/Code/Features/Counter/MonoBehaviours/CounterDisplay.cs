using _Game.Code.Features.Counter.Controllers;
using TMPro;
using UnityEngine;
using VContainer;

namespace _Game.Code.Features.Counter.MonoBehaviours
{
    [RequireComponent(typeof(TextMeshProUGUI))]
    [DisallowMultipleComponent]
    public sealed class CounterDisplay : MonoBehaviour
    {
        private TextMeshProUGUI _label;
        private CounterController _controller;

        [Inject]
        public void Construct(CounterController controller)
        {
            _controller = controller;
        }

        private void Awake()
        {
            _label = GetComponent<TextMeshProUGUI>();
        }

        private void Start()
        {
            _controller.CountChanged += OnCountChanged;
            OnCountChanged(_controller.Count);
        }

        private void OnDestroy()
        {
            if (_controller != null)
                _controller.CountChanged -= OnCountChanged;
        }

        private void OnCountChanged(int count)
        {
            _label.text = count.ToString();
        }
    }
}