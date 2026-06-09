using UnityEngine;
using System.Collections;

public class PuppetAnimator : MonoBehaviour
{
    [System.Serializable]
    public class SpriteAnimationSettings
    {
        public Sprite[] frames;
        public float duration = 0.3f;
        public bool loop;
        [Range(0f, 1f)] public float hitFrame = 0.6f;
    }

    [System.Serializable]
    public class AttackEffectSettings
    {
        public Sprite sprite;
        public Sprite[] frames;
        public Vector2 size = new Vector2(2.2f, 2.2f);
        public float distance = 2.6f;
        public float duration = 0.22f;
        public int count = 1;
        public float spread = 0.35f;
        public float rotationOffset = -270f;
        public int sortingOrderOffset = 20;
        public Color startColor = new Color(1f, 1f, 1f, 0.85f);
        public Color endColor = new Color(1f, 1f, 1f, 0f);
    }

    [Header("Main Sprite")]
    public SpriteRenderer mainRenderer;
    public bool autoCreateMainRenderer = true;
    public bool hideLegacyBodySprites = true;
    public Vector3 mainRendererLocalPosition = new Vector3(0.25f, 0.287f, 0f);
    public Vector3 mainRendererLocalScale = Vector3.one;
    public int mainRendererSortingOrder = 0;
    public Sprite defaultSprite;

    [Header("Sprite Animations")]
    public SpriteAnimationSettings idle = new SpriteAnimationSettings { duration = 1f, loop = true };
    public SpriteAnimationSettings walk = new SpriteAnimationSettings { duration = 0.35f, loop = true };
    public SpriteAnimationSettings lightAttack1 = new SpriteAnimationSettings { duration = 0.3f, hitFrame = 0.35f };
    public SpriteAnimationSettings lightAttack2 = new SpriteAnimationSettings { duration = 0.3f, hitFrame = 0.35f };
    public SpriteAnimationSettings heavyAttack = new SpriteAnimationSettings { duration = 0.9f, hitFrame = 0.45f };
    public SpriteAnimationSettings barrage = new SpriteAnimationSettings { duration = 0.25f, loop = true };
    public SpriteAnimationSettings block = new SpriteAnimationSettings { duration = 0.2f };
    public SpriteAnimationSettings hit = new SpriteAnimationSettings { duration = 0.25f };
    public SpriteAnimationSettings death = new SpriteAnimationSettings { duration = 1f };

    [Header("Hit Effects")]
    public AttackEffectSettings lightAttack1Effect = new AttackEffectSettings();
    public AttackEffectSettings lightAttack2Effect = new AttackEffectSettings();
    public AttackEffectSettings heavyAttackEffect = new AttackEffectSettings
    {
        size = new Vector2(2.6f, 2.6f),
        distance = 3.8f,
        duration = 0.28f,
        count = 2
    };
    public AttackEffectSettings barrageEffect = new AttackEffectSettings
    {
        size = new Vector2(2.1f, 2.1f),
        distance = 3.2f,
        duration = 0.18f,
        count = 2
    };

    [Header("Barrage")]
    public float barrageChargeTime = 2f;
    public float barrageCircleAppearTime = 0.5f;
    public float barrageDuration = 4f;
    public float barrageHitInterval = 0.06f;
    public float barrageFlyDistance = 3f;
    public float barrageDamagePerHit = 3f;
    public Sprite chargeCircleSprite;

    [Header("Other States")]
    public float dashDuration = 0.25f;
    public float rollDuration = 0.5f;

    public enum AnimState
    {
        Idle, Walk, Run,
        Jab, Cross, Uppercut, Heavy,
        Dash, Block, Roll,
        Hit, Death, Barrage, BarrageCharging
    }

    private AnimState currentState = AnimState.Idle;
    private float stateTimer;
    private bool hitFrameTriggered;
    private bool isBlocking;
    private Transform currentTarget;
    private Vector2 targetDir = Vector2.right;
    private float chargeTimer;
    private bool isCharging;
    private GameObject chargeCircle;
    private SpriteRenderer chargeCircleRenderer;
    private Coroutine barrageRoutine;

    public System.Action OnHitFrame;
    public System.Action<float> OnAttackEnd;
    public System.Action<Vector2, float> OnBarrageHit;

    void Start()
    {
        EnsureMainRenderer();
        HideLegacyBodySprites();
        SetAnimationFrame(GetAnimationForState(currentState), 0f);
    }

