using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using System.Collections;

public class MenuTransition : MonoBehaviour
{
    [Header("Элементы меню")]
    public RectTransform logo;
    public RectTransform[] buttons;
    public RectTransform moon;
    public RectTransform forestOverlay;
    public RectTransform bushOverlay;         // Кусты (поверх леса)

    [Header("Настройки выхода (Play)")]
    public float logoSlideDuration = 1.2f;
    public float buttonsSlideDuration = 1.2f;
    public float buttonsDelay = 0.5f;
    public float moonMoveDuration = 2f;
    public float forestSlideDuration = 2f;
    public float bushSpeedMultiplier = 1.3f;  // Кусты быстрее леса
    public float pauseBehindForest = 0.5f;
    public string nextSceneName = "CharacterSelect";
    public string playArenaSceneName = "TestArena_PrototypeMVP";
    public string trainingSceneName = "TrainingScene";

    [Header("Настройки входа (возврат)")]
    public float entryForestDuration = 2f;
    public float entryMoonDuration = 2f;
    public float entryElementsDuration = 1f;
    public float entryElementsDelay = 0.15f;

    [Header("Конечные позиции (= начало Сцены 2)")]
    public Vector2 moonCenterPos = new Vector2(0, 236);
    public Vector2 forestCoverPos = new Vector2(0, 160);
    public Vector2 bushCoverPos = new Vector2(0, 160);

    private bool isTransitioning = false;
    private static bool playEntryAnimation = false;
    private static bool playPauseEntry = false;

    private enum EntryAnimationMode
    {
        None,
        Standard,
        FromPause
    }

    private EntryAnimationMode entryAnimationMode;
    private CanvasGroup menuCanvasGroup;
    private LayoutGroup buttonsLayoutGroup;
    private RectTransform buttonsLayoutRoot;
    private bool buttonsLayoutSuspended;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStaticState()
    {
        playEntryAnimation = false;
        playPauseEntry = false;
    }

    public static void SetEntryAnimation()
    {
        playEntryAnimation = true;
        playPauseEntry = false;
    }

    public static void SetPauseEntry()
    {
        playPauseEntry = true;
        playEntryAnimation = false;
    }

    [Header("Вход из паузы")]
    public UnityEngine.UI.Image blackScreen;   // Чёрный Image на весь экран
    public float blackFadeOutDuration = 1.5f;
    public float pauseBushDuration = 1.5f;

    void Awake()
    {
        if (playPauseEntry)
            entryAnimationMode = EntryAnimationMode.FromPause;
        else if (playEntryAnimation)
            entryAnimationMode = EntryAnimationMode.Standard;
        else
            entryAnimationMode = EntryAnimationMode.None;

        playEntryAnimation = false;
        playPauseEntry = false;

        Transform menuRoot = logo != null && logo.parent != null ? logo.parent : transform;
        menuCanvasGroup = menuRoot.GetComponent<CanvasGroup>();
        if (menuCanvasGroup == null)
            menuCanvasGroup = menuRoot.gameObject.AddComponent<CanvasGroup>();

        if (entryAnimationMode != EntryAnimationMode.None)
            SetMenuPresentation(0f, false);
    }

    void Start()
    {
        if (blackScreen != null) blackScreen.gameObject.SetActive(false);

        if (entryAnimationMode == EntryAnimationMode.FromPause)
        {
            StartCoroutine(PlayPauseEntryAnimation());
        }
        else if (entryAnimationMode == EntryAnimationMode.Standard)
        {
            StartCoroutine(PlayEntryAnimation());
        }
        else
        {
            SetMenuPresentation(1f, true);
        }
    }

