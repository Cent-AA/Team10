using UnityEngine;
using System.Collections;

public class CharacterSelectTransition : MonoBehaviour
{
    [Header("Элементы сцены")]
    public RectTransform forestOverlay;   // Лес (закрывает экран при старте)
    public RectTransform moon;            // Луна (в центре при старте)
    public RectTransform characterPanel;  // Панель выбора персонажей

    [Header("Настройки")]
    public float forestSlideDuration = 2f;
    public float moonMoveDuration = 2f;    // Одновременно с лесом
    public float panelFadeDuration = 1f;

    private CanvasGroup panelCanvasGroup;

    void Start()
    {
        if (characterPanel != null)
        {
            panelCanvasGroup = characterPanel.GetComponent<CanvasGroup>();
            if (panelCanvasGroup == null)
                panelCanvasGroup = characterPanel.gameObject.AddComponent<CanvasGroup>();
            panelCanvasGroup.alpha = 0f;
        }

        StartCoroutine(PlayEntryAnimation());
    }

    IEnumerator PlayEntryAnimation()
    {
        Vector2 forestStart = new Vector2(0, 160);
        Vector2 forestEnd = new Vector2(-2700f, 0);

        Vector2 moonStart = moon.anchoredPosition;          // Центр
        Vector2 moonEnd = new Vector2(-600f, moonStart.y);  // Влево

        float elapsed = 0f;
        float phase1Duration = Mathf.Max(forestSlideDuration, moonMoveDuration);

        // === Лес уезжает влево + луна влево (одновременно) ===
        while (elapsed < phase1Duration)
        {
            elapsed += Time.deltaTime;

            float forestT = EaseInOutCubic(Mathf.Clamp01(elapsed / forestSlideDuration));
            forestOverlay.anchoredPosition = Vector2.Lerp(forestStart, forestEnd, forestT);

            float moonT = EaseInOutSine(Mathf.Clamp01(elapsed / moonMoveDuration));
            moon.anchoredPosition = Vector2.Lerp(moonStart, moonEnd, moonT);

            yield return null;
        }

        // === Панель персонажей появляется ===
        elapsed = 0f;
        while (elapsed < panelFadeDuration)
        {
            elapsed += Time.deltaTime;
            panelCanvasGroup.alpha = Mathf.Clamp01(elapsed / panelFadeDuration);
            yield return null;
        }
        panelCanvasGroup.alpha = 1f;
    }

    float EaseInOutCubic(float t) { return t < 0.5f ? 4f * t * t * t : 1f - Mathf.Pow(-2f * t + 2f, 3f) / 2f; }
    float EaseInOutSine(float t) { return -(Mathf.Cos(Mathf.PI * t) - 1f) / 2f; }
}