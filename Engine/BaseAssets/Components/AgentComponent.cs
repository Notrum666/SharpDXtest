using System;
using System.Collections.Generic;
using Engine;

namespace Engine.BaseAssets.Components
{
    public class AgentComponent : BehaviourComponent
    {
        public Dictionary<string, Action<object>> Actions = new Dictionary<string, Action<object>>();
        private List<AgentKnowledge> knowledges = new List<AgentKnowledge>();

        protected override void OnInitialized()
        {
            base.OnInitialized();

            /// TODO: fill knowledges
        }

        public override void Update()
        {
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
