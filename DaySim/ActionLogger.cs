using System;
using System.Collections.Generic;

namespace DaySim
{
    /// <summary>
    /// Central log of all user actions for the current (and future) sessions.
    /// For MVP this is in-memory; you can later add disk persistence.
    /// </summary>
    [Serializable]
    public class ActionLogger
    {
        private readonly List<UserAction> _actions = new List<UserAction>();

        public event Action<UserAction> OnActionLogged;

        public IReadOnlyList<UserAction> Actions => _actions;

        public void LogAction(UserAction action)
        {
            if (action == null) return;

            _actions.Add(action);
            OnActionLogged?.Invoke(action);
        }

        public UserAction GetMostRecentAction()
        {
            if (_actions.Count == 0) return null;
            return _actions[_actions.Count - 1];
        }

        /// <summary>
        /// Replace current actions with a preloaded history (e.g. from disk).
        /// </summary>
        public void LoadHistory(IEnumerable<UserAction> actions)
        {
            _actions.Clear();
            if (actions == null) return;

            foreach (var action in actions)
            {
                if (action == null) continue;
                _actions.Add(action);
            }
        }
    }
}

