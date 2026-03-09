using System;
using UnityEngine;

namespace DaySim
{
    public enum HabitCategory
    {
        Unknown = 0,
        Sleep = 10,
        Hygiene = 20,
        Nutrition = 30,
        Hydration = 40,
        Exercise = 50,
        WorkStudy = 60,
        Relaxation = 70,
    }

    /// <summary>
    /// Categories of user actions that the avatar can mirror.
    /// Extend this enum as you add more habits and activities.
    /// </summary>
    public enum UserActionType
    {
        Unknown = 0,
        WakeUp = 10,
        BrushTeeth = 20,
        EatBreakfast = 30,
        EatLunch = 31,
        EatDinner = 32,
        DrinkWater = 40,
        Exercise = 50,
        Study = 60,
        Work = 70,
        Relax = 80,
        Sleep = 90
    }

    /// <summary>
    /// Represents a single real-world action logged by the user.
    /// </summary>
    [Serializable]
    public class UserAction
    {
        public UserActionType ActionType;
        public DateTime TimestampUtc;
        public float EstimatedDurationMinutes;
        public string RawText;
        public HabitCategory Category;

        public UserAction(
            UserActionType actionType,
            DateTime timestampUtc,
            float estimatedDurationMinutes,
            string rawText)
        {
            ActionType = actionType;
            TimestampUtc = timestampUtc;
            EstimatedDurationMinutes = estimatedDurationMinutes;
            RawText = rawText;
            Category = MapActionTypeToCategory(actionType);
        }

        /// <summary>
        /// Create an action representing "right now" with a best-guess duration.
        /// </summary>
        public static UserAction CreateNow(UserActionType type, string rawText, float defaultDurationMinutes = 10f)
        {
            return new UserAction(
                type,
                DateTime.UtcNow,
                defaultDurationMinutes,
                rawText
            );
        }

        /// <summary>
        /// Parses free-text into a UserAction. Returns null for unrecognized input
        /// so callers can give the user helpful feedback without logging a no-op.
        /// Supports duration hints such as "ran for 30 minutes" or "studied 2 hours".
        /// </summary>
        public static UserAction ParseFromText(string text)
        {
            if (string.IsNullOrWhiteSpace(text))
                return null;

            var normalized = text.Trim().ToLowerInvariant();
            var type = DetectActionType(normalized);

            if (type == UserActionType.Unknown)
                return null;

            var duration = ExtractDurationMinutes(normalized, GetDefaultDuration(type));
            return new UserAction(type, DateTime.UtcNow, duration, text.Trim());
        }

        private static UserActionType DetectActionType(string n)
        {
            // ---- Sleep / WakeUp ----
            if (Contains(n, "wake", "woke", "got up", "get up", "rise", "arose", "morning", "out of bed"))
                return UserActionType.WakeUp;

            if (Contains(n, "sleep", "slept", "went to bed", "bedtime", "good night", "hit the bed", "nap", "napped"))
                return UserActionType.Sleep;

            // ---- Hygiene ----
            if (Contains(n, "brush", "teeth", "floss", "flossed", "mouthwash",
                         "shower", "showered", "bathe", "bathed", "bath",
                         "shampoo", "wash hair", "washed hair", "groomed"))
                return UserActionType.BrushTeeth;

            // ---- Nutrition — try to detect meal by time-of-day words first ----
            if (Contains(n, "breakfast", "break fast", "morning meal", "ate in the morning"))
                return UserActionType.EatBreakfast;

            if (Contains(n, "lunch", "midday meal", "afternoon meal", "lunchtime"))
                return UserActionType.EatLunch;

            if (Contains(n, "dinner", "supper", "evening meal", "night meal"))
                return UserActionType.EatDinner;

            // Generic eating — guess meal by local hour
            if (Contains(n, "eat", "ate", "meal", "food", "snack", "cooked", "cook", "had food",
                         "sandwich", "salad", "pizza", "burger", "pasta", "rice", "soup"))
            {
                var hour = DateTime.Now.Hour;
                if (hour < 11) return UserActionType.EatBreakfast;
                if (hour < 16) return UserActionType.EatLunch;
                return UserActionType.EatDinner;
            }

            // ---- Hydration ----
            if (Contains(n, "water", "drink", "drank", "hydrat", "juice", "tea", "coffee", "smoothie", "beverage"))
                return UserActionType.DrinkWater;

            // ---- Exercise ----
            if (Contains(n, "exercise", "workout", "gym", "jog", "jogged", "run", "ran", "running",
                         "walk", "walked", "hike", "hiked", "swim", "swam",
                         "yoga", "pilates", "crossfit", "lift", "lifted", "weights",
                         "bike", "biked", "cycling", "cycle", "push-up", "pushup",
                         "squat", "stretch", "stretching", "cardio", "training", "trained",
                         "sport", "tennis", "football", "soccer", "basketball", "climb", "climbing"))
                return UserActionType.Exercise;

            // ---- Study ----
            if (Contains(n, "study", "studied", "learn", "learned", "class", "lecture",
                         "homework", "assignment", "revision", "revise", "read", "reading",
                         "research", "course", "tutorial", "practise", "practice"))
                return UserActionType.Study;

            // ---- Work ----
            if (Contains(n, "work", "worked", "job", "office", "meeting", "call", "email",
                         "project", "task", "coding", "code", "coded", "programming",
                         "presentation", "report", "deadline", "client", "boss"))
                return UserActionType.Work;

            // ---- Relax ----
            if (Contains(n, "relax", "relaxed", "chill", "chilled", "rest", "rested",
                         "meditate", "meditation", "game", "gaming", "gamed",
                         "watch", "movie", "tv", "netflix", "youtube", "music",
                         "podcast", "read a book", "reading a book", "hobby", "hobbies",
                         "hang out", "hangout", "friends", "social", "party", "chat"))
                return UserActionType.Relax;

            return UserActionType.Unknown;
        }

