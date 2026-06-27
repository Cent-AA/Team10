using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Serialization;
using UnityEngine.UI;

/// <summary>
/// Controls the two serialized tutorial panels that live in TrainingScene.
/// No tutorial objects are created at runtime.
/// </summary>
public sealed class TrainingTutorialManager : MonoBehaviour
{
    private const string TrainingSceneName = "TrainingScene";

    [Serializable]
    public sealed class PlayerTutorialView
    {
        [Range(1, 2)] public int playerNumber = 1;
        public GameObject hint;
        public GameObject panel;
        public GameObject basicsContent;
        public GameObject cardsContent;
        [FormerlySerializedAs("basicsTabButton")] public Toggle basicsTabToggle;
        [FormerlySerializedAs("cardsTabButton")] public Toggle cardsTabToggle;
        public Button closeButton;

        [NonSerialized] public bool isOpen;
        [NonSerialized] public int selectedTab;
    }

    [Header("Editable scene hierarchy")]
    [SerializeField] private PlayerTutorialView playerOne = new PlayerTutorialView { playerNumber = 1 };
    [SerializeField] private PlayerTutorialView playerTwo = new PlayerTutorialView { playerNumber = 2 };

    private static readonly bool[] PlayerInputBlocked = new bool[3];
    private static TrainingTutorialManager instance;
    private readonly Dictionary<Image, Sprite> radioDefaultSprites = new Dictionary<Image, Sprite>();

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics()
    {
        instance = null;
        PlayerInputBlocked[1] = false;
        PlayerInputBlocked[2] = false;
    }

    public static bool IsPlayerInputBlocked(int playerNumber)
    {
        return playerNumber >= 1 && playerNumber <= 2 && PlayerInputBlocked[playerNumber];
    }

