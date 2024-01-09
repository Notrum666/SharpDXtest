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

        public virtual void DoShadowPass() { }
        public virtual void DoLightPass() { }
        
        protected void RenderObjects(ShaderPipeline pipeline)
        {
            foreach (MeshComponent meshComponent in Scene.FindComponentsOfType<MeshComponent>())
            {
                if (!meshComponent.LocalEnabled)
                    continue;
                pipeline.UpdateUniform("model", (Matrix4x4f)meshComponent.GameObject.Transform.Model);

                pipeline.UploadUpdatedUniforms();

                meshComponent.Render();
            }
        }
    }
}