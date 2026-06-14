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

    [Header("═══ Аудиоэффекты ═══")]
    [SerializeField] private AudioClip browseSound;    // Звук перелистывания (влево/вправо)
    [SerializeField] private AudioClip confirmSound;   // Звук подтверждения (готов)
    private AudioSource audioSource;
    private Image[] characterCardImages;

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

    public void Activate() 
    { 
        active = true; 
    }

    void Start()
    {
        // Инициализируем аудиокомпонент
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
        }

        CacheCardImages();
    }

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

            // ⬇️ ВОТ ЗДЕСЬ МЫ СОХРАНЯЕМ ВЫБОР ДЛЯ СЦЕНЫ АРЕНЫ ⬇️
            PlayerPrefs.SetInt("P1_Character", p1Selection);
            PlayerPrefs.SetInt("P2_Character", p2Selection);
            PlayerPrefs.Save(); // Принудительно записываем в память
            // ⬆️ ----------------------------------------------- ⬆️

            // Запускаем переход в арену
            if (transition != null)
                transition.GoToArena();
        }
    }

    void HandleP1Input()
    {
        if (p1Confirmed) return;
        HandlePlayerInput(1, InputJoinManager.player1Input, InputJoinManager.player1GamepadIndex, ref p1Selection, ref p1Confirmed);
    }

    void HandleP2Input()
    {
        if (p2Confirmed) return;
        HandlePlayerInput(2, InputJoinManager.player2Input, InputJoinManager.player2GamepadIndex, ref p2Selection, ref p2Confirmed);
    }

    void HandlePlayerInput(int playerNumber, InputJoinManager.InputType type, int gamepadIndex, ref int selection, ref bool confirmed)
    {
        bool left = GetActionDown(playerNumber, type, gamepadIndex, PlayerControlAction.SelectLeft);
        bool right = GetActionDown(playerNumber, type, gamepadIndex, PlayerControlAction.SelectRight);
        bool confirm = GetActionDown(playerNumber, type, gamepadIndex, PlayerControlAction.Confirm);

        if (left)
        {
            int nextSelection = Mathf.Max(0, selection - 1);
            if (nextSelection != selection) { selection = nextSelection; PlaySound(browseSound); }
        }
        if (right)
        {
            int nextSelection = Mathf.Min(characterCards.Length - 1, selection + 1);
            if (nextSelection != selection) { selection = nextSelection; PlaySound(browseSound); }
        }
        if (confirm)
        {
            confirmed = true;
            PlaySound(confirmSound);
        }
    }

    bool GetActionDown(int playerNumber, InputJoinManager.InputType type, int gamepadIndex, PlayerControlAction action)
    {
        switch (type)
        {
            case InputJoinManager.InputType.KeyboardWASD:
            case InputJoinManager.InputType.KeyboardArrows:
                return PlayerInputBindings.GetKeyboardActionDown(playerNumber, action);
            case InputJoinManager.InputType.Gamepad:
                return PlayerInputBindings.GetGamepadActionDown(playerNumber, action, gamepadIndex);
        }

        return false;
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

    void CacheCardImages()
    {
        if (characterCards == null)
        {
            characterCardImages = new Image[0];
            return;
        }

        characterCardImages = new Image[characterCards.Length];
        for (int i = 0; i < characterCards.Length; i++)
        {
            if (characterCards[i] != null)
                characterCardImages[i] = characterCards[i].GetComponent<Image>();
        }
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

            Image img = characterCardImages != null && i < characterCardImages.Length ? characterCardImages[i] : null;
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

    void PlaySound(AudioClip clip)
    {
        if (clip != null && audioSource != null)
        {
            audioSource.PlayOneShot(clip);
        }
    }
}