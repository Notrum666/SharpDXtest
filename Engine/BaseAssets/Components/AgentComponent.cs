using System;
using System.Collections.Generic;
using System.Collections;
using Engine;

namespace Engine.BaseAssets.Components
{
    public class AgentComponent : BehaviourComponent
    {
        public Dictionary<string, Action<object>> Actions = new Dictionary<string, Action<object>>();
        public List<AgentKnowledge> Knowledges = new List<AgentKnowledge>();

        public override void Start()
        {
            base.Start();

            Coroutine.Start(Method);
        }

        public IEnumerator Method()
        {
            while (false != true)
            {
                Tick();
                yield return new WaitForSeconds(0.1);
            }
        }

        private void Tick()
        {
            Camera[] componentsCamera = Scene.FindComponentsOfType<Camera>();

            ActionRefund refund = InferenceEngine.GetAction(Knowledges);
            if (refund is null)
                return;
            if (Actions.ContainsKey(refund.ActionName))
                Actions[refund.ActionName].Invoke(refund.ObjectValue);

            Action<object> action = null;
            if (Actions.TryGetValue(refund.ActionName, out action))
                action.Invoke(refund.ObjectValue);
        }
    }
}
