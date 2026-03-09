using System;
using DaySim.Config;

namespace DaySim.Needs
{
    /// <summary>
    /// Applies passive decay and action-based adjustments to needs.
    /// </summary>
    [Serializable]
    public class NeedsSystem
    {
        public NeedsState State = new NeedsState();

        // Per-hour passive decay (negative means gets worse over time).
        public float HungerDecayPerHour = -8f;
        public float EnergyDecayPerHour = -5f;
        public float HygieneDecayPerHour = -3f;
        public float FunDecayPerHour = -2f;
        public float SocialDecayPerHour = -2f;

        private DaySimConfig _config;

        public void ApplyConfig(DaySimConfig config)
        {
            _config = config;
            if (config == null) return;

            HungerDecayPerHour = config.hungerDecayPerHour;
            EnergyDecayPerHour = config.energyDecayPerHour;
            HygieneDecayPerHour = config.hygieneDecayPerHour;
            FunDecayPerHour = config.funDecayPerHour;
            SocialDecayPerHour = config.socialDecayPerHour;
        }

        public void Tick(float deltaHours)
        {
            if (Math.Abs(deltaHours) < 0.0001f) return;

            State.Hunger += HungerDecayPerHour * deltaHours;
            State.Energy += EnergyDecayPerHour * deltaHours;
            State.Hygiene += HygieneDecayPerHour * deltaHours;
            State.Fun += FunDecayPerHour * deltaHours;
            State.Social += SocialDecayPerHour * deltaHours;

            State.ClampAll();
        }

        /// <summary>
        /// Apply an immediate adjustment to needs based on a logged action.
        /// </summary>
        public void ApplyAction(UserAction action)
        {
            if (action == null) return;

            switch (action.ActionType)
            {
                case UserActionType.EatBreakfast:
                case UserActionType.EatLunch:
                case UserActionType.EatDinner:
                    State.Hunger += 25f;
                    State.Energy += 5f;
                    State.Social += 8f;   // meals are often social
                    break;
                case UserActionType.DrinkWater:
                    State.Hunger += 5f;   // feels slightly better
                    break;
                case UserActionType.Sleep:
                    State.Energy += 40f;
                    State.Hygiene -= 5f;  // mild overnight hygiene decay
                    break;
                case UserActionType.WakeUp:
                    State.Energy += 10f;
                    break;
                case UserActionType.BrushTeeth:
                    State.Hygiene += 20f;
                    State.Fun += 2f;      // small mood boost from fresh feeling
                    break;
                case UserActionType.Exercise:
                    State.Energy -= 10f;
                    State.Fun += 15f;
                    State.Hygiene -= 10f;
                    State.Social += 5f;   // gym / outdoor activity is social
                    break;
                case UserActionType.Relax:
                    State.Fun += 20f;
                    State.Energy += 5f;
                    State.Social += 10f;  // downtime often involves social connection
                    break;
                case UserActionType.Study:
                case UserActionType.Work:
                    State.Fun -= 5f;
                    State.Energy -= 5f;
                    State.Social -= 3f;   // focused solo work reduces social
                    break;
                default:
                    break;
            }

            State.ClampAll();
        }
    }
}
