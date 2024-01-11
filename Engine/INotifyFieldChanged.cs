using System.Reflection;

namespace Engine
{
    public interface INotifyFieldChanged
    {
        public void OnFieldChanged(FieldInfo fieldInfo);
    }
}