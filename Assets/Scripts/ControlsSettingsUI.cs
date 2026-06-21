using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ControlsSettingsUI : MonoBehaviour
{
    [Serializable]
    public class BindingRow
    {
        public PlayerControlAction action = PlayerControlAction.MoveUp;
        public GameObject rowRoot;
        public Button rebindButton;
        public Text actionText;
        public TMP_Text actionTMP;
        public Text bindingText;
        public TMP_Text bindingTMP;
    }

    [Header("Auto find manual hierarchy")]
    [SerializeField] private bool autoFindManualHierarchy = true;

    [Header("Manual references")]
    [SerializeField] private RectTransform contentRoot;
    [SerializeField] private ScrollRect scrollRect;
    [SerializeField] private RectTransform viewport;
    [SerializeField] private Selectable player1RadioButton;
    [SerializeField] private Selectable player2RadioButton;
    [SerializeField] private Selectable keyboardRadioButton;
    [SerializeField] private Selectable gamepadRadioButton;
    [SerializeField] private Button applyButton;
    [SerializeField] private Button resetCurrentButton;
    [SerializeField] private Button resetAllButton;
    [SerializeField] private Text statusText;
    [SerializeField] private TMP_Text statusTMP;
    [SerializeField] private BindingRow[] manualRows;

    private readonly List<BindingRow> runtimeRows = new List<BindingRow>();
    private readonly Dictionary<Image, Sprite> radioDefaultSprites = new Dictionary<Image, Sprite>();
    private bool wired;
    private bool waitingForInput;
    private int selectedPlayerNumber = 1;
    private PlayerControlDevice selectedDevice = PlayerControlDevice.Keyboard;
    private int waitingPlayerNumber = 1;
    private PlayerControlDevice waitingDevice = PlayerControlDevice.Keyboard;
    private PlayerControlAction waitingAction;

    private static readonly PlayerControlAction[] DisplayedActions =
    {
        PlayerControlAction.MoveUp,
        PlayerControlAction.MoveDown,
        PlayerControlAction.MoveLeft,
        PlayerControlAction.MoveRight,
        PlayerControlAction.Run,
        PlayerControlAction.LightAttack,
        PlayerControlAction.HeavyAttack,
        PlayerControlAction.Dash,
        PlayerControlAction.Roll,
        PlayerControlAction.Block,
        PlayerControlAction.Confirm,
        PlayerControlAction.SelectLeft,
        PlayerControlAction.SelectRight
    };

    private void Awake()
    {
        Setup();
    }

    private void OnEnable()
    {
        Open();
    }

    private void Update()
    {
        if (!waitingForInput)
            return;

        if (Input.GetKeyDown(KeyCode.Escape))
        {
            CancelRebind();
            return;
        }

        if (waitingDevice == PlayerControlDevice.Keyboard)
        {
            KeyCode key;
            if (PlayerInputBindings.TryCaptureKeyboardKey(out key))
            {
                PlayerInputBindings.SetKeyboardKey(waitingPlayerNumber, waitingAction, key);
                FinishRebind();
            }
        }
        else
        {
            PlayerGamepadControl control;
            if (PlayerInputBindings.TryCaptureGamepadControl(out control))
            {
                PlayerInputBindings.SetGamepadControl(waitingPlayerNumber, waitingAction, control);
                FinishRebind();
            }
        }
    }

    private void LateUpdate()
    {
        SyncSelectionRadioButtons();
    }

    public void Open()
    {
        Setup();
        SetVisible(true);
        SyncSelectionRadioButtons();
        RefreshAll();
    }

    public void SetVisible(bool visible)
    {
        gameObject.SetActive(visible);
    }

    public void ShowPlayer1()
    {
        selectedPlayerNumber = 1;
        SyncSelectionRadioButtons();
        RefreshAll();
    }

    public void ShowPlayer2()
    {
        selectedPlayerNumber = 2;
        SyncSelectionRadioButtons();
        RefreshAll();
    }

    public void ShowKeyboard()
    {
        selectedDevice = PlayerControlDevice.Keyboard;
        SyncSelectionRadioButtons();
        RefreshAll();
    }

    public void ShowGamepad()
    {
        selectedDevice = PlayerControlDevice.Gamepad;
        SyncSelectionRadioButtons();
        RefreshAll();
    }

    public void BeginKeyboardRebind(int playerNumber, int actionIndex)
    {
        BeginRebind(playerNumber, PlayerControlDevice.Keyboard, (PlayerControlAction)actionIndex);
    }

    public void BeginGamepadRebind(int playerNumber, int actionIndex)
    {
        BeginRebind(playerNumber, PlayerControlDevice.Gamepad, (PlayerControlAction)actionIndex);
    }

    public void BeginRebind(int playerNumber, PlayerControlDevice device, PlayerControlAction action)
    {
        waitingForInput = true;
        waitingPlayerNumber = Mathf.Clamp(playerNumber, 1, 2);
        waitingDevice = device;
        waitingAction = action;
    }

    public void ApplyBindings()
    {
        RefreshAll();
    }

    public void ResetAllBindings()
    {
        PlayerInputBindings.ResetAll();
        RefreshAll();
    }

    public void ResetCurrentBindings()
    {
        if (selectedDevice == PlayerControlDevice.Keyboard)
            PlayerInputBindings.ResetPlayerKeyboard(selectedPlayerNumber);
        else
            PlayerInputBindings.ResetPlayerGamepad(selectedPlayerNumber);

        RefreshAll();
    }

    public void ResetPlayerBindings(int playerNumber)
    {
        PlayerInputBindings.ResetPlayer(playerNumber);
        RefreshAll();
    }

    public void ResetPlayerKeyboardBindings(int playerNumber)
    {
        PlayerInputBindings.ResetPlayerKeyboard(playerNumber);
        RefreshAll();
    }

    public void ResetPlayerGamepadBindings(int playerNumber)
    {
        PlayerInputBindings.ResetPlayerGamepad(playerNumber);
        RefreshAll();
    }

    public string GetBindingText(int playerNumber, PlayerControlDevice device, PlayerControlAction action)
    {
        return PlayerInputBindings.GetBindingName(playerNumber, device, action);
    }

    private void Setup()
    {
        if (wired)
            return;

        if (autoFindManualHierarchy)
            AutoFindReferences();

        BuildRuntimeRows();
        WireControls();

        wired = true;
    }

    private void AutoFindReferences()
    {
        Transform listRoot = FindChildRecursive(transform, "ListRoot");
        Transform viewportTransform = listRoot != null ? FindChildRecursive(listRoot, "Viewport") : FindChildRecursive(transform, "Viewport");

        if (scrollRect == null)
        {
            if (listRoot != null)
                scrollRect = listRoot.GetComponent<ScrollRect>();

            if (scrollRect == null)
                scrollRect = GetComponentInChildren<ScrollRect>(true);

            if (scrollRect == null && listRoot != null)
                scrollRect = listRoot.gameObject.AddComponent<ScrollRect>();
        }

        if (viewport == null && viewportTransform != null)
            viewport = viewportTransform as RectTransform;

        if (contentRoot == null)
        {
            Transform content = viewportTransform != null ? FindChildRecursive(viewportTransform, "Content") : null;
            if (content == null && listRoot != null)
                content = FindChildRecursive(listRoot, "Content");
            if (content == null)
                content = FindChildRecursive(transform, "Content");
            if (content != null)
                contentRoot = content as RectTransform;
        }

        Transform pickPlayer = FindChildRecursive(transform, "PickPlayer");
        if (pickPlayer != null)
        {
            Transform buttons = FindChildRecursive(pickPlayer, "Buttons");
            player1RadioButton = player1RadioButton != null ? player1RadioButton : FindSelectableInChildren(buttons, "Player1", 0);
            player2RadioButton = player2RadioButton != null ? player2RadioButton : FindSelectableInChildren(buttons, "Player2", 1);
        }

        Transform pickDevice = FindChildRecursive(transform, "PickDevice");
        if (pickDevice != null)
        {
            Transform buttons = FindChildRecursive(pickDevice, "Buttons");
            keyboardRadioButton = keyboardRadioButton != null ? keyboardRadioButton : FindSelectableInChildren(buttons, "Keyboard", 0);
            if (keyboardRadioButton == null)
                keyboardRadioButton = FindSelectableInChildren(buttons, "Player1", 0);

            gamepadRadioButton = gamepadRadioButton != null ? gamepadRadioButton : FindSelectableInChildren(buttons, "Gamepad", 1);
            if (gamepadRadioButton == null)
                gamepadRadioButton = FindSelectableInChildren(buttons, "Player2", 1);
        }

        Transform footer = FindChildRecursive(transform, "Footer");
        if (footer != null)
        {
            applyButton = applyButton != null ? applyButton : FindButtonInChildren(footer, "Apply");
            resetCurrentButton = resetCurrentButton != null ? resetCurrentButton : FindButtonInChildren(footer, "ResetCurrent");
            resetAllButton = resetAllButton != null ? resetAllButton : FindButtonInChildren(footer, "ResetAll");
        }

        if (statusText == null && statusTMP == null)
        {
            Transform status = FindChildRecursive(transform, "Status");
            if (status != null)
            {
                statusText = status.GetComponent<Text>();
                statusTMP = status.GetComponent<TMP_Text>();
            }
        }
    }

    private void BuildRuntimeRows()
    {
        runtimeRows.Clear();

        if (manualRows != null)
        {
            for (int i = 0; i < manualRows.Length; i++)
            {
                BindingRow row = manualRows[i];
                if (row != null && row.rowRoot != null)
                {
                    CompleteRowReferences(row);
                    runtimeRows.Add(row);
                }
            }
        }

        if (contentRoot == null)
            return;

        for (int i = 0; i < DisplayedActions.Length; i++)
        {
            PlayerControlAction action = DisplayedActions[i];
            if (HasRuntimeRow(action))
                continue;

            GameObject rowRoot = FindRowObject(action);

            if (rowRoot == null)
                continue;

            BindingRow row = new BindingRow { action = action, rowRoot = rowRoot };
            CompleteRowReferences(row);
            runtimeRows.Add(row);
        }
    }

    private bool HasRuntimeRow(PlayerControlAction action)
    {
        for (int i = 0; i < runtimeRows.Count; i++)
        {
            if (runtimeRows[i].action == action)
                return true;
        }

        return false;
    }

    private GameObject FindRowObject(PlayerControlAction action)
    {
        string[] names =
        {
            "Row" + action,
            "Row_" + action,
            action.ToString()
        };

        for (int i = 0; i < names.Length; i++)
        {
            Transform row = FindChildRecursive(contentRoot, names[i]);
            if (row != null)
                return row.gameObject;
        }

        return null;
    }


    private void CompleteRowReferences(BindingRow row)
    {
        Transform rowTransform = row.rowRoot.transform;
        Transform action = FindChildRecursive(rowTransform, "Action");
        Transform binding = FindChildRecursive(rowTransform, "Binding");

        if (action != null)
        {
            row.actionText = row.actionText != null ? row.actionText : action.GetComponent<Text>();
            row.actionTMP = row.actionTMP != null ? row.actionTMP : action.GetComponent<TMP_Text>();
        }

        if (binding != null)
        {
            row.rebindButton = row.rebindButton != null ? row.rebindButton : binding.GetComponent<Button>();
            if (row.rebindButton == null)
                row.rebindButton = binding.GetComponentInChildren<Button>(true);

            Transform label = FindChildRecursive(binding, "Label");
            if (label != null)
            {
                row.bindingText = row.bindingText != null ? row.bindingText : label.GetComponent<Text>();
                row.bindingTMP = row.bindingTMP != null ? row.bindingTMP : label.GetComponent<TMP_Text>();
            }
        }
    }

    private void WireControls()
    {
        WireSelectable(player1RadioButton, ShowPlayer1);
        WireSelectable(player2RadioButton, ShowPlayer2);
        WireSelectable(keyboardRadioButton, ShowKeyboard);
        WireSelectable(gamepadRadioButton, ShowGamepad);

        if (applyButton != null)
            applyButton.onClick.AddListener(ApplyBindings);

        if (resetCurrentButton != null)
            resetCurrentButton.onClick.AddListener(ResetCurrentBindings);

        if (resetAllButton != null)
            resetAllButton.onClick.AddListener(ResetAllBindings);

        for (int i = 0; i < runtimeRows.Count; i++)
            WireRow(runtimeRows[i]);
    }

    private void WireRow(BindingRow row)
    {
        if (row.rebindButton == null)
            return;

        PlayerControlAction capturedAction = row.action;
        row.rebindButton.onClick.AddListener(() => BeginRebind(selectedPlayerNumber, selectedDevice, capturedAction));
    }

    private void WireSelectable(Selectable selectable, UnityEngine.Events.UnityAction action)
    {
        if (selectable == null)
            return;

        Toggle toggle = selectable as Toggle;
        if (toggle != null)
        {
            toggle.onValueChanged.AddListener(isOn =>
            {
                if (isOn)
                    action.Invoke();
                else
                    SyncSelectionRadioButtons();
            });
            return;
        }

        Button button = selectable as Button;
        if (button != null)
            button.onClick.AddListener(action);
    }

    private void SyncSelectionRadioButtons()
    {
        SetToggleState(player1RadioButton, selectedPlayerNumber == 1);
        SetToggleState(player2RadioButton, selectedPlayerNumber == 2);
        SetToggleState(keyboardRadioButton, selectedDevice == PlayerControlDevice.Keyboard);
        SetToggleState(gamepadRadioButton, selectedDevice == PlayerControlDevice.Gamepad);
    }

    private void SetToggleState(Selectable selectable, bool isOn)
    {
        Toggle toggle = selectable as Toggle;
        if (toggle == null)
            return;

        toggle.SetIsOnWithoutNotify(isOn);
        ApplyToggleSpriteState(toggle, isOn);
    }

    private void ApplyToggleSpriteState(Toggle toggle, bool isOn)
    {
        Image targetImage = toggle.targetGraphic as Image;
        Sprite selectedSprite = toggle.spriteState.selectedSprite;

        if (targetImage == null || selectedSprite == null)
            return;

        if (!radioDefaultSprites.ContainsKey(targetImage))
            radioDefaultSprites[targetImage] = targetImage.sprite;

        targetImage.overrideSprite = isOn ? selectedSprite : radioDefaultSprites[targetImage];
    }

    public void RefreshAll()
    {
        for (int i = 0; i < runtimeRows.Count; i++)
        {
            BindingRow row = runtimeRows[i];
            SetLabel(row.bindingText, row.bindingTMP, PlayerInputBindings.GetBindingName(selectedPlayerNumber, selectedDevice, row.action));
        }
    }

    private void FinishRebind()
    {
        waitingForInput = false;
        RefreshAll();
    }

    private void CancelRebind()
    {
        waitingForInput = false;
    }

    private void SetLabel(Text text, TMP_Text tmp, string value)
    {
        if (text != null)
            text.text = value;

        if (tmp != null)
            tmp.text = value;
    }

    private Selectable FindSelectableInChildren(Transform parent, string childName, int fallbackIndex)
    {
        if (parent == null)
            return null;

        Transform named = FindChildRecursive(parent, childName);
        if (named != null)
        {
            Selectable selectable = named.GetComponent<Selectable>();
            if (selectable == null)
                selectable = named.GetComponentInChildren<Selectable>(true);
            if (selectable != null)
                return selectable;
        }

        if (fallbackIndex >= 0 && fallbackIndex < parent.childCount)
        {
            Selectable selectable = parent.GetChild(fallbackIndex).GetComponent<Selectable>();
            if (selectable == null)
                selectable = parent.GetChild(fallbackIndex).GetComponentInChildren<Selectable>(true);
            return selectable;
        }

        return null;
    }
    private Button FindButtonInChildren(Transform parent, string childName)
    {
        Transform child = FindChildRecursive(parent, childName);
        if (child == null)
            return null;

        Button button = child.GetComponent<Button>();
        if (button == null)
            button = child.GetComponentInChildren<Button>(true);
        return button;
    }

    private Transform FindChildRecursive(Transform parent, string childName)
    {
        if (parent == null)
            return null;

        for (int i = 0; i < parent.childCount; i++)
        {
            Transform child = parent.GetChild(i);
            if (child.name == childName)
                return child;

            Transform nested = FindChildRecursive(child, childName);
            if (nested != null)
                return nested;
        }

        return null;
    }
}








