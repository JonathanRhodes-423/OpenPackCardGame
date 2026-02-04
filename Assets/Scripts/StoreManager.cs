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

    [Header("Trade System")]
    // We need both: one for the card logic, one for the UI slot logic
    public List<string> tradeCartIDs = new List<string>();
    public List<StoreCardSlot> playerTradeCart = new List<StoreCardSlot>();
    public TextMeshProUGUI tradeValueText;
    private float aggregateTradeValue = 0f;

    [Header("Market Settings")]
    public bool allowDuplicates = true;
    public int shelfSpace = 16;
    public float storeMarkup = 1.1f;
    public float storeMarkdown = 0.9f;

    [Header("Dependencies")]
    public MarketManager marketManager;
    [HideInInspector] public StorefrontUIController uiController;

    private StoreCardSlot selectedPlayerCard;
    private StoreCardSlot selectedStoreCard;

    [Header("Confirmation UI")]
    public GameObject openCardPanel;
    public StoreCardSlot previewSlot;
    public Button confirmBuyButton;
    public Button confirmTradeButton;
    public Button cancelButton;
    public Button confirmSellButton;

    private CardData pendingCard;
    private CardPack pendingPack;

    void Start()
    {
        UpdateMoneyUI();
        if (marketManager != null && marketManager.allCards.Count > 0)
        {
            marketManager.UpdateMarketPrices();
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

    public void ReceiveSundayShipment()
    {
        storeInventory.packContents.Clear();
        List<CardPack> shipment = Warehouse.Instance.GetWeeklyShipment(shelfSpace);
        foreach (CardPack pack in shipment)
        {
            storeInventory.AddPack(pack.packName);
        }
        SeedStoreSingles();
        if (uiController != null) uiController.RefreshUI();
        Debug.Log("<color=cyan>Store:</color> Sunday shipment arrived!");
    }

    private void SeedStoreSingles()
    {
        storeInventory.contents.Clear();
        for (int i = 0; i < shelfSpace; i++)
        {
            int randomIndex = Random.Range(0, marketManager.allCards.Count);
            storeInventory.AddCard(marketManager.allCards[randomIndex].cardID, 1);
        }
    }

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

        if (previewSlot != null) previewSlot.gameObject.SetActive(true);

        previewSlot.transform.localScale = new Vector3(30f, 30f, 1f);
        pendingCard = slot.cardData;
        pendingPack = null;
        selectedPlayerCard = slot;

        float sellPrice = GetStoreBuyPrice(pendingCard);
        previewSlot.Setup(pendingCard, sellPrice, this);

        confirmBuyButton.gameObject.SetActive(false);
        confirmTradeButton.gameObject.SetActive(true);
        confirmSellButton.gameObject.SetActive(true);
        cancelButton.gameObject.SetActive(true);
    }

    private void ShowStoreConfirmation(StoreCardSlot slot)
    {
        openCardPanel.SetActive(true);

        if (previewSlot != null) previewSlot.gameObject.SetActive(true);

        previewSlot.transform.localScale = new Vector3(30f, 30f, 1f);
        if (cancelButton != null) cancelButton.gameObject.SetActive(true);
        confirmSellButton.gameObject.SetActive(false);

        if (!string.IsNullOrEmpty(slot.packName))
        {
            pendingPack = uiController.availablePacks.Find(p => p.packName == slot.packName);
            pendingCard = null;
            previewSlot.SetupPack(pendingPack, this);
            confirmBuyButton.gameObject.SetActive(true);
            if (confirmTradeButton != null) confirmTradeButton.gameObject.SetActive(false);
        }
        else
        {
            pendingCard = slot.cardData;
            pendingPack = null;
            selectedStoreCard = slot;
            previewSlot.Setup(pendingCard, GetStoreSellPrice(pendingCard), this);
            confirmBuyButton.gameObject.SetActive(true);
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
        if (pendingPack != null) return;

        if (pendingCard != null)
        {
            // Use the reference to selectedStoreCard we set in ShowStoreConfirmation
            bool isStoreCard = selectedStoreCard != null && pendingCard == selectedStoreCard.cardData;

            if (isStoreCard)
            {
                // IMPORTANT: Only call ExecuteTrade here, NOT PurchaseSingle
                ExecuteTrade(pendingCard);
            }
            else if (selectedPlayerCard != null)
            {
                AddToTradeCart(selectedPlayerCard);
            }
        }
        CloseConfirmation();
    }

    private void AddToTradeCart(StoreCardSlot slot)
    {
        // Ensure we are tracking the unique instance fingerprint
        if (!tradeCartIDs.Contains(slot.instanceID))
        {
            tradeCartIDs.Add(slot.instanceID);
            playerTradeCart.Add(slot); // Keep reference for the visual border

            slot.SetSelectionActive(true);

            // Activate UI text and force a calculation update
            if (tradeValueText != null)
            {
                tradeValueText.gameObject.SetActive(true);
                CalculateAggregateValue(); // Recalculate based on current IDs
            }

            Debug.Log($"Added {slot.cardData.cardName} to trade. New Total: {aggregateTradeValue}");
        }
    }

    public void CalculateAggregateValue()
    {
        aggregateTradeValue = 0f;

        // Loop through every unique card instance selected for trade
        foreach (string uid in tradeCartIDs)
        {
            // Find the instance in the player's inventory to get its master data (price)
            CardInstance inst = playerInventory.cardInstances.Find(c => c.instanceID == uid);
            if (inst != null)
            {
                // Use the markdown price (what the store buys it for)
                aggregateTradeValue += GetStoreBuyPrice(inst.masterData);
            }
        }

        // Update the UI text display
        if (tradeValueText != null)
        {
            tradeValueText.text = $"Trade Value: ${aggregateTradeValue:N0}";
        }
    }

    private void ExecuteTrade(CardData storeCard)
    {
        if (tradeCartIDs.Count == 0) return;

        // Ensure value is fresh before math
        CalculateAggregateValue();

        float storeCardCost = GetStoreSellPrice(storeCard);

        // Math: (Sum of my cards) - (Cost of store card)
        // If I give $10 of cards for a $7 store card, difference is +$3 (Store pays me)
        // If I give $5 of cards for a $7 store card, difference is -$2 (I pay store)
        float difference = aggregateTradeValue - storeCardCost;

        if (difference < 0 && playerMoney < Mathf.Abs(difference))
        {
            Debug.LogWarning("Insufficient funds to cover the trade gap.");
            return;
        }

        // Process the balance
        playerMoney += difference;

        // INVENTORY SWAP
        foreach (string uid in tradeCartIDs)
        {
            CardInstance inst = playerInventory.cardInstances.Find(c => c.instanceID == uid);
            if (inst != null)
            {
                storeInventory.AddCard(inst.masterData.cardID);
                playerInventory.RemoveCardInstance(uid);
            }
        }

        storeInventory.RemoveCard(storeCard.cardID);
        playerInventory.AddCardInstance(storeCard, "TRADE", 0);

        // CLEANUP
        ClearTradeCart();
        UpdateMoneyUI();
        uiController.RefreshUI();
    }

    public void ClearTradeCart()
    {
        tradeCartIDs.Clear();
        playerTradeCart.Clear();
        aggregateTradeValue = 0f;
        if (tradeValueText != null) tradeValueText.gameObject.SetActive(false);
    }

    public void ResetTrade() => ClearTradeCart();

    public void CloseConfirmation()
    {
        openCardPanel.SetActive(false);

        // Wipe all temporary references
        pendingCard = null;
        pendingPack = null;
        selectedStoreCard = null;
        selectedPlayerCard = null;

        // Reset the preview slot visibility for next time
        if (previewSlot != null) previewSlot.gameObject.SetActive(false);
    }

    public void PurchasePack(CardPack pack)
    {
        if (storeInventory.packContents.ContainsKey(pack.packName) && playerMoney >= pack.cost)
        {
            playerMoney -= pack.cost;
            storeInventory.RemovePack(pack.packName);
            playerInventory.AddPack(pack.packName);
            UpdateMoneyUI();
            if (uiController != null) uiController.RefreshUI();
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