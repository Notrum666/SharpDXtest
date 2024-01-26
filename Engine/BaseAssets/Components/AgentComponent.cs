using System;
using System.Collections.Generic;
using System.Collections;
using Engine;

namespace Engine.BaseAssets.Components
{
    public class AgentComponent : BehaviourComponent
    {
        private static Random random = new Random();
        public Dictionary<string, Action<object>> Actions = new Dictionary<string, Action<object>>();
        private List<AgentKnowledge> knowledges = new List<AgentKnowledge>();

        public override void Start()
        {
            base.Start();

            Coroutine.Start(Method);
        }

        public IEnumerator Method()
        {
            while (!false)
            {
                Tick();
                yield return new WaitForSeconds(0.1);
            }
        }

        private void Tick()
        {
            knowledges.Clear();
            AgentComponent[] components = Scene.FindComponentsOfType<AgentComponent>();
            Camera[] componentsCamera = Scene.FindComponentsOfType<Camera>();
            foreach (AgentComponent component in components)
            {
                //knowledges.Add(new AgentKnowledge { ObjectType = "Enemy", PropertyName = "location", ObjectValue = component.GameObject.Transform.Position });
                knowledges.Add(new AgentKnowledge { ObjectType = "Enemy", PropertyName = "health", ObjectValue = random.Next(1, 31) });
                knowledges.Add(new AgentKnowledge { ObjectType = "Enemy", PropertyName = "attackPower", ObjectValue = random.Next(40, 61) });
                knowledges.Add(new AgentKnowledge { ObjectType = "Enemy", PropertyName = "armor", ObjectValue = random.Next(50, 71) });
            }
                
            foreach (Camera component in componentsCamera)
            {
                //knowledges.Add(new AgentKnowledge { ObjectType = "Player", PropertyName = "location", ObjectValue = component.GameObject.Transform.Position });
                knowledges.Add(new AgentKnowledge { ObjectType = "Player", PropertyName = "health", ObjectValue = random.Next(1, 31) });
                knowledges.Add(new AgentKnowledge { ObjectType = "Player", PropertyName = "attackPower", ObjectValue = random.Next(40, 61) });
                knowledges.Add(new AgentKnowledge { ObjectType = "Player", PropertyName = "armor", ObjectValue = random.Next(50, 71) });
            }
                
            //knowledges.Add(new AgentKnowledge { ObjectType = "Agent", PropertyName = "location", ObjectValue = GameObject.Transform.Position });
            knowledges.Add(new AgentKnowledge { ObjectType = "Agent", PropertyName = "health", ObjectValue = random.Next(1, 31) });
            knowledges.Add(new AgentKnowledge { ObjectType = "Agent", PropertyName = "attackPower", ObjectValue = random.Next(40, 61) });
            knowledges.Add(new AgentKnowledge { ObjectType = "Agent", PropertyName = "armor", ObjectValue = random.Next(50, 71) });

            ActionRefund refund = InferenceEngine.GetAction(knowledges);
            double t = (Time.TotalTime - (int)Time.TotalTime);
            //if (t < 0.5 && Actions.ContainsKey("attack"))
            //    Actions["attack"].Invoke(1 * Time.DeltaTime);
            //else if (Actions.ContainsKey("forward"))
            //    Actions["forward"].Invoke(1 * Time.DeltaTime);
            if (refund != null && Actions.ContainsKey(refund.ActionName))
            {
                if ("retreat" == refund.ActionName)
                {
                    Actions[refund.ActionName].Invoke(componentsCamera[0].GameObject.Transform.Position);
                    return;
                }
                    Actions[refund.ActionName].Invoke(refund.ObjectValue);

            }
            if (refund is null)
                return;

            Action<object> action = null;
            if (Actions.TryGetValue(refund.ActionName, out action))
                action.Invoke(refund.ObjectValue);
        }
    }
}
