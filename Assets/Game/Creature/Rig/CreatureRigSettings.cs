using TentacleVania.Core.Characters;
using UnityEngine;

namespace TentacleVania.Creature.Rig
{
    [CreateAssetMenu(menuName = "TentacleVania/Creature/Rig Settings")]
    public sealed class CreatureRigSettings : ScriptableObject
    {
        public const int MaxChains = 20;
        public const int MaxMawChains = 20;

        [Header("Legs")]
        [SerializeField, Range(2, MaxChains)] private int _legCount = 6;
        [SerializeField, Min(0.1f)] private float _legLength = 0.9f;
        [SerializeField, Min(0f)] private float _rootRadius = 0.22f;
        [SerializeField, Min(0f)] private float _rootHeight = 0.42f;
        [SerializeField, Min(0.1f)] private float _restRadius = 0.62f;
        [SerializeField, Min(0f)] private float _archHeight = 0.3f;

        [Header("Stepping")]
        [SerializeField, Min(0.01f)] private float _stepThreshold = 0.28f;
        [SerializeField, Min(0.01f)] private float _emergencyDistance = 0.6f;
        [SerializeField, Min(0.01f)] private float _stepDuration = 0.11f;
        [SerializeField, Min(0f)] private float _stepHeight = 0.18f;
        [SerializeField, Min(0f)] private float _predictTime = 0.12f;

        [Header("Organic motion")]
        [SerializeField, Range(0f, 0.5f)] private float _variation = 0.2f;
        [SerializeField, Range(0.3f, 1f)] private float _liftCurve = 0.55f;
        [SerializeField, Min(0f)] private float _swingOutward = 0.35f;
        [SerializeField, Min(0f)] private float _writheAmplitude = 0.06f;
        [SerializeField, Min(0f)] private float _writheSpeed = 1.2f;
        [SerializeField, Min(0f)] private float _bodyBob = 0.05f;
        [SerializeField, Min(0f)] private float _bodySway = 0.12f;
        [SerializeField, Min(0.1f)] private float _bodySpringFrequency = 3f;
        [SerializeField, Range(0.05f, 1f)] private float _bodySpringDamping = 0.3f;
        [SerializeField, Min(0f)] private float _bodyInertia = 0.25f;
        [SerializeField, Min(0f)] private float _bodyMaxOffset = 0.15f;

        [Header("Solver")]
        [SerializeField, Range(1, 20)] private int _solverIterations = 6;

        [Header("Free tentacles")]
        [SerializeField, Range(0, MaxChains)] private int _freeCount = 3;
        [SerializeField, Min(0.1f)] private float _freeLength = 0.9f;
        [SerializeField, Min(0f)] private float _freeRootHeight = 0.38f;
        [SerializeField, Range(0f, 360f)] private float _freeSpread = 110f;
        [SerializeField, Range(-90f, 90f)] private float _freePitch = 40f;
        [SerializeField, Range(-180f, 180f)] private float _freeCurl = 70f;
        [SerializeField, Min(0f)] private float _freeSway = 14f;
        [SerializeField, Min(0f)] private float _freeSwaySpeed = 0.7f;
        [SerializeField, Min(0.001f)] private float _freeRootWidth = 0.07f;
        [SerializeField, Min(0.001f)] private float _freeTipWidth = 0.01f;
        [SerializeField] private SpringChainSettings _freeSpring = new()
        {
            PoseStiffness = 14f,
            TipLooseness = 0.75f,
            Damping = 0.1f,
            Gravity = 3f,
            Iterations = 1,
        };

        [Header("Dash stretch")]
        [SerializeField, Min(1f)] private float _dashStretch = 2f;
        [SerializeField, Min(0.001f)] private float _dashRise = 0.04f;
        [SerializeField, Min(0.1f)] private float _dashRecoverFrequency = 2.5f;
        [SerializeField, Range(0.05f, 1f)] private float _dashRecoverDamping = 0.5f;
        [SerializeField, Range(0.1f, 1f)] private float _dashThinning = 0.5f;
        [SerializeField, Range(0f, 90f)] private float _dashFan = 30f;
        [SerializeField, Min(0f)] private float _dashTrailHeight = 0.15f;

        [Header("Maw")]
        [SerializeField, Range(0, MaxMawChains)] private int _mawCount = 8;
        [SerializeField] private Vector3 _mawOffset = new(0f, 0.45f, 0.35f);
        [SerializeField, Min(0f)] private float _mawRootRadius = 0.25f;
        [SerializeField, Min(0f)] private float _mawOpenMargin = 0.35f;
        [SerializeField] private Vector2 _mawOpenRadius = new(0.6f, 1.4f);
        [SerializeField, Range(0f, 1f)] private float _mawOpenReach = 0.55f;
        [SerializeField, Min(0f)] private float _mawPinch = 0.06f;
        [SerializeField, Min(0f)] private float _mawWrap = 1.1f;
        [SerializeField, Min(0f)] private float _mawLungeStop = 0.5f;
        [SerializeField, Min(0.001f)] private float _mawRootWidth = 0.1f;
        [SerializeField, Min(0.001f)] private float _mawTipWidth = 0.02f;
        [SerializeField, Min(0.01f)] private float _mawOpenTime = 0.3f;
        [SerializeField, Min(0.01f)] private float _mawCloseTime = 0.2f;
        [SerializeField, Min(0.01f)] private float _mawSwallowTime = 0.4f;
        [SerializeField, Min(0.01f)] private float _mawRetractTime = 0.2f;
        [SerializeField] private SpringChainSettings _mawSpring = new()
        {
            PoseStiffness = 30f,
            TipLooseness = 0.5f,
            Damping = 0.15f,
            Gravity = 1f,
            Iterations = 3,
        };
        [SerializeField] private SpringChainSettings _mawWhipSpring = new()
        {
            PoseStiffness = 14f,
            TipLooseness = 0.85f,
            Damping = 0.04f,
            Gravity = 1f,
            Iterations = 2,
        };

