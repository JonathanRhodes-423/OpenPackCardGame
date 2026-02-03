using UnityEngine;
using System.Collections.Generic;

public class BotManager : MonoBehaviour
{
    public List<BotData> activeBots = new List<BotData>();
    public MarketManager marketManager;

    public void InitializeBots(int count)
    {
        activeBots.Clear();
        for (int i = 0; i < count; i++)
        {
            BotData newBot = new BotData { botID = i, inventoryCardIDs = new List<string>() };
            float roll = Random.value;

            // Assign Archetypes based on rarity
            if (roll > 0.90f) newBot.archetype = BotArchetype.Zealous; // 10%
            else if (roll > 0.60f) newBot.archetype = BotArchetype.Avid; // 30%
            else newBot.archetype = BotArchetype.Casual; // 60%

            activeBots.Add(newBot);
        }
    }

    public void SimulateDailyPurchases()
    {
        int totalPacksBoughtToday = 0;

        foreach (var bot in activeBots)
        {
            int packsToBuy = DeterminePurchaseAmount(bot.archetype);

            // Validate against Warehouse stock
            int available = Warehouse.Instance.GlobalUnopenedPackBuffer;
            int finalPurchase = Mathf.Min(packsToBuy, available);

            if (finalPurchase > 0)
            {
                Warehouse.Instance.GlobalUnopenedPackBuffer -= finalPurchase;
                totalPacksBoughtToday += finalPurchase;

                // Simulate opening the packs and adding to bot inventory
                SimulateBotOpeningPacks(bot, finalPurchase);
            }
        }
        Debug.Log($"<color=orange>Daily Log:</color> Bots bought {totalPacksBoughtToday} packs.");
    }

    private int DeterminePurchaseAmount(BotArchetype archetype)
    {
        return archetype switch
        {
            BotArchetype.Zealous => Random.Range(3, 8), // High volume
            BotArchetype.Avid => (Random.value > 0.4f) ? Random.Range(1, 3) : 0, // Frequent
            BotArchetype.Casual => (Random.value > 0.8f) ? 1 : 0, // Rare
            _ => 0
        };
    }

    private void SimulateBotOpeningPacks(BotData bot, int count)
    {
        // For performance, we don't run the full PackOpener logic for every bot.
        // We just pick random cards from the MarketManager's pool.
        for (int i = 0; i < count * 5; i++) // Assuming 5 cards per pack
        {
            int index = Random.Range(0, marketManager.allCards.Count);
            bot.inventoryCardIDs.Add(marketManager.allCards[index].cardID);
        }
    }
}