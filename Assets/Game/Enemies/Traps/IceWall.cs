using System.Collections.Generic;
using FMODUnity;
using TentacleVania.Enemies.Data;
using TentacleVania.Enemies.Telegraphs;
using UnityEngine;
using UnityEngine.AI;

namespace TentacleVania.Enemies.Traps
{
    public sealed class IceWall : MonoBehaviour
    {
        public enum WallState
        {
            Warning,
            Rising,
            Up,
            Falling,
        }

        private static readonly List<IceWall> Raised = new();

        [SerializeField] private Transform _body;
        [SerializeField] private BoxCollider _collider;
        [SerializeField] private NavMeshObstacle _obstacle;
        [SerializeField] private GroundIndicator _indicator;
        [SerializeField, Min(0.4f)] private float _warningTime = 0.8f;
        [SerializeField, Min(0.05f)] private float _riseTime = 0.25f;
        [SerializeField, Min(0.05f)] private float _fallTime = 0.4f;
        [SerializeField, Min(0.1f)] private float _height = 2.6f;
        [SerializeField, Min(0.1f)] private float _thickness = 1f;
        [SerializeField] private EventReference _riseSound;
        [SerializeField] private EventReference _shatterSound;

        private float _width;
        private float _lifetime;
        private float _stateStarted;

        public static IReadOnlyList<IceWall> Active => Raised;

        public IceWall Prefab { get; set; }
        public WallState State { get; private set; }
        public float Width => _width;
        public int Generation { get; private set; }
        public bool Solid => State == WallState.Rising || State == WallState.Up;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics() => Raised.Clear();

        public static float Distance(Vector3 center, Vector3 along, float width, float thickness, Vector3 point)
        {
            var offset = point - center;
            offset.y = 0f;
            along.y = 0f;
            along = along.sqrMagnitude > 1e-6f ? along.normalized : Vector3.forward;
            var closest = along * Mathf.Clamp(Vector3.Dot(offset, along), -width * 0.5f, width * 0.5f);
            return Mathf.Max(0f, (offset - closest).magnitude - thickness * 0.5f);
        }

        public bool Touches(Vector3 point, float radius) => Solid && Distance(transform.position, transform.forward, _width, _thickness, point) <= radius;

        public void Raise(Vector3 position, Vector3 along, float width, float lifetime)
        {
            along.y = 0f;
            transform.SetPositionAndRotation(position, Quaternion.LookRotation(along.sqrMagnitude > 1e-4f ? along.normalized : Vector3.forward));
            _width = width;
            _lifetime = lifetime;
            Generation++;
            if (_collider != null)
            {
                _collider.size = new Vector3(_thickness, _height, width);
                _collider.center = new Vector3(0f, _height * 0.5f, 0f);
            }
            if (_obstacle != null)
            {
                _obstacle.size = new Vector3(_thickness, _height, width);
                _obstacle.center = new Vector3(0f, _height * 0.5f, 0f);
            }
            gameObject.SetActive(true);
            Enter(WallState.Warning);
        }

        public void Shatter()
        {
            if (State == WallState.Falling || !gameObject.activeSelf)
                return;
            if (!_shatterSound.IsNull)
                RuntimeManager.PlayOneShot(_shatterSound, transform.position);
            Enter(WallState.Falling);
        }

        private void OnDisable() => Raised.Remove(this);

        private void Enter(WallState state)
        {
            State = state;
            _stateStarted = Time.time;
            var solid = Solid;
            if (_collider != null)
                _collider.enabled = solid;
            if (_obstacle != null)
                _obstacle.enabled = solid;
            if (solid && !Raised.Contains(this))
                Raised.Add(this);
            else if (!solid)
                Raised.Remove(this);
            if (state == WallState.Rising && !_riseSound.IsNull)
                RuntimeManager.PlayOneShot(_riseSound, transform.position);
            if (state != WallState.Warning && _indicator != null)
                _indicator.Hide();
            Place(state == WallState.Warning ? 0f : state == WallState.Up ? 1f : Body());
        }

        private float Body() => _body != null ? _body.localScale.y / _height : 0f;

        private void Update()
        {
            var elapsed = Time.time - _stateStarted;
            switch (State)
            {
                case WallState.Warning:
                    if (_indicator != null)
                    {
                        var along = transform.forward;
                        _indicator.ShowLane(transform.position - along * (_width * 0.5f), along, _thickness, _width, Telegraph.Dodgeable, elapsed / _warningTime, true);
                    }
                    if (elapsed >= _warningTime)
                        Enter(WallState.Rising);
                    break;
                case WallState.Rising:
                    Place(elapsed / _riseTime);
                    if (elapsed >= _riseTime)
                        Enter(WallState.Up);
                    break;
                case WallState.Up:
                    if (elapsed >= _lifetime)
                        Enter(WallState.Falling);
                    break;
                case WallState.Falling:
                    Place(1f - elapsed / _fallTime);
                    if (elapsed >= _fallTime)
                        EnemyManager.ReturnWall(this);
                    break;
            }
        }

        private void Place(float raised)
        {
            if (_body == null)
                return;
            raised = Mathf.Clamp01(raised);
            _body.localScale = new Vector3(_thickness, Mathf.Max(0.01f, _height * raised), _width);
            _body.localPosition = new Vector3(0f, _height * raised * 0.5f, 0f);
            _body.gameObject.SetActive(raised > 0.01f);
        }
    }
}
