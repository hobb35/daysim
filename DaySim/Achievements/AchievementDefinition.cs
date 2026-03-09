using System;

namespace DaySim.Achievements
{
    public enum AchievementTrigger
    {
        FirstAction,         // Awarded after logging the very first action
        ReachLevel,          // Awarded when avatar reaches a target level
        AnyStreakDays,        // Awarded when any habit reaches N consecutive days
        CategoryStreakDays,  // Awarded when a specific habit reaches N consecutive days
        CompleteAllDailyQuests, // Awarded when every quest is completed in one day
        TotalActionsLogged,  // Awarded when the lifetime action count hits N
    }

    [Serializable]
    public class AchievementDefinition
    {
        public string Id;
        public string Title;
        public string Description;
        public AchievementTrigger Trigger;
        public int TriggerThreshold;          // Level, streak length, or action count target
        public HabitCategory TriggerCategory; // Used only by CategoryStreakDays
    }
}
