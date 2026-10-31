using FMODUnity;
using TentacleVania.Core.Interaction;
using TentacleVania.Core.Progression;
using TentacleVania.World.Map;
using UnityEngine;

namespace TentacleVania.World.Shortcuts
{
    public sealed class Crawlspace : MonoBehaviour, IInteractable
    {
        public const string OpenPromptKey = "prompt.shortcut.open";
        public const string CrawlPromptKey = "prompt.crawlspace.enter";

        [Tooltip("Shortcut id shared by both ends (saved through ShortcutProgress).")]
        [SerializeField] private string _id;
        [Tooltip("This end can open the crawlspace (the far side); the other end waits until it is open.")]
        [SerializeField] private bool _opensHere;
        [Tooltip("Optional: the crawlspace opens when this nest is grown (Specimen Holding T1: SH/PreBossNest).")]
        [SerializeField] private string _openOnNestGrown;
        [Tooltip("Where the creature comes out: the other end's exit point.")]
        [SerializeField] private Vector3 _exitPosition;
        [SerializeField] private float _exitYaw;
        [SerializeField, Min(0.1f)] private float _radius = 1.8f;
        [SerializeField] private Transform _grate;
        [SerializeField] private Vector3 _grateOpenOffset = new(0f, 0.9f, 0f);
        [SerializeField] private EventReference _openSound;

        private static readonly System.Collections.Generic.List<Vector3> Route = new(16);

        private Vector3 _grateClosed;

        public string Id => _id;
        public bool IsOpen => ShortcutProgress.IsOpen(_id);
        public Vector3 Position => transform.position;
        public float Radius => _radius;
        public string PromptKey => IsOpen ? CrawlPromptKey : OpenPromptKey;
        public bool CanInteract => isActiveAndEnabled && (IsOpen || _opensHere);

        private void OnEnable()
        {
            Interactions.Register(this);
            ShortcutProgress.Opened += OnOpened;
            Nests.Grown += OnNestGrown;
            if (!string.IsNullOrEmpty(_openOnNestGrown) && Nests.IsGrown(_openOnNestGrown))
                ShortcutProgress.Open(_id);
            if (_grate != null)
                _grateClosed = _grate.localPosition;
            RefreshGrate();
        }

        private void OnDisable()
        {
            Interactions.Unregister(this);
            ShortcutProgress.Opened -= OnOpened;
            Nests.Grown -= OnNestGrown;
        }

        public void Configure(string id, bool opensHere, Vector3 exitPosition, float exitYaw)
        {
            _id = id;
            _opensHere = opensHere;
            _exitPosition = exitPosition;
            _exitYaw = exitYaw;
        }

        public void Interact()
        {
            if (!IsOpen)
            {
                if (!_opensHere || !ShortcutProgress.Open(_id))
                    return;
                if (!_openSound.IsNull)
                    RuntimeManager.PlayOneShot(_openSound, transform.position);
                return;
            }
            var streamer = Streaming.RoomStreamer.Active;
            if (streamer == null)
            {
                FastTravel.Crawl(_exitPosition, _exitYaw);
                return;
            }
            TunnelRoute.Build(streamer.Registry.Rooms, transform.position, _exitPosition, Route);
            FastTravel.Tunnel(Route, _exitPosition, _exitYaw);
        }

        private void OnNestGrown(string nest)
        {
            if (nest == _openOnNestGrown)
                ShortcutProgress.Open(_id);
        }

        private void OnOpened(string id)
        {
            if (id == _id)
                RefreshGrate();
        }

        private void RefreshGrate()
        {
            if (_grate != null)
                _grate.localPosition = _grateClosed + (IsOpen ? _grateOpenOffset : Vector3.zero);
        }
    }
}
