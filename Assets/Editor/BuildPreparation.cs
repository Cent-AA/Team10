using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class BuildPreparation
{
    private static readonly string[] TargetScenes =
    {
        "Assets/Scenes/MainMenu.unity",
        "Assets/Scenes/CharacterSelect.unity",
        "Assets/Scenes/TestArena_PrototypeMVP.unity",
        "Assets/Scenes/TrainingScene.unity"
    };

    [MenuItem("Tools/Build/Prepare Target Build")]
    public static void PrepareTargetBuild()
    {
        if (!Application.isBatchMode && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            return;

        SceneSetup[] previousSetup = EditorSceneManager.GetSceneManagerSetup();

        try
        {
            EditorBuildSettings.scenes = TargetScenes
                .Select(path => new EditorBuildSettingsScene(path, true))
                .ToArray();

            ValidateTargetBuildInternal();
            AssetDatabase.SaveAssets();
            Debug.Log("Build preparation complete. Four target scenes are enabled and validated.");
        }
        finally
        {
            if (!Application.isBatchMode && previousSetup.Length > 0)
                EditorSceneManager.RestoreSceneManagerSetup(previousSetup);
        }
    }

    [MenuItem("Tools/Build/Validate Target Build")]
    public static void ValidateTargetBuild()
    {
        if (!Application.isBatchMode && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            return;

        SceneSetup[] previousSetup = EditorSceneManager.GetSceneManagerSetup();

        try
        {
            ValidateTargetBuildInternal();
            Debug.Log("Build validation passed for all four target scenes.");
        }
        finally
        {
            if (!Application.isBatchMode && previousSetup.Length > 0)
                EditorSceneManager.RestoreSceneManagerSetup(previousSetup);
        }
    }

    public static void PrepareTargetBuildBatch()
    {
        PrepareTargetBuild();
    }

    private static void ValidateTargetBuildInternal()
    {
        var errors = new List<string>();
        string[] enabledScenes = EditorBuildSettings.scenes
            .Where(scene => scene.enabled)
            .Select(scene => scene.path)
            .ToArray();

        if (!enabledScenes.SequenceEqual(TargetScenes))
            errors.Add("Build Settings must contain exactly the four target scenes in the expected order.");

        foreach (string scenePath in TargetScenes)
        {
            if (AssetDatabase.LoadAssetAtPath<SceneAsset>(scenePath) == null)
            {
                errors.Add($"Scene is missing or cannot be imported: {scenePath}");
                continue;
            }

            Scene scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
            int missingScripts = scene.GetRootGameObjects()
                .SelectMany(root => root.GetComponentsInChildren<Transform>(true))
                .Sum(transform => GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(transform.gameObject));

            if (missingScripts > 0)
                errors.Add($"{scenePath} contains {missingScripts} missing script reference(s).");

            foreach (string dependency in AssetDatabase.GetDependencies(scenePath, true))
            {
                if (AssetDatabase.LoadMainAssetAtPath(dependency) == null)
                    errors.Add($"{scenePath} has an unavailable dependency: {dependency}");
            }
        }

        if (!BuildPipeline.IsBuildTargetSupported(BuildTargetGroup.Standalone, BuildTarget.StandaloneWindows64))
            errors.Add("Windows 64-bit build support is not installed for this Unity editor.");

        if (errors.Count > 0)
            throw new BuildFailedException(string.Join(Environment.NewLine, errors.Distinct()));
    }
}
