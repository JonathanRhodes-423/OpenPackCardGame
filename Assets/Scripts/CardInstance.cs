[System.Serializable]
public class CardInstance
{
    public string instanceID; // e.g., SirPercival_20530_1
    public CardData masterData;

    public CardInstance(CardData data, string packID, int index)
    {
        masterData = data;
        // System.Guid guarantees a unique string for every single card created
        instanceID = System.Guid.NewGuid().ToString();
    }
}