using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

// Этот атрибут автоматически добавит AudioSource на объект, если его там нет
[RequireComponent(typeof(AudioSource))]
public class UIButtonJuice : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [Header("Свечение (Glow)")]
    [SerializeField] private GameObject glowEffect; // Объект неонового свечения (картинка под кнопкой)

    [Header("Звуковые эффекты")]
    [SerializeField] private AudioClip hoverSound;    // Звук при наведении (тихий щелчок)
    [SerializeField] private AudioClip clickSound;    // Звук при самом клике (сочный)

    private Button button;
    private AudioSource audioSource;

    private void Awake()
    {
        button = GetComponent<Button>();
        audioSource = GetComponent<AudioSource>();

        // Настраиваем AudioSource, чтобы он не орал на всю сцену в 3D
        audioSource.playOnAwake = false;
        audioSource.spatialBlend = 0f; // 2D звук интерфейса

        // Программно цепляем звук клика к кнопке, чтобы не делать это руками в OnClick
        if (button != null && clickSound != null)
        {
            button.onClick.AddListener(PlayClickSound);
        }

        // На старте игры прячем свечение
        if (glowEffect != null)
        {
            glowEffect.SetActive(false);
        }
    }

    // Метод срабатывает АВТОМАТИЧЕСКИ, когда мышка наводится на кнопку
    public void OnPointerEnter(PointerEventData eventData)
    {
        // Если кнопка заблокирована (interactable = false), то игнорируем наведение
        if (button != null && !button.interactable) return;

        // Включаем неон
        if (glowEffect != null)
        {
            glowEffect.SetActive(true);
        }

        // Играем звук наведения
        if (hoverSound != null)
        {
            audioSource.PlayOneShot(hoverSound);
        }
    }

    // Метод срабатывает АВТОМАТИЧЕСКИ, когда мышка уходит с кнопки
    public void OnPointerExit(PointerEventData eventData)
    {
        // Гасим неон
        if (glowEffect != null)
        {
            glowEffect.SetActive(false);
        }
    }

    private void PlayClickSound()
    {
        if (clickSound != null)
        {
            audioSource.PlayOneShot(clickSound);
        }
    }
}