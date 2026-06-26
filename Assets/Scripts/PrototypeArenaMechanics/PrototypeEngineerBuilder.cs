using TMPro;
using UnityEngine;

public class PrototypeEngineerBuilder : MonoBehaviour
{
    [Header("Build")]
    public float buildDistance = 1.35f;
    public float turretCooldown = 8f;
    public float dispenserCooldown = 12f;
    public int maxTurrets = 1;
    public int maxDispensers = 1;

    [Header("Editable Prefabs")]
    public GameObject turretPrefab;
    public GameObject dispenserPrefab;

    private EngineerController engineer;
    private PlayerSharedInput sharedInput;
    private PrototypeTurret activeTurret;
    private PrototypeDispenser activeDispenser;
    private float turretTimer;
    private float dispenserTimer;
    private TextMeshProUGUI hintText;

    void Awake()
    {
        engineer = GetComponent<EngineerController>();
        sharedInput = GetComponent<PlayerSharedInput>();
    }

    void Start()
    {
        CreateHint();
    }

    void Update()
    {
        if (engineer == null || engineer.currentHealth <= 0f)
            return;

        turretTimer = Mathf.Max(0f, turretTimer - Time.deltaTime);
        dispenserTimer = Mathf.Max(0f, dispenserTimer - Time.deltaTime);

        if (GetBuildPressed())
        {
            if (GetModifierHeld())
                TryBuildDispenser();
            else
                TryBuildTurret();
        }

        UpdateHint();
    }

    void TryBuildTurret()
    {
        if (turretTimer > 0f)
            return;

        if (activeTurret != null)
            Destroy(activeTurret.gameObject);

        Vector3 position = GetBuildPosition();
        if (turretPrefab == null)
        {
            Debug.LogWarning($"{nameof(PrototypeEngineerBuilder)} on {name} has no turret prefab assigned.", this);
            return;
        }

        GameObject turretObject = Instantiate(turretPrefab, position, Quaternion.identity);

        turretObject.name = "Prototype Turret P" + engineer.playerNumber;
        turretObject.transform.position = position;
        activeTurret = turretObject.GetComponent<PrototypeTurret>();
        if (activeTurret == null)
            activeTurret = turretObject.AddComponent<PrototypeTurret>();

        activeTurret.owner = transform;
        activeTurret.OnKill += OnSentryKill;
        turretTimer = turretCooldown;
        engineer.PlaySound(engineer.buildTurretVoiceLine);
        ArenaCamera.Shake(0.2f, 0.12f);
    }

    void TryBuildDispenser()
    {
        if (dispenserTimer > 0f)
            return;

        if (activeDispenser != null)
            Destroy(activeDispenser.gameObject);

        Vector3 position = GetBuildPosition();
        if (dispenserPrefab == null)
        {
            Debug.LogWarning($"{nameof(PrototypeEngineerBuilder)} on {name} has no dispenser prefab assigned.", this);
            return;
        }

        GameObject dispenserObject = Instantiate(dispenserPrefab, position, Quaternion.identity);

        dispenserObject.name = "Prototype Dispenser P" + engineer.playerNumber;
        dispenserObject.transform.position = position;
        activeDispenser = dispenserObject.GetComponent<PrototypeDispenser>();
        if (activeDispenser == null)
            activeDispenser = dispenserObject.AddComponent<PrototypeDispenser>();

        activeDispenser.owner = transform;
        dispenserTimer = dispenserCooldown;
        engineer.PlaySound(engineer.buildDispenserVoiceLine);
        ArenaCamera.Shake(0.15f, 0.1f);
    }

    Vector3 GetBuildPosition()
    {
        Vector2 direction = GetFacingDirection();
        return transform.position + (Vector3)(direction * buildDistance);
    }

    Vector2 GetFacingDirection()
    {
        if (engineer.targeting != null && engineer.targeting.currentTarget != null)
        {
            Vector2 toTarget = engineer.targeting.currentTarget.position - transform.position;
            if (toTarget.sqrMagnitude > 0.01f)
                return toTarget.normalized;
        }

        if (engineer.rb != null && engineer.rb.linearVelocity.sqrMagnitude > 0.05f)
            return engineer.rb.linearVelocity.normalized;

        if (engineer.spriteRenderer != null && engineer.spriteRenderer.flipX)
            return Vector2.left;

        return Vector2.right;
    }