        [Header("Maw organic motion")]
        [SerializeField] private Vector2Int _mawLeaders = new(2, 3);
        [SerializeField, Range(0.1f, 1f)] private float _mawLeaderStrike = 0.6f;
        [SerializeField, Range(0f, 2f)] private float _mawLeaderHug = 0.9f;
        [SerializeField, Min(0f)] private float _mawAnticipation = 0.06f;
        [SerializeField, Min(0f)] private float _mawRearBack = 0.15f;
        [SerializeField, Range(0f, 90f)] private float _mawAngleJitter = 20f;
        [SerializeField, Range(0f, 1f)] private float _mawRootJitter = 0.3f;
        [SerializeField, Range(0f, 1f)] private float _mawLengthJitter = 0.25f;
        [SerializeField, Range(0f, 1f)] private float _mawWidthJitter = 0.3f;
        [SerializeField, Range(0f, 0.9f)] private float _mawTimingJitter = 0.4f;
        [SerializeField, Min(0f)] private float _mawOvershoot = 1.7f;
        [SerializeField, Min(0f)] private float _mawSeekWrithe = 0.12f;
        [SerializeField, Range(0f, 1f)] private float _mawSeekLean = 0.2f;
        [SerializeField, Min(0f)] private float _mawWrithe = 0.1f;
        [SerializeField, Min(0f)] private float _mawWritheSpeed = 5f;

        [Header("Placeholder rendering")]
        [SerializeField, Min(0.001f)] private float _rootWidth = 0.1f;
        [SerializeField, Min(0.001f)] private float _tipWidth = 0.025f;

        public int LegCount => _legCount;
        public int BonesPerLeg => Chains.Joints - 1;
        public int JointsPerLeg => Chains.Joints;
        public float LegLength => _legLength;
        public float BoneLength => _legLength / BonesPerLeg;
        public float RootRadius => _rootRadius;
        public float RootHeight => _rootHeight;
        public float RestRadius => _restRadius;
        public float ArchHeight => _archHeight;
        public float StepThreshold => _stepThreshold;
        public float EmergencyDistance => _emergencyDistance;
        public float StepDuration => _stepDuration;
        public float StepHeight => _stepHeight;
        public float PredictTime => _predictTime;
        public float Variation => _variation;
        public float LiftCurve => _liftCurve;
        public float SwingOutward => _swingOutward;
        public float WritheAmplitude => _writheAmplitude;
        public float WritheSpeed => _writheSpeed;
        public float BodyBob => _bodyBob;
        public float BodySway => _bodySway;
        public float BodySpringFrequency => _bodySpringFrequency;
        public float BodySpringDamping => _bodySpringDamping;
        public float BodyInertia => _bodyInertia;
        public float BodyMaxOffset => _bodyMaxOffset;
        public int SolverIterations => _solverIterations;
        public int FreeCount => _freeCount;
        public float FreeLength => _freeLength;
        public float FreeRootHeight => _freeRootHeight;
        public float FreeSpread => _freeSpread;
        public float FreePitch => _freePitch;
        public float FreeCurl => _freeCurl;
        public float FreeSway => _freeSway;
        public float FreeSwaySpeed => _freeSwaySpeed;
        public float FreeRootWidth => _freeRootWidth;
        public float FreeTipWidth => _freeTipWidth;
        public SpringChainSettings FreeSpring => _freeSpring;
        public float DashStretch => _dashStretch;
        public float DashRise => _dashRise;
        public float DashRecoverFrequency => _dashRecoverFrequency;
        public float DashRecoverDamping => _dashRecoverDamping;
        public float DashThinning => _dashThinning;
        public float DashFan => _dashFan;
        public float DashTrailHeight => _dashTrailHeight;
        public int MawCount => _mawCount;
        public Vector3 MawOffset => _mawOffset;
        public float MawRootRadius => _mawRootRadius;
        public float MawOpenMargin => _mawOpenMargin;
        public Vector2 MawOpenRadius => _mawOpenRadius;
        public float MawOpenReach => _mawOpenReach;
        public float MawPinch => _mawPinch;
        public float MawWrap => _mawWrap;
        public float MawLungeStop => _mawLungeStop;
        public float MawRootWidth => _mawRootWidth;
        public float MawTipWidth => _mawTipWidth;
        public MawTimeline MawTimeline => new(_mawOpenTime, _mawCloseTime, _mawSwallowTime, _mawRetractTime);
        public SpringChainSettings MawSpring => _mawSpring;
        public SpringChainSettings MawWhipSpring => _mawWhipSpring;
        public Vector2Int MawLeaders => _mawLeaders;
        public float MawLeaderStrike => _mawLeaderStrike;
        public float MawLeaderHug => _mawLeaderHug;
        public float MawAnticipation => _mawAnticipation;
        public float MawRearBack => _mawRearBack;
        public float MawAngleJitter => _mawAngleJitter;
        public float MawRootJitter => _mawRootJitter;
        public float MawLengthJitter => _mawLengthJitter;
        public float MawWidthJitter => _mawWidthJitter;
        public float MawTimingJitter => _mawTimingJitter;
        public float MawOvershoot => _mawOvershoot;
        public float MawSeekWrithe => _mawSeekWrithe;
        public float MawSeekLean => _mawSeekLean;
        public float MawWrithe => _mawWrithe;
        public float MawWritheSpeed => _mawWritheSpeed;
        public float RootWidth => _rootWidth;
        public float TipWidth => _tipWidth;

        public float LegAngle(int leg) => 360f / _legCount * (leg + 0.5f);
    }
}
