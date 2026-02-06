using UnityEngine;
using System.Collections.Generic;
using UnityEngine.UI; // Required for Image
using TMPro; // Required for TextMeshPro

public class PackOpener : MonoBehaviour
{
    [Header("UI References")]
    public GameObject revealOverlay;
    public Image revealImage; // REPLACED: Drag your UI Image here
    public TextMeshProUGUI revealNameText; // Optional: To show the name of the card
    public StoreManager storeManager;

    private List<CardData> cardsToReveal = new List<CardData>();
    private int currentIndex = 0;

    public void StartPackOpening(CardPack pack)
    {
        storeManager.openCardPanel.SetActive(true);

        // 1. Roll the cards
        cardsToReveal = pack.Open(storeManager.marketManager.allCards);

        if (cardsToReveal == null || cardsToReveal.Count == 0) return;

        // 2. Remove pack from inventory
        PlayerManager.Instance.inventory.RemovePack(pack.packName);

        // 3. SANITIZE THE PANEL (The "Clean Room" Step)
        // Since revealOverlay IS openCardPanel, we must hide the Store's buttons manually
        if (storeManager.confirmBuyButton != null) storeManager.confirmBuyButton.gameObject.SetActive(false);
        if (storeManager.confirmTradeButton != null) storeManager.confirmTradeButton.gameObject.SetActive(false);
        if (storeManager.confirmSellButton != null) storeManager.confirmSellButton.gameObject.SetActive(false);
        if (storeManager.cancelButton != null) storeManager.cancelButton.gameObject.SetActive(false);

        // Hide the price text specifically
        if (storeManager.previewPriceText != null) storeManager.previewPriceText.text = "";

        // 4. Start Reveal
        revealImage.transform.SetAsLastSibling();
        currentIndex = 0;
        revealOverlay.SetActive(true);
        ShowNextCard();
    }

    public void ShowNextCard()
    {
        if (currentIndex < cardsToReveal.Count)
        {
            CardData data = cardsToReveal[currentIndex];

            // Safety: Ensure the data and artwork actually exist
            if (data == null || data.artwork == null)
            {
                Debug.LogError($"PackOpener: Card data or artwork missing for {data?.cardName}");
                currentIndex++;
                ShowNextCard();
                return;
            }

            // 1. ADD TO INVENTORY
            string sessionID = System.DateTime.Now.Ticks.ToString().Substring(10);
            PlayerManager.Instance.inventory.AddCardInstance(data, sessionID, currentIndex);

            // 2. FORCE IMAGE UPDATE
            if (revealImage != null)
            {
                revealImage.gameObject.SetActive(true); // Force object active
                revealImage.enabled = true;             // Force component enabled
                revealImage.sprite = data.artwork;      // Apply sprite
                revealImage.color = Color.white;        // Reset alpha/color in case it was faded

                // 3. RE-SCALE ANIMATION
                // Set to zero first so the tween has a starting point
                revealImage.rectTransform.localScale = Vector3.zero;
                LeanTween.cancel(revealImage.gameObject); // Stop any previous tweens
                LeanTween.scale(revealImage.gameObject, Vector3.one, 0.4f).setEaseOutBack();
            }

            if (revealNameText != null)
                revealNameText.text = data.cardName;

            currentIndex++;
        }
        else
        {
            FinishOpening();
        }
    }

    private void FinishOpening()
    {
        Debug.Log("Pack finished. Closing panel.");

        if (revealImage != null) revealImage.gameObject.SetActive(false);
        revealOverlay.SetActive(false);

        // Reset the price text in case the store uses it next
        if (storeManager.previewPriceText != null) storeManager.previewPriceText.text = "";

        // Refresh the collection UI to show the cards we just added
        if (storeManager.uiController != null)
        {
            storeManager.uiController.RefreshUI();
        }
    }
}