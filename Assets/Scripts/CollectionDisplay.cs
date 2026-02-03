using UnityEngine;
using System.Collections.Generic;

public class CollectionDisplay : MonoBehaviour
{
    [Header("Prefabs & Anchors")]
    public GameObject cardPrefab;
    public Transform anchor;

    [Header("Grid Settings")]
    public int cardsPerRow = 5;
    public float xSpacing = 1.2f;
    public float zSpacing = 1.5f;

    void Start()
    {
        DisplayCollection();
    }

    public void DisplayCollection()
    {
        // 1. Get the list from the InventoryManager
        List<CardData> collection = InventoryManager.Instance.ownedCards;

        // 2. Loop through the list and place them in a grid
        for (int i = 0; i < collection.Count; i++)
        {
            // Calculate grid position
            int row = i / cardsPerRow;
            int col = i % cardsPerRow;

            Vector3 spawnPos = anchor.position + new Vector3(col * xSpacing, 0, -row * zSpacing);

            // 3. Instantiate the card
            GameObject newCard = Instantiate(cardPrefab, spawnPos, anchor.rotation);

            // 4. Feed it the data
            CardDisplay display = newCard.GetComponent<CardDisplay>();
            if (display != null)
            {
                display.ApplyData(collection[i]);
                // We don't want them moving/following the mouse here, 
                // so we don't call .Reveal()
            }
        }
    }
}