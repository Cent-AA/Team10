using System.Collections;
using TMPro;
using UnityEngine;

public class PrototypeClassRoleTuner : MonoBehaviour
{
    public bool tuneRolesOnStart = true;

    private bool tuned;
    private TextMeshProUGUI rolesText;

    void Start()
    {
        if (tuneRolesOnStart)
            StartCoroutine(TuneWhenPlayersExist());
    }

    IEnumerator TuneWhenPlayersExist()
    {
        float timeout = 5f;
        while (timeout > 0f)
        {
            Registry.CleanupPlayers();
            if (Registry.Players.Count > 0)
                break;

            timeout -= Time.deltaTime;
            yield return null;
        }

        TuneRoles();
    }

    void TuneRoles()
    {
        if (tuned)
            return;

        tuned = true;

        bool hasEngineer = false;
        Registry.CleanupPlayers();

        for (int i = 0; i < Registry.Players.Count; i++)
        {
            Transform playerTransform = Registry.Players[i];
            if (playerTransform == null)
                continue;

            PlayerController player = playerTransform.GetComponent<PlayerController>();
            if (player == null)
                player = playerTransform.GetComponentInChildren<PlayerController>();

            if (player != null)
            {
                if (player.playerNumber == 1)
                    TuneHeavy(player);
                else
                    TuneScoutFallback(player);

                continue;
            }

            EngineerController engineer = playerTransform.GetComponent<EngineerController>();
            if (engineer == null)
                engineer = playerTransform.GetComponentInChildren<EngineerController>();

            if (engineer != null)
            {
                hasEngineer = true;
                TuneEngineer(engineer);
            }
        }

        CreateRolesHud(hasEngineer);
    }

    void TuneHeavy(PlayerController player)
    {
        player.maxHealth = 145f;
        player.currentHealth = player.maxHealth;
        player.walkSpeed *= 0.95f;
        player.runSpeed *= 0.95f;
        player.blockDamageReduction = 0.82f;
        player.heavyDamage *= 1.25f;
        if (player.puppet != null)
            player.puppet.barrageDamagePerHit *= 1.2f;
        player.NotifyHealthChanged();
    }

    void TuneScoutFallback(PlayerController player)
    {
        player.maxHealth = 95f;
        player.currentHealth = player.maxHealth;
        player.walkSpeed *= 1.18f;
        player.runSpeed *= 1.18f;
        player.dashCooldown *= 0.75f;
        player.lightAttackCooldown *= 0.85f;
        player.NotifyHealthChanged();
    }

    void TuneEngineer(EngineerController engineer)
    {
        if (engineer == null)
            return;

        engineer.SetHealth(105f, 105f);
        engineer.moveSpeed *= 1.08f;
        engineer.runSpeed *= 1.08f;
        engineer.attackDamage *= 1.15f;
        engineer.chargedDamage *= 1.25f;
        engineer.chargeTime = Mathf.Max(1.5f, engineer.chargeTime * 0.85f);

        if (engineer.GetComponent<PrototypeEngineerBuilder>() == null)
            engineer.gameObject.AddComponent<PrototypeEngineerBuilder>();
    }

    void CreateRolesHud(bool hasEngineer)
    {
        Canvas canvas = PrototypeArenaUi.GetOrCreateCanvas("PrototypeArenaHUD", 5500);
        string p2Role = hasEngineer ? "P2 Engineer: charge attacks, faster repairs through cards" : "P2 Scout: fast attacks, fast dash";
        rolesText = PrototypeArenaUi.CreateText(
            canvas.transform,
            "RolesText",
            "P1 Heavy: tank, block, heavy barrage\n" + p2Role,
            20,
            TextAlignmentOptions.TopLeft,
            new Vector2(0f, 1f),
            new Vector2(0f, 1f),
            new Vector2(24f, -24f),
            new Vector2(540f, 110f));
    }
}
