using UnityEngine;
using UnityEngine.EventSystems;

namespace OnlyUpPrototype
{
    [DefaultExecutionOrder(100)]
    [RequireComponent(typeof(Camera))]
    public sealed class ThirdPersonOrbitCamera : MonoBehaviour
    {
        [Header("Target and initial framing")]
        [SerializeField] private Transform target;
        [SerializeField, InspectorName("Look At Height")] private float targetHeight = 1.2f;
        [SerializeField, Tooltip("Initial camera eye height above the player at the default distance.")]
        private float cameraHeight = 1.5f;
        [SerializeField, Min(0.1f), InspectorName("Default Camera Distance")] private float distance = 3f;
        [SerializeField, Min(0.1f)] private float minCameraDistance = 1.5f;
        [SerializeField, Min(0.1f)] private float maxCameraDistance = 7f;

        [Header("Look and follow")]
        [SerializeField, Min(0f)] private float sensitivity = 3f;
        [SerializeField] private float minPitch = -35f;
        [SerializeField] private float maxPitch = 70f;
        [SerializeField, HideInInspector] private float pitch = 5.74f;
        [SerializeField] private float yaw;
        [SerializeField, Min(0.1f)] private float followSmoothness = 20f;
        [SerializeField, Min(0.1f)] private float rotationSmoothness = 18f;
        [SerializeField, Min(1f)] private float teleportSnapDistance = 8f;
        [SerializeField] private bool autoCaptureOnEnable = true;

        [Header("Wheel zoom")]
        [SerializeField, Min(0f)] private float zoomSpeed = 10f;
        [SerializeField, Min(0.1f)] private float zoomSmoothness = 12f;

        [Header("Occlusion")]
        [SerializeField, Min(0.05f), InspectorName("Camera Collision Radius")] private float collisionRadius = 0.22f;
        [SerializeField, Min(0f)] private float collisionPadding = 0.1f;
        [SerializeField, Min(0.1f)] private float returnSmoothness = 10f;
        [SerializeField] private LayerMask collisionMask = ~0;
        [SerializeField, Tooltip("Avoid rendering the inside of the model when an obstruction forces the camera extremely close.")]
        private bool hidePlayerWhenVeryClose = true;
        [SerializeField, Min(0.1f)] private float playerHideDistance = 0.6f;

        private float targetDistance, smoothedZoomDistance, currentDistance;
        private Vector3 followedPosition;
        private Quaternion orbitRotation;
        private bool initialized, captured, pendingCapture, inputSuspended, recovering, obstructed, playerHidden;
        private Transform cachedTarget;
        private Renderer[] targetRenderers;
        private bool[] previousRenderingOff;
        private readonly RaycastHit[] hits = new RaycastHit[64];

        public float CurrentDistance => currentDistance;
        public float TargetDistance => targetDistance;
        public float SmoothedZoomDistance => smoothedZoomDistance;
        public float MinDistance => minCameraDistance;
        public float MaxDistance => maxCameraDistance;
        public float DefaultDistance => distance;
        public float Pitch => pitch;
        public float Yaw => yaw;
        public bool IsCursorCaptured => captured;
        public bool IsObstructed => obstructed;
        public bool IsPlayerHidden => playerHidden;
        public float Sensitivity { get => sensitivity; set => sensitivity = Mathf.Clamp(value, 0.2f, 10f); }

