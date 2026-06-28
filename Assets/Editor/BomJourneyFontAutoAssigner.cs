using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

[InitializeOnLoad]
public static class BomJourneyFontAutoAssigner
{
    private const string TMPFontName = "bom Journey SDF";
    private const string LegacyFontName = "bom Journey";
    private const string PreferredTMPFontPath = "Assets/Font/bom Journey SDF.asset";
    private const string ConfigPath = "Assets/Resources/RuntimeFontSwitcherConfig.asset";
    private const string SessionScanKey = "Team10.BomJourneyFontAutoAssigner.InitialScan.v1";

    private static readonly HashSet<string> PendingAssetPaths = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

    private static bool applyScheduled;
    private static bool fullScanPending;
    private static bool manualRunPending;
    private static bool isApplying;

    internal static bool IsApplying => isApplying;

    static BomJourneyFontAutoAssigner()
    {
        if (AssetDatabase.IsAssetImportWorkerProcess())
            return;

        if (!SessionState.GetBool(SessionScanKey, false))
        {
            SessionState.SetBool(SessionScanKey, true);
            RequestFullScan(false);
        }
    }

    [MenuItem("Tools/Fonts/Reassign bom Journey SDF Across Project")]
    private static void ReassignFromMenu()
    {
        RequestFullScan(true);
    }

    internal static void NotifyAssetsChanged(
        string[] importedAssets,
        string[] deletedAssets,
        string[] movedAssets,
        string[] movedFromAssetPaths)
    {
        if (isApplying || AssetDatabase.IsAssetImportWorkerProcess())
            return;

        bool fontChanged = ContainsTargetFontPath(importedAssets)
            || ContainsTargetFontPath(deletedAssets)
            || ContainsTargetFontPath(movedAssets)
            || ContainsTargetFontPath(movedFromAssetPaths);

        if (fontChanged)
        {
            RequestFullScan(false);
            return;
        }

        AddEditableAssetPaths(importedAssets);
        AddEditableAssetPaths(movedAssets);
        if (PendingAssetPaths.Count > 0)
            ScheduleApply();
    }

    private static void RequestFullScan(bool manual)
    {
        fullScanPending = true;
        manualRunPending |= manual;
        PendingAssetPaths.Clear();
        ScheduleApply();
    }

    private static void ScheduleApply()
    {
        if (applyScheduled)
            return;

        applyScheduled = true;
        EditorApplication.delayCall += ApplyPendingChanges;
    }

    private static void ApplyPendingChanges()
    {
        applyScheduled = false;

        if (isApplying)
            return;

        if (EditorApplication.isCompiling || EditorApplication.isUpdating)
        {
            ScheduleApply();
            return;
        }

        if (EditorApplication.isPlayingOrWillChangePlaymode)
        {
            EditorApplication.playModeStateChanged -= OnPlayModeStateChanged;
            EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
            return;
        }

        TMP_FontAsset tmpFont = FindTMPFontAsset();
        if (tmpFont == null)
        {
            if (manualRunPending || fullScanPending)
                Debug.LogWarning($"[Font Auto Assigner] TMP font asset named '{TMPFontName}' was not found under Assets.");

            manualRunPending = false;
            fullScanPending = false;
            PendingAssetPaths.Clear();
            return;
        }

        Font legacyFont = FindLegacyFont();
        bool runFullScan = fullScanPending;
        bool wasManualRun = manualRunPending;
        string[] paths = runFullScan ? GetAllEditableAssetPaths() : PendingAssetPaths.ToArray();

        fullScanPending = false;
        manualRunPending = false;
        PendingAssetPaths.Clear();

        AssignmentReport report = new AssignmentReport();
        isApplying = true;
        try
        {
            UpdateRuntimeConfig(tmpFont, legacyFont, report);
            UpdateTMPSettings(tmpFont, report);

            foreach (string path in paths.Where(IsPrefabPath))
                UpdatePrefab(path, tmpFont, legacyFont, report);

            foreach (string path in paths.Where(IsScenePath))
                UpdateScene(path, tmpFont, legacyFont, report);

            AssetDatabase.SaveAssets();
        }
        finally
        {
            isApplying = false;
        }

        if (report.Errors.Count > 0)
        {
            Debug.LogWarning(
                $"[Font Auto Assigner] Finished with {report.Errors.Count} error(s):\n" +
                string.Join("\n", report.Errors));
        }

        if (wasManualRun || report.HasChanges)
        {
            string dirtySceneNote = report.DirtyLoadedScenes.Count == 0
                ? string.Empty
                : $" Loaded scenes left dirty for manual save: {string.Join(", ", report.DirtyLoadedScenes)}.";

            Debug.Log(
                $"[Font Auto Assigner] '{TMPFontName}' assigned. " +
                $"TMP texts: {report.TMPTextsChanged}, legacy texts: {report.LegacyTextsChanged}, " +
                $"prefabs saved: {report.PrefabsSaved}, scenes saved: {report.ScenesSaved}, " +
                $"settings updated: {report.SettingsChanged}.{dirtySceneNote}");
        }
    }

