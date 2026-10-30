using System;
using System.Collections.Generic;
using UnityEngine;

namespace TentacleVania.Core.Characters
{
    public interface IChainSource
    {
        int ChainOrder { get; }
        int ChainCount { get; }
        int ChainJoints { get; }
        void WriteChain(int chain, Span<Vector4> joints);
    }

    public static class Chains
    {
        public const int DefaultJoints = 6;
        public const int MinJoints = 3;
        public const int MaxJoints = 16;

        public static bool ShowPlaceholderLines { get; set; }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetState() => ShowPlaceholderLines = false;

        public static int CountChains(IReadOnlyList<IChainSource> sources)
        {
            var count = 0;
            for (var i = 0; i < sources.Count; i++)
                count += sources[i].ChainCount;
            return count;
        }

        public static int Width(IReadOnlyList<IChainSource> sources)
        {
            var width = MinJoints;
            for (var i = 0; i < sources.Count; i++)
                width = Mathf.Max(width, sources[i].ChainJoints);
            return Mathf.Min(width, MaxJoints);
        }

        public static int Pack(IReadOnlyList<IChainSource> sources, Span<Vector4> pixels, int maxChains, int width)
        {
            var row = 0;
            for (var s = 0; s < sources.Count; s++)
            {
                var source = sources[s];
                var joints = Mathf.Clamp(source.ChainJoints, 1, width);
                for (var c = 0; c < source.ChainCount && row < maxChains; c++, row++)
                {
                    var slice = pixels.Slice(row * width, width);
                    source.WriteChain(c, slice[..joints]);
                    for (var j = joints; j < width; j++)
                        slice[j] = slice[joints - 1];
                }
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
