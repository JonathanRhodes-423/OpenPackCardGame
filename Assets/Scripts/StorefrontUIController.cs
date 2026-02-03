using UnityEngine;
using System.Collections.Generic;
using TMPro;

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

    [Header("Pack Settings")]
    public List<CardPack> availablePacks = new List<CardPack>();

    [Header("Dependencies")]
    public StoreManager storeManager;
    public MarketManager marketManager;

    public StoreView GetCurrentView() => currentView;

    public void SetViewToSingles() { currentView = StoreView.Singles; RefreshUI(); }
    public void SetViewToPacks() { currentView = StoreView.Packs; RefreshUI(); }
    public void SetPlayerViewToSingles() { currentPlayerView = PlayerView.Singles; RefreshUI(); }
    public void SetPlayerViewToPacks() { currentPlayerView = PlayerView.Packs; RefreshUI(); }

    void Start()
    {
        if (storeManager != null)
        {
            storeManager.uiController = this;
        }

        // Now this will work because dateDisplay is defined!
        if (dateDisplay != null)
        {
            dateDisplay.text = System.DateTime.Now.ToString("M/d/yyyy h:mm tt");
        }
    }
    void Update()
    {
        // Make sure this is exactly 's' and not 'S' (Shift+S)
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
            RefreshUI();
        }
    }
    public void RefreshUI()
    {
        // Clear both grids
        foreach (Transform child in playerGridParent) Destroy(child.gameObject);
        foreach (Transform child in storeGridParent) Destroy(child.gameObject);

        // --- PLAYER SIDE ---
        if (currentPlayerView == PlayerView.Singles)
        {
            foreach (var entry in storeManager.playerInventory.contents)
                CreateSingleCardSlot(entry.Key, playerGridParent, false);
        }
        else
        {
            foreach (var entry in storeManager.playerInventory.packContents)
            {
                CardPack packData = availablePacks.Find(p => p.packName == entry.Key);
                GameObject go = Instantiate(cardSlotPrefab, playerGridParent);
                go.GetComponent<StoreCardSlot>().SetupPack(packData, storeManager);
            }
        }

        // --- STORE SIDE ---
        if (currentView == StoreView.Singles)
        {
            foreach (var entry in storeManager.storeInventory.contents)
            {
                CreateSingleCardSlot(entry.Key, storeGridParent, true);
            }
        }
        else
        {
            // FIX: Only loop through the inventory the store ACTUALLY has
            foreach (var entry in storeManager.storeInventory.packContents)
            {
                // Find the data template for this specific pack
                CardPack packData = availablePacks.Find(p => p.packName == entry.Key);

                if (packData != null)
                {
                    // Create a slot for every individual pack in stock
                    for (int i = 0; i < entry.Value; i++)
                    {
                        GameObject go = Instantiate(cardSlotPrefab, storeGridParent);
                        go.GetComponent<StoreCardSlot>().SetupPack(packData, storeManager);
                    }
                }
            }
        }
    }

    void CreateSingleCardSlot(string cardID, Transform parent, bool isStore)
    {
        CardData data = marketManager.allCards.Find(c => c.cardID == cardID);
        if (data == null) return;

        GameObject go = Instantiate(cardSlotPrefab, parent);
        float price = isStore ? storeManager.GetStoreSellPrice(data) : storeManager.GetStoreBuyPrice(data);
        go.GetComponent<StoreCardSlot>().Setup(data, price, storeManager);
    }
}