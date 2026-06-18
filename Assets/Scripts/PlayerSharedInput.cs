using UnityEngine;

public class PlayerSharedInput : MonoBehaviour
{
    public int playerNumber = 1;

    public Vector2 Movement { get; private set; }
    public bool RunHeld { get; private set; }

    void Update()
    {
        Movement = GetMovementInput();
        RunHeld = GetAction(PlayerControlAction.Run);
    }

    public bool GetAction(PlayerControlAction action)
    {
        InputJoinManager.InputType type = GetInputType();
        switch (type)
        {
            case InputJoinManager.InputType.KeyboardWASD:
            case InputJoinManager.InputType.KeyboardArrows:
                return PlayerInputBindings.GetKeyboardAction(playerNumber, action);
            case InputJoinManager.InputType.Gamepad:
                return PlayerInputBindings.GetGamepadAction(playerNumber, action, GetGamepadIndex());
        }

        return false;
    }

    public bool GetActionDown(PlayerControlAction action)
    {
        InputJoinManager.InputType type = GetInputType();
        switch (type)
        {
            case InputJoinManager.InputType.KeyboardWASD:
            case InputJoinManager.InputType.KeyboardArrows:
                return PlayerInputBindings.GetKeyboardActionDown(playerNumber, action);
            case InputJoinManager.InputType.Gamepad:
                return PlayerInputBindings.GetGamepadActionDown(playerNumber, action, GetGamepadIndex());
        }

        return false;
    }

    InputJoinManager.InputType GetInputType()
    {
        return playerNumber == 1 ? InputJoinManager.player1Input : InputJoinManager.player2Input;
    }

    int GetGamepadIndex()
    {
        return playerNumber == 1 ? InputJoinManager.player1GamepadIndex : InputJoinManager.player2GamepadIndex;
    }

    Vector2 GetMovementInput()
    {
        InputJoinManager.InputType type = GetInputType();
        switch (type)
        {
            case InputJoinManager.InputType.KeyboardWASD:
            case InputJoinManager.InputType.KeyboardArrows:
                return PlayerInputBindings.GetKeyboardMovement(playerNumber);
            case InputJoinManager.InputType.Gamepad:
                return PlayerInputBindings.GetGamepadMovement(playerNumber, GetGamepadIndex());
        }

        return Vector2.zero;
    }
}
