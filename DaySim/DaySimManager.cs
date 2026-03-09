using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using DaySim.Achievements;
using DaySim.Config;
using DaySim.Needs;
using DaySim.Quests;

namespace DaySim
{
    /// <summary>
    /// Central MonoBehaviour that ties together input, logging, stats, and avatar.
    /// Drop this onto a GameObject in your scene and wire up the UI fields.
    /// </summary>
    public class DaySimManager : MonoBehaviour
    {
        [Header("UI")]
        [SerializeField] private InputField actionInputField;
        [SerializeField] private Button submitButton;
        [SerializeField] private Text currentActionText;
        [SerializeField] private Text levelText;
        [SerializeField] private Text xpText;
        [SerializeField] private Text streaksText;
        [SerializeField] private Text questsText;
        [SerializeField] private DaySimConfig config;
        [SerializeField] private QuestConfig questConfig;
        [SerializeField] private Text needsText;
        [SerializeField] private Text moodText;
        [SerializeField] private Text clockText;

        [Header("Achievement Banner")]
        [SerializeField] private Text achievementBannerText;
        [SerializeField] private float bannerDisplaySeconds = 4f;

        [Header("Avatar")]
        [SerializeField] private AvatarController avatarController;

        private readonly ActionLogger _actionLogger = new ActionLogger();
        private AvatarStats _avatarStats = new AvatarStats();
        private readonly HabitStreakTracker _habitStreakTracker = new HabitStreakTracker();
        private readonly QuestTracker _questTracker = new QuestTracker();
        private readonly NeedsSystem _needsSystem = new NeedsSystem();
        private readonly AchievementTracker _achievementTracker = new AchievementTracker();

        // Controls how fast in-game time passes relative to real time for needs decay.
        [SerializeField] private float hoursPerRealSecond = 0.1f;

        /// <summary>
        /// Fired after a user action has been fully processed (stats, streaks, quests, avatar).
        /// </summary>
        public event System.Action<UserAction> OnUserActionLogged;

        /// <summary>
        /// Fired when a new achievement is unlocked.
        /// </summary>
        public event System.Action<AchievementDefinition> OnAchievementUnlocked;

        private void Awake()
        {
            _actionLogger.OnActionLogged += HandleActionLogged;
            _achievementTracker.OnAchievementEarned += HandleAchievementEarned;

            if (submitButton != null)
                submitButton.onClick.AddListener(SubmitCurrentInput);
        }

        private void Start()
        {
            if (config != null)
            {
                _avatarStats.ApplyConfig(config);
                _needsSystem.ApplyConfig(config);
                hoursPerRealSecond = Mathf.Max(0.01f, config.hoursPerRealSecond);
            }

            if (questConfig != null)
                _questTracker.LoadFromConfig(questConfig);

            LoadSession();

            RefreshStatsUI();
            RefreshCurrentActionUI(null);
            RefreshNeedsUI();
            RefreshMoodUI();
        }

        private void LoadSession()
        {
            DaySimSaveSystem.DaySimLoadResult loaded;
            if (!DaySimSaveSystem.TryLoad(out loaded))
                return;

            if (loaded.AvatarStats != null)
                _avatarStats = loaded.AvatarStats;

            _actionLogger.LoadHistory(loaded.Actions);

            if (loaded.NeedsState != null)
                _needsSystem.State = loaded.NeedsState;

            // Restore streaks: use persisted data if available, otherwise rebuild from history.
            if (loaded.Streaks != null && loaded.Streaks.Count > 0)
                _habitStreakTracker.LoadStreakData(loaded.Streaks);
            else
                _habitStreakTracker.ReplayHistory(loaded.Actions);

            // Restore today's quest progress (stale data is silently ignored by QuestTracker).
            _questTracker.LoadSaveData(loaded.QuestStates);

            // Restore earned achievements (no events fired).
            _achievementTracker.LoadEarned(loaded.Achievements);

            // Sync avatar to last known action.
            var last = _actionLogger.GetMostRecentAction();
            if (last != null && avatarController != null)
                avatarController.OnUserAction(last);
        }

        // ── Public API ───────────────────────────────────────────────────────────

        public void SetActionInputField(InputField field)
        {
            actionInputField = field;
        }

        public AchievementTracker GetAchievementTracker() => _achievementTracker;

        public IReadOnlyList<UserAction> GetAllActions() => _actionLogger.Actions;

        /// <summary>
        /// Called by the UI submit button or programmatically when the user types an action.
        /// Shows a hint if the text cannot be recognized instead of silently discarding it.
        /// </summary>
        public void SubmitCurrentInput()
        {
            if (actionInputField == null) return;

            var text = actionInputField.text;
            if (string.IsNullOrWhiteSpace(text)) return;

            var action = UserAction.ParseFromText(text);
            if (action == null)
            {
                // Unrecognized — give the user a helpful nudge.
                if (currentActionText != null)
                    currentActionText.text = "Unrecognized — try: \"jogged 30 min\", \"ate lunch\", \"brushed teeth\"…";
                actionInputField.text = string.Empty;
                return;
            }

            _actionLogger.LogAction(action);
            actionInputField.text = string.Empty;
        }

