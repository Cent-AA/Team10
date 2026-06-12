using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;
using System.Collections;
using System.Collections.Generic;

public class PauseManager : MonoBehaviour
{
    [Header("═══ UI ═══")]
    public GameObject pausePanel;
    public Image darkOverlay;
    public Color overlayColor = new Color(0, 0, 0, 0.6f);

    [Header("═══ Кнопки на верёвках ═══")]
    public RopeButton[] ropeButtons;

    [Header("═══ Анимация кнопок ═══")]
    public float dropDelay = 0.15f;
    public float dropDuration = 0.8f;
    public float swingDamping = 3f;
    public float swingFrequency = 4f;
    public float initialSwingAngle = 25f;
    public float fadeInSpeed = 5f;
    public float retractDuration = 0.5f;

    [Header("═══ Переход в меню ═══")]
    public RectTransform bushTransition;
    public float bushDropDuration = 1.5f;
    public string menuSceneName = "MainMenu";

    private bool isPaused = false;
    private bool isAnimating = false;
    private List<RopeButtonState> buttonStates = new List<RopeButtonState>();
    private Vector3 bushOriginalScale;

    [System.Serializable]
    public class RopeButton
    {
        public RectTransform button;
        public float ropeLength = 300f;
        public float extraDelay = 0f;
    }

    private class RopeButtonState
    {
        public Vector2 hiddenPos;
        public Vector2 hangPos;
        public float dropTimer;
        public float swingAngle;
        public float swingVelocity;
        public bool dropped;
    }

    void Start()
    {
        if (pausePanel != null) pausePanel.SetActive(false);
        if (darkOverlay != null)
        {
            darkOverlay.gameObject.SetActive(false);
            darkOverlay.color = new Color(overlayColor.r, overlayColor.g, overlayColor.b, 0);
        }
        if (bushTransition != null)
        {
            bushOriginalScale = bushTransition.localScale;
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

        if (isPaused) UpdateSwinging();
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
            rb.button.anchoredPosition = state.hiddenPos;

            buttonStates.Add(state);
        }
    }

    // ═══════════ ПАУЗА ═══════════
    public void PauseGame()
    {
        isPaused = true;
        isAnimating = true;
        Time.timeScale = 0f;
        pausePanel.SetActive(true);
        if (darkOverlay != null) darkOverlay.gameObject.SetActive(true);

        StartCoroutine(PauseAnimation());
    }

