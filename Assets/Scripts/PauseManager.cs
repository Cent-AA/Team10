using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System.Collections;
using System.Collections.Generic;

public class PauseManager : MonoBehaviour
{
    [Header("UI")]
    public GameObject pausePanel;
    public Image darkOverlay;
    public Color overlayColor = new Color(0, 0, 0, 0.6f);

    [Header("Rope Buttons")]
    public RopeButton[] ropeButtons;

    [Header("Button Animation")]
    public float dropDelay = 0.15f;
    public float dropDuration = 0.8f;
    public float swingDamping = 3f;
    public float swingFrequency = 4f;
    public float initialSwingAngle = 25f;
    public float fadeInSpeed = 5f;
    public float retractDuration = 0.5f;

    [Header("Menu Transition")]
    public RectTransform bushTransition;
    public float bushDropDuration = 1.5f;
    public string menuSceneName = "MainMenu";

    private bool isPaused = false;
    private bool isAnimating = false;
    private bool isExitingToMenu = false;
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
            darkOverlay.color = new Color(overlayColor.r, overlayColor.g, overlayColor.b, 0f);
        }

        if (bushTransition != null)
            bushOriginalScale = bushTransition.localScale;

        SetupButtons();
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape) && !isAnimating && !isExitingToMenu)
        {
            if (isPaused) ResumeGame();
            else PauseGame();
        }

        if (isPaused && !isExitingToMenu)
            UpdateSwinging();
    }

    void SetupButtons()
    {
        buttonStates.Clear();
        if (ropeButtons == null) return;

        for (int i = 0; i < ropeButtons.Length; i++)
        {
            RopeButton rb = ropeButtons[i];
            RopeButtonState state = new RopeButtonState();

            if (rb == null || rb.button == null)
            {
                buttonStates.Add(state);
                continue;
            }

            state.hangPos = rb.button.anchoredPosition;
            state.hiddenPos = new Vector2(state.hangPos.x, state.hangPos.y + rb.ropeLength + 500f);
            rb.button.anchoredPosition = state.hiddenPos;

            buttonStates.Add(state);
        }
    }

    public void PauseGame()
    {
        if (pausePanel == null) return;

        isPaused = true;
        isAnimating = true;
        Time.timeScale = 0f;
        pausePanel.SetActive(true);
        if (darkOverlay != null) darkOverlay.gameObject.SetActive(true);

        StartCoroutine(PauseAnimation());
    }

    IEnumerator PauseAnimation()
    {
        if (ropeButtons == null)
        {
            isAnimating = false;
            yield break;
        }

        for (int i = 0; i < ropeButtons.Length; i++)
        {
            if (i >= buttonStates.Count || ropeButtons[i] == null || ropeButtons[i].button == null) continue;
            buttonStates[i].dropTimer = 0f;
            buttonStates[i].dropped = false;
            buttonStates[i].swingAngle = 0f;
            buttonStates[i].swingVelocity = 0f;
            ropeButtons[i].button.anchoredPosition = buttonStates[i].hiddenPos;
        }

        float totalAnimTime = dropDuration + ropeButtons.Length * dropDelay + 1f;
        float elapsed = 0f;

        while (elapsed < totalAnimTime)
        {
            elapsed += Time.unscaledDeltaTime;

            if (darkOverlay != null)
            {
                float alpha = Mathf.Lerp(0f, overlayColor.a, Mathf.Clamp01(elapsed * fadeInSpeed));
                darkOverlay.color = new Color(overlayColor.r, overlayColor.g, overlayColor.b, alpha);
            }

            for (int i = 0; i < ropeButtons.Length; i++)
            {
                if (i >= buttonStates.Count || ropeButtons[i] == null || ropeButtons[i].button == null) continue;
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

    void UpdateSwinging()
    {
        if (ropeButtons == null) return;

        for (int i = 0; i < ropeButtons.Length; i++)
        {
            if (i >= buttonStates.Count || !buttonStates[i].dropped) continue;
            if (ropeButtons[i] == null || ropeButtons[i].button == null) continue;

            RopeButtonState state = buttonStates[i];

            float gravity = 9.8f;
            float length = ropeButtons[i].ropeLength / 100f;
            float acceleration = -(gravity / length) * Mathf.Sin(state.swingAngle * Mathf.Deg2Rad);

            state.swingVelocity += acceleration * Time.unscaledDeltaTime * swingFrequency;
            state.swingVelocity *= (1f - swingDamping * Time.unscaledDeltaTime);
            state.swingAngle += state.swingVelocity * Time.unscaledDeltaTime * 60f;

            float offsetX = Mathf.Sin(state.swingAngle * Mathf.Deg2Rad) * ropeButtons[i].ropeLength * 0.3f;
            ropeButtons[i].button.anchoredPosition = new Vector2(state.hangPos.x + offsetX, state.hangPos.y);
            ropeButtons[i].button.localRotation = Quaternion.Euler(0f, 0f, -state.swingAngle * 0.5f);
        }
    }

    public void ResumeGame()
    {
        if (isExitingToMenu) return;
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
            float t = retractDuration <= 0f ? 1f : Mathf.Clamp01(elapsed / retractDuration);

            RetractButtons(t);

            if (darkOverlay != null)
            {
                float alpha = Mathf.Lerp(overlayColor.a, 0f, t);
                darkOverlay.color = new Color(overlayColor.r, overlayColor.g, overlayColor.b, alpha);
            }

            yield return null;
        }

        if (pausePanel != null) pausePanel.SetActive(false);
        if (darkOverlay != null) darkOverlay.gameObject.SetActive(false);
        isPaused = false;
        isAnimating = false;
    }

    public void OnMainMenuButton()
    {
        if (isExitingToMenu) return;
        isExitingToMenu = true;
        StartCoroutine(MenuTransitionRoutine());
    }

    IEnumerator MenuTransitionRoutine()
    {
        isAnimating = true;
        isPaused = false;

        Vector2[] buttonStartPositions = CaptureButtonPositions();
        Quaternion[] buttonStartRotations = CaptureButtonRotations();

        Vector3 bushStartPos = Vector3.zero;
        Vector3 bushEndPos = Vector3.zero;
        bool hasBushTransition = bushTransition != null;

        if (hasBushTransition)
        {
            bushTransition.gameObject.SetActive(true);
            bushTransition.localScale = bushOriginalScale;
            bushEndPos = new Vector3(bushTransition.localPosition.x, 0f, bushTransition.localPosition.z);
            bushStartPos = bushEndPos + Vector3.up * 2000f;
            bushTransition.localPosition = bushStartPos;
        }
        else
        {
            Debug.LogWarning("PauseManager: Bush Transition is not assigned.", this);
        }

        float elapsed = 0f;
        float totalDuration = hasBushTransition
            ? Mathf.Max(retractDuration, bushDropDuration)
            : retractDuration;

        while (elapsed < totalDuration)
        {
            elapsed += Time.unscaledDeltaTime;

            float buttonT = retractDuration <= 0f ? 1f : Mathf.Clamp01(elapsed / retractDuration);
            RetractButtonsFrom(buttonStartPositions, buttonStartRotations, buttonT);

            if (hasBushTransition)
            {
                float bushT = bushDropDuration <= 0f ? 1f : Mathf.Clamp01(elapsed / bushDropDuration);
                bushTransition.localPosition = Vector3.Lerp(bushStartPos, bushEndPos, EaseInOutSine(bushT));
            }

            yield return null;
        }

        if (hasBushTransition)
            bushTransition.localPosition = bushEndPos;

        yield return new WaitForSecondsRealtime(0.5f);

        Time.timeScale = 1f;
        MenuTransition.SetPauseEntry();
        SceneManager.LoadScene(menuSceneName);
    }

    Vector2[] CaptureButtonPositions()
    {
        int count = ropeButtons != null ? ropeButtons.Length : 0;
        Vector2[] positions = new Vector2[count];
        for (int i = 0; i < count; i++)
        {
            if (ropeButtons[i] != null && ropeButtons[i].button != null)
                positions[i] = ropeButtons[i].button.anchoredPosition;
        }

        return positions;
    }

    Quaternion[] CaptureButtonRotations()
    {
        int count = ropeButtons != null ? ropeButtons.Length : 0;
        Quaternion[] rotations = new Quaternion[count];
        for (int i = 0; i < count; i++)
        {
            rotations[i] = Quaternion.identity;
            if (ropeButtons[i] != null && ropeButtons[i].button != null)
                rotations[i] = ropeButtons[i].button.localRotation;
        }

        return rotations;
    }

    void RetractButtons(float t)
    {
        Vector2[] startPositions = CaptureButtonPositions();
        Quaternion[] startRotations = CaptureButtonRotations();
        RetractButtonsFrom(startPositions, startRotations, t);
    }

    void RetractButtonsFrom(Vector2[] startPositions, Quaternion[] startRotations, float t)
    {
        if (ropeButtons == null) return;

        float eased = t * t;
        for (int i = 0; i < ropeButtons.Length; i++)
        {
            if (i >= buttonStates.Count || ropeButtons[i] == null || ropeButtons[i].button == null) continue;
            ropeButtons[i].button.anchoredPosition = Vector2.Lerp(
                startPositions[i],
                buttonStates[i].hiddenPos,
                eased);
            ropeButtons[i].button.localRotation = Quaternion.Lerp(
                startRotations[i],
                Quaternion.identity,
                t);
        }
    }

    public void OnResumeButton()
    {
        ResumeGame();
    }

    public void OnRestartButton()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    public void OnExitButton()
    {
        Application.Quit();
    }

    float BounceEaseOut(float t)
    {
        if (t < 1f / 2.75f) return 7.5625f * t * t;
        if (t < 2f / 2.75f) { t -= 1.5f / 2.75f; return 7.5625f * t * t + 0.75f; }
        if (t < 2.5f / 2.75f) { t -= 2.25f / 2.75f; return 7.5625f * t * t + 0.9375f; }
        t -= 2.625f / 2.75f;
        return 7.5625f * t * t + 0.984375f;
    }

    float EaseInOutSine(float t)
    {
        return -(Mathf.Cos(Mathf.PI * t) - 1f) / 2f;
    }
}
