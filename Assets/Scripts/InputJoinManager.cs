using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class InputJoinManager : MonoBehaviour
{
    [Header("P1 Слот")]
    public RectTransform slot1Panel;
    public Image slot1KeyboardIcon;       // WASD картинка
    public Image slot1GamepadIcon;        // Геймпад картинка
    public Sprite wasdNormal;             // WASD обычный
    public Sprite wasdPressed;            // WASD нажатый

    [Header("P2 Слот")]
    public RectTransform slot2Panel;
    public Image slot2KeyboardIcon;       // Стрелки картинка
    public Image slot2GamepadIcon;        // Геймпад картинка
    public Sprite arrowsNormal;           // Стрелки обычные
    public Sprite arrowsPressed;          // Стрелки нажатые

    [Header("Общий геймпад спрайт")]
    public Sprite gamepadSprite;          // Иконка геймпада

    [Header("Настройки анимации")]
    public float switchInterval = 2f;     // Время между сменой иконок
    public float fadeSpeed = 3f;          // Скорость затухания/появления
    public float floatAmplitude = 10f;    // Парение
    public float floatSpeed = 2f;
    public float fadeOutDuration = 0.5f;

    // Статические данные
    public static InputType player1Input = InputType.None;
    public static InputType player2Input = InputType.None;
    public static int player1GamepadIndex = -1;
    public static int player2GamepadIndex = -1;
    public static bool bothJoined = false;

    public enum InputType { None, KeyboardWASD, KeyboardArrows, Gamepad }

    private bool p1Joined = false;
    private bool p2Joined = false;
    private bool joinPhaseComplete = false;

    // Какая иконка сейчас активна (true = клавиатура, false = геймпад)
    private bool slot1ShowKeyboard = true;
    private bool slot2ShowKeyboard = true;
    private float switchTimer = 0f;

    public System.Action OnBothPlayersJoined;

    void Start()
    {
        player1Input = InputType.None;
        player2Input = InputType.None;
        player1GamepadIndex = -1;
        player2GamepadIndex = -1;
        bothJoined = false;

        // Начальные спрайты
        if (slot1KeyboardIcon != null) slot1KeyboardIcon.sprite = wasdNormal;
        if (slot1GamepadIcon != null)
        {
            slot1GamepadIcon.sprite = gamepadSprite;
            SetAlpha(slot1GamepadIcon, 0f);
        }

        if (slot2KeyboardIcon != null) slot2KeyboardIcon.sprite = arrowsNormal;
        if (slot2GamepadIcon != null)
        {
            slot2GamepadIcon.sprite = gamepadSprite;
            SetAlpha(slot2GamepadIcon, 0f);
        }
    }

    void Update()
    {
        if (joinPhaseComplete) return;

        // Чередование иконок
        switchTimer += Time.deltaTime;
        if (switchTimer >= switchInterval)
        {
            switchTimer = 0f;
            if (!p1Joined) slot1ShowKeyboard = !slot1ShowKeyboard;
            if (!p2Joined) slot2ShowKeyboard = !slot2ShowKeyboard;
        }

        // Плавная смена + парение
        if (!p1Joined)
        {
            AnimateSlot(slot1KeyboardIcon, slot1GamepadIcon, slot1ShowKeyboard, 0f);
        }
        if (!p2Joined)
        {
            AnimateSlot(slot2KeyboardIcon, slot2GamepadIcon, slot2ShowKeyboard, Mathf.PI);
        }

        // Проверяем ввод
        if (!p1Joined && PlayerInputBindings.GetKeyboardActionDown(1, PlayerControlAction.Confirm))
            JoinPlayer1(InputType.KeyboardWASD, -1);

        if (p1Joined && !p2Joined && PlayerInputBindings.GetKeyboardActionDown(2, PlayerControlAction.Confirm))
            JoinPlayer2(InputType.KeyboardArrows, -1);

        for (int g = 1; g <= 4; g++)
        {
            if (!p1Joined && PlayerInputBindings.GetGamepadActionDown(1, PlayerControlAction.Confirm, g))
            {
                JoinPlayer1(InputType.Gamepad, g);
            }
            else if (p1Joined && !p2Joined && PlayerInputBindings.GetGamepadActionDown(2, PlayerControlAction.Confirm, g))
            {
                JoinPlayer2(InputType.Gamepad, g);
            }
        }
    }

    void AnimateSlot(Image keyboardImg, Image gamepadImg, bool showKeyboard, float phase)
    {
        if (keyboardImg == null || gamepadImg == null) return;

        // Парение
        float floatY = Mathf.Sin(Time.time * floatSpeed + phase) * floatAmplitude;

        // Плавная смена прозрачности
        float kbAlpha = keyboardImg.color.a;
        float gpAlpha = gamepadImg.color.a;

        float kbTarget = showKeyboard ? 1f : 0f;
        float gpTarget = showKeyboard ? 0f : 1f;

        kbAlpha = Mathf.Lerp(kbAlpha, kbTarget, Time.deltaTime * fadeSpeed);
        gpAlpha = Mathf.Lerp(gpAlpha, gpTarget, Time.deltaTime * fadeSpeed);

        SetAlpha(keyboardImg, kbAlpha);
        SetAlpha(gamepadImg, gpAlpha);

        // Парение для видимой иконки
        Vector2 kbPos = keyboardImg.rectTransform.anchoredPosition;
        Vector2 gpPos = gamepadImg.rectTransform.anchoredPosition;
        keyboardImg.rectTransform.anchoredPosition = new Vector2(kbPos.x, floatY);
        gamepadImg.rectTransform.anchoredPosition = new Vector2(gpPos.x, floatY);
    }

    void TryJoin(InputType type, int gamepadIndex)
    {
        if (p1Joined && IsSameInput(player1Input, player1GamepadIndex, type, gamepadIndex))
            return;
        if (p2Joined && IsSameInput(player2Input, player2GamepadIndex, type, gamepadIndex))
            return;

        if (!p1Joined)
        {
            JoinPlayer1(type, gamepadIndex);
        }
        else if (!p2Joined)
        {
            JoinPlayer2(type, gamepadIndex);
        }
    }

    void JoinPlayer1(InputType type, int gamepadIndex)
    {
        if (p1Joined)
            return;
        if (p2Joined && IsSameInput(player2Input, player2GamepadIndex, type, gamepadIndex))
            return;

        p1Joined = true;
        player1Input = type;
        player1GamepadIndex = gamepadIndex;
        LockSlot(slot1KeyboardIcon, slot1GamepadIcon, type, wasdPressed);
        CompleteJoinIfReady();
    }

    void JoinPlayer2(InputType type, int gamepadIndex)
    {
        if (!p1Joined || p2Joined)
            return;
        if (p1Joined && IsSameInput(player1Input, player1GamepadIndex, type, gamepadIndex))
            return;

        p2Joined = true;
        player2Input = type;
        player2GamepadIndex = gamepadIndex;
        LockSlot(slot2KeyboardIcon, slot2GamepadIcon, type, arrowsPressed);
        CompleteJoinIfReady();
    }

    void CompleteJoinIfReady()
    {
        if (!p1Joined || !p2Joined || bothJoined)
            return;

        bothJoined = true;
        StartCoroutine(OnBothJoined());
    }

    void LockSlot(Image keyboardImg, Image gamepadImg, InputType type, Sprite pressedSprite)
    {
        if (type == InputType.Gamepad)
        {
            // Показать геймпад, скрыть клавиатуру
            SetAlpha(gamepadImg, 1f);
            SetAlpha(keyboardImg, 0f);
        }
        else
        {
            // Показать нажатую клавиатуру, скрыть геймпад
            if (keyboardImg != null && pressedSprite != null)
                keyboardImg.sprite = pressedSprite;
            SetAlpha(keyboardImg, 1f);
            SetAlpha(gamepadImg, 0f);
        }
    }

    bool IsSameInput(InputType existingType, int existingPad, InputType newType, int newPad)
    {
        if (existingType != newType) return false;
        if (existingType == InputType.Gamepad) return existingPad == newPad;
        return true;
    }

    IEnumerator OnBothJoined()
    {
        yield return new WaitForSeconds(1.5f);

        float elapsed = 0f;
        CanvasGroup cg1 = GetOrAddCG(slot1Panel);
        CanvasGroup cg2 = GetOrAddCG(slot2Panel);

        while (elapsed < fadeOutDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / fadeOutDuration;
            if (cg1 != null) cg1.alpha = 1f - t;
            if (cg2 != null) cg2.alpha = 1f - t;
            yield return null;
        }

        if (slot1Panel != null) slot1Panel.gameObject.SetActive(false);
        if (slot2Panel != null) slot2Panel.gameObject.SetActive(false);

        joinPhaseComplete = true;
        OnBothPlayersJoined?.Invoke();
    }

    void SetAlpha(Image img, float alpha)
    {
        if (img == null) return;
        Color c = img.color;
        c.a = alpha;
        img.color = c;
    }

    CanvasGroup GetOrAddCG(RectTransform rt)
    {
        if (rt == null) return null;
        CanvasGroup cg = rt.GetComponent<CanvasGroup>();
        if (cg == null) cg = rt.gameObject.AddComponent<CanvasGroup>();
        return cg;
    }
}