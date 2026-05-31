using UnityEngine;
using UnityEngine.InputSystem;
public class PlayerControl : MonoBehaviour
{
    public float movespeed;
    [SerializeField] private float speed=5f;
    private CharacterController controller;
    private Vector2 moveInput;
    public bool isMoving;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
        public void Move(InputAction.CallbackContext context)
    {
        moveInput =context.ReadValue<Vector2>();
    }

    void Awake()
    {
        controller = GetComponent<CharacterController>();
    }
    //void Start()
    //{
    //    
    //}
    // Update is called once per frame
    void Update()
    {
        Vector2 move = new Vector2(moveInput.x,moveInput.y);
        controller.Move(move * speed * Time.deltaTime);
        //Debug.Log(Registry.Players.Count);
    }
}
