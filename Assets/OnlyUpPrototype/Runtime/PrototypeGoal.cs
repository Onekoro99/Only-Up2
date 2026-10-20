using System;
using UnityEngine;

namespace OnlyUpPrototype
{
    public sealed class PrototypeGoal : MonoBehaviour
    {
        [SerializeField] private Transform player;
        [SerializeField] private Vector3 halfExtents = new Vector3(1.3f, 1.6f, 1.3f);
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
