using System;
using TentacleVania.Core.Interaction;
using TentacleVania.Core.Runs;
using UnityEngine;

namespace TentacleVania.World.Run
{
    public sealed class RunExit : MonoBehaviour, IInteractable
    {
        [SerializeField] private Collider _blocker;
        [SerializeField] private Transform _barrier;
        [SerializeField] private Renderer _orb;
        [SerializeField, Min(0.5f)] private float _radius = 2f;
        [SerializeField] private Color _mutation = new(0.8f, 0.3f, 1f);
        [SerializeField] private Color _evolution = new(1f, 0.55f, 0.1f);
        [SerializeField] private Color _biomass = new(0.9f, 0.15f, 0.2f);
        [SerializeField] private Color _essence = new(0.2f, 0.9f, 1f);
        [SerializeField] private Color _shop = new(1f, 0.85f, 0.2f);
        [SerializeField] private Color _challenge = new(1f, 1f, 1f);
        [SerializeField] private Color _boss = new(0.6f, 0f, 0f);

        private Material _orbMaterial;

        public static event Action<RunExit> Chosen;

        public DoorOffer Offer { get; private set; }
        public bool HasOffer { get; private set; }
        public bool IsOpen { get; private set; }
        public Vector3 Position => transform.position;
        public float Radius => _radius;
        public string PromptKey => "run.door." + Offer.Reward.ToString().ToLowerInvariant();
        public bool CanInteract => IsOpen && HasOffer;

        private void Awake()
        {
            if (_orb != null)
                _orbMaterial = _orb.material;
            Show(false);
        }

        private void OnEnable() => Interactions.Register(this);

        private void OnDisable() => Interactions.Unregister(this);

        private void OnDestroy()
        {
            if (_orbMaterial != null)
                Destroy(_orbMaterial);
        }

        public void Assign(DoorOffer offer)
        {
            Offer = offer;
            HasOffer = true;
            IsOpen = false;
            if (_orb != null)
            {
                _orb.gameObject.SetActive(true);
                var color = ColorFor(offer);
                _orbMaterial.SetColor("_BaseColor", color);
                _orbMaterial.SetColor("_EmissionColor", color * 2f);
            }
            Show(false);
        }

        public void Clear()
        {
            HasOffer = false;
            IsOpen = false;
            if (_orb != null)
                _orb.gameObject.SetActive(false);
            Show(false);
        }

        public void Open()
        {
            if (!HasOffer)
                return;
            IsOpen = true;
            Show(true);
        }

        public void Interact()
        {
            if (CanInteract)
                Chosen?.Invoke(this);
        }

        private void Show(bool open)
        {
            if (_barrier != null)
                _barrier.gameObject.SetActive(!open);
        }

        private Color ColorFor(DoorOffer offer) => offer.Role == RoomRole.Boss ? _boss : offer.Reward switch
        {
            RunReward.Mutation => _mutation,
            RunReward.Evolution => _evolution,
            RunReward.Biomass => _biomass,
            RunReward.Essence => _essence,
            RunReward.Shop => _shop,
            RunReward.Challenge => _challenge,
            _ => Color.grey,
        };
    }
}
