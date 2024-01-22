using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Engine.Coroutines;

namespace Engine
{
    public class WaitForSeconds : YieldInstruction
    {
        private double seconds;
        public WaitForSeconds(double seconds)
        {
            this.seconds = seconds;
        }
        public override bool MoveNext()
        {
            seconds -= Time.DeltaTime;
            return seconds <= 0;
        }
    }
}
