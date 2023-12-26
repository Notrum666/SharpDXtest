using System;
using Engine.Serialization;
using YamlDotNet.Serialization.Callbacks;

namespace Engine
{
    [YamlTagMapped]
    public class SerializedObject
    {
        [SerializedField]
        private readonly Guid instanceId;

        public Guid InstanceId => instanceId;

        private bool destroyed;

        protected SerializedObject()
        {
            instanceId = Guid.NewGuid();
        }

        protected static object Instantiate(Type type)
        {
            return Activator.CreateInstance(type);
        }

        protected static T Instantiate<T>()
        {
            return Activator.CreateInstance<T>();
        }

        internal void DestroyImmediate()
        {
            if (destroyed)
                return;
            destroyed = true;

            DestroyImmediateInternal();
            OnDestroyed?.Invoke(this);
        }

        private protected virtual void DestroyImmediateInternal() { }

        [OnDeserialized]
        protected void OnSelfDeserialized()
        {
            SerializedObjectPromise.RegisterLoadedObject(this);
        }

        internal virtual void OnDeserialized() { }

        #region Runtime

        public bool Initialized { get; private set; }
        public bool PendingDestroy { get; private set; }
        internal event Action<SerializedObject> OnDestroyed;

        public void Initialize()
        {
            if (Initialized)
                return;
            Initialized = true;

            InitializeInner();
        }

        private protected virtual void InitializeInner() { }

        /// <summary>
        /// Mark GameObject or Component pending for destruction
        /// </summary>
        public void Destroy()
        {
            if (PendingDestroy)
                return;
            PendingDestroy = true;
        }

        #endregion Runtime

        #region Comparison

        public override int GetHashCode()
        {
            return HashCode.Combine(InstanceId);
        }

        public override bool Equals(object other)
        {
            if (other is SerializedObject otherObj)
                return CompareObjects(this, otherObj);

            return false;
        }

        public static bool operator ==(SerializedObject obj1, SerializedObject obj2)
        {
            return CompareObjects(obj1, obj2);
        }

        public static bool operator !=(SerializedObject obj1, SerializedObject obj2)
        {
            return !CompareObjects(obj1, obj2);
        }

        private static bool CompareObjects(SerializedObject lhs, SerializedObject rhs)
        {
            bool lhsNull = (object)lhs == null;
            bool rhsNull = (object)rhs == null;

            if (rhsNull & lhsNull)
                return true;

            if (rhsNull)
                return lhs.destroyed;

            if (lhsNull)
                return rhs.destroyed;

            return lhs.InstanceId == rhs.InstanceId;
        }

        #endregion Comparison

    }
}