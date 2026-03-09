using UnityEngine;

namespace DaySim.Graphics
{
    /// <summary>
    /// Defines how an avatar looks and animates.
    /// You can create multiple assets for different characters/skins.
    /// </summary>
    [CreateAssetMenu(menuName = "DaySim/Avatar Visual", fileName = "AvatarVisualDefinition")]
    public class AvatarVisualDefinition : ScriptableObject
    {
        [Header("Identity")]
        public string id;
        public string displayName;

        [Header("Preview")]
        public Sprite previewSprite;

        [Header("Animation")]
        public RuntimeAnimatorController animatorController;
    }
}

