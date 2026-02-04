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
        cardsToReveal = pack.Open(storeManager.marketManager.allCards);

        if (cardsToReveal.Count == 0) return;

        storeManager.playerInventory.RemovePack(pack.packName);

        if (storeManager.confirmBuyButton != null) storeManager.confirmBuyButton.gameObject.SetActive(false);
        if (storeManager.confirmTradeButton != null) storeManager.confirmTradeButton.gameObject.SetActive(false);
        if (storeManager.cancelButton != null) storeManager.cancelButton.gameObject.SetActive(false);
        if (storeManager.confirmSellButton != null) storeManager.confirmSellButton.gameObject.SetActive(false);

        currentIndex = 0;
        revealOverlay.SetActive(true);
        ShowNextCard();
    }

    public void ShowNextCard()
    {
        if (currentIndex < cardsToReveal.Count)
        {
            CardData data = cardsToReveal[currentIndex];

            // Generate a unique pack ID (using Ticks for a quick unique string)
            string sessionPackID = System.DateTime.Now.Ticks.ToString().Substring(10);

            // 1. ADD TO PLAYER INVENTORY (Unique Instance)
            storeManager.playerInventory.AddCardInstance(data, sessionPackID, currentIndex);

            // 2. SETUP UI
            revealSlot.gameObject.SetActive(true);
            revealSlot.Setup(data, 0, storeManager);

            // Assign the unique ID to the UI slot for trade tracking
            revealSlot.instanceID = $"{data.cardID}_{sessionPackID}_{currentIndex}";

            // 3. ANIMATION
            revealSlot.transform.localScale = Vector3.zero;
            LeanTween.scale(revealSlot.gameObject, new Vector3(30f, 30f, 1f), 0.3f).setEaseOutBack();

            currentIndex++;
        }
        else
        {
            // EXIT CONDITION: No more cards left to show
            Debug.Log("Pack finished. Closing panel.");

            // 1. Disable the reveal slot and the overlay
            revealSlot.gameObject.SetActive(false);
            revealOverlay.SetActive(false);

            // 2. Optional: Refresh the collection UI to show new unique cards
            if (storeManager.uiController != null)
            {
                storeManager.uiController.RefreshUI();
            }
        }
    }
}