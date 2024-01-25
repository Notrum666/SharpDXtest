using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.IO;
using System.Numerics;
using System.Text;
using System.Windows.Documents;

namespace Engine
{
    public static class InferenceEngine
    {
        static InferenceEngine()
        {
        }

        public static ActionRefund GetAction(List<AgentKnowledge> knowledges)
        {
            return new ActionRefund()
            {
                ObjectType = "Agent",
                ActionName = "location",
                ObjectValue = Vector3.Zero
            };
        }
    }
    public class AgentKnowledge
    {
        public string ObjectType { get; set; }
        public string PropertyName { get; set; }
        public object ObjectValue { get; set; }
    }
    public class ActionRefund
    {
        public string ObjectType { get; set; }
        public string ActionName { get; set; }
        public object ObjectValue { get; set; }
    }
}