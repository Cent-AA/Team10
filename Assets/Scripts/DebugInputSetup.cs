using UnityEngine;

// Временный скрипт для тестирования арены без прохождения меню
// Удали когда всё будет работать через полный поток
public class DebugInputSetup : MonoBehaviour
{
    void Awake()
    {
        // Если никто не подключился (прямой запуск арены)
        if (!InputJoinManager.bothJoined)
        {
            InputJoinManager.player1Input = InputJoinManager.InputType.KeyboardWASD;
            InputJoinManager.player2Input = InputJoinManager.InputType.KeyboardArrows;
            InputJoinManager.bothJoined = true;
            Debug.Log("DEBUG: Автоматически назначен P1=WASD, P2=Arrows");
        }
    }
}
