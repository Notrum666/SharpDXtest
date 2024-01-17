using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Editor
{
    public abstract class InspectorObjectViewModel : ViewModelBase
    {
        public abstract void Reload();
        public abstract void Update();
    }
}
