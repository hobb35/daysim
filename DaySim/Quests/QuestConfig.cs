using System.Collections.Generic;
using UnityEngine;

namespace DaySim.Quests
{
    /// <summary>
    /// ScriptableObject container for quest definitions so you can tune quests without code changes.
    /// Create via: Assets → Create → DaySim → Quest Config.
    /// </summary>
    [CreateAssetMenu(menuName = "DaySim/Quest Config", fileName = "DaySimQuestConfig")]
    public class QuestConfig : ScriptableObject
    {
        public List<QuestDefinition> quests = new List<QuestDefinition>();
    }
}

