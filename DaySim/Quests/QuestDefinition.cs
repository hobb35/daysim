using System;

namespace DaySim.Quests
{
    /// <summary>
    /// Simple quest definition. For MVP we can hardcode a few daily goals.
    /// </summary>
    [Serializable]
    public class QuestDefinition
    {
        public string Id;
        public string Title;
        public string Description;

        // Example: "Do at least 2 hygiene actions today"
        public HabitCategory TargetCategory;
        public int RequiredCountToday;
    }
}