    void Update()
    {
        stateTimer += Time.deltaTime;
        UpdateCurrentAnimation();

        switch (currentState)
        {
            case AnimState.Jab:
                UpdateAttack(lightAttack1, lightAttack1Effect, 0.3f, 0.1f);
                break;
            case AnimState.Cross:
            case AnimState.Uppercut:
                UpdateAttack(lightAttack2, lightAttack2Effect, 0.3f, 0.1f);
                break;
            case AnimState.Heavy:
                UpdateAttack(heavyAttack, heavyAttackEffect, 0.8f, 0.25f);
                break;
            case AnimState.BarrageCharging:
                UpdateCharge();
                break;
            case AnimState.Dash:
                if (stateTimer >= dashDuration) EndAttack();
                break;
            case AnimState.Roll:
                if (stateTimer >= rollDuration) EndAttack();
                break;
            case AnimState.Hit:
                if (stateTimer >= GetStateDuration(hit, 0.25f)) EndAttack();
                break;
        }
    }

    void EnsureMainRenderer()
    {
        if (mainRenderer == null)
            mainRenderer = FindMainRenderer();

        if (mainRenderer == null && autoCreateMainRenderer)
        {
            GameObject rendererObject = new GameObject("MainRenderer");
            rendererObject.transform.SetParent(transform, false);
            rendererObject.transform.localPosition = mainRendererLocalPosition;
            rendererObject.transform.localScale = mainRendererLocalScale;
            mainRenderer = rendererObject.AddComponent<SpriteRenderer>();
        }

        if (mainRenderer == null) return;

        mainRenderer.sortingOrder = mainRendererSortingOrder;
        if (defaultSprite != null && mainRenderer.sprite == null)
            mainRenderer.sprite = defaultSprite;
    }

    public SpriteRenderer GetMainRenderer()
    {
        EnsureMainRenderer();
        return mainRenderer;
    }

    SpriteRenderer FindMainRenderer()
    {
        SpriteRenderer[] renderers = GetComponentsInChildren<SpriteRenderer>(true);
        for (int i = 0; i < renderers.Length; i++)
        {
            SpriteRenderer sr = renderers[i];
            if (sr != null && sr.name.StartsWith("MainRender"))
                return sr;
        }

        return null;
    }

    void HideLegacyBodySprites()
    {
        if (!hideLegacyBodySprites) return;

        SpriteRenderer[] renderers = GetComponentsInChildren<SpriteRenderer>(true);
        for (int i = 0; i < renderers.Length; i++)
        {
            SpriteRenderer sr = renderers[i];
            if (sr == null || sr == mainRenderer) continue;

            string n = sr.name;
            if (n.StartsWith("Head") || n.StartsWith("Tors") || n.StartsWith("LeftArm") ||
                n.StartsWith("RightArm") || n.StartsWith("LeftLeg") || n.StartsWith("RightLeg"))
            {
                sr.enabled = false;
            }
        }
    }

    void SetState(AnimState state)
    {
        if (currentState == state) return;

        currentState = state;
        stateTimer = 0f;
        hitFrameTriggered = false;
        SetAnimationFrame(GetAnimationForState(state), 0f);
    }

    void UpdateCurrentAnimation()
    {
        SetAnimationFrame(GetAnimationForState(currentState), stateTimer);
    }

    SpriteAnimationSettings GetAnimationForState(AnimState state)
    {
        switch (state)
        {
            case AnimState.Walk:
            case AnimState.Run:
                return walk;
            case AnimState.Jab:
                return lightAttack1;
            case AnimState.Cross:
            case AnimState.Uppercut:
                return lightAttack2;
            case AnimState.Heavy:
            case AnimState.BarrageCharging:
                return heavyAttack;
            case AnimState.Barrage:
                return barrage;
            case AnimState.Block:
                return block;
            case AnimState.Hit:
                return hit;
            case AnimState.Death:
                return death;
            default:
                return idle;
        }
    }

    void SetAnimationFrame(SpriteAnimationSettings animation, float time)
    {
        if (mainRenderer == null || animation == null || animation.frames == null || animation.frames.Length == 0)
        {
            if (mainRenderer != null && defaultSprite != null)
                mainRenderer.sprite = defaultSprite;
            return;
        }

        float duration = Mathf.Max(0.01f, animation.duration);
        float normalizedTime = animation.loop ? Mathf.Repeat(time, duration) / duration : Mathf.Clamp01(time / duration);
        int frameIndex = Mathf.Clamp(Mathf.FloorToInt(normalizedTime * animation.frames.Length), 0, animation.frames.Length - 1);
        if (!animation.loop && normalizedTime >= 1f)
            frameIndex = animation.frames.Length - 1;

        Sprite frame = animation.frames[frameIndex];
        if (frame != null)
            mainRenderer.sprite = frame;
    }

    void UpdateAttack(SpriteAnimationSettings animation, AttackEffectSettings effect, float shakeAmount, float shakeDuration)
    {
        float duration = GetStateDuration(animation, 0.3f);
        float hitFrame = animation != null ? Mathf.Clamp01(animation.hitFrame) : 0.6f;
        if (!hitFrameTriggered && stateTimer >= duration * hitFrame)
            TriggerHitFrame(effect, shakeAmount, shakeDuration);

        if (stateTimer >= duration)
            EndAttack();
    }

