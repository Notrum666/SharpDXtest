using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Engine.Coroutines;

namespace Engine
{
    public class WaitWhile : YieldInstruction
    {
        private Func<bool> predicate;
        public WaitWhile(Func<bool> predicate) 
        {
            this.predicate = predicate;
        }
        public override bool MoveNext()
        {
            return !predicate();
        }
    }
}
