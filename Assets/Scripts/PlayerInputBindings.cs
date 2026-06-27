using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;

public enum PlayerControlAction
{
    MoveUp,
    MoveDown,
    MoveLeft,
    MoveRight,
    Run,
    LightAttack,
    HeavyAttack,
    Dash,
    Roll,
    Block,
    Shoot,
    Confirm,
    SelectLeft,
    SelectRight
}

public enum PlayerControlDevice
{
    Keyboard,
    Gamepad
}

public enum PlayerGamepadControl
{
    LeftStickUp,
    LeftStickDown,
    LeftStickLeft,
    LeftStickRight,
    DpadUp,
    DpadDown,
    DpadLeft,
    DpadRight,
    ButtonSouth,
    ButtonEast,
    ButtonWest,
    ButtonNorth,
    LeftShoulder,
    RightShoulder,
    LeftTrigger,
    RightTrigger,
    Select,
    Start,
    LeftStickButton,
    RightStickButton
}

public static class PlayerInputBindings
{
    private const float StickThreshold = 0.5f;
    private const string KeyboardPrefix = "Controls.Keyboard";
    private const string GamepadPrefix = "Controls.Gamepad";

    private static readonly PlayerControlAction[] Actions = (PlayerControlAction[])Enum.GetValues(typeof(PlayerControlAction));
    private static readonly KeyCode[] KeyboardCandidates = (KeyCode[])Enum.GetValues(typeof(KeyCode));
    private static readonly PlayerGamepadControl[] GamepadControlCandidates = (PlayerGamepadControl[])Enum.GetValues(typeof(PlayerGamepadControl));
    private static readonly KeyCode[,] keyboardBindings = new KeyCode[3, Actions.Length];
    private static readonly PlayerGamepadControl[,] gamepadBindings = new PlayerGamepadControl[3, Actions.Length];
    private static bool bindingsLoaded;

    private class CachedPress
    {
        public int frame = -1;
        public bool wasPressed;
        public bool downThisFrame;
    }

    private static readonly Dictionary<string, CachedPress> cachedGamepadPresses = new Dictionary<string, CachedPress>();

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetRuntimeCache()
    {
        bindingsLoaded = false;
        cachedGamepadPresses.Clear();
    }

    public static KeyCode GetKeyboardKey(int playerNumber, PlayerControlAction action)
    {
        EnsureBindingsLoaded();
        return keyboardBindings[NormalizePlayer(playerNumber), GetActionIndex(action)];
    }

    public static PlayerGamepadControl GetGamepadControl(int playerNumber, PlayerControlAction action)
    {
        EnsureBindingsLoaded();
        return gamepadBindings[NormalizePlayer(playerNumber), GetActionIndex(action)];
    }

    public static void SetKeyboardKey(int playerNumber, PlayerControlAction action, KeyCode key)
    {
        EnsureBindingsLoaded();
        playerNumber = NormalizePlayer(playerNumber);
        keyboardBindings[playerNumber, GetActionIndex(action)] = key;
        PlayerPrefs.SetString(GetKeyboardKeyName(playerNumber, action), key.ToString());
        PlayerPrefs.Save();
    }

    public static void SetGamepadControl(int playerNumber, PlayerControlAction action, PlayerGamepadControl control)
    {
        EnsureBindingsLoaded();
        playerNumber = NormalizePlayer(playerNumber);
        gamepadBindings[playerNumber, GetActionIndex(action)] = control;
        PlayerPrefs.SetString(GetGamepadKeyName(playerNumber, action), control.ToString());
        PlayerPrefs.Save();
    }

    public static void ResetAll()
    {
        EnsureBindingsLoaded();
        ResetPlayer(1);
        ResetPlayer(2);
    }

    public static void ResetPlayer(int playerNumber)
    {
        ResetPlayerKeyboard(playerNumber);
        ResetPlayerGamepad(playerNumber);
    }

