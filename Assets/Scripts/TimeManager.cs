using UnityEngine;
using System;

public class TimeManager : MonoBehaviour
{
    public static TimeManager Instance;
    public StoreManager storeManager;
    public BotManager botManager;

    void Awake() => Instance = this;

    void Start()
    {
        CheckRealTimeProgress();
    }

    private void CheckRealTimeProgress()
    {
        // Get the last saved date or default to right now
        string lastDateStr = PlayerPrefs.GetString("LastLoginDate", DateTime.Now.ToString());
        DateTime lastDate = DateTime.Parse(lastDateStr);
        DateTime today = DateTime.Now.Date;

        // 1. If today is Sunday, ensure store is restocked
        if (today.DayOfWeek == DayOfWeek.Sunday)
        {
            storeManager.ReceiveSundayShipment();
        }

        // 2. If at least one real-world day has passed
        if (today > lastDate)
        {
            // Calculate how many days passed (in case player was gone for a week)
            int daysPassed = (today - lastDate).Days;

            for (int i = 0; i < daysPassed; i++)
            {
                botManager.SimulateDailyPurchases();
            }

            storeManager.marketManager.UpdateMarketPrices();
            PlayerPrefs.SetString("LastLoginDate", today.ToString());
        }
    }
}