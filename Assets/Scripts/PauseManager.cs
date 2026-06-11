using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;
using System.Collections;
using System.Collections.Generic;

public class PauseManager : MonoBehaviour
{
    [Header("═══ UI ═══")]
    public GameObject pausePanel;              // Панель паузы
    public Image darkOverlay;                  // Затемнение экрана
    public Color overlayColor = new Color(0, 0, 0, 0.6f);

    [Header("═══ Кнопки на верёвках ═══")]
    public RopeButton[] ropeButtons;

    [Header("═══ Анимация ═══")]
    public float dropDelay = 0.15f;            // Задержка между кнопками
    public float dropDuration = 0.8f;          // Время падения
    public float swingDamping = 3f;            // Затухание качания
    public float swingFrequency = 4f;          // Частота качания
    public float initialSwingAngle = 25f;      // Начальный угол качания
    public float fadeInSpeed = 5f;             // Скорость затемнения

    [Header("═══ Верёвка ═══")]
    public float ropeWidth = 3f;
    public Color ropeColor = new Color(0.6f, 0.4f, 0.2f, 1f);
    public int ropeSegments = 10;

    [Header("═══ Выход ═══")]
    public float retractDuration = 0.5f;       // Время сворачивания
    public string menuSceneName = "MainMenu";

    private bool isPaused = false;
    private bool isAnimating = false;
    private List<LineRenderer> ropeLines = new List<LineRenderer>();
    private List<RopeButtonState> buttonStates = new List<RopeButtonState>();

    [System.Serializable]
    public class RopeButton
    {
        public RectTransform button;
        public float ropeLength = 300f;        // Длина верёвки в пикселях
        public float extraDelay = 0f;          // Доп. задержка
    }

    private class RopeButtonState
    {
        public Vector2 hiddenPos;              // За экраном сверху
        public Vector2 hangPos;                // Конечная позиция (висит)
        public float dropTimer;
        public float swingAngle;
        public float swingVelocity;
        public bool dropped;
        public RectTransform anchor;           // Точка крепления верёвки (верх)
    }

