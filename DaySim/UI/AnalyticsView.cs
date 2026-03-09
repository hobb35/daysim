using UnityEngine;
using UnityEngine.UI;
using DaySim.Analytics;

namespace DaySim.UI
{
    /// <summary>
    /// Displays a 7-day activity summary and daily motivation message.
    /// Refreshes each time its panel becomes active.
    /// Attach to a GameObject in the Stats / Analytics panel.
    /// </summary>
    public class AnalyticsView : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private DaySimManager daySimManager;
        [SerializeField] private Text summaryText;
        [SerializeField] private Text motivationText;

        private void Awake()
        {
            if (summaryText == null)
            {
                var texts = GetComponentsInChildren<Text>();
                if (texts.Length > 0) summaryText = texts[0];
                if (texts.Length > 1) motivationText = texts[1];
            }
        }

        private void OnEnable()
        {
            RefreshUI();
        }

        private void RefreshUI()
        {
            if (daySimManager == null) return;

            var actions = daySimManager.GetAllActions();

            if (summaryText != null)
                summaryText.text = DaySimAnalytics.FormatWeeklySummary(actions);

            if (motivationText != null)
                motivationText.text = DaySimAnalytics.GetMotivationMessage(actions);
        }
    }
}
