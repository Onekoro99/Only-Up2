using System;
using UnityEngine;

namespace OnlyUpPrototype
{
    [Serializable]
    public struct FallReport
    {
        public float DepartureHeight;
        public float StartHeight;
        public float EndHeight;
        public float Distance;
        public bool IsSignificant;
        public bool EndedByRespawn;
    }

    [RequireComponent(typeof(PrototypePlayerMotor))]
    public sealed class FallTracker : MonoBehaviour
    {
        [SerializeField, Min(0f)] private float significantFallDistance = 3f;
        private PrototypePlayerMotor motor;
        private bool airborne;
        private float departureHeight;
        private float peakHeight;
        public bool IsFalling { get; private set; }
        public bool Airborne => airborne;
        public float DepartureHeight => departureHeight;
        public float PeakHeight => peakHeight;
        public float CurrentFallDistance => IsFalling ? Mathf.Max(0f, peakHeight - transform.position.y) : 0f;
        public FallReport LastReport { get; private set; }
        public event Action<float> FallStarted;
        public event Action<FallReport> FallCompleted;

        private void Awake() { motor = GetComponent<PrototypePlayerMotor>(); ResetTracking(); }
        private void Update() { Sample(motor.IsGrounded, motor.VerticalSpeed); }
        public void Sample(bool grounded, float verticalSpeed)
        {
            float height = transform.position.y;
            if (!grounded)
            {
                if (!airborne) { airborne = true; peakHeight = departureHeight; }
                peakHeight = Mathf.Max(peakHeight, height);
                if (!IsFalling && verticalSpeed < 0f)
                {
                    IsFalling = true;
                    FallStarted?.Invoke(peakHeight);
                }
            }
            else
            {
                if (airborne && IsFalling) Complete(height, false);
                airborne = false;
                departureHeight = peakHeight = height;
                IsFalling = false;
            }
        }

        public void FinishForRespawn()
        {
            if (airborne) Complete(transform.position.y, true);
            ResetTracking();
        }
        private void Complete(float height, bool respawned)
        {
            float distance = Mathf.Max(0f, peakHeight - height);
            LastReport = new FallReport { DepartureHeight = departureHeight, StartHeight = peakHeight,
                EndHeight = height, Distance = distance, IsSignificant = distance >= significantFallDistance, EndedByRespawn = respawned };
            IsFalling = false;
            airborne = false;
            FallCompleted?.Invoke(LastReport);
        }
        public void ResetTracking()
        {
            airborne = IsFalling = false;
            departureHeight = peakHeight = transform.position.y;
        }
        public void RestoreRuntimeState(bool wasAirborne, bool wasFalling, float departure, float peak)
        {
            airborne = wasAirborne;
            IsFalling = wasFalling;
            departureHeight = departure;
            peakHeight = Mathf.Max(peak, transform.position.y);
        }
    }
}
