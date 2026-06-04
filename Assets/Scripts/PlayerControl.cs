using UnityEngine;
using UnityEngine.InputSystem;
public class PlayerControl : MonoBehaviour
{
    public float movespeed;
    [SerializeField] public float speed=5f;
    private Rigidbody2D rb;
    private Vector2 moveInput;
    public bool isMoving;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
        public void Move(InputAction.CallbackContext context)
    {
        moveInput =context.ReadValue<Vector2>();
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
        rb.MovePosition(rb.position + moveInput * speed * Time.fixedDeltaTime);
        //Debug.Log(Registry.Players.Count);
    }
}
