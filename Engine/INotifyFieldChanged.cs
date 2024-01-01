using System.Reflection;

namespace Engine
{
    public interface INotifyFieldChanged
    {
        public void OnInspectorFieldChanged(FieldInfo fieldInfo);
    }
}