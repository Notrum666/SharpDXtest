using System.Linq;

using Engine;
using Engine.BaseAssets.Components;

namespace TestProject
{
    public class TestProjectComponent : BehaviourComponent
    {
        public int[] data = new int[3];
        private double time = 0;

        public override void Update()
        {
            time += Time.DeltaTime;
            if (time > 1)
            {
                Logger.Log(LogType.Info, string.Join(", ", data.Select(v => v.ToString())));
                time = 0;
            }
        }
    }
}