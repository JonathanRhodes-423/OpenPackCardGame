using UnityEngine;
using System.Collections.Generic;

[System.Serializable]
public class CardInventory
{
    // Dictionary <CardID, Quantity> for fast lookups
    public Dictionary<string, int> contents = new Dictionary<string, int>();
    public Dictionary<string, int> packContents = new Dictionary<string, int>();

    public void AddPack(string packName, int amount = 1)
    {
        if (packContents.ContainsKey(packName)) packContents[packName] += amount;
        else packContents.Add(packName, amount);
    }

    public void RemovePack(string packName, int amount = 1)
    {
        if (packContents.ContainsKey(packName))
        {
            packContents[packName] -= amount;
            if (packContents[packName] <= 0) packContents.Remove(packName);
        }
    }

    public void AddCard(string cardID, int amount = 1)
    {
        if (contents.ContainsKey(cardID)) contents[cardID] += amount;
        else contents.Add(cardID, amount);
    }

    public void RemoveCard(string cardID, int amount = 1)
    {
        if (contents.ContainsKey(cardID))
        {
            contents[cardID] -= amount;
            if (contents[cardID] <= 0) contents.Remove(cardID);
        }
    }
}