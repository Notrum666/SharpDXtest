using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Engine.Coroutines
{
    public abstract class YieldInstruction : IEnumerator
    {
        public object Current => null;

        public abstract bool MoveNext();

        public void Reset()
        {

        }
    }
}
