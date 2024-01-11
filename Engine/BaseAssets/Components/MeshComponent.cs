﻿using System;
using System.Linq;
using System.Reflection;

namespace Engine.BaseAssets.Components
{
    public class MeshComponent : BehaviourComponent
    {
        [SerializedField]
        protected Model model = null;
        [SerializedField]
        private Material[] materials = Array.Empty<Material>();

        public virtual Model Model
        {
            get => model;
            set
            {
                model = value;
                RefreshMaterialsSlots();
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
    }
}