    public static void ResetPlayerKeyboard(int playerNumber)
    {
        EnsureBindingsLoaded();
        playerNumber = NormalizePlayer(playerNumber);
        for (int i = 0; i < Actions.Length; i++)
        {
            PlayerControlAction action = Actions[i];
            PlayerPrefs.DeleteKey(GetKeyboardKeyName(playerNumber, action));
            keyboardBindings[playerNumber, i] = GetDefaultKeyboardKey(playerNumber, action);
        }
        PlayerPrefs.Save();
    }

    public static void ResetPlayerGamepad(int playerNumber)
    {
        EnsureBindingsLoaded();
        playerNumber = NormalizePlayer(playerNumber);
        for (int i = 0; i < Actions.Length; i++)
        {
            PlayerControlAction action = Actions[i];
            PlayerPrefs.DeleteKey(GetGamepadKeyName(playerNumber, action));
            gamepadBindings[playerNumber, i] = GetDefaultGamepadControl(playerNumber, action);
        }
        PlayerPrefs.Save();
    }

    public static Vector2 GetKeyboardMovement(int playerNumber)
    {
        Vector2 input = Vector2.zero;
        if (GetKeyboardAction(playerNumber, PlayerControlAction.MoveUp)) input.y += 1f;
        if (GetKeyboardAction(playerNumber, PlayerControlAction.MoveDown)) input.y -= 1f;
        if (GetKeyboardAction(playerNumber, PlayerControlAction.MoveLeft)) input.x -= 1f;
        if (GetKeyboardAction(playerNumber, PlayerControlAction.MoveRight)) input.x += 1f;
        return input.normalized;
    }

    public static Vector2 GetGamepadMovement(int playerNumber, int gamepadIndex)
    {
        Vector2 input = Vector2.zero;
        if (GetGamepadAction(playerNumber, PlayerControlAction.MoveUp, gamepadIndex)) input.y += 1f;
        if (GetGamepadAction(playerNumber, PlayerControlAction.MoveDown, gamepadIndex)) input.y -= 1f;
        if (GetGamepadAction(playerNumber, PlayerControlAction.MoveLeft, gamepadIndex)) input.x -= 1f;
        if (GetGamepadAction(playerNumber, PlayerControlAction.MoveRight, gamepadIndex)) input.x += 1f;
        return input.normalized;
    }

    public static bool GetKeyboardAction(int playerNumber, PlayerControlAction action)
    {
        if (TrainingTutorialManager.IsPlayerInputBlocked(playerNumber))
            return false;

        return Input.GetKey(GetKeyboardKey(playerNumber, action));
    }

    public static bool GetKeyboardActionDown(int playerNumber, PlayerControlAction action)
    {
        if (TrainingTutorialManager.IsPlayerInputBlocked(playerNumber))
            return false;

        return GetKeyboardActionDownIgnoringGameplayBlock(playerNumber, action);
    }

    public static bool GetKeyboardActionDownIgnoringGameplayBlock(int playerNumber, PlayerControlAction action)
    {
        return Input.GetKeyDown(GetKeyboardKey(playerNumber, action));
    }

    public static bool GetGamepadAction(int playerNumber, PlayerControlAction action, int gamepadIndex)
    {
        if (TrainingTutorialManager.IsPlayerInputBlocked(playerNumber))
            return false;

        Gamepad gamepad = GetGamepad(gamepadIndex);
        if (gamepad == null)
            return false;

        return IsGamepadControlPressed(gamepad, GetGamepadControl(playerNumber, action));
    }

    public static bool GetGamepadActionDown(int playerNumber, PlayerControlAction action, int gamepadIndex)
    {
        if (TrainingTutorialManager.IsPlayerInputBlocked(playerNumber))
            return false;

        return GetGamepadActionDownIgnoringGameplayBlock(playerNumber, action, gamepadIndex);
    }

