using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Centralizes target choice, attack-slot ownership and crowd collision rules.
/// Zombies still move independently, but no longer compete for the same target
/// or perform their own full-scene target searches.
/// </summary>
public class EnemyDirector : MonoBehaviour
{
    public struct Assignment
    {
        public Transform PlayerTarget;
        public Vector2 Destination;
        public bool HasCampfireAttackSlot;
    }

    public static EnemyDirector Instance { get; private set; }

    [Header("Decision Budget")]
    [Min(0.05f)] public float decisionIntervalMin = 0.2f;
    [Min(0.05f)] public float decisionIntervalMax = 0.3f;

    [Header("Aggression Limits")]
    [Min(1)] public int maxAttackersPerPlayer = 2;
    [Min(1)] public int maxCampfireAttackers = 4;
    [Range(0.1f, 1f)] public float woundedTargetThreshold = 0.55f;

    [Header("Threat")]
    [Min(0f)] public float threatDecayPerSecond = 8f;
    [Min(0f)] public float blockingHeavyTauntBonus = 40f;
    [Min(0f)] public float distanceScoreWeight = 1f;
    [Min(0f)] public float targetSwitchScoreMargin = 6f;

    [Header("Formation")]
    [Min(0.5f)] public float campfireSurroundRadius = 2.8f;
    [Min(4)] public int surroundSlotsPerRing = 12;
    [Min(0.1f)] public float surroundRingSpacing = 0.75f;

    private Transform campfireTarget;
    private ZombieAI woundedRunnerHunter;
    private float cleanupTimer;

