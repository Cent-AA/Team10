using UnityEngine;
using UnityEngine.UI;

public class MainMenuController : MonoBehaviour
{
    [Header("Панели окон")]
    [SerializeField] private GameObject mainMenuPanel;
    [SerializeField] private GameObject optionsMenuPanel;
    [SerializeField] private GameObject exitConfirmationPanel;

    [Header("Элементы настроек")]
    [SerializeField] private Slider volumeSlider;
    [SerializeField] private Toggle muteToggle;

    // Кючевые слова (ключи), по которым Unity будет искать наши сохранения
    private const string VolumeKey = "MasterVolume";
    private const string SoundKey = "SoundOn"; // 1 = Звук есть, 0 = Мьют

    private void Start()
    {
        // 1. ЗАГРУЗКА НАСТРОЕК ГРОМКОСТИ
        // Если игра запускается ПЕРВЫЙ раз и сохранения нет, то по дефолту поставится 1.0f (максимум)
        float savedVolume = PlayerPrefs.GetFloat(VolumeKey, 1.0f);
        
        // 2. ЗАГРУЗКА СОСТОЯНИЯ ГАЛОЧКИ
        // По дефолту ставим 1 (звук включен при первом запуске)
        int savedSoundState = PlayerPrefs.GetInt(SoundKey, 1);
        bool isSoundOn = (savedSoundState == 1);

        // 3. ПРИМЕНЯЕМ ЗАГРУЖЕННЫЕ ДАННЫЕ К ИНТЕРФЕЙСУ
        if (volumeSlider != null)
        {
            volumeSlider.value = savedVolume;
        }

        if (muteToggle != null)
        {
            muteToggle.isOn = isSoundOn;
        }

        // 4. ПРИМЕНЯЕМ ЗАГРУЖЕННЫЕ ДАННЫЕ К ДВИЖКУ UNITTY
        AudioListener.volume = savedVolume;
        AudioListener.pause = !isSoundOn;
    }

    // --- Переключение окон ---
    public void OpenOptions()
    {
        mainMenuPanel.SetActive(false);
        optionsMenuPanel.SetActive(true);
    }

    public void CloseOptions()
    {
        optionsMenuPanel.SetActive(false);
        mainMenuPanel.SetActive(true);
    }

    // --- Окно подтверждения выхода ---
    public void OpenExitConfirmation()
    {
        mainMenuPanel.SetActive(false);
        exitConfirmationPanel.SetActive(true);
    }

    public void CloseExitConfirmation()
    {
        exitConfirmationPanel.SetActive(false);
        mainMenuPanel.SetActive(true);
    }

    public void ConfirmExit()
    {
        Debug.Log("Игра закрывается... (В билде это закроет приложение)"); 
        Application.Quit(); 
    }

    // --- Изменение громкости + СОХРАНЕНИЕ ---
    public void ChangeVolume(float value)
    {
        AudioListener.volume = value;
        
        // Записываем значение ползунка в память
        PlayerPrefs.SetFloat(VolumeKey, value);
        PlayerPrefs.Save(); // Принудительно сохраняем данные на диск
    }

    // --- Включение/Выключение звука + СОХРАНЕНИЕ ---
    public void ToggleSound(bool soundOn)
    {
        AudioListener.pause = !soundOn;
        
        // Если soundOn равен true — сохраняем 1, если false — сохраняем 0
        int stateToSave = soundOn ? 1 : 0;
        
        PlayerPrefs.SetInt(SoundKey, stateToSave);
        PlayerPrefs.Save(); // Принудительно сохраняем данные на диск
    }
}