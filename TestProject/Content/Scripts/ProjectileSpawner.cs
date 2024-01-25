using Engine.Assets;
using Engine.BaseAssets.Components;
using Engine;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections;

namespace TestProject.Content.Scripts
{
    internal class ProjectileSpawner : BehaviourComponent
    {
        [SerializedField]
        private Prefab projectile;

        [SerializedField]
        private Sound sound;

        public override void Start()
        {
            base.Start();
            Logger.Log(LogType.Info, "test");
            Logger.Log(LogType.Info, "test2");
            Coroutine.Start(MyCoroutine);
        }

        public override void Update()
        {
            if (Input.IsKeyPressed(System.Windows.Input.Key.K))
            {
                projectile.Instantiate();
                GameObject.GetComponent<SoundSource>()?.Play(sound);
            }
        }

        IEnumerator MyCoroutine()
        {
            while (true)
            {
                int goCount = Scene.CurrentScene.GameObjects.Count;
                Logger.Log(LogType.Warning, $"count = {goCount}");
                yield return new WaitForSeconds(1);
            }
        }
    }
}
