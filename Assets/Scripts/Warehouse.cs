using UnityEngine;
using System;
using System.Collections.Generic;

public class Warehouse : MonoBehaviour
{
    public static Warehouse Instance;

    void Awake()
    {
        Instance = this;
        // Force initialize the backlog immediately so other scripts can use it in their Start()
        InitializeBacklog(DateTime.Now);
    }

    [Header("Production Settings")]
    public DateTime ManufactureStartDate = new DateTime(2025, 1, 1);
    public int WeeklyPackProduction = 500; // Total packs created per week globally

    [Header("Global Stock")]
    public int GlobalUnopenedPackBuffer; // Packs made but not yet sold/opened
    public List<CardPack> MasterPackTemplates;

    public void InitializeBacklog(DateTime currentInGameDate)
    {
        TimeSpan elapsed = currentInGameDate - ManufactureStartDate;
        int weeksElapsed = elapsed.Days / 7;

        // Calculate total packs produced since 2025
        GlobalUnopenedPackBuffer = weeksElapsed * WeeklyPackProduction;
        Debug.Log($"Warehouse: {GlobalUnopenedPackBuffer} packs produced since 2025.");
    }

    public List<CardPack> GetWeeklyShipment(int storeCapacity)
    {
        Debug.Log($"Warehouse: Attempting shipment. Buffer: {GlobalUnopenedPackBuffer}, Capacity: {storeCapacity}");
        // Determine how many packs the store gets vs what goes to "distributors" (bots)
        int shipmentSize = Mathf.Min(storeCapacity, GlobalUnopenedPackBuffer);
        GlobalUnopenedPackBuffer -= shipmentSize;

        // Return a list of pack instances for the store
        List<CardPack> shipment = new List<CardPack>();
        for (int i = 0; i < shipmentSize; i++)
        {
            shipment.Add(MasterPackTemplates[UnityEngine.Random.Range(0, MasterPackTemplates.Count)]);
        }
        return shipment;
    }

    // Add this to Warehouse.cs
    public int GetUnopenedCount(CardData card)
    {
        // 1. Determine the card's probability based on rarity 
        // (Matches the weights in CardPack.cs)
        float probability = card.rarity switch
        {
            Rarity.Legendary => 0.01f,
            Rarity.Epic => 0.04f,
            Rarity.Rare => 0.10f,
            Rarity.Uncommon => 0.25f,
            _ => 0.60f // Common
        };

        // 2. Estimate how many of this specific card are inside the unopened buffer
        // Calculation: (Total Packs Produced) * (Cards per Pack) * (Rarity Probability)
        // We assume an average of 5 cards per pack for the global estimate.
        int estimatedGlobalStock = Mathf.FloorToInt(GlobalUnopenedPackBuffer * 5 * probability);

        // 3. Ensure we don't exceed the intended total print run
        return Mathf.Clamp(estimatedGlobalStock, 0, card.totalPrintRun);
    }
}