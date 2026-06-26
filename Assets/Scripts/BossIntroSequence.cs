using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class BossIntroSequence : MonoBehaviour
{
    [Header("References")]
    public BossController boss;
    public Canvas uiCanvas;

    [Header("Panel")]
    public Sprite heavyShockSprite;
    public RectTransform heavyShockPanel;
    [SerializeField] float panelWidth = 1.2f;
    [SerializeField] float panelHeight = 0.45f;
    [SerializeField] float panelVerticalOffset = 0f;

    [Header("Timing")]
    [SerializeField] float slideInDuration = 0.07f;
    [SerializeField] float holdDuration = 1.3f;
    [SerializeField] float slideOutDuration = 0.07f;
    [SerializeField] float delayBeforeCamera = 0.2f;
    [SerializeField] float delayOnBoss = 0.4f;
    [SerializeField] float screamDuration = 1.6f;
    [SerializeField] float delayBeforeActivate = 0.5f;

    [Header("Camera")]
    [SerializeField] float cinematicZoom = 4f;
    [SerializeField] float screamShakeMagnitude = 0.45f;

    [Header("Panel Wobble")]
    [SerializeField] float wobbleAmplitude = 5f;
    [SerializeField] float wobbleFrequency = 9f;

    public void Play()
    {
        StartCoroutine(Sequence());
    }

    IEnumerator Sequence()
    {
        SetPlayersActive(false);

        // --- Панель HeavyShock ---
        GameObject panelObj = CreatePanel();
        RectTransform rect = panelObj != null ? panelObj.GetComponent<RectTransform>() : null;

        float screenW = Screen.width;
        float screenH = Screen.height;

        if (rect != null)
            rect.sizeDelta = new Vector2(screenW * panelWidth, screenH * panelHeight);

        Vector2 offscreenLeft = new Vector2(-screenW * 1.6f, panelVerticalOffset);
        Vector2 center = new Vector2(0f, panelVerticalOffset);
        Vector2 offscreenRight = new Vector2(screenW * 1.6f, panelVerticalOffset);

        if (rect != null)
        {
        rect.anchoredPosition = offscreenLeft;

        // Влетает резко
        yield return SlidePanel(rect, offscreenLeft, center, slideInDuration, easeOut: true);

        // Держится с покачиванием
        float t = 0f;
        while (t < holdDuration)
        {
            t += Time.deltaTime;
            float wobble = Mathf.Sin(t * wobbleFrequency) * wobbleAmplitude;
            rect.anchoredPosition = center + new Vector2(0f, wobble);
            yield return null;
        }
        rect.anchoredPosition = center;

        // Вылетает резко
        yield return SlidePanel(rect, center, offscreenRight, slideOutDuration, easeOut: false);
        panelObj.SetActive(false);
        }

        yield return new WaitForSeconds(delayBeforeCamera);

        // --- Камера на босса ---
        if (boss != null)
            ArenaCamera.SetCinematicTarget(boss.transform.position, cinematicZoom);

        yield return new WaitForSeconds(delayOnBoss);

        // --- Крик + трясение ---
        float actualScreamDuration = boss != null ? boss.ScreamDuration : screamDuration;
        if (boss != null)
            boss.PlayIntroScream();

        ArenaCamera.Shake(screamShakeMagnitude, actualScreamDuration);
        yield return new WaitForSeconds(actualScreamDuration);

        // --- Камера обратно ---
        ArenaCamera.RestoreNormal();

        yield return new WaitForSeconds(delayBeforeActivate);

        SetPlayersActive(true);

        if (boss != null)
            boss.BeginChase();
    }

    GameObject CreatePanel()
    {
        if (heavyShockPanel == null && uiCanvas != null)
            heavyShockPanel = uiCanvas.transform.Find("HeavyShockPanel") as RectTransform;

        if (heavyShockPanel == null)
        {
            Debug.LogWarning($"{nameof(BossIntroSequence)} has no HeavyShockPanel assigned.", this);
            return null;
        }

        GameObject obj = heavyShockPanel.gameObject;
        if (uiCanvas != null && obj.transform.parent != uiCanvas.transform)
            obj.transform.SetParent(uiCanvas.transform, false);

        RectTransform rect = heavyShockPanel;
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);

        Image img = obj.GetComponent<Image>();
        if (img == null)
            img = obj.AddComponent<Image>();

        img.sprite = heavyShockSprite;
        img.preserveAspect = true;
        img.raycastTarget = false;

        obj.SetActive(true);
        obj.transform.SetAsLastSibling();
        return obj;
    }

    IEnumerator SlidePanel(RectTransform rect, Vector2 from, Vector2 to, float duration, bool easeOut)
    {
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float raw = Mathf.Clamp01(elapsed / duration);
            float eased = easeOut ? 1f - Mathf.Pow(1f - raw, 3f) : Mathf.Pow(raw, 3f);
            rect.anchoredPosition = Vector2.LerpUnclamped(from, to, eased);
            yield return null;
        }
        rect.anchoredPosition = to;
    }

    void SetPlayersActive(bool active)
    {
        foreach (Transform p in Registry.Players)
        {
            if (p == null) continue;
            var pc = p.GetComponent<PlayerController>();
            if (pc != null) pc.enabled = active;
        }
    }
}
