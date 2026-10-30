using System;
using UnityEngine;
using UnityEngine.InputSystem;

namespace TentacleVania.Core.Feedback
{
    public static class HitStop
    {
        public const float StoppedScale = 0.02f;

        public static bool Enabled { get; set; } = true;
        public static bool IsStopped => FeedbackRunner.StopRemaining > 0f;

        public static void Request(float duration)
        {
            if (!Enabled || duration <= 0f)
                return;
            FeedbackRunner.Ensure().BeginStop(duration);
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics() => Enabled = true;
    }

    public static class Rumble
    {
        public static void Play(float low, float high, float duration)
        {
            if (!GameSettings.Rumble || duration <= 0f || (low <= 0f && high <= 0f))
                return;
            FeedbackRunner.Ensure().BeginRumble(low, high, duration);
        }

        public static void Stop()
        {
            if (FeedbackRunner.Instance != null)
                FeedbackRunner.Instance.EndRumble();
        }
    }

    public static class ScreenShake
    {
        public delegate void ShakeHandler(Vector3 origin, float strength);

        public static event ShakeHandler Requested;

        public static void Request(Vector3 origin, float strength)
        {
            if (strength > 0f)
                Requested?.Invoke(origin, strength);
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics() => Requested = null;
    }

    [AddComponentMenu("")]
    internal sealed class FeedbackRunner : MonoBehaviour
    {
        public static FeedbackRunner Instance { get; private set; }
        public static float StopRemaining => Instance != null ? Instance._stopRemaining : 0f;

        private float _stopRemaining;
        private float _resumeScale = 1f;
        private float _rumbleRemaining;
        private float _rumbleLow;
        private float _rumbleHigh;
        private Gamepad _rumblePad;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics() => Instance = null;

        public static FeedbackRunner Ensure()
        {
            if (Instance != null)
                return Instance;
            var host = new GameObject("FeedbackRunner");
            DontDestroyOnLoad(host);
            Instance = host.AddComponent<FeedbackRunner>();
            return Instance;
        }

        public void BeginStop(float duration)
        {
            if (_stopRemaining <= 0f)
            {
                _resumeScale = Time.timeScale;
                Time.timeScale = _resumeScale * HitStop.StoppedScale;
            }
            _stopRemaining = Mathf.Max(_stopRemaining, duration);
        }

        public void BeginRumble(float low, float high, float duration)
        {
            var pad = Gamepad.current;
            if (pad == null)
                return;
            if (_rumbleRemaining > 0f && _rumblePad != pad)
                EndRumble();
            _rumblePad = pad;
            _rumbleLow = Mathf.Max(_rumbleRemaining > 0f ? _rumbleLow : 0f, Mathf.Clamp01(low));
            _rumbleHigh = Mathf.Max(_rumbleRemaining > 0f ? _rumbleHigh : 0f, Mathf.Clamp01(high));
            _rumbleRemaining = Mathf.Max(_rumbleRemaining, duration);
            pad.SetMotorSpeeds(_rumbleLow, _rumbleHigh);
        }

        public void EndRumble()
        {
            _rumbleRemaining = 0f;
            if (_rumblePad != null && _rumblePad.added)
                _rumblePad.ResetHaptics();
            _rumblePad = null;
        }

        private void Update()
        {
            var dt = Time.unscaledDeltaTime;
            if (_stopRemaining > 0f)
            {
                _stopRemaining -= dt;
                if (_stopRemaining <= 0f)
                    EndStop();
            }
            if (_rumbleRemaining > 0f)
            {
                _rumbleRemaining -= dt;
                if (_rumbleRemaining <= 0f || !GameSettings.Rumble)
                    EndRumble();
            }
        }

        private void EndStop()
        {
            _stopRemaining = 0f;
            if (Mathf.Approximately(Time.timeScale, _resumeScale * HitStop.StoppedScale))
                Time.timeScale = _resumeScale;
        }

        private void OnApplicationFocus(bool focused)
        {
            if (!focused)
                EndRumble();
        }

        private void OnDestroy()
        {
            if (_stopRemaining > 0f)
                EndStop();
            EndRumble();
            if (Instance == this)
                Instance = null;
        }
    }
}
