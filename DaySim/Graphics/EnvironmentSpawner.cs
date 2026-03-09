using UnityEngine;

namespace DaySim.Graphics
{
    /// <summary>
    /// Spawns environment objects (furniture/props) from an EnvironmentLayoutConfig.
    /// Drop this onto an empty GameObject in your scene and assign a layout asset.
    /// </summary>
    public class EnvironmentSpawner : MonoBehaviour
    {
        [SerializeField] private EnvironmentLayoutConfig layout;

        private void Start()
        {
            if (layout == null || layout.objects == null)
            {
                return;
            }

            foreach (var obj in layout.objects)
            {
                if (obj == null || obj.prefab == null) continue;

                var instance = Instantiate(obj.prefab, obj.position, Quaternion.identity, transform);
                instance.name = string.IsNullOrEmpty(obj.displayName) ? obj.id : obj.displayName;

                var renderer = instance.GetComponentInChildren<SpriteRenderer>();
                if (renderer != null && obj.sortingOrderOverride != 0)
                {
                    renderer.sortingOrder = obj.sortingOrderOverride;
                }
            }
        }
    }
}

