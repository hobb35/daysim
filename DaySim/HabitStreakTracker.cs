using System;
using System.Collections.Generic;
using DaySim.Persistence;

namespace DaySim
{
    /// <summary>
    /// Tracks simple daily streaks per habit category.
    /// A "hit" is counted once per calendar day where at least one action in that category occurs.
    /// </summary>
    [Serializable]
    public class HabitStreakTracker
    {
        [Serializable]
        public class HabitStreak
        {
            public HabitCategory Category;
            public int CurrentStreakDays;
            public int BestStreakDays;
            public string LastActionDateUtc; // stored as yyyy-MM-dd
        }

        private readonly Dictionary<HabitCategory, HabitStreak> _streaks =
            new Dictionary<HabitCategory, HabitStreak>();

        public IReadOnlyDictionary<HabitCategory, HabitStreak> Streaks => _streaks;

        public void RegisterAction(UserAction action)
        {
            if (action == null) return;

            var category = action.Category;
            if (category == HabitCategory.Unknown) return;

            var dateKey = action.TimestampUtc.ToUniversalTime().ToString("yyyy-MM-dd");

            if (!_streaks.TryGetValue(category, out var streak))
            {
                streak = new HabitStreak
                {
                    Category = category,
                    CurrentStreakDays = 1,
                    BestStreakDays = 1,
                    LastActionDateUtc = dateKey
                };
                _streaks[category] = streak;
                return;
            }

            if (streak.LastActionDateUtc == dateKey)
            {
                // Already counted today.
                return;
            }

            var lastDate = DateTime.Parse(streak.LastActionDateUtc).Date;
            var currentDate = DateTime.Parse(dateKey).Date;
            var deltaDays = (currentDate - lastDate).Days;

            if (deltaDays == 1)
            {
                streak.CurrentStreakDays++;
            }
            else if (deltaDays > 1)
            {
                // Gap detected — streak resets.
                streak.CurrentStreakDays = 1;
            }

            streak.LastActionDateUtc = dateKey;
            if (streak.CurrentStreakDays > streak.BestStreakDays)
            {
                streak.BestStreakDays = streak.CurrentStreakDays;
            }
        }

        /// <summary>
        /// Rebuilds all streaks by replaying a sorted history of actions.
        /// Call this on first load when no persisted streak data exists.
        /// </summary>
        public void ReplayHistory(IReadOnlyList<UserAction> actions)
        {
            _streaks.Clear();
            if (actions == null || actions.Count == 0) return;

            var sorted = new List<UserAction>(actions);
            sorted.Sort((a, b) => DateTime.Compare(a.TimestampUtc, b.TimestampUtc));

            foreach (var action in sorted)
                RegisterAction(action);
        }

        /// <summary>
        /// Restores streak state directly from persisted data (faster than replaying history).
        /// </summary>
        public void LoadStreakData(IReadOnlyList<HabitStreakSaveData> data)
        {
            _streaks.Clear();
            if (data == null) return;

            foreach (var d in data)
            {
                if (d == null) continue;
                var category = (HabitCategory)d.Category;
                _streaks[category] = new HabitStreak
                {
                    Category = category,
                    CurrentStreakDays = d.CurrentStreakDays,
                    BestStreakDays = d.BestStreakDays,
                    LastActionDateUtc = d.LastActionDateUtc
                };
            }
        }

        /// <summary>
        /// Returns current streak data serialized for persistence.
        /// </summary>
        public List<HabitStreakSaveData> GetStreakSaveData()
        {
            var result = new List<HabitStreakSaveData>(_streaks.Count);
            foreach (var kvp in _streaks)
            {
                result.Add(new HabitStreakSaveData
                {
                    Category = (int)kvp.Key,
                    CurrentStreakDays = kvp.Value.CurrentStreakDays,
                    BestStreakDays = kvp.Value.BestStreakDays,
                    LastActionDateUtc = kvp.Value.LastActionDateUtc
                });
            }
            return result;
        }
    }
}