        /// <summary>
        /// Allows other systems (e.g., voice-to-text) to submit actions directly as raw text.
        /// </summary>
        public void SubmitExplicitText(string text)
        {
            if (string.IsNullOrWhiteSpace(text)) return;

            var action = UserAction.ParseFromText(text);
            if (action != null)
                _actionLogger.LogAction(action);
        }

        // ── Update loop ──────────────────────────────────────────────────────────

        private void Update()
        {
            var deltaHours = Time.deltaTime * hoursPerRealSecond;
            _needsSystem.Tick(deltaHours);
            RefreshNeedsUI();
            RefreshMoodUI();
            RefreshClockUI();
        }

        // ── Event handlers ───────────────────────────────────────────────────────

        private void HandleActionLogged(UserAction action)
        {
            if (action == null) return;

            // XP
            var xp = _avatarStats.GetXpRewardForAction(action.ActionType);
            _avatarStats.AddXp(xp);
            RefreshStatsUI();

            // Needs
            _needsSystem.ApplyAction(action);
            RefreshNeedsUI();
            RefreshMoodUI();

            // Streaks
            _habitStreakTracker.RegisterAction(action);
            RefreshStreaksUI();

            // Quests
            _questTracker.RegisterAction(action);
            RefreshQuestsUI();

            // Avatar
            if (avatarController != null)
                avatarController.OnUserAction(action);

            // Current action label
            RefreshCurrentActionUI(action);

            // Achievements — check after all state is updated
            _achievementTracker.CheckAfterAction(
                _actionLogger.Actions.Count,
                _avatarStats.Level,
                _habitStreakTracker.Streaks,
                _questTracker.Quests);

            // Persist everything
            SaveSession();

            OnUserActionLogged?.Invoke(action);
        }

        private void HandleAchievementEarned(AchievementDefinition def)
        {
            OnAchievementUnlocked?.Invoke(def);

            if (achievementBannerText != null)
            {
                achievementBannerText.text = $"Achievement Unlocked: {def.Title}!";
                StopCoroutine("ClearAchievementBanner");
                StartCoroutine(ClearAchievementBanner());
            }
        }

        private IEnumerator ClearAchievementBanner()
        {
            yield return new WaitForSeconds(bannerDisplaySeconds);
            if (achievementBannerText != null)
                achievementBannerText.text = string.Empty;
        }

        // ── Persistence ──────────────────────────────────────────────────────────

        private void SaveSession()
        {
            DaySimSaveSystem.Save(
                _avatarStats,
                _actionLogger.Actions,
                _needsSystem.State,
                _habitStreakTracker.GetStreakSaveData(),
                _questTracker.GetSaveData(),
                _achievementTracker.GetSaveData());
        }

        // ── UI refresh helpers ───────────────────────────────────────────────────

        private void RefreshStreaksUI()
        {
            if (streaksText == null) return;

            var summary = "";
            foreach (var kvp in _habitStreakTracker.Streaks)
            {
                var streak = kvp.Value;
                if (streak.CurrentStreakDays <= 0) continue;

                if (summary.Length > 0)
                    summary += " | ";

                summary += $"{streak.Category}: {streak.CurrentStreakDays}d (best {streak.BestStreakDays})";
            }

            streaksText.text = string.IsNullOrEmpty(summary) ? "Streaks: none yet" : summary;
        }

        private void RefreshQuestsUI()
        {
            if (questsText == null) return;

            var lines = "";
            foreach (var quest in _questTracker.Quests)
            {
                if (lines.Length > 0)
                    lines += "\n";

                var status = quest.IsCompletedToday
                    ? "✓"
                    : $"{quest.CompletedCountToday}/{quest.Definition.RequiredCountToday}";

                lines += $"{quest.Definition.Title}: {status}";
            }

            questsText.text = lines;
        }

        private void RefreshStatsUI()
        {
            if (levelText != null)
                levelText.text = $"Level: {_avatarStats.Level}";

            if (xpText != null)
            {
                var progress = _avatarStats.GetXpProgressToNextLevel() * 100f;
                xpText.text = $"XP: {_avatarStats.CurrentXp:F1} ({progress:F0}% to next level)";
            }
        }

        private void RefreshCurrentActionUI(UserAction action)
        {
            if (currentActionText == null) return;

            currentActionText.text = action == null
                ? "Current: Idle"
                : $"Current: {action.ActionType} ({action.RawText})";
        }

        private void RefreshNeedsUI()
        {
            if (needsText == null) return;

            var s = _needsSystem.State;
            needsText.text =
                $"Hunger {s.Hunger:F0}  |  Energy {s.Energy:F0}\n" +
                $"Hygiene {s.Hygiene:F0}  |  Fun {s.Fun:F0}  |  Social {s.Social:F0}";
        }

        private void RefreshMoodUI()
        {
            if (moodText == null) return;

            var mood = MoodUtility.ComputeMood(_needsSystem.State);
            moodText.text = $"Mood: {MoodUtility.GetMoodLabel(_needsSystem.State)}";

            if (avatarController != null)
                avatarController.OnMoodChanged(mood);
        }

        private void RefreshClockUI()
        {
            if (clockText == null) return;
            clockText.text = System.DateTime.Now.ToString("HH:mm");
        }
    }
}
