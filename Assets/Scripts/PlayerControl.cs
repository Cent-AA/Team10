using UnityEngine;
using UnityEngine.InputSystem;
public class PlayerControl : MonoBehaviour
{
    public float movespeed;
    public float damage;
    [SerializeField] public float speed=5f;
    private Rigidbody2D rb;
    private Vector2 moveInput;

    [SerializeField] private float sprintSpeed = 8f; // Скорость при спринте
    private bool isSprinting; // Флаг: бежим мы или нет

    public bool isMoving;
    private RaycastHit2D[] hits;
    [SerializeField] public float attackRange;
    [SerializeField] public float damageAbsorption;
    [SerializeField] public float dodgeChance;
    [SerializeField] public float luck;
    [SerializeField] private Transform attackTransform;
    [SerializeField] private LayerMask attackableLayer;

    [Header("Ranged Attack")]
    [SerializeField] private GameObject bulletPrefab; // Сюда перетащим префаб пули
    [SerializeField] private Transform firePoint;     // Сюда перетащим пустой объект FirePoint
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    public void Move(InputAction.CallbackContext context)
    {
        moveInput =context.ReadValue<Vector2>();
    }

    public void Sprint(InputAction.CallbackContext context)
    {
        if (context.performed)
            {
                isSprinting = true;
            }
            else if (context.canceled)
            {
                isSprinting = false;
            }
    }
    // tihs might be better in a separate script
    public void Attack(InputAction.CallbackContext context)
    {
       // Debug.Log("Attack pressed");
        if (!context.performed) return;
        if (attackTransform == null) return;
        //Debug.Log("Attack confird");
        hits =Physics2D.CircleCastAll(attackTransform.position,attackRange,Vector2.zero, 0f,attackableLayer);
        foreach (RaycastHit2D hit in hits)
        {
            //Debug.Log("hit");
            Health health =hit.collider.GetComponent<Health>();
            if(health != null)
            {
                health.TakeDamage(damage,luck);
            }
        }
    }

    public void Shoot(InputAction.CallbackContext context)
    {
        // Выстрел срабатывает один раз строго в момент НАЖАТИЯ на кнопку
        if (context.performed)
        {
            // Создаем пулю в координатах firePoint
            GameObject newBullet = Instantiate(bulletPrefab, firePoint.position, Quaternion.identity);
            
            // Находим на пуле её скрипт и запускаем вперед
            Bullet bulletScript = newBullet.GetComponent<Bullet>();
            if (bulletScript != null)
            {
                // transform.right сам смотрит влево или вправо, 
                // так как ты разворачиваешь персонажа через localScale в Update!
                bulletScript.Launch(transform.right);
            }
        }
    }
    private void OnDrawGizmosSelected()
    {
        if (attackTransform == null) return;
        Gizmos.DrawWireSphere(attackTransform.position,attackRange);
    }

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
    }
    //void Start()
    //{
    //    
    //}
    // Update is called once per frame
    void Update()
    {
        // 1. Проверяем, двигается ли игрок в данный момент
        bool isMoving = moveInput.magnitude > 0;

        // Переменная для финальной скорости в этом кадре
        float currentSpeed;

        // 2. Условие бега: зажат Shift, игрок идет И у менеджера Стамина > 0
        UIBarsManager barsManager = UIBarsManager.Instance;

        if (isSprinting && isMoving && barsManager != null && barsManager.CanSprint)
        {
            currentSpeed = sprintSpeed;           // Применяем скорость спринта
            barsManager.UseStamina();  // Тратим стамину через менеджер
        }
        else
        {
            currentSpeed = speed;                      // Возвращаем обычную скорость (5f)
            if (barsManager != null)
            {
                barsManager.RegenerateStamina(); // Восстанавливаем стамину
            }
        }

        // Передвигаем физическое тело игрока с итоговой скоростью
        rb.MovePosition(rb.position + moveInput * currentSpeed * Time.fixedDeltaTime);

        // Поворот спрайта влево/вправо в зависимости от направления движения
        if (moveInput.x > 0)
        {
            Vector3 scale = transform.localScale;
            scale.x = Mathf.Abs(scale.x);
            transform.localScale = scale;
        }
        else if (moveInput.x < 0)
        {
            Vector3 scale = transform.localScale;
            scale.x = -Mathf.Abs(scale.x);
            transform.localScale = scale;
        }
    }
}
