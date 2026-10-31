using UnityEngine;

namespace TentacleVania.Combat.Feedback
{
    internal sealed class GibPool
    {
        private const float ShrinkTime = 0.5f;

        private readonly Rigidbody[] _gibs;
        private readonly float[] _expires;
        private readonly Vector3[] _scales;
        private int _next;

        public GibPool(Rigidbody prefab, int size, Transform parent)
        {
            if (prefab == null)
            {
                _gibs = System.Array.Empty<Rigidbody>();
                _expires = System.Array.Empty<float>();
                _scales = System.Array.Empty<Vector3>();
                return;
            }
            _gibs = new Rigidbody[size];
            _expires = new float[size];
            _scales = new Vector3[size];
            for (var i = 0; i < size; i++)
            {
                _gibs[i] = Object.Instantiate(prefab, parent);
                _gibs[i].name = $"{prefab.name} {i + 1}";
                _scales[i] = _gibs[i].transform.localScale;
                _gibs[i].gameObject.SetActive(false);
            }
        }

        public int Active { get; private set; }

        public void Spawn(Vector3 point, Vector3 velocity, float lifetime)
        {
            if (_gibs.Length == 0)
                return;
            var index = _next;
            _next = (_next + 1) % _gibs.Length;
            var gib = _gibs[index];
            if (!gib.gameObject.activeSelf)
                Active++;
            gib.transform.SetPositionAndRotation(point, Random.rotationUniform);
            gib.transform.localScale = _scales[index] * Random.Range(0.7f, 1.3f);
            gib.gameObject.SetActive(true);
            gib.linearVelocity = velocity;
            gib.angularVelocity = Random.insideUnitSphere * 15f;
            _expires[index] = Time.time + lifetime;
        }

        public void Tick(float now)
        {
            if (Active == 0)
                return;
            for (var i = 0; i < _gibs.Length; i++)
            {
                var gib = _gibs[i];
                if (!gib.gameObject.activeSelf)
                    continue;
                var left = _expires[i] - now;
                if (left <= 0f)
                {
                    gib.gameObject.SetActive(false);
                    Active--;
                }
                else if (left < ShrinkTime)
                {
                    gib.transform.localScale = Vector3.Lerp(Vector3.zero, gib.transform.localScale, left / (left + Time.deltaTime));
                }
            }
        }

        public void Clear()
        {
            foreach (var gib in _gibs)
                gib.gameObject.SetActive(false);
            Active = 0;
        }
    }
}
