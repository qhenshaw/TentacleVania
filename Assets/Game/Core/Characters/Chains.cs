using System;
using System.Collections.Generic;
using UnityEngine;

namespace TentacleVania.Core.Characters
{
    public interface IChainSource
    {
        int ChainOrder { get; }
        int ChainCount { get; }
        void WriteChain(int chain, Span<Vector4> joints);
    }

    public static class Chains
    {
        public const int Joints = 6;

        public static bool ShowPlaceholderLines { get; set; } = true;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetState() => ShowPlaceholderLines = true;

        public static int CountChains(IReadOnlyList<IChainSource> sources)
        {
            var count = 0;
            for (var i = 0; i < sources.Count; i++)
                count += sources[i].ChainCount;
            return count;
        }

        public static int Pack(IReadOnlyList<IChainSource> sources, Span<Vector4> pixels, int maxChains)
        {
            var row = 0;
            for (var s = 0; s < sources.Count; s++)
            {
                var source = sources[s];
                for (var c = 0; c < source.ChainCount && row < maxChains; c++, row++)
                    source.WriteChain(c, pixels.Slice(row * Joints, Joints));
            }
            return row;
        }

        public static void Write(Span<Vector4> joints, ReadOnlySpan<Vector3> positions, float rootWidth, float tipWidth)
        {
            var last = joints.Length - 1;
            for (var i = 0; i <= last; i++)
            {
                var p = positions[i];
                joints[i] = new(p.x, p.y, p.z, Mathf.Lerp(rootWidth, tipWidth, (float)i / last));
            }
        }

        public static void Collapse(Span<Vector4> joints, Vector3 root)
        {
            for (var i = 0; i < joints.Length; i++)
                joints[i] = new(root.x, root.y, root.z, 0f);
        }
    }
}
