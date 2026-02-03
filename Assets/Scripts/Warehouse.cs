using UnityEngine;
using System;
using System.Collections.Generic;

public class Warehouse : MonoBehaviour
{
    public static Warehouse Instance;

    [Header("Production Settings")]
    public DateTime ManufactureStartDate = new DateTime(2025, 1, 1);
    public int WeeklyPackProduction = 500; // Total packs created per week globally

    [Header("Global Stock")]
    public int GlobalUnopenedPackBuffer; // Packs made but not yet sold/opened
    public List<CardPack> MasterPackTemplates;

    void Awake() => Instance = this;

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
}