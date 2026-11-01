using FMODUnity;
using TentacleVania.Core.Interaction;
using TentacleVania.Core.Progression;
using TentacleVania.World.Map;
using UnityEngine;

namespace TentacleVania.World.Transit
{
    public sealed class FreightCrate : MonoBehaviour, IInteractable
    {
        public const string RidePromptKey = "prompt.crate.ride";
        public const string LockedPromptKey = "prompt.crate.locked";
        public const string ClearanceId = "FreightClearance";

        [Tooltip("Root scene of the destination sector (each station has one fixed route).")]
        [SerializeField] private string _destinationScene;
        [Tooltip("Where the creature climbs out in the destination scene (the paired station).")]
        [SerializeField] private Vector3 _arrivalPosition;
        [SerializeField] private float _arrivalYaw;
        [SerializeField, Min(0.1f)] private float _radius = 2f;
        [Tooltip("Placeholder lamp: lit once the creature has Freight Clearance.")]
        [SerializeField] private Renderer _lamp;
        [SerializeField] private Material _clearedMaterial;
        [SerializeField] private Material _lockedMaterial;
        [SerializeField] private EventReference _rideSound;

        public static bool ForceCleared { get; set; }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics() => ForceCleared = false;

        public static bool Cleared => ForceCleared || BossProgress.IsAbilityUnlocked(ClearanceId);

        public string DestinationScene => _destinationScene;
        public Vector3 Position => transform.position;
        public float Radius => _radius;
        public bool Reachable => !string.IsNullOrEmpty(_destinationScene) && Application.CanStreamedLevelBeLoaded(_destinationScene);
        public string PromptKey => !Reachable ? SectorElevator.SealedPromptKey : Cleared ? RidePromptKey : LockedPromptKey;
        public bool CanInteract => isActiveAndEnabled && !string.IsNullOrEmpty(_destinationScene);

        private void OnEnable()
        {
            Interactions.Register(this);
            BossProgress.AbilityUnlocked += OnAbilityUnlocked;
            RefreshLamp();
        }

        private void OnDisable()
        {
            Interactions.Unregister(this);
            BossProgress.AbilityUnlocked -= OnAbilityUnlocked;
        }

        public void Configure(string destinationScene, Vector3 arrivalPosition, float arrivalYaw)
        {
            _destinationScene = destinationScene;
            _arrivalPosition = arrivalPosition;
            _arrivalYaw = arrivalYaw;
            RefreshLamp();
        }

        public void Interact() => Ride();

        public bool Ride()
        {
            if (!Cleared || !Reachable || !FastTravel.Transit(_destinationScene, _arrivalPosition, _arrivalYaw, TravelKind.Freight))
                return false;
            if (!_rideSound.IsNull)
                RuntimeManager.PlayOneShot(_rideSound, transform.position);
            return true;
        }

        private void OnAbilityUnlocked(string id) => RefreshLamp();

        private void RefreshLamp()
        {
            if (_lamp == null)
                return;
            var material = Cleared ? _clearedMaterial : _lockedMaterial;
            if (material != null)
                _lamp.sharedMaterial = material;
        }

        private void OnDrawGizmos()
        {
            Gizmos.color = new Color(0.1f, 0.8f, 0.75f, 0.7f);
            Gizmos.DrawWireSphere(transform.position, _radius);
        }
    }
}
