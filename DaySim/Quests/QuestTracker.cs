using System;
using System.Collections.Generic;
using DaySim.Persistence;

namespace DaySim.Quests
{
    /// <summary>
    /// Tracks a set of daily quests based on habit categories.
    /// </summary>
    [Serializable]
    public class QuestTracker
    {
        [Serializable]
        public class QuestState
        {
            public QuestDefinition Definition;
            public int CompletedCountToday;
            public bool IsCompletedToday;
            public string DateKeyUtc; // yyyy-MM-dd for the current day
        }

        private readonly List<QuestState> _quests = new List<QuestState>();

        public IReadOnlyList<QuestState> Quests => _quests;

        public QuestTracker()
        {
            // Default starter quests if no config-driven quests are injected.
            AddQuest(new QuestDefinition
            {
                Id = "daily_hygiene",
                Title = "Hygiene Hero",
                Description = "Do at least 2 hygiene actions today (e.g. brush teeth, shower).",
                TargetCategory = HabitCategory.Hygiene,
                RequiredCountToday = 2
            });

            AddQuest(new QuestDefinition
            {
                Id = "daily_sleep",
                Title = "Sleep Steward",
                Description = "Log both a sleep and a wake-up today.",
                TargetCategory = HabitCategory.Sleep,
                RequiredCountToday = 2
            });

            AddQuest(new QuestDefinition
            {
                Id = "daily_hydration",
                Title = "Hydration Habit",
                Description = "Log at least 3 hydration actions (drinking water).",
                TargetCategory = HabitCategory.Hydration,
                RequiredCountToday = 3
            });

            AddQuest(new QuestDefinition
            {
                Id = "daily_exercise",
                Title = "Move It",
                Description = "Log at least one exercise session today.",
                TargetCategory = HabitCategory.Exercise,
                RequiredCountToday = 1
            });

            AddQuest(new QuestDefinition
            {
                Id = "daily_nutrition",
                Title = "Well Fed",
                Description = "Log all three meals today.",
                TargetCategory = HabitCategory.Nutrition,
                RequiredCountToday = 3
            });
        }

        public void LoadFromConfig(QuestConfig config)
        {
            _quests.Clear();
            if (config == null || config.quests == null || config.quests.Count == 0)
            {
                // Fall back to defaults defined in the constructor.
                return;
            }

            foreach (var def in config.quests)
            {
                if (def == null) continue;
                AddQuest(def);
            }
        }

        private void AddQuest(QuestDefinition def)
        {
            _quests.Add(new QuestState
            {
                Definition = def,
                CompletedCountToday = 0,
                IsCompletedToday = false,
                DateKeyUtc = CurrentDateKey()
            });
        }

        private static string CurrentDateKey()
        {
            return DateTime.UtcNow.ToString("yyyy-MM-dd");
        }

        private void EnsureDate(QuestState state)
        {
            var today = CurrentDateKey();
            if (state.DateKeyUtc != today)
            {
                state.DateKeyUtc = today;
                state.CompletedCountToday = 0;
                state.IsCompletedToday = false;
            }
        }

        public void RegisterAction(UserAction action)
        {
            if (action == null) return;

            foreach (var quest in _quests)
            {
                EnsureDate(quest);

                if (quest.IsCompletedToday) continue;
                if (quest.Definition.TargetCategory != action.Category) continue;

                quest.CompletedCountToday++;
                if (quest.CompletedCountToday >= quest.Definition.RequiredCountToday)
                {
                    quest.IsCompletedToday = true;
                }
            }
        }

        /// <summary>
        /// Returns current quest state serialized for persistence.
        /// </summary>
        public List<QuestStateSaveData> GetSaveData()
        {
            var result = new List<QuestStateSaveData>(_quests.Count);
            foreach (var quest in _quests)
            {
                result.Add(new QuestStateSaveData
                {
                    QuestId = quest.Definition.Id,
                    CompletedCountToday = quest.CompletedCountToday,
                    IsCompletedToday = quest.IsCompletedToday,
                    DateKeyUtc = quest.DateKeyUtc
                });
            }
            return result;
        }

        /// <summary>
        /// Restores today's quest progress from persisted data.
        /// Stale data (from a previous day) is silently ignored.
        /// </summary>
        public void LoadSaveData(IReadOnlyList<QuestStateSaveData> data)
        {
            if (data == null) return;

            var today = CurrentDateKey();
            foreach (var saved in data)
            {
                if (saved == null || saved.DateKeyUtc != today) continue;

                var quest = _quests.Find(q => q.Definition.Id == saved.QuestId);
                if (quest == null) continue;

                quest.CompletedCountToday = saved.CompletedCountToday;
                quest.IsCompletedToday = saved.IsCompletedToday;
                quest.DateKeyUtc = saved.DateKeyUtc;
            }
        }
    }
}
