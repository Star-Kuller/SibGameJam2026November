using UnityEngine.UIElements;

namespace _Game.Code.Features.GateEncounter.View
{
    public sealed class NotificationView : DocumentSectionView
    {
        private Label _message;
        private IVisualElementScheduledItem _hide;
        private Label _wave;
        private IVisualElementScheduledItem _hideWave;
        private string _waveText = string.Empty;
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
            ClearMessage();
            ClearWave();
        }

        public void ShowWave(string message)
        {
            _waveText = message;
            RenderWave();
        }

        private void ClearMessage()
        {
            _pending = string.Empty;
            _hide?.Pause();
            Render();
        }

        protected override void BindElements()
        {
            _message = Element<Label>("notification");
            _wave = Element<Label>("wave-announcement");
            Render();
            RenderWave();
        }

        protected override void UnbindElements()
        {
            _hide?.Pause();
            _hideWave?.Pause();
            _message = null;
            _wave = null;
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
                _hide = _message.schedule.Execute(ClearMessage).StartingIn(6500);
            }
        }

        private void ClearWave()
        {
            _waveText = string.Empty;
            RenderWave();
        }

        private void RenderWave()
        {
            if (_wave == null)
            {
                return;
            }

            _hideWave?.Pause();
            _wave.text = _waveText;
            _wave.style.display = string.IsNullOrEmpty(_waveText) ? DisplayStyle.None : DisplayStyle.Flex;

            if (!string.IsNullOrEmpty(_waveText))
            {
                _hideWave = _wave.schedule.Execute(ClearWave).StartingIn(4000);
            }
        }
    }
}
