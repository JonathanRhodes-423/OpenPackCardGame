using UnityEngine;
using System.Collections.Generic;

public class InventoryManager : MonoBehaviour
{
    // This 'Instance' allows other scripts to find the Inventory easily
    public static InventoryManager Instance;

    [Header("Collection Data")]
    // This is the actual list of cards the player owns
    public List<CardData> ownedCards = new List<CardData>();

    void Awake()
    {
        // Simple Singleton pattern: make sure there is only one vault
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject); // Keeps your cards saved if you switch scenes
        }
        else
        {
            Destroy(gameObject);
        }
    }

    public void AddCard(CardData cardData)
    {
        ownedCards.Add(cardData);
        Debug.Log($"<color=green>Inventory:</color> Added {cardData.cardName}. Total: {ownedCards.Count}");
    }
}