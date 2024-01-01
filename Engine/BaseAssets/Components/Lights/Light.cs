using System;

using Engine.AssetsData;

using LinearAlgebra;

namespace Engine.BaseAssets.Components
{
    public abstract class Light : BehaviourComponent
    {
        [SerializedField]
        private float brightness = 1.0f;
        public Vector3f Color = new Vector3f(1f, 1f, 1f);

        public Ranged<float> Brightness => new Ranged<float>(ref brightness, 0.0f);
    }
}