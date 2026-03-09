using System;
using System.Collections.Generic;
using System.IO;
using DaySim.Needs;
using DaySim.Persistence;
using UnityEngine;

namespace DaySim
{
    /// <summary>
    /// Handles JSON save/load of DaySim state to Application.persistentDataPath.
    /// Keeps runtime classes decoupled from storage format.
    /// </summary>
    public static class DaySimSaveSystem
    {
        private const string SaveFileName = "daysim_save.json";

        private static string SaveFilePath =>
            Path.Combine(Application.persistentDataPath, SaveFileName);

        public static void Save(
            AvatarStats avatarStats,
            IReadOnlyList<UserAction> actions,
            NeedsState needsState,
            List<HabitStreakSaveData> streaks,
            List<QuestStateSaveData> questStates,
            List<AchievementSaveData> achievements)
        {
            if (avatarStats == null) throw new ArgumentNullException(nameof(avatarStats));
            if (actions == null) throw new ArgumentNullException(nameof(actions));

            var data = new DaySimSaveData
            {
                AvatarStats = new AvatarStatsData
                {
                    Level = avatarStats.Level,
                    CurrentXp = avatarStats.CurrentXp
                },
                NeedsState = needsState != null
                    ? new NeedsStateData
                    {
                        Hunger = needsState.Hunger,
                        Energy = needsState.Energy,
                        Hygiene = needsState.Hygiene,
                        Fun = needsState.Fun,
                        Social = needsState.Social
                    }
                    : null,
                LastSavedUtc = DateTime.UtcNow.ToString("o")
            };

            foreach (var action in actions)
            {
                if (action == null) continue;

                data.Actions.Add(new UserActionData
                {
                    ActionType = (int)action.ActionType,
                    TimestampUtc = action.TimestampUtc.ToString("o"),
                    EstimatedDurationMinutes = action.EstimatedDurationMinutes,
                    RawText = action.RawText
                });
            }

            if (streaks != null)
                data.Streaks.AddRange(streaks);

            if (questStates != null)
                data.QuestStates.AddRange(questStates);

            if (achievements != null)
                data.Achievements.AddRange(achievements);

            var json = JsonUtility.ToJson(data, true);

            try
            {
                var directory = Path.GetDirectoryName(SaveFilePath);
                if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                }

                File.WriteAllText(SaveFilePath, json);
            }
            catch (Exception ex)
            {
                Debug.LogError($"DaySimSaveSystem.Save failed: {ex}");
            }
        }

        public static bool TryLoad(out DaySimLoadResult result)
        {
            result = null;

            if (!File.Exists(SaveFilePath))
                return false;

            try
            {
                var json = File.ReadAllText(SaveFilePath);
                if (string.IsNullOrWhiteSpace(json))
                    return false;

                var data = JsonUtility.FromJson<DaySimSaveData>(json);
                if (data == null)
                    return false;

                result = new DaySimLoadResult
                {
                    AvatarStats = new AvatarStats
                    {
                        Level = data.AvatarStats != null ? data.AvatarStats.Level : 1,
                        CurrentXp = data.AvatarStats != null ? data.AvatarStats.CurrentXp : 0f
                    }
                };

                if (data.NeedsState != null)
                {
                    result.NeedsState = new NeedsState
                    {
                        Hunger = data.NeedsState.Hunger,
                        Energy = data.NeedsState.Energy,
                        Hygiene = data.NeedsState.Hygiene,
                        Fun = data.NeedsState.Fun,
                        Social = data.NeedsState.Social
                    };
                    result.NeedsState.ClampAll();
                }

                if (data.Actions != null)
                {
                    foreach (var a in data.Actions)
                    {
                        if (a == null) continue;

                        DateTime timestamp;
                        if (!DateTime.TryParse(a.TimestampUtc, null,
                            System.Globalization.DateTimeStyles.RoundtripKind, out timestamp))
                        {
                            timestamp = DateTime.UtcNow;
                        }

                        result.Actions.Add(new UserAction(
                            (UserActionType)a.ActionType,
                            timestamp,
                            a.EstimatedDurationMinutes,
                            a.RawText
                        ));
                    }
                }

                if (data.Streaks != null)
                    result.Streaks.AddRange(data.Streaks);

                if (data.QuestStates != null)
                    result.QuestStates.AddRange(data.QuestStates);

                if (data.Achievements != null)
                    result.Achievements.AddRange(data.Achievements);

                return true;
            }
            catch (Exception ex)
            {
                Debug.LogError($"DaySimSaveSystem.TryLoad failed: {ex}");
                return false;
            }
        }
    }
}
