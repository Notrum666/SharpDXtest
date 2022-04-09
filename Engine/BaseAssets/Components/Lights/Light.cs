using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using LinearAlgebra;

namespace Engine.BaseAssets.Components
{
    public abstract class Light : Component
    {
        public Vector3f color = new Vector3f(1f, 1f, 1f);
        protected float brightness = 1.0f;
        public float Brightness
        {
            get => brightness;
            set
            {
                if (value < 0.0f || value > 1.0f)
                    throw new ArgumentOutOfRangeException("Brightness", "Brightness can't be negative or more than 1");
                brightness = value;
            }
        }
    }
}
