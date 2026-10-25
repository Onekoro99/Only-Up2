using System;
using UnityEngine;

namespace OnlyUpPrototype
{
    [DefaultExecutionOrder(20)]
    [RequireComponent(typeof(PrototypePlayerMotor), typeof(FallTracker))]
    public sealed class StageRespawn : MonoBehaviour
    {
        [SerializeField] private Transform startPoint;
        [SerializeField] private float killHeight = -5f;
        [SerializeField] private ThirdPersonOrbitCamera orbitCamera;
        private PrototypePlayerMotor motor;
        private FallTracker falls;
        public int RespawnCount { get; private set; }
        public event Action Respawned;
        private void Awake() { motor = GetComponent<PrototypePlayerMotor>(); falls = GetComponent<FallTracker>(); }
        private void Update() { if (transform.position.y < killHeight) Respawn(); }
        public void Respawn()
        {
            if (!startPoint) return;
            falls.FinishForRespawn();
            motor.Teleport(startPoint.position, startPoint.rotation);
            falls.ResetTracking();
            if (orbitCamera) orbitCamera.SnapToTarget();
            RespawnCount++;
            Respawned?.Invoke();
        }
    }
}
