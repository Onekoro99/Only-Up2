using UnityEngine;

namespace OnlyUpPrototype
{
    [DefaultExecutionOrder(100)]
    [RequireComponent(typeof(Camera))]
    public sealed class ThirdPersonOrbitCamera : MonoBehaviour
    {
        [SerializeField] private Transform target;
        [SerializeField] private float targetHeight = 1.1f;
        [SerializeField, Min(0.5f)] private float distance = 5.5f;
        [SerializeField, Min(0f)] private float sensitivity = 3f;
        [SerializeField] private float minPitch = -15f;
        [SerializeField] private float maxPitch = 70f;
        [SerializeField] private float pitch = 22f;
        [SerializeField] private float yaw;
        [SerializeField, Min(0.05f)] private float collisionRadius = 0.22f;
        [SerializeField, Min(0f)] private float collisionPadding = 0.1f;
        [SerializeField, Min(0f)] private float returnSmoothness = 10f;
        [SerializeField] private LayerMask collisionMask = ~0;
        private float currentDistance;
        private readonly RaycastHit[] hits = new RaycastHit[64];
        private CursorLockMode previousLock;
        private bool previousVisible;
        private bool captured;
        public float CurrentDistance => currentDistance;
        public float Sensitivity { get => sensitivity; set => sensitivity = Mathf.Clamp(value, 0.2f, 10f); }

        private void Awake() { currentDistance = distance; }
        private void LateUpdate()
        {
            if (Input.GetMouseButton(1))
            {
                if (!captured)
                {
                    previousLock = Cursor.lockState;
                    previousVisible = Cursor.visible;
                    captured = true;
                }
                Cursor.lockState = CursorLockMode.Locked;
                Cursor.visible = false;
                ApplyLook(new Vector2(Input.GetAxis("Mouse X"), Input.GetAxis("Mouse Y")));
            }
            else RestoreCursor();
            Tick(Time.deltaTime);
        }

        public void ApplyLook(Vector2 delta)
        {
            yaw = Mathf.Repeat(yaw + delta.x * sensitivity, 360f);
            pitch = Mathf.Clamp(pitch - delta.y * sensitivity, minPitch, maxPitch);
        }

        public void Tick(float deltaTime)
        {
            if (!target) return;
            Vector3 pivot = target.position + Vector3.up * targetHeight;
            Quaternion rotation = Quaternion.Euler(pitch, yaw, 0f);
            Vector3 backwards = rotation * Vector3.back;
            float allowedDistance = distance;
            int count = Physics.SphereCastNonAlloc(pivot, collisionRadius, backwards, hits, distance, collisionMask, QueryTriggerInteraction.Ignore);
            for (int i = 0; i < count; i++)
            {
                if (hits[i].transform == target || hits[i].transform.IsChildOf(target)) continue;
                allowedDistance = Mathf.Min(allowedDistance, Mathf.Max(0f, hits[i].distance - collisionPadding));
            }
            // Obstruction snaps inward immediately; recovery alone is smoothed.
            currentDistance = allowedDistance < currentDistance ? allowedDistance : Mathf.Lerp(currentDistance, allowedDistance, 1f - Mathf.Exp(-returnSmoothness * deltaTime));
            transform.SetPositionAndRotation(pivot + backwards * currentDistance, rotation);
        }

        public void SnapToTarget() { currentDistance = distance; Tick(0f); }
        private void RestoreCursor()
        {
            if (!captured) return;
            Cursor.lockState = previousLock;
            Cursor.visible = previousVisible;
            captured = false;
        }
        private void OnDisable() { RestoreCursor(); }
        private void OnApplicationFocus(bool focused) { if (!focused) RestoreCursor(); }
        private void OnValidate() { maxPitch = Mathf.Max(minPitch, maxPitch); pitch = Mathf.Clamp(pitch, minPitch, maxPitch); }
    }
}
