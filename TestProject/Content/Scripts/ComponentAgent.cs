using System;
using System.Collections.Generic;
using System.Windows.Documents;
using Engine;
using Engine.BaseAssets.Components;
using LinearAlgebra;

namespace TestProject.Content.Scripts
{
    public class ComponentAgent : BehaviourComponent
    {
        private static Random random = new Random();
        private int hp = 100;
        private float movementSpeed = 10.0f;

        public override void Start()
        {
            base.Start();

            var ac = GameObject.GetComponent<AgentComponent>();
            ac.Actions.Add("attack", Attack);
            ac.Actions.Add("retreat", Retreat);
            ac.Actions.Add("CollectGold", Gold);
        }

        public override void Update()
        {
            base.Update();

            AgentComponent[] components = Scene.FindComponentsOfType<AgentComponent>();
            Camera[] componentsCamera = Scene.FindComponentsOfType<Camera>();

            AgentComponent ac = GameObject.GetComponent<AgentComponent>();
            ac.Knowledges.Clear();
            foreach (AgentComponent component in components)
            {
                //ac.Knowledges.Add(new AgentKnowledge { ObjectType = "Enemy", PropertyName = "location", ObjectValue = component.GameObject.Transform.Position });
                ac.Knowledges.Add(new AgentKnowledge { ObjectType = "Enemy", PropertyName = "health", ObjectValue = random.Next(1, 31) });
                ac.Knowledges.Add(new AgentKnowledge { ObjectType = "Enemy", PropertyName = "attackPower", ObjectValue = random.Next(40, 61) });
                ac.Knowledges.Add(new AgentKnowledge { ObjectType = "Enemy", PropertyName = "armor", ObjectValue = random.Next(50, 71) });
            }

            foreach (Camera component in componentsCamera)
            {
                //ac.Knowledges.Add(new AgentKnowledge { ObjectType = "Player", PropertyName = "location", ObjectValue = component.GameObject.Transform.Position });
                ac.Knowledges.Add(new AgentKnowledge { ObjectType = "Player", PropertyName = "health", ObjectValue = random.Next(1, 31) });
                ac.Knowledges.Add(new AgentKnowledge { ObjectType = "Player", PropertyName = "attackPower", ObjectValue = random.Next(40, 61) });
                ac.Knowledges.Add(new AgentKnowledge { ObjectType = "Player", PropertyName = "armor", ObjectValue = random.Next(50, 71) });
            }

            //ac.Knowledges.Add(new AgentKnowledge { ObjectType = "Agent", PropertyName = "location", ObjectValue = GameObject.Transform.Position });
            ac.Knowledges.Add(new AgentKnowledge { ObjectType = "Agent", PropertyName = "health", ObjectValue = random.Next(1, 31) });
            ac.Knowledges.Add(new AgentKnowledge { ObjectType = "Agent", PropertyName = "attackPower", ObjectValue = random.Next(40, 61) });
            ac.Knowledges.Add(new AgentKnowledge { ObjectType = "Agent", PropertyName = "armor", ObjectValue = random.Next(50, 71) });
        }

        private void Attack(object Data)
        {
            Logger.Log(LogType.Info, "Attack");

            //GameObject.Transform.Position += new Vector3(0, 0, (double)Data);
            Camera[] componentsCamera = Scene.FindComponentsOfType<Camera>();

            Vector3 delta = (componentsCamera[0].GameObject.Transform.Position - GameObject.Transform.Position);
            delta.z = 0;
            delta.normalize();
            { 
                double angle = 0.0f;
                angle = -Math.Acos(delta.x);
                if (delta.y < 0)
                    angle = (Math.PI - angle) * 2 + angle;
                GameObject.Transform.LocalRotation = Quaternion.FromAxisAngle(Vector3.Right, -Math.PI / 2) * Quaternion.FromAxisAngle(Vector3.UnitY, angle);
            }
            delta *= movementSpeed * Time.DeltaTime;
            GameObject.Transform.Position += new Vector3(delta.x, delta.y, 0);

            SkeletalAnimation run = AssetsManager.LoadAssetAtPath<SkeletalAnimation>("Models\\cesium_man_Animation\\Armature_001_Anim_8_Armature_001.anim");
            if (run is not null)
                GameObject.GetComponent<SkeletalMeshComponent>().Animation = run;

        }

        private void Gold(object Data)
        {
            Logger.Log(LogType.Info, "Gold");

            //GameObject.Transform.Position += new Vector3(0, 0, (double)Data);
        }

        private void Retreat(object Data)
        {
            Logger.Log(LogType.Info, "Retreat");

            Camera[] componentsCamera = Scene.FindComponentsOfType<Camera>();

            Vector3 delta = (componentsCamera[0].GameObject.Transform.Position - GameObject.Transform.Position);
            delta.z = 0;
            delta.normalize();
            { 
                double angle = 0.0f;
                angle = -Math.Acos(delta.x);
                if (delta.y < 0)
                    angle = (Math.PI - angle) * 2 + angle;
                angle += Math.PI;
                GameObject.Transform.LocalRotation = Quaternion.FromAxisAngle(Vector3.Right, -Math.PI / 2) * Quaternion.FromAxisAngle(Vector3.UnitY, angle);
            }
            delta *= movementSpeed * Time.DeltaTime;
            GameObject.Transform.Position -= new Vector3(delta.x, delta.y, 0);

            SkeletalAnimation run = AssetsManager.LoadAssetAtPath<SkeletalAnimation>("Models\\cesium_man_Animation\\Armature_001_Anim_15_Armature_001.anim");
            if (run is not null)
                GameObject.GetComponent<SkeletalMeshComponent>().Animation = run;
        }
    }
}
