using System;
using System.Collections.Generic;

namespace DaySim.Persistence
{
    /// <summary>
    /// Serializable DTO used for saving/loading DaySim state as JSON.
    /// Stored separately from runtime classes so it can evolve more easily.
    /// </summary>
    [Serializable]
    public class DaySimSaveData
    {
        public AvatarStatsData AvatarStats;
        public NeedsStateData NeedsState;
        public List<UserActionData> Actions = new List<UserActionData>();
        public List<HabitStreakSaveData> Streaks = new List<HabitStreakSaveData>();
        public List<QuestStateSaveData> QuestStates = new List<QuestStateSaveData>();
        public List<AchievementSaveData> Achievements = new List<AchievementSaveData>();
        public string LastSavedUtc;
    }

    [Serializable]
    public class AvatarStatsData
    {
        public int Level;
        public float CurrentXp;
    }

    [Serializable]
    public class NeedsStateData
    {
        public float Hunger;
        public float Energy;
        public float Hygiene;
        public float Fun;
        public float Social;
    }

    [Serializable]
    public class UserActionData
    {
        public int ActionType;
        public string TimestampUtc;
        public float EstimatedDurationMinutes;
        public string RawText;
    }

    [Serializable]
    public class HabitStreakSaveData
    {
        public int Category;            // HabitCategory enum stored as int
        public int CurrentStreakDays;
        public int BestStreakDays;
        public string LastActionDateUtc; // yyyy-MM-dd
    }

    [Serializable]
    public class QuestStateSaveData
    {
        public string QuestId;
        public int CompletedCountToday;
        public bool IsCompletedToday;
        public string DateKeyUtc;       // yyyy-MM-dd
    }

    [Serializable]
    public class AchievementSaveData
    {
        public string AchievementId;
        public string EarnedAtUtc;
    }

    /// <summary>
    /// Result object returned by TryLoad to avoid a long out-param list.
    /// </summary>
    public class DaySimLoadResult
    {
        public AvatarStats AvatarStats;
        public List<UserAction> Actions = new List<UserAction>();
        public Needs.NeedsState NeedsState;
        public List<HabitStreakSaveData> Streaks = new List<HabitStreakSaveData>();
        public List<QuestStateSaveData> QuestStates = new List<QuestStateSaveData>();
        public List<AchievementSaveData> Achievements = new List<AchievementSaveData>();
    }
}
