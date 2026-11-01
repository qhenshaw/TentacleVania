using System;
using FMODUnity;
using TentacleVania.Core.Interaction;
using UnityEngine;

namespace TentacleVania.World.Hoarder
{
    public sealed class HoarderMerchant : MonoBehaviour, IInteractable
    {
        public const string PromptTradeKey = "prompt.hoarder.trade";

        [SerializeField] private HoarderStock _stock;
        [SerializeField, Range(0, 3)] private int _floor;
        [SerializeField, Min(0.1f)] private float _radius = 2.2f;
        [Tooltip("Placeholder hands, bobbed and twitched while the creature is near.")]
        [SerializeField] private Transform[] _hands = Array.Empty<Transform>();
        [SerializeField] private EventReference _chitter;

        private Vector3[] _handBase = Array.Empty<Vector3>();

        public static event Action<HoarderMerchant> TradeRequested;

        public HoarderStock Stock => _stock;
        public int Floor => _floor;

        public bool Sells(StockEntry entry) => entry != null && entry.MinFloor <= _floor && Core.Progression.Shop.InStock(entry.Kind, entry.Mutation, entry.Trinket);
        public Vector3 Position => transform.position;
        public float Radius => _radius;
        public string PromptKey => PromptTradeKey;
        public bool CanInteract => isActiveAndEnabled && _stock != null;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics() => TradeRequested = null;

        private void OnEnable()
        {
            Interactions.Register(this);
            _handBase = new Vector3[_hands.Length];
            for (var i = 0; i < _hands.Length; i++)
                if (_hands[i] != null)
                    _handBase[i] = _hands[i].localPosition;
        }

        private void OnDisable() => Interactions.Unregister(this);

        private void Update()
        {
            for (var i = 0; i < _hands.Length; i++)
            {
                if (_hands[i] == null)
                    continue;
                var t = Time.time * (2.1f + i * 0.7f) + i;
                _hands[i].localPosition = _handBase[i] + new Vector3(Mathf.Sin(t) * 0.04f, Mathf.Sin(t * 1.7f) * 0.06f, 0f);
            }
        }

        public void Interact()
        {
            Chitter();
            TradeRequested?.Invoke(this);
        }

        public void Chitter()
        {
            if (!_chitter.IsNull)
                RuntimeManager.PlayOneShot(_chitter, transform.position);
        }
    }
}
