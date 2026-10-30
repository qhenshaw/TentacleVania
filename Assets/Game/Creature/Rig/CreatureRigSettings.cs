using TentacleVania.Core.Characters;
using UnityEngine;

namespace TentacleVania.Creature.Rig
{
    [CreateAssetMenu(menuName = "TentacleVania/Creature/Rig Settings")]
    public sealed class CreatureRigSettings : ScriptableObject
    {
        public const int MaxChains = 20;

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
        public float RootWidth => _rootWidth;
        public float TipWidth => _tipWidth;

        public float LegAngle(int leg) => 360f / _legCount * (leg + 0.5f);
    }
}