    bool GetBuildPressed()
    {
        if (sharedInput != null)
            return sharedInput.GetActionDown(PlayerControlAction.Shoot);

        InputJoinManager.InputType type = GetInputType();
        switch (type)
        {
            case InputJoinManager.InputType.KeyboardWASD:
            case InputJoinManager.InputType.KeyboardArrows:
                return PlayerInputBindings.GetKeyboardActionDown(engineer.playerNumber, PlayerControlAction.Shoot);
            case InputJoinManager.InputType.Gamepad:
                return PlayerInputBindings.GetGamepadActionDown(engineer.playerNumber, PlayerControlAction.Shoot, GetGamepadIndex());
        }

        return false;
    }

    bool GetModifierHeld()
    {
        if (sharedInput != null)
            return sharedInput.GetAction(PlayerControlAction.Block);

        InputJoinManager.InputType type = GetInputType();
        switch (type)
        {
            case InputJoinManager.InputType.KeyboardWASD:
            case InputJoinManager.InputType.KeyboardArrows:
                return PlayerInputBindings.GetKeyboardAction(engineer.playerNumber, PlayerControlAction.Block);
            case InputJoinManager.InputType.Gamepad:
                return PlayerInputBindings.GetGamepadAction(engineer.playerNumber, PlayerControlAction.Block, GetGamepadIndex());
        }

        return false;
    }

    InputJoinManager.InputType GetInputType()
    {
        return engineer.playerNumber == 1 ? InputJoinManager.player1Input : InputJoinManager.player2Input;
    }

    int GetGamepadIndex()
    {
        return engineer.playerNumber == 1 ? InputJoinManager.player1GamepadIndex : InputJoinManager.player2GamepadIndex;
    }

    void CreateHint()
    {
        Canvas canvas = PrototypeArenaUi.GetOrCreateCanvas("PrototypeArenaHUD", 5500);
        hintText = PrototypeArenaUi.CreateText(
            canvas.transform,
            "EngineerBuildHint",
            "",
            18,
            TextAlignmentOptions.BottomLeft,
            new Vector2(0f, 0f),
            new Vector2(0f, 0f),
            new Vector2(24f, 22f + 52f * (engineer != null ? engineer.playerNumber - 1 : 0)),
            new Vector2(620f, 48f));
    }

    void UpdateHint()
    {
        if (hintText == null || engineer == null)
            return;

        string buildKey = GetBuildKeyName();
        string modifierKey = GetModifierKeyName();
        string turretReady = turretTimer <= 0f ? "ready" : Mathf.CeilToInt(turretTimer).ToString();
        string dispenserReady = dispenserTimer <= 0f ? "ready" : Mathf.CeilToInt(dispenserTimer).ToString();
        hintText.text = $"Engineer P{engineer.playerNumber}: {buildKey} turret ({turretReady}) | {modifierKey}+{buildKey} dispenser ({dispenserReady})";
    }

    string GetBuildKeyName()
    {
        if (GetInputType() == InputJoinManager.InputType.Gamepad)
            return PlayerInputBindings.GetGamepadControl(engineer.playerNumber, PlayerControlAction.Shoot).ToString();

        return PlayerInputBindings.GetKeyboardKey(engineer.playerNumber, PlayerControlAction.Shoot).ToString();
    }

    string GetModifierKeyName()
    {
        if (GetInputType() == InputJoinManager.InputType.Gamepad)
            return PlayerInputBindings.GetGamepadControl(engineer.playerNumber, PlayerControlAction.Block).ToString();

        return PlayerInputBindings.GetKeyboardKey(engineer.playerNumber, PlayerControlAction.Block).ToString();
    }

    void OnSentryKill() => engineer.PlaySound(engineer.sentryKillVoiceLine);
}
