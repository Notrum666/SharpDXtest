using System;

using Engine.AssetsData;

using LinearAlgebra;

namespace Engine.BaseAssets.Components
{
    public abstract class Light : BehaviourComponent
    {
        protected override Type CacheType => typeof(Light);

        [SerializedField]
        protected float brightness = 1.0f;
        public Vector3f Color = new Vector3f(1f, 1f, 1f);

        public Ranged<float> Brightness => new Ranged<float>(ref brightness, 0.0f);

        public virtual void RenderShadows(Camera camera) { }

        public virtual bool PrepareLightPass(Camera camera)
        {
            return false;
        }

        protected void RenderObjects(ShaderPipeline pipeline, bool withMaterial = true)
        {
            foreach (MeshComponent meshComponent in Component.GetCached<MeshComponent>())
            {
                if (!meshComponent.LocalEnabled)
                    continue;
                pipeline.UpdateUniform("model", (Matrix4x4f)meshComponent.GameObject.Transform.Model);

                pipeline.UploadUpdatedUniforms();

                meshComponent.Render(withMaterial);
            }
        }
    }
}