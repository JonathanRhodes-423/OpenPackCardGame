using UnityEngine;
using System.Collections.Generic;

public class MarketManager : MonoBehaviour
{
    public List<CardData> allCards = new List<CardData>();

    // Pack prices are static, but cards are dynamic
    public float CalculateDynamicPrice(CardData card)
    {
        if (card.totalPrintRun <= 0) return card.baseValue;

        // Scarcity now looks at the Warehouse's global buffer
        // If the warehouse is full (100%), multiplier is 1.0x. 
        // If the warehouse is empty (0%), multiplier scales toward 2.0x.
        float totalProducedSoFar = (float)card.totalPrintRun;
        float remainingInWarehouse = (float)Warehouse.Instance.GlobalUnopenedPackBuffer;

        // We assume each card's individual scarcity is tied to the general 
        // availability of the packs they are found in.
        float unopenedRatio = Mathf.Clamp01(remainingInWarehouse / totalProducedSoFar);
        float scarcityMultiplier = 1.0f + (1.0f - unopenedRatio);

        return card.baseValue * scarcityMultiplier;
    }

    public void UpdateMarketPrices()
    {
        foreach (CardData card in allCards)
        {
            float supplyAdjustedValue = CalculateDynamicPrice(card);

            // Add daily 5% fluctuation for "market noise"
            float fluctuation = Random.Range(0.95f, 1.05f);
            card.currentMarketValue = supplyAdjustedValue * fluctuation;

            // Clamp prices (0.5x to 10x base value)
            card.currentMarketValue = Mathf.Clamp(card.currentMarketValue, card.baseValue * 0.5f, card.baseValue * 10f);
        }
    }
}