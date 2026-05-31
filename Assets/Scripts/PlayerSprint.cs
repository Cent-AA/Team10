using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerSprint : MonoBehaviour
{
    [Header("Настройки ускорения")]
    [SerializeField] private float sprintMultiplier = 1.8f; // Во сколько раз увеличится скорость

    private PlayerControl movementScript; // Ссылка на твой скрипт движения

    void Start()
    {
        // Автоматически находим скрипт движения, который висит на этом же персонаже
        movementScript = GetComponent<PlayerControl>();
    }

    // Этот метод вызывается автоматически при нажатии кнопки "Sprint" в Input Actions
    private void OnSprint(InputValue value)
    {
        if (movementScript == null) return;

        // Если кнопка зажата — умножаем скорость, если отпущена — возвращаем исходную
        if (value.isPressed)
        {
            movementScript.speed *= sprintMultiplier;
        }
        else
        {
            movementScript.speed /= sprintMultiplier;
        }
    }
}