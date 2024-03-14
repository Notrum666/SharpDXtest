using System.Linq;
using System.Reflection;

using LinearAlgebra;

using SharpDX;
using SharpDX.Direct3D11;
using SharpDX.DXGI;

namespace Engine.BaseAssets.Components
{
    public abstract class GasVolume : Volume
    {
        [SerializedField]
        protected Vector3f lightDirection = -Vector3f.Up;
        public Vector3f LightDirection
        {
            get => lightDirection;
            set
            {
                if (value.isZero())
                {
                    Logger.Log(LogType.Warning, "LightDirection must be non-zero");
                    return;
                }
                lightDirection = value;
            }
        }
        [SerializedField]
        protected double absorptionCoef = 0.0;
        public double AbsorptionCoef
        {
            get => absorptionCoef;
            set
            {
                if (value < 0.0 || value > 1.0)
                {
                    Logger.Log(LogType.Warning, "AbsorptionCoef must be in range of 0 to 1");
                    return;
                }
                absorptionCoef = value;
            }
        }
        [SerializedField]
        protected double scatteringCoef = 0.03;
        public double ScatteringCoef
        {
            get => scatteringCoef;
            set
            {
                if (value < 0.0 || value > 1.0)
                {
                    Logger.Log(LogType.Warning, "ScatteringCoef must be in range of 0 to 1");
                    return;
                }
                scatteringCoef = value;
            }
        }

        protected override void OnInitialized()
        {
            base.OnInitialized();
        }
        
        public override void OnFieldChanged(FieldInfo fieldInfo)
        {
            base.OnFieldChanged(fieldInfo);

            if (fieldInfo.Name == nameof(scatteringCoef))
                ScatteringCoef = scatteringCoef;
            if (fieldInfo.Name == nameof(absorptionCoef))
                AbsorptionCoef = absorptionCoef;
            if (fieldInfo.Name == nameof(lightDirection))
                LightDirection = lightDirection;
        }

        //public void Render()
        //{
        //    GraphicsCore.CurrentDevice.ImmediateContext.InputAssembler.SetVertexBuffers(0, vertexBufferBinding);
        //    GraphicsCore.CurrentDevice.ImmediateContext.InputAssembler.SetIndexBuffer(indexBuffer, Format.R32_UInt, 0);
        //    GraphicsCore.CurrentDevice.ImmediateContext.DrawIndexed(36, 0, 0);
        //}
    }
}