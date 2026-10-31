using UnityEngine;
using UnityEngine.UIElements;

namespace TentacleVania.UI.Hud
{
    public sealed class HoldRing : VisualElement
    {
        private float _progress;

        public HoldRing(float size)
        {
            pickingMode = PickingMode.Ignore;
            style.width = size;
            style.height = size;
            generateVisualContent += Draw;
        }

        public Color Track { get; set; } = new(1f, 1f, 1f, 0.25f);
        public Color Fill { get; set; } = new(0.55f, 1f, 0.7f, 0.95f);
        public float Thickness { get; set; } = 3f;

        public float Progress
        {
            get => _progress;
            set
            {
                var clamped = Mathf.Clamp01(value);
                if (Mathf.Approximately(clamped, _progress))
                    return;
                _progress = clamped;
                MarkDirtyRepaint();
            }
        }

        private void Draw(MeshGenerationContext context)
        {
            var rect = contentRect;
            var radius = Mathf.Min(rect.width, rect.height) * 0.5f - Thickness;
            if (radius <= 0f)
                return;
            var centre = rect.center;
            var painter = context.painter2D;
            painter.lineWidth = Thickness;
            painter.lineCap = LineCap.Butt;
            painter.strokeColor = Track;
            painter.BeginPath();
            painter.Arc(centre, radius, 0f, 360f);
            painter.Stroke();
            if (_progress <= 0f)
                return;
            painter.strokeColor = Fill;
            painter.BeginPath();
            painter.Arc(centre, radius, -90f, -90f + 360f * _progress);
            painter.Stroke();
        }
    }
}
