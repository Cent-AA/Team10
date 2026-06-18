using UnityEngine;

public class PlayerMotor2D : MonoBehaviour
{
    public float walkSpeed = 5f;
    public float runSpeed = 8f;
    public Rigidbody2D rb;

    public Vector2 MoveInput { get; private set; }
    public bool IsRunning { get; private set; }

    bool movementLocked;

    void Awake()
    {
        if (rb == null)
            rb = GetComponent<Rigidbody2D>();
    }

    public void SetMovement(Vector2 moveInput, bool runHeld)
    {
        MoveInput = moveInput.sqrMagnitude > 1f ? moveInput.normalized : moveInput;
        IsRunning = runHeld;
    }

    public void SetMovementLocked(bool locked)
    {
        movementLocked = locked;
    }

    void FixedUpdate()
    {
        if (movementLocked || rb == null)
            return;

        float speed = IsRunning ? runSpeed : walkSpeed;
        rb.MovePosition(rb.position + MoveInput * speed * Time.fixedDeltaTime);
    }
}