    public static bool GetGamepadActionDownIgnoringGameplayBlock(
        int playerNumber,
        PlayerControlAction action,
        int gamepadIndex)
    {

        Gamepad gamepad = GetGamepad(gamepadIndex);
        if (gamepad == null)
            return false;

        PlayerGamepadControl control = GetGamepadControl(playerNumber, action);
        return IsGamepadControlDown(gamepad, gamepadIndex, control, "P" + NormalizePlayer(playerNumber) + "." + action);
    }

    public static string GetBindingName(int playerNumber, PlayerControlDevice device, PlayerControlAction action)
    {
        if (device == PlayerControlDevice.Keyboard)
            return GetKeyboardKeyDisplayName(GetKeyboardKey(playerNumber, action));

        return FormatBindingName(GetGamepadControlName(GetGamepadControl(playerNumber, action)));
    }

    public static bool TryCaptureKeyboardKey(out KeyCode key)
    {
        for (int i = 0; i < KeyboardCandidates.Length; i++)
        {
            KeyCode candidate = KeyboardCandidates[i];
            if (candidate == KeyCode.None)
                continue;

            string name = candidate.ToString();
            if (name.StartsWith("Mouse") || name.StartsWith("Joystick"))
                continue;

            if (Input.GetKeyDown(candidate))
            {
                key = candidate;
                return true;
            }
        }

        key = KeyCode.None;
        return false;
    }

    public static bool TryCaptureGamepadControl(out PlayerGamepadControl control)
    {
        foreach (Gamepad gamepad in Gamepad.all)
        {
            int index = GetGamepadIndex(gamepad);
            for (int i = 0; i < GamepadControlCandidates.Length; i++)
            {
                PlayerGamepadControl candidate = GamepadControlCandidates[i];
                if (IsGamepadControlDown(gamepad, index, candidate, "Capture." + candidate))
                {
                    control = candidate;
                    return true;
                }
            }
        }

        control = PlayerGamepadControl.ButtonSouth;
        return false;
    }

    private static void EnsureBindingsLoaded()
    {
        if (bindingsLoaded)
            return;

        for (int playerNumber = 1; playerNumber <= 2; playerNumber++)
        {
            for (int i = 0; i < Actions.Length; i++)
            {
                PlayerControlAction action = Actions[i];
                keyboardBindings[playerNumber, i] = LoadKeyboardKey(playerNumber, action);
                gamepadBindings[playerNumber, i] = LoadGamepadControl(playerNumber, action);
            }
        }

        bindingsLoaded = true;
    }

    private static KeyCode LoadKeyboardKey(int playerNumber, PlayerControlAction action)
    {
        string saved = PlayerPrefs.GetString(GetKeyboardKeyName(playerNumber, action), string.Empty);
        if (!string.IsNullOrEmpty(saved))
        {
            try
            {
                return (KeyCode)Enum.Parse(typeof(KeyCode), saved);
            }
            catch
            {
                PlayerPrefs.DeleteKey(GetKeyboardKeyName(playerNumber, action));
            }
        }

        return GetDefaultKeyboardKey(playerNumber, action);
    }

    private static PlayerGamepadControl LoadGamepadControl(int playerNumber, PlayerControlAction action)
    {
        string saved = PlayerPrefs.GetString(GetGamepadKeyName(playerNumber, action), string.Empty);
        if (!string.IsNullOrEmpty(saved))
        {
            try
            {
                return (PlayerGamepadControl)Enum.Parse(typeof(PlayerGamepadControl), saved);
            }
            catch
            {
                PlayerPrefs.DeleteKey(GetGamepadKeyName(playerNumber, action));
            }
        }

        return GetDefaultGamepadControl(playerNumber, action);
    }

    private static int GetActionIndex(PlayerControlAction action)
    {
        return (int)action;
    }

