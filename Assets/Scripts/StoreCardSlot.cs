using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class StoreCardSlot : MonoBehaviour
{
    [Header("UI References")]
    public Image artworkImage; // Drag the Button's Image component here
    public TextMeshProUGUI priceText; // Drag your TMP object here

    [Header("Selection UI")]
    public GameObject selectionBorder;

    [Header("Runtime Data")]
    public CardData cardData;
    public string packName;
    public string instanceID; // NEW: The fingerprint of the card
    private StoreManager storeManager;

    public void Setup(CardData data, float price, StoreManager manager)
    {
        cardData = data;
        packName = "";
        storeManager = manager;
        instanceID = ""; // Reset for non-player cards

        if (artworkImage != null) artworkImage.sprite = data.artwork;
        if (priceText != null) priceText.text = $"${price:N0}";
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
        Canvas parentCanvas = GetComponentInParent<Canvas>();

        if (parentCanvas != null && parentCanvas.gameObject.name.Contains("NPC"))
        {
            // Talk to NPC Manager instead of Store
            NPCManager npcMan = UnityEngine.Object.FindAnyObjectByType<NPCManager>();
            // We handle the specific logic inside NPCManager listeners now
        }
        else
        {
            // Standard Store logic
            storeManager.HandleCardSelection(this);
        }
    }

    public void SetSelectionActive(bool isActive)
    {
        if (selectionBorder != null)
            selectionBorder.SetActive(isActive);
    }
}