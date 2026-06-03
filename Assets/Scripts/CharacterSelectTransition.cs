using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;

public class CharacterSelectTransition : MonoBehaviour
{
    [Header("Элементы сцены")]
    public RectTransform forestOverlay;
    public RectTransform moon;
    public RectTransform characterPanel;
    public RectTransform[] characterCards;

    [Header("Игрок 1")]
    public RectTransform p1Arrow;
    public RectTransform p1Letter;
    public RectTransform p1Number;

    [Header("Игрок 2")]
    public RectTransform p2Arrow;
    public RectTransform p2Letter;
    public RectTransform p2Number;

    [Header("Подключение игроков")]
    public InputJoinManager joinManager;
    public CharacterSelector characterSelector;

    [Header("Настройки входа")]
    public float forestSlideDuration = 2f;
    public float moonMoveDuration = 2f;
    public float cardsSlideDuration = 1f;
    public float cardsDelay = 0.15f;
    public float arrowSlideDuration = 0.8f;
    public float arrowDelay = 0.3f;

    [Header("Настройки выхода")]
    public float exitCardsDuration = 0.8f;
    public float exitCardsDelay = 0.1f;
    public float exitArrowDuration = 0.5f;
    public float exitMoonDuration = 2f;
    public float exitForestDuration = 2f;
    public float pauseBehindForest = 0.5f;
    public string menuSceneName = "MainMenu";

    [Header("Позиции")]
    public Vector2 forestStartPos = new Vector2(0, 160);
    public Vector2 forestEndPos = new Vector2(-2700, 0);
    public Vector2 moonStartPos = new Vector2(0, 236);
    public Vector2 moonEndPos = new Vector2(-600, 236);

    private CanvasGroup panelCanvasGroup;
    private Vector2[] cardTargets;
    private Vector2 p1ArrowTarget, p1LetterTarget, p1NumberTarget;
    private Vector2 p2ArrowTarget, p2LetterTarget, p2NumberTarget;
    private bool isTransitioning = false;

    void Start()
    {
        forestOverlay.anchoredPosition = forestStartPos;
        moon.anchoredPosition = moonStartPos;

        if (characterPanel != null)
        {
            panelCanvasGroup = characterPanel.GetComponent<CanvasGroup>();
            if (panelCanvasGroup == null)
                panelCanvasGroup = characterPanel.gameObject.AddComponent<CanvasGroup>();
            panelCanvasGroup.alpha = 0f;
        }

        // Прячем карты вниз
        cardTargets = new Vector2[characterCards.Length];
        for (int i = 0; i < characterCards.Length; i++)
        {
            cardTargets[i] = characterCards[i].anchoredPosition;
            characterCards[i].anchoredPosition = new Vector2(cardTargets[i].x, cardTargets[i].y - 1200f);
        }

        // Прячем стрелки/буквы/цифры вверх
        HideUp(p1Arrow, out p1ArrowTarget, 800f);
        HideUp(p1Letter, out p1LetterTarget, 800f);
        HideUp(p1Number, out p1NumberTarget, 800f);
        HideUp(p2Arrow, out p2ArrowTarget, 800f);
        HideUp(p2Letter, out p2LetterTarget, 800f);
        HideUp(p2Number, out p2NumberTarget, 800f);

        // Подписываемся на событие подключения обоих игроков
        if (joinManager != null)
            joinManager.OnBothPlayersJoined += ShowCharacterSelect;

        StartCoroutine(PlayEntryAnimation());
    }

    void HideUp(RectTransform rt, out Vector2 target, float offset)
    {
        if (rt != null)
        {
            target = rt.anchoredPosition;
            rt.anchoredPosition = new Vector2(target.x, target.y + offset);
        }
        else target = Vector2.zero;
    }

    // === ФАЗА 1: Лес + луна (потом ждём подключения) ===
    IEnumerator PlayEntryAnimation()
    {
        float elapsed = 0f;
        float phase1Duration = Mathf.Max(forestSlideDuration, moonMoveDuration);

        while (elapsed < phase1Duration)
        {
            elapsed += Time.deltaTime;

            float forestT = EaseInOutSine(Mathf.Clamp01(elapsed / forestSlideDuration));
            forestOverlay.anchoredPosition = Vector2.Lerp(forestStartPos, forestEndPos, forestT);

            float moonT = EaseInOutSine(Mathf.Clamp01(elapsed / moonMoveDuration));
            moon.anchoredPosition = Vector2.Lerp(moonStartPos, moonEndPos, moonT);

            yield return null;
        }

        // Экран подключения уже видят — ждём OnBothPlayersJoined
    }

    // === ФАЗА 2: Карты + стрелки (после подключения) ===
    void ShowCharacterSelect()
    {
        StartCoroutine(PlayCardsAnimation());
    }

