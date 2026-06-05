using UnityEngine;
using UnityEngine.UI;

public class UIBarsManager : MonoBehaviour
{
    // Синглтон (удобный доступ из любого скрипта без лишних ссылок)
    public static UIBarsManager Instance { get; private set; }

    [Header("UI Sliders")]
    [SerializeField] private Slider hpSlider;
    [SerializeField] private Slider staminaSlider;

    [Header("Health Settings")]
    [SerializeField] private float maxHealth = 100f;
    private float currentHealth;

    [Header("Stamina Settings")]
    [SerializeField] private float maxStamina = 100f;
    [SerializeField] private float staminaDrain = 25f;  // Расход в секунду
    [SerializeField] private float staminaRegen = 15f;  // Восстановление в секунду
    private float currentStamina;

    // Свойство, которое твой скрипт движения сможет проверять перед бегом
    public bool CanSprint => currentStamina > 0;

    private void Awake()
    {
        // Настройка синглтона
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void Start()
    {
        // Инициализация здоровья
        currentHealth = maxHealth;
        if (hpSlider != null)
        {
            hpSlider.maxValue = maxHealth;
            hpSlider.value = maxHealth;
        }

        // Инициализация выносливости
        currentStamina = maxStamina;
        if (staminaSlider != null)
        {
            staminaSlider.maxValue = maxStamina;
            staminaSlider.value = maxStamina;
        }
    }

    // --- ЛОГИКА ЗДОРОВЬЯ ---

    /// <summary>
    /// Вызывай этот метод из скрипта противника или игрока при получении урона.
    /// </summary>
    public void TakeDamage(float damage)
    {
        currentHealth -= damage;
        currentHealth = Mathf.Clamp(currentHealth, 0, maxHealth);

        if (hpSlider != null)
        {
            hpSlider.value = currentHealth;
        }

        if (currentHealth <= 0)
        {
            Debug.Log("Игрок погиб!");
            // Здесь можно вызвать метод смерти игрока
        }
    }

    /// <summary>
    /// Метод для лечения (если пригодится в будущем)
    /// </summary>
    public void Heal(float amount)
    {
        currentHealth += amount;
        currentHealth = Mathf.Clamp(currentHealth, 0, maxHealth);

        if (hpSlider != null)
        {
            hpSlider.value = currentHealth;
        }
    }

    // --- ЛОГИКА ВЫНОСЛИВОСТИ ---

    /// <summary>
    /// Вызывай этот метод в Update твоего скрипта движения, если игрок БЕЖИТ.
    /// </summary>
    public void UseStamina()
    {
        if (currentStamina > 0)
        {
            currentStamina -= staminaDrain * Time.deltaTime;
            UpdateStaminaUI();
        }
    }

    /// <summary>
    /// Вызывай этот метод в Update твоего скрипта движения, если игрок НЕ бежит (стоит или идет).
    /// </summary>
    public void RegenerateStamina()
    {
        if (currentStamina < maxStamina)
        {
            currentStamina += staminaRegen * Time.deltaTime;
            UpdateStaminaUI();
        }
    }

    private void UpdateStaminaUI()
    {
        currentStamina = Mathf.Clamp(currentStamina, 0, maxStamina);
        if (staminaSlider != null)
        {
            staminaSlider.value = currentStamina;
        }
    }
}