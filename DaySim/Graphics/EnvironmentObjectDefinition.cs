using System;
using UnityEngine;

namespace DaySim.Graphics
{
    /// <summary>
    /// Descriptor for a single environment object (e.g. bed, sink).
    /// </summary>
    [Serializable]
    public class EnvironmentObjectDefinition
    {
        public string id;
        public string displayName;
        public GameObject prefab;

        [Tooltip("Position in world units where this object should be spawned.")]
        public Vector2 position;

        [Tooltip("Optional sorting order override for fine control.")]
        public int sortingOrderOverride = 0;
    }
}

