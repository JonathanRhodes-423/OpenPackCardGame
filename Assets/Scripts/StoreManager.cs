using UnityEngine;
using System.Collections.Generic;
using TMPro;
using UnityEngine.UI;

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

    [Header("Confirmation UI")]
    public GameObject openCardPanel; // Your existing panel
    public StoreCardSlot previewSlot; // The large slot in the panel
    public Button confirmBuyButton;
    public Button confirmTradeButton;
    public Button cancelButton;
    public Button confirmSellButton;

    private CardData pendingCard;
    private CardPack pendingPack;

    // StoreManager.cs
    void Start()
    {
        UpdateMoneyUI();
        if (marketManager != null && marketManager.allCards.Count > 0)
        {
            marketManager.UpdateMarketPrices();

            // Use a small delay or check if the buffer is actually ready
            if (Warehouse.Instance != null)
            {
                ReceiveSundayShipment();
            }
            else
            {
                Debug.LogError("StoreManager: Warehouse Instance is missing!");
            }
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

        bool isStoreSlot = slot.transform.IsChildOf(uiController.storeGridParent);

        if (isStoreSlot)
        {
            ShowStoreConfirmation(slot);
        }
        else
        {
            // Player inventory behavior remains the same (selecting for trade or opening packs)
            if (!string.IsNullOrEmpty(slot.packName))
            {
                CardPack pack = uiController.availablePacks.Find(p => p.packName == slot.packName);
                if (pack != null) OpenPack(pack);
            }
            else
            {
                ShowPlayerCardConfirmation(slot);
            }
        }
    }

    private void ShowPlayerCardConfirmation(StoreCardSlot slot)
    {
        openCardPanel.SetActive(true);
        previewSlot.transform.localScale = new Vector3(30f, 30f, 1f);

        pendingCard = slot.cardData;
        pendingPack = null;
        selectedPlayerCard = slot;

        // Use the Store's Buy Price (Markdown value)
        float sellPrice = GetStoreBuyPrice(pendingCard);
        previewSlot.Setup(pendingCard, sellPrice, this);

        // Toggle Buttons
        confirmBuyButton.gameObject.SetActive(false);
        confirmTradeButton.gameObject.SetActive(true);
        confirmSellButton.gameObject.SetActive(true); // Show Sell
        cancelButton.gameObject.SetActive(true);
    }

    private void ShowStoreConfirmation(StoreCardSlot slot)
    {
        openCardPanel.SetActive(true); // Open the preview panel
        previewSlot.transform.localScale = new Vector3(30f, 30f, 1f); // Set large preview scale

        if (cancelButton != null) cancelButton.gameObject.SetActive(true); // Ensure Cancel is always available
        confirmSellButton.gameObject.SetActive(false);

        if (!string.IsNullOrEmpty(slot.packName))
        {
            // --- PACK SELECTED ---
            pendingPack = uiController.availablePacks.Find(p => p.packName == slot.packName);
            pendingCard = null;
            previewSlot.SetupPack(pendingPack, this);

            confirmBuyButton.gameObject.SetActive(true);

            // HIDE TRADE BUTTON: Packs cannot be traded
            if (confirmTradeButton != null) confirmTradeButton.gameObject.SetActive(false);
        }
        else
        {
            // --- CARD SELECTED ---
            pendingCard = slot.cardData;
            pendingPack = null;
            selectedStoreCard = slot;
            previewSlot.Setup(pendingCard, GetStoreSellPrice(pendingCard), this);

            confirmBuyButton.gameObject.SetActive(true);

            // SHOW TRADE BUTTON: Individual cards can be traded
            if (confirmTradeButton != null) confirmTradeButton.gameObject.SetActive(true);
        }
    }

    public void OnConfirmBuy()
    {
        if (pendingPack != null) PurchasePack(pendingPack);
        else if (pendingCard != null) PurchaseSingle(pendingCard);

        CloseConfirmation();
    }

    public void OnConfirmSell()
    {
        if (pendingCard != null)
        {
            float price = GetStoreBuyPrice(pendingCard);

            // Remove from player, add to store, give money
            playerInventory.RemoveCard(pendingCard.cardID);
            storeInventory.AddCard(pendingCard.cardID);
            playerMoney += price;

            UpdateMoneyUI();
            uiController.RefreshUI();
        }
        CloseConfirmation();
    }

    public void OnConfirmTrade()
    {
        if (pendingCard != null && selectedPlayerCard != null)
        {
            ExecuteTrade();
        }
        else if (selectedPlayerCard == null)
        {
            Debug.LogWarning("Please select a card from your inventory first to trade!");
        }

        CloseConfirmation();
    }

    public void CloseConfirmation()
    {
        openCardPanel.SetActive(false);
        pendingCard = null;
        pendingPack = null;
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