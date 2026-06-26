using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class MainMenuController : MonoBehaviour
{
    private enum OptionsPage
    {
        Sound,
        Controls
    }

    [Header("Панели окон")]
    [SerializeField] private GameObject mainMenuPanel;
    [SerializeField] private GameObject optionsMenuPanel;
    [SerializeField] private GameObject exitConfirmationPanel;

    [Header("Элементы настроек")]
    [SerializeField] private Slider volumeSlider;
    [SerializeField] private Toggle muteToggle;
    [SerializeField] private Image muteToggleBackground;
    [SerializeField] private Sprite soundOnSprite;
    [SerializeField] private Sprite soundOffSprite;

    [Header("Страницы настроек")]
    [SerializeField] private GameObject soundSettingsPage;
    [SerializeField] private GameObject controlsSettingsPage;
    [SerializeField] private ControlsSettingsUI controlsSettingsUI;

    [Header("Radio / кнопки страниц")]
    [SerializeField] private Selectable soundSettingsRadioButton;
    [SerializeField] private Selectable controlsSettingsRadioButton;
    [SerializeField] private Button backButton;

    private OptionsPage currentOptionsPage = OptionsPage.Sound;
    private bool optionButtonsWired;
    private bool audioPrefsDirty;
    private bool soundOn = true;
    private readonly Dictionary<Image, Sprite> optionRadioDefaultSprites = new Dictionary<Image, Sprite>();

    private const string VolumeKey = "MasterVolume";
    private const string SoundKey = "SoundOn"; // 1 = Звук есть, 0 = Мьют

    private void Start()
    {
        float savedVolume = PlayerPrefs.GetFloat(VolumeKey, 1.0f);
        int savedSoundState = PlayerPrefs.GetInt(SoundKey, 1);
        soundOn = savedSoundState == 1;

        if (volumeSlider != null)
            volumeSlider.value = savedVolume;

        AudioListener.volume = savedVolume;
        ApplySoundState(soundOn, false);

        EnsureOptionsReferences();
        WireOptionButtons();
        SyncOptionRadioButtons();
        SyncMuteToggleVisual();
    }

    private void OnDisable()
    {
        SaveAudioPrefsIfDirty();
    }

    private void OnApplicationQuit()
    {
        SaveAudioPrefsIfDirty();
    }

    private void Update()
    {
        if (optionsMenuPanel != null && optionsMenuPanel.activeInHierarchy && Input.GetKeyDown(KeyCode.Escape))
            CloseOptions();
    }

    private void LateUpdate()
    {
        if (optionsMenuPanel != null && optionsMenuPanel.activeInHierarchy)
        {
            SyncOptionRadioButtons();
            SyncMuteToggleVisual();
        }
    }

    private void SaveAudioPrefsIfDirty()
    {
        if (!audioPrefsDirty)
            return;

        PlayerPrefs.Save();
        audioPrefsDirty = false;
    }

    public void OpenOptions()
    {
        if (mainMenuPanel != null)
            mainMenuPanel.SetActive(false);

        if (optionsMenuPanel != null)
            optionsMenuPanel.SetActive(true);

        EnsureOptionsReferences();
        WireOptionButtons();
        ShowSoundSettings();
    }

    public void CloseOptions()
    {
        SaveAudioPrefsIfDirty();

        if (optionsMenuPanel != null)
            optionsMenuPanel.SetActive(false);

        if (mainMenuPanel != null)
            mainMenuPanel.SetActive(true);
    }

    public void OpenExitConfirmation()
    {
        if (mainMenuPanel != null)
            mainMenuPanel.SetActive(false);

        if (exitConfirmationPanel != null)
            exitConfirmationPanel.SetActive(true);
    }

    public void CloseExitConfirmation()
    {
        if (exitConfirmationPanel != null)
            exitConfirmationPanel.SetActive(false);

        if (mainMenuPanel != null)
            mainMenuPanel.SetActive(true);
    }

    public void ConfirmExit()
    {
#if UNITY_EDITOR
        Debug.Log("Игра закрывается... (В билде это закроет приложение)");
#endif
        Application.Quit();
    }

    public void ChangeVolume(float value)
    {
        AudioListener.volume = value;
        PlayerPrefs.SetFloat(VolumeKey, value);
        audioPrefsDirty = true;
    }

    public void ToggleSound(bool soundOn)
    {
        ApplySoundState(soundOn, true);
    }

    private void ApplySoundState(bool enabled, bool save)
    {
        soundOn = enabled;
        AudioListener.pause = !soundOn;

        if (muteToggle != null)
            muteToggle.SetIsOnWithoutNotify(soundOn);

        SyncMuteToggleVisual();

        if (!save)
            return;

        PlayerPrefs.SetInt(SoundKey, soundOn ? 1 : 0);
        audioPrefsDirty = true;
        SaveAudioPrefsIfDirty();
    }

    private void SyncMuteToggleVisual()
    {
        if (muteToggleBackground == null && muteToggle != null)
            muteToggleBackground = muteToggle.targetGraphic as Image;

        if (muteToggleBackground == null)
            return;

        Sprite targetSprite = soundOn ? soundOnSprite : soundOffSprite;
        if (targetSprite != null)
            muteToggleBackground.overrideSprite = targetSprite;
    }

    public void ShowSoundSettings(bool isOn)
    {
        if (isOn)
            ShowSoundSettings();
        else
            SyncOptionRadioButtons();
    }

    public void ShowControlsSettings(bool isOn)
    {
        if (isOn)
            ShowControlsSettings();
        else
            SyncOptionRadioButtons();
    }

    public void ShowSoundSettings()
    {
        EnsureOptionsReferences();
        currentOptionsPage = OptionsPage.Sound;

        if (soundSettingsPage != null)
            soundSettingsPage.SetActive(true);

        if (controlsSettingsPage != null)
            controlsSettingsPage.SetActive(false);

        if (controlsSettingsUI != null)
            controlsSettingsUI.SetVisible(false);

        SyncOptionRadioButtons();
    }

    public void ShowControlsSettings()
    {
        EnsureOptionsReferences();
        currentOptionsPage = OptionsPage.Controls;

        if (soundSettingsPage != null)
            soundSettingsPage.SetActive(false);

        if (controlsSettingsPage != null)
            controlsSettingsPage.SetActive(true);

        if (controlsSettingsUI != null)
            controlsSettingsUI.Open();

        SyncOptionRadioButtons();
    }

    public void OpenControlsSettings()
    {
        ShowControlsSettings();
    }

    private void EnsureOptionsReferences()
    {
        if (optionsMenuPanel == null)
            return;

        if (soundSettingsPage == null)
            soundSettingsPage = FindSoundSettingsPage();

        if (controlsSettingsPage == null)
            controlsSettingsPage = FindDirectOrRecursiveChild("ControlSettings");

        if (controlsSettingsPage == null)
            controlsSettingsPage = FindDirectOrRecursiveChild("ControlsSettings");

        if (controlsSettingsUI == null && controlsSettingsPage != null)
            controlsSettingsUI = controlsSettingsPage.GetComponent<ControlsSettingsUI>();

        if (controlsSettingsUI == null && controlsSettingsPage != null)
            controlsSettingsUI = controlsSettingsPage.AddComponent<ControlsSettingsUI>();

        if (soundSettingsRadioButton == null)
            soundSettingsRadioButton = FindSideSelectable("Audio");

        if (controlsSettingsRadioButton == null)
            controlsSettingsRadioButton = FindSideSelectable("Controls");

        if (backButton == null)
        {
            Transform found = FindChildRecursive(optionsMenuPanel.transform, "BackButton");
            if (found != null)
                backButton = found.GetComponent<Button>();
        }
    }

    private GameObject FindSoundSettingsPage()
    {
        if (volumeSlider != null && volumeSlider.transform.parent != null)
            return volumeSlider.transform.parent.gameObject;

        GameObject audioPage = FindPageWithChild("VolumeSlider");
        if (audioPage != null)
            return audioPage;

        return FindDirectOrRecursiveChild("Audio");
    }

    private GameObject FindPageWithChild(string childName)
    {
        Transform child = FindChildRecursive(optionsMenuPanel.transform, childName);
        if (child == null || child.parent == null)
            return null;

        return child.parent.gameObject;
    }

    private Selectable FindSideSelectable(string childName)
    {
        Transform side = FindChildRecursive(optionsMenuPanel.transform, "SideBTN");
        Transform target = side != null ? FindChildRecursive(side, childName) : FindChildRecursive(optionsMenuPanel.transform, childName);
        if (target == null)
            return null;

        Selectable selectable = target.GetComponent<Selectable>();
        if (selectable == null)
            selectable = target.GetComponentInChildren<Selectable>(true);
        return selectable;
    }

    private GameObject FindDirectOrRecursiveChild(string childName)
    {
        Transform child = FindChildRecursive(optionsMenuPanel.transform, childName);
        return child != null ? child.gameObject : null;
    }

    private Transform FindChildRecursive(Transform parent, string childName)
    {
        if (parent == null)
            return null;

        for (int i = 0; i < parent.childCount; i++)
        {
            Transform child = parent.GetChild(i);
            if (child.name == childName)
                return child;

            Transform nested = FindChildRecursive(child, childName);
            if (nested != null)
                return nested;
        }

        return null;
    }

    private void WireOptionButtons()
    {
        if (optionButtonsWired)
            return;

        optionButtonsWired = true;
        WireSelectable(soundSettingsRadioButton, ShowSoundSettings);
        WireSelectable(controlsSettingsRadioButton, ShowControlsSettings);

        if (backButton != null)
            backButton.onClick.AddListener(CloseOptions);
    }

    private void WireSelectable(Selectable selectable, UnityEngine.Events.UnityAction action)
    {
        if (selectable == null)
            return;

        Toggle toggle = selectable as Toggle;
        if (toggle != null)
        {
            toggle.onValueChanged.AddListener(isOn =>
            {
                if (isOn)
                    action.Invoke();
                else
                    SyncOptionRadioButtons();
            });
            return;
        }

        Button button = selectable as Button;
        if (button != null)
            button.onClick.AddListener(action);
    }

    private void SyncOptionRadioButtons()
    {
        SetToggleState(soundSettingsRadioButton, currentOptionsPage == OptionsPage.Sound);
        SetToggleState(controlsSettingsRadioButton, currentOptionsPage == OptionsPage.Controls);
    }

    private void SetToggleState(Selectable selectable, bool isOn)
    {
        Toggle toggle = selectable as Toggle;
        if (toggle == null)
            return;

        toggle.SetIsOnWithoutNotify(isOn);
        ApplyToggleSpriteState(toggle, isOn);
    }

    private void ApplyToggleSpriteState(Toggle toggle, bool isOn)
    {
        Image targetImage = toggle.targetGraphic as Image;
        Sprite selectedSprite = toggle.spriteState.selectedSprite;

        if (targetImage == null || selectedSprite == null)
            return;

        if (!optionRadioDefaultSprites.ContainsKey(targetImage))
            optionRadioDefaultSprites[targetImage] = targetImage.sprite;

        targetImage.overrideSprite = isOn ? selectedSprite : optionRadioDefaultSprites[targetImage];
    }

}


