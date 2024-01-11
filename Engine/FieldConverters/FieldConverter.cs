using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Engine
{
    public abstract class FieldConverter
    {
        public abstract Type SourceType { get; }
        public abstract Type TargetType { get; }
        public abstract object Convert(object obj);
        public abstract object ConvertBack(object obj);
    }
}
