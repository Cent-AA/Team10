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
    [SerializeField] private Transform attackTransform;
    [SerializeField] private LayerMask attackableLayer;
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
        //Debug.Log("Attack confird");
        hits =Physics2D.CircleCastAll(attackTransform.position,attackRange,Vector2.zero, 0f,attackableLayer);
        foreach (RaycastHit2D hit in hits)
        {
            //Debug.Log("hit");
            Health health =hit.collider.GetComponent<Health>();
            if(health != null)
            {
                health.TakeDamage(damage);
            }
        }
    }
    private void OnDrawGizmosSelected()
    {
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
        Vector2 move = new Vector2(moveInput.x,moveInput.y);
        //rb.MovePosition(move * speed * Time.deltaTime);
        // Если isSprinting равен true, используем sprintSpeed, иначе — обычную speed
        float currentSpeed = isSprinting ? sprintSpeed : speed;
        rb.MovePosition(rb.position + moveInput * currentSpeed * Time.fixedDeltaTime);
        //Debug.Log(Registry.Players.Count);
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