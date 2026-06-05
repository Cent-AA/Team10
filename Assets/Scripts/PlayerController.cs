using UnityEngine;

public class PlayerController : MonoBehaviour
{
    [Header("Настройки")]
    public int playerNumber = 1;          // 1 или 2
    public float moveSpeed = 5f;

    [Header("Компоненты")]
    public PuppetAnimator puppetAnimator;
    public Rigidbody2D rb;

    private Vector2 moveInput;

    void Update()
    {
        // Получаем ввод в зависимости от выбранного устройства
        moveInput = GetMovementInput();

        // Анимация
        if (puppetAnimator != null)
            puppetAnimator.SetWalking(moveInput.magnitude > 0.1f);

        // Атака
        if (GetAttackInput())
        {
            if (puppetAnimator != null)
                puppetAnimator.Attack(true);
        }
    }

    void FixedUpdate()
    {
        // Движение
        if (rb != null)
        {
            Vector2 newPos = rb.position + moveInput * moveSpeed * Time.fixedDeltaTime;
            rb.MovePosition(newPos);
        }
    }

    Vector2 GetMovementInput()
    {
        var type = playerNumber == 1 ? InputJoinManager.player1Input : InputJoinManager.player2Input;
        int pad = playerNumber == 1 ? InputJoinManager.player1GamepadIndex : InputJoinManager.player2GamepadIndex;

        Vector2 input = Vector2.zero;

        switch (type)
        {
            case InputJoinManager.InputType.KeyboardWASD:
                if (Input.GetKey(KeyCode.W)) input.y += 1;
                if (Input.GetKey(KeyCode.S)) input.y -= 1;
                if (Input.GetKey(KeyCode.A)) input.x -= 1;
                if (Input.GetKey(KeyCode.D)) input.x += 1;
                break;

            case InputJoinManager.InputType.KeyboardArrows:
                if (Input.GetKey(KeyCode.UpArrow)) input.y += 1;
                if (Input.GetKey(KeyCode.DownArrow)) input.y -= 1;
                if (Input.GetKey(KeyCode.LeftArrow)) input.x -= 1;
                if (Input.GetKey(KeyCode.RightArrow)) input.x += 1;
                break;

            case InputJoinManager.InputType.Gamepad:
                input.x = Input.GetAxis("Horizontal");
                input.y = Input.GetAxis("Vertical");
                break;
        }

        return input.normalized;
    }

    bool GetAttackInput()
    {
        var type = playerNumber == 1 ? InputJoinManager.player1Input : InputJoinManager.player2Input;
        int pad = playerNumber == 1 ? InputJoinManager.player1GamepadIndex : InputJoinManager.player2GamepadIndex;

        switch (type)
        {
            case InputJoinManager.InputType.KeyboardWASD:
                return Input.GetKeyDown(KeyCode.Space);

            case InputJoinManager.InputType.KeyboardArrows:
                return Input.GetKeyDown(KeyCode.RightShift);

            case InputJoinManager.InputType.Gamepad:
                KeyCode kc = (KeyCode)System.Enum.Parse(typeof(KeyCode), "Joystick" + pad + "Button0");
                return Input.GetKeyDown(kc);
        }

        return false;
    }
}