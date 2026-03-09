using System;

namespace DaySim.Needs
{
    /// <summary>
    /// Simple Sims-like needs model.
    /// Values are 0–100; 0 is terrible, 100 is perfect.
    /// </summary>
    [Serializable]
    public class NeedsState
    {
        public float Hunger = 70f;
        public float Energy = 70f;
        public float Hygiene = 70f;
        public float Fun = 70f;
        public float Social = 70f;

        public void ClampAll()
        {
            Hunger = Clamp(Hunger);
            Energy = Clamp(Energy);
            Hygiene = Clamp(Hygiene);
            Fun = Clamp(Fun);
            Social = Clamp(Social);
        }

        private float Clamp(float v) => Math.Max(0f, Math.Min(100f, v));
    }
}

