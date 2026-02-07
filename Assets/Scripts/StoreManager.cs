using UnityEngine;
using System.Collections.Generic;
using TMPro;
using UnityEngine.UI;

public class StoreManager : MonoBehaviour
{
    public CardInventory storeInventory = new CardInventory();

    [Header("UI Displays")]
    public TextMeshProUGUI moneyDisplay;
    public TextMeshProUGUI tradeValueText;

    [Header("NPC State (For UI Only)")]
    public bool isNPCTrade = false;
    // Note: npcInteractionPanel and npcOpenCardPanel are now handled primarily by NPCManager, 
    // but kept here if your UI animations or toggles still reference them.
    public GameObject npcInteractionPanel;
    public Transform npcGridParent;
    public GameObject npcOpenCardPanel;

    [Header("Market Settings")]
    public bool allowDuplicates = true;
    public int shelfSpace = 16;
    public float storeMarkup = 1.1f;
    public float storeMarkdown = 0.9f;

    [Header("Dependencies")]
    public MarketManager marketManager;
    [HideInInspector] public StorefrontUIController uiController;

    [Header("Trade System")]
    public List<string> tradeCartIDs = new List<string>();
    public List<StoreCardSlot> playerTradeCart = new List<StoreCardSlot>();
    private float aggregateTradeValue = 0f;

    [Header("Confirmation UI")]
    public GameObject openCardPanel;
    public Image previewImage; // REPLACED: Drag a UI Image here instead of StoreCardSlot
    public TextMeshProUGUI previewPriceText; // Optional: Drag a text component for the price
    public Button confirmBuyButton;
    public Button confirmTradeButton;
    public Button cancelButton;
    public Button confirmSellButton;

    private StoreCardSlot selectedPlayerCard;
    private StoreCardSlot selectedStoreCard;
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

    // --- STORE LOGIC ---