    private static KeyCode GetDefaultKeyboardKey(int playerNumber, PlayerControlAction action)
    {
        if (playerNumber == 2)
        {
            switch (action)
            {
                case PlayerControlAction.MoveUp: return KeyCode.UpArrow;
                case PlayerControlAction.MoveDown: return KeyCode.DownArrow;
                case PlayerControlAction.MoveLeft: return KeyCode.LeftArrow;
                case PlayerControlAction.MoveRight: return KeyCode.RightArrow;
                case PlayerControlAction.Run: return KeyCode.RightShift;
                case PlayerControlAction.LightAttack: return KeyCode.Keypad0;
                case PlayerControlAction.HeavyAttack: return KeyCode.Keypad1;
                case PlayerControlAction.Dash: return KeyCode.Keypad2;
                case PlayerControlAction.Roll: return KeyCode.Keypad3;
                case PlayerControlAction.Block: return KeyCode.Keypad4;
                case PlayerControlAction.Shoot: return KeyCode.Keypad5;
                case PlayerControlAction.Confirm: return KeyCode.UpArrow;
                case PlayerControlAction.SelectLeft: return KeyCode.LeftArrow;
                case PlayerControlAction.SelectRight: return KeyCode.RightArrow;
            }
        }

        switch (action)
        {
            case PlayerControlAction.MoveUp: return KeyCode.W;
            case PlayerControlAction.MoveDown: return KeyCode.S;
            case PlayerControlAction.MoveLeft: return KeyCode.A;
            case PlayerControlAction.MoveRight: return KeyCode.D;
            case PlayerControlAction.Run: return KeyCode.LeftShift;
            case PlayerControlAction.LightAttack: return KeyCode.Space;
            case PlayerControlAction.HeavyAttack: return KeyCode.Q;
            case PlayerControlAction.Dash: return KeyCode.R;
            case PlayerControlAction.Roll: return KeyCode.F;
            case PlayerControlAction.Block: return KeyCode.C;
            case PlayerControlAction.Shoot: return KeyCode.J;
            case PlayerControlAction.Confirm: return KeyCode.W;
            case PlayerControlAction.SelectLeft: return KeyCode.A;
            case PlayerControlAction.SelectRight: return KeyCode.D;
            default: return KeyCode.None;
        }
    }

    private static PlayerGamepadControl GetDefaultGamepadControl(int playerNumber, PlayerControlAction action)
    {
        switch (action)
        {
            case PlayerControlAction.MoveUp: return PlayerGamepadControl.LeftStickUp;
            case PlayerControlAction.MoveDown: return PlayerGamepadControl.LeftStickDown;
            case PlayerControlAction.MoveLeft: return PlayerGamepadControl.LeftStickLeft;
            case PlayerControlAction.MoveRight: return PlayerGamepadControl.LeftStickRight;
            case PlayerControlAction.Run: return PlayerGamepadControl.RightShoulder;
            case PlayerControlAction.LightAttack: return PlayerGamepadControl.ButtonSouth;
            case PlayerControlAction.HeavyAttack: return PlayerGamepadControl.ButtonWest;
            case PlayerControlAction.Dash: return PlayerGamepadControl.ButtonEast;
            case PlayerControlAction.Roll: return PlayerGamepadControl.LeftShoulder;
            case PlayerControlAction.Block: return PlayerGamepadControl.LeftTrigger;
            case PlayerControlAction.Shoot: return PlayerGamepadControl.RightTrigger;
            case PlayerControlAction.Confirm: return PlayerGamepadControl.ButtonNorth;
            case PlayerControlAction.SelectLeft: return PlayerGamepadControl.LeftStickLeft;
            case PlayerControlAction.SelectRight: return PlayerGamepadControl.LeftStickRight;
            default: return PlayerGamepadControl.ButtonSouth;
        }
    }

