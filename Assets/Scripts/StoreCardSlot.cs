using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class StoreCardSlot : MonoBehaviour
{
    [Header("UI References")]
    public Image artworkImage; // Drag the Button's Image component here
    public TextMeshProUGUI priceText; // Drag your TMP object here

    [Header("Runtime Data")]
    public CardData cardData;
    public string packName; // Add this line to fix the reference error
    private StoreManager storeManager;

    // This is called by the StorefrontUIController loop
    public void Setup(CardData data, float price, StoreManager manager)
    {
        cardData = data;
        packName = "";
        storeManager = manager;

        if (artworkImage != null && data.artwork != null)
        {
            artworkImage.sprite = data.artwork;
        }

        // This check prevents the NullReferenceException
        if (priceText != null)
        {
            priceText.text = $"${price:N0}";
        }
    }

    public void SetupPack(CardPack pack, StoreManager manager)
    {
        cardData = null;
        packName = pack.packName;
        storeManager = manager;

        if (artworkImage != null)
        {
            artworkImage.sprite = pack.packArt;
        }
        else
        {
            Debug.LogError($"StoreCardSlot on {gameObject.name} is missing its 'artworkImage' reference!");
        }

        if (priceText != null)
        {
            priceText.text = $"${pack.cost:N0}";
        }
    }

    public void OnSlotClicked()
    {
        storeManager.HandleCardSelection(this);
    }
}