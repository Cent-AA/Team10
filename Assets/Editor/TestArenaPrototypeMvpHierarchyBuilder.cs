#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public static class TestArenaPrototypeMvpHierarchyBuilder
{
    private const string ScenePath = "Assets/Scenes/TestArena_PrototypeMVP.unity";
    private const string AutoBuildSessionKey = "TestArenaPrototypeMvpHierarchyBuilder.AutoBuild.20260625";
    private const string ZombiePrefabPath = "Assets/Prefabs/Zombie.prefab";
    private const string BossPrefabPath = "Assets/Prefabs/ZombieBoss.prefab";
    private const string HeavyPrefabPath = "Assets/Prefabs/Player1 1.prefab";
    private const string EngineerPrefabPath = "Assets/Prefabs/Engineer.prefab";
    private const string TurretPrefabPath = "Assets/Prefabs/PrototypeTurret.prefab";
    private const string DispenserPrefabPath = "Assets/Prefabs/PrototypeDispenser.prefab";
    private const string ReviveTargetPrefabPath = "Assets/Prefabs/ReviveTarget.prefab";

    [InitializeOnLoadMethod]
    private static void ScheduleAutoBuildForOpenTestArena()
    {
        if (Application.isBatchMode)
            return;

        EditorApplication.delayCall += () =>
        {
            if (SessionState.GetBool(AutoBuildSessionKey, false))
                return;

            if (EditorApplication.isCompiling || EditorApplication.isPlayingOrWillChangePlaymode)
                return;

            Scene activeScene = EditorSceneManager.GetActiveScene();
            if (!activeScene.IsValid() || activeScene.path != ScenePath)
                return;

            SessionState.SetBool(AutoBuildSessionKey, true);
            BuildAndSave();
        };
    }

    [MenuItem("Codex/Test Arena/Rebuild Prototype MVP Hierarchy")]
    public static void BuildAndSaveFromMenu()
    {
        if (BuildAndSave())
            Debug.Log("TestArena_PrototypeMVP hierarchy rebuilt and saved.");
    }

    public static void BuildAndSaveFromCommandLine()
    {
        bool success = false;
        try
        {
            success = BuildAndSave();
        }
        catch (Exception exception)
        {
            Debug.LogException(exception);
        }

        if (Application.isBatchMode)
            EditorApplication.Exit(success ? 0 : 1);
    }

    private static bool BuildAndSave()
    {
        Scene scene = EditorSceneManager.GetActiveScene();
        if (!scene.IsValid() || scene.path != ScenePath)
            scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);

        BuildEditablePrefabs();
        BuildScene(scene);

        EditorSceneManager.MarkSceneDirty(scene);
        bool saved = EditorSceneManager.SaveScene(scene);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        return saved;
    }

    private static void BuildScene(Scene scene)
    {
        GameObject arena = EnsureRoot(scene, "Arena");
        GameObject gameplay = EnsureRoot(scene, "Gameplay");
        GameObject cameras = EnsureRoot(scene, "Cameras");
        GameObject ui = EnsureRoot(scene, "UI");

        BuildArena(scene, arena);
        BuildGameplay(scene, gameplay, cameras);
        BuildCameras(scene, cameras);
        BuildUi(scene, ui);
        WireSceneReferences(scene);
    }

    private static void BuildArena(Scene scene, GameObject arena)
    {
        GameObject map = Find(scene, "ArenaFoet_0");
        ParentIfFound(map, arena.transform, true);
        SpriteRenderer mapRenderer = map != null ? EnsureComponent<SpriteRenderer>(map) : null;
        if (mapRenderer != null)
            mapRenderer.sprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Sprites/ArenaFoet.jpg");

        GameObject bounds = Find(scene, "ArenaBounds");
        if (bounds == null)
        {
            bounds = FindAll(scene, "Walls").FirstOrDefault(HasCardinalBoundsChildren);
            if (bounds == null)
                bounds = FindAll(scene, "Walls").FirstOrDefault(go => go.GetComponent<PolygonCollider2D>() == null);
            if (bounds != null)
                bounds.name = "ArenaBounds";
        }

        ParentIfFound(bounds, arena.transform, true);

        foreach (GameObject legacyBounds in FindAll(scene, "Walls").ToList())
        {
            if (legacyBounds == bounds)
                continue;

            legacyBounds.name = "ArenaBounds_PolygonCollider";
            legacyBounds.SetActive(false);
            ParentIfFound(legacyBounds, bounds != null ? bounds.transform : arena.transform, true);
        }

        GameObject campfire = Find(scene, "CampFire") ?? CreateSceneObject(scene, "CampFire");
        ParentIfFound(campfire, arena.transform, true);
        ConfigureCampfire(campfire);
    }

    private static void ConfigureCampfire(GameObject campfire)
    {
        SpriteRenderer renderer = EnsureComponent<SpriteRenderer>(campfire);
        Sprite campfireSprite = LoadSprite("Assets/Sprites/FIRECAMP.png", "FIRECAMP_0");
        if (campfireSprite != null)
            renderer.sprite = campfireSprite;

        Animator animator = EnsureComponent<Animator>(campfire);
        RuntimeAnimatorController controller =
            AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>("Assets/Sprites/CampFire.controller");
        if (controller != null)
            animator.runtimeAnimatorController = controller;

        if (campfire.GetComponent<Collider2D>() == null)
        {
            CircleCollider2D collider = campfire.AddComponent<CircleCollider2D>();
            collider.radius = 0.85f;
        }

        foreach (Component component in campfire.GetComponents<Component>())
        {
            if (component != null && component.GetType().Name == "CampfireController")
                UnityEngine.Object.DestroyImmediate(component);
        }
    }

    private static void BuildGameplay(Scene scene, GameObject gameplay, GameObject cameras)
    {
        GameObject spawnPoints = Find(scene, "SpawnPoints") ?? CreateSceneObject(scene, "SpawnPoints");
        ParentIfFound(spawnPoints, gameplay.transform, true);

        GameObject playerSpawns = EnsureChild(scene, gameplay.transform, "PlayerSpawns", false);
        ParentIfFound(Find(scene, "SpawnPoint1"), playerSpawns.transform, true);
        ParentIfFound(Find(scene, "SpawnPoint2"), playerSpawns.transform, true);

        GameObject waveManager = Find(scene, "WaveManager") ?? CreateSceneObject(scene, "WaveManager");
        ParentIfFound(waveManager, gameplay.transform, true);
        EnsureComponent<WaveManager>(waveManager);

        GameObject characterSpawner = Find(scene, "CharacterSpawner") ?? CreateSceneObject(scene, "CharacterSpawner");
        ParentIfFound(characterSpawner, gameplay.transform, true);
        EnsureComponent<CharacterSpawner>(characterSpawner);

        GameObject mechanics = Find(scene, "PrototypeArenaMechanics") ?? CreateSceneObject(scene, "PrototypeArenaMechanics");
        ParentIfFound(mechanics, gameplay.transform, true);
        EnsureComponent<PrototypeRunStats>(mechanics);
        EnsureComponent<PrototypeCampfireHealth>(mechanics);
        EnsureComponent<PrototypeCardRewardManager>(mechanics);
        EnsureComponent<PrototypeEnemyVariantManager>(mechanics);
        EnsureComponent<PrototypeClassRoleTuner>(mechanics);
        EnsureComponent<PrototypeReviveManager>(mechanics);

        GameObject debugSetup = Find(scene, "DebugSetup") ?? CreateSceneObject(scene, "DebugSetup");
        ParentIfFound(debugSetup, gameplay.transform, true);
        EnsureComponent<DebugInputSetup>(debugSetup);

        GameObject transitionManager = Find(scene, "TransitionManager") ?? CreateSceneObject(scene, "TransitionManager");
        ParentIfFound(transitionManager, gameplay.transform, true);
        EnsureComponent<ArenaEntryTransition>(transitionManager);

        ParentIfFound(Find(scene, "PauseManager"), gameplay.transform, true);
        ParentIfFound(Find(scene, "CardsTestArena"), gameplay.transform, true);

        GameObject bossIntro = Find(scene, "BossIntro") ?? CreateSceneObject(scene, "BossIntro");
        ParentIfFound(bossIntro, gameplay.transform, true);
        EnsureComponent<BossIntroSequence>(bossIntro);

        cameras.transform.SetSiblingIndex(2);
    }

    private static void BuildCameras(Scene scene, GameObject cameras)
    {
        GameObject main = EnsureChild(scene, cameras.transform, "Main Camera", true);
        Camera mainCamera = EnsureComponent<Camera>(main);
        EnsureComponent<AudioListener>(main);
        EnsureComponent<UniversalAdditionalCameraData>(main);
        ArenaCamera arenaCamera = EnsureComponent<ArenaCamera>(main);

        GameObject left = EnsureChild(scene, cameras.transform, "LeftSplit", true);
        Camera leftCamera = EnsureComponent<Camera>(left);
        EnsureComponent<UniversalAdditionalCameraData>(left);
        leftCamera.enabled = false;

        GameObject right = EnsureChild(scene, cameras.transform, "RightSplit", true);
        Camera rightCamera = EnsureComponent<Camera>(right);
        EnsureComponent<UniversalAdditionalCameraData>(right);
        rightCamera.enabled = false;

        GameObject screenDivider = EnsureRectChild(scene, cameras.transform, "ScreenDivider");
        Canvas dividerCanvas = EnsureComponent<Canvas>(screenDivider);
        dividerCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
        dividerCanvas.sortingOrder = 1000;
        EnsureComponent<CanvasScaler>(screenDivider);

        RectTransform lineRect = EnsureRectChild(screenDivider.transform, "DivideLine");
        lineRect.anchorMin = new Vector2(0.5f, 0f);
        lineRect.anchorMax = new Vector2(0.5f, 1f);
        lineRect.pivot = new Vector2(0.5f, 0.5f);
        lineRect.anchoredPosition = Vector2.zero;
        lineRect.sizeDelta = new Vector2(3f, 0f);
        Image dividerImage = EnsureComponent<Image>(lineRect.gameObject);
        dividerImage.color = Color.black;
        dividerImage.raycastTarget = false;
        lineRect.gameObject.SetActive(false);

        arenaCamera.leftSplitCamera = leftCamera;
        arenaCamera.rightSplitCamera = rightCamera;
        arenaCamera.createSplitCamerasAutomatically = false;
        arenaCamera.dividerImage = dividerImage;
        arenaCamera.createDividerAutomatically = false;
        arenaCamera.shakeIntensityLevel = Mathf.Clamp(arenaCamera.shakeIntensityLevel, 1f, 10f);

        SpriteRenderer mapSprite = Find(scene, "ArenaFoet_0")?.GetComponent<SpriteRenderer>();
        if (mapSprite != null)
            arenaCamera.mapSprite = mapSprite;
    }

    private static void BuildUi(Scene scene, GameObject ui)
    {
        GameObject canvasObject = Find(scene, "Canvas") ?? CreateRectSceneObject(scene, "Canvas");
        ParentIfFound(canvasObject, ui.transform, true);
        Canvas canvas = EnsureComponent<Canvas>(canvasObject);
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        EnsureComponent<CanvasScaler>(canvasObject);
        EnsureComponent<GraphicRaycaster>(canvasObject);

        GameObject eventSystem = Find(scene, "EventSystem") ?? CreateSceneObject(scene, "EventSystem");
        ParentIfFound(eventSystem, ui.transform, true);
        EnsureComponent<EventSystem>(eventSystem);
        if (eventSystem.GetComponent<StandaloneInputModule>() == null)
            eventSystem.AddComponent<StandaloneInputModule>();

        GameObject level = Find(scene, "Level");
        if (level != null)
            level.transform.SetParent(canvasObject.transform, false);

        RectTransform heavyShockPanel = EnsureRectChild(canvasObject.transform, "HeavyShockPanel");
        heavyShockPanel.anchorMin = new Vector2(0.5f, 0.5f);
        heavyShockPanel.anchorMax = new Vector2(0.5f, 0.5f);
        heavyShockPanel.pivot = new Vector2(0.5f, 0.5f);
        heavyShockPanel.anchoredPosition = Vector2.zero;
        heavyShockPanel.sizeDelta = Vector2.zero;
        heavyShockPanel.gameObject.SetActive(false);

        GameObject hud = Find(scene, "PrototypeArenaHUD") ?? CreateRectSceneObject(scene, "PrototypeArenaHUD");
        hud.transform.SetParent(canvasObject.transform, false);
        RectTransform hudRect = EnsureRectTransform(hud);
        hudRect.anchorMin = Vector2.zero;
        hudRect.anchorMax = Vector2.one;
        hudRect.anchoredPosition = Vector2.zero;
        hudRect.sizeDelta = Vector2.zero;

        Canvas hudCanvas = EnsureComponent<Canvas>(hud);
        hudCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
        hudCanvas.overrideSorting = true;
        hudCanvas.sortingOrder = 5500;
        CanvasScaler scaler = EnsureComponent<CanvasScaler>(hud);
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;
        EnsureComponent<GraphicRaycaster>(hud);

        BuildPrototypeHud(hud.transform);
    }

    private static void BuildPrototypeHud(Transform hud)
    {
        Image campfireFrame = EnsurePanel(
            hud,
            "CampfireHealthFrame",
            new Color(0.03f, 0.025f, 0.02f, 0.72f),
            new Vector2(0.5f, 1f),
            new Vector2(0.5f, 1f),
            new Vector2(0f, -34f),
            new Vector2(520f, 54f));

        Image fill = EnsurePanel(
            campfireFrame.transform,
            "Fill",
            new Color(1f, 0.45f, 0.08f, 0.95f),
            Vector2.zero,
            Vector2.one,
            Vector2.zero,
            new Vector2(-10f, -10f));
        fill.type = Image.Type.Filled;
        fill.fillMethod = Image.FillMethod.Horizontal;

        EnsureText(
            campfireFrame.transform,
            "Label",
            "Campfire",
            16,
            TextAlignmentOptions.Center,
            Vector2.zero,
            Vector2.one,
            Vector2.zero,
            Vector2.zero);

        EnsureText(
            hud,
            "RunStatsText",
            "Wave 0\nKills 0\nCards 0",
            24,
            TextAlignmentOptions.TopRight,
            new Vector2(1f, 1f),
            new Vector2(1f, 1f),
            new Vector2(-24f, -24f),
            new Vector2(420f, 130f));

        Image rewardPanel = EnsurePanel(
            hud,
            "RewardPanel",
            new Color(0.025f, 0.03f, 0.035f, 0.94f),
            new Vector2(0.5f, 0.5f),
            new Vector2(0.5f, 0.5f),
            Vector2.zero,
            new Vector2(940f, 560f));
        BuildRewardPanel(rewardPanel.transform);
        rewardPanel.gameObject.SetActive(false);

        Image resultPanel = EnsurePanel(
            hud,
            "ResultPanel",
            new Color(0.03f, 0.025f, 0.02f, 0.92f),
            new Vector2(0.5f, 0.5f),
            new Vector2(0.5f, 0.5f),
            Vector2.zero,
            new Vector2(760f, 520f));
        BuildResultPanel(resultPanel.transform);
        resultPanel.gameObject.SetActive(false);

        EnsureText(
            hud,
            "RolesText",
            "P1 Heavy: tank, block, heavy barrage",
            20,
            TextAlignmentOptions.TopLeft,
            new Vector2(0f, 1f),
            new Vector2(0f, 1f),
            new Vector2(24f, -24f),
            new Vector2(540f, 110f));

        EnsureText(
            hud,
            "EngineerBuildHint",
            "",
            18,
            TextAlignmentOptions.BottomLeft,
            Vector2.zero,
            Vector2.zero,
            new Vector2(24f, 74f),
            new Vector2(620f, 48f));

        TextMeshProUGUI reviveTemplate = EnsureText(
            hud,
            "ReviveProgressTemplate",
            "REVIVE\n0 / 0",
            24,
            TextAlignmentOptions.Center,
            new Vector2(0.5f, 0.5f),
            new Vector2(0.5f, 0.5f),
            Vector2.zero,
            new Vector2(240f, 90f));
        reviveTemplate.gameObject.SetActive(false);
    }

    private static void BuildRewardPanel(Transform panel)
    {
        EnsureText(
            panel,
            "Title",
            "Wave cleared\nPick one upgrade",
            24,
            TextAlignmentOptions.Center,
            new Vector2(0.5f, 1f),
            new Vector2(0.5f, 1f),
            new Vector2(0f, -80f),
            new Vector2(780f, 110f));

        for (int i = 0; i < 3; i++)
        {
            EnsureButton(
                panel,
                "CardButton" + (i + 1),
                "Card " + (i + 1),
                new Vector2(0.5f, 0.5f),
                new Vector2(0.5f, 0.5f),
                new Vector2(-300f + i * 300f, -40f),
                new Vector2(260f, 270f));
        }
    }

    private static void BuildResultPanel(Transform panel)
    {
        EnsureText(
            panel,
            "Title",
            "RUN RESULT",
            20,
            TextAlignmentOptions.Center,
            new Vector2(0.5f, 1f),
            new Vector2(0.5f, 1f),
            new Vector2(0f, -58f),
            new Vector2(620f, 70f));

        EnsureText(
            panel,
            "Stats",
            "",
            16,
            TextAlignmentOptions.Top,
            Vector2.zero,
            Vector2.one,
            new Vector2(0f, 16f),
            new Vector2(-80f, -190f));

        EnsureButton(
            panel,
            "RestartButton",
            "Restart",
            new Vector2(0.5f, 0f),
            new Vector2(0.5f, 0f),
            new Vector2(-140f, 70f),
            new Vector2(220f, 64f));

        EnsureButton(
            panel,
            "MenuButton",
            "Menu",
            new Vector2(0.5f, 0f),
            new Vector2(0.5f, 0f),
            new Vector2(140f, 70f),
            new Vector2(220f, 64f));
    }

    private static void WireSceneReferences(Scene scene)
    {
        Transform campfire = Find(scene, "CampFire")?.transform;
        Transform spawnPointsRoot = Find(scene, "SpawnPoints")?.transform;
        TextMeshProUGUI waveText = Find(scene, "WaveText")?.GetComponent<TextMeshProUGUI>();
        TextMeshProUGUI zombieCount = Find(scene, "ZombieCount")?.GetComponent<TextMeshProUGUI>();
        ArenaCamera arenaCamera = Find(scene, "Main Camera")?.GetComponent<ArenaCamera>();
        Camera leftSplit = Find(scene, "LeftSplit")?.GetComponent<Camera>();
        Camera rightSplit = Find(scene, "RightSplit")?.GetComponent<Camera>();

        WaveManager waveManager = Find(scene, "WaveManager")?.GetComponent<WaveManager>();
        if (waveManager != null)
        {
            waveManager.zombiePrefab = AssetDatabase.LoadAssetAtPath<GameObject>(ZombiePrefabPath);
            waveManager.spawnPoints = new[]
            {
                FindDirectChild(spawnPointsRoot, "Point1"),
                FindDirectChild(spawnPointsRoot, "Point2"),
                FindDirectChild(spawnPointsRoot, "Point3"),
                FindDirectChild(spawnPointsRoot, "Point4"),
                FindDirectChild(spawnPointsRoot, "Point5")
            };
            waveManager.campfireTarget = campfire;
            waveManager.waveText = waveText;
            waveManager.zombieCountText = zombieCount;
            waveManager.bossPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(BossPrefabPath);
            waveManager.bossIntroSequence = Find(scene, "BossIntro")?.GetComponent<BossIntroSequence>();
            waveManager.bossSpawnPoint = FindDirectChild(spawnPointsRoot, "bossSpawnPoint");
        }

        CharacterSpawner characterSpawner = Find(scene, "CharacterSpawner")?.GetComponent<CharacterSpawner>();
        if (characterSpawner != null)
        {
            GameObject heavy = AssetDatabase.LoadAssetAtPath<GameObject>(HeavyPrefabPath);
            GameObject engineer = AssetDatabase.LoadAssetAtPath<GameObject>(EngineerPrefabPath);
            characterSpawner.heavyPrefab = heavy;
            characterSpawner.engineerPrefab = engineer;
            characterSpawner.medicPrefab = heavy;
            characterSpawner.sniperPrefab = heavy;
            characterSpawner.spawnPoint1 = Find(scene, "SpawnPoint1")?.transform;
            characterSpawner.spawnPoint2 = Find(scene, "SpawnPoint2")?.transform;
            characterSpawner.arenaCamera = arenaCamera;
            characterSpawner.splitCamP1 = leftSplit;
            characterSpawner.splitCamP2 = rightSplit;
        }

        PrototypeCampfireHealth health = Find(scene, "PrototypeArenaMechanics")?.GetComponent<PrototypeCampfireHealth>();
        if (health != null)
            health.campfire = campfire;

        BossIntroSequence bossIntro = Find(scene, "BossIntro")?.GetComponent<BossIntroSequence>();
        if (bossIntro != null)
        {
            GameObject bossPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(BossPrefabPath);
            bossIntro.boss = bossPrefab != null ? bossPrefab.GetComponent<BossController>() : null;
            bossIntro.uiCanvas = Find(scene, "Canvas")?.GetComponent<Canvas>();
            bossIntro.heavyShockSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Sprites/HeavyShock.PNG");
            bossIntro.heavyShockPanel = Find(scene, "HeavyShockPanel")?.GetComponent<RectTransform>();
        }
    }

    private static void BuildEditablePrefabs()
    {
        GameObject turretPrefab = EnsureEditablePrefab(
            TurretPrefabPath,
            "PrototypeTurret",
            ConfigureTurretPrefab);

        GameObject dispenserPrefab = EnsureEditablePrefab(
            DispenserPrefabPath,
            "PrototypeDispenser",
            ConfigureDispenserPrefab);

        EnsureEditablePrefab(
            ReviveTargetPrefabPath,
            "ReviveTarget",
            ConfigureReviveTargetPrefab);

        EnsureZombieVariant();
        AssignEngineerBuilderPrefabs(turretPrefab, dispenserPrefab);
        EnsurePlayerReviveTargets();
    }

    private static GameObject EnsureEditablePrefab(string path, string rootName, Action<GameObject> configure)
    {
        bool prefabExists = AssetDatabase.LoadAssetAtPath<GameObject>(path) != null;
        GameObject root = prefabExists ? PrefabUtility.LoadPrefabContents(path) : new GameObject(rootName);
        root.name = rootName;
        configure(root);

        PrefabUtility.SaveAsPrefabAsset(root, path);

        if (prefabExists)
            PrefabUtility.UnloadPrefabContents(root);
        else
            UnityEngine.Object.DestroyImmediate(root);

        return AssetDatabase.LoadAssetAtPath<GameObject>(path);
    }

    private static void ConfigureTurretPrefab(GameObject root)
    {
        EnsureComponent<PrototypeTurret>(root);
        Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Sprites/9slice.png");

        GameObject baseObject = EnsureChild(root.transform, "Base", false).gameObject;
        SpriteRenderer baseRenderer = EnsureComponent<SpriteRenderer>(baseObject);
        baseRenderer.sprite = sprite;
        baseRenderer.color = new Color(0.22f, 0.32f, 0.36f, 1f);
        baseObject.transform.localScale = new Vector3(0.7f, 0.5f, 1f);

        GameObject barrelObject = EnsureChild(root.transform, "Barrel", false).gameObject;
        SpriteRenderer barrelRenderer = EnsureComponent<SpriteRenderer>(barrelObject);
        barrelRenderer.sprite = sprite;
        barrelRenderer.color = new Color(0.55f, 0.9f, 1f, 1f);
        barrelObject.transform.localPosition = new Vector3(0.25f, 0.12f, 0f);
        barrelObject.transform.localScale = new Vector3(0.8f, 0.18f, 1f);

        GameObject tracerObject = EnsureChild(root.transform, "TurretTracer", false).gameObject;
        LineRenderer tracer = EnsureComponent<LineRenderer>(tracerObject);
        tracer.positionCount = 2;
        tracer.startWidth = 0.055f;
        tracer.endWidth = 0.015f;
        tracerObject.SetActive(false);
    }

    private static void ConfigureDispenserPrefab(GameObject root)
    {
        EnsureComponent<PrototypeDispenser>(root);
        Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Sprites/9slice.png");

        GameObject auraObject = EnsureChild(root.transform, "HealAura", false).gameObject;
        SpriteRenderer aura = EnsureComponent<SpriteRenderer>(auraObject);
        aura.sprite = sprite;
        aura.color = new Color(0.1f, 0.85f, 0.45f, 0.18f);
        auraObject.transform.localScale = Vector3.one * 6.4f;

        GameObject coreObject = EnsureChild(root.transform, "Core", false).gameObject;
        SpriteRenderer core = EnsureComponent<SpriteRenderer>(coreObject);
        core.sprite = sprite;
        core.color = new Color(0.18f, 0.65f, 0.42f, 1f);
        coreObject.transform.localScale = new Vector3(0.7f, 0.8f, 1f);
    }

    private static void ConfigureReviveTargetPrefab(GameObject root)
    {
        EnsureComponent<PrototypeReviveTarget>(root);
        GameObject label = EnsureChild(root.transform, "ReviveProgressTemplate", false).gameObject;
        TextMeshPro text = EnsureComponent<TextMeshPro>(label);
        text.text = "REVIVE\n0 / 0";
        text.fontSize = 3.2f;
        text.alignment = TextAlignmentOptions.Center;
        text.color = new Color(0.5f, 0.95f, 1f, 1f);
        label.transform.localPosition = new Vector3(0f, 1.65f, 0f);
        label.SetActive(false);
    }

    private static void EnsureZombieVariant()
    {
        GameObject zombiePrefab = AssetDatabase.LoadAssetAtPath<GameObject>(ZombiePrefabPath);
        if (zombiePrefab == null)
            return;

        GameObject root = PrefabUtility.LoadPrefabContents(ZombiePrefabPath);
        EnsureComponent<PrototypeEnemyVariant>(root);
        PrefabUtility.SaveAsPrefabAsset(root, ZombiePrefabPath);
        PrefabUtility.UnloadPrefabContents(root);
    }

    private static void AssignEngineerBuilderPrefabs(GameObject turretPrefab, GameObject dispenserPrefab)
    {
        GameObject engineerPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(EngineerPrefabPath);
        if (engineerPrefab == null)
            return;

        GameObject root = PrefabUtility.LoadPrefabContents(EngineerPrefabPath);
        PrototypeEngineerBuilder builder = EnsureComponent<PrototypeEngineerBuilder>(root);
        builder.turretPrefab = turretPrefab;
        builder.dispenserPrefab = dispenserPrefab;
        PrefabUtility.SaveAsPrefabAsset(root, EngineerPrefabPath);
        PrefabUtility.UnloadPrefabContents(root);
    }

    private static void EnsurePlayerReviveTargets()
    {
        EnsurePlayerReviveTarget(HeavyPrefabPath);
        EnsurePlayerReviveTarget(EngineerPrefabPath);
    }

    private static void EnsurePlayerReviveTarget(string prefabPath)
    {
        if (AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath) == null)
            return;

        GameObject root = PrefabUtility.LoadPrefabContents(prefabPath);
        PrototypeReviveTarget reviveTarget = EnsureComponent<PrototypeReviveTarget>(root);
        Transform template = EnsureChild(root.transform, "ReviveProgressTemplate", false);
        TextMeshPro text = EnsureComponent<TextMeshPro>(template.gameObject);
        text.text = "REVIVE\n0 / 0";
        text.fontSize = 3.2f;
        text.alignment = TextAlignmentOptions.Center;
        text.color = new Color(0.5f, 0.95f, 1f, 1f);
        template.localPosition = new Vector3(0f, 1.65f, 0f);
        template.gameObject.SetActive(false);
        reviveTarget.reviveProgressTemplate = template;
        PrefabUtility.SaveAsPrefabAsset(root, prefabPath);
        PrefabUtility.UnloadPrefabContents(root);
    }

    private static GameObject EnsureRoot(Scene scene, string name)
    {
        GameObject root = scene.GetRootGameObjects().FirstOrDefault(go => go.name == name);
        if (root == null)
            root = CreateSceneObject(scene, name);

        root.transform.SetParent(null);
        root.transform.localPosition = Vector3.zero;
        root.transform.localRotation = Quaternion.identity;
        root.transform.localScale = Vector3.one;
        return root;
    }

    private static GameObject EnsureChild(Scene scene, Transform parent, string name, bool preserveWorld)
    {
        Transform directChild = parent.Find(name);
        GameObject child = directChild != null ? directChild.gameObject : Find(scene, name);
        if (child == null)
            child = CreateSceneObject(scene, name);

        child.transform.SetParent(parent, preserveWorld);
        if (!preserveWorld)
        {
            child.transform.localPosition = Vector3.zero;
            child.transform.localRotation = Quaternion.identity;
            child.transform.localScale = Vector3.one;
        }

        return child;
    }

    private static Transform EnsureChild(Transform parent, string name, bool preserveWorld)
    {
        Transform child = parent.Find(name);
        if (child == null)
        {
            GameObject childObject = new GameObject(name);
            child = childObject.transform;
        }

        child.SetParent(parent, preserveWorld);
        if (!preserveWorld)
        {
            child.localPosition = Vector3.zero;
            child.localRotation = Quaternion.identity;
            child.localScale = Vector3.one;
        }

        return child;
    }

    private static GameObject EnsureRectChild(Scene scene, Transform parent, string name)
    {
        Transform directChild = parent.Find(name);
        GameObject child = directChild != null ? directChild.gameObject : Find(scene, name);
        if (child == null)
            child = CreateRectSceneObject(scene, name);

        child.transform.SetParent(parent, false);
        EnsureRectTransform(child);
        return child;
    }

    private static RectTransform EnsureRectChild(Transform parent, string name)
    {
        Transform directChild = parent.Find(name);
        GameObject child = directChild != null ? directChild.gameObject : new GameObject(name, typeof(RectTransform));
        child.transform.SetParent(parent, false);
        return EnsureRectTransform(child);
    }

    private static Image EnsurePanel(
        Transform parent,
        string name,
        Color color,
        Vector2 anchorMin,
        Vector2 anchorMax,
        Vector2 anchoredPosition,
        Vector2 sizeDelta)
    {
        RectTransform rect = EnsureRectChild(parent, name);
        rect.anchorMin = anchorMin;
        rect.anchorMax = anchorMax;
        rect.anchoredPosition = anchoredPosition;
        rect.sizeDelta = sizeDelta;

        Image image = EnsureComponent<Image>(rect.gameObject);
        image.color = color;
        return image;
    }

    private static TextMeshProUGUI EnsureText(
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
        RectTransform rect = EnsureRectChild(parent, name);
        rect.anchorMin = anchorMin;
        rect.anchorMax = anchorMax;
        rect.anchoredPosition = anchoredPosition;
        rect.sizeDelta = sizeDelta;

        TextMeshProUGUI label = EnsureComponent<TextMeshProUGUI>(rect.gameObject);
        label.text = text;
        label.fontSize = fontSize;
        label.color = Color.white;
        label.alignment = alignment;
        label.enableWordWrapping = true;
        return label;
    }

    private static Button EnsureButton(
        Transform parent,
        string name,
        string label,
        Vector2 anchorMin,
        Vector2 anchorMax,
        Vector2 anchoredPosition,
        Vector2 sizeDelta)
    {
        Image image = EnsurePanel(
            parent,
            name,
            new Color(0.12f, 0.14f, 0.16f, 0.96f),
            anchorMin,
            anchorMax,
            anchoredPosition,
            sizeDelta);

        Button button = EnsureComponent<Button>(image.gameObject);
        button.targetGraphic = image;

        EnsureText(
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

    private static RectTransform EnsureRectTransform(GameObject gameObject)
    {
        RectTransform rect = gameObject.GetComponent<RectTransform>();
        if (rect == null)
            rect = gameObject.AddComponent<RectTransform>();
        return rect;
    }

    private static T EnsureComponent<T>(GameObject gameObject) where T : Component
    {
        T component = gameObject.GetComponent<T>();
        if (component == null)
            component = gameObject.AddComponent<T>();
        return component;
    }

    private static GameObject CreateSceneObject(Scene scene, string name)
    {
        GameObject gameObject = new GameObject(name);
        SceneManager.MoveGameObjectToScene(gameObject, scene);
        return gameObject;
    }

    private static GameObject CreateRectSceneObject(Scene scene, string name)
    {
        GameObject gameObject = new GameObject(name, typeof(RectTransform));
        SceneManager.MoveGameObjectToScene(gameObject, scene);
        return gameObject;
    }

    private static void ParentIfFound(GameObject child, Transform parent, bool preserveWorld)
    {
        if (child == null || parent == null)
            return;

        child.transform.SetParent(parent, preserveWorld);
    }

    private static GameObject Find(Scene scene, string name)
    {
        return GetAllSceneObjects(scene).FirstOrDefault(go => go.name == name);
    }

    private static IEnumerable<GameObject> FindAll(Scene scene, string name)
    {
        return GetAllSceneObjects(scene).Where(go => go.name == name);
    }

    private static List<GameObject> GetAllSceneObjects(Scene scene)
    {
        List<GameObject> objects = new List<GameObject>();
        foreach (GameObject root in scene.GetRootGameObjects())
            AddRecursive(root.transform, objects);

        return objects;
    }

    private static void AddRecursive(Transform transform, List<GameObject> objects)
    {
        objects.Add(transform.gameObject);
        for (int i = 0; i < transform.childCount; i++)
            AddRecursive(transform.GetChild(i), objects);
    }

    private static bool HasCardinalBoundsChildren(GameObject gameObject)
    {
        if (gameObject == null)
            return false;

        return gameObject.transform.Find("LEFT") != null
            && gameObject.transform.Find("RIGHT") != null
            && gameObject.transform.Find("UP") != null
            && gameObject.transform.Find("DOWN") != null;
    }

    private static Transform FindDirectChild(Transform parent, string name)
    {
        return parent != null ? parent.Find(name) : null;
    }

    private static Sprite LoadSprite(string path, string preferredName)
    {
        Sprite directSprite = AssetDatabase.LoadAssetAtPath<Sprite>(path);
        if (directSprite != null && (string.IsNullOrEmpty(preferredName) || directSprite.name == preferredName))
            return directSprite;

        return AssetDatabase.LoadAllAssetsAtPath(path)
            .OfType<Sprite>()
            .FirstOrDefault(sprite => sprite.name == preferredName)
            ?? directSprite;
    }
}
#endif
