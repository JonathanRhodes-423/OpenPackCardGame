using System;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class NPCManager : MonoBehaviour
{
    [Header("Main NPC UI")]
    public GameObject npcListUI;
    public Transform contentParent;
    public GameObject npcEntryPrefab;

    [Header("NPC Inventory Display")]
    public GameObject npcInteractionPanel;
    public Transform npcGridParent;
    public TextMeshProUGUI npcNameText;

    [Header("NPC Offer Panel")]
    public GameObject npcOfferPanel;
    public Image offerPreviewImage;
    public TextMeshProUGUI offerPriceText;
    public Button buyButton;
    public Button tradeButton; // This usually sits on the NPC's card preview
    public Button sellButton;  // Drag your Sell/Offer button here
    public Button cancelButton;

    [Header("NPC Specific Prefabs")]
    public GameObject npcCardSlotPrefab;

    [Header("Player Inventory Display")]
    public Transform playerGridParent;
    public TextMeshProUGUI playerWalletText;

    [Header("Trade Selection")]
    public List<string> npcTradeCartIDs = new List<string>();
    public TextMeshProUGUI playerTradeValueText;

    [Header("Settings")]
    public int cardsPerNPC = 10;
    public float npcMarkup = 1.15f;

    [Header("Dependencies")]
    public StoreManager storeManager;

    private List<BotData> currentBots = new List<BotData>();
    private DateTime nextRefreshTime;
    private BotData currentBot;
    private CardData pendingCard;
    private float playerOfferValue = 0f;

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.B)) ToggleNPCMenu();
    }

    public void ToggleNPCMenu()
    {
        bool opening = !npcListUI.activeSelf;
        npcListUI.SetActive(opening);

        if (opening)
        {
            RefreshCheck();
            npcTradeCartIDs.Clear();
            RefreshPlayerInventory();
            UpdateWalletUI();
        }
        else
        {
            CloseAllNPCUI();
        }
    }

    public void UpdateWalletUI()
    {
        if (playerWalletText != null)
        {
            // Pull the value directly from the Singleton
            playerWalletText.text = $"${PlayerManager.Instance.money:N0}";
        }
    }

    void RefreshCheck()
    {
        if (DateTime.Now >= nextRefreshTime)
        {
            GenerateNewNPCs();
            nextRefreshTime = DateTime.Now.AddHours(1);
        }
    }

    public void RefreshPlayerInventory()
    {
        foreach (Transform child in playerGridParent) Destroy(child.gameObject);

        foreach (var inst in PlayerManager.Instance.inventory.cardInstances)
        {
            GameObject go = Instantiate(npcCardSlotPrefab, playerGridParent);
            go.transform.localScale = Vector3.one;

            NPCCardSlot slot = go.GetComponent<NPCCardSlot>();
            slot.Setup(inst.masterData, inst.masterData.currentMarketValue, this);
            slot.instanceID = inst.instanceID;
            slot.SetSelectionActive(npcTradeCartIDs.Contains(slot.instanceID));
        }
    }

    void GenerateNewNPCs()
    {
        foreach (Transform child in contentParent) Destroy(child.gameObject);
        currentBots.Clear();

        List<CardData> masterCards = MarketManager.Instance.allCards;
        if (masterCards == null || masterCards.Count == 0) return;

        for (int i = 0; i < 100; i++)
        {
            BotData newBot = new BotData();
            newBot.botID = i + 1;
            newBot.archetype = (BotArchetype)UnityEngine.Random.Range(0, 3);
            newBot.inventoryCardIDs = new List<string>();

            // Set budget and markup based on archetype
            switch (newBot.archetype)
            {
                case BotArchetype.Casual:
                    newBot.budget = UnityEngine.Random.Range(100, 500);
                    newBot.markup = UnityEngine.Random.Range(1.15f, 1.20f);
                    break;
                case BotArchetype.Avid:
                    newBot.budget = UnityEngine.Random.Range(500, 2000);
                    newBot.markup = UnityEngine.Random.Range(1.20f, 1.30f);
                    break;
                case BotArchetype.Zealous:
                    newBot.budget = UnityEngine.Random.Range(2000, 10000);
                    newBot.markup = UnityEngine.Random.Range(1.30f, 1.40f);
                    break;
            }

            for (int j = 0; j < cardsPerNPC; j++)
            {
                newBot.inventoryCardIDs.Add(GetWeightedRandomCard(newBot.archetype, masterCards));
            }

            currentBots.Add(newBot);

            GameObject entry = Instantiate(npcEntryPrefab, contentParent);
            entry.transform.localScale = Vector3.one;
            entry.GetComponent<NPCEntryUI>()?.Setup(newBot);
        }
    }

    // Helper to handle the rarity logic
    private string GetWeightedRandomCard(BotArchetype archetype, List<CardData> allCards)
    {
        // Define chance for Epic/Legendary based on archetype
        float epicChance = 0.05f;
        float legendaryChance = 0.01f;

        if (archetype == BotArchetype.Avid)
        {
            epicChance = 0.20f;
            legendaryChance = 0.05f;
        }
        else if (archetype == BotArchetype.Zealous)
        {
            epicChance = 0.40f;
            legendaryChance = 0.15f;
        }

        float roll = UnityEngine.Random.value;

        // CHANGE: Use 'Rarity' instead of 'CardRarity'
        Rarity targetRarity = Rarity.Common;

        if (roll < legendaryChance) targetRarity = Rarity.Legendary;
        else if (roll < epicChance + legendaryChance) targetRarity = Rarity.Epic;
        else if (roll < 0.6f) targetRarity = Rarity.Rare;

        // Filter list for that rarity
        List<CardData> filtered = allCards.FindAll(c => c.rarity == targetRarity);

        if (filtered.Count == 0) return allCards[UnityEngine.Random.Range(0, allCards.Count)].cardID;

        return filtered[UnityEngine.Random.Range(0, filtered.Count)].cardID;
    }

    public void OpenNPCInventory(BotData data)
    {
        currentBot = data;
        if (npcNameText != null) npcNameText.text = $"Bot #{data.botID} ({data.archetype})";

        npcInteractionPanel.SetActive(true);
        npcTradeCartIDs.Clear();

        foreach (Transform child in npcGridParent) Destroy(child.gameObject);

        foreach (string id in data.inventoryCardIDs)
        {
            CardData card = MarketManager.Instance.allCards.Find(c => c.cardID == id);
            if (card == null) continue;

            GameObject slotObj = Instantiate(npcCardSlotPrefab, npcGridParent);
            slotObj.transform.localScale = Vector3.one;
            slotObj.SetActive(true);

            NPCCardSlot slot = slotObj.GetComponent<NPCCardSlot>();
            // Use the individual bot's unique markup here
            slot.Setup(card, card.currentMarketValue * data.markup, this);
        }

        RefreshPlayerInventory();
        CalculateTradeValue();
    }

    private void ShowOfferMenu(NPCCardSlot slot)
    {
        pendingCard = slot.cardData;
        npcOfferPanel.SetActive(true);

        if (offerPreviewImage != null)
        {
            offerPreviewImage.sprite = pendingCard.artwork;
            offerPreviewImage.gameObject.SetActive(true);
        }

        bool isPlayerCard = !string.IsNullOrEmpty(slot.instanceID);

        buyButton.gameObject.SetActive(false);
        sellButton.gameObject.SetActive(false);
        tradeButton.gameObject.SetActive(true);
        cancelButton.gameObject.SetActive(true);

        buyButton.onClick.RemoveAllListeners();
        sellButton.onClick.RemoveAllListeners();
        tradeButton.onClick.RemoveAllListeners();

        if (isPlayerCard)
        {
            // Calculate the specific offer from this NPC
            float offer = CalculateNPCOffer(pendingCard, currentBot);

            if (offer < 0)
            {
                // NPC Declines
                if (offerPriceText != null) offerPriceText.text = "NPC is not interested in this card.";
                sellButton.gameObject.SetActive(false);
            }
            else
            {
                sellButton.gameObject.SetActive(true);
                if (offerPriceText != null) offerPriceText.text = $"Offer: ${offer:N2}";

                // Store the calculated offer to use in the confirmation
                playerOfferValue = offer;
                sellButton.onClick.AddListener(() => OnConfirmSellToNPC(slot.instanceID, offer));
            }
        }
        else
        {
            // NPC CARD CONTEXT: Player buying from NPC
            buyButton.gameObject.SetActive(true);
            if (offerPriceText != null)
                offerPriceText.text = $"Cost: ${(pendingCard.currentMarketValue * currentBot.markup):N0}";

            tradeButton.onClick.AddListener(() => OnConfirmTrade());
        }
    }

    // Helper to calculate how much the NPC will offer for a player's card
    private float CalculateNPCOffer(CardData card, BotData bot)
    {
        float marketValue = card.currentMarketValue;
        int countInInventory = bot.inventoryCardIDs.FindAll(id => id == card.cardID).Count;

        switch (card.rarity)
        {
            case Rarity.Common:
            case Rarity.Uncommon:
                // Zealous decline check
                if (bot.archetype == BotArchetype.Zealous && UnityEngine.Random.value < 0.25f)
                    return -1f; // Signal for declining

                float reduction = bot.archetype switch
                {
                    BotArchetype.Casual => UnityEngine.Random.Range(0.001f, 0.0015f),
                    BotArchetype.Avid => UnityEngine.Random.Range(0.002f, 0.0025f),
                    BotArchetype.Zealous => UnityEngine.Random.Range(0.003f, 0.004f),
                    _ => 0f
                };
                return marketValue * (1f - reduction);

            case Rarity.Rare:
                // Offer -0.1% to +0.1% based on inventory count
                float rareModifier = (countInInventory > 0) ? -0.001f : 0.001f;
                return marketValue * (1f + rareModifier);

            case Rarity.Epic:
                // Market value to +0.2%
                float epicModifier = (countInInventory == 0) ? 0.002f : 0f;
                return marketValue * (1f + epicModifier);

            case Rarity.Legendary:
                // Casual decline check
                if (bot.archetype == BotArchetype.Casual && UnityEngine.Random.value < 0.25f)
                    return -1f;

                // Offer +0.15% to +0.25% based on inventory count
                float legModifier = (countInInventory == 0) ? 0.0025f : 0.0015f;
                return marketValue * (1f + legModifier);

            default:
                return marketValue;
        }
    }

    public void OnConfirmBuy()
    {
        if (pendingCard == null) return;
        float cost = pendingCard.currentMarketValue * npcMarkup;

        if (PlayerManager.Instance.SpendMoney(cost))
        {
            PlayerManager.Instance.inventory.AddCardInstance(pendingCard, "NPC_PURCHASE", 0);
            currentBot.inventoryCardIDs.Remove(pendingCard.cardID);

            OpenNPCInventory(currentBot);
            npcOfferPanel.SetActive(false);
            storeManager.UpdateMoneyUI();
            storeManager.uiController.RefreshUI();
            UpdateWalletUI();
        }
    }

    public void OnConfirmSellToNPC(string playerCardUID, float finalPrice)
    {
        if (pendingCard == null) return;

        if (currentBot.budget >= finalPrice)
        {
            CardData soldCard = pendingCard;
            pendingCard = null;

            PlayerManager.Instance.AddMoney(finalPrice);
            currentBot.budget -= finalPrice;

            PlayerManager.Instance.inventory.RemoveCardInstance(playerCardUID);
            currentBot.inventoryCardIDs.Add(soldCard.cardID);

            npcOfferPanel.SetActive(false);
            RefreshPlayerInventory();
            UpdateWalletUI();
            OpenNPCInventory(currentBot);

            storeManager.UpdateMoneyUI();
            storeManager.uiController.RefreshUI();
        }
        else
        {
            Debug.Log("This bot doesn't have enough money for this offer!");
        }
    }

    public void OnConfirmTrade()
    {
        if (pendingCard == null) return;
        float npcCardValue = pendingCard.currentMarketValue * npcMarkup;

        if (playerOfferValue >= npcCardValue)
        {
            foreach (string uid in new List<string>(npcTradeCartIDs))
            {
                PlayerManager.Instance.inventory.RemoveCardInstance(uid);
            }

            PlayerManager.Instance.inventory.AddCardInstance(pendingCard, "NPC_TRADE", 0);
            currentBot.inventoryCardIDs.Remove(pendingCard.cardID);

            npcTradeCartIDs.Clear();
            OpenNPCInventory(currentBot);
            npcOfferPanel.SetActive(false);
            storeManager.uiController.RefreshUI();
        }
    }

    public void CalculateTradeValue()
    {
        playerOfferValue = 0f;
        foreach (string uid in npcTradeCartIDs)
        {
            CardInstance inst = PlayerManager.Instance.inventory.cardInstances.Find(c => c.instanceID == uid);
            if (inst != null) playerOfferValue += inst.masterData.currentMarketValue;
        }

        if (playerTradeValueText != null)
            playerTradeValueText.text = $"${playerOfferValue:N0}";
    }

    public void HandleSlotClick(NPCCardSlot slot)
    {
        // Determine if the slot belongs to the Player or the NPC
        bool isPlayerCard = !string.IsNullOrEmpty(slot.instanceID);

        if (isPlayerCard)
        {
            // 1. Toggle the selection for the trade cart
            if (npcTradeCartIDs.Contains(slot.instanceID))
                npcTradeCartIDs.Remove(slot.instanceID);
            else
                npcTradeCartIDs.Add(slot.instanceID);

            // 2. Refresh visuals to show the border
            RefreshPlayerInventory();
            CalculateTradeValue();

            // 3. NEW: Also show the preview panel for the player's card!
            ShowOfferMenu(slot);

            // 4. Adjust the preview panel buttons for player context
            // You don't "Buy" your own card, so hide that button
            buyButton.gameObject.SetActive(false);
            // Keep trade button active or hide it depending on your preference
            tradeButton.gameObject.SetActive(false);
        }
        else
        {
            // It's an NPC card
            ShowOfferMenu(slot);

            // Ensure Buy/Trade buttons are visible for NPC cards
            buyButton.gameObject.SetActive(true);
            tradeButton.gameObject.SetActive(true);
        }
    }

    public void CloseAllNPCUI()
    {
        npcListUI.SetActive(false);
        npcInteractionPanel.SetActive(false);
        npcOfferPanel.SetActive(false);
        npcTradeCartIDs.Clear();
    }
}