    void Start()
    {
        if (pausePanel != null)
            pausePanel.SetActive(false);
        if (darkOverlay != null)
        {
            darkOverlay.gameObject.SetActive(false);
            darkOverlay.color = new Color(overlayColor.r, overlayColor.g, overlayColor.b, 0);
        }

        SetupButtons();
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape) && !isAnimating)
        {
            if (isPaused) ResumeGame();
            else PauseGame();
        }

        if (isPaused)
            UpdateSwinging();
    }

    void SetupButtons()
    {
        for (int i = 0; i < ropeButtons.Length; i++)
        {
            RopeButton rb = ropeButtons[i];
            if (rb.button == null) continue;

            RopeButtonState state = new RopeButtonState();
            state.hangPos = rb.button.anchoredPosition;
            state.hiddenPos = new Vector2(state.hangPos.x, state.hangPos.y + rb.ropeLength + 500f);
            state.swingAngle = 0f;
            state.swingVelocity = 0f;
            state.dropped = false;

            // Скрываем кнопку за экран
            rb.button.anchoredPosition = state.hiddenPos;

            // Создаём LineRenderer для верёвки
            GameObject ropeObj = new GameObject("Rope_" + i);
            ropeObj.transform.SetParent(pausePanel.transform);
            ropeObj.transform.localScale = Vector3.one;

            // Для UI используем Image-based rope вместо LineRenderer
            // Создаём anchor точку сверху
            GameObject anchorObj = new GameObject("Anchor_" + i);
            anchorObj.transform.SetParent(pausePanel.transform);
            RectTransform anchorRT = anchorObj.AddComponent<RectTransform>();
            anchorRT.anchoredPosition = new Vector2(state.hangPos.x, state.hangPos.y + rb.ropeLength);
            state.anchor = anchorRT;

            buttonStates.Add(state);
        }
    }

    // ═══════════ ПАУЗА ═══════════
    public void PauseGame()
    {
        isPaused = true;
        isAnimating = true;
        pausePanel.SetActive(true);
        darkOverlay.gameObject.SetActive(true);

        StartCoroutine(PauseAnimation());
    }

    IEnumerator PauseAnimation()
    {
        // Затемнение
        float fadeTimer = 0f;

        // Кнопки падают каскадно
        for (int i = 0; i < ropeButtons.Length; i++)
        {
            if (i < buttonStates.Count)
            {
                buttonStates[i].dropTimer = 0f;
                buttonStates[i].dropped = false;
                buttonStates[i].swingAngle = 0f;
                buttonStates[i].swingVelocity = 0f;
                ropeButtons[i].button.anchoredPosition = buttonStates[i].hiddenPos;
            }
        }

        float totalAnimTime = dropDuration + ropeButtons.Length * dropDelay + 1f;
        float elapsed = 0f;

        while (elapsed < totalAnimTime)
        {
            elapsed += Time.unscaledDeltaTime;

            // Затемнение плавное
            if (darkOverlay != null)
            {
                float alpha = Mathf.Lerp(0, overlayColor.a, Mathf.Clamp01(elapsed * fadeInSpeed));
                darkOverlay.color = new Color(overlayColor.r, overlayColor.g, overlayColor.b, alpha);
            }

            // Кнопки падают
            for (int i = 0; i < ropeButtons.Length; i++)
            {
                if (i >= buttonStates.Count) continue;
                float delay = dropDelay * i + ropeButtons[i].extraDelay;

                if (elapsed > delay && !buttonStates[i].dropped)
                {
                    buttonStates[i].dropTimer += Time.unscaledDeltaTime;
                    float t = Mathf.Clamp01(buttonStates[i].dropTimer / dropDuration);

                    // Easing — bounce при падении
                    float eased = BounceEaseOut(t);

                    // Позиция: от скрытой к конечной
                    ropeButtons[i].button.anchoredPosition = Vector2.Lerp(
                        buttonStates[i].hiddenPos,
                        buttonStates[i].hangPos,
                        eased);

                    if (t >= 1f)
                    {
                        buttonStates[i].dropped = true;
                        buttonStates[i].swingAngle = initialSwingAngle * (i % 2 == 0 ? 1f : -1f);
                        buttonStates[i].swingVelocity = 0f;
                    }
                }
            }

            yield return null;
        }

        Time.timeScale = 0f;
        isAnimating = false;
    }

    // ═══════════ КАЧАНИЕ ═══════════
    void UpdateSwinging()
    {
        for (int i = 0; i < ropeButtons.Length; i++)
        {
            if (i >= buttonStates.Count || !buttonStates[i].dropped) continue;

            RopeButtonState state = buttonStates[i];

            // Физика маятника
            float gravity = 9.8f;
            float length = ropeButtons[i].ropeLength / 100f;  // В метры
            float acceleration = -(gravity / length) * Mathf.Sin(state.swingAngle * Mathf.Deg2Rad);

            state.swingVelocity += acceleration * Time.unscaledDeltaTime * swingFrequency;
            state.swingVelocity *= (1f - swingDamping * Time.unscaledDeltaTime);  // Затухание
            state.swingAngle += state.swingVelocity * Time.unscaledDeltaTime * 60f;

            // Применяем качание к кнопке
            float offsetX = Mathf.Sin(state.swingAngle * Mathf.Deg2Rad) * ropeButtons[i].ropeLength * 0.3f;
            Vector2 swungPos = new Vector2(state.hangPos.x + offsetX, state.hangPos.y);
            ropeButtons[i].button.anchoredPosition = swungPos;

            // Наклон кнопки
            ropeButtons[i].button.localRotation = Quaternion.Euler(0, 0, -state.swingAngle * 0.5f);
        }
    }

    // ═══════════ ПРОДОЛЖИТЬ ═══════════
    public void ResumeGame()
    {
        StartCoroutine(ResumeAnimation());
    }

    IEnumerator ResumeAnimation()
    {
        isAnimating = true;
        Time.timeScale = 1f;

        float elapsed = 0f;

        while (elapsed < retractDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = elapsed / retractDuration;
            float eased = t * t;

            // Кнопки уходят вверх
            for (int i = 0; i < ropeButtons.Length; i++)
            {
                if (i >= buttonStates.Count) continue;
                Vector2 current = ropeButtons[i].button.anchoredPosition;
                ropeButtons[i].button.anchoredPosition = Vector2.Lerp(current, buttonStates[i].hiddenPos, eased);
                ropeButtons[i].button.localRotation = Quaternion.Lerp(
                    ropeButtons[i].button.localRotation, Quaternion.identity, eased);
            }

            // Затемнение уходит
            if (darkOverlay != null)
            {
                float alpha = Mathf.Lerp(overlayColor.a, 0, t);
                darkOverlay.color = new Color(overlayColor.r, overlayColor.g, overlayColor.b, alpha);
            }

            yield return null;
        }

        pausePanel.SetActive(false);
        darkOverlay.gameObject.SetActive(false);
        isPaused = false;
        isAnimating = false;
    }

    // ═══════════ КНОПКИ МЕНЮ ═══════════
    public void OnResumeButton() { ResumeGame(); }

    public void OnRestartButton()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    public void OnMainMenuButton()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(menuSceneName);
    }

    public void OnExitButton()
    {
        Application.Quit();
    }

    // ═══════════ BOUNCE EASING ═══════════
    float BounceEaseOut(float t)
    {
        if (t < 1f / 2.75f)
            return 7.5625f * t * t;
        else if (t < 2f / 2.75f)
        {
            t -= 1.5f / 2.75f;
            return 7.5625f * t * t + 0.75f;
        }
        else if (t < 2.5f / 2.75f)
        {
            t -= 2.25f / 2.75f;
            return 7.5625f * t * t + 0.9375f;
        }
        else
        {
            t -= 2.625f / 2.75f;
            return 7.5625f * t * t + 0.984375f;
        }
    }
}
