using TMPro;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

[InitializeOnLoad]
public static class TrainingTutorialHierarchyBuilder
{
    private const string TrainingScenePath = "Assets/Scenes/TrainingScene.unity";
    private const string TutorialRootName = "TrainingTutorialUI";

    static TrainingTutorialHierarchyBuilder()
    {
        EditorApplication.update -= TryAutomaticMigration;
        EditorApplication.update += TryAutomaticMigration;
    }

    [MenuItem("Tools/Training/Build Editable Tutorial Hierarchy")]
    private static void BuildFromMenu()
    {
        Scene scene = SceneManager.GetActiveScene();
        if (scene.path != TrainingScenePath)
        {
            EditorUtility.DisplayDialog("Training Tutorial", "Open TrainingScene before running this command.", "OK");
            return;
        }

        Build(scene, true);
    }

    public static void BuildSceneFromCommandLine()
    {
        Scene scene = EditorSceneManager.OpenScene(TrainingScenePath, OpenSceneMode.Single);
        Build(scene, true);
        CodexSceneExporter.ExportOpenSceneReport();
    }

    [MenuItem("Tools/Training/Mirror Player 1 Tutorial Visual To Player 2")]
    private static void MirrorPlayerOneVisualFromMenu()
    {
        Scene scene = SceneManager.GetActiveScene();
        if (scene.path != TrainingScenePath)
        {
            EditorUtility.DisplayDialog("Training Tutorial", "Open TrainingScene before running this command.", "OK");
            return;
        }

        MirrorPlayerOneVisualToPlayerTwo(scene);
    }

    public static void MirrorPlayerOneVisualFromCommandLine()
    {
        Scene scene = EditorSceneManager.OpenScene(TrainingScenePath, OpenSceneMode.Single);
        MirrorPlayerOneVisualToPlayerTwo(scene);
        CodexSceneExporter.ExportOpenSceneReport();
    }

    [MenuItem("Tools/Training/Convert Tutorial Tabs To Radio Buttons")]
    private static void ConvertTutorialTabsToRadioFromMenu()
    {
        Scene scene = SceneManager.GetActiveScene();
        if (scene.path != TrainingScenePath)
        {
            EditorUtility.DisplayDialog("Training Tutorial", "Open TrainingScene before running this command.", "OK");
            return;
        }

        ConvertTutorialTabsToRadio(scene);
    }

    public static void ConvertTutorialTabsToRadioFromCommandLine()
    {
        Scene scene = EditorSceneManager.OpenScene(TrainingScenePath, OpenSceneMode.Single);
        ConvertTutorialTabsToRadio(scene);
        CodexSceneExporter.ExportOpenSceneReport();
    }

