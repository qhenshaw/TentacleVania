using FMODUnity;
using TentacleVania.Core.Interaction;
using TentacleVania.Core.Progression;
using TentacleVania.World.Map;
using UnityEngine;

namespace TentacleVania.World.Transit
{
    public sealed class SectorElevator : MonoBehaviour, IInteractable
    {
        public const string RidePromptKey = "prompt.elevator.ride";
        public const string LockedPromptKey = "prompt.elevator.locked";
        public const string SealedPromptKey = "prompt.transit.sealed";

        [Tooltip("Boss id whose defeat powers the elevator (Warden); empty = always powered.")]
        [SerializeField] private string _requiredBoss = "SH/WardenDrone";
        [Tooltip("Ability id also needed (TentacleGrapple...); empty = none.")]
        [SerializeField] private string _requiredAbility;
        [Tooltip("Root scene of the destination sector.")]
        [SerializeField] private string _destinationScene;
        [Tooltip("Where the creature arrives in the destination scene (the paired elevator's platform).")]
        [SerializeField] private Vector3 _arrivalPosition;
        [SerializeField] private float _arrivalYaw;
        [SerializeField, Min(0.1f)] private float _radius = 2f;
        [Tooltip("Placeholder lamp: lit while powered.")]
        [SerializeField] private Renderer _powerLamp;
        [SerializeField] private Material _poweredMaterial;
        [SerializeField] private Material _unpoweredMaterial;
        [SerializeField] private EventReference _rideSound;
        [Tooltip("Elevator, or Corridor for same-floor transit doors (transition label).")]
        [SerializeField] private TravelKind _travelKind = TravelKind.Elevator;
        [Tooltip("Prompt key while locked (empty: the elevator's).")]
        [SerializeField] private string _lockedPromptKey;

        public bool Powered => (string.IsNullOrEmpty(_requiredBoss) || BossProgress.IsDefeated(_requiredBoss))
            && (string.IsNullOrEmpty(_requiredAbility) || BossProgress.IsAbilityUnlocked(_requiredAbility));
        public string DestinationScene => _destinationScene;
        public Vector3 Position => transform.position;
        public float Radius => _radius;
        public bool Reachable => !string.IsNullOrEmpty(_destinationScene) && Application.CanStreamedLevelBeLoaded(_destinationScene);
        public string PromptKey => !Reachable ? SealedPromptKey : Powered ? (_travelKind == TravelKind.Corridor ? "prompt.transit.enter" : RidePromptKey) : string.IsNullOrEmpty(_lockedPromptKey) ? LockedPromptKey : _lockedPromptKey;
        public bool CanInteract => isActiveAndEnabled && !string.IsNullOrEmpty(_destinationScene);

        private void OnEnable()
        {
            Interactions.Register(this);
            BossProgress.Defeated += OnBossDefeated;
            RefreshLamp();
        }

        private void OnDisable()
        {
            Interactions.Unregister(this);
            BossProgress.Defeated -= OnBossDefeated;
        }

        public void Configure(string destinationScene, Vector3 arrivalPosition, float arrivalYaw, string requiredBoss, string requiredAbility = null)
        {
            _requiredAbility = requiredAbility ?? "";
            _destinationScene = destinationScene;
            _arrivalPosition = arrivalPosition;
            _arrivalYaw = arrivalYaw;
            _requiredBoss = requiredBoss;
            RefreshLamp();
        }

        public void Interact() => Ride();

        public bool Ride()
        {
            if (!Powered || !Reachable || !FastTravel.Transit(_destinationScene, _arrivalPosition, _arrivalYaw, _travelKind))
                return false;
            if (!_rideSound.IsNull)
                RuntimeManager.PlayOneShot(_rideSound, transform.position);
            return true;
        }

        private void OnBossDefeated(string id) => RefreshLamp();

        private void RefreshLamp()
        {
            if (_powerLamp == null)
                return;
            var material = Powered ? _poweredMaterial : _unpoweredMaterial;
            if (material != null)
                _powerLamp.sharedMaterial = material;
        }

        private void OnDrawGizmos()
        {
            Gizmos.color = Powered ? new Color(0.3f, 1f, 0.5f, 0.7f) : new Color(1f, 0.4f, 0.2f, 0.7f);
            Gizmos.DrawWireSphere(transform.position, _radius);
        }
    }
}
