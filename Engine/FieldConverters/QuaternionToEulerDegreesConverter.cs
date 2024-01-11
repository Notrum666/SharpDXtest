using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using LinearAlgebra;

namespace Engine
{
    public class QuaternionToEulerDegreesConverter : FieldConverter
    {
        public override Type SourceType => typeof(Quaternion);
        public override Type TargetType => typeof(Vector3);

        public override object Convert(object obj)
        {
            return ((Quaternion)obj).ToEuler() / Math.PI * 180.0;
        }

        public override object ConvertBack(object obj)
        {
            return Quaternion.FromEuler((Vector3)obj / 180.0 * Math.PI);
        }
    }
}
