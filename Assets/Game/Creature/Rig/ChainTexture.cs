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
        [SerializeField] private MonoBehaviour[] _sources;
        [SerializeField] private bool _ownBudget;

        private readonly List<IChainSource> _chainSources = new(4);
        private readonly List<ChainTexture> _textures = new(2);

        private Texture2D _texture;

        public VisualEffect Effect => _effect;
        public Texture2D Texture => _texture;
        public int Count { get; private set; }
        public int Width { get; private set; }

        private int Requested => Chains.CountChains(_chainSources);

        private void Awake()
        {
            foreach (var source in _sources)
                if (source is IChainSource chainSource)
                    _chainSources.Add(chainSource);
            _chainSources.Sort((a, b) => a.ChainOrder.CompareTo(b.ChainOrder));
            GetComponents(_textures);
            if (TryGetComponent<CharacterMaterials>(out var materials))
                materials.Register(_effect);
        }

        private void OnDestroy()
        {
            if (_texture != null)
                Destroy(_texture);
        }

        private void LateUpdate()
        {
            var count = Mathf.Min(Requested, Budget());
            var width = Chains.Width(_chainSources);
            if (_texture == null || count != Count || width != Width)
                Rebuild(count, width);
            if (count == 0)
                return;
            Chains.Pack(_chainSources, _texture.GetPixelData<Vector4>(0).AsSpan(), count, width);
            _texture.Apply(false);
        }

        private int Budget()
        {
            if (_ownBudget)
                return CreatureRigSettings.MaxMawChains;
            var budget = CreatureRigSettings.MaxChains;
            foreach (var texture in _textures)
            {
                if (texture == this)
                    break;
                if (!texture._ownBudget)
                    budget -= texture.Requested;
            }
            return Mathf.Max(0, budget);
        }

        private void Rebuild(int count, int width)
        {
            if (_texture != null)
                Destroy(_texture);
            Count = count;
            Width = width;
            _texture = new(width, Mathf.Max(1, count), TextureFormat.RGBAFloat, false, true)
            {
                name = "ChainPositions",
                filterMode = FilterMode.Trilinear,
                wrapMode = TextureWrapMode.Clamp,
            };
            if (_effect == null)
                return;
            _effect.SetTexture(ChainPositions, _texture);
            _effect.SetInt(ChainCount, count);
            _effect.SetInt(ChainLength, width);
        }
    }
}
