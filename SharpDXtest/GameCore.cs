using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using SharpDXtest.BaseAssets.Components;

namespace SharpDXtest
{
    public static class GameCore
    {
        public static Scene CurrentScene { get; private set; }
        private static double accumulator = 0.0;
        public static void Init()
        {
            CurrentScene = AssetsManager.LoadScene("Assets\\Scenes\\Level1.xml");
            CurrentScene.mainCamera.MakeCurrent();
        }
        public static void Update()
        {
            update();

            accumulator += Time.DeltaTime;

            Time.SwitchToFixed();
            while (accumulator >= Time.FixedDeltaTime)
            {
                fixedUpdate();
                accumulator -= Time.FixedDeltaTime;
            }
            Time.SwitchToVariating();
        }
        private static void update()
        {
            foreach (GameObject obj in CurrentScene.objects)
                if (obj.Enabled)
                    obj.update();
        }
        private static void fixedUpdate()
        {
            foreach (GameObject obj in CurrentScene.objects)
                if (obj.Enabled)
                    obj.fixedUpdate();

            List<Rigidbody> rigidbodies = new List<Rigidbody>();
            for (int i = 0; i < CurrentScene.objects.Count; i++)
            {
                Rigidbody rigidbody = CurrentScene.objects[i].getComponent<Rigidbody>();
                if (rigidbody != null)
                {
                    foreach (Rigidbody otherRigidbody in rigidbodies)
                        rigidbody.solveCollisionWith(otherRigidbody);
                    rigidbodies.Add(rigidbody);
                }
            }

            foreach (Rigidbody rigidbody in rigidbodies)
                rigidbody.applyCollisionExitVectors();
        }
    }
}
