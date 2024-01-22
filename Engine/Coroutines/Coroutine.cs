using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Engine
{
    public static class Coroutine
    {
        private static List<IEnumerator> coroutines = new List<IEnumerator>();
        static Coroutine()
        {
            SceneManager.OnSceneUnloading += OnSceneUnloading;
        }
        public static void Start(Func<IEnumerator> func)
        {
            IEnumerator enumerator = func();
            enumerator.MoveNext();
            coroutines.Add(enumerator);
        }
        internal static void Update()
        {
            foreach (IEnumerator coroutine in coroutines.ToImmutableList())
                if (((IEnumerator)coroutine.Current).MoveNext() && !coroutine.MoveNext())
                    coroutines.Remove(coroutine);
        }
        private static void OnSceneUnloading(string name)
        {
            coroutines.Clear();
        }
    }
}
