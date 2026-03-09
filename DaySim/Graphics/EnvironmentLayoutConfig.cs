using System.Collections.Generic;
using UnityEngine;

namespace DaySim.Graphics
{
    /// <summary>
    /// ScriptableObject describing the arrangement of core furniture/props
    /// (bed, sink, stove, desk, etc.) in the DaySim apartment.
    /// </summary>
    [CreateAssetMenu(menuName = "DaySim/Environment Layout", fileName = "DaySimEnvironmentLayout")]
    public class EnvironmentLayoutConfig : ScriptableObject
    {
        public List<EnvironmentObjectDefinition> objects = new List<EnvironmentObjectDefinition>();
    }
}

