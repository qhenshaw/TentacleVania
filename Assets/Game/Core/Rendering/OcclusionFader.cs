using System.Collections.Generic;
using UnityEngine;

namespace TentacleVania.Core.Rendering
{
    public sealed class OcclusionFader : MonoBehaviour
    {
        private static readonly int FadeId = Shader.PropertyToID("_Fade");
        private static readonly int FocusId = Shader.PropertyToID("_EnvironmentFadeFocus");
        private static readonly Vector3[] RayOffsets =
        {
            new(0f, 0.6f, 0f), new(-0.6f, 0.3f, 0f), new(0.6f, 0.3f, 0f), new(0f, 1.3f, 0f), new(0f, 0.05f, 0f),
        };

        [SerializeField] private LayerMask _occluders = 1;
        [SerializeField, Range(0f, 1f)] private float _maxFade = 0.85f;
        [SerializeField, Min(0.01f)] private float _fadeSpeed = 4f;
        [Tooltip("Radius of the faded hole around the creature, as a fraction of screen height.")]
        [SerializeField, Range(0.05f, 0.5f)] private float _holeRadius = 0.16f;
        [SerializeField, Min(0f)] private float _margin = 0.4f;

        private readonly RaycastHit[] _hits = new RaycastHit[16];
        private readonly Dictionary<Collider, Entry> _byCollider = new(64);
        private readonly List<Entry> _entries = new(64);
        private UnityEngine.Camera _camera;
        private int _frame;

        public static bool Enabled { get; set; } = true;
        public int FadedCount { get; private set; }

        private sealed class Entry
        {
            public Renderer Renderer;
            public Material[] Originals;
            public Material[] Instances;
            public float Fade;
            public int HitFrame = -1;
            public bool Applied;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics() => Enabled = true;

        private void Awake() => _camera = GetComponentInChildren<UnityEngine.Camera>();

        private void OnDisable()
        {
            foreach (var entry in _entries)
                Restore(entry);
        }

        private void OnDestroy()
        {
            foreach (var entry in _entries)
                if (entry.Instances != null)
                    foreach (var material in entry.Instances)
                        Destroy(material);
        }

        private void LateUpdate()
        {
            _frame++;
            var target = CameraFocus.Target;
            if (_camera == null)
                _camera = UnityEngine.Camera.main;
            if (Enabled && target != null && _camera != null)
                Probe(target.position);
            var step = _fadeSpeed * Time.unscaledDeltaTime;
            var faded = 0;
            foreach (var entry in _entries)
            {
                if (entry.Renderer == null)
                    continue;
                var wanted = entry.HitFrame == _frame ? _maxFade : 0f;
                if (entry.Fade == wanted && !(wanted > 0f && !entry.Applied))
                    continue;
                entry.Fade = Mathf.MoveTowards(entry.Fade, wanted, step);
                if (entry.Fade <= 0f)
                {
                    Restore(entry);
                    continue;
                }
                if (!entry.Applied)
                {
                    entry.Instances ??= CreateInstances(entry.Originals);
                    entry.Renderer.sharedMaterials = entry.Instances;
                    entry.Applied = true;
                }
                foreach (var material in entry.Instances)
                    material.SetFloat(FadeId, entry.Fade);
            }
            foreach (var entry in _entries)
                if (entry.Applied)
                    faded++;
            FadedCount = faded;
        }

        private void Probe(Vector3 creature)
        {
            var origin = _camera.transform.position;
            var right = _camera.transform.right;
            var screen = _camera.WorldToViewportPoint(creature + Vector3.up * 0.6f);
            Shader.SetGlobalVector(FocusId, new Vector4(screen.x, screen.y, _holeRadius, _camera.aspect));
            foreach (var offset in RayOffsets)
            {
                var point = creature + right * offset.x + Vector3.up * offset.y;
                var toPoint = point - origin;
                var distance = toPoint.magnitude - _margin;
                if (distance <= 0f)
                    continue;
                var count = Physics.RaycastNonAlloc(origin, toPoint.normalized, _hits, distance, _occluders, QueryTriggerInteraction.Ignore);
                for (var i = 0; i < count; i++)
                {
                    var entry = Lookup(_hits[i].collider);
                    if (entry != null)
                        entry.HitFrame = _frame;
                }
            }
        }

        private Entry Lookup(Collider collider)
        {
            if (_byCollider.TryGetValue(collider, out var entry))
                return entry;
            var renderer = collider.GetComponent<Renderer>();
            if (renderer == null)
                renderer = collider.GetComponentInParent<Renderer>();
            entry = null;
            if (renderer != null && Fadeable(renderer.sharedMaterials))
            {
                entry = new Entry { Renderer = renderer, Originals = renderer.sharedMaterials };
                _entries.Add(entry);
            }
            _byCollider.Add(collider, entry);
            return entry;
        }

        private static bool Fadeable(Material[] materials)
        {
            if (materials.Length == 0)
                return false;
            foreach (var material in materials)
                if (material == null || !material.HasProperty(FadeId))
                    return false;
            return true;
        }

        private static Material[] CreateInstances(Material[] originals)
        {
            var instances = new Material[originals.Length];
            for (var i = 0; i < originals.Length; i++)
                instances[i] = new Material(originals[i]) { name = originals[i].name + " (Fade)" };
            return instances;
        }

        private static void Restore(Entry entry)
        {
            entry.Fade = 0f;
            if (!entry.Applied || entry.Renderer == null)
                return;
            entry.Renderer.sharedMaterials = entry.Originals;
            entry.Applied = false;
        }
    }
}
