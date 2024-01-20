using System;

namespace Engine
{
    public class ScriptAsset : BaseAsset
    {
        public Type ComponentType { get; private set; }

        internal ScriptAsset UpdateType(Type componentType)
        {
            ComponentType = componentType;
            return this;
        }
    }
}