using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public static class RuntimeFontSwitcher
{
    private const string ConfigResourcePath = "RuntimeFontSwitcherConfig";
    private static RuntimeFontSwitcherConfig config;
    private static RuntimeFontSwitcherRunner runner;

    public static bool IsReady => LoadConfig() != null;
    public static float RescanInterval => LoadConfig() != null ? config.RescanInterval : 0.25f;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStaticState()
    {
        config = null;
        runner = null;
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Install()
    {
        if (!IsReady)
            return;

        SceneManager.sceneLoaded -= OnSceneLoaded;
        SceneManager.sceneLoaded += OnSceneLoaded;

        EnsureRunner();
        ApplyNow();
    }

    public static void ApplyNow()
    {
        RuntimeFontSwitcherConfig loadedConfig = LoadConfig();
        if (loadedConfig == null)
            return;

        ApplyToTMPTexts(loadedConfig.TMPFontAsset);
        ApplyToLegacyTexts(loadedConfig.LegacyFont);
    }

    private static RuntimeFontSwitcherConfig LoadConfig()
    {
        if (config != null)
            return config;

        config = Resources.Load<RuntimeFontSwitcherConfig>(ConfigResourcePath);
        return config;
    }

    private static void EnsureRunner()
    {
        if (runner != null)
            return;

        GameObject runnerObject = new GameObject("[Runtime Font Switcher]");
        Object.DontDestroyOnLoad(runnerObject);
        runnerObject.hideFlags = HideFlags.HideAndDontSave;
        runner = runnerObject.AddComponent<RuntimeFontSwitcherRunner>();
    }

    private static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        ApplyNow();
    }

    private static void ApplyToTMPTexts(TMP_FontAsset fontAsset)
    {
        if (fontAsset == null)
            return;

        Material sharedMaterial = fontAsset.material;
        TMP_Text[] texts = Resources.FindObjectsOfTypeAll<TMP_Text>();
        for (int i = 0; i < texts.Length; i++)
        {
            TMP_Text text = texts[i];
            if (!ShouldApply(text))
                continue;

            bool changed = false;
            if (text.font != fontAsset)
            {
                text.font = fontAsset;
                changed = true;
            }

            if (sharedMaterial != null && text.fontSharedMaterial != sharedMaterial)
            {
                text.fontSharedMaterial = sharedMaterial;
                changed = true;
            }

            if (changed)
                text.SetAllDirty();
        }
    }

    private static void ApplyToLegacyTexts(Font font)
    {
        if (font == null)
            return;

        Text[] texts = Resources.FindObjectsOfTypeAll<Text>();
        for (int i = 0; i < texts.Length; i++)
        {
            Text text = texts[i];
            if (!ShouldApply(text) || text.font == font)
                continue;

            text.font = font;
            text.SetAllDirty();
        }
    }

    private static bool ShouldApply(Component component)
    {
        if (component == null)
            return false;

        GameObject gameObject = component.gameObject;
        if (gameObject == null || !gameObject.scene.IsValid())
            return false;

        const HideFlags skipFlags = HideFlags.NotEditable | HideFlags.HideAndDontSave;
        return (component.hideFlags & skipFlags) == 0 && (gameObject.hideFlags & skipFlags) == 0;
    }
}

public sealed class RuntimeFontSwitcherRunner : MonoBehaviour
{
    private float nextScanTime;

    private void OnEnable()
    {
        RuntimeFontSwitcher.ApplyNow();
    }

    private void LateUpdate()
    {
        if (!RuntimeFontSwitcher.IsReady || Time.unscaledTime < nextScanTime)
            return;

        RuntimeFontSwitcher.ApplyNow();
        nextScanTime = Time.unscaledTime + RuntimeFontSwitcher.RescanInterval;
    }
}
