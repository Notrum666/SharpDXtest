using System.Reflection;

namespace Engine
{
    public interface INotifyFieldChanged
    {
        public abstract void OnFieldChanged(FieldInfo fieldInfo);
    }
}