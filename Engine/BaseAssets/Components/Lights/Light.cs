using System;

using LinearAlgebra;

namespace Engine.BaseAssets.Components
{
    public abstract class Light : BehaviourComponent
    {
        [SerializedField]
        private float brightness = 1.0f;
        public Vector3f Color = new Vector3f(1f, 1f, 1f);

        public float Brightness
        {
            get => brightness;
            set
            {
                if (value < 0.0f)
                    throw new ArgumentOutOfRangeException("Brightness", "Brightness can't be negative");
                brightness = value;
            }
        }
    }
}