    float GetStateDuration(SpriteAnimationSettings animation, float fallback)
    {
        return animation != null ? Mathf.Max(0.01f, animation.duration) : fallback;
    }

    void TriggerHitFrame(AttackEffectSettings effect, float shakeAmount, float shakeDuration)
    {
        hitFrameTriggered = true;
        SpawnHitEffect(effect);
        OnHitFrame?.Invoke();
        ArenaCamera.Shake(shakeAmount, shakeDuration);
    }

    void SpawnHitEffect(AttackEffectSettings effect)
    {
        if (effect == null || GetEffectFrame(effect, 0f) == null) return;

        Vector2 dir = targetDir.sqrMagnitude > 0.01f ? targetDir.normalized : Vector2.right;
        Vector2 side = new Vector2(-dir.y, dir.x);
        int count = Mathf.Max(1, effect.count);
        float startSide = -(count - 1) * 0.5f;

        for (int i = 0; i < count; i++)
        {
            Vector3 start = transform.position + (Vector3)(dir * 0.35f + side * ((startSide + i) * effect.spread));
            Vector3 end = start + (Vector3)(dir * effect.distance);
            StartCoroutine(HitEffectRoutine(effect, start, end, dir, i));
        }
    }

    IEnumerator HitEffectRoutine(AttackEffectSettings effect, Vector3 start, Vector3 end, Vector2 dir, int orderOffset)
    {
        GameObject effectObject = new GameObject("AttackHitSprite");
        Transform effectParent = transform.parent != null ? transform.parent : transform;
        effectObject.transform.SetParent(effectParent, true);

        SpriteRenderer sr = effectObject.AddComponent<SpriteRenderer>();
        sr.sprite = GetEffectFrame(effect, 0f);
        sr.sortingOrder = GetEffectSortingOrder(effect, orderOffset);

        float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg + effect.rotationOffset;
        effectObject.transform.rotation = Quaternion.Euler(0f, 0f, angle);

        float duration = Mathf.Max(0.01f, effect.duration);
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            float eased = 1f - (1f - t) * (1f - t);
            float pulse = Mathf.Lerp(0.75f, 1.15f, Mathf.Sin(t * Mathf.PI));

            effectObject.transform.position = Vector3.Lerp(start, end, eased);
            effectObject.transform.localScale = new Vector3(effect.size.x * pulse, effect.size.y * pulse, 1f);
            sr.sprite = GetEffectFrame(effect, t);
            sr.color = Color.Lerp(effect.startColor, effect.endColor, t);

            yield return null;
        }