    // === ВХОД ИЗ ПАУЗЫ (чёрный экран → осветление → кусты вниз) ===
    IEnumerator PlayPauseEntryAnimation()
    {
        // Скрываем элементы
        Vector2 logoTarget = logo.anchoredPosition;
        Vector2[] btnTargets = CaptureButtonTargetsAndSuspendLayout();
        Vector2 bushTarget = bushOverlay != null ? bushOverlay.anchoredPosition : Vector2.zero;
        Vector3 bushTargetScale = bushOverlay != null ? bushOverlay.localScale : Vector3.one;
        logo.anchoredPosition = new Vector2(logoTarget.x - 2000f, logoTarget.y);
        for (int i = 0; i < buttons.Length; i++)
        {
            if (buttons[i] == null) continue;
            buttons[i].anchoredPosition = new Vector2(btnTargets[i].x - 2000f, btnTargets[i].y);
        }

        // Чёрный экран + кусты закрывают
        if (blackScreen != null)
        {
            blackScreen.gameObject.SetActive(true);
            blackScreen.color = Color.black;
        }
        if (bushOverlay != null)
        {
            bushOverlay.anchoredPosition = new Vector2(0, 0);
            bushOverlay.localScale = Vector3.one * 3f;
        }

        SetMenuPresentation(1f, false);
        yield return new WaitForSecondsRealtime(0.3f);

        // Фаза 1: Чёрный экран осветляется
        float elapsed = 0f;
        while (blackScreen != null && elapsed < blackFadeOutDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = blackFadeOutDuration <= 0f ? 1f : Mathf.Clamp01(elapsed / blackFadeOutDuration);
            blackScreen.color = new Color(0, 0, 0, 1f - t);
            yield return null;
        }
        if (blackScreen != null) blackScreen.gameObject.SetActive(false);

        // Фаза 2: Кусты уходят вниз
        if (bushOverlay != null)
        {
            Vector2 bushStart = bushOverlay.anchoredPosition;
            Vector2 bushEnd = new Vector2(0, -1200f);
            elapsed = 0f;
            while (elapsed < pauseBushDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = pauseBushDuration <= 0f ? 1f : EaseInOutSine(Mathf.Clamp01(elapsed / pauseBushDuration));
                bushOverlay.anchoredPosition = Vector2.Lerp(bushStart, bushEnd, t);
                bushOverlay.localScale = Vector3.one * Mathf.Lerp(3f, 1f, t);
                yield return null;
            }

            // Телепортируем куст на оригинальную позицию (справа за экраном)
            bushOverlay.gameObject.SetActive(false);
            bushOverlay.anchoredPosition = bushTarget;
            bushOverlay.localScale = bushTargetScale;
            bushOverlay.gameObject.SetActive(true);
        }

        // Фаза 3: Элементы заезжают
        elapsed = 0f;
        float totalDuration = entryElementsDuration + buttons.Length * entryElementsDelay + 0.5f;
        while (elapsed < totalDuration)
        {
            elapsed += Time.unscaledDeltaTime;

            float logoT = EaseOutCubic(Mathf.Clamp01(elapsed / entryElementsDuration));
            logo.anchoredPosition = Vector2.Lerp(
                new Vector2(logoTarget.x - 2000f, logoTarget.y), logoTarget, logoT);

            for (int i = 0; i < buttons.Length; i++)
            {
                float delay = entryElementsDelay + i * entryElementsDelay;
                float btnElapsed = elapsed - delay;
                if (btnElapsed > 0)
                {
                    float btnT = EaseOutCubic(Mathf.Clamp01(btnElapsed / entryElementsDuration));
                    buttons[i].anchoredPosition = Vector2.Lerp(
                        new Vector2(btnTargets[i].x - 2000f, btnTargets[i].y), btnTargets[i], btnT);
                }
            }
            yield return null;
        }

        logo.anchoredPosition = logoTarget;
        RestoreButtonLayout(btnTargets);
        SetMenuPresentation(1f, true);
    }