    private void Awake()
    {
        if (gameObject.scene.name != TrainingSceneName)
        {
            gameObject.SetActive(false);
            return;
        }

        instance = this;
        PrepareView(playerOne, 1);
        PrepareView(playerTwo, 2);
        SetOpen(playerOne, false);
        SetOpen(playerTwo, false);
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Tab))
            SetOpen(playerOne, !playerOne.isOpen);

        if (WasPlusPressedThisFrame())
            SetOpen(playerTwo, !playerTwo.isOpen);

        if (Input.GetKeyDown(KeyCode.Escape) && (playerOne.isOpen || playerTwo.isOpen))
        {
            SetOpen(playerOne, false);
            SetOpen(playerTwo, false);
        }

        HandleTabInput(playerOne);
        HandleTabInput(playerTwo);

        bool gameplayRunning = Time.timeScale > 0f;
        UpdateHintVisibility(playerOne, gameplayRunning);
        UpdateHintVisibility(playerTwo, gameplayRunning);
    }

    private void PrepareView(PlayerTutorialView view, int playerNumber)
    {
        if (view == null)
            return;

        view.playerNumber = playerNumber;
        view.selectedTab = 0;
        view.isOpen = false;

        if (view.basicsTabToggle != null)
            view.basicsTabToggle.onValueChanged.AddListener(isOn =>
            {
                if (isOn)
                    SetTab(view, 0);
            });
        if (view.cardsTabToggle != null)
            view.cardsTabToggle.onValueChanged.AddListener(isOn =>
            {
                if (isOn)
                    SetTab(view, 1);
            });
        if (view.closeButton != null)
            view.closeButton.onClick.AddListener(() => SetOpen(view, false));

        SetTab(view, 0);
    }

    private static bool WasPlusPressedThisFrame()
    {
        Keyboard keyboard = Keyboard.current;
        if (keyboard == null)
            return Input.GetKeyDown(KeyCode.KeypadPlus);

        bool numpadPlus = keyboard.numpadPlusKey.wasPressedThisFrame;
        bool mainKeyboardPlus = keyboard.equalsKey.wasPressedThisFrame &&
                                (keyboard.leftShiftKey.isPressed || keyboard.rightShiftKey.isPressed);
        return numpadPlus || mainKeyboardPlus;
    }

    private void HandleTabInput(PlayerTutorialView view)
    {
        if (view == null || !view.isOpen)
            return;

        if (WasTutorialActionPressed(view.playerNumber, PlayerControlAction.SelectLeft))
            SetTab(view, 0);
        else if (WasTutorialActionPressed(view.playerNumber, PlayerControlAction.SelectRight))
            SetTab(view, 1);
    }

    private void SetOpen(PlayerTutorialView view, bool open)
    {
        if (view == null)
            return;

        view.isOpen = open;
        PlayerInputBlocked[view.playerNumber] = open;

        if (open)
            RefreshBindingLabels(view);

        if (view.panel != null)
        {
            view.panel.SetActive(open);
            if (open)
                view.panel.transform.SetAsLastSibling();
        }

        if (view.hint != null)
            view.hint.SetActive(!open && Time.timeScale > 0f);

        if (open)
            SetTab(view, view.selectedTab);
    }

    private static bool WasTutorialActionPressed(int playerNumber, PlayerControlAction action)
    {
        if (GetInputDevice(playerNumber) == PlayerControlDevice.Gamepad)
        {
            return PlayerInputBindings.GetGamepadActionDownIgnoringGameplayBlock(
                playerNumber,
                action,
                GetGamepadIndex(playerNumber));
        }

        return PlayerInputBindings.GetKeyboardActionDownIgnoringGameplayBlock(playerNumber, action);
    }

    private static void RefreshBindingLabels(PlayerTutorialView view)
    {
        if (view == null)
            return;

        int playerNumber = view.playerNumber;
        PlayerControlDevice device = GetInputDevice(playerNumber);
        string navigation = BuildNavigationBinding(playerNumber, device);

        TMP_Text basicsText = GetText(view.basicsContent);
        if (basicsText != null)
        {
            string text = basicsText.text;
            text = ReplaceLinkContent(text, "Move", BuildMovementBinding(playerNumber, device));
            text = ReplaceLinkContent(text, "Run", GetBinding(playerNumber, device, PlayerControlAction.Run));
            text = ReplaceLinkContent(text, "Dash", GetBinding(playerNumber, device, PlayerControlAction.Dash));
            text = ReplaceLinkContent(text, "Roll", GetBinding(playerNumber, device, PlayerControlAction.Roll));
            text = ReplaceLinkContent(text, "LightAttack", GetBinding(playerNumber, device, PlayerControlAction.LightAttack));
            text = ReplaceLinkContent(text, "HeavyAttack", GetBinding(playerNumber, device, PlayerControlAction.HeavyAttack));
            text = ReplaceLinkContent(text, "Block", GetBinding(playerNumber, device, PlayerControlAction.Block));
            text = ReplaceLinkContent(text, "Shoot", GetBinding(playerNumber, device, PlayerControlAction.Shoot));
            text = ReplaceLinkContent(text, "Navigate", navigation);
            SetTextIfChanged(basicsText, text);
        }

        TMP_Text cardsText = GetText(view.cardsContent);
        if (cardsText != null)
            SetTextIfChanged(cardsText, ReplaceLinkContent(cardsText.text, "Navigate", navigation));

        TMP_Text instructionsText = FindInstructionsText(view.panel);
        if (instructionsText != null)
            SetTextIfChanged(instructionsText, ReplaceLinkContent(instructionsText.text, "Navigate", navigation));
    }

    private static PlayerControlDevice GetInputDevice(int playerNumber)
    {
        InputJoinManager.InputType inputType = playerNumber == 1
            ? InputJoinManager.player1Input
            : InputJoinManager.player2Input;

        return inputType == InputJoinManager.InputType.Gamepad
            ? PlayerControlDevice.Gamepad
            : PlayerControlDevice.Keyboard;
    }

    private static int GetGamepadIndex(int playerNumber)
    {
        return playerNumber == 1
            ? InputJoinManager.player1GamepadIndex
            : InputJoinManager.player2GamepadIndex;
    }

    private static string GetBinding(
        int playerNumber,
        PlayerControlDevice device,
        PlayerControlAction action)
    {
        return PlayerInputBindings.GetBindingName(playerNumber, device, action);
    }

    private static string BuildMovementBinding(int playerNumber, PlayerControlDevice device)
    {
        if (device == PlayerControlDevice.Gamepad)
        {
            PlayerGamepadControl up = PlayerInputBindings.GetGamepadControl(playerNumber, PlayerControlAction.MoveUp);
            PlayerGamepadControl down = PlayerInputBindings.GetGamepadControl(playerNumber, PlayerControlAction.MoveDown);
            PlayerGamepadControl left = PlayerInputBindings.GetGamepadControl(playerNumber, PlayerControlAction.MoveLeft);
            PlayerGamepadControl right = PlayerInputBindings.GetGamepadControl(playerNumber, PlayerControlAction.MoveRight);

            if (up == PlayerGamepadControl.LeftStickUp && down == PlayerGamepadControl.LeftStickDown &&
                left == PlayerGamepadControl.LeftStickLeft && right == PlayerGamepadControl.LeftStickRight)
                return "LEFT STICK";

            if (up == PlayerGamepadControl.DpadUp && down == PlayerGamepadControl.DpadDown &&
                left == PlayerGamepadControl.DpadLeft && right == PlayerGamepadControl.DpadRight)
                return "D-PAD";
        }

        return string.Join(" ", new[]
        {
            GetBinding(playerNumber, device, PlayerControlAction.MoveUp),
            GetBinding(playerNumber, device, PlayerControlAction.MoveLeft),
            GetBinding(playerNumber, device, PlayerControlAction.MoveDown),
            GetBinding(playerNumber, device, PlayerControlAction.MoveRight)
        });
    }

    private static string BuildNavigationBinding(int playerNumber, PlayerControlDevice device)
    {
        if (device == PlayerControlDevice.Gamepad)
        {
            PlayerGamepadControl left = PlayerInputBindings.GetGamepadControl(playerNumber, PlayerControlAction.SelectLeft);
            PlayerGamepadControl right = PlayerInputBindings.GetGamepadControl(playerNumber, PlayerControlAction.SelectRight);

            if (left == PlayerGamepadControl.LeftStickLeft && right == PlayerGamepadControl.LeftStickRight)
                return "LEFT STICK";
            if (left == PlayerGamepadControl.DpadLeft && right == PlayerGamepadControl.DpadRight)
                return "D-PAD";
        }

        return GetBinding(playerNumber, device, PlayerControlAction.SelectLeft) + " / " +
               GetBinding(playerNumber, device, PlayerControlAction.SelectRight);
    }

    private static TMP_Text GetText(GameObject root)
    {
        if (root == null)
            return null;

        TMP_Text text = root.GetComponent<TMP_Text>();
        return text != null ? text : root.GetComponentInChildren<TMP_Text>(true);
    }

    private static TMP_Text FindInstructionsText(GameObject panel)
    {
        if (panel == null)
            return null;

        Transform instructions = panel.transform.Find("InfoPanel/Instructons") ??
                                 panel.transform.Find("InfoPanel/Instructions");
        return instructions != null ? instructions.GetComponent<TMP_Text>() : null;
    }

    private static string ReplaceLinkContent(string source, string linkId, string value)
    {
        if (string.IsNullOrEmpty(source))
            return source;

        string openingTag = "<link=\"" + linkId + "\">";
        int contentStart = source.IndexOf(openingTag, StringComparison.Ordinal);
        if (contentStart < 0)
            return source;

        contentStart += openingTag.Length;
        int contentEnd = source.IndexOf("</link>", contentStart, StringComparison.Ordinal);
        if (contentEnd < 0)
            return source;

        return source.Substring(0, contentStart) + value + source.Substring(contentEnd);
    }

    private static void SetTextIfChanged(TMP_Text target, string value)
    {
        if (target != null && target.text != value)
            target.text = value;
    }

    private void SetTab(PlayerTutorialView view, int tabIndex)
    {
        if (view == null)
            return;

        view.selectedTab = Mathf.Clamp(tabIndex, 0, 1);
        if (view.basicsContent != null)
            view.basicsContent.SetActive(view.selectedTab == 0);
        if (view.cardsContent != null)
            view.cardsContent.SetActive(view.selectedTab == 1);

        SetRadioState(view.basicsTabToggle, view.selectedTab == 0);
        SetRadioState(view.cardsTabToggle, view.selectedTab == 1);
    }

    private void SetRadioState(Toggle toggle, bool isOn)
    {
        if (toggle == null)
            return;

        toggle.SetIsOnWithoutNotify(isOn);

        Image targetImage = toggle.targetGraphic as Image;
        Sprite selectedSprite = toggle.spriteState.selectedSprite;
        if (targetImage == null || selectedSprite == null)
            return;

        if (!radioDefaultSprites.ContainsKey(targetImage))
            radioDefaultSprites[targetImage] = targetImage.sprite;

        targetImage.overrideSprite = isOn ? selectedSprite : radioDefaultSprites[targetImage];
    }

    private static void UpdateHintVisibility(PlayerTutorialView view, bool gameplayRunning)
    {
        if (view != null && view.hint != null && !view.isOpen)
            view.hint.SetActive(gameplayRunning);
    }

#if UNITY_EDITOR
    public void ConfigureForEditor(PlayerTutorialView firstPlayer, PlayerTutorialView secondPlayer)
    {
        playerOne = firstPlayer;
        playerTwo = secondPlayer;
    }
#endif

    private void OnValidate()
    {
        if (playerOne != null)
            playerOne.playerNumber = 1;
        if (playerTwo != null)
            playerTwo.playerNumber = 2;
    }

    private void OnDestroy()
    {
        if (instance != this)
            return;

        PlayerInputBlocked[1] = false;
        PlayerInputBlocked[2] = false;
        instance = null;
    }
}
