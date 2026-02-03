using UnityEngine;
using System.Collections.Generic;
using TMPro;

public class StoreManager : MonoBehaviour
{
    public CardInventory storeInventory = new CardInventory();
    public CardInventory playerInventory = new CardInventory();

    [Header("Player Economy")]
    public float playerMoney = 1000f;
    public TextMeshProUGUI moneyDisplay;

    [Header("Market Settings")]
    public bool allowDuplicates = true;
    public int shelfSpace = 16;
    public float storeMarkup = 1.1f; //
    public float storeMarkdown = 0.9f; //

    [Header("Dependencies")]
    public MarketManager marketManager;
    [HideInInspector] public StorefrontUIController uiController;

    private StoreCardSlot selectedPlayerCard;
    private StoreCardSlot selectedStoreCard;

    void Start()
    {
        UpdateMoneyUI(); //
        if (marketManager != null && marketManager.allCards.Count > 0)
        {
            marketManager.UpdateMarketPrices(); //
            // Initial seed for Day 1
            ReceiveSundayShipment();
        }
    }

    // --- NEW ECONOMY FEATURES ---

    public void ReceiveSundayShipment()
    {
        // 1. Clear old pack stock
        storeInventory.packContents.Clear();

        // 2. Request packs from Warehouse
        List<CardPack> shipment = Warehouse.Instance.GetWeeklyShipment(shelfSpace);

        foreach (CardPack pack in shipment)
        {
            storeInventory.AddPack(pack.packName);
        }

        // 3. Refresh Singles (Optional: Store gets new singles every Sunday too)
        SeedStoreSingles();

        if (uiController != null) uiController.RefreshUI();
        Debug.Log("<color=cyan>Store:</color> Sunday shipment arrived!");
    }

    private void SeedStoreSingles()
    {
        storeInventory.contents.Clear();
        // Fills the singles slots based on your existing logic
        for (int i = 0; i < shelfSpace; i++)
        {
            int randomIndex = Random.Range(0, marketManager.allCards.Count);
            storeInventory.AddCard(marketManager.allCards[randomIndex].cardID, 1);
        }
    }

    // --- PRICING & SELECTION ---

    public float GetStoreSellPrice(CardData card) => card.currentMarketValue * storeMarkup;
    public float GetStoreBuyPrice(CardData card) => card.currentMarketValue * storeMarkdown;

    public void HandleCardSelection(StoreCardSlot slot)
    {
        if (uiController == null) uiController = Object.FindAnyObjectByType<StorefrontUIController>();

        // Robust check: Is this slot a child of the Store Grid?
        bool isStoreSlot = slot.transform.IsChildOf(uiController.storeGridParent);

        if (!string.IsNullOrEmpty(slot.packName))
        {
            HandlePackInteraction(slot, isStoreSlot);
        }
        else
        {
            HandleSingleInteraction(slot, isStoreSlot);
        }
    }

    private void HandlePackInteraction(StoreCardSlot slot, bool isStoreSlot)
    {
        CardPack selectedPack = uiController.availablePacks.Find(p => p.packName == slot.packName);
        if (selectedPack == null) return;

        if (isStoreSlot)
        {
            // Verify store actually has this pack in stock from the Sunday shipment
            if (storeInventory.packContents.ContainsKey(selectedPack.packName))
            {
                PurchasePack(selectedPack);
            }
        }
        else
        {
            // Open pack from player inventory
            PackOpener opener = Object.FindAnyObjectByType<PackOpener>();
            if (opener != null) opener.StartPackOpening(selectedPack);
        }
    }

    private void HandleSingleInteraction(StoreCardSlot slot, bool isStoreSlot)
    {
        if (isStoreSlot)
        {
            selectedStoreCard = slot;
            PurchaseSingle(slot.cardData);
        }
        else
        {
            selectedPlayerCard = slot;
        }
    }

    // --- TRANSACTIONS ---

    public void PurchasePack(CardPack pack)
    {
        // Validate: Store must have it in inventory and Player must have money
        if (storeInventory.packContents.ContainsKey(pack.packName) && playerMoney >= pack.cost)
        {
            playerMoney -= pack.cost;
            storeInventory.RemovePack(pack.packName); // Remove from Store
            playerInventory.AddPack(pack.packName);    // Add to Player

            UpdateMoneyUI(); //
            uiController.RefreshUI(); //
            Debug.Log($"Successfully bought {pack.packName}!");
        }
        else
        {
            Debug.LogWarning("Purchase failed: Either no stock or not enough money.");
        }
    }

    public void PurchaseSingle(CardData card)
    {
        float price = GetStoreSellPrice(card);
        if (playerMoney >= price && storeInventory.contents.ContainsKey(card.cardID))
        {
            playerMoney -= price;
            storeInventory.RemoveCard(card.cardID);
            playerInventory.AddCard(card.cardID);
            UpdateMoneyUI();
            uiController.RefreshUI();
        }
    }

    public void ExecuteTrade()
    {
        if (selectedPlayerCard == null || selectedStoreCard == null) return;

        CardData pCard = selectedPlayerCard.cardData;
        CardData sCard = selectedStoreCard.cardData;

        if (GetStoreBuyPrice(pCard) >= GetStoreSellPrice(sCard))
        {
            playerInventory.RemoveCard(pCard.cardID);
            storeInventory.AddCard(pCard.cardID);
            storeInventory.RemoveCard(sCard.cardID);
            playerInventory.AddCard(sCard.cardID);

            selectedPlayerCard = null;
            selectedStoreCard = null;
            uiController.RefreshUI();
        }
    }

    public void UpdateMoneyUI()
    {
        if (moneyDisplay != null) moneyDisplay.text = $"${playerMoney:N0}";
    }

    private void OpenPack(CardPack pack)
    {
        PackOpener opener = Object.FindAnyObjectByType<PackOpener>();
        if (opener != null) opener.StartPackOpening(pack);
    }
}