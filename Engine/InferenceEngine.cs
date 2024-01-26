using System;
using System.Collections.Generic;
using System.Data;
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
        private static List<(List<(Func<string, int, bool>, string)>, string)> rules;
        private static Random random = new Random();
        static InferenceEngine()
        {
            rules = new List<(List<(Func<string, int, bool>, string)>, string)>();
            LoadConditionsFromFile("conditions.txt");
        }

        public static void LoadConditionsFromFile(string filePath)
        {
            try
            {
                string[] lines = File.ReadAllLines(filePath);

                List<(Func<string, int, bool>, string)> currentRuleConditions = null;
                string currentAction = null;

                foreach (var line in lines)
                {
                    if (line.StartsWith("| Fact"))
                    {
                        currentRuleConditions = new List<(Func<string, int, bool>, string)>();
                        continue;
                    }
                    else if (line.StartsWith("| Action"))
                    {
                        rules.Add((currentRuleConditions, line.Replace("| Action, ", "").Trim()));
                        continue;
                    }

                    string[] parts = line.Split(',');
                    if (parts.Length >= 5 && currentRuleConditions != null)
                    {
                        Func<string, int, bool> condition = GetCondition(parts[3].Trim(), int.Parse(parts[4].Trim()));
                        string propertyType = parts[2].Trim();
                        currentRuleConditions.Add((condition, propertyType));
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error loading conditions from file: {ex.Message}");
            }
        }

        public static ActionRefund GetAction(List<AgentKnowledge> agentMemory)
        {
            List<string> possibleActions = new List<string>();

            foreach (var rule in rules)
            {
                bool allConditionsMet = true;

                foreach (var condition in rule.Item1)
                {
                    string propertyType = condition.Item2;
                    var agentMemoryEntry = agentMemory.Find(entry => entry.ObjectType == "Agent" && entry.PropertyName == propertyType);
                    if (agentMemoryEntry.ObjectValue.GetType() == typeof(int))
                        if (agentMemoryEntry == null || !condition.Item1(agentMemoryEntry.ObjectValue.ToString(), (int)agentMemoryEntry.ObjectValue))
                        {
                            allConditionsMet = false;
                            break;
                        }
                }

                if (allConditionsMet)
                    possibleActions.Add(rule.Item2);
            }

            if (possibleActions.Count > 0)
            {
                int randomIndex = random.Next(0, possibleActions.Count);
                return new ActionRefund()
                {
                    ObjectType = "Agent",
                    ActionName = possibleActions[randomIndex],
                    ObjectValue = Vector3.Zero
                };
            }
            return new ActionRefund()
            {
                ObjectType = "Agent",
                ActionName = "No matching action",
                ObjectValue = Vector3.Zero
            };
        }

        private static Func<string, int, bool> GetCondition(string conditionType, int threshold)
        {
            switch (conditionType.ToLower())
            {
                case "lessthan":
                    return (value, target) => int.Parse(value) < threshold;
                case "greaterthan":
                    return (value, target) => int.Parse(value) > threshold;
                default:
                    throw new ArgumentException($"Unsupported condition type: {conditionType}");
            }
        }

        //public static ActionRefund GetAction(List<AgentKnowledge> knowledges)
        //{
        //    return new ActionRefund()
        //    {
        //        ObjectType = "Agent",
        //        ActionName = "location",
        //        ObjectValue = Vector3.Zero
        //    };
        //}
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