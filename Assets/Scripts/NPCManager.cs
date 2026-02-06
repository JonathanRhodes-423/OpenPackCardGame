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
            newBot.budget = UnityEngine.Random.Range(100, 1000);
            newBot.archetype = (BotArchetype)UnityEngine.Random.Range(0, 3);
            newBot.inventoryCardIDs = new List<string>();

            for (int j = 0; j < cardsPerNPC; j++)
            {
                newBot.inventoryCardIDs.Add(masterCards[UnityEngine.Random.Range(0, masterCards.Count)].cardID);
            }
            currentBots.Add(newBot);

            GameObject entry = Instantiate(npcEntryPrefab, contentParent);
            entry.transform.localScale = Vector3.one;
            // Ensure your NPCEntryUI script is updated to accept BotData
            entry.GetComponent<NPCEntryUI>()?.Setup(newBot);
        }
    }

    public void OpenNPCInventory(BotData data)
    {
        currentBot = data;
        if (npcNameText != null) npcNameText.text = $"Bot #{data.botID} ({data.archetype})";

        npcInteractionPanel.SetActive(true);
        npcTradeCartIDs.Clear();

        foreach (Transform child in npcGridParent) Destroy(child.gameObject);

        // Convert the string IDs back into actual card visuals
        foreach (string id in data.inventoryCardIDs)
        {
            CardData card = MarketManager.Instance.allCards.Find(c => c.cardID == id);
            if (card == null) continue;

            GameObject slotObj = Instantiate(npcCardSlotPrefab, npcGridParent);
            slotObj.transform.localScale = Vector3.one;
            slotObj.SetActive(true);

            NPCCardSlot slot = slotObj.GetComponent<NPCCardSlot>();
            slot.Setup(card, card.currentMarketValue * npcMarkup, this);
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

        // Reset visibility
        buyButton.gameObject.SetActive(false);
        sellButton.gameObject.SetActive(false);
        tradeButton.gameObject.SetActive(true); // Trade is visible in both contexts
        cancelButton.gameObject.SetActive(true);

        if (isPlayerCard)
        {
            // PLAYER CARD CONTEXT: Player wants to Sell or Trade their own card
            sellButton.gameObject.SetActive(true);

            if (offerPriceText != null)
                offerPriceText.text = $"Sell Value: ${pendingCard.currentMarketValue:N0}";

            // Link the Sell button to the new selling logic
            sellButton.onClick.RemoveAllListeners();
            sellButton.onClick.AddListener(() => OnConfirmSellToNPC(slot.instanceID));
        }
        else
        {
            // NPC CARD CONTEXT: Player wants to Buy or Trade for the bot's card
            buyButton.gameObject.SetActive(true);

            if (offerPriceText != null)
                offerPriceText.text = $"Cost: ${(pendingCard.currentMarketValue * npcMarkup):N0}";

            // Trade button on an NPC card executes the full cart swap
            tradeButton.onClick.RemoveAllListeners();
            tradeButton.onClick.AddListener(() => OnConfirmTrade());
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

    public void OnConfirmSellToNPC(string playerCardUID)
    {
        if (pendingCard == null) return;

        float price = pendingCard.currentMarketValue; // Bots buy at market value

        // Check if bot can afford it (Optional, using your BotData budget)
        if (currentBot.budget >= price)
        {
            // 1. Transaction
            PlayerManager.Instance.AddMoney(price);
            currentBot.budget -= price;

            // 2. Inventory Swap
            PlayerManager.Instance.inventory.RemoveCardInstance(playerCardUID);
            currentBot.inventoryCardIDs.Add(pendingCard.cardID);

            // 3. Cleanup
            npcOfferPanel.SetActive(false);
            RefreshPlayerInventory();
            UpdateWalletUI();
            OpenNPCInventory(currentBot);
            storeManager.UpdateMoneyUI();
            storeManager.uiController.RefreshUI();

            Debug.Log($"Sold {pendingCard.cardName} to Bot #{currentBot.botID}");
        }
        else
        {
            Debug.Log("This bot doesn't have enough money!");
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