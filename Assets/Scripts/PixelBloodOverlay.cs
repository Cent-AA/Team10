using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class PixelBloodOverlay : MonoBehaviour
{
    [Header("Scene Setup")]
    public int sortingOrder = 7600;
    [Min(1)] public int baseParticleCount = 34;
    public RectTransform player1Root;
    public RectTransform player2Root;

    private static PixelBloodOverlay instance;
    private static Sprite pixelSprite;

    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }

        instance = this;
        BuildCanvas();
    }

    public static void PlayForPlayer(int playerNumber, float damage = 0f)
    {
        EnsureInstance();
        instance.SpawnBlood(Mathf.Max(1, playerNumber), damage);
    }

    private static void EnsureInstance()
    {
        if (instance != null)
            return;

        instance = FindFirstObjectByType<PixelBloodOverlay>(FindObjectsInactive.Include);
        if (instance != null)
        {
            instance.BuildCanvas();
            return;
        }

        GameObject overlayObject = new GameObject("PixelBloodOverlay", typeof(RectTransform));
        instance = overlayObject.AddComponent<PixelBloodOverlay>();
        instance.BuildCanvas();
    }

    private void OnDestroy()
    {
        if (instance == this)
            instance = null;
    }

    private void BuildCanvas()
    {
        Canvas canvas = GetComponent<Canvas>();
        if (canvas == null)
        {
            canvas = gameObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        }
        canvas.sortingOrder = sortingOrder;

        CanvasScaler scaler = GetComponent<CanvasScaler>();
        if (scaler == null)
        {
            scaler = gameObject.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;
        }

        if (player1Root == null)
            player1Root = FindCornerRoot("Player1PixelBlood") ?? CreateCornerRoot("Player1PixelBlood", new Vector2(0f, 1f), new Vector2(0f, 1f));
        if (player2Root == null)
            player2Root = FindCornerRoot("Player2PixelBlood") ?? CreateCornerRoot("Player2PixelBlood", new Vector2(1f, 1f), new Vector2(1f, 1f));
    }

    private RectTransform FindCornerRoot(string rootName)
    {
        Transform child = transform.Find(rootName);
        return child != null ? child.GetComponent<RectTransform>() : null;
    }

    private RectTransform CreateCornerRoot(string rootName, Vector2 anchor, Vector2 pivot)
    {
        GameObject rootObject = new GameObject(rootName, typeof(RectTransform));
        RectTransform root = rootObject.GetComponent<RectTransform>();
        root.SetParent(transform, false);
        root.anchorMin = anchor;
        root.anchorMax = anchor;
        root.pivot = pivot;
        root.anchoredPosition = anchor.x < 0.5f ? new Vector2(22f, -20f) : new Vector2(-22f, -20f);
        root.sizeDelta = new Vector2(380f, 260f);

        return root;
    }

    private void SpawnBlood(int playerNumber, float damage)
    {
        RectTransform root = playerNumber == 2 ? player2Root : player1Root;
        if (root == null)
            return;

        int particleCount = baseParticleCount + Mathf.Clamp(Mathf.RoundToInt(damage * 0.75f), 0, 28);
        for (int i = 0; i < particleCount; i++)
        {
            bool isRightSide = playerNumber == 2;
            RectTransform particle = CreateParticle(root, isRightSide);
            Vector2 start = GetStartPosition(isRightSide);
            Vector2 drift = GetDrift(isRightSide);
            float duration = Random.Range(0.35f, 0.75f);
            StartCoroutine(AnimateParticle(particle, start, drift, duration));
        }
    }

    private RectTransform CreateParticle(RectTransform root, bool isRightSide)
    {
        GameObject particleObject = new GameObject("BloodPixel", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        particleObject.transform.SetParent(root, false);

        Image image = particleObject.GetComponent<Image>();
        image.sprite = GetPixelSprite();
        image.raycastTarget = false;
        image.color = RandomBloodColor();

        RectTransform rect = image.rectTransform;
        Vector2 cornerAnchor = isRightSide ? new Vector2(1f, 1f) : new Vector2(0f, 1f);
        rect.anchorMin = cornerAnchor;
        rect.anchorMax = cornerAnchor;
        rect.pivot = new Vector2(0.5f, 0.5f);

        float size = Random.Range(1, 5) * 7f;
        rect.sizeDelta = new Vector2(size, size);
        rect.localRotation = Quaternion.Euler(0f, 0f, Random.Range(0, 4) * 90f);

        return rect;
    }

    private Vector2 GetStartPosition(bool isRightSide)
    {
        float x = Random.Range(0f, 280f);
        if (isRightSide)
            x = -x;

        return new Vector2(x, -Random.Range(0f, 165f));
    }

    private Vector2 GetDrift(bool isRightSide)
    {
        float sidePush = Random.Range(20f, 110f) * (isRightSide ? -1f : 1f);
        float fall = -Random.Range(70f, 185f);
        return new Vector2(sidePush, fall);
    }

    private IEnumerator AnimateParticle(RectTransform particle, Vector2 start, Vector2 drift, float duration)
    {
        if (particle == null)
            yield break;

        Image image = particle.GetComponent<Image>();
        Color startColor = image != null ? image.color : Color.red;
        float elapsed = 0f;

        particle.anchoredPosition = start;
        particle.localScale = Vector3.one;

        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            float steppedT = Mathf.Floor(t * 8f) / 8f;

            particle.anchoredPosition = start + drift * steppedT;
            particle.localScale = Vector3.one * Mathf.Lerp(1f, 0.65f, steppedT);

            if (image != null)
            {
                Color color = startColor;
                color.a = Mathf.Lerp(startColor.a, 0f, steppedT);
                image.color = color;
            }

            yield return null;
        }

        Destroy(particle.gameObject);
    }

    private static Color RandomBloodColor()
    {
        Color[] colors =
        {
            new Color(0.65f, 0.02f, 0.02f, 0.92f),
            new Color(0.45f, 0f, 0f, 0.9f),
            new Color(0.9f, 0.08f, 0.03f, 0.85f),
            new Color(0.28f, 0f, 0f, 0.88f)
        };

        return colors[Random.Range(0, colors.Length)];
    }

    private static Sprite GetPixelSprite()
    {
        if (pixelSprite != null)
            return pixelSprite;

        Texture2D texture = new Texture2D(1, 1, TextureFormat.RGBA32, false);
        texture.filterMode = FilterMode.Point;
        texture.wrapMode = TextureWrapMode.Clamp;
        texture.SetPixel(0, 0, Color.white);
        texture.Apply();

        pixelSprite = Sprite.Create(texture, new Rect(0f, 0f, 1f, 1f), new Vector2(0.5f, 0.5f), 1f);
        pixelSprite.name = "RuntimeBloodPixel";
        return pixelSprite;
    }
}
