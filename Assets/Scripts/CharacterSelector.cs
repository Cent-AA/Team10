using UnityEngine;
using UnityEngine.UI;

public class CharacterSelector : MonoBehaviour
{
    [Header("Карты персонажей")]
    public RectTransform[] characterCards;

    [Header("Игрок 1 — элементы стрелки")]
    public RectTransform p1Arrow;        // Стрелка (шеврон)
    public RectTransform p1Letter;       // Буква P
    public RectTransform p1Number;       // Цифра 1

    [Header("Игрок 2 — элементы стрелки")]
    public RectTransform p2Arrow;        // Стрелка (шеврон)
    public RectTransform p2Letter;       // Буква P
    public RectTransform p2Number;       // Цифра 2

    [Header("Парение П и цифры")]
    public float floatAmplitude = 15f;   // Высота парения (пикселей)
    public float floatSpeed = 2f;        // Скорость парения
    public float stopSmooth = 5f;        // Плавность остановки

    [Header("Настройки выбора")]
    public float arrowMoveSpeed = 10f;
    public float arrowOffsetY = 80f;
    public float letterOffsetY = 50f;    // Отступ П и цифры над стрелкой
    public float numberOffsetX = 25f;    // Расстояние цифры от П
    public float selectedScale = 1.15f;
    public float scaleSpeed = 8f;

    [Header("Подсветка")]
    public Color p1HighlightColor = new Color(0.3f, 0.6f, 1f, 1f);
    public Color p2HighlightColor = new Color(1f, 0.7f, 0.2f, 1f);
    public Color normalColor = Color.white;

    private int p1Selection = 0;
    private int p2Selection = 1;
    private bool p1Confirmed = false;
    private bool p2Confirmed = false;
    private float p1FloatAmount = 1f;    // 1 = парит, 0 = остановился
    private float p2FloatAmount = 1f;

    void Update()
    {
        HandleInput();
        UpdateArrows();
        UpdateFloating();
        UpdateCardVisuals();

        if (p1Confirmed && p2Confirmed)
            OnBothConfirmed();
    }

    void HandleInput()
    {
        // Игрок 1: A/D + W
        if (!p1Confirmed)
        {
            if (Input.GetKeyDown(KeyCode.A))
                p1Selection = Mathf.Max(0, p1Selection - 1);
            if (Input.GetKeyDown(KeyCode.D))
                p1Selection = Mathf.Min(characterCards.Length - 1, p1Selection + 1);
            if (Input.GetKeyDown(KeyCode.W))
                p1Confirmed = true;
        }

        // Игрок 2: Стрелки + Up
        if (!p2Confirmed)
        {
            if (Input.GetKeyDown(KeyCode.LeftArrow))
                p2Selection = Mathf.Max(0, p2Selection - 1);
            if (Input.GetKeyDown(KeyCode.RightArrow))
                p2Selection = Mathf.Min(characterCards.Length - 1, p2Selection + 1);
            if (Input.GetKeyDown(KeyCode.UpArrow))
                p2Confirmed = true;
        }
    }

    void UpdateArrows()
    {
        // Стрелки плавно двигаются к выбранной карте
        MoveToCard(p1Arrow, p1Selection, 0f);
        MoveToCard(p2Arrow, p2Selection, 0f);
    }

    void MoveToCard(RectTransform element, int selection, float extraOffsetY)
    {
        if (element == null || selection >= characterCards.Length) return;

        Vector2 cardPos = characterCards[selection].anchoredPosition;
        float cardHeight = characterCards[selection].sizeDelta.y;
        Vector2 target = new Vector2(cardPos.x, cardPos.y + cardHeight / 2f + arrowOffsetY + extraOffsetY);

        element.anchoredPosition = Vector2.Lerp(element.anchoredPosition, target, Time.deltaTime * arrowMoveSpeed);
    }

    void UpdateFloating()
    {
        // Плавно уменьшаем амплитуду при подтверждении
        if (p1Confirmed)
            p1FloatAmount = Mathf.Lerp(p1FloatAmount, 0f, Time.deltaTime * stopSmooth);
        if (p2Confirmed)
            p2FloatAmount = Mathf.Lerp(p2FloatAmount, 0f, Time.deltaTime * stopSmooth);

        // П и цифра игрока 1
        FloatElements(p1Letter, p1Number, p1Arrow, p1FloatAmount, 0f);

        // П и цифра игрока 2
        FloatElements(p2Letter, p2Number, p2Arrow, p2FloatAmount, Mathf.PI);
    }

    void FloatElements(RectTransform letter, RectTransform number, RectTransform arrow, float amount, float phaseOffset)
    {
        if (arrow == null) return;

        // Парение вверх-вниз
        float floatY = Mathf.Sin(Time.time * floatSpeed + phaseOffset) * floatAmplitude * amount;

        Vector2 arrowPos = arrow.anchoredPosition;
        Vector2 basePos = new Vector2(arrowPos.x, arrowPos.y + letterOffsetY);

        if (letter != null)
            letter.anchoredPosition = new Vector2(basePos.x - numberOffsetX, basePos.y + floatY);

        if (number != null)
            number.anchoredPosition = new Vector2(basePos.x + numberOffsetX, basePos.y + floatY);
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
                if (isP1 && isP2)
                    target = Color.Lerp(p1HighlightColor, p2HighlightColor, 0.5f);
                else if (isP1)
                    target = p1HighlightColor;
                else if (isP2)
                    target = p2HighlightColor;
                else
                    target = normalColor;

                img.color = Color.Lerp(img.color, target, Time.deltaTime * scaleSpeed);
            }
        }
    }

    void OnBothConfirmed()
    {
        Debug.Log("P1 выбрал: " + p1Selection);
        Debug.Log("P2 выбрал: " + p2Selection);
    }
}