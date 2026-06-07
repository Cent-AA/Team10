using UnityEngine;

public class CampfireController : MonoBehaviour
{
    [Header("═══ Кадры огня ═══")]
    public Sprite[] fireFrames;                   // 3 кадра огня
    public float frameRate = 8f;                   // Кадров в секунду
    public float frameRateVariation = 3f;          // Рандомная вариация скорости
    public SpriteRenderer fireRenderer;

    [Header("═══ Масштабный пульс огня ═══")]
    public float scalePulseAmount = 0.08f;
    public float scalePulseSpeed = 4f;
    public float scaleRandomness = 0.03f;

    [Header("═══ Свечение (гало) ═══")]
    public bool createGlow = true;
    public Color glowColor = new Color(1f, 0.6f, 0.15f, 0.25f);
    public float glowSize = 5f;
    public float glowPulseAmount = 0.3f;
    public float glowPulseSpeed = 3f;

    [Header("═══ Свет на земле ═══")]
    public bool createGroundLight = true;
    public Color groundLightColor = new Color(1f, 0.5f, 0.1f, 0.15f);
    public float groundLightSize = 8f;

    [Header("═══ Мерцание ═══")]
    public float flickerSpeed = 15f;
    public float flickerAmount = 0.15f;

    [Header("═══ Искры ═══")]
    public bool createSparks = true;
    public int sparkCount = 30;
    public Color sparkColor1 = new Color(1f, 0.8f, 0.2f, 1f);
    public Color sparkColor2 = new Color(1f, 0.3f, 0f, 1f);

    [Header("═══ Дым ═══")]
    public bool createSmoke = true;
    public int smokeCount = 10;
    public Color smokeColor = new Color(0.3f, 0.3f, 0.3f, 0.15f);

    [Header("═══ Угольки ═══")]
    public bool createEmbers = true;
    public int emberCount = 15;
    public Color emberColor = new Color(1f, 0.2f, 0f, 0.8f);

    [Header("═══ Звук ═══")]
    public AudioClip cracklingSound;
    public float soundVolume = 0.3f;

    // Приватные
    private float frameTimer;
    private float currentFrameRate;
    private int currentFrame;
    private Vector3 originalScale;

    private SpriteRenderer glowRenderer;
    private SpriteRenderer groundLightRenderer;
    private float glowBaseAlpha;
    private float groundBaseAlpha;

    // Рандомные оффсеты для органичного мерцания
    private float flickerOffset1, flickerOffset2, flickerOffset3;

    void Start()
    {
        if (fireRenderer == null)
            fireRenderer = GetComponent<SpriteRenderer>();

        originalScale = transform.localScale;
        currentFrameRate = frameRate;

        // Рандомные фазы для уникального мерцания
        flickerOffset1 = Random.Range(0f, 100f);
        flickerOffset2 = Random.Range(0f, 100f);
        flickerOffset3 = Random.Range(0f, 100f);

        if (createGlow) SetupGlow();
        if (createGroundLight) SetupGroundLight();
        if (createSparks) SetupSparks();
        if (createSmoke) SetupSmoke();
        if (createEmbers) SetupEmbers();
        if (cracklingSound != null) SetupAudio();
    }

    void Update()
    {
        AnimateFrames();
        AnimateScale();
        AnimateGlow();
        AnimateGroundLight();
    }

    // ═══════════ КАДРОВАЯ АНИМАЦИЯ ═══════════
    void AnimateFrames()
    {
        if (fireFrames == null || fireFrames.Length == 0 || fireRenderer == null) return;

        frameTimer += Time.deltaTime;
        float interval = 1f / currentFrameRate;

        if (frameTimer >= interval)
        {
            frameTimer -= interval;
            currentFrame = (currentFrame + 1) % fireFrames.Length;
            fireRenderer.sprite = fireFrames[currentFrame];

            // Рандомная скорость — огонь "живой"
            currentFrameRate = frameRate + Random.Range(-frameRateVariation, frameRateVariation);
            currentFrameRate = Mathf.Max(currentFrameRate, 2f);
        }
    }