    // === ВХОДНАЯ АНИМАЦИЯ ===
    IEnumerator PlayEntryAnimation()
    {
        Vector2 logoTarget = logo.anchoredPosition;
        Vector2[] btnTargets = CaptureButtonTargetsAndSuspendLayout();
        Vector2 forestRestingPosition = forestOverlay.anchoredPosition;
        Vector2 bushRestingPosition = bushOverlay != null ? bushOverlay.anchoredPosition : Vector2.zero;

        logo.anchoredPosition = new Vector2(logoTarget.x - 2000f, logoTarget.y);
        for (int i = 0; i < buttons.Length; i++)
        {
            if (buttons[i] == null) continue;
            buttons[i].anchoredPosition = new Vector2(btnTargets[i].x - 2000f, btnTargets[i].y);
        }

        Vector2 moonOriginal = moon.anchoredPosition;
        moon.anchoredPosition = moonCenterPos;
        forestOverlay.anchoredPosition = forestCoverPos;
        if (bushOverlay != null) bushOverlay.anchoredPosition = bushCoverPos;

        Vector2 forestOffscreen = forestRestingPosition;
        Vector2 bushOffscreen = bushRestingPosition;
        float bushDuration = entryForestDuration / bushSpeedMultiplier;

        SetMenuPresentation(1f, false);

        float elapsed = 0f;
        float totalDuration = Mathf.Max(entryForestDuration, entryMoonDuration,
            entryElementsDuration + buttons.Length * entryElementsDelay);

        while (elapsed < totalDuration)
        {
            elapsed += Time.unscaledDeltaTime;

            // Кусты уезжают быстрее
            if (bushOverlay != null)
            {
                float bushT = EaseInOutSine(Mathf.Clamp01(elapsed / bushDuration));
                bushOverlay.anchoredPosition = Vector2.Lerp(bushCoverPos, bushOffscreen, bushT);
            }

            // Лес уезжает
            float forestT = EaseInOutSine(Mathf.Clamp01(elapsed / entryForestDuration));
            forestOverlay.anchoredPosition = Vector2.Lerp(forestCoverPos, forestOffscreen, forestT);

            // Луна
            float moonT = EaseInOutSine(Mathf.Clamp01(elapsed / entryMoonDuration));
            moon.anchoredPosition = Vector2.Lerp(moonCenterPos, moonOriginal, moonT);

            // Логотип
            float logoT = EaseOutCubic(Mathf.Clamp01(elapsed / entryElementsDuration));
            logo.anchoredPosition = Vector2.Lerp(
                new Vector2(logoTarget.x - 2000f, logoTarget.y), logoTarget, logoT);

            // Кнопки каскадно
            for (int i = 0; i < buttons.Length; i++)
            {
                float delay = entryElementsDelay + i * entryElementsDelay;
                float btnElapsed = elapsed - delay;
                if (btnElapsed > 0)
                {
                    float btnT = EaseOutCubic(Mathf.Clamp01(btnElapsed / entryElementsDuration));
                    buttons[i].anchoredPosition = Vector2.Lerp(
                        new Vector2(btnTargets[i].x - 2000f, btnTargets[i].y), btnTargets[i], btnT);
                }
            }

            yield return null;
        }

        logo.anchoredPosition = logoTarget;
        moon.anchoredPosition = moonOriginal;
        forestOverlay.anchoredPosition = forestOffscreen;
        if (bushOverlay != null) bushOverlay.anchoredPosition = bushOffscreen;
        RestoreButtonLayout(btnTargets);
        SetMenuPresentation(1f, true);
    }

    // === ВЫХОДНАЯ АНИМАЦИЯ (Play) ===
    public void StartTransition()
    {
        StartTransitionToCharacterSelect(playArenaSceneName);
    }

    public void StartTrainingTransition()
    {
        StartTransitionToCharacterSelect(trainingSceneName);
    }

    private void StartTransitionToCharacterSelect(string targetArenaSceneName)
    {
        if (isTransitioning) return;

        CharacterSelectTransition.SetNextArenaScene(targetArenaSceneName);
        isTransitioning = true;
        StartCoroutine(PlayExitTransition());
    }

