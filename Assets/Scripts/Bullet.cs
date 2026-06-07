using UnityEngine;

public class Bullet : MonoBehaviour
{
    [Header("Settings")]
    [SerializeField] private float speed = 12f;      // Скорость полета пули
    [SerializeField] private float damage = 15f;     // Урон, который получит враг
    [SerializeField] private float lifeTime = 3f;   // Время жизни пули в секундах

    private Rigidbody2D rb;

    void Awake()
    {
        // Находим компонент физики в момент появления пули
        rb = GetComponent<Rigidbody2D>();
    }

    void Start()
    {
        // Автоматически уничтожаем пулю через 3 секунды, 
        // чтобы улетевшие за экран пули не тратили память игры
        Destroy(gameObject, lifeTime);
    }

    // Этот метод вызывается из скрипта игрока (PlayerControl) и толкает пулю
    public void Launch(Vector2 direction)
    {
        if (rb != null)
        {
            // Задаем физическую скорость в указанном направлении
            rb.linearVelocity = direction * speed;
        }
    }

    // Фиксируем столкновение пули с объектами
    private void OnTriggerEnter2D(Collider2D collision)
    {
        // Проверяем, что пуля врезалась НЕ в самого игрока
        if (!collision.CompareTag("Player"))
        {
            // Проверяем, есть ли у объекта, в который мы попали, скрипт Health
            Health enemyHealth = collision.GetComponent<Health>();
            
            if (enemyHealth != null)
            {
                // Наносим врагу урон
                enemyHealth.TakeDamage(damage);
            }

            // В любом случае уничтожаем саму пулю при столкновении (со стеной или врагом)
            Destroy(gameObject);
        }
    }
}