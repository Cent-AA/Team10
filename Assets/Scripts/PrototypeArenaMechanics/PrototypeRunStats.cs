using System.Collections.Generic;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class PrototypeRunStats : MonoBehaviour
{
    public static PrototypeRunStats Instance { get; private set; }

    [Header("Run Result")]
    public string resultTitle = "RUN RESULT";
    public bool pauseOnGameOver = true;

    [Header("Serialized HUD")]
    public TextMeshProUGUI hudText;
    public GameObject resultPanel;
    public TextMeshProUGUI resultTitleText;
    public TextMeshProUGUI resultStatsText;
    public Button restartButton;
    public Button menuButton;

    private WaveManager waveManager;
    private readonly List<string> selectedCards = new List<string>();
    private int currentWave;
    private int kills;
    private bool runEnded;

    public bool RunEnded => runEnded;
    public int CurrentWave => currentWave;
    public int Kills => kills;

    void Awake()
    {
        Instance = this;
    }

    void OnDestroy()
    {
        if (Instance == this)
            Instance = null;

        if (waveManager != null)
        {
            waveManager.OnWaveStart -= HandleWaveStart;
        }
    }

    void Start()
    {
        waveManager = FindFirstObjectByType<WaveManager>();
        if (waveManager != null)
            waveManager.OnWaveStart += HandleWaveStart;

        ResolveHudReferences();
        UpdateHud();
    }

    void Update()
    {
        if (runEnded)
            return;

        if (AllPlayersDead())
            EndRun("All players fell");
    }

    void HandleWaveStart(int wave)
    {
        currentWave = wave;
        UpdateHud();
    }

    public void RegisterKill()
    {
        if (runEnded)
            return;

        kills++;
        UpdateHud();
    }

    public void RegisterCard(string cardName)
    {
        if (!string.IsNullOrWhiteSpace(cardName))
            selectedCards.Add(cardName);

        UpdateHud();
    }

    public void EndRun(string reason)
    {
        if (runEnded)
            return;

        runEnded = true;
        ShowResult(reason);

        if (pauseOnGameOver)
            Time.timeScale = 0f;
    }

    bool AllPlayersDead()
    {
        Registry.CleanupPlayers();
        bool sawPlayer = false;

        for (int i = 0; i < Registry.Players.Count; i++)
        {
            Transform player = Registry.Players[i];
            if (player == null)
                continue;

            PlayerController playerController = player.GetComponent<PlayerController>();
            if (playerController == null)
                playerController = player.GetComponentInChildren<PlayerController>();

            if (playerController != null)
            {
                sawPlayer = true;
                if (playerController.currentHealth > 0f)
                    return false;

                continue;
            }

            EngineerController engineer = player.GetComponent<EngineerController>();
            if (engineer == null)
                engineer = player.GetComponentInChildren<EngineerController>();

            if (engineer != null)
            {
                sawPlayer = true;
                if (engineer.currentHealth > 0f)
                    return false;
            }
        }

        return sawPlayer;
    }

    void ResolveHudReferences()
    {
        Canvas canvas = null;
        if (hudText == null || resultPanel == null)
            canvas = PrototypeArenaUi.GetOrCreateCanvas("PrototypeArenaHUD", 5500);

        if (hudText == null)
        {
            hudText = PrototypeArenaUi.CreateText(
                canvas.transform,
                "RunStatsText",
                "",
                24,
                TextAlignmentOptions.TopRight,
                new Vector2(1f, 1f),
                new Vector2(1f, 1f),
                new Vector2(-24f, -24f),
                new Vector2(420f, 130f));
        }

        if (resultPanel == null)
        {
            Transform existing = canvas.transform.Find("ResultPanel");
            resultPanel = existing != null
                ? existing.gameObject
                : PrototypeArenaUi.CreatePanel(
                    canvas.transform,
                    "ResultPanel",
                    new Color(0.03f, 0.025f, 0.02f, 0.92f),
                    new Vector2(0.5f, 0.5f),
                    new Vector2(0.5f, 0.5f),
                    Vector2.zero,
                    new Vector2(760f, 520f)).gameObject;
        }

        EnsureResultHudComponents();
        resultPanel.SetActive(false);
    }

    void EnsureResultHudComponents()
    {
        if (resultPanel == null)
            return;

        if (resultTitleText == null)
        {
            resultTitleText = PrototypeArenaUi.CreateText(
                resultPanel.transform,
                "Title",
                resultTitle,
                20,
                TextAlignmentOptions.Center,
                new Vector2(0.5f, 1f),
                new Vector2(0.5f, 1f),
                new Vector2(0f, -58f),
                new Vector2(620f, 70f));
        }

        if (resultStatsText == null)
        {
            resultStatsText = PrototypeArenaUi.CreateText(
                resultPanel.transform,
                "Stats",
                "",
                16,
                TextAlignmentOptions.Top,
                Vector2.zero,
                Vector2.one,
                new Vector2(0f, 16f),
                new Vector2(-80f, -190f));
        }

        if (restartButton == null)
        {
            restartButton = PrototypeArenaUi.CreateButton(
                resultPanel.transform,
                "RestartButton",
                "Restart",
                new Vector2(0.5f, 0f),
                new Vector2(0.5f, 0f),
                new Vector2(-140f, 70f),
                new Vector2(220f, 64f),
                RestartScene);
        }

        if (menuButton == null)
        {
            menuButton = PrototypeArenaUi.CreateButton(
                resultPanel.transform,
                "MenuButton",
                "Menu",
                new Vector2(0.5f, 0f),
                new Vector2(0.5f, 0f),
                new Vector2(140f, 70f),
                new Vector2(220f, 64f),
                LoadMainMenu);
        }

        restartButton.onClick.RemoveListener(RestartScene);
        restartButton.onClick.AddListener(RestartScene);
        menuButton.onClick.RemoveListener(LoadMainMenu);
        menuButton.onClick.AddListener(LoadMainMenu);
    }

    void UpdateHud()
    {
        if (hudText == null)
            return;

        hudText.text = $"Wave {currentWave}\nKills {kills}\nCards {selectedCards.Count}";
    }

    void ShowResult(string reason)
    {
        if (resultPanel == null || resultTitleText == null || resultStatsText == null)
            ResolveHudReferences();

        resultTitleText.text = resultTitle;

        StringBuilder builder = new StringBuilder();
        builder.AppendLine(reason);
        builder.AppendLine($"Reached wave: {currentWave}");
        builder.AppendLine($"Kills: {kills}");
        builder.AppendLine();
        builder.AppendLine("Selected cards:");

        if (selectedCards.Count == 0)
        {
            builder.AppendLine("- none");
        }
        else
        {
            for (int i = 0; i < selectedCards.Count; i++)
                builder.AppendLine("- " + selectedCards[i]);
        }

        resultStatsText.text = builder.ToString();
        resultPanel.SetActive(true);
    }

    void RestartScene()
    {
        Time.timeScale = 1f;
        Scene activeScene = SceneManager.GetActiveScene();
        if (activeScene.IsValid())
            SceneManager.LoadScene(activeScene.name);
    }

    void LoadMainMenu()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene("MainMenu");
    }
}

