using UnityEngine;

public class DirectionalSprites : MonoBehaviour
{
    [System.Serializable]
    public class FourDirFrames
    {
        public Sprite[] front;    // Вниз (лицом к камере)
        public Sprite[] back;     // Вверх (спиной)
        public Sprite[] left;
        public Sprite[] right;
    }

    [Header("═══ Анимации 4 стороны ═══")]
    public FourDirFrames idle;
    public FourDirFrames walk;
    public FourDirFrames lightAttack1;
    public FourDirFrames lightAttack2;
    public FourDirFrames heavyAttack;
    public FourDirFrames hit;
    public FourDirFrames death;
    public FourDirFrames barrage;

    [Header("═══ Компоненты ═══")]
    public PuppetAnimator puppet;

    private Direction lastDir = Direction.Front;
    private PuppetAnimator.AnimState lastState;

    enum Direction { Front, Back, Left, Right }

    void Start()
    {
        if (puppet == null) puppet = GetComponent<PuppetAnimator>();
    }

    void LateUpdate()
    {
        if (puppet == null) return;

        Vector2 dir = GetTargetDir();
        Direction facing = GetFacing(dir);
        PuppetAnimator.AnimState state = puppet.CurrentState;

        if (state != lastState || facing != lastDir)
        {
            lastState = state;
            lastDir = facing;
            SwapFrames(state, facing);
        }
    }

    Vector2 GetTargetDir()
    {
        var field = typeof(PuppetAnimator).GetField("targetDir",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        if (field != null) return (Vector2)field.GetValue(puppet);
        return Vector2.down;
    }

    Direction GetFacing(Vector2 dir)
    {
        if (Mathf.Abs(dir.x) > Mathf.Abs(dir.y))
            return dir.x < 0 ? Direction.Left : Direction.Right;
        else
            return dir.y > 0 ? Direction.Back : Direction.Front;
    }

    void SwapFrames(PuppetAnimator.AnimState state, Direction dir)
    {
        FourDirFrames frames = GetFrameSet(state);
        if (frames == null) return;

        Sprite[] selected = Pick(frames, dir);
        if (selected == null || selected.Length == 0) return;

        switch (state)
        {
            case PuppetAnimator.AnimState.Idle: puppet.idle.frames = selected; break;
            case PuppetAnimator.AnimState.Walk:
            case PuppetAnimator.AnimState.Run: puppet.walk.frames = selected; break;
            case PuppetAnimator.AnimState.Jab: puppet.lightAttack1.frames = selected; break;
            case PuppetAnimator.AnimState.Cross:
            case PuppetAnimator.AnimState.Uppercut: puppet.lightAttack2.frames = selected; break;
            case PuppetAnimator.AnimState.Heavy:
            case PuppetAnimator.AnimState.BarrageCharging: puppet.heavyAttack.frames = selected; break;
            case PuppetAnimator.AnimState.Barrage: puppet.barrage.frames = selected; break;
            case PuppetAnimator.AnimState.Hit: puppet.hit.frames = selected; break;
            case PuppetAnimator.AnimState.Death: puppet.death.frames = selected; break;
        }
    }

    FourDirFrames GetFrameSet(PuppetAnimator.AnimState state)
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
            case PuppetAnimator.AnimState.Barrage: return barrage;
            case PuppetAnimator.AnimState.Hit: return hit;
            case PuppetAnimator.AnimState.Death: return death;
            default: return idle;
        }
    }

    Sprite[] Pick(FourDirFrames f, Direction dir)
    {
        Sprite[] result = null;
        switch (dir)
        {
            case Direction.Front: result = f.front; break;
            case Direction.Back: result = f.back; break;
            case Direction.Left: result = f.left; break;
            case Direction.Right: result = f.right; break;
        }
        // Фоллбэк если направление пустое
        if (result != null && result.Length > 0) return result;
        if (f.front != null && f.front.Length > 0) return f.front;
        if (f.right != null && f.right.Length > 0) return f.right;
        return null;
    }
}