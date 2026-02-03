using UnityEngine;
using System.Collections.Generic;

[System.Serializable]
public class CardPack
{
    public string packName;
    public int cardCount;
    public float cost;
    public Sprite packArt;

    public List<CardData> Open(List<CardData> allCards)
    {
        List<CardData> pulledCards = new List<CardData>();
        List<CardData> availablePool = new List<CardData>();

        foreach (var card in allCards)
        {
            // The system "figures it out" by checking your existing logic:
            // Is today >= LaunchDate AND is there still stock left in the print run?
            int currentStock = card.totalPrintRun - card.GetCurrentSupply();

            if (System.DateTime.Now >= card.LaunchDate && currentStock > 0)
            {
                availablePool.Add(card);
            }
        }

        for (int i = 0; i < cardCount; i++)
        {
            if (availablePool.Count == 0) break;

            CardData pickedCard = GetRandomCardByRarity(availablePool);

            if (pickedCard != null)
            {
                pulledCards.Add(pickedCard);

                // Instead of manually updating a variable, we just remove it 
                // from the pool for this specific pack to keep it unique.
                availablePool.Remove(pickedCard);

                // Note: To make this persist globally, your GetCurrentSupply() 
                // would need to account for cards already pulled by the player.
            }
        }
        return pulledCards;
    }

    private CardData GetRandomCardByRarity(List<CardData> pool)
    {
        if (pool.Count == 0) return null;

        float roll = Random.Range(0f, 100f);
        Rarity targetRarity;

        if (roll < 1f) targetRarity = Rarity.Legendary;
        else if (roll < 5f) targetRarity = Rarity.Epic;
        else if (roll < 15f) targetRarity = Rarity.Rare;
        else if (roll < 40f) targetRarity = Rarity.Uncommon;
        else targetRarity = Rarity.Common;

        List<CardData> subPool = pool.FindAll(c => c.rarity == targetRarity);

        if (subPool.Count == 0) return pool[Random.Range(0, pool.Count)];

        return subPool[Random.Range(0, subPool.Count)];
    }
}