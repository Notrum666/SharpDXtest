using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Engine.Coroutines;

namespace Engine
{
    public class WaitForNextUpdate : YieldInstruction
    {
        public override bool MoveNext()
        {
            return true;
        }
    }
}