    // ═══════════ ПУЛЬСАЦИЯ МАСШТАБА ═══════════
    void AnimateScale()
    {
        float t = Time.time * scalePulseSpeed;

        // Несколько синусоид для органичного движения
        float pulseX = 1f + Mathf.Sin(t + flickerOffset1) * scalePulseAmount
                          + Mathf.Sin(t * 2.3f + flickerOffset2) * scaleRandomness;
        float pulseY = 1f + Mathf.Sin(t * 1.1f + flickerOffset3) * scalePulseAmount * 1.3f
                          + Mathf.Sin(t * 3.1f + flickerOffset1) * scaleRandomness;

        transform.localScale = new Vector3(
            originalScale.x * pulseX,
            originalScale.y * pulseY,
            originalScale.z);
    }

    // ═══════════ СВЕЧЕНИЕ ═══════════
    void SetupGlow()
    {
        GameObject glowObj = new GameObject("FireGlow");
        glowObj.transform.SetParent(transform);
        glowObj.transform.localPosition = new Vector3(0, 0.3f, 0);
        glowObj.transform.localScale = Vector3.one * glowSize;

        glowRenderer = glowObj.AddComponent<SpriteRenderer>();
        glowRenderer.sprite = CreateCircleSprite(64);
        glowRenderer.color = glowColor;
        glowRenderer.sortingOrder = fireRenderer != null ? fireRenderer.sortingOrder - 1 : -1;
        glowRenderer.material = new Material(Shader.Find("Sprites/Default"));
        glowRenderer.material.SetFloat("_Mode", 1);  // Additive-like

        glowBaseAlpha = glowColor.a;
    }

    void AnimateGlow()
    {
        if (glowRenderer == null) return;

        float t = Time.time;
        // Сложное мерцание из нескольких частот
        float flicker = 1f
            + Mathf.Sin(t * flickerSpeed + flickerOffset1) * flickerAmount
            + Mathf.Sin(t * flickerSpeed * 0.7f + flickerOffset2) * flickerAmount * 0.5f
            + Mathf.PerlinNoise(t * 3f, flickerOffset3) * flickerAmount * 0.8f;

        float pulse = Mathf.Sin(t * glowPulseSpeed) * glowPulseAmount;

        Color c = glowRenderer.color;
        c.a = glowBaseAlpha * (flicker + pulse);
        c.a = Mathf.Clamp01(c.a);
        glowRenderer.color = c;

        // Лёгкое изменение размера
        float sizeFlicker = glowSize * (1f + Mathf.Sin(t * 2f) * 0.05f);
        glowRenderer.transform.localScale = Vector3.one * sizeFlicker;
    }

    // ═══════════ СВЕТ НА ЗЕМЛЕ ═══════════
    void SetupGroundLight()
    {
        GameObject groundObj = new GameObject("GroundLight");
        groundObj.transform.SetParent(transform);
        groundObj.transform.localPosition = new Vector3(0, -0.2f, 0);
        groundObj.transform.localScale = new Vector3(groundLightSize, groundLightSize * 0.6f, 1f);

        groundLightRenderer = groundObj.AddComponent<SpriteRenderer>();
        groundLightRenderer.sprite = CreateCircleSprite(64);
        groundLightRenderer.color = groundLightColor;
        groundLightRenderer.sortingOrder = fireRenderer != null ? fireRenderer.sortingOrder - 2 : -2;

        groundBaseAlpha = groundLightColor.a;
    }

    void AnimateGroundLight()
    {
        if (groundLightRenderer == null) return;

        float t = Time.time;
        float flicker = 1f + Mathf.Sin(t * flickerSpeed * 0.5f + flickerOffset2) * flickerAmount * 0.3f;

        Color c = groundLightRenderer.color;
        c.a = groundBaseAlpha * flicker;
        groundLightRenderer.color = c;
    }

