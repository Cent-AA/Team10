using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class ButtonGlow : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [Header("Настройки свечения")]
    public Image glowImage;
    public float fadeInDuration = 0.6f;
    public float fadeOutDuration = 0.6f;
    public float maxAlpha = 0.1f;       // Максимальная яркость (30%)

    private bool isHovered = false;
    private float progress = 0f;        // 0..1

    void Start()
    {
        if (glowImage != null)
        {
            Color c = glowImage.color;
            c.a = 0f;
            glowImage.color = c;
        }
    }

    void Update()
    {
        if (glowImage == null) return;

        if (isHovered)
            progress += Time.deltaTime / fadeInDuration;
        else
            progress -= Time.deltaTime / fadeOutDuration;

        progress = Mathf.Clamp01(progress);

        Color color = glowImage.color;
        color.a = progress * maxAlpha;
        glowImage.color = color;
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        isHovered = true;
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        isHovered = false;
    }
}