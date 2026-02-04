[System.Serializable]
public class CardInstance
{
    public string instanceID; // e.g., SirPercival_20530_1
    public CardData masterData;

    public CardInstance(CardData data, string packID, int indexInPack)
    {
        masterData = data;
        // Unique fingerprint: CardID + PackID + Position in Pack
        instanceID = $"{data.cardID}_{packID}_{indexInPack}";
    }
}