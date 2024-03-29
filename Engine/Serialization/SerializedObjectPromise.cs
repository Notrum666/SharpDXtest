using System;
using System.Collections.Generic;
using YamlDotNet.Serialization;

namespace Engine.Serialization
{
    public class SerializedObjectPromise : IValuePromise
    {
        public event Action<object> ValueAvailable;

        private readonly Guid instanceId;

        private static readonly Dictionary<Guid, SerializableObject> loadedObjects = new Dictionary<Guid, SerializableObject>();
        private static event Action InjectObjectsEvent;

        public SerializedObjectPromise(Guid instanceId)
        {
            this.instanceId = instanceId;
            InjectObjectsEvent += OnInjectObject;
        }

        public static void InjectSerializedObjects()
        {
            InjectObjectsEvent?.Invoke();

            InjectObjectsEvent = null;
            loadedObjects.Clear();
        }

        public static void RegisterLoadedObject(SerializableObject serializableObject)
        {
            loadedObjects[serializableObject.InstanceId] = serializableObject;
        }

        private void OnInjectObject()
        {
            SerializableObject obj = loadedObjects.GetValueOrDefault(instanceId, null);
            ValueAvailable?.Invoke(obj);
        }
    }
}