    // ═══════════ ИСКРЫ ═══════════
    void SetupSparks()
    {
        GameObject sparkObj = new GameObject("Sparks");
        sparkObj.transform.SetParent(transform);
        sparkObj.transform.localPosition = Vector3.zero;

        ParticleSystem ps = sparkObj.AddComponent<ParticleSystem>();
        var main = ps.main;
        main.startLifetime = new ParticleSystem.MinMaxCurve(0.5f, 1.5f);
        main.startSpeed = new ParticleSystem.MinMaxCurve(1f, 3f);
        main.startSize = new ParticleSystem.MinMaxCurve(0.02f, 0.06f);
        main.startColor = new ParticleSystem.MinMaxGradient(sparkColor1, sparkColor2);
        main.maxParticles = sparkCount;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.gravityModifier = -0.3f;  // Вверх

        var emission = ps.emission;
        emission.rateOverTime = sparkCount / 2f;

        var shape = ps.shape;
        shape.shapeType = ParticleSystemShapeType.Circle;
        shape.radius = 0.3f;

        // ИСПРАВЛЕНИЕ ОШИБКИ: Явно принуждаем оси X и Y работать в одном режиме распределения ценностей
        var vel = ps.velocityOverLifetime;
        vel.enabled = true;
        vel.x = new ParticleSystem.MinMaxCurve(-0.5f, 0.5f) { mode = ParticleSystemCurveMode.TwoConstants };
        vel.y = new ParticleSystem.MinMaxCurve(0.5f, 2f) { mode = ParticleSystemCurveMode.TwoConstants };
        vel.z = new ParticleSystem.MinMaxCurve(0f, 0f) { mode = ParticleSystemCurveMode.TwoConstants };

        var colorLife = ps.colorOverLifetime;
        colorLife.enabled = true;
        Gradient grad = new Gradient();
        grad.SetKeys(
            new GradientColorKey[] {
                new GradientColorKey(sparkColor1, 0f),
                new GradientColorKey(sparkColor2, 1f)
            },
            new GradientAlphaKey[] {
                new GradientAlphaKey(1f, 0f),
                new GradientAlphaKey(0f, 1f)
            });
        colorLife.color = grad;

        var sizeLife = ps.sizeOverLifetime;
        sizeLife.enabled = true;
        sizeLife.size = new ParticleSystem.MinMaxCurve(1f, new AnimationCurve(
            new Keyframe(0, 1), new Keyframe(1, 0)));

        // Рендерер
        var renderer = sparkObj.GetComponent<ParticleSystemRenderer>();
        renderer.material = new Material(Shader.Find("Sprites/Default"));
        renderer.sortingOrder = fireRenderer != null ? fireRenderer.sortingOrder + 1 : 1;
    }

    // ═══════════ ДЫМ ═══════════
    void SetupSmoke()
    {
        GameObject smokeObj = new GameObject("Smoke");
        smokeObj.transform.SetParent(transform);
        smokeObj.transform.localPosition = new Vector3(0, 0.5f, 0);

        ParticleSystem ps = smokeObj.AddComponent<ParticleSystem>();
        var main = ps.main;
        main.startLifetime = new ParticleSystem.MinMaxCurve(2f, 4f);
        main.startSpeed = new ParticleSystem.MinMaxCurve(0.2f, 0.5f);
        main.startSize = new ParticleSystem.MinMaxCurve(0.3f, 0.8f);
        main.startColor = smokeColor;
        main.maxParticles = smokeCount;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.gravityModifier = -0.1f;

        var emission = ps.emission;
        emission.rateOverTime = smokeCount / 3f;

        var shape = ps.shape;
        shape.shapeType = ParticleSystemShapeType.Circle;
        shape.radius = 0.15f;

        var sizeLife = ps.sizeOverLifetime;
        sizeLife.enabled = true;
        sizeLife.size = new ParticleSystem.MinMaxCurve(1f, new AnimationCurve(
            new Keyframe(0, 0.3f), new Keyframe(0.5f, 1f), new Keyframe(1, 1.5f)));

        var colorLife = ps.colorOverLifetime;
        colorLife.enabled = true;
        Gradient grad = new Gradient();
        grad.SetKeys(
            new GradientColorKey[] {
                new GradientColorKey(Color.gray, 0f),
                new GradientColorKey(Color.gray, 1f)
            },
            new GradientAlphaKey[] {
                new GradientAlphaKey(0.15f, 0f),
                new GradientAlphaKey(0.05f, 0.5f),
                new GradientAlphaKey(0f, 1f)
            });
        colorLife.color = grad;

        // ИСПРАВЛЕНИЕ ОШИБКИ: Явно принуждаем оси работать в одном режиме TwoConstants
        var vel = ps.velocityOverLifetime;
        vel.enabled = true;
        vel.x = new ParticleSystem.MinMaxCurve(-0.2f, 0.2f) { mode = ParticleSystemCurveMode.TwoConstants };
        vel.y = new ParticleSystem.MinMaxCurve(0.3f, 0.6f) { mode = ParticleSystemCurveMode.TwoConstants };
        vel.z = new ParticleSystem.MinMaxCurve(0f, 0f) { mode = ParticleSystemCurveMode.TwoConstants };

        var renderer = smokeObj.GetComponent<ParticleSystemRenderer>();
        renderer.material = new Material(Shader.Find("Sprites/Default"));
        renderer.sortingOrder = fireRenderer != null ? fireRenderer.sortingOrder + 2 : 2;
    }