    private static bool IsGamepadControlPressed(Gamepad gamepad, PlayerGamepadControl control)
    {
        if (gamepad == null)
            return false;

        ButtonControl button = GetButtonControl(gamepad, control);
        if (button != null)
            return button.isPressed;

        Vector2 stick = gamepad.leftStick.ReadValue();
        switch (control)
        {
            case PlayerGamepadControl.LeftStickUp: return stick.y > StickThreshold;
            case PlayerGamepadControl.LeftStickDown: return stick.y < -StickThreshold;
            case PlayerGamepadControl.LeftStickLeft: return stick.x < -StickThreshold;
            case PlayerGamepadControl.LeftStickRight: return stick.x > StickThreshold;
            default: return false;
        }
    }

    private static bool IsGamepadControlDown(Gamepad gamepad, int gamepadIndex, PlayerGamepadControl control, string context)
    {
        ButtonControl button = GetButtonControl(gamepad, control);
        if (button != null)
            return button.wasPressedThisFrame;

        string key = gamepadIndex + "." + context + "." + control;
        CachedPress state;
        if (!cachedGamepadPresses.TryGetValue(key, out state))
        {
            state = new CachedPress();
            cachedGamepadPresses[key] = state;
        }

        if (state.frame == Time.frameCount)
            return state.downThisFrame;

        bool pressed = IsGamepadControlPressed(gamepad, control);
        state.downThisFrame = pressed && !state.wasPressed;
        state.wasPressed = pressed;
        state.frame = Time.frameCount;
        return state.downThisFrame;
    }

    private static ButtonControl GetButtonControl(Gamepad gamepad, PlayerGamepadControl control)
    {
        if (gamepad == null)
            return null;

        switch (control)
        {
            case PlayerGamepadControl.DpadUp: return gamepad.dpad.up;
            case PlayerGamepadControl.DpadDown: return gamepad.dpad.down;
            case PlayerGamepadControl.DpadLeft: return gamepad.dpad.left;
            case PlayerGamepadControl.DpadRight: return gamepad.dpad.right;
            case PlayerGamepadControl.ButtonSouth: return gamepad.buttonSouth;
            case PlayerGamepadControl.ButtonEast: return gamepad.buttonEast;
            case PlayerGamepadControl.ButtonWest: return gamepad.buttonWest;
            case PlayerGamepadControl.ButtonNorth: return gamepad.buttonNorth;
            case PlayerGamepadControl.LeftShoulder: return gamepad.leftShoulder;
            case PlayerGamepadControl.RightShoulder: return gamepad.rightShoulder;
            case PlayerGamepadControl.LeftTrigger: return gamepad.leftTrigger;
            case PlayerGamepadControl.RightTrigger: return gamepad.rightTrigger;
            case PlayerGamepadControl.Select: return gamepad.selectButton;
            case PlayerGamepadControl.Start: return gamepad.startButton;
            case PlayerGamepadControl.LeftStickButton: return gamepad.leftStickButton;
            case PlayerGamepadControl.RightStickButton: return gamepad.rightStickButton;
            default: return null;
        }
    }

    private static Gamepad GetGamepad(int gamepadIndex)
    {
        if (gamepadIndex > 0)
        {
            int zeroBasedIndex = gamepadIndex - 1;
            if (zeroBasedIndex >= 0 && zeroBasedIndex < Gamepad.all.Count)
                return Gamepad.all[zeroBasedIndex];
        }

        return Gamepad.current;
    }

    private static int GetGamepadIndex(Gamepad gamepad)
    {
        for (int i = 0; i < Gamepad.all.Count; i++)
        {
            if (Gamepad.all[i] == gamepad)
                return i + 1;
        }

        return -1;
    }

