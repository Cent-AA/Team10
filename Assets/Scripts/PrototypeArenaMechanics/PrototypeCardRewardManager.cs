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
        EmergencyHeal
    }

    private struct Card
    {
        public string Title;
        public string Body;
        public CardEffect Effect;
    }

    public bool showRewardsAfterWave = true;
    public int choicesPerReward = 3;

    private readonly List<Card> deck = new List<Card>();
    private WaveManager waveManager;
    private GameObject rewardPanel;
    private Card[] activeChoices;
    private bool rewardOpen;

    void Start()
    {
        BuildDeck();

        waveManager = FindFirstObjectByType<WaveManager>();
        if (waveManager != null)
            waveManager.OnWaveComplete += HandleWaveComplete;
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

        OpenReward(wave);
    }

    void BuildDeck()
    {
        deck.Add(new Card { Title = "Sharper Blows", Body = "+20% melee damage for both players.", Effect = CardEffect.TeamDamage });
        deck.Add(new Card { Title = "Second Wind", Body = "+25% max health and heal both players.", Effect = CardEffect.TeamHealth });
        deck.Add(new Card { Title = "Moonlit Boots", Body = "+15% move speed for both players.", Effect = CardEffect.TeamSpeed });
        deck.Add(new Card { Title = "Quick Step", Body = "Dash cooldown becomes 20% shorter.", Effect = CardEffect.FasterDash });
        deck.Add(new Card { Title = "Barrage Rhythm", Body = "Barrage cooldown becomes 20% shorter.", Effect = CardEffect.FasterBarrage });
        deck.Add(new Card { Title = "Kindling", Body = "Repair the campfire by 60 HP.", Effect = CardEffect.RepairCampfire });
        deck.Add(new Card { Title = "Stone Ring", Body = "+45 campfire max HP and heal it.", Effect = CardEffect.FortifyCampfire });
        deck.Add(new Card { Title = "Engineer Overclock", Body = "Engineer charges faster and hits harder.", Effect = CardEffect.EngineerOverclock });
        deck.Add(new Card { Title = "Heavy Impact", Body = "Heavy attacks and barrage hit harder.", Effect = CardEffect.HeavyImpact });
        deck.Add(new Card { Title = "Emergency Heal", Body = "Restore 45 HP to both players.", Effect = CardEffect.EmergencyHeal });
    }

    void OpenReward(int wave)
    {
        rewardOpen = true;
        Time.timeScale = 0f;

        Canvas canvas = PrototypeArenaUi.GetOrCreateCanvas("PrototypeArenaHUD", 5500);
        rewardPanel = PrototypeArenaUi.CreatePanel(
            canvas.transform,
            "WaveReward",
            new Color(0.025f, 0.03f, 0.035f, 0.94f),
            new Vector2(0.5f, 0.5f),
            new Vector2(0.5f, 0.5f),
            Vector2.zero,
            new Vector2(940f, 560f)).gameObject;

        PrototypeArenaUi.CreateText(
            rewardPanel.transform,
            "Title",
            $"Wave {wave} cleared\nPick one upgrade",
            34,
            TextAlignmentOptions.Center,
            new Vector2(0.5f, 1f),
            new Vector2(0.5f, 1f),
            new Vector2(0f, -80f),
            new Vector2(780f, 110f));

        activeChoices = RollChoices();

        for (int i = 0; i < activeChoices.Length; i++)
        {
            int choiceIndex = i;
            Card card = activeChoices[i];
            string label = $"{i + 1}. {card.Title}\n\n{card.Body}";
            PrototypeArenaUi.CreateButton(
                rewardPanel.transform,
                "Card" + i,
                label,
                new Vector2(0.5f, 0.5f),
                new Vector2(0.5f, 0.5f),
                new Vector2(-300f + i * 300f, -40f),
                new Vector2(260f, 270f),
                () => SelectCard(choiceIndex));
        }
    }

    Card[] RollChoices()
    {
        int count = Mathf.Clamp(choicesPerReward, 1, Mathf.Min(3, deck.Count));
        Card[] choices = new Card[count];
        HashSet<int> used = new HashSet<int>();

        for (int i = 0; i < count; i++)
        {
            int index = Random.Range(0, deck.Count);
            int guard = 0;
            while (used.Contains(index) && guard < 50)
            {
                index = Random.Range(0, deck.Count);
                guard++;
            }

            used.Add(index);
            choices[i] = deck[index];
        }

        return choices;
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
            Destroy(rewardPanel);

        rewardOpen = false;
        activeChoices = null;
        Time.timeScale = 1f;
    }

    void ApplyCard(Card card)
    {
        switch (card.Effect)
        {
            case CardEffect.TeamDamage:
                ForEachPlayer(player =>
                {
                    player.jabDamage *= 1.2f;
                    player.crossDamage *= 1.2f;
                    player.uppercutDamage *= 1.2f;
                    player.heavyDamage *= 1.2f;
                    player.dashDamage *= 1.2f;
                });
                ForEachEngineer(engineer =>
                {
                    engineer.attackDamage *= 1.2f;
                    engineer.chargedDamage *= 1.2f;
                });
                break;

            case CardEffect.TeamHealth:
                ForEachPlayer(player => player.MultiplyHealth(1.25f));
                ForEachEngineer(engineer => engineer.MultiplyHealth(1.25f));
                break;

            case CardEffect.TeamSpeed:
                ForEachPlayer(player =>
                {
                    player.walkSpeed *= 1.15f;
                    player.runSpeed *= 1.15f;
                });
                ForEachEngineer(engineer =>
                {
                    engineer.moveSpeed *= 1.15f;
                    engineer.runSpeed *= 1.15f;
                });
                break;

            case CardEffect.FasterDash:
                ForEachPlayer(player => player.dashCooldown = Mathf.Max(0.25f, player.dashCooldown * 0.8f));
                break;

            case CardEffect.FasterBarrage:
                ForEachPlayer(player => player.barrageCooldown = Mathf.Max(2f, player.barrageCooldown * 0.8f));
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
                ForEachEngineer(engineer =>
                {
                    engineer.chargeTime = Mathf.Max(0.8f, engineer.chargeTime * 0.75f);
                    engineer.chargedDamage *= 1.35f;
                    engineer.attackCooldown = Mathf.Max(0.15f, engineer.attackCooldown * 0.85f);
                });
                break;

            case CardEffect.HeavyImpact:
                ForEachPlayer(player =>
                {
                    player.heavyDamage *= 1.35f;
                    player.barrageDuration *= 1.15f;
                });
                break;

            case CardEffect.EmergencyHeal:
                ForEachPlayer(player => player.SetHealth(player.currentHealth + 45f, player.maxHealth));
                ForEachEngineer(engineer => engineer.SetHealth(engineer.currentHealth + 45f, engineer.maxHealth));
                break;
        }
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
