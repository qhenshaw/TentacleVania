using TentacleVania.Enemies.Data;
using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace TentacleVania.Enemies.Telegraphs
{
    public sealed class GroundIndicator : MonoBehaviour
    {
        private const float ProjectionDepth = 0.6f;
        private const float HeightAboveFloor = 0.3f;

        [SerializeField] private DecalProjector _outline;
        [SerializeField] private DecalProjector _fill;
        [SerializeField] private Material _dodgeableOutline;
        [SerializeField] private Material _dodgeableFill;
        [SerializeField] private Material _undodgeableOutline;
        [SerializeField] private Material _undodgeableFill;

        private void Awake()
        {
            var pivot = Vector3.forward * (ProjectionDepth * 0.5f);
            _outline.pivot = pivot;
            _fill.pivot = pivot;
            Hide();
        }

        public void Show(Vector3 center, float radius, Telegraph telegraph, float progress)
        {
            var undodgeable = telegraph == Telegraph.Undodgeable;
            transform.SetPositionAndRotation(center + Vector3.up * HeightAboveFloor, Quaternion.Euler(90f, 0f, 0f));
            _outline.material = undodgeable ? _undodgeableOutline : _dodgeableOutline;
            _fill.material = undodgeable ? _undodgeableFill : _dodgeableFill;
            var diameter = radius * 2f;
            _outline.size = new Vector3(diameter, diameter, ProjectionDepth);
            var fill = diameter * Mathf.Clamp01(progress);
            _fill.size = new Vector3(fill, fill, ProjectionDepth);
            _outline.enabled = true;
            _fill.enabled = fill > 0.01f;
        }

        public void Hide()
        {
            _outline.enabled = false;
            _fill.enabled = false;
        }
    }
}
