using UnityEngine;
using TMPro;

public class NPCEntryUI : MonoBehaviour
{
    public TextMeshProUGUI buttonText; // Drag the text object from the prefab here
    private BotData myData;

    public void Setup(BotData data)
    {
        myData = data;

        // Construct a name since BotData doesn't have a 'name' string
        if (buttonText != null)
        {
            buttonText.text = $"Bot #{data.botID} [{data.archetype}]";
        }
    }

    public void OnClickNPC()
    {
        // Tell the NPCManager to open this bot
        NPCManager manager = Object.FindAnyObjectByType<NPCManager>();
        if (manager != null)
        {
            manager.OpenNPCInventory(myData);
        }
    }
}