    private readonly Dictionary<ZombieAI, Transform> playerAssignments = new Dictionary<ZombieAI, Transform>();
    private readonly Dictionary<Transform, int> playerAttackerCounts = new Dictionary<Transform, int>();
    private readonly HashSet<ZombieAI> campfireAttackers = new HashSet<ZombieAI>();
    private readonly Dictionary<Transform, float> playerThreat = new Dictionary<Transform, float>();
    private readonly List<Transform> threatKeys = new List<Transform>(4);

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(this);
            return;
        }

        Instance = this;
    }

    void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    void Update()
    {
        DecayThreat(Time.deltaTime);

        cleanupTimer -= Time.deltaTime;
        if (cleanupTimer <= 0f)
        {
            cleanupTimer = 1f;
            CleanupAssignments();
        }
    }

    public void Initialize(Transform newCampfireTarget)
    {
        campfireTarget = newCampfireTarget;
    }

    public float GetNextDecisionDelay()
    {
        float min = Mathf.Max(0.05f, decisionIntervalMin);
        float max = Mathf.Max(min, decisionIntervalMax);
        return Random.Range(min, max);
    }

    public Assignment Evaluate(ZombieAI zombie)
    {
        if (zombie == null)
            return default;

        ReleaseInvalidAssignment(zombie);

        Transform assignedPlayer = GetAssignedPlayer(zombie);
        bool isRunner = zombie.Archetype == PrototypeEnemyVariant.VariantType.Runner;
        if (isRunner)
        {
            Transform wounded = FindWoundedPlayer();
            if (wounded == null && woundedRunnerHunter == zombie)
                woundedRunnerHunter = null;
            if (wounded != null && wounded != assignedPlayer && (woundedRunnerHunter == null || woundedRunnerHunter == zombie))
            {
                woundedRunnerHunter = zombie;
                if (TryAssignPlayer(zombie, wounded))
                    return BuildPlayerAssignment(zombie, wounded);
            }
        }

        if (assignedPlayer != null)
        {
            float currentScore = GetPlayerScore(zombie, assignedPlayer);
            Transform betterPlayer = FindBestAvailablePlayer(zombie, assignedPlayer, out float betterScore);
            if (betterPlayer != null && betterPlayer != assignedPlayer && betterScore > currentScore + targetSwitchScoreMargin)
            {
                if (TryAssignPlayer(zombie, betterPlayer))
                    return BuildPlayerAssignment(zombie, betterPlayer);
            }

            return BuildPlayerAssignment(zombie, assignedPlayer);
        }

        // Roughly one third of non-runners press the objective while slots are free.
        bool objectiveRole = !isRunner && PositiveModulo(zombie.GetInstanceID(), 3) == 0;
        if (objectiveRole && TryAssignCampfire(zombie))
            return BuildCampfireAssignment(zombie, true);

        Transform bestPlayer = FindBestAvailablePlayer(zombie, null, out _);
        if (bestPlayer != null && TryAssignPlayer(zombie, bestPlayer))
            return BuildPlayerAssignment(zombie, bestPlayer);

        if (TryAssignCampfire(zombie))
            return BuildCampfireAssignment(zombie, true);

        return BuildCampfireAssignment(zombie, false);
    }

    public void ReportThreat(Transform attacker, float amount)
    {
        Transform player = ResolveRegisteredPlayer(attacker);
        if (player == null)
            return;

        playerThreat.TryGetValue(player, out float current);
        playerThreat[player] = current + Mathf.Max(0f, amount);
    }

    public bool HasCampfireAttackSlot(ZombieAI zombie)
    {
        return zombie != null && campfireAttackers.Contains(zombie);
    }

    public void ReleaseZombie(ZombieAI zombie)
    {
        if (zombie == null)
            return;

        ReleasePlayerAssignment(zombie);
        campfireAttackers.Remove(zombie);

        if (woundedRunnerHunter == zombie)
            woundedRunnerHunter = null;
    }

    /// <summary>
    /// Ordinary enemies ignore hard body collisions and use soft separation.
    /// Tanks remain blocking. This runs only when an enemy is spawned/reused.
    /// </summary>
    public void RefreshCollisionRules(ZombieAI zombie)
    {
        if (zombie == null || zombie.CachedCollider == null)
            return;

        Registry.CleanupZombies();
        for (int i = 0; i < Registry.Zombies.Count; i++)
        {
            ZombieAI other = Registry.Zombies[i];
            if (other == null || other == zombie || other.CachedCollider == null)
                continue;

            bool ignore = !zombie.IsTank && !other.IsTank;
            Physics2D.IgnoreCollision(zombie.CachedCollider, other.CachedCollider, ignore);
        }
    }

    Assignment BuildPlayerAssignment(ZombieAI zombie, Transform player)
    {
        float angle = StableAngle(zombie.GetInstanceID());
        float radius = Mathf.Max(0.35f, zombie.attackRange * 0.72f);
        Vector2 offset = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * radius;
        return new Assignment
        {
            PlayerTarget = player,
            Destination = (Vector2)player.position + offset,
            HasCampfireAttackSlot = false
        };
    }

    Assignment BuildCampfireAssignment(ZombieAI zombie, bool attackSlot)
    {
        Vector2 center = campfireTarget != null ? campfireTarget.position : Vector2.zero;
        if (attackSlot)
        {
            float angle = StableAngle(zombie.GetInstanceID());
            return new Assignment
            {
                Destination = center + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * 0.75f,
                HasCampfireAttackSlot = true
            };
        }

        int slot = PositiveModulo(zombie.GetInstanceID(), Mathf.Max(4, surroundSlotsPerRing * 3));
        int ring = slot / Mathf.Max(4, surroundSlotsPerRing);
        float slotAngle = (slot % Mathf.Max(4, surroundSlotsPerRing)) * Mathf.PI * 2f / Mathf.Max(4, surroundSlotsPerRing);
        float radius = campfireSurroundRadius + ring * surroundRingSpacing;
        return new Assignment
        {
            Destination = center + new Vector2(Mathf.Cos(slotAngle), Mathf.Sin(slotAngle)) * radius,
            HasCampfireAttackSlot = false
        };
    }

    Transform FindBestAvailablePlayer(ZombieAI zombie, Transform currentTarget, out float bestScore)
    {
        Registry.CleanupPlayers();

        Transform best = null;
        bestScore = float.MinValue;
        for (int i = 0; i < Registry.Players.Count; i++)
        {
            Transform player = Registry.Players[i];
            int occupiedSlots = GetAttackerCount(player) - (player == currentTarget ? 1 : 0);
            if (!IsValidPlayer(player) || occupiedSlots >= maxAttackersPerPlayer)
                continue;

            float score = GetPlayerScore(zombie, player);
            if (score > bestScore)
            {
                bestScore = score;
                best = player;
            }
        }

        return best;
    }

    float GetPlayerScore(ZombieAI zombie, Transform player)
    {
        if (zombie == null || player == null)
            return float.MinValue;

        float distance = Vector2.Distance(zombie.transform.position, player.position);
        playerThreat.TryGetValue(player, out float threat);
        PlayerController heavy = Registry.GetPlayerController(player);
        float taunt = heavy != null && heavy.IsBlocking ? blockingHeavyTauntBonus : 0f;
        return threat + taunt - distance * distanceScoreWeight;
    }

    Transform FindWoundedPlayer()
    {
        Registry.CleanupPlayers();

        Transform wounded = null;
        float lowestRatio = woundedTargetThreshold;
        for (int i = 0; i < Registry.Players.Count; i++)
        {
            Transform player = Registry.Players[i];
            if (!IsValidPlayer(player) || GetAttackerCount(player) >= maxAttackersPerPlayer)
                continue;

            float ratio = GetHealthRatio(player);
            if (ratio < lowestRatio)
            {
                lowestRatio = ratio;
                wounded = player;
            }
        }

        return wounded;
    }

    bool TryAssignPlayer(ZombieAI zombie, Transform player)
    {
        if (zombie == null || !IsValidPlayer(player) || GetAttackerCount(player) >= maxAttackersPerPlayer)
            return false;

        campfireAttackers.Remove(zombie);
        ReleasePlayerAssignment(zombie);
        playerAssignments[zombie] = player;
        playerAttackerCounts[player] = GetAttackerCount(player) + 1;
        return true;
    }

    bool TryAssignCampfire(ZombieAI zombie)
    {
        if (zombie == null || campfireTarget == null)
            return false;

        if (campfireAttackers.Contains(zombie))
            return true;

        if (campfireAttackers.Count >= maxCampfireAttackers)
            return false;

        ReleasePlayerAssignment(zombie);
        campfireAttackers.Add(zombie);
        return true;
    }

    Transform GetAssignedPlayer(ZombieAI zombie)
    {
        if (!playerAssignments.TryGetValue(zombie, out Transform player))
            return null;

        return IsValidPlayer(player) ? player : null;
    }

    void ReleaseInvalidAssignment(ZombieAI zombie)
    {
        if (playerAssignments.TryGetValue(zombie, out Transform player) && !IsValidPlayer(player))
            ReleasePlayerAssignment(zombie);

        if (woundedRunnerHunter != null && (!woundedRunnerHunter.IsAlive || !woundedRunnerHunter.gameObject.activeInHierarchy))
            woundedRunnerHunter = null;
    }

    void ReleasePlayerAssignment(ZombieAI zombie)
    {
        if (!playerAssignments.TryGetValue(zombie, out Transform player))
            return;

        playerAssignments.Remove(zombie);
        int count = GetAttackerCount(player) - 1;
        if (count > 0)
            playerAttackerCounts[player] = count;
        else
            playerAttackerCounts.Remove(player);
    }

    int GetAttackerCount(Transform player)
    {
        return player != null && playerAttackerCounts.TryGetValue(player, out int count) ? count : 0;
    }

    void CleanupAssignments()
    {
        Registry.CleanupZombies();

        for (int i = Registry.Zombies.Count - 1; i >= 0; i--)
        {
            ZombieAI zombie = Registry.Zombies[i];
            if (zombie == null || !zombie.IsAlive || !zombie.gameObject.activeInHierarchy)
                ReleaseZombie(zombie);
        }

        if (woundedRunnerHunter != null && !woundedRunnerHunter.IsAlive)
            woundedRunnerHunter = null;
    }

    void DecayThreat(float deltaTime)
    {
        if (playerThreat.Count == 0)
            return;

        threatKeys.Clear();
        foreach (KeyValuePair<Transform, float> pair in playerThreat)
            threatKeys.Add(pair.Key);

        float decay = Mathf.Max(0f, threatDecayPerSecond) * deltaTime;
        for (int i = 0; i < threatKeys.Count; i++)
        {
            Transform player = threatKeys[i];
            if (player == null)
            {
                playerThreat.Remove(player);
                continue;
            }

            float next = playerThreat[player] - decay;
            if (next <= 0f)
                playerThreat.Remove(player);
            else
                playerThreat[player] = next;
        }
    }

    bool IsValidPlayer(Transform player)
    {
        if (player == null || !player.gameObject.activeInHierarchy)
            return false;

        PlayerController heavy = Registry.GetPlayerController(player);
        if (heavy != null)
            return heavy.currentHealth > 0f;

        EngineerController engineer = player.GetComponent<EngineerController>();
        if (engineer == null) engineer = player.GetComponentInChildren<EngineerController>();
        if (engineer == null) engineer = player.GetComponentInParent<EngineerController>();
        return engineer != null && engineer.currentHealth > 0f;
    }

    float GetHealthRatio(Transform player)
    {
        PlayerController heavy = Registry.GetPlayerController(player);
        if (heavy != null)
            return heavy.maxHealth > 0f ? heavy.currentHealth / heavy.maxHealth : 1f;

        EngineerController engineer = player.GetComponent<EngineerController>();
        if (engineer == null) engineer = player.GetComponentInChildren<EngineerController>();
        if (engineer == null) engineer = player.GetComponentInParent<EngineerController>();
        return engineer != null && engineer.maxHealth > 0f ? engineer.currentHealth / engineer.maxHealth : 1f;
    }

    Transform ResolveRegisteredPlayer(Transform candidate)
    {
        if (candidate == null)
            return null;

        Registry.CleanupPlayers();
        for (int i = 0; i < Registry.Players.Count; i++)
        {
            Transform player = Registry.Players[i];
            if (player == null)
                continue;

            if (candidate == player || candidate.IsChildOf(player) || player.IsChildOf(candidate))
                return player;
        }

        return null;
    }

    static float StableAngle(int id)
    {
        return PositiveModulo(id, 360) * Mathf.Deg2Rad;
    }

    static int PositiveModulo(int value, int modulus)
    {
        if (modulus <= 0)
            return 0;

        int result = value % modulus;
        return result < 0 ? result + modulus : result;
    }
}
