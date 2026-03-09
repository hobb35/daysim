using System.Text;
using UnityEngine;
using UnityEngine.UI;
using DaySim.Achievements;

namespace DaySim.UI
{
    /// <summary>
    /// Displays the full achievement list (locked/unlocked) and refreshes
    /// automatically whenever the manager broadcasts a new achievement unlock.
    /// Attach to a GameObject in the Achievements panel.
    /// </summary>
    public class AchievementsView : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private DaySimManager daySimManager;
        [SerializeField] private Text achievementsText;

        private void Awake()
        {
            if (achievementsText == null)
                achievementsText = GetComponentInChildren<Text>();
        }

        private void OnEnable()
        {
            if (daySimManager != null)
                daySimManager.OnAchievementUnlocked += HandleAchievementUnlocked;

            RefreshUI();
        }

        private void OnDisable()
        {
            if (daySimManager != null)
                daySimManager.OnAchievementUnlocked -= HandleAchievementUnlocked;
        }

        private void HandleAchievementUnlocked(AchievementDefinition def)
        {
            RefreshUI();
        }

        private void RefreshUI()
        {
            if (achievementsText == null || daySimManager == null) return;

            var tracker = daySimManager.GetAchievementTracker();
            if (tracker == null)
            {
                achievementsText.text = "Achievements unavailable.";
                return;
            }

            var sb = new StringBuilder();
            int earned = 0;

            foreach (var def in tracker.All)
            {
                bool isEarned = tracker.IsEarned(def.Id);
                if (isEarned)
                {
                    sb.AppendLine($"[✓] {def.Title}");
                    sb.AppendLine($"    {def.Description}");
                    earned++;
                }
                else
                {
                    sb.AppendLine($"[  ] {def.Title}");
                    sb.AppendLine($"    {def.Description}");
                }
            }

            sb.Insert(0, $"Achievements: {earned}/{tracker.All.Count}\n\n");
            achievementsText.text = sb.ToString();
        }
    }
}
