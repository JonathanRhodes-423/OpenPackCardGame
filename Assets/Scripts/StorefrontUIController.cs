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
    public int slotsPerPage = 16; // Strictly enforced 4x4

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

    public void SetViewToSingles() { currentStorePage = 0; currentView = StoreView.Singles; RefreshUI(); }
    public void SetViewToPacks() { currentStorePage = 0; currentView = StoreView.Packs; RefreshUI(); }
    public void SetPlayerViewToSingles() { currentPlayerPage = 0; currentPlayerView = PlayerView.Singles; RefreshUI(); }
    public void SetPlayerViewToPacks() { currentPlayerPage = 0; currentPlayerView = PlayerView.Packs; RefreshUI(); }

    void Start()
    {
        if (storeManager != null) storeManager.uiController = this;

        // Initialize Listeners
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
        List<string> playerItems = FlattenInventory(storeManager.playerInventory, currentPlayerView == PlayerView.Singles);
        RenderGridPage(playerItems, playerGridParent, false, currentPlayerPage, playerNextButton, playerPrevButton);

        // --- RENDER STORE SIDE ---
        List<string> storeItems = FlattenInventory(storeManager.storeInventory, currentView == StoreView.Singles);
        RenderGridPage(storeItems, storeGridParent, true, currentStorePage, storeNextButton, storePrevButton);
    }

    private List<string> FlattenInventory(CardInventory inventory, bool isSingles)
    {
        List<string> list = new List<string>();
        var targetDict = isSingles ? inventory.contents : inventory.packContents;

        foreach (var entry in targetDict)
        {
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
            bool isActuallySingles = isStoreSide ? (currentView == StoreView.Singles) : (currentPlayerView == PlayerView.Singles);

            if (isActuallySingles)
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

        // Handle Button Visibility
        if (nextBtn != null) nextBtn.gameObject.SetActive((pageNum + 1) * slotsPerPage < items.Count);
        if (prevBtn != null) prevBtn.gameObject.SetActive(pageNum > 0);
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