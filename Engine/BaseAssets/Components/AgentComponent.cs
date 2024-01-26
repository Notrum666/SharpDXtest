using System;
using System.Collections.Generic;
using System.Collections;
using Engine;

namespace Engine.BaseAssets.Components
{
    public class AgentComponent : BehaviourComponent
    {
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
                knowledges.Add(new AgentKnowledge { ObjectType = "Enemy", PropertyName = "location", ObjectValue = component.GameObject.Transform.Position});
            foreach (Camera component in componentsCamera)
                knowledges.Add(new AgentKnowledge { ObjectType = "Player", PropertyName = "location", ObjectValue = component.GameObject.Transform.Position });
            knowledges.Add(new AgentKnowledge { ObjectType = "Agent", PropertyName = "location", ObjectValue = GameObject.Transform.Position });

            ActionRefund refund = InferenceEngine.GetAction(knowledges);
            double t = (Time.TotalTime - (int)Time.TotalTime);
            if (t < 0.5 && Actions.ContainsKey("attack"))
                Actions["attack"].Invoke(1 * Time.DeltaTime);
            else if (Actions.ContainsKey("forward"))
                Actions["forward"].Invoke(1 * Time.DeltaTime);

            if (refund is null)
                return;

            Action<object> action = null;
            if (Actions.TryGetValue(refund.ActionName, out action))
                action.Invoke(refund.ObjectValue);
        }
    }
}
