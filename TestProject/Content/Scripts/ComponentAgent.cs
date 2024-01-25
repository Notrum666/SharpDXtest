using System;
using System.Collections.Generic;
using Engine;
using Engine.BaseAssets.Components;
using LinearAlgebra;

namespace TestProject.Content.Scripts
{
    public class ComponentAgent : BehaviourComponent
    {
        public override void Start()
        {
            base.Start();

            var ac = GameObject.GetComponent<AgentComponent>();
            ac.Actions.Add("attack", Attack);
            ac.Actions.Add("forward", Forward);
        }

        private void Attack(object Data)
        {
            Logger.Log(LogType.Info, "Attack");

            GameObject.Transform.Position += new Vector3(0, 0, (double)Data);
        }

        private void Forward(object Data)
        {
            Logger.Log(LogType.Info, "Forward");

            GameObject.Transform.Position -= new Vector3(0, 0, (double)Data);
        }
    }
}
