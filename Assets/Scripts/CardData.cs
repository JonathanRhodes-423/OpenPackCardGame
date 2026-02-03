using UnityEngine;
using System;
using System.Globalization; // Required for the InvariantCulture fix

public enum Rarity { Common, Uncommon, Rare, Epic, Legendary }

[CreateAssetMenu(fileName = "NewCard", menuName = "Cards/CardData")]
public class CardData : ScriptableObject
{
    public string cardID;
    public string cardName;
    public Sprite artwork;
    public Rarity rarity;
    public float baseValue;

    [Header("Production Stats")]
    public string launchDateString;
    public int productionMonths;
    public int totalPrintRun;
    public int unopenedInPacks;

    [HideInInspector] public float currentMarketValue;

    public void InitializeSupply()
    {
        // The system calculates the 'Unopened' pool based on the 2026 timeline
        // Total Prints - (Natural production based on time)
        int naturallyProduced = GetCurrentSupply();
        unopenedInPacks = totalPrintRun - naturallyProduced;
    }

    public DateTime LaunchDate
    {
        get
        {
            if (DateTime.TryParse(launchDateString, CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime result))
            {
                return result;
            }
            return new DateTime(2026, 1, 1); // Fallback
        }
    }

    public DateTime EndDate => LaunchDate.AddMonths(productionMonths);

    public float GetValueBasedOnSupply()
    {
        float supplyPercent = GetCurrentSupply() / totalPrintRun;
        float supplyMultiplier = Mathf.Lerp(2.0f, 1.0f, supplyPercent);
        return baseValue * supplyMultiplier;
    }

    public int GetCurrentSupply()
    {
        return totalPrintRun - unopenedInPacks;
    }
}