    IEnumerator PauseAnimation()
    {
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

            if (darkOverlay != null)
            {
                float alpha = Mathf.Lerp(0, overlayColor.a, Mathf.Clamp01(elapsed * fadeInSpeed));
                darkOverlay.color = new Color(overlayColor.r, overlayColor.g, overlayColor.b, alpha);
            }

            for (int i = 0; i < ropeButtons.Length; i++)
            {
                if (i >= buttonStates.Count) continue;
                float delay = dropDelay * i + ropeButtons[i].extraDelay;

                if (elapsed > delay && !buttonStates[i].dropped)
                {
                    buttonStates[i].dropTimer += Time.unscaledDeltaTime;
                    float t = Mathf.Clamp01(buttonStates[i].dropTimer / dropDuration);
                    float eased = BounceEaseOut(t);

                    ropeButtons[i].button.anchoredPosition = Vector2.Lerp(
                        buttonStates[i].hiddenPos, buttonStates[i].hangPos, eased);

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

        isAnimating = false;
    }

    // ═══════════ КАЧАНИЕ ═══════════
    void UpdateSwinging()
    {
        for (int i = 0; i < ropeButtons.Length; i++)
        {
            if (i >= buttonStates.Count || !buttonStates[i].dropped) continue;
            if (ropeButtons[i].button == null) continue;

            RopeButtonState state = buttonStates[i];

            float gravity = 9.8f;
            float length = ropeButtons[i].ropeLength / 100f;
            float acceleration = -(gravity / length) * Mathf.Sin(state.swingAngle * Mathf.Deg2Rad);

            state.swingVelocity += acceleration * Time.unscaledDeltaTime * swingFrequency;
            state.swingVelocity *= (1f - swingDamping * Time.unscaledDeltaTime);
            state.swingAngle += state.swingVelocity * Time.unscaledDeltaTime * 60f;

            float offsetX = Mathf.Sin(state.swingAngle * Mathf.Deg2Rad) * ropeButtons[i].ropeLength * 0.3f;
            ropeButtons[i].button.anchoredPosition = new Vector2(state.hangPos.x + offsetX, state.hangPos.y);
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

            for (int i = 0; i < ropeButtons.Length; i++)
            {
                if (i >= buttonStates.Count || ropeButtons[i].button == null) continue;
                ropeButtons[i].button.anchoredPosition = Vector2.Lerp(
                    ropeButtons[i].button.anchoredPosition, buttonStates[i].hiddenPos, t * t);
                ropeButtons[i].button.localRotation = Quaternion.Lerp(
                    ropeButtons[i].button.localRotation, Quaternion.identity, t);
            }

            if (darkOverlay != null)
            {
                float alpha = Mathf.Lerp(overlayColor.a, 0, t);
                darkOverlay.color = new Color(overlayColor.r, overlayColor.g, overlayColor.b, alpha);
            }

            yield return null;
        }

        pausePanel.SetActive(false);
        if (darkOverlay != null) darkOverlay.gameObject.SetActive(false);
        isPaused = false;
        isAnimating = false;
    }

    // ═══════════ ВЫХОД В МЕНЮ — куст плавно спускается ═══════════
    public void OnMainMenuButton()
    {
        StartCoroutine(MenuTransitionRoutine());
    }

    IEnumerator MenuTransitionRoutine()
    {
        isAnimating = true;

        // Кнопки уходят вверх
        float elapsed = 0f;
        while (elapsed < retractDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = elapsed / retractDuration;

            for (int i = 0; i < ropeButtons.Length; i++)
            {
                if (i >= buttonStates.Count || ropeButtons[i].button == null) continue;
                ropeButtons[i].button.anchoredPosition = Vector2.Lerp(
                    ropeButtons[i].button.anchoredPosition, buttonStates[i].hiddenPos, t * t);
            }
            yield return null;
        }

        // Куст плавно спускается сверху
        if (bushTransition != null)
        {
            bushTransition.gameObject.SetActive(true);
            bushTransition.localScale = bushOriginalScale;

            // Запоминаем конечную позицию — центр экрана
            Vector3 endPos = new Vector3(bushTransition.localPosition.x, 0, bushTransition.localPosition.z);
            Vector3 startPos = endPos + Vector3.up * 2000f;
            bushTransition.localPosition = startPos;

            Debug.Log("Bush start: " + startPos + " end: " + endPos + " duration: " + bushDropDuration);

            elapsed = 0f;
            while (elapsed < bushDropDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / bushDropDuration);
                float eased = EaseInOutSine(t);

                bushTransition.localPosition = Vector3.Lerp(startPos, endPos, eased);
                yield return null;
            }

            bushTransition.localPosition = endPos;
            Debug.Log("Bush animation done");
        }
        else
        {
            Debug.LogWarning("Bush Transition не назначен!");
        }

        yield return new WaitForSecondsRealtime(0.5f);

        Time.timeScale = 1f;
        MenuTransition.SetPauseEntry();
        SceneManager.LoadScene(menuSceneName);
    }

    // ═══════════ ДРУГИЕ КНОПКИ ═══════════
    public void OnResumeButton() { ResumeGame(); }

    public void OnRestartButton()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    public void OnExitButton()
    {
        Application.Quit();
    }

    // ═══════════ EASING ═══════════
    float BounceEaseOut(float t)
    {
        if (t < 1f / 2.75f) return 7.5625f * t * t;
        else if (t < 2f / 2.75f) { t -= 1.5f / 2.75f; return 7.5625f * t * t + 0.75f; }
        else if (t < 2.5f / 2.75f) { t -= 2.25f / 2.75f; return 7.5625f * t * t + 0.9375f; }
        else { t -= 2.625f / 2.75f; return 7.5625f * t * t + 0.984375f; }
    }

    float EaseInOutSine(float t) { return -(Mathf.Cos(Mathf.PI * t) - 1f) / 2f; }
}