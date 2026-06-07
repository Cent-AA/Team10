using UnityEngine;

public class Bullet : MonoBehaviour
{
    [Header("Settings")]
    [SerializeField] private float speed = 12f;      // Скорость полета пули
    [SerializeField] private float damage = 15f;     // Урон, который получит враг
    [SerializeField] private float lifeTime = 3f;    // Время жизни пули в секундах

    private Rigidbody2D rb;
    private Transform owner;

    public void Init(Transform ownerTransform)
    {
        owner = ownerTransform;
    }

    void Awake()
    {
        // Находим компонент физики в момент появления пули
        rb = GetComponent<Rigidbody2D>();
    }

    void Start()
    {
        // Автоматически уничтожаем пулю через заданное время, чтобы не забивать память
        Destroy(gameObject, lifeTime);

        // Пуля летит САМА сразу при создании
        if (rb != null)
        {
            rb.linearVelocity = transform.right * speed;
        }
    }

    // Фиксируем столкновение пули с объектами
    private void OnTriggerEnter2D(Collider2D collision)
    {
        // Проверяем, что пуля врезалась НЕ в самого игрока
        if (!collision.CompareTag("Player"))
        {
            // Проверяем, есть ли на объекте скрипт зомби
            ZombieAI zombie = collision.GetComponent<ZombieAI>();
            
            if (zombie != null)
            {
                // Направление толчка — вектор полета пули (куда летит, туда и толкает)
                Vector2 knockbackDir = transform.right;

                // Вызываем метод урона у зомби, передавая урон и нокбэк
                zombie.TakeDamage(damage, knockbackDir, owner);
            }

            // Уничтожаем пулю при любом столкновении (со стеной или врагом)
            Destroy(gameObject);
        }
    }
}
