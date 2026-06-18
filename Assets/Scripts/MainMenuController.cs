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
    [SerializeField] private Color inactiveRadioColor = new Color(0.18f, 0.18f, 0.2f, 1f);
    [SerializeField] private Color activeRadioColor = new Color(0.55f, 0.37f, 0.16f, 1f);

    private Sprite defaultMuteToggleSprite;
    private OptionsPage currentOptionsPage = OptionsPage.Sound;
    private bool optionButtonsWired;
    private bool audioPrefsDirty;

    private const string VolumeKey = "MasterVolume";
    private const string SoundKey = "SoundOn"; // 1 = Звук есть, 0 = Мьют

    private void Start()
    {
        float savedVolume = PlayerPrefs.GetFloat(VolumeKey, 1.0f);
        int savedSoundState = PlayerPrefs.GetInt(SoundKey, 1);
        bool isSoundOn = savedSoundState == 1;

        CacheDefaultMuteToggleSprite();

        if (volumeSlider != null)
            volumeSlider.value = savedVolume;

        if (muteToggle != null)
            muteToggle.isOn = isSoundOn;

        UpdateMuteToggleBackground(isSoundOn);

        AudioListener.volume = savedVolume;
        AudioListener.pause = !isSoundOn;
    }

    private void OnDisable()
    {
        SaveAudioPrefsIfDirty();
    }

    private void OnApplicationQuit()
    {
        SaveAudioPrefsIfDirty();
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
        AudioListener.pause = !soundOn;
        UpdateMuteToggleBackground(soundOn);

        PlayerPrefs.SetInt(SoundKey, soundOn ? 1 : 0);
        audioPrefsDirty = true;
        SaveAudioPrefsIfDirty();
    }

    public void ShowSoundSettings(bool isOn)
    {
        if (isOn)
            ShowSoundSettings();
    }

    public void ShowControlsSettings(bool isOn)
    {
        if (isOn)
            ShowControlsSettings();
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

        RefreshOptionButtonVisuals();
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

        RefreshOptionButtonVisuals();
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
                    EnforceOneOptionSelected();
            });
            return;
        }

        Button button = selectable as Button;
        if (button != null)
            button.onClick.AddListener(action);
    }

    private void EnforceOneOptionSelected()
    {
        Toggle soundToggle = soundSettingsRadioButton as Toggle;
        Toggle controlsToggle = controlsSettingsRadioButton as Toggle;
        if (soundToggle == null || controlsToggle == null)
            return;

        if (!soundToggle.isOn && !controlsToggle.isOn)
        {
            if (currentOptionsPage == OptionsPage.Controls)
                controlsToggle.SetIsOnWithoutNotify(true);
            else
                soundToggle.SetIsOnWithoutNotify(true);
        }

        RefreshOptionButtonVisuals();
    }

    private void RefreshOptionButtonVisuals()
    {
        SetSelectableState(soundSettingsRadioButton, currentOptionsPage == OptionsPage.Sound);
        SetSelectableState(controlsSettingsRadioButton, currentOptionsPage == OptionsPage.Controls);
    }

    private void SetSelectableState(Selectable selectable, bool active)
    {
        if (selectable == null)
            return;

        Toggle toggle = selectable as Toggle;
        if (toggle != null)
            toggle.SetIsOnWithoutNotify(active);

        Graphic graphic = selectable.targetGraphic;
        if (graphic == null)
            graphic = selectable.GetComponent<Graphic>();

        if (graphic != null)
            graphic.color = active ? activeRadioColor : inactiveRadioColor;
    }

    private void CacheDefaultMuteToggleSprite()
    {
        Image background = GetMuteToggleBackground();
        if (background != null && defaultMuteToggleSprite == null)
            defaultMuteToggleSprite = background.sprite;
    }

    private void UpdateMuteToggleBackground(bool soundOn)
    {
        Image background = GetMuteToggleBackground();
        if (background == null)
            return;

        Sprite targetSprite = soundOn ? GetSoundOnSprite() : soundOffSprite;
        if (targetSprite != null)
            background.sprite = targetSprite;
    }

    private Sprite GetSoundOnSprite()
    {
        return soundOnSprite != null ? soundOnSprite : defaultMuteToggleSprite;
    }

    private Image GetMuteToggleBackground()
    {
        if (muteToggleBackground != null)
            return muteToggleBackground;

        if (muteToggle != null)
            return muteToggle.targetGraphic as Image;

        return null;
    }
}


