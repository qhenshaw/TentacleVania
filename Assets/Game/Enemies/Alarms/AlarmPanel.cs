using System.Collections;
using System.Collections.Generic;
using FMODUnity;
using UnityEngine;
using UnityEngine.AI;

namespace TentacleVania.Enemies.Alarms
{
    public sealed class AlarmPanel : MonoBehaviour
    {
        private static readonly List<AlarmPanel> Panels = new();

        [Tooltip("Enemies called in when the alarm goes off (design: guards).")]
        [SerializeField] private Enemy _guardPrefab;
        [SerializeField, Min(0)] private int _guards = 2;
        [Tooltip("Ceiling turrets dropped near the creature when the alarm goes off.")]
        [SerializeField] private Enemy _ceilingTurretPrefab;
        [SerializeField, Min(0)] private int _ceilingTurrets = 2;
        [SerializeField, Min(0f)] private float _dropHeight = 5f;
        [SerializeField, Min(0.05f)] private float _dropTime = 0.5f;
        [SerializeField, Min(1f)] private float _spawnRadius = 5f;
        [SerializeField] private Renderer _lamp;
        [SerializeField] private Material _armedMaterial;
        [SerializeField] private Material _triggeredMaterial;
        [SerializeField] private EventReference _alarmSound;

        public static IReadOnlyList<AlarmPanel> All => Panels;
        public bool Triggered { get; private set; }
        public Vector3 Position => transform.position;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics() => Panels.Clear();

        private void OnEnable()
        {
            Panels.Add(this);
            SetLamp();
        }

        private void OnDisable() => Panels.Remove(this);

        public static AlarmPanel Nearest(Vector3 position, float range)
        {
            AlarmPanel best = null;
            var bestDistance = range * range;
            foreach (var panel in Panels)
            {
                if (panel.Triggered)
                    continue;
                var distance = (panel.Position - position).sqrMagnitude;
                if (distance < bestDistance)
                {
                    bestDistance = distance;
                    best = panel;
                }
            }
            return best;
        }

        public void Trigger()
        {
            if (Triggered)
                return;
            Triggered = true;
            SetLamp();
            if (!_alarmSound.IsNull)
                RuntimeManager.PlayOneShot(_alarmSound, transform.position);
            for (var i = 0; i < _guards && _guardPrefab != null; i++)
                Spawn(_guardPrefab, transform.position, false);
            var target = Combat.CombatTargets.Creature;
            var around = target != null ? target.transform.position : transform.position;
            for (var i = 0; i < _ceilingTurrets && _ceilingTurretPrefab != null; i++)
                Spawn(_ceilingTurretPrefab, around, true);
        }

        private void Spawn(Enemy prefab, Vector3 around, bool drop)
        {
            var offset = Random.insideUnitCircle.normalized * Random.Range(_spawnRadius * 0.5f, _spawnRadius);
            var position = around + new Vector3(offset.x, 0f, offset.y);
            if (NavMesh.SamplePosition(position, out var hit, _spawnRadius, NavMesh.AllAreas))
                position = hit.position;
            var enemy = Instantiate(prefab, position, Quaternion.LookRotation(around - position == Vector3.zero ? Vector3.forward : Vector3.ProjectOnPlane(around - position, Vector3.up)));
            if (drop)
                StartCoroutine(Drop(enemy.transform.Find("Model")));
        }

        private IEnumerator Drop(Transform model)
        {
            if (model == null)
                yield break;
            var rest = model.localPosition;
            var elapsed = 0f;
            while (elapsed < _dropTime && model != null)
            {
                elapsed += Time.deltaTime;
                var t = Mathf.Clamp01(elapsed / _dropTime);
                model.localPosition = rest + Vector3.up * (_dropHeight * (1f - t * t));
                yield return null;
            }
            if (model != null)
                model.localPosition = rest;
        }

        private void SetLamp()
        {
            var material = Triggered ? _triggeredMaterial : _armedMaterial;
            if (_lamp != null && material != null)
                _lamp.sharedMaterial = material;
        }
    }
}
