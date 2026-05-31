using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;

public class MenuTransition : MonoBehaviour
{
    [Header("Элементы меню")]
    public RectTransform logo;
    public RectTransform[] buttons;       // Play, Options, Credits, Exit
    public RectTransform moon;
    public RectTransform forestOverlay;

    [Header("Настройки анимации")]
    public float logoSlideDuration = 1.2f;
    public float buttonsSlideDuration = 1.2f;
    public float buttonsDelay = 0.5f;         // Задержка кнопок после логотипа
    public float moonMoveDuration = 2f;
    public float forestSlideDuration = 2f;
    public string nextSceneName = "CharacterSelect";

    private bool isTransitioning = false;
    private AsyncOperation sceneLoad;

    public void StartTransition()
    {
        if (isTransitioning) return;
        isTransitioning = true;

        // Начинаем загрузку сцены в фоне сразу
        StartCoroutine(PreloadScene());
        StartCoroutine(PlayTransition());
    }

    IEnumerator PreloadScene()
    {
        sceneLoad = SceneManager.LoadSceneAsync(nextSceneName);
        sceneLoad.allowSceneActivation = false; // Не переключаемся пока не готовы
        yield return null;
    }

    IEnumerator PlayTransition()
    {
        Vector2 logoStart = logo.anchoredPosition;
        Vector2 moonStart = moon.anchoredPosition;
        Vector2 moonEnd = new Vector2(0, moonStart.y);
        Vector2 forestStart = forestOverlay.anchoredPosition;
        Vector2 forestEnd = new Vector2(0, 0);

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

            // === Логотип уезжает влево (сразу) ===
            float logoT = EaseInCubic(Mathf.Clamp01(elapsed / logoSlideDuration));
            logo.anchoredPosition = new Vector2(
                Mathf.Lerp(logoStart.x, logoStart.x - 2000f, logoT),
                logoStart.y
            );

            // === Кнопки уезжают влево (через 0.5с после логотипа, каскадно) ===
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

            // === Луна к центру + лес справа (одновременно) ===
            float moonT = EaseInOutSine(Mathf.Clamp01(elapsed / moonMoveDuration));
            moon.anchoredPosition = Vector2.Lerp(moonStart, moonEnd, moonT);

            float forestT = EaseInOutCubic(Mathf.Clamp01(elapsed / forestSlideDuration));
            forestOverlay.anchoredPosition = Vector2.Lerp(forestStart, forestEnd, forestT);

            yield return null;
        }

        // Переключаемся на загруженную сцену без задержки
        if (sceneLoad != null)
            sceneLoad.allowSceneActivation = true;
    }

    float EaseInCubic(float t) { return t * t * t; }
    float EaseInOutCubic(float t) { return t < 0.5f ? 4f * t * t * t : 1f - Mathf.Pow(-2f * t + 2f, 3f) / 2f; }
    float EaseInOutSine(float t) { return -(Mathf.Cos(Mathf.PI * t) - 1f) / 2f; }
}