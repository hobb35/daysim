using System;
using System.Collections.Generic;
using System.Text;

namespace DaySim.Analytics
{
    /// <summary>
    /// Stateless utility that computes aggregate stats over a logged action history.
    /// </summary>
    public static class DaySimAnalytics
    {
        /// <summary>
        /// Counts actions per non-Unknown category for the past <paramref name="daysBack"/> days.
        /// </summary>
        public static Dictionary<HabitCategory, int> GetCategoryCountsForPastDays(
            IReadOnlyList<UserAction> actions, int daysBack)
        {
            var cutoff = DateTime.UtcNow.Date.AddDays(-daysBack);
            var counts = new Dictionary<HabitCategory, int>();

            foreach (var action in actions)
            {
                if (action == null || action.Category == HabitCategory.Unknown) continue;
                if (action.TimestampUtc.Date < cutoff) continue;

                if (!counts.ContainsKey(action.Category))
                    counts[action.Category] = 0;

                counts[action.Category]++;
            }

            return counts;
        }

        /// <summary>
        /// Returns the total number of recognized actions logged today (UTC).
        /// </summary>
        public static int GetTodayActionCount(IReadOnlyList<UserAction> actions)
        {
            var today = DateTime.UtcNow.Date;
            int count = 0;
            foreach (var action in actions)
            {
                if (action != null && action.Category != HabitCategory.Unknown
                    && action.TimestampUtc.Date == today)
                {
                    count++;
                }
            }
            return count;
        }

        /// <summary>
        /// Returns the category with the most actions in the past <paramref name="daysBack"/> days.
        /// </summary>
        public static HabitCategory GetMostActiveCategory(IReadOnlyList<UserAction> actions, int daysBack)
        {
            var counts = GetCategoryCountsForPastDays(actions, daysBack);
            var best = HabitCategory.Unknown;
            int bestCount = 0;
            foreach (var kvp in counts)
            {
                if (kvp.Value > bestCount)
                {
                    bestCount = kvp.Value;
                    best = kvp.Key;
                }
            }
            return best;
        }

        /// <summary>
        /// Builds a human-readable 7-day activity summary sorted by most active category.
        /// </summary>
        public static string FormatWeeklySummary(IReadOnlyList<UserAction> actions)
        {
            var counts = GetCategoryCountsForPastDays(actions, 7);
            if (counts.Count == 0)
                return "No activity logged in the past 7 days.";

            // Sort descending by count (no LINQ — manual insertion sort)
            var sorted = new List<KeyValuePair<HabitCategory, int>>(counts);
            for (int i = 1; i < sorted.Count; i++)
            {
                var cur = sorted[i];
                int j = i - 1;
                while (j >= 0 && sorted[j].Value < cur.Value)
                {
                    sorted[j + 1] = sorted[j];
                    j--;
                }
                sorted[j + 1] = cur;
            }

            int total = 0;
            foreach (var kvp in sorted) total += kvp.Value;

            var sb = new StringBuilder();
            sb.AppendLine("Last 7 Days:");
            foreach (var kvp in sorted)
                sb.AppendLine($"  {kvp.Key}: {kvp.Value}x");
            sb.AppendLine($"Total: {total} actions");

            int today = GetTodayActionCount(actions);
            if (today > 0)
                sb.AppendLine($"Today so far: {today} actions");

            return sb.ToString();
        }

        /// <summary>
        /// Returns a short encouragement message based on activity level.
        /// </summary>
        public static string GetMotivationMessage(IReadOnlyList<UserAction> actions)
        {
            int today = GetTodayActionCount(actions);

            if (today == 0) return "Log your first action today!";
            if (today < 3)  return "Good start — keep going!";
            if (today < 6)  return "You're building momentum!";
            if (today < 10) return "Solid day — well done!";
            return "Outstanding effort today!";
        }
    }
}