    private static void OnPlayModeStateChanged(PlayModeStateChange state)
    {
        if (state != PlayModeStateChange.EnteredEditMode)
            return;

        EditorApplication.playModeStateChanged -= OnPlayModeStateChanged;
        ScheduleApply();
    }

    private static TMP_FontAsset FindTMPFontAsset()
    {
        List<TMP_FontAsset> matches = AssetDatabase.FindAssets("t:TMP_FontAsset", new[] { "Assets" })
            .Select(AssetDatabase.GUIDToAssetPath)
            .Select(AssetDatabase.LoadAssetAtPath<TMP_FontAsset>)
            .Where(font => font != null && string.Equals(font.name, TMPFontName, StringComparison.Ordinal))
            .OrderBy(font => AssetDatabase.GetAssetPath(font), StringComparer.OrdinalIgnoreCase)
            .ToList();

        if (matches.Count == 0)
            return null;

        TMP_FontAsset preferred = matches.FirstOrDefault(font =>
            string.Equals(AssetDatabase.GetAssetPath(font), PreferredTMPFontPath, StringComparison.OrdinalIgnoreCase));

        TMP_FontAsset selected = preferred != null ? preferred : matches[0];
        if (matches.Count > 1)
        {
            Debug.LogWarning(
                $"[Font Auto Assigner] Found {matches.Count} TMP assets named '{TMPFontName}'. " +
                $"Using '{AssetDatabase.GetAssetPath(selected)}'. Keep this name unique to avoid ambiguity.");
        }

        return selected;
    }

    private static Font FindLegacyFont()
    {
        return AssetDatabase.FindAssets("t:Font", new[] { "Assets" })
            .Select(AssetDatabase.GUIDToAssetPath)
            .Select(AssetDatabase.LoadAssetAtPath<Font>)
            .Where(font => font != null && string.Equals(font.name, LegacyFontName, StringComparison.Ordinal))
            .OrderBy(font => AssetDatabase.GetAssetPath(font), StringComparer.OrdinalIgnoreCase)
            .FirstOrDefault();
    }

    private static void UpdateRuntimeConfig(
        TMP_FontAsset tmpFont,
        Font legacyFont,
        AssignmentReport report)
    {
        RuntimeFontSwitcherConfig config = AssetDatabase.LoadAssetAtPath<RuntimeFontSwitcherConfig>(ConfigPath);
        if (config == null)
        {
            config = ScriptableObject.CreateInstance<RuntimeFontSwitcherConfig>();
            AssetDatabase.CreateAsset(config, ConfigPath);
            report.SettingsChanged++;
        }

        SerializedObject serializedConfig = new SerializedObject(config);
        SerializedProperty tmpFontProperty = serializedConfig.FindProperty("tmpFontAsset");
        SerializedProperty legacyFontProperty = serializedConfig.FindProperty("legacyFont");
        bool changed = false;

        if (tmpFontProperty != null && tmpFontProperty.objectReferenceValue != tmpFont)
        {
            tmpFontProperty.objectReferenceValue = tmpFont;
            changed = true;
        }

        if (legacyFont != null && legacyFontProperty != null && legacyFontProperty.objectReferenceValue != legacyFont)
        {
            legacyFontProperty.objectReferenceValue = legacyFont;
            changed = true;
        }

        if (changed)
        {
            serializedConfig.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(config);
            report.SettingsChanged++;
        }
    }

    private static void UpdateTMPSettings(TMP_FontAsset tmpFont, AssignmentReport report)
    {
        TMP_Settings settings = TMP_Settings.instance;
        if (settings == null)
            return;

        SerializedObject serializedSettings = new SerializedObject(settings);
        SerializedProperty defaultFontProperty = serializedSettings.FindProperty("m_defaultFontAsset");
        if (defaultFontProperty == null || defaultFontProperty.objectReferenceValue == tmpFont)
            return;

        defaultFontProperty.objectReferenceValue = tmpFont;
        serializedSettings.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(settings);
        report.SettingsChanged++;
    }

    private static void UpdatePrefab(
        string path,
        TMP_FontAsset tmpFont,
        Font legacyFont,
        AssignmentReport report)
    {
        GameObject root = null;
        try
        {
            root = PrefabUtility.LoadPrefabContents(path);
            int changed = ApplyToHierarchy(root, tmpFont, legacyFont, report);
            if (changed == 0)
                return;

            PrefabUtility.SaveAsPrefabAsset(root, path);
            report.PrefabsSaved++;
        }
        catch (Exception exception)
        {
            report.Errors.Add($"Prefab '{path}': {exception.Message}");
        }
        finally
        {
            if (root != null)
                PrefabUtility.UnloadPrefabContents(root);
        }
    }

