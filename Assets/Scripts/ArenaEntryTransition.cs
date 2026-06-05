using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class ArenaEntryTransition : MonoBehaviour
{
    [Header("Оверлеи")]
    public RectTransform forestOverlay;    // Лес (закрывает экран при старте)
    public RectTransform bushOverlay;      // Кусты (внизу леса)

    [Header("Настройки")]
    public float forestSlideDuration = 2f;
    public float bushSpeedMultiplier = 1.3f;

    void Start()
    {
        // Лес и кусты закрывают экран
        forestOverlay.anchoredPosition = new Vector2(0, 0);
        if (bushOverlay != null)
            bushOverlay.anchoredPosition = new Vector2(0, -100f);

        StartCoroutine(PlayEntryAnimation());
    }

    IEnumerator PlayEntryAnimation()
    {
        yield return new WaitForSeconds(0.3f);

        Vector2 forestStart = forestOverlay.anchoredPosition;
        Vector2 forestEnd = new Vector2(0, 1200f);              // Лес уходит вверх

        Vector2 bushStart = bushOverlay != null ? bushOverlay.anchoredPosition : Vector2.zero;
        Vector2 bushEnd = new Vector2(0, 1400f);                // Кусты уходят вверх
        float bushDuration = forestSlideDuration / bushSpeedMultiplier;

        float elapsed = 0f;

        while (elapsed < forestSlideDuration)
        {
            elapsed += Time.deltaTime;

            // Лес вверх
            float forestT = EaseInOutSine(Mathf.Clamp01(elapsed / forestSlideDuration));
            forestOverlay.anchoredPosition = Vector2.Lerp(forestStart, forestEnd, forestT);

            // Кусты вверх быстрее
            if (bushOverlay != null)
            {
                float bushT = EaseInOutSine(Mathf.Clamp01(elapsed / bushDuration));
                bushOverlay.anchoredPosition = Vector2.Lerp(bushStart, bushEnd, bushT);
            }

            yield return null;
        }

        // Скрываем оверлеи
        forestOverlay.gameObject.SetActive(false);
        if (bushOverlay != null)
            bushOverlay.gameObject.SetActive(false);
    }

    float EaseInOutSine(float t) { return -(Mathf.Cos(Mathf.PI * t) - 1f) / 2f; }
}
