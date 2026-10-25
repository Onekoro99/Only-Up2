using System;
using UnityEngine;

namespace OnlyUpPrototype
{
    public sealed class PrototypeGoal : MonoBehaviour
    {
        [SerializeField]
        [Header("ゴールの到達判定")]
        [Tooltip("ゴール範囲に入ったかを確認するプレイヤーのTransformです。指定した位置が範囲内に入ると到達イベントを発生させます。")]
        private Transform player;
        [SerializeField]
        [Tooltip("ゴール中心から各軸へ判定する距離（ローカル座標の半幅）です。大きいほど離れた位置でもゴールになります。スケール1ならm単位で、全幅はこの値の2倍です。")]
        private Vector3 halfExtents = new Vector3(1.3f, 1.6f, 1.3f);
        public bool IsReached { get; private set; }
        public event Action Reached;
        public void RestoreReached(bool reached) { IsReached = reached; }
        private void Update() { CheckPlayer(); }
        public void CheckPlayer()
        {
            if (IsReached || !player) return;
            Vector3 offset = transform.InverseTransformPoint(player.position);
            if (Mathf.Abs(offset.x) <= halfExtents.x && Mathf.Abs(offset.z) <= halfExtents.z && Mathf.Abs(offset.y) <= halfExtents.y)
            {
                IsReached = true;
                Reached?.Invoke();
            }
        }
    }
}