        private float InitialPitch()
        {
            return Mathf.Clamp(Mathf.Asin(Mathf.Clamp((cameraHeight - targetHeight) / Mathf.Max(.1f, distance), -.95f, .95f)) * Mathf.Rad2Deg, minPitch, maxPitch);
        }
        private void Awake()
        {
            pitch = InitialPitch();
            Initialize();
        }
        private void OnEnable()
        {
            if (Application.isPlaying) pendingCapture = autoCaptureOnEnable;
        }
        private bool Initialize()
        {
            if (!target) return false;
            if (initialized && cachedTarget == target) return true;
            RestorePlayerVisibility();
            cachedTarget = target;
            followedPosition = target.position;
            orbitRotation = Quaternion.Euler(pitch, yaw, 0f);
            targetDistance = Mathf.Clamp(distance, minCameraDistance, maxCameraDistance);
            smoothedZoomDistance = currentDistance = targetDistance;
            targetRenderers = target.GetComponentsInChildren<Renderer>(true);
            previousRenderingOff = new bool[targetRenderers.Length];
            initialized = true; recovering = false;
            return true;
        }
        private void LateUpdate()
        {
            if (captured && Cursor.lockState != CursorLockMode.Locked) ReleaseCursor();
            bool overUi = EventSystem.current && EventSystem.current.IsPointerOverGameObject();
            ProcessInput(new Vector2(Input.GetAxis("Mouse X"), Input.GetAxis("Mouse Y")),
                Input.GetAxisRaw("Mouse ScrollWheel"), Input.GetKeyDown(KeyCode.Escape),
                Input.GetMouseButtonDown(0), Input.GetMouseButtonDown(2), Application.isFocused, overUi);
            Tick(Time.unscaledDeltaTime);
        }
        // The live input path also accepts recorded samples for deterministic validation.
        public void ProcessInput(Vector2 mouse, float wheel, bool escape, bool click, bool resetZoom, bool focused, bool pointerOverUi = false)
        {
            if (!isActiveAndEnabled || !target || inputSuspended || !focused || Time.timeScale <= 0f)
            { ReleaseCursor(); return; }
            if (escape) { ReleaseCursor(); return; }
            if (pendingCapture || (!captured && click))
            {
                if (!pointerOverUi) CaptureCursor();
                return; // Discard the capture frame so locking never produces a look jump.
            }
            if (!captured) return;
            ApplyLook(mouse);
            ApplyZoom(wheel);
            if (resetZoom) ResetZoom();
        }
        public void CaptureCursor()
        {
            if (!Application.isPlaying || !isActiveAndEnabled || !target || inputSuspended || Time.timeScale <= 0f) return;
            pendingCapture = false; captured = true;
            Cursor.lockState = CursorLockMode.Locked; Cursor.visible = false;
        }
        public void ReleaseCursor()
        {
            pendingCapture = false; captured = false;
            if (Application.isPlaying) { Cursor.lockState = CursorLockMode.None; Cursor.visible = true; }
        }
        public void SetInputSuspended(bool value)
        {
            inputSuspended = value;
            if (value) ReleaseCursor();
        }
        public void ApplyLook(Vector2 delta)
        {
            if (float.IsNaN(delta.x) || float.IsNaN(delta.y) || float.IsInfinity(delta.x) || float.IsInfinity(delta.y)) return;
            yaw = Mathf.Repeat(yaw + delta.x * sensitivity, 360f);
            pitch = Mathf.Clamp(pitch - delta.y * sensitivity, minPitch, maxPitch);
        }
        public void ApplyZoom(float wheel)
        {
            if (!Initialize() || float.IsNaN(wheel) || float.IsInfinity(wheel)) return;
            SetZoomDistance(targetDistance - wheel * zoomSpeed);
        }
        public void SetZoomDistance(float value)
        {
            if (!Initialize() || float.IsNaN(value) || float.IsInfinity(value)) return;
            targetDistance = Mathf.Clamp(value, minCameraDistance, maxCameraDistance);
        }
        public void ResetZoom() { SetZoomDistance(distance); }

