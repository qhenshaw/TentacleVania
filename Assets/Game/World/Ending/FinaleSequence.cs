using System.Collections;
using FMODUnity;
using PrimeTween;
using TentacleVania.Core;
using TentacleVania.Core.Progression;
using TentacleVania.Core.Saves;
using TentacleVania.Core.Scenes;
using TentacleVania.World.BioNests;
using UnityEngine;

namespace TentacleVania.World.Ending
{
    public sealed class FinaleSequence : MonoBehaviour
    {
        [Tooltip("Boss whose defeat starts the finale (design: Crane; the hive erupts and Mother is freed).")]
        [SerializeField] private string _bossId = "MV/Crane";
        [Tooltip("Nest the creature wakes at after the credits (design: the Vault's pre-boss nest; the world stays as it was, no New Game+).")]
        [SerializeField] private string _wakeNest = "MV/PreBossNest";
        [SerializeField] private GameObject _glass;
        [SerializeField] private Transform _mother;
        [SerializeField, Min(0f)] private float _absorbWait = 3f;
        [SerializeField, Min(0f)] private float _revealTime = 3f;
        [SerializeField, Min(0f)] private float _eruptionTime = 9f;
        [SerializeField, Min(1f)] private float _motherGrowth = 1.6f;
        [SerializeField] private EventReference _shatterSound;

        private bool _running;
        private bool _creditsDone;

        public static bool Playing { get; private set; }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics() => Playing = false;

        private void OnEnable()
        {
            BossProgress.Defeated += OnDefeated;
            Finale.CreditsFinished += OnCreditsFinished;
            if (Finale.Completed && _glass != null)
                _glass.SetActive(false);
        }

        private void OnDisable()
        {
            BossProgress.Defeated -= OnDefeated;
            Finale.CreditsFinished -= OnCreditsFinished;
            if (_running)
            {
                Cutscene.End(this);
                Playing = false;
            }
        }

        private void OnDefeated(string id)
        {
            if (id == _bossId && !_running)
                StartCoroutine(Run());
        }

        public void Play()
        {
            if (!_running)
                StartCoroutine(Run());
        }

        private void OnCreditsFinished() => _creditsDone = true;

        private IEnumerator Run()
        {
            _running = true;
            Playing = true;
            _creditsDone = false;
            yield return new WaitForSeconds(_absorbWait);
            Cutscene.Begin(this);
            CameraFocus.Target = _mother != null ? _mother : transform;
            if (_glass != null)
                _glass.SetActive(false);
            if (!_shatterSound.IsNull)
                RuntimeManager.PlayOneShot(_shatterSound, transform.position);
            if (_mother != null)
                Tween.Scale(_mother, _mother.localScale * _motherGrowth, _revealTime, Ease.OutSine);
            yield return new WaitForSeconds(_revealTime + _eruptionTime);
            Finale.RollCredits();
            while (!_creditsDone)
                yield return null;
            Finale.Complete();
            WakeAtNest();
            CameraFocus.Target = null;
            Cutscene.End(this);
            Playing = false;
            _running = false;
            Autosave.Request(AutosaveReason.BossDefeated);
            SceneFlow.Reload();
        }

        private void WakeAtNest()
        {
            foreach (var nest in FindObjectsByType<BioNest>())
            {
                if (nest.SaveId != _wakeNest)
                    continue;
                if (!nest.IsGrown)
                    nest.Grow();
                nest.SetCheckpoint();
                return;
            }
        }
    }
}
