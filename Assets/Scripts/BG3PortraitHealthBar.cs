using UnityEngine;
using UnityEngine.UI;

public class BG3PortraitHealthBar : MonoBehaviour
{
    [Header("Player")]
    [SerializeField] private int playerNumber = 1;
    [SerializeField] private bool autoFindPlayer = true;

    [Header("Health")]
    [SerializeField] private float maxHealth = 100f;
    [SerializeField] private float currentHealth = 100f;

    [Header("Bar")]
    [SerializeField] private Image healthFill;
    [SerializeField] private Gradient healthGradient;
    [SerializeField] private float smoothSpeed = 8f;

    [Header("Portrait")]
    [SerializeField] private Image grayPortrait;
    [SerializeField] private Image colorPortrait;
    [SerializeField] private Sprite portraitSprite;
    [SerializeField] private bool fillPortraitFromBottom = true;
    [SerializeField] private bool makeGraySpriteAtRuntime = true;

    private float targetPercent = 1f;
    private Sprite generatedGraySprite;
    private PlayerController boundPlayer;

    public int PlayerNumber => playerNumber;

    private void Awake()
    {
        InferPlayerNumberFromName();

        maxHealth = Mathf.Max(1f, maxHealth);
        currentHealth = Mathf.Clamp(currentHealth, 0f, maxHealth);
        targetPercent = currentHealth / maxHealth;

        SetupPortraits();
        ApplyHealthInstant();
    }

    private void OnValidate()
    {
        playerNumber = Mathf.Max(1, playerNumber);
        InferPlayerNumberFromName();
    }

    private void OnEnable()
    {
        TryAutoBind();
    }

    private void Update()
    {
        if (boundPlayer == null)
        {
            TryAutoBind();
        }

        if (healthFill == null && colorPortrait == null)
        {
            return;
        }

        float currentFill = healthFill != null ? healthFill.fillAmount : colorPortrait.fillAmount;
        float smoothFill = Mathf.MoveTowards(currentFill, targetPercent, smoothSpeed * Time.deltaTime);

        ApplyVisuals(smoothFill);
    }

    public void Bind(PlayerController player)
    {
        if (player != null)
        {
            SetPlayerNumber(player.playerNumber);
        }

        if (boundPlayer == player)
        {
            if (boundPlayer != null)
            {
                SetHealth(boundPlayer.currentHealth, boundPlayer.maxHealth);
                ApplyHealthInstant();
            }

            return;
        }

        Unbind();
        boundPlayer = player;

        if (boundPlayer == null)
        {
            return;
        }

        boundPlayer.OnHealthChanged += SetHealth;
        SetHealth(boundPlayer.currentHealth, boundPlayer.maxHealth);
        ApplyHealthInstant();
    }

    public void SetPlayerNumber(int number)
    {
        int newPlayerNumber = Mathf.Max(1, number);
        if (playerNumber == newPlayerNumber)
        {
            return;
        }

        playerNumber = newPlayerNumber;

        if (boundPlayer != null && boundPlayer.playerNumber != playerNumber)
        {
            Unbind();
        }
    }

    public void Unbind()
    {
        if (boundPlayer != null)
        {
            boundPlayer.OnHealthChanged -= SetHealth;
            boundPlayer = null;
        }
    }

    public void SetHealth(float current, float max)
    {
        maxHealth = Mathf.Max(1f, max);
        currentHealth = Mathf.Clamp(current, 0f, maxHealth);
        targetPercent = currentHealth / maxHealth;
    }

    public void TakeDamage(float damage)
    {
        SetHealth(currentHealth - Mathf.Abs(damage), maxHealth);
    }

    public void Heal(float amount)
    {
        SetHealth(currentHealth + Mathf.Abs(amount), maxHealth);
    }

    public void SetPortrait(Sprite sprite)
    {
        portraitSprite = sprite;
        SetupPortraits();
        ApplyHealthInstant();
    }

    [ContextMenu("Apply Health Instant")]
    private void ApplyHealthInstant()
    {
        ApplyVisuals(targetPercent);
    }

    private void ApplyVisuals(float percent)
    {
        percent = Mathf.Clamp01(percent);

        if (healthFill != null)
        {
            healthFill.type = Image.Type.Filled;
            healthFill.fillMethod = Image.FillMethod.Horizontal;
            healthFill.fillOrigin = (int)Image.OriginHorizontal.Left;
            healthFill.fillAmount = percent;

            if (healthGradient != null)
            {
                healthFill.color = healthGradient.Evaluate(percent);
            }
        }

        if (colorPortrait != null)
        {
            colorPortrait.type = Image.Type.Filled;
            colorPortrait.fillMethod = Image.FillMethod.Vertical;
            colorPortrait.fillOrigin = fillPortraitFromBottom
                ? (int)Image.OriginVertical.Bottom
                : (int)Image.OriginVertical.Top;
            colorPortrait.fillAmount = percent;
        }

        if (grayPortrait != null)
        {
            grayPortrait.type = Image.Type.Simple;
            grayPortrait.fillAmount = 1f;
        }
    }

