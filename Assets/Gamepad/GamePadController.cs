using UnityEngine;
using UnityEngine.InputSystem; // Подключаем новую систему ввода

public class PlayerController : MonoBehaviour
{
    private Vector2 moveInput;
    [SerializeField] private float speed = 5f;

    // Этот метод вызывается автоматически компонентом Player Input 
    // при движении левого стика геймпада
    private void OnMove(InputValue value)
    {
        moveInput = value.Get<Vector2>();
    }

    // Этот метод вызывается автоматически при нажатии кнопки "Jump"
    private void OnJump()
    {
        Debug.Log("Прыжок сработал!");
    }

    private void Update()
    {
        // Для 2D-игры: moveInput.x — это влево/вправо, moveInput.y — это вверх/вниз
        Vector3 movement = new Vector3(moveInput.x, moveInput.y, 0f) * speed * Time.deltaTime;
        
        // Двигаем наш объект по экрану
        transform.Translate(movement);
    }
}