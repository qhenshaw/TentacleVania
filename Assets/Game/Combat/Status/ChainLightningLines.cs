using UnityEngine;

namespace TentacleVania.Combat.Status
{
    public sealed class ChainLightningLines : MonoBehaviour
    {
        private const int PoolSize = 12;
        private const int Points = 8;

        private static ChainLightningLines _instance;

        private readonly LineRenderer[] _lines = new LineRenderer[PoolSize];
        private readonly float[] _endsAt = new float[PoolSize];
        private readonly Vector3[] _points = new Vector3[Points];
        private StatusEffectSettings _settings;
        private int _next;

        public static int Active { get; private set; }

        public static void Prewarm(StatusEffectSettings settings)
        {
            if (_instance != null || !Application.isPlaying)
                return;
            var root = new GameObject("ChainLightning");
            _instance = root.AddComponent<ChainLightningLines>();
            _instance.Build(settings);
        }

        public static void Draw(Vector3 from, Vector3 to)
        {
            if (_instance != null)
                _instance.Show(from, to);
        }

        private void Build(StatusEffectSettings settings)
        {
            _settings = settings;
            for (var i = 0; i < PoolSize; i++)
            {
                var child = new GameObject("Arc");
                child.transform.SetParent(transform, false);
                var line = child.AddComponent<LineRenderer>();
                line.positionCount = Points;
                line.useWorldSpace = true;
                line.sharedMaterial = settings.ChainMaterial;
                line.startColor = settings.ChainColor;
                line.endColor = settings.ChainColor;
                line.widthMultiplier = settings.ChainWidth;
                line.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                line.receiveShadows = false;
                line.enabled = false;
                _lines[i] = line;
            }
            enabled = false;
        }

        private void Show(Vector3 from, Vector3 to)
        {
            var line = _lines[_next];
            _endsAt[_next] = Time.time + _settings.ChainDuration;
            _next = (_next + 1) % PoolSize;
            var jitter = _settings.ChainJitter;
            for (var i = 0; i < Points; i++)
            {
                var t = i / (float)(Points - 1);
                var offset = i == 0 || i == Points - 1 ? Vector3.zero : Random.insideUnitSphere * jitter;
                _points[i] = Vector3.Lerp(from, to, t) + offset;
            }
            line.SetPositions(_points);
            line.widthMultiplier = _settings.ChainWidth;
            line.enabled = true;
            enabled = true;
        }

        private void Update()
        {
            var now = Time.time;
            var active = 0;
            for (var i = 0; i < PoolSize; i++)
            {
                var line = _lines[i];
                if (!line.enabled)
                    continue;
                var left = _endsAt[i] - now;
                if (left <= 0f)
                {
                    line.enabled = false;
                    continue;
                }
                line.widthMultiplier = _settings.ChainWidth * Mathf.Clamp01(left / _settings.ChainDuration);
                active++;
            }
            Active = active;
            if (active == 0)
                enabled = false;
        }

        private void OnDestroy()
        {
            if (_instance == this)
                _instance = null;
        }
    }
}
