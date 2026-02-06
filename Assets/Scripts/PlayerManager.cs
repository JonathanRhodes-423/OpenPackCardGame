using UnityEngine;

public class PlayerManager : MonoBehaviour
{
    public static PlayerManager Instance { get; private set; }

    [Header("Economy & Collection")]
    public float money = 1000f;
    public CardInventory inventory = new CardInventory();

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(this); return; }
        Instance = this;
        // Optional: DontDestroyOnLoad(gameObject); // Uncomment if you have multiple scenes
    }

    public void AddMoney(float amount) => money += amount;
    public bool SpendMoney(float amount)
    {
        if (money >= amount)
        {
            money -= amount;
            return true;
        }
        return false;
    }
}