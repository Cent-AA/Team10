using UnityEngine;

public class MusicLoop : MonoBehaviour
{
    [Header("Audio Sources")]
    [SerializeField] private AudioSource introSource; // Сюда перетащим первый Audio Source (с нарастанием)
    [SerializeField] private AudioSource loopSource;  // Сюда перетащим второй Audio Source (основная петля)

    void Start()
    {
        // Убедимся, что loopSource НЕ играет сразу сам
        if (introSource == null || loopSource == null || introSource.clip == null)
        {
            return;
        }

        loopSource.Stop();
        loopSource.loop = true;

        // Запускаем вступление
        introSource.Play();

        // Просим Unity подготовить (запланировать) воспроизведение основной петли 
        // ровно в ту секунду, когда закончится вступление
        double introLength = (double)introSource.clip.samples / introSource.clip.frequency;
        loopSource.PlayScheduled(AudioSettings.dspTime + introLength);
    }
}
