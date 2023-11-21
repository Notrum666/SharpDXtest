using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;
using System.Windows;

namespace Editor
{
    public static class Extensions
    {
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
    }
}
