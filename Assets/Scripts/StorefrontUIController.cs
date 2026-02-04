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

    // These methods should be called by your Tab Buttons in the Inspector
    public void SetViewToSingles() { currentStorePage = 0; currentView = StoreView.Singles; RefreshUI(); }
    public void SetViewToPacks() { currentStorePage = 0; currentView = StoreView.Packs; RefreshUI(); }
    public void SetPlayerViewToSingles()
    {
        storeManager.ClearTradeCart(); // Clear when changing view type
        currentPlayerPage = 0;
        currentPlayerView = PlayerView.Singles;
        RefreshUI();
    }
    public void SetPlayerViewToPacks() { currentPlayerPage = 0; currentPlayerView = PlayerView.Packs; RefreshUI(); }

    void Start()
    {
        if (storeManager != null) storeManager.uiController = this;

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
            RefreshUI();
        }
    }

    public void RefreshUI()
    {
        if (dateDisplay != null) dateDisplay.text = System.DateTime.Now.ToString("M/d/yyyy h:mm tt");

        foreach (Transform child in playerGridParent) Destroy(child.gameObject);
        foreach (Transform child in storeGridParent) Destroy(child.gameObject);

        // --- RENDER PLAYER SIDE ---
        List<string> playerItems = FlattenInventory(storeManager.playerInventory, currentPlayerView == PlayerView.Singles, true);
        RenderGridPage(playerItems, playerGridParent, false, currentPlayerPage, playerNextButton, playerPrevButton);

        // --- RENDER STORE SIDE ---
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
                    go.GetComponent<StoreCardSlot>().SetupPack(packData, storeManager);
                }
            }
        }

        if (nextBtn != null) nextBtn.gameObject.SetActive((pageNum + 1) * slotsPerPage < items.Count);
        if (prevBtn != null) prevBtn.gameObject.SetActive(pageNum > 0);
    }

    void CreateSingleCardSlot(string idOrUid, Transform parent, bool isStore)
    {
        CardData data = null;
        string finalUid = "";

        if (isStore)
        {
            data = marketManager.allCards.Find(c => c.cardID == idOrUid);
        }
        else
        {
            CardInstance inst = storeManager.playerInventory.cardInstances.Find(c => c.instanceID == idOrUid);
            if (inst != null) { data = inst.masterData; finalUid = inst.instanceID; }
        }

        if (data == null) return;

        GameObject go = Instantiate(cardSlotPrefab, parent);
        StoreCardSlot slot = go.GetComponent<StoreCardSlot>();
        float price = isStore ? storeManager.GetStoreSellPrice(data) : storeManager.GetStoreBuyPrice(data);
        slot.Setup(data, price, storeManager);

        if (!isStore)
        {
            slot.instanceID = finalUid;
            if (storeManager.tradeCartIDs.Contains(finalUid)) slot.SetSelectionActive(true);
        }
    }
}