using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class PrototypeCardRewardManager : MonoBehaviour
{
    private enum CardEffect
    {
        TeamDamage,
        TeamHealth,
        TeamSpeed,
        FasterDash,
        FasterBarrage,
        RepairCampfire,
        FortifyCampfire,
        EngineerOverclock,
        HeavyImpact,
        EmergencyHeal,
        TeamReach,
        CombatTempo,
        DashDrive,
        HeavyGuard,
        AmmoScavenger,
        Hearthkeeper,
        Firebreak,
        HeavyComboMastery,
        HeavyIronNerves,
        HeavyBarrageExtension,
        HeavyShoulderCharge,
        EngineerWeightedWrench,
        EngineerWideArc,
        EngineerRapidMechanics,
        EngineerParryField
    }

    private enum CardAudience
    {
        Universal,
        Heavy,
        Engineer
    }

    private struct Card
    {
        public string Title;
        public string Body;
        public CardEffect Effect;
        public CardAudience Audience;
    }

    public bool showRewardsAfterWave = true;
    public int choicesPerReward = 3;

    [Header("Serialized HUD")]
    public GameObject rewardPanel;
    public TextMeshProUGUI rewardTitle;
    public Button[] cardButtons = new Button[3];
    public TextMeshProUGUI[] cardLabels = new TextMeshProUGUI[3];

    private readonly List<Card> deck = new List<Card>();
    private readonly List<int> eligibleCardIndices = new List<int>(32);
    private readonly Dictionary<CardEffect, int> stackCounts = new Dictionary<CardEffect, int>();
    private readonly Dictionary<PlayerController, PlayerBaseStats> playerBaseStats = new Dictionary<PlayerController, PlayerBaseStats>();
    private readonly Dictionary<EngineerController, EngineerBaseStats> engineerBaseStats = new Dictionary<EngineerController, EngineerBaseStats>();
    private WaveManager waveManager;
    private Card[] activeChoices;
    private bool rewardOpen;
    private float campfireBaseContactDamage = -1f;

    private class PlayerBaseStats
    {
        public float MaxHealth;
        public float JabDamage;
        public float CrossDamage;
        public float UppercutDamage;
        public float HeavyDamage;
        public float DashDamage;
        public float WalkSpeed;
        public float RunSpeed;
        public float DashCooldown;
        public float BarrageCooldown;
        public float BarrageDuration;
        public float AttackRange;
        public float LightAttackCooldown;
        public float HeavyAttackCooldown;
        public float DashSpeed;
        public float DashDuration;
        public float BlockDamageReduction;
        public float ComboWindow;
        public float InvulnerabilityTime;
    }

    private class EngineerBaseStats
    {
        public float MaxHealth;
        public float AttackDamage;
        public float ChargedDamage;
        public float MoveSpeed;
        public float RunSpeed;
        public float ChargeTime;
        public float AttackCooldown;
        public float AttackRange;
        public float KnockbackForce;
        public float ChargedKnockback;
        public float SwingAngle;
        public float ChargedSwingAngle;
        public float SwingAnticipation;
        public float SwingDuration;
        public float SwingRecovery;
        public float ParryFreezeDuration;
    }

    void Start()
    {
        BuildDeck();

        waveManager = FindAnyObjectByType<WaveManager>();
        if (waveManager != null)
            waveManager.OnWaveComplete += HandleWaveComplete;

        ResolveRewardHud();
        if (rewardPanel != null)
            rewardPanel.SetActive(false);
    }

    void OnDestroy()
    {
        if (waveManager != null)
            waveManager.OnWaveComplete -= HandleWaveComplete;
    }

    void Update()
    {
        if (!rewardOpen || activeChoices == null)
            return;

        Keyboard keyboard = Keyboard.current;
        if (keyboard == null)
            return;

        if (keyboard.digit1Key.wasPressedThisFrame || keyboard.numpad1Key.wasPressedThisFrame)
            SelectCard(0);
        else if (keyboard.digit2Key.wasPressedThisFrame || keyboard.numpad2Key.wasPressedThisFrame)
            SelectCard(1);
        else if (keyboard.digit3Key.wasPressedThisFrame || keyboard.numpad3Key.wasPressedThisFrame)
            SelectCard(2);
    }

    void HandleWaveComplete(int wave)
    {
        if (!showRewardsAfterWave)
            return;

        if (PrototypeRunStats.Instance != null && PrototypeRunStats.Instance.RunEnded)
            return;

        int hearthStacks = GetStacks(CardEffect.Hearthkeeper);
        if (hearthStacks > 0 && PrototypeCampfireHealth.Instance != null)
            PrototypeCampfireHealth.Instance.Repair(25f * hearthStacks);

        OpenReward(wave);
    }

    void BuildDeck()
    {
        deck.Clear();
        deck.Add(new Card { Title = "Sharper Blows", Body = "+20% base melee damage. Max 3.", Effect = CardEffect.TeamDamage });
        deck.Add(new Card { Title = "Second Wind", Body = "+25% base max health and heal. Max 2.", Effect = CardEffect.TeamHealth });
        deck.Add(new Card { Title = "Moonlit Boots", Body = "+15% base move speed. Max +30%.", Effect = CardEffect.TeamSpeed });
        deck.Add(new Card { Title = "Quick Step", Body = "Dash base cooldown -20%. Max -40%.", Effect = CardEffect.FasterDash, Audience = CardAudience.Heavy });
        deck.Add(new Card { Title = "Barrage Rhythm", Body = "Barrage base cooldown -20%. Max -40%.", Effect = CardEffect.FasterBarrage, Audience = CardAudience.Heavy });
        deck.Add(new Card { Title = "Kindling", Body = "Repair the campfire by 60 HP.", Effect = CardEffect.RepairCampfire });
        deck.Add(new Card { Title = "Stone Ring", Body = "+45 campfire max HP and heal it.", Effect = CardEffect.FortifyCampfire });
        deck.Add(new Card { Title = "Engineer Overclock", Body = "Engineer charges faster and hits harder. Max 2.", Effect = CardEffect.EngineerOverclock, Audience = CardAudience.Engineer });
        deck.Add(new Card { Title = "Heavy Impact", Body = "Heavy attacks and barrage hit harder. Max 2.", Effect = CardEffect.HeavyImpact, Audience = CardAudience.Heavy });
        deck.Add(new Card { Title = "Emergency Heal", Body = "Restore 45 HP to both players.", Effect = CardEffect.EmergencyHeal });
        deck.Add(new Card { Title = "Long Reach", Body = "+12% base attack range for both players. Max 2.", Effect = CardEffect.TeamReach });
        deck.Add(new Card { Title = "Battle Rhythm", Body = "Normal attack cooldowns -12%. Max 2.", Effect = CardEffect.CombatTempo });
        deck.Add(new Card { Title = "Dash Drive", Body = "Heavy dash speed +15% and duration +10%. Max 2.", Effect = CardEffect.DashDrive, Audience = CardAudience.Heavy });
        deck.Add(new Card { Title = "Braced Guard", Body = "Heavy block reduction +6 percentage points. Max 2.", Effect = CardEffect.HeavyGuard, Audience = CardAudience.Heavy });
        deck.Add(new Card { Title = "Scavenger Pouches", Body = "+4% ammo drop chance. Max 2.", Effect = CardEffect.AmmoScavenger });
        deck.Add(new Card { Title = "Hearthkeeper", Body = "Repair 25 now and after every cleared wave. Max 3.", Effect = CardEffect.Hearthkeeper });
        deck.Add(new Card { Title = "Firebreak", Body = "Campfire contact damage -15% of base. Max 2.", Effect = CardEffect.Firebreak });
        deck.Add(new Card { Title = "Combo Mastery", Body = "Heavy combo window +20%. Max 2.", Effect = CardEffect.HeavyComboMastery, Audience = CardAudience.Heavy });
        deck.Add(new Card { Title = "Iron Nerves", Body = "Heavy invulnerability after a hit +20%. Max 2.", Effect = CardEffect.HeavyIronNerves, Audience = CardAudience.Heavy });
        deck.Add(new Card { Title = "Endless Barrage", Body = "Heavy barrage duration +20% of base. Max 2.", Effect = CardEffect.HeavyBarrageExtension, Audience = CardAudience.Heavy });
        deck.Add(new Card { Title = "Shoulder Charge", Body = "Heavy dash damage +30% of base. Max 2.", Effect = CardEffect.HeavyShoulderCharge, Audience = CardAudience.Heavy });
        deck.Add(new Card { Title = "Weighted Wrench", Body = "Engineer knockback +25% of base. Max 2.", Effect = CardEffect.EngineerWeightedWrench, Audience = CardAudience.Engineer });
        deck.Add(new Card { Title = "Wide Arc", Body = "Engineer swing arcs become wider. Max 2.", Effect = CardEffect.EngineerWideArc, Audience = CardAudience.Engineer });
        deck.Add(new Card { Title = "Rapid Mechanics", Body = "Engineer swing animation 10% faster. Max 2.", Effect = CardEffect.EngineerRapidMechanics, Audience = CardAudience.Engineer });
        deck.Add(new Card { Title = "Parry Field", Body = "Engineer parry freeze +25% of base. Max 2.", Effect = CardEffect.EngineerParryField, Audience = CardAudience.Engineer });
    }

    void OpenReward(int wave)
    {
        rewardOpen = true;
        Time.timeScale = 0f;

        ResolveRewardHud();
        rewardPanel.SetActive(true);
        rewardTitle.text = $"Wave {wave} cleared\nPick one upgrade";

        activeChoices = RollChoices();

        for (int i = 0; i < cardButtons.Length; i++)
        {
            bool active = i < activeChoices.Length;
            cardButtons[i].gameObject.SetActive(active);
            if (!active)
                continue;

            int choiceIndex = i;
            Card card = activeChoices[i];
            string classLabel = card.Audience == CardAudience.Heavy
                ? " [HEAVY]"
                : card.Audience == CardAudience.Engineer ? " [ENGINEER]" : "";
            cardLabels[i].text = $"{i + 1}. {card.Title}{classLabel}\n\n{card.Body}";
            cardButtons[i].onClick.RemoveAllListeners();
            cardButtons[i].onClick.AddListener(() => SelectCard(choiceIndex));
        }
    }

    void ResolveRewardHud()
    {
        Canvas canvas = null;

        if (rewardPanel == null)
        {
            canvas = PrototypeArenaUi.GetOrCreateCanvas("PrototypeArenaHUD", 5500);
            Transform existing = canvas.transform.Find("RewardPanel");
            rewardPanel = existing != null
                ? existing.gameObject
                : PrototypeArenaUi.CreatePanel(
                    canvas.transform,
                    "RewardPanel",
                    new Color(0.025f, 0.03f, 0.035f, 0.94f),
                    new Vector2(0.5f, 0.5f),
                    new Vector2(0.5f, 0.5f),
                    Vector2.zero,
                    new Vector2(940f, 560f)).gameObject;
        }

        if (rewardTitle == null)
        {
            rewardTitle = PrototypeArenaUi.CreateText(
                rewardPanel.transform,
                "Title",
                "Wave cleared\nPick one upgrade",
                24,
                TextAlignmentOptions.Center,
                new Vector2(0.5f, 1f),
                new Vector2(0.5f, 1f),
                new Vector2(0f, -80f),
                new Vector2(780f, 110f));
        }

        if (cardButtons == null || cardButtons.Length != 3)
            cardButtons = new Button[3];
        if (cardLabels == null || cardLabels.Length != 3)
            cardLabels = new TextMeshProUGUI[3];

        for (int i = 0; i < 3; i++)
        {
            if (cardButtons[i] == null)
            {
                int choiceIndex = i;
                cardButtons[i] = PrototypeArenaUi.CreateButton(
                    rewardPanel.transform,
                    "CardButton" + (i + 1),
                    "Card " + (i + 1),
                    new Vector2(0.5f, 0.5f),
                    new Vector2(0.5f, 0.5f),
                    new Vector2(-300f + i * 300f, -40f),
                    new Vector2(260f, 270f),
                    () => SelectCard(choiceIndex));
            }

            if (cardLabels[i] == null)
            {
                Transform label = cardButtons[i].transform.Find("Label");
                cardLabels[i] = label != null ? label.GetComponent<TextMeshProUGUI>() : null;
            }

            if (cardLabels[i] == null)
            {
                cardLabels[i] = PrototypeArenaUi.CreateText(
                    cardButtons[i].transform,
                    "Label",
                    "Card " + (i + 1),
                    16,
                    TextAlignmentOptions.Center,
                    Vector2.zero,
                    Vector2.one,
                    Vector2.zero,
                    new Vector2(-24f, -24f));
            }
        }
    }

    Card[] RollChoices()
    {
        bool hasHeavy;
        bool hasEngineer;
        GetTeamComposition(out hasHeavy, out hasEngineer);

        eligibleCardIndices.Clear();
        for (int i = 0; i < deck.Count; i++)
        {
            if (IsEligibleForTeam(deck[i], hasHeavy, hasEngineer))
                eligibleCardIndices.Add(i);
        }

        int count = Mathf.Min(Mathf.Clamp(choicesPerReward, 1, 3), eligibleCardIndices.Count);
        Card[] choices = new Card[count];

        for (int i = 0; i < count; i++)
        {
            int pick = Random.Range(i, eligibleCardIndices.Count);
            int deckIndex = eligibleCardIndices[pick];
            eligibleCardIndices[pick] = eligibleCardIndices[i];
            eligibleCardIndices[i] = deckIndex;
            choices[i] = deck[deckIndex];
        }

        return choices;
    }

    static bool IsEligibleForTeam(Card card, bool hasHeavy, bool hasEngineer)
    {
        switch (card.Audience)
        {
            case CardAudience.Heavy:
                return hasHeavy;
            case CardAudience.Engineer:
                return hasEngineer;
            default:
                return true;
        }
    }

    void GetTeamComposition(out bool hasHeavy, out bool hasEngineer)
    {
        hasHeavy = false;
        hasEngineer = false;
        Registry.CleanupPlayers();

        for (int i = 0; i < Registry.Players.Count; i++)
        {
            Transform player = Registry.Players[i];
            if (player == null)
                continue;

            PlayerController heavy = player.GetComponent<PlayerController>();
            if (heavy == null) heavy = player.GetComponentInChildren<PlayerController>();
            EngineerController engineer = player.GetComponent<EngineerController>();
            if (engineer == null) engineer = player.GetComponentInChildren<EngineerController>();

            hasHeavy |= heavy != null;
            hasEngineer |= engineer != null;
        }

        // Rewards normally open only after both characters have registered.
        // This fallback keeps the deck usable in isolated test scenes.
        if (!hasHeavy && !hasEngineer)
        {
            hasHeavy = FindAnyObjectByType<PlayerController>() != null;
            hasEngineer = FindAnyObjectByType<EngineerController>() != null;
        }
    }

    void SelectCard(int index)
    {
        if (!rewardOpen || activeChoices == null || index < 0 || index >= activeChoices.Length)
            return;

        Card card = activeChoices[index];
        ApplyCard(card);

        if (PrototypeRunStats.Instance != null)
            PrototypeRunStats.Instance.RegisterCard(card.Title);

        if (rewardPanel != null)
            rewardPanel.SetActive(false);

        rewardOpen = false;
        activeChoices = null;
        Time.timeScale = 1f;
    }

    void ApplyCard(Card card)
    {
        int stack = AdvanceStack(card.Effect);

        switch (card.Effect)
        {
            case CardEffect.TeamDamage:
                RecalculateCombatStats();
                break;

            case CardEffect.TeamHealth:
                ApplyHealthStack(stack);
                break;

            case CardEffect.TeamSpeed:
                RecalculateCombatStats();
                break;

            case CardEffect.FasterDash:
                RecalculateCombatStats();
                break;

            case CardEffect.FasterBarrage:
                RecalculateCombatStats();
                break;

            case CardEffect.RepairCampfire:
                if (PrototypeCampfireHealth.Instance != null)
                    PrototypeCampfireHealth.Instance.Repair(60f);
                break;

            case CardEffect.FortifyCampfire:
                if (PrototypeCampfireHealth.Instance != null)
                    PrototypeCampfireHealth.Instance.IncreaseMaxHealth(45f);
                break;

            case CardEffect.EngineerOverclock:
                RecalculateCombatStats();
                break;

            case CardEffect.HeavyImpact:
                RecalculateCombatStats();
                break;

            case CardEffect.EmergencyHeal:
                ForEachPlayer(player => player.SetHealth(player.currentHealth + 45f, player.maxHealth));
                ForEachEngineer(engineer => engineer.SetHealth(engineer.currentHealth + 45f, engineer.maxHealth));
                break;

            case CardEffect.TeamReach:
            case CardEffect.CombatTempo:
            case CardEffect.DashDrive:
            case CardEffect.HeavyGuard:
                RecalculateCombatStats();
                break;

            case CardEffect.AmmoScavenger:
                if (waveManager != null)
                    waveManager.SetAmmoDropChanceBonus(0.04f * stack);
                break;

            case CardEffect.Hearthkeeper:
                if (PrototypeCampfireHealth.Instance != null)
                    PrototypeCampfireHealth.Instance.Repair(25f);
                break;

            case CardEffect.Firebreak:
                ApplyFirebreak(stack);
                break;

            case CardEffect.HeavyComboMastery:
            case CardEffect.HeavyIronNerves:
            case CardEffect.HeavyBarrageExtension:
            case CardEffect.HeavyShoulderCharge:
            case CardEffect.EngineerWeightedWrench:
            case CardEffect.EngineerWideArc:
            case CardEffect.EngineerRapidMechanics:
            case CardEffect.EngineerParryField:
                RecalculateCombatStats();
                break;
        }
    }

    int AdvanceStack(CardEffect effect)
    {
        int maxStacks = GetMaxStacks(effect);
        if (maxStacks == int.MaxValue)
            return 0;

        stackCounts.TryGetValue(effect, out int current);
        int next = Mathf.Min(maxStacks, current + 1);
        stackCounts[effect] = next;

        if (next >= maxStacks)
            deck.RemoveAll(candidate => candidate.Effect == effect);

        return next;
    }

    static int GetMaxStacks(CardEffect effect)
    {
        switch (effect)
        {
            case CardEffect.TeamDamage: return 3;
            case CardEffect.TeamHealth: return 2;
            case CardEffect.TeamSpeed: return 2;
            case CardEffect.FasterDash: return 2;
            case CardEffect.FasterBarrage: return 2;
            case CardEffect.EngineerOverclock: return 2;
            case CardEffect.HeavyImpact: return 2;
            case CardEffect.TeamReach: return 2;
            case CardEffect.CombatTempo: return 2;
            case CardEffect.DashDrive: return 2;
            case CardEffect.HeavyGuard: return 2;
            case CardEffect.AmmoScavenger: return 2;
            case CardEffect.Hearthkeeper: return 3;
            case CardEffect.Firebreak: return 2;
            case CardEffect.HeavyComboMastery: return 2;
            case CardEffect.HeavyIronNerves: return 2;
            case CardEffect.HeavyBarrageExtension: return 2;
            case CardEffect.HeavyShoulderCharge: return 2;
            case CardEffect.EngineerWeightedWrench: return 2;
            case CardEffect.EngineerWideArc: return 2;
            case CardEffect.EngineerRapidMechanics: return 2;
            case CardEffect.EngineerParryField: return 2;
            default: return int.MaxValue;
        }
    }

    int GetStacks(CardEffect effect)
    {
        return stackCounts.TryGetValue(effect, out int count) ? count : 0;
    }

    void RecalculateCombatStats()
    {
        int damageStacks = GetStacks(CardEffect.TeamDamage);
        int speedStacks = GetStacks(CardEffect.TeamSpeed);
        int dashStacks = GetStacks(CardEffect.FasterDash);
        int barrageStacks = GetStacks(CardEffect.FasterBarrage);
        int overclockStacks = GetStacks(CardEffect.EngineerOverclock);
        int heavyImpactStacks = GetStacks(CardEffect.HeavyImpact);
        int reachStacks = GetStacks(CardEffect.TeamReach);
        int tempoStacks = GetStacks(CardEffect.CombatTempo);
        int dashDriveStacks = GetStacks(CardEffect.DashDrive);
        int guardStacks = GetStacks(CardEffect.HeavyGuard);
        int comboStacks = GetStacks(CardEffect.HeavyComboMastery);
        int ironNervesStacks = GetStacks(CardEffect.HeavyIronNerves);
        int barrageExtensionStacks = GetStacks(CardEffect.HeavyBarrageExtension);
        int shoulderChargeStacks = GetStacks(CardEffect.HeavyShoulderCharge);
        int weightedWrenchStacks = GetStacks(CardEffect.EngineerWeightedWrench);
        int wideArcStacks = GetStacks(CardEffect.EngineerWideArc);
        int rapidMechanicsStacks = GetStacks(CardEffect.EngineerRapidMechanics);
        int parryFieldStacks = GetStacks(CardEffect.EngineerParryField);

        ForEachPlayer(player =>
        {
            PlayerBaseStats baseStats = GetPlayerBaseStats(player);
            float damageBonus = 0.2f * damageStacks;
            player.jabDamage = baseStats.JabDamage * (1f + damageBonus);
            player.crossDamage = baseStats.CrossDamage * (1f + damageBonus);
            player.uppercutDamage = baseStats.UppercutDamage * (1f + damageBonus);
            player.heavyDamage = baseStats.HeavyDamage * (1f + damageBonus + 0.35f * heavyImpactStacks);
            player.dashDamage = baseStats.DashDamage * (1f + damageBonus + 0.3f * shoulderChargeStacks);
            player.walkSpeed = baseStats.WalkSpeed * (1f + 0.15f * speedStacks);
            player.runSpeed = baseStats.RunSpeed * (1f + 0.15f * speedStacks);
            player.dashCooldown = Mathf.Max(0.25f, baseStats.DashCooldown * (1f - 0.2f * dashStacks));
            player.barrageCooldown = Mathf.Max(2f, baseStats.BarrageCooldown * (1f - 0.2f * barrageStacks));
            player.barrageDuration = baseStats.BarrageDuration * (1f + 0.15f * heavyImpactStacks + 0.2f * barrageExtensionStacks);
            player.attackRange = baseStats.AttackRange * (1f + 0.12f * reachStacks);
            player.lightAttackCooldown = Mathf.Max(0.2f, baseStats.LightAttackCooldown * (1f - 0.12f * tempoStacks));
            player.heavyAttackCooldown = Mathf.Max(0.5f, baseStats.HeavyAttackCooldown * (1f - 0.12f * tempoStacks));
            player.dashSpeed = baseStats.DashSpeed * (1f + 0.15f * dashDriveStacks);
            player.dashDuration = baseStats.DashDuration * (1f + 0.1f * dashDriveStacks);
            player.blockDamageReduction = Mathf.Clamp(baseStats.BlockDamageReduction + 0.06f * guardStacks, 0f, 0.9f);
            player.comboWindow = baseStats.ComboWindow * (1f + 0.2f * comboStacks);
            player.invulnerabilityTime = baseStats.InvulnerabilityTime * (1f + 0.2f * ironNervesStacks);
        });

        ForEachEngineer(engineer =>
        {
            EngineerBaseStats baseStats = GetEngineerBaseStats(engineer);
            float damageBonus = 0.2f * damageStacks;
            engineer.attackDamage = baseStats.AttackDamage * (1f + damageBonus);
            engineer.chargedDamage = baseStats.ChargedDamage * (1f + damageBonus + 0.35f * overclockStacks);
            engineer.moveSpeed = baseStats.MoveSpeed * (1f + 0.15f * speedStacks);
            engineer.runSpeed = baseStats.RunSpeed * (1f + 0.15f * speedStacks);
            engineer.chargeTime = Mathf.Max(0.8f, baseStats.ChargeTime * (1f - 0.25f * overclockStacks));
            engineer.attackCooldown = Mathf.Max(0.15f, baseStats.AttackCooldown * (1f - 0.15f * overclockStacks - 0.12f * tempoStacks));
            engineer.attackRange = baseStats.AttackRange * (1f + 0.12f * reachStacks);
            engineer.knockbackForce = baseStats.KnockbackForce * (1f + 0.25f * weightedWrenchStacks);
            engineer.chargedKnockback = baseStats.ChargedKnockback * (1f + 0.25f * weightedWrenchStacks);
            engineer.swingAngle = baseStats.SwingAngle * (1f + 0.15f * wideArcStacks);
            engineer.chargedSwingAngle = baseStats.ChargedSwingAngle * (1f + 0.1f * wideArcStacks);
            float swingSpeedMultiplier = Mathf.Max(0.65f, 1f - 0.1f * rapidMechanicsStacks);
            engineer.swingAnticipation = baseStats.SwingAnticipation * swingSpeedMultiplier;
            engineer.swingDuration = baseStats.SwingDuration * swingSpeedMultiplier;
            engineer.swingRecovery = baseStats.SwingRecovery * swingSpeedMultiplier;
            engineer.parryFreezeDuration = baseStats.ParryFreezeDuration * (1f + 0.25f * parryFieldStacks);
        });
    }

    void ApplyHealthStack(int stack)
    {
        ForEachPlayer(player =>
        {
            PlayerBaseStats baseStats = GetPlayerBaseStats(player);
            float nextMax = baseStats.MaxHealth * (1f + 0.25f * stack);
            float gainedCapacity = Mathf.Max(0f, nextMax - player.maxHealth);
            player.SetHealth(player.currentHealth + gainedCapacity, nextMax);
        });

        ForEachEngineer(engineer =>
        {
            EngineerBaseStats baseStats = GetEngineerBaseStats(engineer);
            float nextMax = baseStats.MaxHealth * (1f + 0.25f * stack);
            float gainedCapacity = Mathf.Max(0f, nextMax - engineer.maxHealth);
            engineer.SetHealth(engineer.currentHealth + gainedCapacity, nextMax);
        });
    }

    void ApplyFirebreak(int stack)
    {
        PrototypeCampfireHealth campfire = PrototypeCampfireHealth.Instance;
        if (campfire == null)
            return;

        if (campfireBaseContactDamage < 0f)
            campfireBaseContactDamage = campfire.contactDamagePerZombie;

        campfire.contactDamagePerZombie = Mathf.Max(1f, campfireBaseContactDamage * (1f - 0.15f * stack));
    }

    PlayerBaseStats GetPlayerBaseStats(PlayerController player)
    {
        if (playerBaseStats.TryGetValue(player, out PlayerBaseStats stats))
            return stats;

        stats = new PlayerBaseStats
        {
            MaxHealth = player.maxHealth,
            JabDamage = player.jabDamage,
            CrossDamage = player.crossDamage,
            UppercutDamage = player.uppercutDamage,
            HeavyDamage = player.heavyDamage,
            DashDamage = player.dashDamage,
            WalkSpeed = player.walkSpeed,
            RunSpeed = player.runSpeed,
            DashCooldown = player.dashCooldown,
            BarrageCooldown = player.barrageCooldown,
            BarrageDuration = player.barrageDuration,
            AttackRange = player.attackRange,
            LightAttackCooldown = player.lightAttackCooldown,
            HeavyAttackCooldown = player.heavyAttackCooldown,
            DashSpeed = player.dashSpeed,
            DashDuration = player.dashDuration,
            BlockDamageReduction = player.blockDamageReduction,
            ComboWindow = player.comboWindow,
            InvulnerabilityTime = player.invulnerabilityTime
        };
        playerBaseStats[player] = stats;
        return stats;
    }

    EngineerBaseStats GetEngineerBaseStats(EngineerController engineer)
    {
        if (engineerBaseStats.TryGetValue(engineer, out EngineerBaseStats stats))
            return stats;

        stats = new EngineerBaseStats
        {
            MaxHealth = engineer.maxHealth,
            AttackDamage = engineer.attackDamage,
            ChargedDamage = engineer.chargedDamage,
            MoveSpeed = engineer.moveSpeed,
            RunSpeed = engineer.runSpeed,
            ChargeTime = engineer.chargeTime,
            AttackCooldown = engineer.attackCooldown,
            AttackRange = engineer.attackRange,
            KnockbackForce = engineer.knockbackForce,
            ChargedKnockback = engineer.chargedKnockback,
            SwingAngle = engineer.swingAngle,
            ChargedSwingAngle = engineer.chargedSwingAngle,
            SwingAnticipation = engineer.swingAnticipation,
            SwingDuration = engineer.swingDuration,
            SwingRecovery = engineer.swingRecovery,
            ParryFreezeDuration = engineer.parryFreezeDuration
        };
        engineerBaseStats[engineer] = stats;
        return stats;
    }

    void ForEachPlayer(System.Action<PlayerController> action)
    {
        if (action == null)
            return;

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
                action(player);
        }
    }

    void ForEachEngineer(System.Action<EngineerController> action)
    {
        if (action == null)
            return;

        Registry.CleanupPlayers();
        for (int i = 0; i < Registry.Players.Count; i++)
        {
            Transform playerTransform = Registry.Players[i];
            if (playerTransform == null)
                continue;

            EngineerController engineer = playerTransform.GetComponent<EngineerController>();
            if (engineer == null)
                engineer = playerTransform.GetComponentInChildren<EngineerController>();

            if (engineer != null)
                action(engineer);
        }
    }
}