    public void OpenMainStore()
    {
        isNPCTrade = false;
        // Close NPC specific panels if they were open
        if (npcInteractionPanel != null) npcInteractionPanel.SetActive(false);
        if (npcOpenCardPanel != null) npcOpenCardPanel.SetActive(false);

        ClearTradeCart();
        if (uiController != null) uiController.RefreshUI();
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

    // --- SELECTION LOGIC ---

    public void HandleCardSelection(StoreCardSlot slot)
    {
        bool isStoreSlot = slot.transform.IsChildOf(uiController.storeGridParent);

        if (!string.IsNullOrEmpty(slot.packName))
        {
            if (isStoreSlot)
            {
                ShowStoreConfirmation(slot);
            }
            else
            {
                // OPEN PACK: This calls StartPackOpening which now cleans the panel
                OpenPack(uiController.availablePacks.Find(p => p.packName == slot.packName));
            }
        }
        else
        {
            if (isStoreSlot) ShowStoreConfirmation(slot);
            else ShowPlayerCardConfirmation(slot);
        }
    }

    private void ShowPlayerCardConfirmation(StoreCardSlot slot)
    {
        openCardPanel.SetActive(true);

        // CHANGE: Use previewImage.gameObject instead of previewSlot
        if (previewImage != null) previewImage.gameObject.SetActive(true);

        // CHANGE: No .transform.localScale needed for a UI Image usually, 
        // but if you want to reset it:
        if (previewImage != null) previewImage.rectTransform.localScale = Vector3.one;

        pendingCard = slot.cardData;
        pendingPack = null;
        selectedPlayerCard = slot;

        // CHANGE: Assign sprite directly
        if (previewImage != null) previewImage.sprite = pendingCard.artwork;

        confirmBuyButton.gameObject.SetActive(false);
        confirmTradeButton.gameObject.SetActive(true);
        confirmSellButton.gameObject.SetActive(true);
        cancelButton.gameObject.SetActive(true);
    }

    private void ShowStoreConfirmation(StoreCardSlot slot)
    {
        openCardPanel.SetActive(true);

        // 1. HANDLE PACK VS CARD
        if (!string.IsNullOrEmpty(slot.packName))
        {
            pendingPack = uiController.availablePacks.Find(p => p.packName == slot.packName);
            pendingCard = null;

            if (previewImage != null)
            {
                previewImage.gameObject.SetActive(true); // Safety: ensure image object is on
                previewImage.sprite = pendingPack.packArt;
            }

            if (previewPriceText != null)
                previewPriceText.text = $"${pendingPack.cost:N0}";

            confirmBuyButton.gameObject.SetActive(true);
            confirmTradeButton.gameObject.SetActive(false);
        }
        else
        {
            pendingCard = slot.cardData;
            pendingPack = null;
            selectedStoreCard = slot;

            if (previewImage != null)
            {
                previewImage.gameObject.SetActive(true); // Safety: ensure image object is on
                previewImage.sprite = pendingCard.artwork;
            }

            if (previewPriceText != null)
                previewPriceText.text = $"${GetStoreSellPrice(pendingCard):N0}";

            confirmBuyButton.gameObject.SetActive(true);
            confirmTradeButton.gameObject.SetActive(true);
        }

        // Always hide Sell button when looking at store items
        confirmSellButton.gameObject.SetActive(false);
        cancelButton.gameObject.SetActive(true);
    }

    // --- TRANSACTION LOGIC ---

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

            PlayerManager.Instance.AddMoney(price);
            storeInventory.AddCard(pendingCard.cardID);

            if (selectedPlayerCard != null && !string.IsNullOrEmpty(selectedPlayerCard.instanceID))
            {
                PlayerManager.Instance.inventory.RemoveCardInstance(selectedPlayerCard.instanceID);
            }
            else
            {
                var inst = PlayerManager.Instance.inventory.cardInstances.Find(c => c.masterData.cardID == pendingCard.cardID);
                if (inst != null) PlayerManager.Instance.inventory.RemoveCardInstance(inst.instanceID);
            }

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
            bool isStoreCard = selectedStoreCard != null && pendingCard == selectedStoreCard.cardData;
            if (isStoreCard)
            {
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
        if (!tradeCartIDs.Contains(slot.instanceID))
        {
            tradeCartIDs.Add(slot.instanceID);
            playerTradeCart.Add(slot);
            slot.SetSelectionActive(true);

            if (tradeValueText != null)
            {
                tradeValueText.gameObject.SetActive(true);
                CalculateAggregateValue();
            }
        }
    }

    public void CalculateAggregateValue()
    {
        aggregateTradeValue = 0f;
        foreach (string uid in tradeCartIDs)
        {
            CardInstance inst = PlayerManager.Instance.inventory.cardInstances.Find(c => c.instanceID == uid);
            if (inst != null)
            {
                aggregateTradeValue += GetStoreBuyPrice(inst.masterData);
            }
        }

        if (tradeValueText != null)
        {
            tradeValueText.text = $"Trade Value: ${aggregateTradeValue:N0}";
        }
    }

    private void ExecuteTrade(CardData storeCard)
    {
        if (tradeCartIDs.Count == 0) return;

        CalculateAggregateValue();
        float targetCost = GetStoreSellPrice(storeCard);
        float difference = aggregateTradeValue - targetCost;

        // Store Rule: Check if player can pay the deficit
        if (difference < 0)
        {
            if (!PlayerManager.Instance.SpendMoney(Mathf.Abs(difference)))
            {
                Debug.LogWarning("Store: Insufficient funds for trade deficit.");
                return;
            }
            difference = 0;
        }

        PlayerManager.Instance.AddMoney(difference);

        foreach (string uid in tradeCartIDs)
        {
            CardInstance inst = PlayerManager.Instance.inventory.cardInstances.Find(c => c.instanceID == uid);
            if (inst != null)
            {
                storeInventory.AddCard(inst.masterData.cardID);
                PlayerManager.Instance.inventory.RemoveCardInstance(uid);
            }
        }

        storeInventory.RemoveCard(storeCard.cardID);
        PlayerManager.Instance.inventory.AddCardInstance(storeCard, "STORE_TRADE", 0);

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

    public void CloseConfirmation()
    {
        openCardPanel.SetActive(false);
        pendingCard = null;
        pendingPack = null;

        // CHANGE: Deactivate the image instead of the slot
        if (previewImage != null)
        {
            previewImage.gameObject.SetActive(false);
            previewImage.sprite = null; // Clear it so it doesn't flicker next time
        }
    }

    public void PurchasePack(CardPack pack)
    {
        if (storeInventory.packContents.ContainsKey(pack.packName) && PlayerManager.Instance.SpendMoney(pack.cost))
        {
            storeInventory.RemovePack(pack.packName);
            PlayerManager.Instance.inventory.AddPack(pack.packName);
            UpdateMoneyUI();
            if (uiController != null) uiController.RefreshUI();
        }
    }

    public void PurchaseSingle(CardData card)
    {
        float price = GetStoreSellPrice(card);
        if (storeInventory.contents.ContainsKey(card.cardID) && PlayerManager.Instance.SpendMoney(price))
        {
            storeInventory.RemoveCard(card.cardID);
            PlayerManager.Instance.inventory.AddCardInstance(card, "SINGLE", 0);
            UpdateMoneyUI();
            if (uiController != null) uiController.RefreshUI();
        }
    }

    public void UpdateMoneyUI()
    {
        if (moneyDisplay != null) moneyDisplay.text = $"${PlayerManager.Instance.money:N0}";
    }

    private void OpenPack(CardPack pack)
    {
        PackOpener opener = Object.FindAnyObjectByType<PackOpener>();
        if (opener != null)
        {
            opener.StartPackOpening(pack);
        }
        else
        {
            Debug.LogError("StoreManager: No PackOpener found in scene!");
        }
    }
}