using System;
using DaySim.Config;

namespace DaySim
{
    /// <summary>
    /// Simple XP / Level system for the avatar.
    /// You can expand this later with separate tracks per habit category.
    /// </summary>
    [Serializable]
    public class AvatarStats
    {
        public int Level;
        public float CurrentXp;

        // Controls how quickly the player levels up. Defaults can be overridden by DaySimConfig.
        public float BaseXpPerLevel = 100f;
        public float LevelGrowthFactor = 1.2f;

        private DaySimConfig _config;

        public void ApplyConfig(DaySimConfig config)
        {
            _config = config;
            if (config == null) return;

            BaseXpPerLevel = config.baseXpPerLevel;
            LevelGrowthFactor = config.levelGrowthFactor;
        }

        public AvatarStats()
        {
            Level = 1;
            CurrentXp = 0f;
        }

        public void AddXp(float amount)
        {
            if (amount <= 0f)
            {
                return;
            }

            CurrentXp += amount;

            while (CurrentXp >= GetXpRequiredForNextLevel())
            {
                CurrentXp -= GetXpRequiredForNextLevel();
                Level++;
            }
        }

        public float GetXpRequiredForNextLevel()
        {
            return BaseXpPerLevel * (float)Math.Pow(LevelGrowthFactor, Level - 1);
        }

        public float GetXpProgressToNextLevel()
        {
            var required = GetXpRequiredForNextLevel();
            if (required <= 0f) return 0f;
            return CurrentXp / required;
        }

        /// <summary>
        /// MVP XP reward table per action type.
        /// Later this can be moved to data files or ScriptableObjects.
        /// </summary>
        public float GetXpRewardForAction(UserActionType actionType)
        {
            if (_config != null)
            {
                switch (actionType)
                {
                    case UserActionType.WakeUp:
                        return _config.wakeUpXp;
                    case UserActionType.BrushTeeth:
                        return _config.brushTeethXp;
                    case UserActionType.EatBreakfast:
                    case UserActionType.EatLunch:
                    case UserActionType.EatDinner:
                        return _config.mealXp;
                    case UserActionType.DrinkWater:
                        return _config.drinkWaterXp;
                    case UserActionType.Exercise:
                        return _config.exerciseXp;
                    case UserActionType.Study:
                    case UserActionType.Work:
                        return _config.workStudyXp;
                    case UserActionType.Relax:
                        return _config.relaxXp;
                    case UserActionType.Sleep:
                        return _config.sleepXp;
                    case UserActionType.Unknown:
                    default:
                        return _config.unknownXp;
                }
            }

            switch (actionType)
            {
                case UserActionType.WakeUp:
                    return 5f;
                case UserActionType.BrushTeeth:
                    return 10f;
                case UserActionType.EatBreakfast:
                case UserActionType.EatLunch:
                case UserActionType.EatDinner:
                    return 8f;
                case UserActionType.DrinkWater:
                    return 3f;
                case UserActionType.Exercise:
                    return 20f;
                case UserActionType.Study:
                case UserActionType.Work:
                    return 12f;
                case UserActionType.Relax:
                    return 4f;
                case UserActionType.Sleep:
                    return 10f;
                case UserActionType.Unknown:
                default:
                    return 1f;
            }
        }
    }
}

