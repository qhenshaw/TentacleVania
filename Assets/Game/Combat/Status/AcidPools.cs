using FMODUnity;
using TentacleVania.Combat.Damage;
using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace TentacleVania.Combat.Status
{
    public sealed class AcidPools : MonoBehaviour
    {
        private const float DecalDepth = 0.6f;
        private static readonly Vector3 Parked = new(0f, -1000f, 0f);
        private static readonly int SizeId = Shader.PropertyToID("_Size");
        private static readonly int ProgressId = Shader.PropertyToID("_Progress");
        private static readonly Collider[] Overlaps = new Collider[32];
        private static readonly Health[] Struck = new Health[32];

        private static AcidPools _instance;

        private StatusEffectSettings _settings;
        private AcidPoolSlots _slots;
        private DecalProjector[] _decals;
        private Material[] _materials;
        private int _mask;

        public static int Active => _instance != null ? _instance._slots.ActiveCount : 0;

        public static void Prewarm(StatusEffectSettings settings)
        {
            if (_instance != null || !Application.isPlaying)
                return;
            var root = new GameObject("AcidPools");
            _instance = root.AddComponent<AcidPools>();
            _instance.Build(settings);
        }

        public static void Spawn(Vector3 position, Team team) =>
            Spawn(position, team, -1f, -1f, -1f);

        public static void Spawn(Vector3 position, Team team, float radius, float duration, float damagePerSecond)
        {
            if (_instance == null)
                Prewarm(StatusEffectSettings.Instance);
            if (_instance != null)
                _instance.Place(position, team, radius, duration, damagePerSecond);
        }

        public static void ClearAll()
        {
            if (_instance == null)
                return;
            for (var i = 0; i < _instance._slots.Count; i++)
                _instance.Hide(i);
            _instance._slots.Clear();
        }

        private void Build(StatusEffectSettings settings)
        {
            _settings = settings;
            _mask = LayerMask.GetMask("Enemy", "Creature");
            _slots = new(settings.AcidPoolCount);
            _decals = new DecalProjector[_slots.Count];
            _materials = new Material[_slots.Count];
            for (var i = 0; i < _slots.Count; i++)
            {
                var child = new GameObject("AcidPool");
                child.transform.SetParent(transform, false);
                child.transform.rotation = Quaternion.Euler(90f, 0f, 0f);
                var decal = child.AddComponent<DecalProjector>();
                if (settings.AcidPoolMaterial != null)
                {
                    _materials[i] = new Material(settings.AcidPoolMaterial);
                    decal.material = _materials[i];
                }
                _decals[i] = decal;
                Hide(i);
            }
        }

        private void Place(Vector3 position, Team team, float radius, float duration, float damagePerSecond)
        {
            radius = radius > 0f ? radius : _settings.AcidPoolRadius;
            duration = duration > 0f ? duration : _settings.AcidPoolDuration;
            damagePerSecond = damagePerSecond >= 0f ? damagePerSecond : _settings.AcidPoolDamagePerSecond;
            var index = _slots.Spawn(position, radius, duration, damagePerSecond, team, Time.time);
            var decal = _decals[index];
            decal.transform.position = position + Vector3.up * (DecalDepth * 0.5f);
            decal.size = new Vector3(radius * 2f, radius * 2f, DecalDepth);
            decal.fadeFactor = 1f;
            if (_materials[index] != null)
            {
                _materials[index].SetVector(SizeId, new Vector4(radius * 2f, radius * 2f, 0f, 0f));
                _materials[index].SetFloat(ProgressId, 1f);
            }
            if (!_settings.AcidPoolSound.IsNull)
                RuntimeManager.PlayOneShot(_settings.AcidPoolSound, position);
            enabled = true;
        }

        private void Hide(int index)
        {
            var decal = _decals[index];
            decal.fadeFactor = 0f;
            decal.transform.position = Parked;
        }

        private void Update()
        {
            var now = Time.time;
            var interval = _settings.TickInterval;
            var any = false;
            for (var i = 0; i < _slots.Count; i++)
            {
                if (_slots.Expire(i, now))
                {
                    Hide(i);
                    continue;
                }
                ref var slot = ref _slots[i];
                if (!slot.Active)
                    continue;
                any = true;
                var left = slot.EndsAt - now;
                _decals[i].fadeFactor = _settings.AcidPoolFade > 0f ? Mathf.Clamp01(left / _settings.AcidPoolFade) : 1f;
                if (_slots.TickDue(i, now, interval))
                    Burn(slot, interval);
            }
            if (!any)
                enabled = false;
        }

        private void Burn(in AcidPoolSlots.Slot slot, float interval)
        {
            var bottom = slot.Position + Vector3.up * 0.1f;
            var top = slot.Position + Vector3.up * 1.2f;
            var count = Physics.OverlapCapsuleNonAlloc(bottom, top, slot.Radius, Overlaps, _mask, QueryTriggerInteraction.Collide);
            var struck = 0;
            for (var i = 0; i < count; i++)
            {
                if (!Hurtbox.TryGet(Overlaps[i], out var hurtbox))
                    continue;
                var health = hurtbox.Health;
                if (health == null || !health.IsAlive || !TeamRules.CanDamage(slot.Team, health.Team) || Contains(struck, health))
                    continue;
                var offset = health.transform.position - slot.Position;
                offset.y = 0f;
                if (offset.sqrMagnitude > slot.Radius * slot.Radius)
                    continue;
                Struck[struck++] = health;
                if (health.TryGetComponent<StatusEffects>(out var status))
                    status.Apply(StatusKind.Corrode, 0f, 0f, slot.Team, 1f);
                var damage = slot.DamagePerSecond * interval;
                if (damage > 0f)
                {
                    var info = DamageInfo.Create(damage, slot.Team, DamageType.Acid);
                    info.Flags = HitFlags.DamageOverTime;
                    info.Point = health.transform.position;
                    health.TakeDamage(info);
                }
            }
            for (var i = 0; i < struck; i++)
                Struck[i] = null;
        }

        private static bool Contains(int count, Health health)
        {
            for (var i = 0; i < count; i++)
                if (Struck[i] == health)
                    return true;
            return false;
        }

        private void OnDestroy()
        {
            if (_instance == this)
                _instance = null;
            if (_materials == null)
                return;
            foreach (var material in _materials)
                if (material != null)
                    Destroy(material);
        }
    }
}