        /// <summary>
        /// Extracts a duration in minutes from phrases like "for 30 minutes", "2 hours", "1.5 hr".
        /// Returns defaultMinutes if no duration is found.
        /// </summary>
        private static float ExtractDurationMinutes(string text, float defaultMinutes)
        {
            // Look for patterns: <number> [minutes|mins|min|hours|hour|hrs|hr|h]
            var words = text.Split(new[] { ' ', '\t', ',', ';' }, StringSplitOptions.RemoveEmptyEntries);

            for (int i = 0; i < words.Length - 1; i++)
            {
                float value;
                if (!TryParseFloat(words[i], out value) || value <= 0f)
                    continue;

                var unit = words[i + 1].TrimEnd('.', ',', ';');
                if (IsHoursWord(unit))
                    return value * 60f;
                if (IsMinutesWord(unit))
                    return value;
            }

            // Also check if the last word before "min/hr" is the number (e.g., "30-minute run")
            for (int i = 0; i < words.Length; i++)
            {
                var w = words[i];
                if (w.Contains("min") || w.Contains("hour") || w.Contains("hr"))
                {
                    // Try to find a leading number, e.g. "30-minute"
                    var numPart = w.Split('-')[0];
                    float value;
                    if (TryParseFloat(numPart, out value) && value > 0f)
                    {
                        return w.Contains("hour") || w.StartsWith("hr") ? value * 60f : value;
                    }
                }
            }

            return defaultMinutes;
        }

        private static bool TryParseFloat(string s, out float result)
        {
            return float.TryParse(s, System.Globalization.NumberStyles.Any,
                System.Globalization.CultureInfo.InvariantCulture, out result);
        }

        private static bool IsHoursWord(string w) =>
            w == "hour" || w == "hours" || w == "hr" || w == "hrs" || w == "h";

        private static bool IsMinutesWord(string w) =>
            w == "minute" || w == "minutes" || w == "min" || w == "mins" || w == "m";

        private static bool Contains(string text, params string[] keywords)
        {
            foreach (var kw in keywords)
            {
                if (text.Contains(kw))
                    return true;
            }
            return false;
        }

        private static float GetDefaultDuration(UserActionType type)
        {
            switch (type)
            {
                case UserActionType.Sleep:    return 480f; // 8 hours
                case UserActionType.WakeUp:   return 5f;
                case UserActionType.Exercise: return 30f;
                case UserActionType.Study:
                case UserActionType.Work:     return 60f;
                case UserActionType.Relax:    return 30f;
                case UserActionType.EatBreakfast:
                case UserActionType.EatLunch:
                case UserActionType.EatDinner: return 20f;
                case UserActionType.BrushTeeth: return 5f;
                case UserActionType.DrinkWater: return 2f;
                default:                      return 10f;
            }
        }

        public override string ToString()
        {
            return $"{ActionType} ({Category}) at {TimestampUtc:u} ({EstimatedDurationMinutes:F1} min) – \"{RawText}\"";
        }

        private static HabitCategory MapActionTypeToCategory(UserActionType type)
        {
            switch (type)
            {
                case UserActionType.WakeUp:
                case UserActionType.Sleep:
                    return HabitCategory.Sleep;
                case UserActionType.BrushTeeth:
                    return HabitCategory.Hygiene;
                case UserActionType.EatBreakfast:
                case UserActionType.EatLunch:
                case UserActionType.EatDinner:
                    return HabitCategory.Nutrition;
                case UserActionType.DrinkWater:
                    return HabitCategory.Hydration;
                case UserActionType.Exercise:
                    return HabitCategory.Exercise;
                case UserActionType.Study:
                case UserActionType.Work:
                    return HabitCategory.WorkStudy;
                case UserActionType.Relax:
                    return HabitCategory.Relaxation;
                case UserActionType.Unknown:
                default:
                    return HabitCategory.Unknown;
            }
        }
    }
}