internal static class PrototypeArenaUi
{
    public static Canvas GetOrCreateCanvas(string name, int sortingOrder)
    {
        GameObject existing = GameObject.Find(name);
        if (existing != null)
        {
            Canvas existingCanvas = existing.GetComponent<Canvas>();
            if (existingCanvas == null)
                existingCanvas = existing.AddComponent<Canvas>();

            ConfigureCanvas(existingCanvas, sortingOrder);
            return existingCanvas;
        }

        Canvas parentCanvas = FindMainCanvas();
        GameObject canvasObject = new GameObject(name);
        if (parentCanvas != null)
            canvasObject.transform.SetParent(parentCanvas.transform, false);

        RectTransform rect = canvasObject.AddComponent<RectTransform>();
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.anchoredPosition = Vector2.zero;
        rect.sizeDelta = Vector2.zero;

        Canvas canvas = canvasObject.AddComponent<Canvas>();
        ConfigureCanvas(canvas, sortingOrder);
        EnsureEventSystem();
        return canvas;
    }

    static Canvas FindMainCanvas()
    {
        Canvas[] canvases = Object.FindObjectsByType<Canvas>(FindObjectsSortMode.None);
        for (int i = 0; i < canvases.Length; i++)
        {
            Canvas canvas = canvases[i];
            if (canvas != null && canvas.name == "Canvas")
                return canvas;
        }

        return canvases.Length > 0 ? canvases[0] : null;
    }

    static void ConfigureCanvas(Canvas canvas, int sortingOrder)
    {
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.overrideSorting = true;
        canvas.sortingOrder = sortingOrder;

        CanvasScaler scaler = canvas.GetComponent<CanvasScaler>();
        if (scaler == null)
            scaler = canvas.gameObject.AddComponent<CanvasScaler>();

        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;

        if (canvas.GetComponent<GraphicRaycaster>() == null)
            canvas.gameObject.AddComponent<GraphicRaycaster>();
    }

