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

        internal double SquaredSphereRadius;

        public virtual Model Model
        {
            get => model;
            set
            {
                if (model == value)
                    return;

                model = value;
                RefreshMaterialsSlots();
                CalculateSphereRadius();
            }
        }

        public Material[] Materials => materials;

        public override void OnFieldChanged(FieldInfo fieldInfo)
        {
            if (fieldInfo.Name == nameof(model))
                RefreshMaterialsSlots();
        }

        protected void RefreshMaterialsSlots()
        {
            if (model is null)
                materials = Array.Empty<Material>();
            else
                materials = model.Meshes.Select(p => p.DefaultMaterial).ToArray();
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

        private void CalculateSphereRadius()
        {
            if (model == null)
            {
                SquaredSphereRadius = 0;
                return;
            }

            var localVertices = new List<Vector3>();
            Vector3 center = Vector3.Zero;
            Vector3 offset = Vector3.Zero;

            foreach (Mesh mesh in model.Meshes)
            {
                foreach (Mesh.PrimitiveVertex vertex in mesh.Vertices)
                {
                    localVertices.Add(vertex.v);
                    //center += vertex.v;
                }
            }

            //center /= localVertices.Count;
            center = GameObject.Transform.Model.TransformPoint(center + offset);

            List<Vector3> worldVertices = new List<Vector3>(localVertices.Count);

            Matrix4x4 modelMatrix = GameObject.Transform.Model;
            foreach (Vector3 vertex in localVertices)
                worldVertices.Add(modelMatrix.TransformPoint(vertex + offset));

            SquaredSphereRadius = 0;

            foreach (Vector3 vertex in worldVertices)
            {
                double squaredDistanceToCenter = (vertex - center).squaredLength();
                if (squaredDistanceToCenter > SquaredSphereRadius)
                    SquaredSphereRadius = squaredDistanceToCenter;
            }
        }
    }
}