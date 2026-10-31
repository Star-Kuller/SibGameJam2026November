using _Game.Code.Features.Counter.Models;
using UnityEngine;
using UnityEngine.UI;
using VContainer;

namespace _Game.Code.Features.Counter.View
{
    [RequireComponent(typeof(Button))]
    [DisallowMultipleComponent]
    public sealed class CounterButton : MonoBehaviour
    {
        private Button _button;
        private CounterModel _model;

        [Inject]
        public void Construct(CounterModel model)
        {
            _model = model;
        }

        private void Awake()
        {
            _button = GetComponent<Button>();
        }

        private void OnEnable()
        {
            _button.onClick.AddListener(OnClick);
        }

        private void OnDisable()
        {
            if (_button != null)
                _button.onClick.RemoveListener(OnClick);
        }

        private void OnClick()
        {
            _model.Increment();
            Debug.Log($"[demo] {_model.Count} increment");
        }
    }
}