    // ═══════════ УГОЛЬКИ ═══════════
    void SetupEmbers()
    {
        GameObject emberObj = new GameObject("Embers");
        emberObj.transform.SetParent(transform);
        emberObj.transform.localPosition = Vector3.zero;

        ParticleSystem ps = emberObj.AddComponent<ParticleSystem>();
        var main = ps.main;
        main.startLifetime = new ParticleSystem.MinMaxCurve(1f, 3f);
        main.startSpeed = new ParticleSystem.MinMaxCurve(0.3f, 1f);
        main.startSize = new ParticleSystem.MinMaxCurve(0.01f, 0.04f);
        main.startColor = emberColor;
        main.maxParticles = emberCount;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.gravityModifier = -0.15f;

        var emission = ps.emission;
        emission.rateOverTime = emberCount / 4f;

        // ИСПРАВЛЕНИЕ БАГА: В Unity поворот модуля формы настраивается через структуру вращения, а не прямой заменой Vector3
        var shape = ps.shape;
        shape.shapeType = ParticleSystemShapeType.Cone;
        shape.angle = 25f;
        shape.radius = 0.2f;
        shape.rotation = new Vector3(-90f, 0f, 0f);

        var noise = ps.noise;
        noise.enabled = true;
        noise.strength = 0.3f;
        noise.frequency = 2f;

        var colorLife = ps.colorOverLifetime;
        colorLife.enabled = true;
        Gradient grad = new Gradient();
        grad.SetKeys(
            new GradientColorKey[] {
                new GradientColorKey(new Color(1f, 0.6f, 0f), 0f),
                new GradientColorKey(new Color(1f, 0.1f, 0f), 0.7f),
                new GradientColorKey(Color.black, 1f)
            },
            new GradientAlphaKey[] {
                new GradientAlphaKey(0.8f, 0f),
                new GradientAlphaKey(0.5f, 0.7f),
                new GradientAlphaKey(0f, 1f)
            });
        colorLife.color = grad;

        var renderer = emberObj.GetComponent<ParticleSystemRenderer>();
        renderer.material = new Material(Shader.Find("Sprites/Default"));
        renderer.sortingOrder = fireRenderer != null ? fireRenderer.sortingOrder + 1 : 1;
    }

    // ═══════════ ЗВУК ═══════════
    void SetupAudio()
    {
        AudioSource audio = gameObject.AddComponent<AudioSource>();
        audio.clip = cracklingSound;
        audio.loop = true;
        audio.volume = soundVolume;
        audio.spatialBlend = 1f;      // 3D звук
        audio.maxDistance = 15f;
        audio.rolloffMode = AudioRolloffMode.Linear;
        audio.Play();
    }

    // ═══════════ ГЕНЕРАЦИЯ КРУГЛОГО СПРАЙТА ═══════════
    Sprite CreateCircleSprite(int resolution)
    {
        Texture2D tex = new Texture2D(resolution, resolution, TextureFormat.RGBA32, false);
        tex.filterMode = FilterMode.Bilinear;

        float center = resolution / 2f;
        float radius = resolution / 2f;

        for (int y = 0; y < resolution; y++)
        {
            for (int x = 0; x < resolution; x++)
            {
                float dist = Vector2.Distance(new Vector2(x, y), new Vector2(center, center));
                float alpha = Mathf.Clamp01(1f - (dist / radius));
                alpha = alpha * alpha * alpha;  // Мягкие края (кубическое затухание)
                tex.SetPixel(x, y, new Color(1f, 1f, 1f, alpha));
            }
        }

        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, resolution, resolution),
            new Vector2(0.5f, 0.5f), resolution);
    }
}