    IEnumerator PlayExitTransition()
    {
        Vector2 logoStart = logo.anchoredPosition;
        Vector2 moonStart = moon.anchoredPosition;
        Vector2 forestStart = forestOverlay.anchoredPosition;
        Vector2 bushStart = bushOverlay != null ? bushOverlay.anchoredPosition : Vector2.zero;
        float bushDuration = forestSlideDuration / bushSpeedMultiplier;

        Vector2[] btnStarts = CaptureButtonTargetsAndSuspendLayout();

        float elapsed = 0f;
        float totalDuration = Mathf.Max(
            logoSlideDuration, buttonsDelay + buttonsSlideDuration,
            moonMoveDuration, forestSlideDuration);

        while (elapsed < totalDuration)
        {
            elapsed += Time.deltaTime;

            // Логотип
            float logoT = EaseInCubic(Mathf.Clamp01(elapsed / logoSlideDuration));
            logo.anchoredPosition = new Vector2(
                Mathf.Lerp(logoStart.x, logoStart.x - 2000f, logoT), logoStart.y);

            // Кнопки
            for (int i = 0; i < buttons.Length; i++)
            {
                float btnDelay = buttonsDelay + i * 0.12f;
                float btnElapsed = elapsed - btnDelay;
                if (btnElapsed > 0)
                {
                    float btnT = EaseInCubic(Mathf.Clamp01(btnElapsed / buttonsSlideDuration));
                    buttons[i].anchoredPosition = new Vector2(
                        Mathf.Lerp(btnStarts[i].x, btnStarts[i].x - 2000f, btnT), btnStarts[i].y);
                }
            }

            // Луна + лес
            float moonT = EaseInOutSine(Mathf.Clamp01(elapsed / moonMoveDuration));
            moon.anchoredPosition = Vector2.Lerp(moonStart, moonCenterPos, moonT);

            float forestT = EaseInOutSine(Mathf.Clamp01(elapsed / forestSlideDuration));
            forestOverlay.anchoredPosition = Vector2.Lerp(forestStart, forestCoverPos, forestT);

            // Кусты быстрее леса
            if (bushOverlay != null)
            {
                float bushT = EaseInOutSine(Mathf.Clamp01(elapsed / bushDuration));
                bushOverlay.anchoredPosition = Vector2.Lerp(bushStart, bushCoverPos, bushT);
            }

            yield return null;
        }

        yield return new WaitForSeconds(pauseBehindForest);
        SceneManager.LoadScene(nextSceneName);
    }

    private Vector2[] CaptureButtonTargetsAndSuspendLayout()
    {
        Canvas.ForceUpdateCanvases();

        buttonsLayoutRoot = null;
        buttonsLayoutGroup = null;

        for (int i = 0; i < buttons.Length; i++)
        {
            if (buttons[i] != null && buttons[i].parent is RectTransform parent)
            {
                buttonsLayoutRoot = parent;
                break;
            }
        }

        if (buttonsLayoutRoot != null)
        {
            LayoutRebuilder.ForceRebuildLayoutImmediate(buttonsLayoutRoot);
            buttonsLayoutGroup = buttonsLayoutRoot.GetComponent<LayoutGroup>();
        }

        Vector2[] targets = new Vector2[buttons.Length];
        for (int i = 0; i < buttons.Length; i++)
        {
            if (buttons[i] != null)
                targets[i] = buttons[i].anchoredPosition;
        }

        if (buttonsLayoutGroup != null && buttonsLayoutGroup.enabled)
        {
            buttonsLayoutGroup.enabled = false;
            buttonsLayoutSuspended = true;
        }

        return targets;
    }

    private void RestoreButtonLayout(Vector2[] targets)
    {
        if (targets != null)
        {
            int count = Mathf.Min(buttons.Length, targets.Length);
            for (int i = 0; i < count; i++)
            {
                if (buttons[i] != null)
                    buttons[i].anchoredPosition = targets[i];
            }
        }

        if (buttonsLayoutSuspended && buttonsLayoutGroup != null)
        {
            buttonsLayoutGroup.enabled = true;
            buttonsLayoutSuspended = false;
        }

        if (buttonsLayoutRoot != null)
            LayoutRebuilder.ForceRebuildLayoutImmediate(buttonsLayoutRoot);

        Canvas.ForceUpdateCanvases();
    }

    private void SetMenuPresentation(float alpha, bool interactive)
    {
        if (menuCanvasGroup == null) return;

        menuCanvasGroup.alpha = alpha;
        menuCanvasGroup.interactable = interactive;
        menuCanvasGroup.blocksRaycasts = interactive;
    }

    void OnDisable()
    {
        if (buttonsLayoutSuspended && buttonsLayoutGroup != null)
        {
            buttonsLayoutGroup.enabled = true;
            buttonsLayoutSuspended = false;
        }
    }

    float EaseInCubic(float t) { return t * t * t; }
    float EaseInOutSine(float t) { return -(Mathf.Cos(Mathf.PI * t) - 1f) / 2f; }
    float EaseOutCubic(float t) { return 1f - Mathf.Pow(1f - t, 3f); }
}
