using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Engine.BaseAssets.Components.Postprocessing
{
    public abstract class PostProcessEffect
    {
        public abstract void Process(Texture texture);
    }
}