using System;
using System.Collections.Generic;
using DaySim.Persistence;
using DaySim.Quests;

namespace DaySim.Achievements
{
    [Serializable]
    public class EarnedAchievement
    {
        public string Id;
        public DateTime EarnedAtUtc;
    }

    /// <summary>
    /// Tracks which achievements have been earned and checks unlock conditions
    /// after each logged action. Fire-and-forget: already-earned achievements
    /// are never re-awarded.
    /// </summary>
    public class AchievementTracker
    {
        private static readonly List<AchievementDefinition> AllAchievements = new List<AchievementDefinition>
        {
            new AchievementDefinition { Id = "first_action",      Title = "First Steps",        Description = "Log your very first action.",                         Trigger = AchievementTrigger.FirstAction },
            new AchievementDefinition { Id = "level_5",           Title = "Getting Serious",    Description = "Reach level 5.",                                     Trigger = AchievementTrigger.ReachLevel,         TriggerThreshold = 5  },
            new AchievementDefinition { Id = "level_10",          Title = "Habit Master",       Description = "Reach level 10.",                                    Trigger = AchievementTrigger.ReachLevel,         TriggerThreshold = 10 },
            new AchievementDefinition { Id = "level_25",          Title = "Life Optimized",     Description = "Reach level 25.",                                    Trigger = AchievementTrigger.ReachLevel,         TriggerThreshold = 25 },
            new AchievementDefinition { Id = "streak_3",          Title = "On a Roll",          Description = "Maintain a 3-day streak in any habit.",              Trigger = AchievementTrigger.AnyStreakDays,      TriggerThreshold = 3  },
            new AchievementDefinition { Id = "streak_7",          Title = "Week Warrior",       Description = "Maintain a 7-day streak in any habit.",              Trigger = AchievementTrigger.AnyStreakDays,      TriggerThreshold = 7  },
            new AchievementDefinition { Id = "streak_30",         Title = "Habit Locked",       Description = "Maintain a 30-day streak in any habit.",             Trigger = AchievementTrigger.AnyStreakDays,      TriggerThreshold = 30 },
            new AchievementDefinition { Id = "exercise_streak_7", Title = "Fitness Fanatic",    Description = "Exercise 7 days in a row.",                          Trigger = AchievementTrigger.CategoryStreakDays, TriggerThreshold = 7,  TriggerCategory = HabitCategory.Exercise  },
            new AchievementDefinition { Id = "hygiene_streak_7",  Title = "Clean Machine",      Description = "Maintain hygiene for 7 days in a row.",              Trigger = AchievementTrigger.CategoryStreakDays, TriggerThreshold = 7,  TriggerCategory = HabitCategory.Hygiene   },
            new AchievementDefinition { Id = "sleep_streak_7",    Title = "Sleep Champion",     Description = "Keep your sleep schedule for 7 days in a row.",      Trigger = AchievementTrigger.CategoryStreakDays, TriggerThreshold = 7,  TriggerCategory = HabitCategory.Sleep     },
            new AchievementDefinition { Id = "hydration_streak_7",Title = "Hydration Hero",     Description = "Log hydration actions for 7 consecutive days.",      Trigger = AchievementTrigger.CategoryStreakDays, TriggerThreshold = 7,  TriggerCategory = HabitCategory.Hydration },
            new AchievementDefinition { Id = "quest_all",         Title = "Quest Complete",     Description = "Finish every daily quest in a single day.",          Trigger = AchievementTrigger.CompleteAllDailyQuests },
            new AchievementDefinition { Id = "total_25",          Title = "Logging In",         Description = "Log 25 actions total.",                              Trigger = AchievementTrigger.TotalActionsLogged, TriggerThreshold = 25  },
            new AchievementDefinition { Id = "total_100",         Title = "Dedicated Logger",   Description = "Log 100 actions total.",                             Trigger = AchievementTrigger.TotalActionsLogged, TriggerThreshold = 100 },
            new AchievementDefinition { Id = "total_500",         Title = "Consistent Life",    Description = "Log 500 actions total.",                             Trigger = AchievementTrigger.TotalActionsLogged, TriggerThreshold = 500 },
        };