        private bool Ignore(Collider c)
        {
            return !c || c.transform == target || c.transform.IsChildOf(target);
        }
        private float AllowedDistance(Vector3 origin, Vector3 direction, float wanted)
        {
            float allowed = wanted;
            int count = Physics.SphereCastNonAlloc(origin, collisionRadius, direction, hits, wanted, collisionMask, QueryTriggerInteraction.Ignore);
            if (count < hits.Length)
            {
                for (int i = 0; i < count; i++)
                    if (!Ignore(hits[i].collider)) allowed = Mathf.Min(allowed, Mathf.Max(0f, hits[i].distance - collisionPadding));
            }
            else
            {
                // A rare crowded cast must not miss a nearer obstacle because the reusable buffer filled.
                foreach (var hit in Physics.SphereCastAll(origin, collisionRadius, direction, wanted, collisionMask, QueryTriggerInteraction.Ignore))
                    if (!Ignore(hit.collider)) allowed = Mathf.Min(allowed, Mathf.Max(0f, hit.distance - collisionPadding));
            }
            return allowed;
        }
        public void Tick(float deltaTime)
        {
            if (!Initialize()) return;
            float dt = float.IsNaN(deltaTime) || float.IsInfinity(deltaTime) ? 0f : Mathf.Max(0f, deltaTime);
            targetDistance = Mathf.Clamp(targetDistance, minCameraDistance, maxCameraDistance);
            smoothedZoomDistance = Mathf.Lerp(smoothedZoomDistance, targetDistance, 1f - Mathf.Exp(-zoomSmoothness * dt));
            if (Vector3.Distance(followedPosition, target.position) > teleportSnapDistance) followedPosition = target.position;
            else followedPosition = Vector3.Lerp(followedPosition, target.position, 1f - Mathf.Exp(-followSmoothness * dt));
            orbitRotation = Quaternion.Slerp(orbitRotation, Quaternion.Euler(pitch, yaw, 0f), 1f - Mathf.Exp(-rotationSmoothness * dt));
            Vector3 lookPoint = target.position + Vector3.up * targetHeight;
            Vector3 wantedPosition = followedPosition + Vector3.up * targetHeight + orbitRotation * Vector3.back * smoothedZoomDistance;
            Vector3 delta = wantedPosition - lookPoint;
            float wantedLength = Mathf.Clamp(delta.magnitude, minCameraDistance, maxCameraDistance);
            Vector3 direction = delta.sqrMagnitude > .000001f ? delta.normalized : orbitRotation * Vector3.back;
            float elevation = Mathf.Asin(Mathf.Clamp(direction.y, -1f, 1f)) * Mathf.Rad2Deg;
            if (elevation < minPitch || elevation > maxPitch)
            {
                float angle = Mathf.Clamp(elevation, minPitch, maxPitch) * Mathf.Deg2Rad;
                Vector3 flat = Vector3.ProjectOnPlane(direction, Vector3.up).normalized;
                if (flat.sqrMagnitude < .001f) flat = Vector3.ProjectOnPlane(orbitRotation * Vector3.back, Vector3.up).normalized;
                direction = flat * Mathf.Cos(angle) + Vector3.up * Mathf.Sin(angle);
            }
            float allowed = AllowedDistance(lookPoint, direction, wantedLength);
            bool wasObstructed = obstructed;
            obstructed = allowed < wantedLength - .001f;
            if (allowed < currentDistance) currentDistance = allowed;
            else if (wasObstructed || obstructed || recovering)
            {
                currentDistance = Mathf.Lerp(currentDistance, allowed, 1f - Mathf.Exp(-returnSmoothness * dt));
                recovering = Mathf.Abs(currentDistance - allowed) > .005f;
            }
            else currentDistance = allowed;
            if (obstructed) recovering = true;
            currentDistance = Mathf.Clamp(currentDistance, 0f, allowed);
            transform.SetPositionAndRotation(lookPoint + direction * currentDistance, Quaternion.LookRotation(-direction, Vector3.up));
            UpdatePlayerVisibility();
        }
        public void SnapToTarget()
        {
            if (!Initialize()) return;
            followedPosition = target.position;
            orbitRotation = Quaternion.Euler(pitch, yaw, 0f);
            smoothedZoomDistance = targetDistance;
            currentDistance = smoothedZoomDistance;
            recovering = obstructed = false;
            Tick(0f);
        }
        private void UpdatePlayerVisibility()
        {
            if (!Application.isPlaying || targetRenderers == null) return;
            bool shouldHide = hidePlayerWhenVeryClose && currentDistance < playerHideDistance + (playerHidden ? .15f : 0f);
            if (shouldHide && !playerHidden)
            {
                for (int i = 0; i < targetRenderers.Length; i++)
                    if (targetRenderers[i]) { previousRenderingOff[i] = targetRenderers[i].forceRenderingOff; targetRenderers[i].forceRenderingOff = true; }
                playerHidden = true;
            }
            else if (!shouldHide) RestorePlayerVisibility();
        }
        private void RestorePlayerVisibility()
        {
            if (!playerHidden || targetRenderers == null) return;
            for (int i = 0; i < targetRenderers.Length; i++)
                if (targetRenderers[i]) targetRenderers[i].forceRenderingOff = previousRenderingOff[i];
            playerHidden = false;
        }
        private void OnDisable() { ReleaseCursor(); RestorePlayerVisibility(); }
        private void OnDestroy() { ReleaseCursor(); RestorePlayerVisibility(); }
        private void OnApplicationFocus(bool focused) { if (!focused) ReleaseCursor(); }
        private void OnApplicationPause(bool paused) { if (paused) ReleaseCursor(); }
        private void OnValidate()
        {
            minCameraDistance = Mathf.Max(.1f, minCameraDistance);
            maxCameraDistance = Mathf.Max(minCameraDistance, maxCameraDistance);
            distance = Mathf.Clamp(distance, minCameraDistance, maxCameraDistance);
            minPitch = Mathf.Clamp(minPitch, -85f, 85f);
            maxPitch = Mathf.Clamp(maxPitch, minPitch, 85f);
            pitch = Application.isPlaying ? Mathf.Clamp(pitch, minPitch, maxPitch) : InitialPitch();
            collisionRadius = Mathf.Max(.05f, collisionRadius);
            collisionPadding = Mathf.Max(0f, collisionPadding);
            zoomSpeed = Mathf.Max(0f, zoomSpeed);
            zoomSmoothness = Mathf.Max(.1f, zoomSmoothness);
            followSmoothness = Mathf.Max(.1f, followSmoothness);
            rotationSmoothness = Mathf.Max(.1f, rotationSmoothness);
        }
    }
}