    private static string GetGamepadControlName(PlayerGamepadControl control)
    {
        switch (control)
        {
            case PlayerGamepadControl.LeftStickUp: return "Left Stick Up";
            case PlayerGamepadControl.LeftStickDown: return "Left Stick Down";
            case PlayerGamepadControl.LeftStickLeft: return "Left Stick Left";
            case PlayerGamepadControl.LeftStickRight: return "Left Stick Right";
            case PlayerGamepadControl.DpadUp: return "D-Pad Up";
            case PlayerGamepadControl.DpadDown: return "D-Pad Down";
            case PlayerGamepadControl.DpadLeft: return "D-Pad Left";
            case PlayerGamepadControl.DpadRight: return "D-Pad Right";
            case PlayerGamepadControl.ButtonSouth: return "A / Cross";
            case PlayerGamepadControl.ButtonEast: return "B / Circle";
            case PlayerGamepadControl.ButtonWest: return "X / Square";
            case PlayerGamepadControl.ButtonNorth: return "Y / Triangle";
            case PlayerGamepadControl.LeftShoulder: return "LB / L1";
            case PlayerGamepadControl.RightShoulder: return "RB / R1";
            case PlayerGamepadControl.LeftTrigger: return "LT / L2";
            case PlayerGamepadControl.RightTrigger: return "RT / R2";
            case PlayerGamepadControl.Select: return "Select / Back";
            case PlayerGamepadControl.Start: return "Start / Menu";
            case PlayerGamepadControl.LeftStickButton: return "Left Stick Button";
            case PlayerGamepadControl.RightStickButton: return "Right Stick Button";
            default: return control.ToString();
        }
    }

    private static string FormatBindingName(string value)
    {
        return string.IsNullOrEmpty(value) ? string.Empty : value.ToUpperInvariant();
    }

    private static string GetKeyboardKeyDisplayName(KeyCode key)
    {
        switch (key)
        {
            case KeyCode.UpArrow: return "↑";
            case KeyCode.DownArrow: return "↓";
            case KeyCode.LeftArrow: return "←";
            case KeyCode.RightArrow: return "→";
            case KeyCode.Alpha0:
            case KeyCode.Keypad0: return "0";
            case KeyCode.Alpha1:
            case KeyCode.Keypad1: return "1";
            case KeyCode.Alpha2:
            case KeyCode.Keypad2: return "2";
            case KeyCode.Alpha3:
            case KeyCode.Keypad3: return "3";
            case KeyCode.Alpha4:
            case KeyCode.Keypad4: return "4";
            case KeyCode.Alpha5:
            case KeyCode.Keypad5: return "5";
            case KeyCode.Alpha6:
            case KeyCode.Keypad6: return "6";
            case KeyCode.Alpha7:
            case KeyCode.Keypad7: return "7";
            case KeyCode.Alpha8:
            case KeyCode.Keypad8: return "8";
            case KeyCode.Alpha9:
            case KeyCode.Keypad9: return "9";
            case KeyCode.KeypadPlus: return "+";
            case KeyCode.KeypadMinus: return "−";
            case KeyCode.KeypadMultiply: return "×";
            case KeyCode.KeypadDivide: return "÷";
            case KeyCode.KeypadPeriod: return ".";
            case KeyCode.KeypadEnter: return "ENTER";
        }

        string rawName = key.ToString();
        if (string.IsNullOrEmpty(rawName))
            return string.Empty;

        StringBuilder displayName = new StringBuilder(rawName.Length + 4);
        for (int i = 0; i < rawName.Length; i++)
        {
            char current = rawName[i];
            if (i > 0 && char.IsUpper(current) && !char.IsUpper(rawName[i - 1]))
                displayName.Append(' ');

            displayName.Append(char.ToUpperInvariant(current));
        }

        return displayName.ToString();
    }

    private static string GetKeyboardKeyName(int playerNumber, PlayerControlAction action)
    {
        return KeyboardPrefix + ".P" + playerNumber + "." + action;
    }

    private static string GetGamepadKeyName(int playerNumber, PlayerControlAction action)
    {
        return GamepadPrefix + ".P" + playerNumber + "." + action;
    }

    private static int NormalizePlayer(int playerNumber)
    {
        return Mathf.Clamp(playerNumber, 1, 2);
    }
}
