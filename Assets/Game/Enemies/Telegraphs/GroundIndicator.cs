using TentacleVania.Enemies.Data;
using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace TentacleVania.Enemies.Telegraphs
{
    public sealed class GroundIndicator : MonoBehaviour
    {
        private const float ProjectionDepth = 0.6f;
        private const float HeightAboveFloor = 0.3f;
        private const float CircleShape = 0f;
        private const float LaneShape = 1f;
        private const float SectorShape = 2f;

        private static readonly int ColorId = Shader.PropertyToID("_Color");
        private static readonly int ShapeId = Shader.PropertyToID("_Shape");
        private static readonly int ArcId = Shader.PropertyToID("_Arc");
        private static readonly int ProgressId = Shader.PropertyToID("_Progress");
        private static readonly int SizeId = Shader.PropertyToID("_Size");
        private static readonly int LockId = Shader.PropertyToID("_Lock");

        [SerializeField] private DecalProjector _decal;
        [SerializeField] private Material _material;
        [SerializeField, ColorUsage(false, true)] private Color _dodgeable = new(2f, 1.3f, 0.1f);
        [SerializeField, ColorUsage(false, true)] private Color _undodgeable = new(2.4f, 0.2f, 0.1f);
        [SerializeField, Min(0.01f)] private float _lockFlashTime = 0.25f;
        [SerializeField, Range(0f, 1f)] private float _lockedLevel = 0.35f;

        private Material _instance;
        private bool _locked;
        private float _lockedAt;

        public static bool ForceAll { get; set; }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics() => ForceAll = false;

        private void Awake()
        {
            _instance = new Material(_material);
            _decal.material = _instance;
            _decal.pivot = Vector3.forward * (ProjectionDepth * 0.5f);
            Hide();
        }

        private void OnDestroy()
        {
            if (_instance != null)
                Destroy(_instance);
        }

        public void ShowCircle(Vector3 center, float radius, Telegraph telegraph, float progress, bool locked)
        {
            var diameter = radius * 2f;
            Place(center, Vector3.forward, new Vector2(diameter, diameter));
            Apply(CircleShape, 360f, new Vector2(diameter, diameter), telegraph, progress, locked);
        }

        public void ShowLane(Vector3 origin, Vector3 direction, float width, float length, Telegraph telegraph, float progress, bool locked)
        {
            var size = new Vector2(width, Mathf.Max(length, 0.01f));
            Place(origin + direction * (size.y * 0.5f), direction, size);
            Apply(LaneShape, 0f, size, telegraph, progress, locked);
        }

        public void ShowSector(Vector3 origin, Vector3 forward, float reach, float arc, Telegraph telegraph, float progress, bool locked)
        {
            var diameter = reach * 2f;
            Place(origin, forward, new Vector2(diameter, diameter));
            Apply(SectorShape, arc, new Vector2(diameter, diameter), telegraph, progress, locked);
        }

        public void Hide()
        {
            _decal.enabled = false;
            _locked = false;
        }

        private void Place(Vector3 center, Vector3 forward, Vector2 size)
        {
            forward.y = 0f;
            var yaw = forward.sqrMagnitude > 1e-6f ? Quaternion.LookRotation(forward).eulerAngles.y : 0f;
            transform.SetPositionAndRotation(center + Vector3.up * HeightAboveFloor, Quaternion.Euler(90f, yaw, 0f));
            _decal.size = new Vector3(size.x, size.y, ProjectionDepth);
        }

        private void Apply(float shape, float arc, Vector2 size, Telegraph telegraph, float progress, bool locked)
        {
            if (locked && !_locked)
                _lockedAt = Time.time;
            _locked = locked;
            var flash = locked ? Mathf.Max(_lockedLevel, 1f - (Time.time - _lockedAt) / _lockFlashTime) : 0f;
            _instance.SetColor(ColorId, telegraph == Telegraph.Undodgeable ? _undodgeable : _dodgeable);
            _instance.SetFloat(ShapeId, shape);
            _instance.SetFloat(ArcId, arc);
            _instance.SetFloat(ProgressId, Mathf.Clamp01(progress));
            _instance.SetVector(SizeId, new Vector4(size.x, size.y, 0f, 0f));
            _instance.SetFloat(LockId, flash);
            _decal.enabled = true;
        }
    }
}
