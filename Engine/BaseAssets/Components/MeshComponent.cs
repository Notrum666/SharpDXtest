using LinearAlgebra;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Windows.Documents;
using System.Windows.Shapes;

namespace Engine.BaseAssets.Components
{
    public class MeshComponent : BehaviourComponent
    {
        [SerializedField]
        protected Model model = null;
        [SerializedField]
        private Material[] materials = Array.Empty<Material>();

        private double squaredBoundingSphereRadius;
        public double SquaredBoundingSphereRadius
        {
            get
            {
                if (boundingSphereRequiresRecalculation)
                    RecalculateBoundingSphereRadius();
                return squaredBoundingSphereRadius;
            }
        }
        private double boundingSphereRadius;
        public double BoundingSphereRadius
        {
            get
            {
                if (boundingSphereRequiresRecalculation)
                    RecalculateBoundingSphereRadius();
                return boundingSphereRadius;
            }
        }
        private bool boundingSphereRequiresRecalculation = true;

        public virtual Model Model
        {
            get => model;
            set
            {
                if (model == value)
                    return;

                model = value;
                RefreshMaterialsSlots();
                InvalidateBoundingSphereRadius();
            }
        }

        public Material[] Materials => materials;

        public override void OnFieldChanged(FieldInfo fieldInfo)
        {
            if (fieldInfo.Name == nameof(model))
            {
                RefreshMaterialsSlots();
                InvalidateBoundingSphereRadius();
            }
        }
        
        private void InvalidateBoundingSphereRadius()
        {
            boundingSphereRequiresRecalculation = true;
        }

        protected void RefreshMaterialsSlots()
        {
            if (model is null)
                materials = Array.Empty<Material>();
            else
                materials = model.Meshes.Select(p => p.DefaultMaterial).ToArray();
        }

        protected override void OnInitialized()
        {
            base.OnInitialized();
            GameObject.Transform.Invalidated += InvalidateBoundingSphereRadius;
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();
            GameObject.Transform.Invalidated -= InvalidateBoundingSphereRadius;
        }

        public virtual void Render()
        {
            if (model is null)
            {
                Logger.Log(LogType.Warning, GameObject.ToString() + ": trying to render MeshComponent with no mesh set");
                return;
            }

            for (int i = 0; i < model.Meshes.Count; ++i)
            {
                Material curMaterial = Materials[i];
                if (curMaterial is null)
                    curMaterial = model.Meshes[i].DefaultMaterial;
                if (curMaterial is null)
                    curMaterial = Material.Default;
                curMaterial.Use();

                model.Meshes[i].Render();
            }
        }

        private void RecalculateBoundingSphereRadius()
        {
            boundingSphereRequiresRecalculation = false;
            if (model == null)
            {
                boundingSphereRadius = 0;
                squaredBoundingSphereRadius = 0;
                return;
            }

            Matrix4x4 m = GameObject.Transform.Model;
            Vector3 pos = GameObject.Transform.Position;
            double maxSqrLength = 0;
            foreach (Mesh mesh in model.Meshes)
                foreach (Mesh.PrimitiveVertex vertex in mesh.Vertices)
                {
                    double curSqrLength = (m.TransformPoint(vertex.v) - pos).squaredLength();
                    if (curSqrLength > maxSqrLength)
                        maxSqrLength = curSqrLength;
                }

            squaredBoundingSphereRadius = maxSqrLength;
            boundingSphereRadius = Math.Sqrt(squaredBoundingSphereRadius);
        }
    }
}