using UnityEngine;

public class DirectionalSprites : MonoBehaviour
{
    [System.Serializable]
    public class DirectionalFrames
    {
        public Sprite[] front;
        public Sprite[] back;
        public Sprite[] side;   // Для лево — flipX
    }

    [Header("═══ Анимации по направлениям ═══")]
    public DirectionalFrames idle;
    public DirectionalFrames walk;
    public DirectionalFrames lightAttack1;
    public DirectionalFrames lightAttack2;
    public DirectionalFrames heavyAttack;
    public DirectionalFrames hit;
    public DirectionalFrames death;

    [Header("═══ Компоненты ═══")]
    public PuppetAnimator puppet;
    public SpriteRenderer mainRenderer;

    private Vector2 lastDir = Vector2.down;
    private PuppetAnimator.AnimState lastState;

    void Start()
    {
        if (puppet == null) puppet = GetComponent<PuppetAnimator>();
        if (mainRenderer == null && puppet != null) mainRenderer = puppet.GetMainRenderer();
    }

    void LateUpdate()
    {
        if (puppet == null) return;

        // Определяем направление
        Vector2 dir = GetDirection();
        Direction facing = GetFacingDirection(dir);

        // Обновляем кадры если направление или состояние изменились
        PuppetAnimator.AnimState currentState = puppet.CurrentState;
        if (currentState != lastState || dir != lastDir)
        {
            UpdateFrames(currentState, facing);
            lastState = currentState;
            lastDir = dir;
        }

        // FlipX для стороны
        if (mainRenderer != null)
        {
            if (facing == Direction.Side)
                mainRenderer.flipX = dir.x < 0;
        }
    }

    enum Direction { Front, Back, Side }

    Direction GetFacingDirection(Vector2 dir)
    {
        if (Mathf.Abs(dir.x) > Mathf.Abs(dir.y))
            return Direction.Side;
        else if (dir.y > 0)
            return Direction.Back;
        else
            return Direction.Front;
    }

    Vector2 GetDirection()
    {
        // Берём направление из PuppetAnimator через рефлексию или публичное поле
        // Используем targetDir который PuppetAnimator хранит
        var field = typeof(PuppetAnimator).GetField("targetDir",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        if (field != null)
            return (Vector2)field.GetValue(puppet);

        return lastDir;
    }

    void UpdateFrames(PuppetAnimator.AnimState state, Direction dir)
    {
        DirectionalFrames frames = GetDirectionalFrames(state);
        if (frames == null) return;

        Sprite[] selected = GetFramesForDirection(frames, dir);
        if (selected == null || selected.Length == 0) return;

        // Подменяем кадры в PuppetAnimator
        switch (state)
        {
            case PuppetAnimator.AnimState.Idle:
                puppet.idle.frames = selected; break;
            case PuppetAnimator.AnimState.Walk:
            case PuppetAnimator.AnimState.Run:
                puppet.walk.frames = selected; break;
            case PuppetAnimator.AnimState.Jab:
                puppet.lightAttack1.frames = selected; break;
            case PuppetAnimator.AnimState.Cross:
            case PuppetAnimator.AnimState.Uppercut:
                puppet.lightAttack2.frames = selected; break;
            case PuppetAnimator.AnimState.Heavy:
            case PuppetAnimator.AnimState.BarrageCharging:
                puppet.heavyAttack.frames = selected; break;
            case PuppetAnimator.AnimState.Hit:
                puppet.hit.frames = selected; break;
            case PuppetAnimator.AnimState.Death:
                puppet.death.frames = selected; break;
        }
    }

    DirectionalFrames GetDirectionalFrames(PuppetAnimator.AnimState state)
    {
        switch (state)
        {
            case PuppetAnimator.AnimState.Idle: return idle;
            case PuppetAnimator.AnimState.Walk:
            case PuppetAnimator.AnimState.Run: return walk;
            case PuppetAnimator.AnimState.Jab: return lightAttack1;
            case PuppetAnimator.AnimState.Cross:
            case PuppetAnimator.AnimState.Uppercut: return lightAttack2;
            case PuppetAnimator.AnimState.Heavy:
            case PuppetAnimator.AnimState.BarrageCharging: return heavyAttack;
            case PuppetAnimator.AnimState.Hit: return hit;
            case PuppetAnimator.AnimState.Death: return death;
            default: return idle;
        }
    }

    Sprite[] GetFramesForDirection(DirectionalFrames frames, Direction dir)
    {
        switch (dir)
        {
            case Direction.Front:
                return frames.front != null && frames.front.Length > 0 ? frames.front : frames.side;
            case Direction.Back:
                return frames.back != null && frames.back.Length > 0 ? frames.back : frames.side;
            case Direction.Side:
                return frames.side != null && frames.side.Length > 0 ? frames.side : frames.front;
            default:
                return frames.front;
        }
    }
}