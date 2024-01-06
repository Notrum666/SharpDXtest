using Engine.BaseAssets.Components;
using LinearAlgebra;
using SharpDX.Direct3D;
using SharpDX.Direct3D11;
using SharpDX.Mathematics.Interop;
using SharpDX;
using System.Collections.Generic;
using System;
using Engine.BaseAssets.Components.Postprocessing;

namespace Engine.Layers
{
    internal class RenderLayer : Layer
    {
        public override float UpdateOrder => 3;
        public override float InitOrder => 3;

        public override void Update()
        {
            if (Camera.Current != null)
                //GraphicsCore.RenderShadows();
                GraphicsCore.RenderScene(Camera.Current);
        }
    }
}
