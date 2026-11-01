using System;
using UnityEngine;
using UnityEngine.UIElements;

namespace _Game.Code.Features.GateEncounter.View
{
    [RequireComponent(typeof(UIDocument))]
    [DefaultExecutionOrder(200)]
    public abstract class DocumentSectionView : MonoBehaviour
    {
        protected VisualElement Root { get; private set; }

        private void OnEnable()
        {
            Root = GetComponent<UIDocument>().rootVisualElement;
            BindElements();
        }

        private void OnDisable()
        {
            UnbindElements();
            Root = null;
        }

        protected T Element<T>(string name) where T : VisualElement
        {
            var element = Root.Q<T>(name);

            if (element == null)
            {
                throw new InvalidOperationException($"UI element missing: {name}");
            }

            return element;
        }

        protected abstract void BindElements();
        protected abstract void UnbindElements();
    }
}
