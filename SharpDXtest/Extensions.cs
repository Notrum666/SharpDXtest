using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Media;

namespace Editor
{
    public static class Extensions
    {
        public static bool IsStruct(this Type type)
        {
            return !(type.IsPrimitive || !type.IsValueType || type.IsEnum);
        }
        public static T FindParent<T>(this DependencyObject child) where T : DependencyObject
        {
            DependencyObject parentObject = VisualTreeHelper.GetParent(child);

            if (parentObject == null)
                return null;

            if (parentObject is T parent)
                return parent;

            return FindParent<T>(parentObject);
        }

        public static IList<DependencyObject> FindParentWithPath<T>(this DependencyObject child) where T : DependencyObject
        {
            DependencyObject parentObject = VisualTreeHelper.GetParent(child);

            if (parentObject == null)
                return null;

            if (parentObject is T parent)
                return new List<DependencyObject>() { parent };

            IList<DependencyObject> curPath = FindParentWithPath<T>(parentObject);
            if (curPath != null)
                curPath.Insert(0, parentObject);

            return curPath;
        }

        public static object FindChildByName(this DependencyObject parent, string name)
        {
            int childrenCount = VisualTreeHelper.GetChildrenCount(parent);
            for (int i = 0; i < childrenCount; i++)
            {
                DependencyObject child = VisualTreeHelper.GetChild(parent, i);
                if (child is FrameworkElement frameworkElement)
                {
                    if (frameworkElement.Name == name)
                        return child;
                }
                object subChild = child.FindChildByName(name);
                if (subChild != null)
                    return subChild;
            }
            return null;
        }

        /// <summary>
        /// builds full sequence of ancestor types (not including interfaces) for specified type, including the specified type
        /// </summary>
        public static IEnumerable<Type> GetInheritanceHierarchy(this Type type)
        {
            for (Type current = type; current != null; current = current.BaseType)
                yield return current;
        }

        /// <summary>
        /// Calculates distance in hierarchy from this type to specified type, for example: object->class A->class B, distance(B,A)=2, distance(B,object)=3.
        /// Returns -1 if specified ancestor not found in hierarchy
        /// </summary>
        public static int GetInheritanceDistance<TOther>(this Type type)
        {
            return GetInheritanceDistance(type, typeof(TOther));
        }

        /// <summary>
        /// Calculates distance in hierarchy from this type to specified type, for example: object->class A->class B, distance(B,A)=2, distance(B,object)=3.
        /// Returns -1 if specified ancestor not found in hierarchy
        /// </summary>
        public static int GetInheritanceDistance(this Type type, Type other)
        {
            List<Type> hierarchy = type.GetInheritanceHierarchy().TakeWhile(t => StripGeneric(t) != other).ToList();
            if (hierarchy.Count > 0 && hierarchy.Last().BaseType is null)
                return -1;
            return hierarchy.Count;
        }

        public static bool IsSameOrSubclassOf(this Type type, Type baseType)
        {
            return type.IsSubclassOf(baseType) || type == baseType;
        }

        private static Type StripGeneric(Type t)
        {
            return t.IsGenericType ? t.GetGenericTypeDefinition() : t;
        }
    }
}