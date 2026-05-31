using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;

public class MenuTransition : MonoBehaviour
{
    [Header("Элементы меню")]
    public RectTransform logo;
    public RectTransform[] buttons;
    public RectTransform moon;
    public RectTransform forestOverlay;

    [Header("Настройки выхода (Play)")]
    public float logoSlideDuration = 1.2f;
    public float buttonsSlideDuration = 1.2f;
    public float buttonsDelay = 0.5f;
    public float moonMoveDuration = 2f;
    public float forestSlideDuration = 2f;
    public float pauseBehindForest = 0.5f;
    public string nextSceneName = "CharacterSelect";

    [Header("Настройки входа (возврат)")]
    public float entryForestDuration = 2f;
    public float entryMoonDuration = 2f;
    public float entryElementsDuration = 1f;
    public float entryElementsDelay = 0.15f;

    [Header("Конечные позиции (= начало Сцены 2)")]
    public Vector2 moonCenterPos = new Vector2(0, 236);
    public Vector2 forestCoverPos = new Vector2(0, 160);

    private bool isTransitioning = false;
    private static bool playEntryAnimation = false;

    public static void SetEntryAnimation()
    {
        playEntryAnimation = true;
    }

    void Start()
    {
        if (playEntryAnimation)
        {
            playEntryAnimation = false;
            StartCoroutine(PlayEntryAnimation());
        }
    }

    // === ВХОДНАЯ АНИМАЦИЯ — всё одновременно ===
    IEnumerator PlayEntryAnimation()
    {
        Vector2 logoTarget = logo.anchoredPosition;
        Vector2[] btnTargets = new Vector2[buttons.Length];

        logo.anchoredPosition = new Vector2(logoTarget.x - 2000f, logoTarget.y);
        for (int i = 0; i < buttons.Length; i++)
        {
            btnTargets[i] = buttons[i].anchoredPosition;
            buttons[i].anchoredPosition = new Vector2(btnTargets[i].x - 2000f, btnTargets[i].y);
        }

        Vector2 moonOriginal = moon.anchoredPosition;
        moon.anchoredPosition = moonCenterPos;
        forestOverlay.anchoredPosition = forestCoverPos;
        Vector2 forestOffscreen = new Vector2(2500f, forestCoverPos.y);

        float elapsed = 0f;
        float totalDuration = Mathf.Max(
            entryForestDuration,
            entryMoonDuration,
            entryElementsDuration + buttons.Length * entryElementsDelay
        );

        while (elapsed < totalDuration)
        {
            elapsed += Time.deltaTime;

            // Лес уезжает
            float forestT = EaseInOutSine(Mathf.Clamp01(elapsed / entryForestDuration));
            forestOverlay.anchoredPosition = Vector2.Lerp(forestCoverPos, forestOffscreen, forestT);

            // Луна возвращается
            float moonT = EaseInOutSine(Mathf.Clamp01(elapsed / entryMoonDuration));
            moon.anchoredPosition = Vector2.Lerp(moonCenterPos, moonOriginal, moonT);

            // Логотип заезжает
            float logoT = EaseOutCubic(Mathf.Clamp01(elapsed / entryElementsDuration));
            logo.anchoredPosition = Vector2.Lerp(
                new Vector2(logoTarget.x - 2000f, logoTarget.y),
                logoTarget, logoT
            );

            // Кнопки каскадно
            for (int i = 0; i < buttons.Length; i++)
            {
                float delay = entryElementsDelay + i * entryElementsDelay;
                float btnElapsed = elapsed - delay;
                if (btnElapsed > 0)
                {
                    float btnT = EaseOutCubic(Mathf.Clamp01(btnElapsed / entryElementsDuration));
                    buttons[i].anchoredPosition = Vector2.Lerp(
                        new Vector2(btnTargets[i].x - 2000f, btnTargets[i].y),
                        btnTargets[i], btnT
                    );
                }
            }

            yield return null;
        }
    }

    // === ВЫХОДНАЯ АНИМАЦИЯ (Play) ===
    public void StartTransition()
    {
        if (isTransitioning) return;
        isTransitioning = true;
        StartCoroutine(PlayExitTransition());
    }

    IEnumerator PlayExitTransition()
    {
        Vector2 logoStart = logo.anchoredPosition;
        Vector2 moonStart = moon.anchoredPosition;
        Vector2 forestStart = forestOverlay.anchoredPosition;

        Vector2[] btnStarts = new Vector2[buttons.Length];
        for (int i = 0; i < buttons.Length; i++)
            btnStarts[i] = buttons[i].anchoredPosition;

        float elapsed = 0f;
        float totalDuration = Mathf.Max(
            logoSlideDuration,
            buttonsDelay + buttonsSlideDuration,
            moonMoveDuration,
            forestSlideDuration
        );

        while (elapsed < totalDuration)
        {
            elapsed += Time.deltaTime;

            float logoT = EaseInCubic(Mathf.Clamp01(elapsed / logoSlideDuration));
            logo.anchoredPosition = new Vector2(
                Mathf.Lerp(logoStart.x, logoStart.x - 2000f, logoT),
                logoStart.y
            );

            for (int i = 0; i < buttons.Length; i++)
            {
                float btnDelay = buttonsDelay + i * 0.12f;
                float btnElapsed = elapsed - btnDelay;
                if (btnElapsed > 0)
                {
                    float btnT = EaseInCubic(Mathf.Clamp01(btnElapsed / buttonsSlideDuration));
                    buttons[i].anchoredPosition = new Vector2(
                        Mathf.Lerp(btnStarts[i].x, btnStarts[i].x - 2000f, btnT),
                        btnStarts[i].y
                    );
                }
            }

            float moonT = EaseInOutSine(Mathf.Clamp01(elapsed / moonMoveDuration));
            moon.anchoredPosition = Vector2.Lerp(moonStart, moonCenterPos, moonT);

            float forestT = EaseInOutSine(Mathf.Clamp01(elapsed / forestSlideDuration));
            forestOverlay.anchoredPosition = Vector2.Lerp(forestStart, forestCoverPos, forestT);

            yield return null;
        }

        yield return new WaitForSeconds(pauseBehindForest);
        SceneManager.LoadScene(nextSceneName);
    }

    float EaseInCubic(float t) { return t * t * t; }
    float EaseInOutSine(float t) { return -(Mathf.Cos(Mathf.PI * t) - 1f) / 2f; }
    float EaseOutCubic(float t) { return 1f - Mathf.Pow(1f - t, 3f); }
}