using System;
using UnityEngine;

namespace DaySim.Needs
{
    /// <summary>
    /// High-level emotional state derived from needs.
    /// Can be used to drive avatar facial expressions, posture, or VFX.
    /// </summary>
    [Serializable]
    public enum Mood
    {
        VeryBad = 0,
        Bad = 10,
        Neutral = 20,
        Good = 30,
        Great = 40
    }

    public static class MoodUtility
    {
        // Critical need threshold: below this, the avatar feels awful regardless of other needs.
        private const float CriticalThreshold = 20f;

        // Max mood score when a critical need is depleted (forces Bad or worse).
        private const float CriticalMoodCap = 44f;

        /// <summary>
        /// Computes mood from needs using weighted scoring.
        /// Hunger and Energy are weighted higher because they have the strongest
        /// physical impact. A critically low critical need caps the mood at Bad.
        /// </summary>
        public static Mood ComputeMood(NeedsState state)
        {
            if (state == null) return Mood.Neutral;

            // Hunger and Energy have double weight — survival needs.
            float score = (state.Hunger * 2f + state.Energy * 2f +
                           state.Hygiene + state.Fun + state.Social) / 7f;

            // A critically depleted survival need forces mood down to Bad or worse.
            if (state.Hunger < CriticalThreshold || state.Energy < CriticalThreshold)
                score = Mathf.Min(score, CriticalMoodCap);

            if (score < 25f) return Mood.VeryBad;
            if (score < 45f) return Mood.Bad;
            if (score < 65f) return Mood.Neutral;
            if (score < 85f) return Mood.Good;
            return Mood.Great;
        }

        /// <summary>
        /// Returns a short human-readable label including the mood and any critical warnings.
        /// </summary>
        public static string GetMoodLabel(NeedsState state)
        {
            if (state == null) return "Neutral";

            var mood = ComputeMood(state);
            var label = mood.ToString();

            if (state.Hunger < CriticalThreshold)
                label += " (Starving!)";
            else if (state.Energy < CriticalThreshold)
                label += " (Exhausted!)";

            return label;
        }
    }
}