    static RectTransform FindOrCreateRectTransform(Transform parent, string name)
    {
        Transform child = parent != null ? parent.Find(name) : null;
        GameObject childObject = child != null ? child.gameObject : new GameObject(name);
        if (parent != null && childObject.transform.parent != parent)
            childObject.transform.SetParent(parent, false);

        RectTransform rect = childObject.GetComponent<RectTransform>();
        if (rect == null)
            rect = childObject.AddComponent<RectTransform>();

        childObject.SetActive(true);
        return rect;
    }

    public static void SetChildActive(Transform parent, string name, bool active)
    {
        Transform child = parent != null ? parent.Find(name) : null;
        if (child != null)
            child.gameObject.SetActive(active);
    }

    public static Canvas CreateStandaloneCanvas(string name, int sortingOrder)
    {
        GameObject canvasObject = new GameObject(name);
        Canvas canvas = canvasObject.AddComponent<Canvas>();
        ConfigureCanvas(canvas, sortingOrder);
        EnsureEventSystem();
        return canvas;
    }

    public static Canvas GetOrCreateRootCanvas(string name, int sortingOrder)
    {
        GameObject existing = GameObject.Find(name);
        if (existing != null && existing.TryGetComponent(out Canvas existingCanvas))
        {
            ConfigureCanvas(existingCanvas, sortingOrder);
            return existingCanvas;
        }

        return CreateStandaloneCanvas(name, sortingOrder);
    }

    public static Image CreatePanel(
        Transform parent,
        string name,
        Color color,
        Vector2 anchorMin,
        Vector2 anchorMax,
        Vector2 anchoredPosition,
        Vector2 sizeDelta)
    {
        RectTransform rect = FindOrCreateRectTransform(parent, name);
        rect.anchorMin = anchorMin;
        rect.anchorMax = anchorMax;
        rect.anchoredPosition = anchoredPosition;
        rect.sizeDelta = sizeDelta;

        Image image = rect.GetComponent<Image>();
        if (image == null)
            image = rect.gameObject.AddComponent<Image>();

        image.color = color;
        return image;
    }

    public static TextMeshProUGUI CreateText(
        Transform parent,
        string name,
        string text,
        float fontSize,
        TextAlignmentOptions alignment,
        Vector2 anchorMin,
        Vector2 anchorMax,
        Vector2 anchoredPosition,
        Vector2 sizeDelta)
    {
        RectTransform rect = FindOrCreateRectTransform(parent, name);
        rect.anchorMin = anchorMin;
        rect.anchorMax = anchorMax;
        rect.anchoredPosition = anchoredPosition;
        rect.sizeDelta = sizeDelta;

        TextMeshProUGUI label = rect.GetComponent<TextMeshProUGUI>();
        if (label == null)
            label = rect.gameObject.AddComponent<TextMeshProUGUI>();

        label.text = text;
        label.fontSize = fontSize;
        label.color = Color.white;
        label.alignment = alignment;
        label.enableWordWrapping = true;
        return label;
    }

    public static Button CreateButton(
        Transform parent,
        string name,
        string label,
        Vector2 anchorMin,
        Vector2 anchorMax,
        Vector2 anchoredPosition,
        Vector2 sizeDelta,
        UnityEngine.Events.UnityAction onClick)
    {
        Image image = CreatePanel(parent, name, new Color(0.12f, 0.14f, 0.16f, 0.96f), anchorMin, anchorMax, anchoredPosition, sizeDelta);
        Button button = image.GetComponent<Button>();
        if (button == null)
            button = image.gameObject.AddComponent<Button>();

        button.targetGraphic = image;
        button.onClick.RemoveAllListeners();
        button.onClick.AddListener(onClick);

        CreateText(
            image.transform,
            "Label",
            label,
            16,
            TextAlignmentOptions.Center,
            Vector2.zero,
            Vector2.one,
            Vector2.zero,
            new Vector2(-24f, -24f));

        return button;
    }

    static void EnsureEventSystem()
    {
        if (EventSystem.current != null)
            return;

        GameObject eventSystem = new GameObject("PrototypeEventSystem");
        eventSystem.AddComponent<EventSystem>();
        eventSystem.AddComponent<StandaloneInputModule>();
    }
}
