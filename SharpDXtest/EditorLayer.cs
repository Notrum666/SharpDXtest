using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Engine.Layers;

namespace Editor
{
    internal class EditorLayer : Layer
    {
        private static EditorLayer current = null;
        public static EditorLayer Current => current ?? (current = new EditorLayer());
        public override float InitOrder => 1;
        public override float UpdateOrder => 1;

        public override void Init()
        {

        }
        public override void Update()
        {

        }
    }
}
