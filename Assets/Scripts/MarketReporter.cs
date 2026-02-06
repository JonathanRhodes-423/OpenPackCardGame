using UnityEngine;
using System.Text;
using System.Linq;

public class MarketReporter : MonoBehaviour
{
    public MarketManager marketManager;
    public StoreManager storeManager;
    public int totalBotCount = 500000; // Your planned bot population

    [ContextMenu("Generate Economy Report")]
    public void GenerateReport()
    {
        StringBuilder report = new StringBuilder();
        report.AppendLine("=== 2026 GLOBAL ECONOMY REPORT ===");
        report.AppendLine($"Current Time: {System.DateTime.Now}");
        report.AppendLine($"Total Participating Bots: {totalBotCount:N0}");
        report.AppendLine("-----------------------------------");

        foreach (CardData card in marketManager.allCards)
        {
            float totalPrinted = card.GetCurrentSupply();
            int inPacks = card.totalPrintRun - (int)totalPrinted;

            // Simulation Logic: 90% of printed cards are usually with bots/market
            int botOwned = (int)(totalPrinted * 0.9f);
            int playerOwned = PlayerManager.Instance.inventory.contents.ContainsKey(card.cardID) ?
                              PlayerManager.Instance.inventory.contents[card.cardID] : 0;

            report.AppendLine($"CARD: {card.cardName} ({card.cardID})");
            report.AppendLine($"- Rarity: {card.rarity}");
            report.AppendLine($"- Current Market Value: ${card.currentMarketValue:F2}");
            report.AppendLine($"- Launch Date: {card.launchDateString}");
            report.AppendLine($"- Production Ends: {card.EndDate.ToShortDateString()}");
            report.AppendLine($"- Total Print Run: {card.totalPrintRun:N0}");
            report.AppendLine($"- Current Supply in World: {totalPrinted:N0}");
            report.AppendLine($"- Unopened in Packs: {inPacks:N0}");
            report.AppendLine($"- Owned by Bots: {botOwned:N0}");
            report.AppendLine($"- Owned by Player: {playerOwned}");
            report.AppendLine("-----------------------------------");
        }

        Debug.Log(report.ToString());
    }
}