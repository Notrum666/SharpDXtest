using System;

namespace Engine
{
    public class ScriptAsset : BaseAsset
    {
        public readonly Type ComponentType;

        public ScriptAsset(Type componentType)
        {
            ComponentType = componentType;
        }
    }
}