using UnityEngine;
using UnityEngine.UI; // ОБЯЗАТЕЛЬНО: без этого Unity не увидит слайдеры и тогглы

public class MainMenuController : MonoBehaviour
{
    [Header("Панели окон")]
    [SerializeField] private GameObject mainMenuPanel;
    [SerializeField] private GameObject optionsMenuPanel;

    [Header("Элементы настроек")]
    [SerializeField] private Slider volumeSlider;
    [SerializeField] private Toggle muteToggle;

    private void Start()
    {
        // При старте игры подтягиваем в интерфейс те значения, которые уже настроены в системе
        if (volumeSlider != null)
            volumeSlider.value = AudioListener.volume;

        if (muteToggle != null)
            muteToggle.isOn = AudioListener.pause;
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

    // --- Управление звуком ---

    // Изменение громкости всей игры (слайдер автоматически выдает от 0.0 до 1.0)
    public void ChangeVolume(float value)
    {
        AudioListener.volume = value;
    }

    // Полное отключение звука (Mute)
    public void ToggleMute(bool isMuted)
    {
        AudioListener.pause = isMuted;
    }
}