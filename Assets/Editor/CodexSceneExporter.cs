using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class CodexSceneExporter
{
    private const int MaxSerializedPropertiesPerComponent = 220;

    private sealed class ReportStats
    {
        public int sceneCount;
        public int objectCount;
        public int componentCount;
        public int missingComponentCount;
    }

    [MenuItem("Tools/Codex/Export Scene Report", false, 1000)]
    public static void ExportOpenSceneReport()
    {
        List<Scene> scenes = new List<Scene>();
        for (int i = 0; i < EditorSceneManager.sceneCount; i++)
        {
            Scene scene = EditorSceneManager.GetSceneAt(i);
            if (scene.IsValid() && scene.isLoaded)
            {
                scenes.Add(scene);
            }
        }

        StringBuilder report = CreateReportHeader("Open Loaded Scenes");
        ReportStats stats = new ReportStats();

        for (int i = 0; i < scenes.Count; i++)
        {
            AppendScene(report, scenes[i], stats);
        }

        AppendSummary(report, stats);
        WriteReport(report, "OpenScenes");
    }

    [MenuItem("Tools/Codex/Export All Scenes Report", false, 1001)]
    public static void ExportAllScenesReport()
    {
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
        {
            return;
        }

        SceneSetup[] previousSetup = EditorSceneManager.GetSceneManagerSetup();
        StringBuilder report = CreateReportHeader("All Scenes In Assets");
        ReportStats stats = new ReportStats();

        string[] sceneGuids = AssetDatabase.FindAssets("t:Scene", new[] { "Assets" });
        Array.Sort(sceneGuids, StringComparer.OrdinalIgnoreCase);

        try
        {
            for (int i = 0; i < sceneGuids.Length; i++)
            {
                string scenePath = AssetDatabase.GUIDToAssetPath(sceneGuids[i]);
                if (string.IsNullOrEmpty(scenePath))
                {
                    continue;
                }

                try
                {
                    Scene scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
                    AppendScene(report, scene, stats);
                }
                catch (Exception exception)
                {
                    report.AppendLine("# Scene Load Error");
                    report.AppendLine("Path: " + scenePath);
                    report.AppendLine("Error: " + exception.Message);
                    report.AppendLine();
                }
            }
        }
        finally
        {
            EditorSceneManager.RestoreSceneManagerSetup(previousSetup);
        }

        AppendSummary(report, stats);
        WriteReport(report, "AllScenes");
    }

    [MenuItem("Tools/Codex/Open Scene Reports Folder", false, 1002)]
    public static void OpenReportsFolder()
    {
        string folder = GetReportsFolder();
        Directory.CreateDirectory(folder);
        EditorUtility.RevealInFinder(folder);
    }

    private static StringBuilder CreateReportHeader(string title)
    {
        StringBuilder report = new StringBuilder(1024 * 64);
        report.AppendLine("# Codex Scene Report");
        report.AppendLine("Mode: " + title);
        report.AppendLine("Generated: " + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
        report.AppendLine("Unity Project: " + Directory.GetParent(Application.dataPath).FullName);
        report.AppendLine();
        return report;
    }

    private static void AppendScene(StringBuilder report, Scene scene, ReportStats stats)
    {
        if (!scene.IsValid() || !scene.isLoaded)
        {
            return;
        }

        stats.sceneCount++;
        report.AppendLine("# Scene");
        report.AppendLine("Name: " + scene.name);
        report.AppendLine("Path: " + scene.path);
        report.AppendLine("IsDirty: " + scene.isDirty);
        report.AppendLine("RootCount: " + scene.rootCount);
        report.AppendLine();

        GameObject[] roots = scene.GetRootGameObjects();
        for (int i = 0; i < roots.Length; i++)
        {
            AppendGameObject(report, roots[i], 0, stats);
        }

        report.AppendLine();
    }

    private static void AppendGameObject(StringBuilder report, GameObject gameObject, int depth, ReportStats stats)
    {
        if (gameObject == null)
        {
            return;
        }

        stats.objectCount++;
        string indent = new string(' ', depth * 2);
        string layerName = LayerMask.LayerToName(gameObject.layer);
        if (string.IsNullOrEmpty(layerName))
        {
            layerName = "Layer " + gameObject.layer;
        }

        report.AppendLine(indent + "- GameObject: " + gameObject.name);
        report.AppendLine(indent + "  Path: " + GetHierarchyPath(gameObject));
        report.AppendLine(indent + "  ActiveSelf: " + gameObject.activeSelf + ", ActiveInHierarchy: " + gameObject.activeInHierarchy);
        report.AppendLine(indent + "  Tag: " + gameObject.tag + ", Layer: " + layerName + ", Static: " + gameObject.isStatic);
        AppendPrefabInfo(report, gameObject, indent + "  ");
        AppendTransform(report, gameObject.transform, indent + "  ");

        Component[] components = gameObject.GetComponents<Component>();
        report.AppendLine(indent + "  Components: " + components.Length);
        for (int i = 0; i < components.Length; i++)
        {
            AppendComponent(report, components[i], indent + "    ", stats);
        }

        for (int i = 0; i < gameObject.transform.childCount; i++)
        {
            AppendGameObject(report, gameObject.transform.GetChild(i).gameObject, depth + 1, stats);
        }
    }

    private static void AppendPrefabInfo(StringBuilder report, GameObject gameObject, string indent)
    {
        PrefabInstanceStatus status = PrefabUtility.GetPrefabInstanceStatus(gameObject);
        string prefabPath = PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(gameObject);

        if (status == PrefabInstanceStatus.NotAPrefab && string.IsNullOrEmpty(prefabPath))
        {
            return;
        }

        report.AppendLine(indent + "PrefabStatus: " + status);
        if (!string.IsNullOrEmpty(prefabPath))
        {
            report.AppendLine(indent + "PrefabAsset: " + prefabPath);
        }
    }

    private static void AppendTransform(StringBuilder report, Transform transform, string indent)
    {
        if (transform == null)
        {
            return;
        }

        report.AppendLine(indent + "LocalPosition: " + FormatVector3(transform.localPosition));
        report.AppendLine(indent + "LocalRotation: " + FormatVector3(transform.localEulerAngles));
        report.AppendLine(indent + "LocalScale: " + FormatVector3(transform.localScale));
    }

    private static void AppendComponent(StringBuilder report, Component component, string indent, ReportStats stats)
    {
        if (component == null)
        {
            stats.missingComponentCount++;
            report.AppendLine(indent + "Component: <Missing Script>");
            return;
        }

        stats.componentCount++;
        Type type = component.GetType();
        report.AppendLine(indent + "Component: " + type.FullName);

        Behaviour behaviour = component as Behaviour;
        if (behaviour != null)
        {
            report.AppendLine(indent + "  Enabled: " + behaviour.enabled);
        }

        Renderer renderer = component as Renderer;
        if (renderer != null)
        {
            report.AppendLine(indent + "  SortingLayer: " + renderer.sortingLayerName + ", SortingOrder: " + renderer.sortingOrder);
            report.AppendLine(indent + "  Visible: " + renderer.isVisible);
        }

        MonoBehaviour monoBehaviour = component as MonoBehaviour;
        if (monoBehaviour != null)
        {
            MonoScript script = MonoScript.FromMonoBehaviour(monoBehaviour);
            if (script != null)
            {
                report.AppendLine(indent + "  Script: " + AssetDatabase.GetAssetPath(script));
            }
        }

        AppendSerializedProperties(report, component, indent + "  ");
    }

    private static void AppendSerializedProperties(StringBuilder report, Component component, string indent)
    {
        SerializedObject serializedObject;
        try
        {
            serializedObject = new SerializedObject(component);
            serializedObject.UpdateIfRequiredOrScript();
        }
        catch (Exception exception)
        {
            report.AppendLine(indent + "SerializedFieldsError: " + exception.Message);
            return;
        }

        SerializedProperty property = serializedObject.GetIterator();
        bool enterChildren = true;
        int count = 0;

        while (property.NextVisible(enterChildren))
        {
            enterChildren = false;

            if (!ShouldPrintProperty(property))
            {
                continue;
            }

            report.AppendLine(indent + "Field: " + property.propertyPath + " = " + FormatPropertyValue(property));
            count++;

            if (count >= MaxSerializedPropertiesPerComponent)
            {
                report.AppendLine(indent + "Field: <truncated after " + MaxSerializedPropertiesPerComponent + " serialized fields>");
                break;
            }
        }
    }

    private static bool ShouldPrintProperty(SerializedProperty property)
    {
        if (property == null)
        {
            return false;
        }

        if (property.propertyPath.EndsWith(".Array.size", StringComparison.Ordinal))
        {
            return false;
        }

        switch (property.propertyType)
        {
            case SerializedPropertyType.Generic:
                return false;
            case SerializedPropertyType.AnimationCurve:
                return false;
            case SerializedPropertyType.Gradient:
                return false;
            default:
                return true;
        }
    }

    private static string FormatPropertyValue(SerializedProperty property)
    {
        switch (property.propertyType)
        {
            case SerializedPropertyType.Integer:
                return property.intValue.ToString();
            case SerializedPropertyType.Boolean:
                return property.boolValue.ToString();
            case SerializedPropertyType.Float:
                return property.floatValue.ToString("0.###");
            case SerializedPropertyType.String:
                return Quote(property.stringValue);
            case SerializedPropertyType.Color:
                return property.colorValue.ToString();
            case SerializedPropertyType.ObjectReference:
                return FormatUnityObject(property.objectReferenceValue);
            case SerializedPropertyType.LayerMask:
                return property.intValue.ToString();
            case SerializedPropertyType.Enum:
                return property.enumDisplayNames != null && property.enumValueIndex >= 0 && property.enumValueIndex < property.enumDisplayNames.Length
                    ? property.enumDisplayNames[property.enumValueIndex]
                    : property.enumValueIndex.ToString();
            case SerializedPropertyType.Vector2:
                return property.vector2Value.ToString();
            case SerializedPropertyType.Vector3:
                return property.vector3Value.ToString();
            case SerializedPropertyType.Vector4:
                return property.vector4Value.ToString();
            case SerializedPropertyType.Rect:
                return property.rectValue.ToString();
            case SerializedPropertyType.Bounds:
                return property.boundsValue.ToString();
            case SerializedPropertyType.Quaternion:
                return property.quaternionValue.eulerAngles.ToString();
            case SerializedPropertyType.Vector2Int:
                return property.vector2IntValue.ToString();
            case SerializedPropertyType.Vector3Int:
                return property.vector3IntValue.ToString();
            case SerializedPropertyType.RectInt:
                return property.rectIntValue.ToString();
            case SerializedPropertyType.BoundsInt:
                return property.boundsIntValue.ToString();
            case SerializedPropertyType.ManagedReference:
                return property.managedReferenceFullTypename;
            default:
                return "<" + property.propertyType + ">";
        }
    }

    private static string FormatUnityObject(UnityEngine.Object unityObject)
    {
        if (unityObject == null)
        {
            return "null";
        }

        string assetPath = AssetDatabase.GetAssetPath(unityObject);
        if (!string.IsNullOrEmpty(assetPath))
        {
            return unityObject.name + " (" + unityObject.GetType().Name + ") asset=" + assetPath;
        }

        GameObject gameObject = unityObject as GameObject;
        if (gameObject != null)
        {
            return GetHierarchyPath(gameObject) + " (GameObject)";
        }

        Component component = unityObject as Component;
        if (component != null)
        {
            return GetHierarchyPath(component.gameObject) + " [" + component.GetType().Name + "]";
        }

        return unityObject.name + " (" + unityObject.GetType().Name + ")";
    }

    private static string GetHierarchyPath(GameObject gameObject)
    {
        if (gameObject == null)
        {
            return "<null>";
        }

        Transform transform = gameObject.transform;
        string path = transform.name;
        while (transform.parent != null)
        {
            transform = transform.parent;
            path = transform.name + "/" + path;
        }

        return path;
    }

    private static string FormatVector3(Vector3 value)
    {
        return "(" + value.x.ToString("0.###") + ", " + value.y.ToString("0.###") + ", " + value.z.ToString("0.###") + ")";
    }

    private static string Quote(string value)
    {
        if (value == null)
        {
            return "null";
        }

        return "\"" + value.Replace("\"", "\\\"") + "\"";
    }

    private static void AppendSummary(StringBuilder report, ReportStats stats)
    {
        report.AppendLine("# Summary");
        report.AppendLine("Scenes: " + stats.sceneCount);
        report.AppendLine("GameObjects: " + stats.objectCount);
        report.AppendLine("Components: " + stats.componentCount);
        report.AppendLine("MissingScripts: " + stats.missingComponentCount);
        report.AppendLine();
    }

    private static void WriteReport(StringBuilder report, string mode)
    {
        string folder = GetReportsFolder();
        Directory.CreateDirectory(folder);

        string fileName = "CodexSceneReport_" + mode + "_" + DateTime.Now.ToString("yyyyMMdd_HHmmss") + ".md";
        string fullPath = Path.Combine(folder, fileName);
        File.WriteAllText(fullPath, report.ToString(), new UTF8Encoding(false));
        AssetDatabase.Refresh();

        string projectPath = AbsoluteToProjectPath(fullPath);
        Debug.Log("[Codex] Scene report exported: " + fullPath);
        EditorUtility.DisplayDialog("Codex Scene Report", "Exported:\n" + projectPath, "OK");
    }

    private static string GetReportsFolder()
    {
        return Path.Combine(Application.dataPath, "CodexReports");
    }

    private static string AbsoluteToProjectPath(string fullPath)
    {
        string normalizedFullPath = fullPath.Replace('\\', '/');
        string normalizedDataPath = Application.dataPath.Replace('\\', '/');
        if (normalizedFullPath.StartsWith(normalizedDataPath, StringComparison.OrdinalIgnoreCase))
        {
            return "Assets" + normalizedFullPath.Substring(normalizedDataPath.Length);
        }

        return fullPath;
    }
}
