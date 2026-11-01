using System.Collections.Generic;
using TentacleVania.Combat.Damage;

namespace TentacleVania.Combat.Status
{
    public sealed class StatusAffinity
    {
        public const float Weak = 1.25f;
        public const float Resist = 0.75f;
        public const int KindCount = 8;

        private readonly float[] _scales = new float[KindCount];

        public StatusAffinity() => Clear();

        public float Scale(StatusKind kind)
        {
            var index = (int)kind;
            return index >= 0 && index < KindCount ? _scales[index] : 1f;
        }

        public bool IsWeak(StatusKind kind) => Scale(kind) > 1f;

        public bool Resists(StatusKind kind) => Scale(kind) < 1f;

        public void Clear()
        {
            for (var i = 0; i < KindCount; i++)
                _scales[i] = 1f;
        }

        public void Configure(IReadOnlyList<StatusKind> weakTo, IReadOnlyList<StatusKind> resists)
        {
            Clear();
            if (weakTo != null)
                foreach (var kind in weakTo)
                    Set(kind, Weak);
            if (resists != null)
                foreach (var kind in resists)
                    Set(kind, Resist);
        }

        public void Set(StatusKind kind, float scale)
        {
            var index = (int)kind;
            if (kind != StatusKind.None && index < KindCount)
                _scales[index] = scale;
        }
    }
}