    private static void UpdateScene(
        string path,
        TMP_FontAsset tmpFont,
        Font legacyFont,
        AssignmentReport report)
    {
        Scene scene = SceneManager.GetSceneByPath(path);
        bool wasAlreadyLoaded = scene.IsValid() && scene.isLoaded;
        bool wasDirty = wasAlreadyLoaded && scene.isDirty;

        try
        {
            if (!wasAlreadyLoaded)
                scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Additive);

            int changed = ApplyToScene(scene, tmpFont, legacyFont, report);
            if (changed == 0)
                return;

            EditorSceneManager.MarkSceneDirty(scene);
            if (wasAlreadyLoaded && wasDirty)
            {
                report.DirtyLoadedScenes.Add(path);
                return;
            }

            if (EditorSceneManager.SaveScene(scene))
                report.ScenesSaved++;
        }
        catch (Exception exception)
        {
            report.Errors.Add($"Scene '{path}': {exception.Message}");
        }
        finally
        {
            if (!wasAlreadyLoaded && scene.IsValid() && scene.isLoaded)
                EditorSceneManager.CloseScene(scene, true);
        }
    }

    private static int ApplyToScene(
        Scene scene,
        TMP_FontAsset tmpFont,
        Font legacyFont,
        AssignmentReport report)
    {
        int changed = 0;
        foreach (GameObject root in scene.GetRootGameObjects())
            changed += ApplyToHierarchy(root, tmpFont, legacyFont, report);

        return changed;
    }

    private static int ApplyToHierarchy(
        GameObject root,
        TMP_FontAsset tmpFont,
        Font legacyFont,
        AssignmentReport report)
    {
        int changed = 0;
        Material defaultMaterial = tmpFont.material;

        foreach (TMP_Text text in root.GetComponentsInChildren<TMP_Text>(true))
        {
            bool fontChanged = text.font != tmpFont;
            bool materialMissing = text.fontSharedMaterial == null && defaultMaterial != null;
            if (!fontChanged && !materialMissing)
                continue;

            text.font = tmpFont;
            if ((fontChanged || materialMissing) && defaultMaterial != null)
                text.fontSharedMaterial = defaultMaterial;

            text.SetAllDirty();
            EditorUtility.SetDirty(text);
            report.TMPTextsChanged++;
            changed++;
        }

        if (legacyFont == null)
            return changed;

        foreach (Text text in root.GetComponentsInChildren<Text>(true))
        {
            if (text.font == legacyFont)
                continue;

            text.font = legacyFont;
            text.SetAllDirty();
            EditorUtility.SetDirty(text);
            report.LegacyTextsChanged++;
            changed++;
        }

        return changed;
    }

    private static string[] GetAllEditableAssetPaths()
    {
        IEnumerable<string> prefabPaths = AssetDatabase.FindAssets("t:Prefab", new[] { "Assets" })
            .Select(AssetDatabase.GUIDToAssetPath)
            .Where(IsPrefabPath);

        IEnumerable<string> scenePaths = AssetDatabase.FindAssets("t:Scene", new[] { "Assets" })
            .Select(AssetDatabase.GUIDToAssetPath)
            .Where(IsScenePath);

        return prefabPaths.Concat(scenePaths)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(path => path, StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }

    private static void AddEditableAssetPaths(IEnumerable<string> paths)
    {
        if (paths == null)
            return;

        foreach (string path in paths)
        {
            if (IsPrefabPath(path) || IsScenePath(path))
                PendingAssetPaths.Add(path);
        }
    }

    private static bool ContainsTargetFontPath(IEnumerable<string> paths)
    {
        if (paths == null)
            return false;

        foreach (string path in paths)
        {
            string assetName = Path.GetFileNameWithoutExtension(path);
            if (string.Equals(assetName, TMPFontName, StringComparison.OrdinalIgnoreCase)
                || string.Equals(assetName, LegacyFontName, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        return false;
    }

    private static bool IsPrefabPath(string path)
    {
        return !string.IsNullOrEmpty(path)
            && path.StartsWith("Assets/", StringComparison.OrdinalIgnoreCase)
            && path.EndsWith(".prefab", StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsScenePath(string path)
    {
        return !string.IsNullOrEmpty(path)
            && path.StartsWith("Assets/", StringComparison.OrdinalIgnoreCase)
            && path.EndsWith(".unity", StringComparison.OrdinalIgnoreCase);
    }

    private sealed class AssignmentReport
    {
        public int TMPTextsChanged;
        public int LegacyTextsChanged;
        public int PrefabsSaved;
        public int ScenesSaved;
        public int SettingsChanged;
        public readonly List<string> DirtyLoadedScenes = new List<string>();
        public readonly List<string> Errors = new List<string>();

        public bool HasChanges => TMPTextsChanged > 0
            || LegacyTextsChanged > 0
            || PrefabsSaved > 0
            || ScenesSaved > 0
            || SettingsChanged > 0;
    }
}

public sealed class BomJourneyFontAssetPostprocessor : AssetPostprocessor
{
    private static void OnPostprocessAllAssets(
        string[] importedAssets,
        string[] deletedAssets,
        string[] movedAssets,
        string[] movedFromAssetPaths)
    {
        if (BomJourneyFontAutoAssigner.IsApplying)
            return;

        BomJourneyFontAutoAssigner.NotifyAssetsChanged(
            importedAssets,
            deletedAssets,
            movedAssets,
            movedFromAssetPaths);
    }
}
