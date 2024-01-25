using Engine.Assets;
using Engine.BaseAssets.Components;
using Engine;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TestProject.Content.Scripts
{
    internal class ProjectileSpawner : BehaviourComponent
    {
        [SerializedField]
        private Prefab projectile;

        public override void Update()
        {
            if (Input.IsKeyDown(System.Windows.Input.Key.K))
            {
                projectile.Instantiate();
            }
        }
    }
}
