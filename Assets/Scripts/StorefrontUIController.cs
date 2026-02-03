using UnityEngine;
using System.Collections.Generic;
using TMPro;
using UnityEngine.UI;

public class StorefrontUIController : MonoBehaviour
{
    public enum StoreView { Singles, Packs }
    private StoreView currentView = StoreView.Packs;
    public enum PlayerView { Singles, Packs }
    private PlayerView currentPlayerView = PlayerView.Packs;

    [Header("UI Containers")]
    public GameObject storefrontCanvas;
    public Transform playerGridParent;
    public Transform storeGridParent;
    public GameObject cardSlotPrefab;
    public TextMeshProUGUI dateDisplay;

    [Header("Pagination Settings")]
    public int slotsPerPage = 16;
    public Button nextPageButton;
    public Button prevPageButton;
    private int currentPlayerPage = 0;

    [Header("Pack Settings")]
    public List<CardPack> availablePacks = new List<CardPack>();

    [Header("Dependencies")]
    public StoreManager storeManager;
    public MarketManager marketManager;

    public StoreView GetCurrentView() => currentView;

    // Updated view switches to reset page count
    public void SetViewToSingles() { currentView = StoreView.Singles; RefreshUI(); }
    public void SetViewToPacks() { currentView = StoreView.Packs; RefreshUI(); }

    public void SetPlayerViewToSingles()
    {
        currentPlayerPage = 0;
        currentPlayerView = PlayerView.Singles;
        RefreshUI();
    }

    public void SetPlayerViewToPacks()
    {
        currentPlayerPage = 0;
        currentPlayerView = PlayerView.Packs;
        RefreshUI();
    }

    void Start()
    {
        if (storeManager != null)
        {
            storeManager.uiController = this;
        }

        if (dateDisplay != null)
        {
            dateDisplay.text = System.DateTime.Now.ToString("M/d/yyyy h:mm tt");
        }

        // Initialize button listeners
        if (nextPageButton != null) nextPageButton.onClick.AddListener(NextPage);
        if (prevPageButton != null) prevPageButton.onClick.AddListener(PreviousPage);
    }

    public void NextPage() { currentPlayerPage++; RefreshUI(); }
    public void PreviousPage() { currentPlayerPage--; RefreshUI(); }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.S))
        {
            ToggleStorefront();
        }
    }

    public void ToggleStorefront()
    {
        bool isActive = storefrontCanvas.activeSelf;
        storefrontCanvas.SetActive(!isActive);
        if (!isActive)
        {
            currentPlayerPage = 0; // Reset to page 1 when opening
            RefreshUI();
        }
    }

    public void RefreshUI()
    {
        // Clear both grids
        foreach (Transform child in playerGridParent) Destroy(child.gameObject);
        foreach (Transform child in storeGridParent) Destroy(child.gameObject);

        // --- PLAYER SIDE (with Pagination and Unique Slots) ---
        List<string> playerItemsToDisplay = new List<string>();

        if (currentPlayerView == PlayerView.Singles)
        {
            // Flatten: every card instance gets an entry
            foreach (var entry in storeManager.playerInventory.contents)
            {
                for (int i = 0; i < entry.Value; i++) playerItemsToDisplay.Add(entry.Key);
            }
        }
        else
        {
            // Flatten: every pack instance gets an entry
            foreach (var entry in storeManager.playerInventory.packContents)
            {
                for (int i = 0; i < entry.Value; i++) playerItemsToDisplay.Add(entry.Key);
            }
        }

        int totalItems = playerItemsToDisplay.Count;
        int startIdx = currentPlayerPage * slotsPerPage;
        int endIdx = Mathf.Min(startIdx + slotsPerPage, totalItems);

        for (int i = startIdx; i < endIdx; i++)
        {
            if (currentPlayerView == PlayerView.Singles)
            {
                CreateSingleCardSlot(playerItemsToDisplay[i], playerGridParent, false);
            }
            else
            {
                CardPack packData = availablePacks.Find(p => p.packName == playerItemsToDisplay[i]);
                if (packData != null)
                {
                    GameObject go = Instantiate(cardSlotPrefab, playerGridParent);
                    go.GetComponent<StoreCardSlot>().SetupPack(packData, storeManager);
                }
            }
        }

        UpdatePaginationButtons(totalItems);

        // --- STORE SIDE ---
        if (currentView == StoreView.Singles)
        {
            foreach (var entry in storeManager.storeInventory.contents)
            {
                // Store singles also follow unique slot rule
                for (int i = 0; i < entry.Value; i++)
                {
                    CreateSingleCardSlot(entry.Key, storeGridParent, true);
                }
            }
        }
        else
        {
            foreach (var entry in storeManager.storeInventory.packContents)
            {
                CardPack packData = availablePacks.Find(p => p.packName == entry.Key);
                if (packData != null)
                {
                    for (int i = 0; i < entry.Value; i++)
                    {
                        GameObject go = Instantiate(cardSlotPrefab, storeGridParent);
                        go.GetComponent<StoreCardSlot>().SetupPack(packData, storeManager);
                    }
                }
            }
        }
    }

    private void UpdatePaginationButtons(int totalItems)
    {
        if (nextPageButton == null || prevPageButton == null) return;

        // Show Next if there are more items beyond the current page
        nextPageButton.gameObject.SetActive((currentPlayerPage + 1) * slotsPerPage < totalItems);

        // Show Prev if we aren't on the first page
        prevPageButton.gameObject.SetActive(currentPlayerPage > 0);
    }

    void CreateSingleCardSlot(string cardID, Transform parent, bool isStore)
    {
        CardData data = marketManager.allCards.Find(c => c.cardID == cardID);
        if (data == null) return;

        GameObject go = Instantiate(cardSlotPrefab, parent);
        float price = isStore ? storeManager.GetStoreSellPrice(data) : storeManager.GetStoreBuyPrice(data);
        go.GetComponent<StoreCardSlot>().Setup(data, price, storeManager);
    }

    public void HideConfirmationButtons()
    {
        if (storeManager.confirmBuyButton != null) storeManager.confirmBuyButton.gameObject.SetActive(false);
        if (storeManager.confirmTradeButton != null) storeManager.confirmTradeButton.gameObject.SetActive(false);
    }
}