using System;
using UnityEngine;

namespace OnlyUpPrototype
{
    [DefaultExecutionOrder(20)]
    [RequireComponent(typeof(PrototypePlayerMotor), typeof(FallTracker))]
    public sealed class StageRespawn : MonoBehaviour
    {
        [SerializeField]
        [Header("マップ外からの復帰")]
        [Tooltip("マップ外まで落ちた場合などに戻る地点のTransformです。位置と向きを復元します。未設定なら復帰処理は行いません。")]
        private Transform startPoint;
        [SerializeField]
        [Tooltip("プレイヤーのY座標がこの値より低くなると復帰する境界（m）です。大きいほど浅い落下で復帰し、小さいほど深く落ちるまで待ちます。地面より低く設定してください。")]
        private float killHeight = -5f;
        [SerializeField]
        [Tooltip("復帰後にプレイヤー位置へすぐ合わせる三人称カメラです。未設定の場合は復帰直後のカメラ補正だけ行いません。")]
        private ThirdPersonOrbitCamera orbitCamera;
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
