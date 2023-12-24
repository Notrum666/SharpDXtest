using System;

namespace Engine
{
    [YamlTagMapped]
    public class SerializedObject
    {
        [SerializedField]
        private Guid instanceId;

        public Guid InstanceId => instanceId;

        protected SerializedObject()
        {
            instanceId = Guid.NewGuid();
        }

        public static T Instantiate<T>() where T : SerializedObject
        {
            SerializedObject newObject = Activator.CreateInstance<T>();
            return (T)newObject;
        }

        protected static SerializedObject Instantiate(Type type)
        {
            if (!type.IsSubclassOf(typeof(SerializedObject)))
                return null;

            SerializedObject newObject = (SerializedObject)Activator.CreateInstance(type);
            return newObject;
        }

        protected virtual void InitializeInternal() { }

        protected virtual void DestroyInternal() { }
        
        //TODO: Add OnDeserialized
    }
}