using UnityEngine;
using System.Collections.Generic;

[System.Serializable]

public class CardInventory
{
    public Dictionary<string, int> contents = new Dictionary<string, int>(); // Store's stock
    public Dictionary<string, int> packContents = new Dictionary<string, int>();
    public List<CardInstance> cardInstances = new List<CardInstance>(); // Player's unique cards

    public void AddPack(string packName, int amount = 1)
    {
        if (packContents.ContainsKey(packName)) packContents[packName] += amount;
        else packContents.Add(packName, amount);
    }

    public void RemovePack(string packName)
    {
        if (packContents.ContainsKey(packName))
        {
            packContents[packName]--;
            if (packContents[packName] <= 0) packContents.Remove(packName);
        }
    }

    // Used by PackOpener or Trade reimbursement
    public void AddCardInstance(CardData data, string packID, int index)
    {
        cardInstances.Add(new CardInstance(data, packID, index));
    }

    public void RemoveCardInstance(string uid)
    {
        // Find the specific instance in the list
        CardInstance toRemove = cardInstances.Find(c => c.instanceID == uid);

        if (toRemove != null)
        {
            // .Remove() only deletes the specific reference found
            cardInstances.Remove(toRemove);
        }
    }

    // Store logic (Generic ID counts)
    public void AddCard(string cardID, int amount = 1)
    {
        if (contents.ContainsKey(cardID)) contents[cardID] += amount;
        else contents.Add(cardID, amount);
    }

    public void RemoveCard(string cardID)
    {
        if (contents.ContainsKey(cardID))
        {
            contents[cardID]--;
            if (contents[cardID] <= 0) contents.Remove(cardID);
        }
    }
}