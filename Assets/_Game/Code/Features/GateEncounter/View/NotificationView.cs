using UnityEngine.UIElements;

namespace _Game.Code.Features.GateEncounter.View
{
    public sealed class NotificationView : DocumentSectionView
    {
        private Label _message;
        private IVisualElementScheduledItem _hide;
        private string _pending = string.Empty;
        private bool _danger;

        public void Show(string message, bool danger = false)
        {
            _pending = message;
            _danger = danger;
            Render();
        }

        public void Clear()
        {
            _pending = string.Empty;
            _hide?.Pause();
            Render();
        }

        protected override void BindElements()
        {
            _message = Element<Label>("notification");
            Render();
        }

        protected override void UnbindElements()
        {
            _hide?.Pause();
            _message = null;
        }

        private void Render()
        {
            if (_message == null)
            {
                return;
            }

            _hide?.Pause();
            _message.text = _pending;
            _message.EnableInClassList("danger", _danger);
            _message.style.display = string.IsNullOrEmpty(_pending)
                ? DisplayStyle.None
                : DisplayStyle.Flex;

            if (!string.IsNullOrEmpty(_pending))
            {
                _hide = _message.schedule.Execute(Clear).StartingIn(6500);
            }
        }
    }
}