    IEnumerator PlayCardsAnimation()
    {
        if (panelCanvasGroup != null)
            panelCanvasGroup.alpha = 1f;

        float elapsed = 0f;
        float totalCardsTime = cardsSlideDuration + characterCards.Length * cardsDelay;
        float arrowStartTime = totalCardsTime * 0.5f;
        float phase2Duration = Mathf.Max(totalCardsTime, arrowStartTime + arrowSlideDuration + arrowDelay) + 0.3f;

        while (elapsed < phase2Duration)
        {
            elapsed += Time.deltaTime;

            // Карты снизу
            for (int i = 0; i < characterCards.Length; i++)
            {
                float delay = i * cardsDelay;
                float cardElapsed = elapsed - delay;
                if (cardElapsed > 0)
                {
                    float t = EaseOutBack(Mathf.Clamp01(cardElapsed / cardsSlideDuration));
                    Vector2 from = new Vector2(cardTargets[i].x, cardTargets[i].y - 1200f);
                    characterCards[i].anchoredPosition = Vector2.Lerp(from, cardTargets[i], t);
                }
            }

            // P1 сверху
            float p1Elapsed = elapsed - arrowStartTime;
            if (p1Elapsed > 0)
            {
                float t = EaseOutBack(Mathf.Clamp01(p1Elapsed / arrowSlideDuration));
                SlideDown(p1Arrow, p1ArrowTarget, 800f, t);
                SlideDown(p1Letter, p1LetterTarget, 800f, t);
                SlideDown(p1Number, p1NumberTarget, 800f, t);
            }

            // P2 сверху с задержкой
            float p2Elapsed = elapsed - arrowStartTime - arrowDelay;
            if (p2Elapsed > 0)
            {
                float t = EaseOutBack(Mathf.Clamp01(p2Elapsed / arrowSlideDuration));
                SlideDown(p2Arrow, p2ArrowTarget, 800f, t);
                SlideDown(p2Letter, p2LetterTarget, 800f, t);
                SlideDown(p2Number, p2NumberTarget, 800f, t);
            }

            yield return null;
        }

        // Активируем выбор персонажей
        if (characterSelector != null)
            characterSelector.Activate();
    }

    void SlideDown(RectTransform rt, Vector2 target, float offset, float t)
    {
        if (rt == null) return;
        Vector2 from = new Vector2(target.x, target.y + offset);
        rt.anchoredPosition = Vector2.Lerp(from, target, t);
    }

    // === ВЫХОД ===
    public void ExitToMenu()
    {
        if (isTransitioning) return;
        isTransitioning = true;
        StartCoroutine(PlayExitAnimation());
    }

    IEnumerator PlayExitAnimation()
    {
        // Стрелки вверх + карты вниз
        Vector2 p1ArrCur = GetPos(p1Arrow);
        Vector2 p1LetCur = GetPos(p1Letter);
        Vector2 p1NumCur = GetPos(p1Number);
        Vector2 p2ArrCur = GetPos(p2Arrow);
        Vector2 p2LetCur = GetPos(p2Letter);
        Vector2 p2NumCur = GetPos(p2Number);

        Vector2[] cardCurs = new Vector2[characterCards.Length];
        for (int i = 0; i < characterCards.Length; i++)
            cardCurs[i] = characterCards[i].anchoredPosition;

        float elapsed = 0f;
        float phase1Duration = Mathf.Max(exitArrowDuration, exitCardsDuration + characterCards.Length * exitCardsDelay);

        while (elapsed < phase1Duration)
        {
            elapsed += Time.deltaTime;

            float arrowT = EaseInCubic(Mathf.Clamp01(elapsed / exitArrowDuration));
            SlideUp(p1Arrow, p1ArrCur, 800f, arrowT);
            SlideUp(p1Letter, p1LetCur, 800f, arrowT);
            SlideUp(p1Number, p1NumCur, 800f, arrowT);
            SlideUp(p2Arrow, p2ArrCur, 800f, arrowT);
            SlideUp(p2Letter, p2LetCur, 800f, arrowT);
            SlideUp(p2Number, p2NumCur, 800f, arrowT);

            for (int i = 0; i < characterCards.Length; i++)
            {
                float delay = i * exitCardsDelay;
                float cardElapsed = elapsed - delay;
                if (cardElapsed > 0)
                {
                    float t = EaseInCubic(Mathf.Clamp01(cardElapsed / exitCardsDuration));
                    Vector2 target = new Vector2(cardCurs[i].x, cardCurs[i].y - 1200f);
                    characterCards[i].anchoredPosition = Vector2.Lerp(cardCurs[i], target, t);
                }
            }

            yield return null;
        }

        // Луна + лес
        if (panelCanvasGroup != null)
            panelCanvasGroup.alpha = 0f;

        Vector2 moonCur = moon.anchoredPosition;
        Vector2 forestCur = forestOverlay.anchoredPosition;

        elapsed = 0f;
        float phase2Duration = Mathf.Max(exitMoonDuration, exitForestDuration);

        while (elapsed < phase2Duration)
        {
            elapsed += Time.deltaTime;

            float moonT = EaseInOutSine(Mathf.Clamp01(elapsed / exitMoonDuration));
            moon.anchoredPosition = Vector2.Lerp(moonCur, moonStartPos, moonT);

            float forestT = EaseInOutSine(Mathf.Clamp01(elapsed / exitForestDuration));
            forestOverlay.anchoredPosition = Vector2.Lerp(forestCur, forestStartPos, forestT);

            yield return null;
        }

        yield return new WaitForSeconds(pauseBehindForest);
        MenuTransition.SetEntryAnimation();
        SceneManager.LoadScene(menuSceneName);
    }

    void SlideUp(RectTransform rt, Vector2 from, float offset, float t)
    {
        if (rt == null) return;
        rt.anchoredPosition = Vector2.Lerp(from, new Vector2(from.x, from.y + offset), t);
    }

    Vector2 GetPos(RectTransform rt) { return rt != null ? rt.anchoredPosition : Vector2.zero; }

    float EaseInOutSine(float t) { return -(Mathf.Cos(Mathf.PI * t) - 1f) / 2f; }
    float EaseInCubic(float t) { return t * t * t; }
    float EaseOutBack(float t)
    {
        float c = 1.7f;
        return 1f + (c + 1f) * Mathf.Pow(t - 1f, 3f) + c * Mathf.Pow(t - 1f, 2f);
    }
}