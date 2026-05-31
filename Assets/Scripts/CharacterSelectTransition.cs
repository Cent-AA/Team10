using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;

public class CharacterSelectTransition : MonoBehaviour
{
    [Header("Элементы сцены")]
    public RectTransform forestOverlay;
    public RectTransform moon;
    public RectTransform characterPanel;

    [Header("Настройки входа")]
    public float forestSlideDuration = 2f;
    public float moonMoveDuration = 2f;
    public float panelFadeDuration = 1f;

    [Header("Настройки выхода")]
    public float exitPanelFadeDuration = 0.5f;
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

        StartCoroutine(PlayEntryAnimation());
    }

    // === ВХОД: разгон → замедление → остановка ===
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

        if (panelCanvasGroup != null)
        {
            elapsed = 0f;
            while (elapsed < panelFadeDuration)
            {
                elapsed += Time.deltaTime;
                panelCanvasGroup.alpha = Mathf.Clamp01(elapsed / panelFadeDuration);
                yield return null;
            }
            panelCanvasGroup.alpha = 1f;
        }
    }

    // === ВЫХОД: разгон → замедление → остановка → пауза → переход ===
    public void ExitToMenu()
    {
        if (isTransitioning) return;
        isTransitioning = true;
        StartCoroutine(PlayExitAnimation());
    }

    IEnumerator PlayExitAnimation()
    {
        if (panelCanvasGroup != null)
        {
            float elapsed = 0f;
            float startAlpha = panelCanvasGroup.alpha;
            while (elapsed < exitPanelFadeDuration)
            {
                elapsed += Time.deltaTime;
                panelCanvasGroup.alpha = Mathf.Lerp(startAlpha, 0f, elapsed / exitPanelFadeDuration);
                yield return null;
            }
            panelCanvasGroup.alpha = 0f;
        }

        Vector2 moonCurrent = moon.anchoredPosition;
        Vector2 forestCurrent = forestOverlay.anchoredPosition;

        float elapsed2 = 0f;
        float phase2Duration = Mathf.Max(exitMoonDuration, exitForestDuration);

        while (elapsed2 < phase2Duration)
        {
            elapsed2 += Time.deltaTime;

            float moonT = EaseInOutSine(Mathf.Clamp01(elapsed2 / exitMoonDuration));
            moon.anchoredPosition = Vector2.Lerp(moonCurrent, moonStartPos, moonT);

            float forestT = EaseInOutSine(Mathf.Clamp01(elapsed2 / exitForestDuration));
            forestOverlay.anchoredPosition = Vector2.Lerp(forestCurrent, forestStartPos, forestT);

            yield return null;
        }

        yield return new WaitForSeconds(pauseBehindForest);
        MenuTransition.SetEntryAnimation();
        SceneManager.LoadScene(menuSceneName);
    }

    float EaseInOutSine(float t) { return -(Mathf.Cos(Mathf.PI * t) - 1f) / 2f; }
}