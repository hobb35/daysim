using UnityEngine;

namespace DaySim.Config
{
    /// <summary>
    /// Central ScriptableObject for tuning DaySim without touching code.
    /// Create via: Assets → Create → DaySim → Config.
    /// </summary>
    [CreateAssetMenu(menuName = "DaySim/Config", fileName = "DaySimConfig")]
    public class DaySimConfig : ScriptableObject
    {
        [Header("XP Curve")]
        public float baseXpPerLevel = 100f;
        public float levelGrowthFactor = 1.2f;

        [Header("XP Rewards")]
        public float wakeUpXp = 5f;
        public float brushTeethXp = 10f;
        public float mealXp = 8f;
        public float drinkWaterXp = 3f;
        public float exerciseXp = 20f;
        public float workStudyXp = 12f;
        public float relaxXp = 4f;
        public float sleepXp = 10f;
        public float unknownXp = 1f;

        [Header("Needs Decay (per in-game hour)")]
        public float hungerDecayPerHour = -8f;
        public float energyDecayPerHour = -5f;
        public float hygieneDecayPerHour = -3f;
        public float funDecayPerHour = -2f;
        public float socialDecayPerHour = -2f;

        [Header("Time Scale")]
        [Tooltip("How many in-game hours pass per real-time second. A value of 0.1 means 6 in-game minutes per real second.")]
        public float hoursPerRealSecond = 0.1f;
    }
}

