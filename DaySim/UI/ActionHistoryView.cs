using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.UI;

namespace DaySim.UI
{
    /// <summary>
    /// Simple scrolling history view that lists recent logged actions.
    /// Attach this to a GameObject with a Text inside a ScrollRect.
    /// </summary>
    public class ActionHistoryView : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private DaySimManager daySimManager;
        [SerializeField] private Text historyText;

        [Header("Display")]
        [SerializeField] private int maxEntries = 50;

        private readonly List<UserAction> _buffer = new List<UserAction>();

        private void Awake()
        {
            if (historyText == null)
            {
                historyText = GetComponentInChildren<Text>();
            }
        }

        private void OnEnable()
        {
            if (daySimManager != null)
            {
                daySimManager.OnUserActionLogged += HandleUserActionLogged;
            }
        }

        private void OnDisable()
        {
            if (daySimManager != null)
            {
                daySimManager.OnUserActionLogged -= HandleUserActionLogged;
            }
        }

        private void HandleUserActionLogged(UserAction action)
        {
            if (action == null) return;

            _buffer.Add(action);
            if (_buffer.Count > maxEntries)
            {
                _buffer.RemoveAt(0);
            }

            RefreshHistoryUI();
        }

        private void RefreshHistoryUI()
        {
            if (historyText == null) return;

            var sb = new StringBuilder();
            foreach (var action in _buffer)
            {
                sb.AppendLine($"{action.TimestampUtc:HH:mm} - {action.ActionType} ({action.Category})");
            }

            historyText.text = sb.ToString();
        }
    }
}