        Destroy(effectObject);
    }

    Sprite GetEffectFrame(AttackEffectSettings effect, float normalizedTime)
    {
        if (effect == null) return null;

        if (effect.frames != null && effect.frames.Length > 0)
        {
            int frameIndex = Mathf.Clamp(Mathf.FloorToInt(Mathf.Clamp01(normalizedTime) * effect.frames.Length), 0, effect.frames.Length - 1);
            Sprite frame = effect.frames[frameIndex];
            if (frame != null) return frame;

            for (int i = 0; i < effect.frames.Length; i++)
                if (effect.frames[i] != null)
                    return effect.frames[i];
        }

        return effect.sprite;
    }

    int GetEffectSortingOrder(AttackEffectSettings effect, int orderOffset)
    {
        int baseOrder = mainRenderer != null ? mainRenderer.sortingOrder : 0;
        return baseOrder + effect.sortingOrderOffset + orderOffset;
    }

    void UpdateCharge()
    {
        chargeTimer += Time.deltaTime;
        if (chargeTimer >= barrageCircleAppearTime && chargeCircle == null)
            CreateChargeCircle();

        if (chargeCircleRenderer == null) return;

        float circleDuration = Mathf.Max(0.01f, barrageChargeTime - barrageCircleAppearTime);
        float circleT = Mathf.Clamp01((chargeTimer - barrageCircleAppearTime) / circleDuration);
        chargeCircleRenderer.color = Color.Lerp(new Color(1f, 1f, 1f, 0.3f), new Color(1f, 0f, 0f, 0.6f), circleT);
        float scale = 0.5f + circleT + Mathf.Sin(Time.time * 6f) * 0.1f;
        chargeCircle.transform.localScale = Vector3.one * scale;
    }

    public void StartBarrageCharge(float initialChargeTime = 0f)
    {
        if (IsBusy() && currentState != AnimState.BarrageCharging) return;

        chargeTimer = Mathf.Max(0f, initialChargeTime);
        isCharging = true;
        SetState(AnimState.BarrageCharging);
    }

    public void ReleaseBarrageCharge(bool forceBarrage = false, bool fallbackToHeavy = true)
    {
        if (!isCharging) return;
        isCharging = false;

        if (forceBarrage || chargeTimer >= barrageChargeTime)
        {
            DestroyChargeCircle();
            SetState(AnimState.Barrage);
            if (barrageRoutine != null) StopCoroutine(barrageRoutine);
            barrageRoutine = StartCoroutine(BarrageRoutine());
        }
        else
        {
            DestroyChargeCircle();
            SetState(fallbackToHeavy ? AnimState.Heavy : AnimState.Idle);
        }
    }

    IEnumerator BarrageRoutine()
    {
        float elapsed = 0f;
        float hitTimer = 0f;
        ArenaCamera.Shake(1.5f, barrageDuration);

        while (elapsed < barrageDuration)
        {
            elapsed += Time.deltaTime;
            hitTimer += Time.deltaTime;

            if (hitTimer >= barrageHitInterval)
            {
                hitTimer = 0f;
                SpawnHitEffect(barrageEffect);
                OnBarrageHit?.Invoke(targetDir, barrageDamagePerHit);
            }

            yield return null;
        }

        barrageRoutine = null;
        EndAttack();
    }

    void CreateChargeCircle()
    {
        chargeCircle = new GameObject("ChargeCircle");
        chargeCircle.transform.SetParent(transform, false);
        chargeCircle.transform.localPosition = Vector3.zero;

        chargeCircleRenderer = chargeCircle.AddComponent<SpriteRenderer>();
        chargeCircleRenderer.sprite = chargeCircleSprite != null ? chargeCircleSprite : CreateCircleSprite(64);
        chargeCircleRenderer.color = new Color(1f, 1f, 1f, 0.3f);
        chargeCircleRenderer.sortingOrder = mainRenderer != null ? mainRenderer.sortingOrder + 50 : 50;
    }

    void DestroyChargeCircle()
    {
        if (chargeCircle != null)
            Destroy(chargeCircle);

        chargeCircle = null;
        chargeCircleRenderer = null;
    }

    Sprite CreateCircleSprite(int size)
    {
        Texture2D texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
        float center = size / 2f;
        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float distance = Vector2.Distance(new Vector2(x, y), new Vector2(center, center)) / center;
                float alpha = Mathf.Clamp01(1f - distance);
                texture.SetPixel(x, y, new Color(1f, 1f, 1f, alpha * alpha));
            }
        }

        texture.Apply();
        return Sprite.Create(texture, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), size);
    }

    void EndAttack()
    {
        DestroyChargeCircle();
        OnAttackEnd?.Invoke(stateTimer);
        SetState(AnimState.Idle);
    }

    public bool IsBusy()
    {
        return currentState == AnimState.Jab ||
               currentState == AnimState.Cross ||
               currentState == AnimState.Uppercut ||
               currentState == AnimState.Heavy ||
               currentState == AnimState.Dash ||
               currentState == AnimState.Roll ||
               currentState == AnimState.Hit ||
               currentState == AnimState.Death ||
               currentState == AnimState.Barrage ||
               currentState == AnimState.BarrageCharging;
    }

    public bool IsDead() => currentState == AnimState.Death;
    public bool IsBarraging() => currentState == AnimState.Barrage;
    public bool IsBlocking() => isBlocking;
    public AnimState CurrentState => currentState;

    public void SetMoving(bool moving, bool run = false)
    {
        if (IsBusy() || isBlocking) return;
        SetState(moving ? (run ? AnimState.Run : AnimState.Walk) : AnimState.Idle);
    }

    public void SetTarget(Transform target, Vector2 dir)
    {
        currentTarget = target;
        if (dir.sqrMagnitude > 0.01f)
            targetDir = dir.normalized;
    }

    public void SetFacing(float dir)
    {
        if (Mathf.Abs(dir) > 0.1f)
            targetDir = new Vector2(Mathf.Sign(dir), 0f);
    }

    public void Jab() { if (!IsBusy() && !isBlocking) SetState(AnimState.Jab); }
    public void Cross() { if (!IsBusy() && !isBlocking) SetState(AnimState.Cross); }
    public void Uppercut() { if (!IsBusy() && !isBlocking) SetState(AnimState.Uppercut); }
    public void HeavyAttack() { if (!IsBusy() && !isBlocking) SetState(AnimState.Heavy); }
    public void Dash() { if (!IsBusy() && !isBlocking) SetState(AnimState.Dash); }
    public void Roll() { if (!IsBusy() && !isBlocking) SetState(AnimState.Roll); }
    public void StartBlock() { if (IsBusy()) return; isBlocking = true; SetState(AnimState.Block); }
    public void StopBlock() { isBlocking = false; if (currentState == AnimState.Block) SetState(AnimState.Idle); }
    public void TakeHit() { if (!IsDead()) SetState(AnimState.Hit); }
    public void Die() { SetState(AnimState.Death); }
}
