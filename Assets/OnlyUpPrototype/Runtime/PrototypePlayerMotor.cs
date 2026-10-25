using UnityEngine;

namespace OnlyUpPrototype
{
    [DefaultExecutionOrder(-100)]
    [RequireComponent(typeof(CharacterController), typeof(Animator))]
    public sealed class PrototypePlayerMotor : MonoBehaviour
    {
        [SerializeField]
        [Header("移動の基準と操作性能")]
        [Tooltip("WASDの移動方向を決めるカメラです。カメラの前方を水平面に投影してWの向きにします。未設定の場合は開始時にMain Cameraを探します。")]
        private Transform movementCamera;
        [SerializeField, Min(0f)]
        [Tooltip("地上での移動速度（m/秒）です。大きいほど速く、小さいほどゆっくり移動します。初期値3.5です。ジャンプ中の横移動にも影響します。")]
        private float moveSpeed = 3.5f;
        [SerializeField, Min(0f)]
        [Tooltip("移動方向へキャラクターが向き直る速さ（度/秒）です。大きいほど素早く、小さいほどゆっくり回転します。初期値540です。")]
        private float rotationSpeed = 540f;
        [SerializeField, Min(0f)]
        [Tooltip("ジャンプ直後の上向き速度（m/秒）です。大きいほど高く跳び、小さいほど低くなります。実際の高さはGravityとの組み合わせで決まります。初期値7です。")]
        private float jumpSpeed = 7f;
        [SerializeField]
        [Tooltip("上下速度へ加える重力加速度（m/秒²）です。負の値で、−30のように絶対値を大きくすると速く落ち、同じJump Speedでは低くなります。初期値−20です。")]
        private float gravity = -20f;
        [SerializeField, Range(0f, 1f)]
        [Tooltip("空中で横移動速度を切り替える強さ（0〜1）です。大きいほど方向変更に素早く反応し、小さいほど空中で横速度を変えにくくなります。初期値0.75です。")]
        private float airControl = 0.75f;
        private CharacterController controller;
        private Animator animator;
        private Vector3 horizontalVelocity;
        public bool IsGrounded { get; private set; }
        public float VerticalSpeed { get; private set; }
        public Vector3 HorizontalVelocity => horizontalVelocity;
        public CharacterController Controller => controller;
        private static readonly int SpeedId = Animator.StringToHash("Speed");
        private static readonly int GroundedId = Animator.StringToHash("IsGrounded");

        private void Awake()
        {
            controller = GetComponent<CharacterController>();
            animator = GetComponent<Animator>();
            if (!movementCamera && Camera.main) movementCamera = Camera.main.transform;
            animator.SetBool(GroundedId, true);
        }

        private void Update()
        {
            Simulate(new Vector2(Input.GetAxisRaw("Horizontal"), Input.GetAxisRaw("Vertical")),
                Input.GetButtonDown("Jump"), Time.deltaTime);
        }

        // Also usable by a deterministic input driver without generating keyboard events.
        public void Simulate(Vector2 input, bool jump, float deltaTime)
        {
            if (deltaTime <= 0f || !controller.enabled) return;
            input = Vector2.ClampMagnitude(input, 1f);
            Vector3 forward = movementCamera ? Vector3.ProjectOnPlane(movementCamera.forward, Vector3.up) : Vector3.forward;
            if (forward.sqrMagnitude < 0.001f) forward = Vector3.forward;
            forward.Normalize();
            Vector3 direction = forward * input.y + Vector3.Cross(Vector3.up, forward) * input.x;
            bool groundedBeforeMove = controller.isGrounded;
            if (groundedBeforeMove && VerticalSpeed < 0f) VerticalSpeed = -2f;
            if (groundedBeforeMove && jump) VerticalSpeed = jumpSpeed;
            VerticalSpeed += gravity * deltaTime;
            Vector3 desired = direction * moveSpeed;
            horizontalVelocity = groundedBeforeMove ? desired : Vector3.MoveTowards(horizontalVelocity, desired, moveSpeed * airControl * 8f * deltaTime);
            CollisionFlags flags = controller.Move((horizontalVelocity + Vector3.up * VerticalSpeed) * deltaTime);
            if ((flags & CollisionFlags.Above) != 0 && VerticalSpeed > 0f) VerticalSpeed = 0f;
            IsGrounded = (flags & CollisionFlags.Below) != 0 && VerticalSpeed <= 0f;
            if (IsGrounded) VerticalSpeed = -2f;
            if (direction.sqrMagnitude > 0.001f)
                transform.rotation = Quaternion.RotateTowards(transform.rotation, Quaternion.LookRotation(direction), rotationSpeed * deltaTime);
            animator.SetFloat(SpeedId, horizontalVelocity.magnitude);
            animator.SetBool(GroundedId, IsGrounded);
        }

        public void Teleport(Vector3 position, Quaternion rotation)
        {
            controller.enabled = false;
            transform.SetPositionAndRotation(position, rotation);
            controller.enabled = true;
            Physics.SyncTransforms();
            horizontalVelocity = Vector3.zero;
            VerticalSpeed = 0f;
            IsGrounded = false;
            animator.SetFloat(SpeedId, 0f);
            animator.SetBool(GroundedId, true);
        }

        // Imported Generic clips contain root curves; movement belongs to the motor.
        public void RestoreRuntimeState(Vector3 position, Quaternion rotation, Vector3 horizontal, float vertical, bool grounded)
        {
            Teleport(position, rotation);
            horizontalVelocity = horizontal;
            VerticalSpeed = vertical;
            IsGrounded = grounded;
            animator.SetFloat(SpeedId, horizontal.magnitude);
            animator.SetBool(GroundedId, grounded);
        }

        private void OnAnimatorMove() { }
        private void OnValidate()
        {
            gravity = -Mathf.Max(0.01f, Mathf.Abs(gravity));
            moveSpeed = Mathf.Max(0f, moveSpeed);
            jumpSpeed = Mathf.Max(0f, jumpSpeed);
            rotationSpeed = Mathf.Max(0f, rotationSpeed);
        }
    }
}
