using UnityEngine;

[RequireComponent(typeof(CharacterController), typeof(Animator))]
public sealed class CharacterMovement : MonoBehaviour
{
    [SerializeField, Min(0f)] private float moveSpeed = 3f;
    [SerializeField, Min(0f)] private float rotationSpeed = 540f;
    [SerializeField, Min(0f)] private float jumpSpeed = 6f;
    [SerializeField] private float gravity = -20f;
    private CharacterController controller;
    private Animator animator;
    private float verticalVelocity;
    private static readonly int SpeedId = Animator.StringToHash("Speed");
    private static readonly int GroundedId = Animator.StringToHash("IsGrounded");

    private void Awake()
    {
        controller = GetComponent<CharacterController>();
        animator = GetComponent<Animator>();
        animator.SetBool(GroundedId, true);
    }

    private void Update()
    {
        MoveCharacter(Input.GetAxisRaw("Horizontal"), Input.GetAxisRaw("Vertical"),
            Input.GetButtonDown("Jump"), Time.deltaTime);
    }

    private void MoveCharacter(float horizontal, float vertical, bool jumpPressed, float deltaTime)
    {
        if (deltaTime <= 0f) return;
        Vector3 direction = Vector3.ClampMagnitude(new Vector3(horizontal, 0f, vertical), 1f);
        bool grounded = controller.isGrounded;
        if (grounded && verticalVelocity < 0f) verticalVelocity = -2f;
        if (grounded && jumpPressed) verticalVelocity = jumpSpeed;
        verticalVelocity += gravity * deltaTime;
        CollisionFlags flags = controller.Move((direction * moveSpeed + Vector3.up * verticalVelocity) * deltaTime);
        if ((flags & CollisionFlags.Above) != 0 && verticalVelocity > 0f) verticalVelocity = 0f;
        grounded = (flags & CollisionFlags.Below) != 0 && verticalVelocity <= 0f;
        if (grounded) verticalVelocity = -2f;
        if (direction.sqrMagnitude > 0.0001f)
            transform.rotation = Quaternion.RotateTowards(transform.rotation,
                Quaternion.LookRotation(direction, Vector3.up), rotationSpeed * deltaTime);
        animator.SetFloat(SpeedId, direction.magnitude * moveSpeed);
        animator.SetBool(GroundedId, grounded);
    }

    // CharacterController owns movement; consume imported root motion.
    private void OnAnimatorMove() { }

    private void OnValidate()
    {
        moveSpeed = Mathf.Max(0f, moveSpeed);
        rotationSpeed = Mathf.Max(0f, rotationSpeed);
        jumpSpeed = Mathf.Max(0f, jumpSpeed);
        gravity = -Mathf.Max(0.01f, Mathf.Abs(gravity));
    }
}
