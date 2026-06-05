using UnityEngine;
using UnityEngine.UI;

public class CharacterSelector : MonoBehaviour
{
    [Header("Карты персонажей")]
    public RectTransform[] characterCards;

    [Header("Игрок 1")]
    public RectTransform p1Arrow;
    public RectTransform p1Letter;
    public RectTransform p1Number;

    [Header("Игрок 2")]
    public RectTransform p2Arrow;
    public RectTransform p2Letter;
    public RectTransform p2Number;

    [Header("Парение")]
    public float floatAmplitude = 15f;
    public float floatSpeed = 2f;
    public float stopSmooth = 5f;

    [Header("Настройки")]
    public float arrowMoveSpeed = 10f;
    public float arrowOffsetY = 80f;
    public float letterOffsetY = 50f;
    public float numberOffsetX = 25f;
    public float selectedScale = 1.15f;
    public float scaleSpeed = 8f;

    [Header("Подсветка")]
    public Color p1HighlightColor = new Color(0.3f, 0.6f, 1f, 1f);
    public Color p2HighlightColor = new Color(1f, 0.7f, 0.2f, 1f);
    public Color normalColor = Color.white;

    [Header("Переход")]
    public CharacterSelectTransition transition;

    // Статические данные — доступны из арены
    public static int player1Character = 0;
    public static int player2Character = 0;

    private int p1Selection = 0;
    private int p2Selection = 1;
    private bool p1Confirmed = false;
    private bool p2Confirmed = false;
    private float p1FloatAmount = 1f;
    private float p2FloatAmount = 1f;
    private bool active = false;
    private bool bothConfirmed = false;

    public void Activate() { active = true; }

    void Update()
    {
        if (!active || bothConfirmed) return;

        HandleP1Input();
        HandleP2Input();
        UpdateArrows();
        UpdateFloating();
        UpdateCardVisuals();

        if (p1Confirmed && p2Confirmed && !bothConfirmed)
        {
            bothConfirmed = true;
            player1Character = p1Selection;
            player2Character = p2Selection;

            // Запускаем переход в арену
            if (transition != null)
                transition.GoToArena();
        }
    }

    void HandleP1Input()
    {
        if (p1Confirmed) return;
        var type = InputJoinManager.player1Input;
        int pad = InputJoinManager.player1GamepadIndex;
        bool left = false, right = false, confirm = false;

        switch (type)
        {
            case InputJoinManager.InputType.KeyboardWASD:
                left = Input.GetKeyDown(KeyCode.A);
                right = Input.GetKeyDown(KeyCode.D);
                confirm = Input.GetKeyDown(KeyCode.W);
                break;
            case InputJoinManager.InputType.KeyboardArrows:
                left = Input.GetKeyDown(KeyCode.LeftArrow);
                right = Input.GetKeyDown(KeyCode.RightArrow);
                confirm = Input.GetKeyDown(KeyCode.UpArrow);
                break;
            case InputJoinManager.InputType.Gamepad:
                left = GetGamepadButton(pad, 13);
                right = GetGamepadButton(pad, 14);
                confirm = GetGamepadButton(pad, 0);
                break;
        }

        if (left) p1Selection = Mathf.Max(0, p1Selection - 1);
        if (right) p1Selection = Mathf.Min(characterCards.Length - 1, p1Selection + 1);
        if (confirm) p1Confirmed = true;
    }

    void HandleP2Input()
    {
        if (p2Confirmed) return;
        var type = InputJoinManager.player2Input;
        int pad = InputJoinManager.player2GamepadIndex;
        bool left = false, right = false, confirm = false;

        switch (type)
        {
            case InputJoinManager.InputType.KeyboardWASD:
                left = Input.GetKeyDown(KeyCode.A);
                right = Input.GetKeyDown(KeyCode.D);
                confirm = Input.GetKeyDown(KeyCode.W);
                break;
            case InputJoinManager.InputType.KeyboardArrows:
                left = Input.GetKeyDown(KeyCode.LeftArrow);
                right = Input.GetKeyDown(KeyCode.RightArrow);
                confirm = Input.GetKeyDown(KeyCode.UpArrow);
                break;
            case InputJoinManager.InputType.Gamepad:
                left = GetGamepadButton(pad, 13);
                right = GetGamepadButton(pad, 14);
                confirm = GetGamepadButton(pad, 0);
                break;
        }

        if (left) p2Selection = Mathf.Max(0, p2Selection - 1);
        if (right) p2Selection = Mathf.Min(characterCards.Length - 1, p2Selection + 1);
        if (confirm) p2Confirmed = true;
    }

    bool GetGamepadButton(int pad, int button)
    {
        KeyCode kc = (KeyCode)System.Enum.Parse(typeof(KeyCode), "Joystick" + pad + "Button" + button);
        return Input.GetKeyDown(kc);
    }

    void UpdateArrows()
    {
        MoveToCard(p1Arrow, p1Selection);
        MoveToCard(p2Arrow, p2Selection);
    }

    void MoveToCard(RectTransform element, int selection)
    {
        if (element == null || selection >= characterCards.Length) return;
        Vector2 cardPos = characterCards[selection].anchoredPosition;
        float cardHeight = characterCards[selection].sizeDelta.y;
        Vector2 target = new Vector2(cardPos.x, cardPos.y + cardHeight / 2f + arrowOffsetY);
        element.anchoredPosition = Vector2.Lerp(element.anchoredPosition, target, Time.deltaTime * arrowMoveSpeed);
    }

    void UpdateFloating()
    {
        if (p1Confirmed) p1FloatAmount = Mathf.Lerp(p1FloatAmount, 0f, Time.deltaTime * stopSmooth);
        if (p2Confirmed) p2FloatAmount = Mathf.Lerp(p2FloatAmount, 0f, Time.deltaTime * stopSmooth);
        FloatElements(p1Letter, p1Number, p1Arrow, p1FloatAmount, 0f);
        FloatElements(p2Letter, p2Number, p2Arrow, p2FloatAmount, Mathf.PI);
    }

    void FloatElements(RectTransform letter, RectTransform number, RectTransform arrow, float amount, float phase)
    {
        if (arrow == null) return;
        float floatY = Mathf.Sin(Time.time * floatSpeed + phase) * floatAmplitude * amount;
        Vector2 arrowPos = arrow.anchoredPosition;
        Vector2 basePos = new Vector2(arrowPos.x, arrowPos.y + letterOffsetY);
        if (letter != null) letter.anchoredPosition = new Vector2(basePos.x - numberOffsetX, basePos.y + floatY);
        if (number != null) number.anchoredPosition = new Vector2(basePos.x + numberOffsetX, basePos.y + floatY);
    }

    void UpdateCardVisuals()
    {
        for (int i = 0; i < characterCards.Length; i++)
        {
            bool isP1 = (i == p1Selection);
            bool isP2 = (i == p2Selection);
            float targetScale = (isP1 || isP2) ? selectedScale : 1f;
            float current = characterCards[i].localScale.x;
            float newScale = Mathf.Lerp(current, targetScale, Time.deltaTime * scaleSpeed);
            characterCards[i].localScale = new Vector3(newScale, newScale, 1f);

            Image img = characterCards[i].GetComponent<Image>();
            if (img != null)
            {
                Color target;
                if (isP1 && isP2) target = Color.Lerp(p1HighlightColor, p2HighlightColor, 0.5f);
                else if (isP1) target = p1HighlightColor;
                else if (isP2) target = p2HighlightColor;
                else target = normalColor;
                img.color = Color.Lerp(img.color, target, Time.deltaTime * scaleSpeed);
            }
        }
    }
}