using _Game.Code.Features.GateEncounter.View;
using _Game.Code.Features.Input.Models;
using UnityEngine;
using UnityEngine.UIElements;
using VContainer;

namespace _Game.Code.Features.Presentation.View
{
    // Draws over the world and below the HUD. It never handles input or advances gameplay.
    public sealed class CctvOverlayView : DocumentSectionView
    {
        private PlayerSettingsModel _settings;
        private VisualElement _overlay;
        private IVisualElementScheduledItem _animation;
        private int _frame;

        [Inject]
        public void Bind(PlayerSettingsModel settings)
        {
            _settings = settings;
        }

        protected override void BindElements()
        {
            _overlay = Element<VisualElement>("cctv-overlay");
            _overlay.generateVisualContent += Draw;
            _animation = _overlay.schedule.Execute(Animate).Every(100);
        }

        protected override void UnbindElements()
        {
            _animation?.Pause();
            _overlay.generateVisualContent -= Draw;
            _overlay = null;
        }

        private void Animate()
        {
            _frame++;
            _overlay.MarkDirtyRepaint();
        }

        private void Draw(MeshGenerationContext context)
        {
            if (_settings == null || !_settings.CctvEnabled)
            {
                return;
            }

            var width = _overlay.contentRect.width;
            var height = _overlay.contentRect.height;

            if (width <= 0 || height <= 0)
            {
                return;
            }

            var painter = context.painter2D;
            painter.lineWidth = 0.7f;
            painter.strokeColor = new Color(0.02f, 0.07f, 0.06f, 0.055f);
            painter.BeginPath();

            for (var y = 1.0f; y < height; y += 5)
            {
                painter.MoveTo(new Vector2(0, y));
                painter.LineTo(new Vector2(width, y));
            }

            painter.Stroke();

            // Sparse signal noise stays subtle; there is no full-screen brightness flicker.
            painter.strokeColor = new Color(0.7f, 0.85f, 0.73f, 0.045f);
            painter.lineWidth = 1;
            painter.BeginPath();

            for (var index = 0; index < 24; index++)
            {
                var seed = unchecked((uint)(index * 73856093) ^ (uint)(_frame * 19349663));
                var x = Hash(seed) * width;
                var y = Hash(seed + 37) * height;
                var length = 3 + Hash(seed + 79) * 16;

                painter.MoveTo(new Vector2(x, y));
                painter.LineTo(new Vector2(Mathf.Min(width, x + length), y));
            }

            painter.Stroke();
            DrawCornerMarks(painter, width, height);
        }

        private static void DrawCornerMarks(Painter2D painter, float width, float height)
        {
            painter.strokeColor = new Color(0.65f, 0.77f, 0.64f, 0.22f);
            painter.lineWidth = 1;
            painter.BeginPath();

            DrawCorner(painter, new Vector2(18, 18), Vector2.right, Vector2.up);
            DrawCorner(painter, new Vector2(width - 18, 18), Vector2.left, Vector2.up);
            DrawCorner(painter, new Vector2(18, height - 18), Vector2.right, Vector2.down);
            DrawCorner(painter, new Vector2(width - 18, height - 18), Vector2.left, Vector2.down);

            painter.Stroke();
        }

        private static void DrawCorner(Painter2D painter, Vector2 origin, Vector2 horizontal, Vector2 vertical)
        {
            painter.MoveTo(origin + horizontal * 22);
            painter.LineTo(origin);
            painter.LineTo(origin + vertical * 22);
        }

        private static float Hash(uint value)
        {
            value ^= value >> 16;
            value *= 0x7feb352d;
            value ^= value >> 15;
            value *= 0x846ca68b;
            value ^= value >> 16;
            return (value & 0xffff) / 65535f;
        }
    }
}
