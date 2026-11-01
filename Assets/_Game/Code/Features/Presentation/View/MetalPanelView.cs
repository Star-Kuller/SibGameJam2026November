using System.Collections.Generic;
using _Game.Code.Features.GateEncounter.View;
using UnityEngine;
using UnityEngine.UIElements;

namespace _Game.Code.Features.Presentation.View
{
    // Static surface detail only; controls keep their standard UI Toolkit behaviour.
    public sealed class MetalPanelView : DocumentSectionView
    {
        private readonly List<VisualElement> _plates = new();

        protected override void BindElements()
        {
            Root.Query<VisualElement>(className: "metal-plate").ForEach(plate =>
            {
                var surface = new VisualElement { pickingMode = PickingMode.Ignore };
                surface.style.position = Position.Absolute;
                surface.style.left = 0;
                surface.style.right = 0;
                surface.style.top = 0;
                surface.style.bottom = 0;
                plate.Insert(0, surface);
                surface.generateVisualContent += Draw;
                _plates.Add(surface);
            });
        }

        protected override void UnbindElements()
        {
            foreach (var plate in _plates)
            {
                plate.generateVisualContent -= Draw;
                plate.RemoveFromHierarchy();
            }

            _plates.Clear();
        }

        private static void Draw(MeshGenerationContext context)
        {
            var rect = context.visualElement.contentRect;

            if (rect.width < 16 || rect.height < 16)
            {
                return;
            }

            var painter = context.painter2D;
            painter.lineWidth = 0.6f;
            painter.strokeColor = new Color(0.8f, 0.78f, 0.68f, 0.03f);
            painter.BeginPath();

            for (var index = 0; index < Mathf.Min(180, rect.width * rect.height / 400); index++)
            {
                var x = 5 + Hash(index * 73 + 11) * (rect.width - 10);
                var y = 5 + Hash(index * 97 + 23) * (rect.height - 10);
                painter.MoveTo(new Vector2(x, y));
                painter.LineTo(new Vector2(Mathf.Min(rect.width - 5, x + 2 + Hash(index) * 13), y - 0.5f));
            }

            painter.Stroke();
            painter.lineWidth = 1;
            painter.strokeColor = new Color(0.5f, 0.51f, 0.46f, 0.28f);
            painter.BeginPath();
            painter.MoveTo(new Vector2(3, rect.height - 3));
            painter.LineTo(new Vector2(3, 3));
            painter.LineTo(new Vector2(rect.width - 3, 3));
            painter.Stroke();

            foreach (var point in new[]
            {
                new Vector2(7, 7), new Vector2(rect.width - 7, 7),
                new Vector2(7, rect.height - 7), new Vector2(rect.width - 7, rect.height - 7)
            })
            {
                painter.BeginPath();
                painter.fillColor = new Color(0.08f, 0.09f, 0.08f, 0.95f);
                painter.Arc(point, 2.3f, 0, 360);
                painter.Fill();
                painter.Stroke();
            }
        }

        private static float Hash(int seed)
        {
            var value = unchecked((uint)seed * 747796405u + 2891336453u);
            value ^= value >> 16;
            return (value & 0xffff) / 65535f;
        }
    }
}
