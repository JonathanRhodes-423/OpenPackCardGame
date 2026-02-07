using System.Collections.Generic;

// Refactored from BotData.cs
public enum BotArchetype { Casual, Avid, Zealous }

[System.Serializable]
public struct BotData
{
    public int botID;
    public float budget;
    public float markup; // Added to store individual bot markup
    public BotArchetype archetype;
    public List<string> inventoryCardIDs;
}