        private readonly HashSet<string> _earnedIds = new HashSet<string>();
        private readonly List<EarnedAchievement> _earned = new List<EarnedAchievement>();

        public IReadOnlyList<AchievementDefinition> All => AllAchievements;
        public IReadOnlyList<EarnedAchievement> Earned => _earned;

        /// <summary>Fired when a new achievement is unlocked.</summary>
        public event Action<AchievementDefinition> OnAchievementEarned;

        public bool IsEarned(string id) => _earnedIds.Contains(id);

        /// <summary>
        /// Restores earned achievements from a previous session without triggering events.
        /// </summary>
        public void LoadEarned(IReadOnlyList<AchievementSaveData> data)
        {
            _earnedIds.Clear();
            _earned.Clear();
            if (data == null) return;

            foreach (var d in data)
            {
                if (d == null || string.IsNullOrEmpty(d.AchievementId)) continue;
                _earnedIds.Add(d.AchievementId);

                DateTime earnedAt;
                if (!DateTime.TryParse(d.EarnedAtUtc, null,
                    System.Globalization.DateTimeStyles.RoundtripKind, out earnedAt))
                {
                    earnedAt = DateTime.UtcNow;
                }
                _earned.Add(new EarnedAchievement { Id = d.AchievementId, EarnedAtUtc = earnedAt });
            }
        }

        /// <summary>
        /// Returns earned achievements serialized for persistence.
        /// </summary>
        public List<AchievementSaveData> GetSaveData()
        {
            var result = new List<AchievementSaveData>(_earned.Count);
            foreach (var e in _earned)
            {
                result.Add(new AchievementSaveData
                {
                    AchievementId = e.Id,
                    EarnedAtUtc = e.EarnedAtUtc.ToString("o")
                });
            }
            return result;
        }

        /// <summary>
        /// Check all conditions after an action has been processed.
        /// Any newly satisfied achievements trigger OnAchievementEarned.
        /// </summary>
        public void CheckAfterAction(
            int totalActionsLogged,
            int currentLevel,
            IReadOnlyDictionary<HabitCategory, HabitStreakTracker.HabitStreak> streaks,
            IReadOnlyList<QuestTracker.QuestState> quests)
        {
            foreach (var def in AllAchievements)
            {
                if (_earnedIds.Contains(def.Id)) continue;

                bool earned = false;

                switch (def.Trigger)
                {
                    case AchievementTrigger.FirstAction:
                        earned = totalActionsLogged >= 1;
                        break;

                    case AchievementTrigger.ReachLevel:
                        earned = currentLevel >= def.TriggerThreshold;
                        break;

                    case AchievementTrigger.TotalActionsLogged:
                        earned = totalActionsLogged >= def.TriggerThreshold;
                        break;

                    case AchievementTrigger.AnyStreakDays:
                        if (streaks != null)
                        {
                            foreach (var kvp in streaks)
                            {
                                if (kvp.Value.CurrentStreakDays >= def.TriggerThreshold)
                                {
                                    earned = true;
                                    break;
                                }
                            }
                        }
                        break;

                    case AchievementTrigger.CategoryStreakDays:
                        if (streaks != null)
                        {
                            HabitStreakTracker.HabitStreak streak;
                            if (streaks.TryGetValue(def.TriggerCategory, out streak))
                                earned = streak.CurrentStreakDays >= def.TriggerThreshold;
                        }
                        break;

                    case AchievementTrigger.CompleteAllDailyQuests:
                        if (quests != null && quests.Count > 0)
                        {
                            bool allDone = true;
                            foreach (var q in quests)
                            {
                                if (!q.IsCompletedToday) { allDone = false; break; }
                            }
                            earned = allDone;
                        }
                        break;
                }

                if (earned)
                    Award(def);
            }
        }

        private void Award(AchievementDefinition def)
        {
            if (_earnedIds.Contains(def.Id)) return;

            _earnedIds.Add(def.Id);
            _earned.Add(new EarnedAchievement { Id = def.Id, EarnedAtUtc = DateTime.UtcNow });
            OnAchievementEarned?.Invoke(def);
        }
    }
}
