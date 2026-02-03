using UnityEngine;
using System.Collections.Generic;

public class PackOpener : MonoBehaviour
{
    public GameObject revealOverlay;
    public StoreCardSlot revealSlot; // The slot inside your OpenCardPanel
    public StoreManager storeManager;

    private List<CardData> cardsToReveal = new List<CardData>();
    private int currentIndex = 0;
    private string currentPackName;

    public void StartPackOpening(CardPack pack)
    {
        currentPackName = pack.packName;
        // This pulls cards based on your 2026 production dates
        cardsToReveal = pack.Open(storeManager.marketManager.allCards);

        if (cardsToReveal.Count == 0)
        {
            Debug.LogWarning("Pack was empty! Check your Card Launch Dates for 2026.");
            return;
        }

        if (storeManager.confirmBuyButton != null) storeManager.confirmBuyButton.gameObject.SetActive(false);
        if (storeManager.confirmTradeButton != null) storeManager.confirmTradeButton.gameObject.SetActive(false);
        if (storeManager.cancelButton != null) storeManager.cancelButton.gameObject.SetActive(false);

        currentIndex = 0;
        revealOverlay.SetActive(true);
        ShowNextCard();
    }

    public void ShowNextCard()
    {
        if (currentIndex < cardsToReveal.Count)
        {
            CardData data = cardsToReveal[currentIndex];
            revealSlot.gameObject.SetActive(true);
            revealSlot.Setup(data, 0, storeManager);

            // 1. ADD TO PLAYER INVENTORY
            storeManager.playerInventory.AddCard(data.cardID);

            // 2. ANIMATION & INDEX
            revealSlot.transform.localScale = Vector3.zero;
            LeanTween.scale(revealSlot.gameObject, new Vector3(30f, 30f, 1f), 0.3f).setEaseOutBack();

            currentIndex++;
        }
        else
        {
            // End of reveal...
            storeManager.playerInventory.RemovePack(currentPackName);
            revealOverlay.SetActive(false);
            storeManager.uiController.RefreshUI();
        }
    }
}