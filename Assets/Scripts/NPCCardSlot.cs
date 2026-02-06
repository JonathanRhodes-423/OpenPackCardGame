using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class NPCCardSlot : MonoBehaviour
{
    [Header("UI References")]
    public Image cardImage;
    public TextMeshProUGUI priceText;
    public GameObject selectionBorder;

    [Header("Data")]
    public CardData cardData;
    public string instanceID; // For player-owned cards
    public float price;

    private NPCManager npcManager;

    public void Setup(CardData data, float priceVal, NPCManager manager)
    {
        cardData = data;
        price = priceVal;
        npcManager = manager;

        if (cardImage != null) cardImage.sprite = data.artwork;
        if (priceText != null) priceText.text = $"${price:N0}";

        // Hide selection border by default
        if (selectionBorder != null) selectionBorder.SetActive(false);
    }

    public void OnSlotClicked()
    {
        // Tell the NPC Manager this specific slot was clicked
        npcManager.HandleSlotClick(this);
    }

    public void SetSelectionActive(bool active)
    {
        if (selectionBorder != null) selectionBorder.SetActive(active);
    }
}