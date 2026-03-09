using UnityEngine;
using DaySim.Graphics;
using DaySim.Needs;

namespace DaySim
{
    /// <summary>
    /// Controls the avatar's animations based on the current user action.
    /// Assumes an Animator with parameters configured for each action/state.
    /// </summary>
    public class AvatarController : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private Animator animator;
        [SerializeField] private SpriteRenderer spriteRenderer;
        [SerializeField] private AvatarVisualDefinition avatarVisual;

        // Optional: store last action for debugging or UI
        public UserActionType CurrentActionType { get; private set; } = UserActionType.Unknown;

        public Mood CurrentMood { get; private set; } = Mood.Neutral;

        private static readonly int StateHash = Animator.StringToHash("State");
        private static readonly int MoodHash = Animator.StringToHash("Mood");

        private void Awake()
        {
            if (animator == null)
            {
                animator = GetComponent<Animator>();
            }

            if (spriteRenderer == null)
            {
                spriteRenderer = GetComponentInChildren<SpriteRenderer>();
            }

            // Apply visual definition if provided (animator controller, preview sprite).
            if (avatarVisual != null)
            {
                if (animator != null && avatarVisual.animatorController != null)
                {
                    animator.runtimeAnimatorController = avatarVisual.animatorController;
                }

                if (spriteRenderer != null && avatarVisual.previewSprite != null)
                {
                    spriteRenderer.sprite = avatarVisual.previewSprite;
                }
            }
        }

        /// <summary>
        /// Called by DaySimManager when a new action is logged.
        /// </summary>
        public void OnUserAction(UserAction action)
        {
            if (action == null) return;
            CurrentActionType = action.ActionType;
            UpdateAnimatorForAction(action.ActionType);
        }

        /// <summary>
        /// Called by DaySimManager when needs/mood change significantly.
        /// </summary>
        public void OnMoodChanged(Mood mood)
        {
            CurrentMood = mood;
            UpdateAnimatorForMood(mood);
            UpdateTintForMood(mood);
        }

        private void UpdateAnimatorForAction(UserActionType actionType)
        {
            if (animator == null) return;

            // A simple integer "State" parameter can drive a blend tree or state machine.
            // Map action types to small integer IDs that your Animator understands.
            var stateId = MapActionTypeToStateId(actionType);
            animator.SetInteger(StateHash, stateId);
        }

        private int MapActionTypeToStateId(UserActionType actionType)
        {
            switch (actionType)
            {
                case UserActionType.WakeUp:
                    return 1;
                case UserActionType.BrushTeeth:
                    return 2;
                case UserActionType.EatBreakfast:
                case UserActionType.EatLunch:
                case UserActionType.EatDinner:
                    return 3;
                case UserActionType.DrinkWater:
                    return 4;
                case UserActionType.Exercise:
                    return 5;
                case UserActionType.Study:
                case UserActionType.Work:
                    return 6;
                case UserActionType.Relax:
                    return 7;
                case UserActionType.Sleep:
                    return 8;
                case UserActionType.Unknown:
                default:
                    return 0; // Idle / default
            }
        }

        private void UpdateAnimatorForMood(Mood mood)
        {
            if (animator == null) return;

            // Map mood enum to a small integer level for the Animator.
            var moodLevel = 0;
            switch (mood)
            {
                case Mood.VeryBad:
                    moodLevel = 0;
                    break;
                case Mood.Bad:
                    moodLevel = 1;
                    break;
                case Mood.Neutral:
                    moodLevel = 2;
                    break;
                case Mood.Good:
                    moodLevel = 3;
                    break;
                case Mood.Great:
                    moodLevel = 4;
                    break;
            }

            animator.SetInteger(MoodHash, moodLevel);
        }

        private void UpdateTintForMood(Mood mood)
        {
            if (spriteRenderer == null) return;

            switch (mood)
            {
                case Mood.VeryBad:
                    spriteRenderer.color = new Color(0.7f, 0.7f, 0.8f);
                    break;
                case Mood.Bad:
                    spriteRenderer.color = new Color(0.8f, 0.8f, 0.9f);
                    break;
                case Mood.Neutral:
                    spriteRenderer.color = Color.white;
                    break;
                case Mood.Good:
                    spriteRenderer.color = new Color(1.02f, 1.02f, 1.02f);
                    break;
                case Mood.Great:
                    spriteRenderer.color = new Color(1.05f, 1.05f, 1.05f);
                    break;
            }
        }
    }
}