    private void SetupPortraits()
    {
        Sprite sprite = portraitSprite;

        if (sprite == null && colorPortrait != null)
        {
            sprite = colorPortrait.sprite;
        }

        if (sprite == null && grayPortrait != null)
        {
            sprite = grayPortrait.sprite;
        }

        if (colorPortrait != null)
        {
            colorPortrait.sprite = sprite;
            colorPortrait.preserveAspect = true;
        }

        if (grayPortrait != null)
        {
            grayPortrait.sprite = makeGraySpriteAtRuntime ? GetGraySprite(sprite) : sprite;
            grayPortrait.color = makeGraySpriteAtRuntime ? Color.white : new Color(0.35f, 0.35f, 0.35f, 1f);
            grayPortrait.preserveAspect = true;
        }
    }

    private Sprite GetGraySprite(Sprite source)
    {
        if (source == null)
        {
            return null;
        }

        if (generatedGraySprite != null)
        {
            Destroy(generatedGraySprite.texture);
            Destroy(generatedGraySprite);
        }

        Texture2D grayTexture = null;
        RenderTexture previousRenderTexture = RenderTexture.active;
        RenderTexture temporaryRenderTexture = null;

        try
        {
            Rect rect = source.rect;
            Texture2D sourceTexture = source.texture;
            int width = Mathf.RoundToInt(rect.width);
            int height = Mathf.RoundToInt(rect.height);

            temporaryRenderTexture = RenderTexture.GetTemporary(
                sourceTexture.width,
                sourceTexture.height,
                0,
                RenderTextureFormat.ARGB32);

            Graphics.Blit(sourceTexture, temporaryRenderTexture);
            RenderTexture.active = temporaryRenderTexture;

            grayTexture = new Texture2D(width, height, TextureFormat.RGBA32, false);
            grayTexture.ReadPixels(new Rect(rect.x, rect.y, width, height), 0, 0);
            grayTexture.Apply();

            Color[] sourcePixels = grayTexture.GetPixels();

            for (int i = 0; i < sourcePixels.Length; i++)
            {
                Color pixel = sourcePixels[i];
                float gray = pixel.r * 0.299f + pixel.g * 0.587f + pixel.b * 0.114f;
                sourcePixels[i] = new Color(gray, gray, gray, pixel.a);
            }

            grayTexture.SetPixels(sourcePixels);
            grayTexture.Apply();
            grayTexture.filterMode = sourceTexture.filterMode;
            grayTexture.wrapMode = TextureWrapMode.Clamp;
            grayTexture.name = source.name + "_Gray";

            generatedGraySprite = Sprite.Create(
                grayTexture,
                new Rect(0f, 0f, width, height),
                new Vector2(source.pivot.x / width, source.pivot.y / height),
                source.pixelsPerUnit,
                0,
                SpriteMeshType.FullRect,
                source.border);

            return generatedGraySprite;
        }
        catch (System.Exception exception)
        {
            if (grayTexture != null)
            {
                Destroy(grayTexture);
            }

            makeGraySpriteAtRuntime = false;
            Debug.LogWarning("Could not create gray portrait sprite. Using tint fallback instead. " + exception.Message);
            return source;
        }
        finally
        {
            RenderTexture.active = previousRenderTexture;

            if (temporaryRenderTexture != null)
            {
                RenderTexture.ReleaseTemporary(temporaryRenderTexture);
            }
        }
    }

    private bool TryAutoBind()
    {
        if (!autoFindPlayer || boundPlayer != null)
        {
            return boundPlayer != null;
        }

        InferPlayerNumberFromName();

        PlayerController[] players = FindObjectsByType<PlayerController>(FindObjectsSortMode.None);
        for (int i = 0; i < players.Length; i++)
        {
            if (players[i] != null && players[i].playerNumber == playerNumber)
            {
                Bind(players[i]);
                return true;
            }
        }

        return false;
    }

    private void InferPlayerNumberFromName()
    {
        string lowerName = gameObject.name.ToLowerInvariant();
        if (lowerName.Contains("player2"))
        {
            SetPlayerNumber(2);
        }
        else if (lowerName.Contains("player1"))
        {
            SetPlayerNumber(1);
        }
    }

    private void OnDisable()
    {
        Unbind();
    }

    private void OnDestroy()
    {
        Unbind();

        if (generatedGraySprite != null)
        {
            Destroy(generatedGraySprite.texture);
            Destroy(generatedGraySprite);
        }
    }
}
