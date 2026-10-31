using _Game.Code.Features.Counter.Models;
using TMPro;
using UnityEngine;
using VContainer;

namespace _Game.Code.Features.Counter.View
{
    [RequireComponent(typeof(TextMeshProUGUI))]
    [DisallowMultipleComponent]
    public sealed class CounterDisplay : MonoBehaviour
    {
        private TextMeshProUGUI _label;
        private CounterModel _model;

        [Inject]
        public void Construct(CounterModel model)
        {
            if (_model != null)
                _model.CountChanged -= ShowCount;

            _model = model;

            if (isActiveAndEnabled)
                Bind();
        }

        private void OnEnable()
        {
            if (_model != null)
                Bind();
        }

        private void OnDisable()
        {
            if (_model != null)
                _model.CountChanged -= ShowCount;
        }

        private void Bind()
        {
            _model.CountChanged -= ShowCount;
            _model.CountChanged += ShowCount;
            ShowCount(_model.Count);
        }

        private void ShowCount(int count)
        {
            if (_label == null)
                _label = GetComponent<TextMeshProUGUI>();

            _label.text = count.ToString();
        }
    }
}