    private static void TryAutomaticMigration()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isCompiling || EditorApplication.isUpdating)
            return;

        Scene scene = SceneManager.GetActiveScene();
        if (!scene.IsValid() || scene.path != TrainingScenePath)
            return;

        if (FindRoot(scene, TutorialRootName) != null)
        {
            EditorApplication.update -= TryAutomaticMigration;
            return;
        }

        // Never overwrite unrelated unsaved scene work during the automatic pass.
        if (scene.isDirty)
            return;

        EditorApplication.update -= TryAutomaticMigration;
        Build(scene, false);
    }

    private static void Build(Scene scene, bool forced)
    {
        GameObject existingRoot = FindRoot(scene, TutorialRootName);
        if (existingRoot != null)
        {
            FixPauseUi(scene);
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            Selection.activeGameObject = existingRoot;
            return;
        }

        Canvas canvas = FindMainCanvas(scene);
        if (canvas == null)
        {
            Debug.LogError("Training tutorial hierarchy: Canvas was not found in TrainingScene.");
            return;
        }

        Transform legacyHint = canvas.transform.Find("ButtonTutorial");
        Transform legacyPanel = canvas.transform.Find("TutorialPanel");
        if (legacyHint == null || legacyPanel == null)
        {
            Debug.LogError("Training tutorial hierarchy: ButtonTutorial or TutorialPanel was not found.");
            return;
        }

        DisableAndRemoveLegacyManagers(scene);
        ConfigureCanvasScaler(canvas);

        GameObject tutorialRoot = CreateUiObject(TutorialRootName, canvas.transform);
        ConfigureStretch(tutorialRoot.GetComponent<RectTransform>(), Vector2.zero, Vector2.one);
        tutorialRoot.transform.SetSiblingIndex(0);

        GameObject playerOneRoot = CreateUiObject("Player1Tutorial", tutorialRoot.transform);
        ConfigureStretch(playerOneRoot.GetComponent<RectTransform>(), Vector2.zero, new Vector2(0.5f, 1f));

        Undo.SetTransformParent(legacyHint, playerOneRoot.transform, "Move Player 1 tutorial hint");
        Undo.SetTransformParent(legacyPanel, playerOneRoot.transform, "Move Player 1 tutorial panel");
        legacyHint.name = "Hint";
        legacyPanel.name = "Panel";

        BuiltView playerOne = ConfigurePlayerHierarchy(playerOneRoot, 1);

        GameObject playerTwoRoot = Object.Instantiate(playerOneRoot, tutorialRoot.transform);
        Undo.RegisterCreatedObjectUndo(playerTwoRoot, "Create Player 2 tutorial hierarchy");
        playerTwoRoot.name = "Player2Tutorial";
        ConfigureStretch(playerTwoRoot.GetComponent<RectTransform>(), new Vector2(0.5f, 0f), Vector2.one);
        BuiltView playerTwo = ConfigurePlayerHierarchy(playerTwoRoot, 2);

        TrainingTutorialManager manager = Undo.AddComponent<TrainingTutorialManager>(tutorialRoot);
        manager.ConfigureForEditor(CreateSerializedView(playerOne, 1), CreateSerializedView(playerTwo, 2));
        EditorUtility.SetDirty(manager);

        playerOne.Panel.SetActive(false);
        playerOne.Hint.SetActive(true);
        playerOne.BasicsContent.SetActive(true);
        playerOne.CardsContent.SetActive(false);
        playerTwo.Panel.SetActive(false);
        playerTwo.Hint.SetActive(true);
        playerTwo.BasicsContent.SetActive(true);
        playerTwo.CardsContent.SetActive(false);

        FixPauseUi(scene);
        EditorSceneManager.MarkSceneDirty(scene);
        bool saved = EditorSceneManager.SaveScene(scene);
        Selection.activeGameObject = tutorialRoot;

        Debug.Log(saved
            ? "Training tutorial hierarchy created and TrainingScene saved."
            : "Training tutorial hierarchy created, but TrainingScene could not be saved.");
    }

    private static void ConvertTutorialTabsToRadio(Scene scene)
    {
        Canvas canvas = FindMainCanvas(scene);
        Transform tutorialRoot = canvas != null ? canvas.transform.Find(TutorialRootName) : null;
        TrainingTutorialManager manager = tutorialRoot != null
            ? tutorialRoot.GetComponent<TrainingTutorialManager>()
            : null;
        if (tutorialRoot == null || manager == null)
        {
            Debug.LogError("Training tutorial radio conversion: TrainingTutorialUI or manager was not found.");
            return;
        }

        SerializedObject serializedManager = new SerializedObject(manager);
        for (int playerNumber = 1; playerNumber <= 2; playerNumber++)
        {
            Transform playerRoot = tutorialRoot.Find("Player" + playerNumber + "Tutorial");
            Transform panel = playerRoot != null ? playerRoot.Find("Panel") : null;
            if (panel == null)
            {
                Debug.LogError("Training tutorial radio conversion: Player " + playerNumber + " panel was not found.");
                return;
            }

            Transform basicsTransform = panel.Find("TabBasicsRadioButton") ?? panel.Find("TabBasicsButton");
            Transform cardsTransform = panel.Find("TabCardsRadioButton") ?? panel.Find("TabCardsButton");
            Transform basicsContent = panel.Find("InfoPanel/BasicsContent");
            Transform cardsContent = panel.Find("InfoPanel/CardsContent");
            if (basicsTransform == null || cardsTransform == null || basicsContent == null || cardsContent == null)
            {
                Debug.LogError("Training tutorial radio conversion: Player " + playerNumber + " tabs are incomplete.");
                return;
            }

            basicsTransform.name = "TabBasicsRadioButton";
            cardsTransform.name = "TabCardsRadioButton";

            ToggleGroup group = panel.GetComponent<ToggleGroup>();
            if (group == null)
                group = Undo.AddComponent<ToggleGroup>(panel.gameObject);
            group.allowSwitchOff = false;
            EditorUtility.SetDirty(group);

            Toggle basicsToggle = ConvertSelectableToToggle(basicsTransform, group, true);
            Toggle cardsToggle = ConvertSelectableToToggle(cardsTransform, group, false);
            Sprite basicsDefault = basicsTransform.GetComponent<Image>() != null
                ? basicsTransform.GetComponent<Image>().sprite
                : null;
            Sprite cardsDefault = cardsTransform.GetComponent<Image>() != null
                ? cardsTransform.GetComponent<Image>().sprite
                : null;
            ApplyEditorRadioSprite(basicsToggle, true, basicsDefault);
            ApplyEditorRadioSprite(cardsToggle, false, cardsDefault);

            basicsContent.gameObject.SetActive(true);
            cardsContent.gameObject.SetActive(false);

            SerializedProperty view = serializedManager.FindProperty(playerNumber == 1 ? "playerOne" : "playerTwo");
            view.FindPropertyRelative("basicsTabToggle").objectReferenceValue = basicsToggle;
            view.FindPropertyRelative("cardsTabToggle").objectReferenceValue = cardsToggle;
        }

        serializedManager.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(manager);
        EditorSceneManager.MarkSceneDirty(scene);
        bool saved = EditorSceneManager.SaveScene(scene);
        Selection.activeGameObject = tutorialRoot.gameObject;
        Debug.Log(saved
            ? "Training tutorial tabs converted to independent radio toggle groups and TrainingScene saved."
            : "Training tutorial tabs converted, but TrainingScene could not be saved.");
    }

    private static void MirrorPlayerOneVisualToPlayerTwo(Scene scene)
    {
        Canvas canvas = FindMainCanvas(scene);
        if (canvas == null)
        {
            Debug.LogError("Training tutorial mirror: Canvas was not found.");
            return;
        }

        Transform tutorialRoot = canvas.transform.Find(TutorialRootName);
        Transform playerOneRoot = tutorialRoot != null ? tutorialRoot.Find("Player1Tutorial") : null;
        Transform playerTwoRoot = tutorialRoot != null ? tutorialRoot.Find("Player2Tutorial") : null;
        Transform playerOnePanel = playerOneRoot != null ? playerOneRoot.Find("Panel") : null;
        Transform oldPlayerTwoPanel = playerTwoRoot != null ? playerTwoRoot.Find("Panel") : null;
        if (tutorialRoot == null || playerOnePanel == null || oldPlayerTwoPanel == null)
        {
            Debug.LogError("Training tutorial mirror: the Player 1 or Player 2 hierarchy is incomplete.");
            return;
        }

        int siblingIndex = oldPlayerTwoPanel.GetSiblingIndex();
        GameObject replacement = Object.Instantiate(playerOnePanel.gameObject, playerTwoRoot);
        Undo.RegisterCreatedObjectUndo(replacement, "Mirror Player 1 tutorial visual to Player 2");
        replacement.name = "Panel";
        replacement.transform.SetSiblingIndex(siblingIndex);
        Undo.DestroyObjectImmediate(oldPlayerTwoPanel.gameObject);

        AdaptMirroredTextForPlayerTwo(replacement.transform);

        Transform basicsContent = replacement.transform.Find("InfoPanel/BasicsContent");
        Transform cardsContent = replacement.transform.Find("InfoPanel/CardsContent");
        Transform basicsButton = replacement.transform.Find("TabBasicsRadioButton") ?? replacement.transform.Find("TabBasicsButton");
        Transform cardsButton = replacement.transform.Find("TabCardsRadioButton") ?? replacement.transform.Find("TabCardsButton");
        Transform closeButton = replacement.transform.Find("CloseButton");
        if (basicsContent == null || cardsContent == null || basicsButton == null ||
            cardsButton == null || closeButton == null)
        {
            Debug.LogError("Training tutorial mirror: the cloned Player 2 panel is incomplete.");
            return;
        }

        TrainingTutorialManager manager = tutorialRoot.GetComponent<TrainingTutorialManager>();
        if (manager == null)
        {
            Debug.LogError("Training tutorial mirror: TrainingTutorialManager was not found.");
            return;
        }

        SerializedObject serializedManager = new SerializedObject(manager);
        SerializedProperty playerTwo = serializedManager.FindProperty("playerTwo");
        playerTwo.FindPropertyRelative("panel").objectReferenceValue = replacement;
        playerTwo.FindPropertyRelative("basicsContent").objectReferenceValue = basicsContent.gameObject;
        playerTwo.FindPropertyRelative("cardsContent").objectReferenceValue = cardsContent.gameObject;
        playerTwo.FindPropertyRelative("basicsTabToggle").objectReferenceValue = basicsButton.GetComponent<Toggle>();
        playerTwo.FindPropertyRelative("cardsTabToggle").objectReferenceValue = cardsButton.GetComponent<Toggle>();
        playerTwo.FindPropertyRelative("closeButton").objectReferenceValue = closeButton.GetComponent<Button>();
        serializedManager.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(manager);

        EditorSceneManager.MarkSceneDirty(scene);
        bool saved = EditorSceneManager.SaveScene(scene);
        Selection.activeGameObject = replacement;
        Debug.Log(saved
            ? "Player 1 tutorial visual mirrored to Player 2 and TrainingScene saved."
            : "Player 2 tutorial visual mirrored, but TrainingScene could not be saved.");
    }

    private static void AdaptMirroredTextForPlayerTwo(Transform panel)
    {
        TMP_Text[] texts = panel.GetComponentsInChildren<TMP_Text>(true);
        for (int i = 0; i < texts.Length; i++)
        {
            texts[i].text = texts[i].text
                .Replace("#68A8FF", "#FFB14A")
                .Replace("PLAYER 1", "PLAYER 2")
                .Replace("Player 1", "Player 2")
                .Replace("<link=\"Move\">W A S D</link>", "<link=\"Move\">↑ ← ↓ →</link>")
                .Replace("<link=\"Run\">LEFT SHIFT</link>", "<link=\"Run\">RIGHT SHIFT</link>")
                .Replace("<link=\"Dash\">R</link>", "<link=\"Dash\">2</link>")
                .Replace("<link=\"Roll\">F</link>", "<link=\"Roll\">3</link>")
                .Replace("<link=\"LightAttack\">SPACE</link>", "<link=\"LightAttack\">0</link>")
                .Replace("<link=\"HeavyAttack\">Q</link>", "<link=\"HeavyAttack\">1</link>")
                .Replace("<link=\"Block\">C</link>", "<link=\"Block\">4</link>")
                .Replace("<link=\"Shoot\">J</link>", "<link=\"Shoot\">5</link>");
            EditorUtility.SetDirty(texts[i]);
        }

        Transform instructions = panel.Find("InfoPanel/Instructons");
        if (instructions == null)
            return;

        TMP_Text instructionsText = instructions.GetComponent<TMP_Text>();
        if (instructionsText != null)
        {
            instructionsText.text = "\t\t<color=#AEB8C4><link=\"Navigate\">← / →</link>: switch tab \t\t\t\t\t+: close</color>";
            EditorUtility.SetDirty(instructionsText);
        }
    }

    private static BuiltView ConfigurePlayerHierarchy(GameObject playerRoot, int playerNumber)
    {
        Transform hint = playerRoot.transform.Find("Hint");
        Transform panel = playerRoot.transform.Find("Panel");
        Transform basicsButton = panel.Find("ButtonMovement") ?? panel.Find("TabBasicsButton") ?? panel.Find("TabBasicsRadioButton");
        Transform cardsButton = panel.Find("ButtonCombat") ?? panel.Find("TabCardsButton") ?? panel.Find("TabCardsRadioButton");
        Transform unusedButton = panel.Find("ButtonTips");
        Transform infoPanel = panel.Find("InfoPanel");
        Transform basicsContent = infoPanel.Find("InfoText") ?? infoPanel.Find("BasicsContent");
        Transform closeButton = panel.Find("ButtonClose") ?? panel.Find("CloseButton");

        if (unusedButton != null)
            Undo.DestroyObjectImmediate(unusedButton.gameObject);

        hint.name = "Hint";
        panel.name = "Panel";
        basicsButton.name = "TabBasicsRadioButton";
        cardsButton.name = "TabCardsRadioButton";
        basicsContent.name = "BasicsContent";
        closeButton.name = "CloseButton";

        Button hintButton = hint.GetComponent<Button>();
        if (hintButton != null)
            Undo.DestroyObjectImmediate(hintButton);

        ConfigureHint(hint, playerNumber);
        ConfigurePanel(panel, playerNumber);
        ConfigureTopButton(basicsButton.GetComponent<RectTransform>(), new Vector2(20f, -20f));
        ConfigureTopButton(cardsButton.GetComponent<RectTransform>(), new Vector2(220f, -20f));
        ConfigureCloseButton(closeButton.GetComponent<RectTransform>());
        ConfigureInfoPanel(infoPanel.GetComponent<RectTransform>());
        ConfigureContentText(basicsContent.GetComponent<TMP_Text>());

        Transform cardsContent = infoPanel.Find("CardsContent");
        if (cardsContent == null)
        {
            GameObject copy = Object.Instantiate(basicsContent.gameObject, infoPanel);
            Undo.RegisterCreatedObjectUndo(copy, "Create cards tutorial content");
            copy.name = "CardsContent";
            cardsContent = copy.transform;
        }
        ConfigureContentText(cardsContent.GetComponent<TMP_Text>());

        SetText(hint, playerNumber == 1 ? "TAB  -  TUTORIAL" : "+  -  TUTORIAL");
        SetText(basicsButton, "BASICS");
        SetText(cardsButton, "CARDS");
        SetText(closeButton, "CLOSE");

        TMP_Text basicsText = basicsContent.GetComponent<TMP_Text>();
        TMP_Text cardsText = cardsContent.GetComponent<TMP_Text>();
        basicsText.text = BuildBasicsText(playerNumber);
        cardsText.text = BuildCardsText(playerNumber);
        EditorUtility.SetDirty(basicsText);
        EditorUtility.SetDirty(cardsText);

        ToggleGroup toggleGroup = panel.GetComponent<ToggleGroup>();
        if (toggleGroup == null)
            toggleGroup = Undo.AddComponent<ToggleGroup>(panel.gameObject);
        toggleGroup.allowSwitchOff = false;

        Toggle basics = ConfigureRadioToggle(basicsButton, toggleGroup, true, GetPlayerColor(playerNumber));
        Toggle cards = ConfigureRadioToggle(cardsButton, toggleGroup, false, new Color(0.13f, 0.15f, 0.18f, 0.95f));
        Button close = ConfigureButton(closeButton, new Color(0.18f, 0.2f, 0.23f, 0.95f));

        return new BuiltView
        {
            Hint = hint.gameObject,
            Panel = panel.gameObject,
            BasicsContent = basicsContent.gameObject,
            CardsContent = cardsContent.gameObject,
            BasicsToggle = basics,
            CardsToggle = cards,
            CloseButton = close
        };
    }

    private static TrainingTutorialManager.PlayerTutorialView CreateSerializedView(BuiltView built, int playerNumber)
    {
        return new TrainingTutorialManager.PlayerTutorialView
        {
            playerNumber = playerNumber,
            hint = built.Hint,
            panel = built.Panel,
            basicsContent = built.BasicsContent,
            cardsContent = built.CardsContent,
            basicsTabToggle = built.BasicsToggle,
            cardsTabToggle = built.CardsToggle,
            closeButton = built.CloseButton
        };
    }

    private static void ConfigureCanvasScaler(Canvas canvas)
    {
        CanvasScaler scaler = canvas.GetComponent<CanvasScaler>();
        if (scaler == null)
            return;

        Undo.RecordObject(scaler, "Configure Training Canvas Scaler");
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight = 0.5f;
        EditorUtility.SetDirty(scaler);
    }

    private static void ConfigureHint(Transform hint, int playerNumber)
    {
        RectTransform rect = hint.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 1f);
        rect.anchorMax = new Vector2(0.5f, 1f);
        rect.pivot = new Vector2(0.5f, 1f);
        rect.anchoredPosition = new Vector2(0f, -22f);
        rect.sizeDelta = new Vector2(320f, 62f);
        rect.localScale = Vector3.one;
        rect.localRotation = Quaternion.identity;

        Image image = hint.GetComponent<Image>();
        if (image != null)
        {
            Color accent = GetPlayerColor(playerNumber);
            image.color = new Color(accent.r * 0.35f, accent.g * 0.35f, accent.b * 0.35f, 0.9f);
            image.raycastTarget = false;
            EditorUtility.SetDirty(image);
        }

        TMP_Text label = hint.GetComponentInChildren<TMP_Text>(true);
        if (label != null)
        {
            label.fontSize = 27f;
            label.enableAutoSizing = true;
            label.fontSizeMin = 18f;
            label.fontSizeMax = 27f;
            label.alignment = TextAlignmentOptions.Center;
            label.raycastTarget = false;
            EditorUtility.SetDirty(label);
        }
    }

    private static void ConfigurePanel(Transform panel, int playerNumber)
    {
        RectTransform rect = panel.GetComponent<RectTransform>();
        ConfigureStretch(rect, Vector2.zero, Vector2.one);
        rect.offsetMin = new Vector2(10f, 10f);
        rect.offsetMax = new Vector2(-10f, -10f);

        Image image = panel.GetComponent<Image>();
        if (image != null)
        {
            Color accent = GetPlayerColor(playerNumber);
            image.color = new Color(accent.r * 0.12f, accent.g * 0.12f, accent.b * 0.12f, 0.96f);
            image.raycastTarget = true;
            EditorUtility.SetDirty(image);
        }
    }

    private static void ConfigureTopButton(RectTransform rect, Vector2 position)
    {
        rect.anchorMin = new Vector2(0f, 1f);
        rect.anchorMax = new Vector2(0f, 1f);
        rect.pivot = new Vector2(0f, 1f);
        rect.anchoredPosition = position;
        rect.sizeDelta = new Vector2(190f, 58f);
        rect.localScale = Vector3.one;
        rect.localRotation = Quaternion.identity;
    }

    private static void ConfigureCloseButton(RectTransform rect)
    {
        rect.anchorMin = Vector2.one;
        rect.anchorMax = Vector2.one;
        rect.pivot = Vector2.one;
        rect.anchoredPosition = new Vector2(-20f, -20f);
        rect.sizeDelta = new Vector2(120f, 58f);
        rect.localScale = Vector3.one;
        rect.localRotation = Quaternion.identity;
    }

    private static void ConfigureInfoPanel(RectTransform rect)
    {
        ConfigureStretch(rect, Vector2.zero, Vector2.one);
        rect.offsetMin = new Vector2(24f, 24f);
        rect.offsetMax = new Vector2(-24f, -100f);
    }

    private static void ConfigureContentText(TMP_Text text)
    {
        RectTransform rect = text.rectTransform;
        ConfigureStretch(rect, Vector2.zero, Vector2.one);
        rect.offsetMin = new Vector2(24f, 20f);
        rect.offsetMax = new Vector2(-24f, -20f);
        text.enableAutoSizing = true;
        text.fontSizeMin = 17f;
        text.fontSizeMax = 29f;
        text.alignment = TextAlignmentOptions.TopLeft;
        text.raycastTarget = false;
        EditorUtility.SetDirty(text);
    }

    private static Toggle ConfigureRadioToggle(Transform target, ToggleGroup group, bool isOn, Color color)
    {
        Toggle toggle = ConvertSelectableToToggle(target, group, isOn);
        Image image = target.GetComponent<Image>();
        if (image != null)
        {
            image.color = color;
            image.raycastTarget = true;
            ApplyEditorRadioSprite(toggle, isOn, image.sprite);
            EditorUtility.SetDirty(image);
        }
        return toggle;
    }

    private static Toggle ConvertSelectableToToggle(Transform target, ToggleGroup group, bool isOn)
    {
        Toggle existingToggle = target.GetComponent<Toggle>();
        if (existingToggle != null)
        {
            existingToggle.group = group;
            existingToggle.onValueChanged = new Toggle.ToggleEvent();
            existingToggle.SetIsOnWithoutNotify(isOn);
            EditorUtility.SetDirty(existingToggle);
            return existingToggle;
        }

        Button button = target.GetComponent<Button>();
        Navigation navigation = button != null ? button.navigation : Navigation.defaultNavigation;
        Selectable.Transition transition = button != null ? button.transition : Selectable.Transition.SpriteSwap;
        ColorBlock colors = button != null ? button.colors : ColorBlock.defaultColorBlock;
        SpriteState spriteState = button != null ? button.spriteState : new SpriteState();
        AnimationTriggers animationTriggers = button != null ? button.animationTriggers : new AnimationTriggers();
        Graphic targetGraphic = button != null ? button.targetGraphic : target.GetComponent<Image>();
        bool interactable = button == null || button.interactable;

        if (button != null)
            Undo.DestroyObjectImmediate(button);

        Toggle toggle = Undo.AddComponent<Toggle>(target.gameObject);
        toggle.navigation = navigation;
        toggle.transition = transition;
        toggle.colors = colors;
        toggle.spriteState = spriteState;
        toggle.animationTriggers = animationTriggers;
        toggle.interactable = interactable;
        toggle.targetGraphic = targetGraphic;
        toggle.toggleTransition = Toggle.ToggleTransition.Fade;
        toggle.graphic = null;
        toggle.group = group;
        toggle.onValueChanged = new Toggle.ToggleEvent();
        toggle.SetIsOnWithoutNotify(isOn);
        EditorUtility.SetDirty(toggle);
        return toggle;
    }

    private static void ApplyEditorRadioSprite(Toggle toggle, bool isOn, Sprite defaultSprite)
    {
        if (toggle == null)
            return;

        Image targetImage = toggle.targetGraphic as Image;
        Sprite selectedSprite = toggle.spriteState.selectedSprite;
        if (targetImage == null || selectedSprite == null)
            return;

        targetImage.overrideSprite = isOn ? selectedSprite : defaultSprite;
        EditorUtility.SetDirty(targetImage);
    }

    private static Button ConfigureButton(Transform target, Color color)
    {
        Button button = target.GetComponent<Button>();
        if (button == null)
            button = Undo.AddComponent<Button>(target.gameObject);

        Undo.RecordObject(button, "Configure tutorial button");
        button.onClick = new Button.ButtonClickedEvent();
        Image image = target.GetComponent<Image>();
        button.targetGraphic = image;
        button.transition = Selectable.Transition.ColorTint;
        if (image != null)
        {
            image.color = color;
            image.raycastTarget = true;
            EditorUtility.SetDirty(image);
        }
        EditorUtility.SetDirty(button);
        return button;
    }

    private static void SetText(Transform root, string value)
    {
        TMP_Text text = root.GetComponentInChildren<TMP_Text>(true);
        if (text == null)
            return;

        text.text = value;
        text.enableAutoSizing = true;
        text.fontSizeMin = 16f;
        text.fontSizeMax = 27f;
        text.alignment = TextAlignmentOptions.Center;
        text.raycastTarget = false;
        EditorUtility.SetDirty(text);
    }

    private static void FixPauseUi(Scene scene)
    {
        PauseManager pauseManager = FindComponentInScene<PauseManager>(scene);
        Canvas canvas = FindMainCanvas(scene);
        if (pauseManager == null || canvas == null)
            return;

        Transform pause = canvas.transform.Find("Pause");
        if (pause == null)
            return;

        RectTransform resume = GetRect(pause, "ResumeBtn");
        RectTransform restart = GetRect(pause, "RestartBtn");
        RectTransform menu = GetRect(pause, "MenuBtn");
        RectTransform exit = GetRect(pause, "ExitBtn");
        if (resume == null || restart == null || menu == null || exit == null)
            return;

        BindPauseButton(resume, pauseManager.OnResumeButton);
        BindPauseButton(restart, pauseManager.OnRestartButton);
        BindPauseButton(menu, pauseManager.OnMainMenuButton);
        BindPauseButton(exit, pauseManager.OnExitButton);

        Undo.RecordObject(pauseManager, "Repair Training pause references");
        pauseManager.ropeButtons = new[]
        {
            new PauseManager.RopeButton { button = resume, ropeLength = 250f },
            new PauseManager.RopeButton { button = restart, ropeLength = 300f },
            new PauseManager.RopeButton { button = menu, ropeLength = 350f },
            new PauseManager.RopeButton { button = exit, ropeLength = 400f }
        };
        EditorUtility.SetDirty(pauseManager);
    }

    private static void BindPauseButton(RectTransform rect, UnityEngine.Events.UnityAction action)
    {
        Button button = rect.GetComponent<Button>();
        if (button == null)
            return;

        Undo.RecordObject(button, "Repair Training pause callback");
        button.onClick = new Button.ButtonClickedEvent();
        UnityEventTools.AddPersistentListener(button.onClick, action);
        EditorUtility.SetDirty(button);
    }

    private static RectTransform GetRect(Transform parent, string childName)
    {
        Transform child = parent.Find(childName);
        return child != null ? child.GetComponent<RectTransform>() : null;
    }

    private static string BuildBasicsText(int playerNumber)
    {
        if (playerNumber == 1)
        {
            return
                "<color=#68A8FF><b>PLAYER 1  |  MOVEMENT & COMBAT</b></color>\n\n" +
                "<b>MOVEMENT</b>\nMove: <link=\"Move\">W A S D</link>\n" +
                "Run: <link=\"Run\">LEFT SHIFT</link>    Dash: <link=\"Dash\">R</link>    Roll: <link=\"Roll\">F</link>\n\n" +
                "<b>COMBAT</b>\nLight attack: <link=\"LightAttack\">SPACE</link>\n" +
                "Heavy attack: <link=\"HeavyAttack\">Q</link>\n" +
                "Block: <link=\"Block\">C</link>    Shoot / class skill: <link=\"Shoot\">J</link>\n\n" +
                "<color=#AEB8C4><link=\"Navigate\">A / D</link>: switch tab    |    TAB: close\n" +
                "Only Player 1 is locked while this panel is open.</color>";
        }

        return
            "<color=#FFB14A><b>PLAYER 2  |  MOVEMENT & COMBAT</b></color>\n\n" +
            "<b>MOVEMENT</b>\nMove: <link=\"Move\">↑ ← ↓ →</link>\n" +
            "Run: <link=\"Run\">RIGHT SHIFT</link>    Dash: <link=\"Dash\">2</link>    Roll: <link=\"Roll\">3</link>\n\n" +
            "<b>COMBAT</b>\nLight attack: <link=\"LightAttack\">0</link>\n" +
            "Heavy attack: <link=\"HeavyAttack\">1</link>\n" +
            "Block: <link=\"Block\">4</link>    Shoot / class skill: <link=\"Shoot\">5</link>\n\n" +
            "<color=#AEB8C4><link=\"Navigate\">← / →</link>: switch tab    |    +: close\n" +
            "Only Player 2 is locked while this panel is open.</color>";
    }

    private static string BuildCardsText(int playerNumber)
    {
        string color = playerNumber == 1 ? "68A8FF" : "FFB14A";
        string navigation = playerNumber == 1 ? "A / D" : "← / →";
        string close = playerNumber == 1 ? "TAB" : "+";
        return
            $"<color=#{color}><b>PLAYER {playerNumber}  |  POWER-UP CARDS</b></color>\n\n" +
            "Cards are offered after a completed wave. One choice affects the current run.\n\n" +
            "<b>SHARPER BLOWS</b> - +20% team melee damage\n" +
            "<b>SECOND WIND</b> - +25% max health and team heal\n" +
            "<b>MOONLIT BOOTS</b> - +15% team movement speed\n" +
            "<b>QUICK STEP</b> - 20% shorter dash cooldown\n" +
            "<b>BARRAGE RHYTHM</b> - 20% shorter barrage cooldown\n" +
            "<b>KINDLING</b> - repair the campfire by 60 HP\n" +
            "<b>STONE RING</b> - +45 campfire max HP and heal it\n" +
            "<b>ENGINEER OVERCLOCK</b> - faster, stronger Engineer charges\n" +
            "<b>HEAVY IMPACT</b> - stronger Heavy attacks and barrage\n" +
            "<b>EMERGENCY HEAL</b> - restore 45 HP to both players\n\n" +
            $"<color=#AEB8C4><link=\"Navigate\">{navigation}</link>: switch tab    |    {close}: close</color>";
    }

    private static Color GetPlayerColor(int playerNumber)
    {
        return playerNumber == 1
            ? new Color(0.25f, 0.58f, 1f, 0.96f)
            : new Color(1f, 0.58f, 0.18f, 0.96f);
    }

    private static void DisableAndRemoveLegacyManagers(Scene scene)
    {
        GameObject[] roots = scene.GetRootGameObjects();
        for (int i = 0; i < roots.Length; i++)
        {
            TutorialPanelManager[] managers = roots[i].GetComponentsInChildren<TutorialPanelManager>(true);
            for (int j = 0; j < managers.Length; j++)
                Undo.DestroyObjectImmediate(managers[j]);
        }
    }

    private static Canvas FindMainCanvas(Scene scene)
    {
        GameObject[] roots = scene.GetRootGameObjects();
        for (int i = 0; i < roots.Length; i++)
        {
            Canvas[] canvases = roots[i].GetComponentsInChildren<Canvas>(true);
            for (int j = 0; j < canvases.Length; j++)
            {
                if (canvases[j].name == "Canvas")
                    return canvases[j];
            }
        }
        return null;
    }

    private static T FindComponentInScene<T>(Scene scene) where T : Component
    {
        GameObject[] roots = scene.GetRootGameObjects();
        for (int i = 0; i < roots.Length; i++)
        {
            T component = roots[i].GetComponentInChildren<T>(true);
            if (component != null)
                return component;
        }
        return null;
    }

    private static GameObject FindRoot(Scene scene, string rootName)
    {
        GameObject[] roots = scene.GetRootGameObjects();
        for (int i = 0; i < roots.Length; i++)
        {
            if (roots[i].name == rootName)
                return roots[i];

            Transform nested = roots[i].transform.Find(rootName);
            if (nested != null)
                return nested.gameObject;
        }
        return null;
    }

    private static GameObject CreateUiObject(string name, Transform parent)
    {
        GameObject gameObject = new GameObject(name, typeof(RectTransform));
        Undo.RegisterCreatedObjectUndo(gameObject, "Create " + name);
        gameObject.transform.SetParent(parent, false);
        return gameObject;
    }

    private static void ConfigureStretch(RectTransform rect, Vector2 anchorMin, Vector2 anchorMax)
    {
        rect.anchorMin = anchorMin;
        rect.anchorMax = anchorMax;
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
        rect.localScale = Vector3.one;
        rect.localRotation = Quaternion.identity;
    }

    private sealed class BuiltView
    {
        public GameObject Hint;
        public GameObject Panel;
        public GameObject BasicsContent;
        public GameObject CardsContent;
        public Toggle BasicsToggle;
        public Toggle CardsToggle;
        public Button CloseButton;
    }
}
