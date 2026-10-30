using System.Collections.Generic;
using TentacleVania.Core.Characters;
using UnityEngine;
using UnityEngine.VFX;

namespace TentacleVania.Creature.Rig
{
    [DefaultExecutionOrder(1000)]
    public sealed class ChainTexture : MonoBehaviour
    {
        private static readonly int ChainPositions = Shader.PropertyToID("ChainPositions");
        private static readonly int ChainCount = Shader.PropertyToID("ChainCount");
        private static readonly int ChainLength = Shader.PropertyToID("ChainLength");

        [SerializeField] private VisualEffect _effect;

        private readonly List<IChainSource> _sources = new(4);

        private Texture2D _texture;

        public Texture2D Texture => _texture;
        public int Count { get; private set; }

        private void Awake()
        {
            GetComponents(_sources);
            _sources.Sort((a, b) => a.ChainOrder.CompareTo(b.ChainOrder));
        }

        private void OnDestroy()
        {
            if (_texture != null)
                Destroy(_texture);
        }

        private void LateUpdate()
        {
            var count = Mathf.Min(Chains.CountChains(_sources), CreatureRigSettings.MaxChains);
            if (_texture == null || count != Count)
                Rebuild(count);
            if (count == 0)
                return;
            Chains.Pack(_sources, _texture.GetPixelData<Vector4>(0).AsSpan(), count);
            _texture.Apply(false);
        }

        private void Rebuild(int count)
        {
            if (_texture != null)
                Destroy(_texture);
            Count = count;
            _texture = new(Chains.Joints, Mathf.Max(1, count), TextureFormat.RGBAFloat, false, true)
            {
                name = "ChainPositions",
                filterMode = FilterMode.Point,
                wrapMode = TextureWrapMode.Clamp,
            };
            if (_effect == null)
                return;
            _effect.SetTexture(ChainPositions, _texture);
            _effect.SetInt(ChainCount, count);
            _effect.SetInt(ChainLength, Chains.Joints);
        }
    }
}
