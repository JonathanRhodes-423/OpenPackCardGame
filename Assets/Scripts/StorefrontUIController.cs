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

    [Header("Player Page Buttons")]
    public Button playerNextButton;
    public Button playerPrevButton;
    private int currentPlayerPage = 0;

    [Header("Store Page Buttons")]
    public Button storeNextButton;
    public Button storePrevButton;
    private int currentStorePage = 0;

    [Header("Pack Settings")]
    public List<CardPack> availablePacks = new List<CardPack>();

    [Header("Dependencies")]
    public StoreManager storeManager;
    public MarketManager marketManager;

    // --- VIEW TOGGLES ---
    public void SetViewToSingles() { currentStorePage = 0; currentView = StoreView.Singles; RefreshUI(); }
    public void SetViewToPacks() { currentStorePage = 0; currentView = StoreView.Packs; RefreshUI(); }

    public void SetPlayerViewToSingles()
    {
        storeManager.ClearTradeCart();
        currentPlayerPage = 0;
        currentPlayerView = PlayerView.Singles;
        RefreshUI();
    }
    public void SetPlayerViewToPacks() { currentPlayerPage = 0; currentPlayerView = PlayerView.Packs; RefreshUI(); }

    void Start()
    {
        if (storeManager != null) storeManager.uiController = this;

        // Pagination Listeners
        if (playerNextButton != null) playerNextButton.onClick.AddListener(() => { currentPlayerPage++; RefreshUI(); });
        if (playerPrevButton != null) playerPrevButton.onClick.AddListener(() => { currentPlayerPage--; RefreshUI(); });
        if (storeNextButton != null) storeNextButton.onClick.AddListener(() => { currentStorePage++; RefreshUI(); });
        if (storePrevButton != null) storePrevButton.onClick.AddListener(() => { currentStorePage--; RefreshUI(); });
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.S)) ToggleStorefront();
    }

    public void ToggleStorefront()
    {
        bool isActive = storefrontCanvas.activeSelf;
        storefrontCanvas.SetActive(!isActive);
        if (!isActive)
        {
            currentPlayerPage = 0;
            currentStorePage = 0;
            // Always refresh from PlayerManager source on open
            RefreshUI();
        }
    }

    public void RefreshUI()
    {
        if (dateDisplay != null) dateDisplay.text = System.DateTime.Now.ToString("M/d/yyyy h:mm tt");

        // Clear existing slots
        foreach (Transform child in playerGridParent) Destroy(child.gameObject);
        foreach (Transform child in storeGridParent) Destroy(child.gameObject);

        // --- RENDER PLAYER SIDE (Refactored to PlayerManager) ---
        List<string> playerItems = FlattenInventory(PlayerManager.Instance.inventory, currentPlayerView == PlayerView.Singles, true);
        RenderGridPage(playerItems, playerGridParent, false, currentPlayerPage, playerNextButton, playerPrevButton);

        // --- RENDER STORE SIDE (Uses StoreManager's unique storeInventory) ---
        List<string> storeItems = FlattenInventory(storeManager.storeInventory, currentView == StoreView.Singles, false);
        RenderGridPage(storeItems, storeGridParent, true, currentStorePage, storeNextButton, storePrevButton);
    }

    private List<string> FlattenInventory(CardInventory inventory, bool isSingles, bool isPlayer)
    {
        List<string> list = new List<string>();
        if (isSingles)
        {
            if (isPlayer)
                foreach (var inst in inventory.cardInstances) list.Add(inst.instanceID);
            else
                foreach (var entry in inventory.contents)
                    for (int i = 0; i < entry.Value; i++) list.Add(entry.Key);
        }
        else
        {
            foreach (var entry in inventory.packContents)
                for (int i = 0; i < entry.Value; i++) list.Add(entry.Key);
        }
        return list;
    }

    private void RenderGridPage(List<string> items, Transform grid, bool isStoreSide, int pageNum, Button nextBtn, Button prevBtn)
    {
        int startIdx = pageNum * slotsPerPage;
        int endIdx = Mathf.Min(startIdx + slotsPerPage, items.Count);

        for (int i = startIdx; i < endIdx; i++)
        {
            bool drawingSingles = isStoreSide ? (currentView == StoreView.Singles) : (currentPlayerView == PlayerView.Singles);

            if (drawingSingles)
            {
                CreateSingleCardSlot(items[i], grid, isStoreSide);
            }
            else
            {
                CardPack packData = availablePacks.Find(p => p.packName == items[i]);
                if (packData != null)
                {
                    GameObject go = Instantiate(cardSlotPrefab, grid);
                    go.transform.localScale = Vector3.one;
                    go.SetActive(true);
                    go.GetComponent<StoreCardSlot>().SetupPack(packData, storeManager);
                }
            }
        }

        if (nextBtn != null) nextBtn.gameObject.SetActive((pageNum + 1) * slotsPerPage < items.Count);
        if (prevBtn != null) prevBtn.gameObject.SetActive(pageNum > 0);
    }

    private void CreateSingleCardSlot(string itemID, Transform grid, bool isStoreSide)
    {
        GameObject go = Instantiate(cardSlotPrefab, grid);
        go.transform.localScale = Vector3.one;
        go.transform.localPosition = Vector3.zero;
        go.SetActive(true);

        StoreCardSlot slot = go.GetComponent<StoreCardSlot>();

        if (!isStoreSide)
        {
            // PULL FROM PLAYERMANAGER: Find unique instance by ID
            CardInstance inst = PlayerManager.Instance.inventory.cardInstances.Find(c => c.instanceID == itemID);
            if (inst != null)
            {
                slot.Setup(inst.masterData, storeManager.GetStoreBuyPrice(inst.masterData), storeManager);
                slot.instanceID = inst.instanceID;

                if (storeManager.tradeCartIDs.Contains(inst.instanceID))
                {
                    slot.SetSelectionActive(true);
                }
            }
        }
        else
        {
            // PULL FROM STOREMANAGER: Find master data by ID
            CardData data = storeManager.marketManager.allCards.Find(c => c.cardID == itemID);
            if (data != null)
            {
                slot.Setup(data, storeManager.GetStoreSellPrice(data), storeManager);
            }
        }
    }
}