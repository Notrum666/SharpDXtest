using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using SharpDXtest.BaseAssets.Components;

namespace SharpDXtest.Assets.Components
{
    class CubeScript : Component
    {
        private bool first = true;
        public override void update()
        {
            if (first)
            {
                gameObject.getComponent<SoundSource>().play(AssetsManager.Sounds["sample"]);
                first = false;
            }
        }
    }
}
