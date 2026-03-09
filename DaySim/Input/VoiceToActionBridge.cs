using UnityEngine;

namespace DaySim.Input
{
    /// <summary>
    /// Simple bridge between any speech-to-text system and DaySim.
    /// Call OnTranscriptionReceived from your voice layer when you have recognized text.
    /// </summary>
    public class VoiceToActionBridge : MonoBehaviour
    {
        [SerializeField] private DaySimManager daySimManager;

        private void Awake()
        {
            if (daySimManager == null)
            {
                daySimManager = FindObjectOfType<DaySimManager>();
            }
        }

        /// <summary>
        /// Hook this up from a speech recognition callback.
        /// </summary>
        public void OnTranscriptionReceived(string transcript)
        {
            if (daySimManager == null) return;
            daySimManager.SubmitExplicitText(transcript);
        }
    }
}

