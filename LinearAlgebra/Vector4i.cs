using System;

namespace LinearAlgebra
{
    /// <summary>
    /// 4-dimensional vector with single precision
    /// </summary>
    public struct Vector4i
    {
        /// <summary>
        /// Returns new zero vector
        /// </summary>
        public static readonly Vector4i Zero = new Vector4i();
        /// <summary>
        /// Returns new unit x vector (x = 1, y = 0, z = 0, w = 0)
        /// </summary>
        public static readonly Vector4i UnitX = new Vector4i(1, 0, 0, 0);
        /// <summary>
        /// Returns new unit y vector (x = 0, y = 1, z = 0, w = 0)
        /// </summary>
        public static readonly Vector4i UnitY = new Vector4i(0, 1, 0, 0);
        /// <summary>
        /// Returns new unit z vector (x = 0, y = 0, z = 1, w = 0)
        /// </summary>
        public static readonly Vector4i UnitZ = new Vector4i(0, 0, 1, 0);
        /// <summary>
        /// Returns new unit w vector (x = 0, y = 0, z = 0, w = 1)
        /// </summary>
        public static readonly Vector4i UnitW = new Vector4i(0, 0, 0, 1);

        public int x;
        public int y;
        public int z;
        public int w;
        public Vector3i xyz => new Vector3i(x, y, z);

        public Vector4i(int x = 0, int y = 0, int z = 0, int w = 0)
        {
            this.x = x;
            this.y = y;
            this.z = z;
            this.w = w;
        }

        public Vector4i(in Vector2i vec, int z = 0, int w = 0)
        {
            x = vec.x;
            y = vec.y;
            this.z = z;
            this.w = w;
        }

        public Vector4i(in Vector3i vec, int w = 0)
        {
            x = vec.x;
            y = vec.y;
            z = vec.z;
            this.w = w;
        }

        public Vector4i(params int[] values)
        {
            if (values.Length != 4)
                throw new Exception("Array length must be 4.");

            x = values[0];
            y = values[1];
            z = values[2];
            w = values[3];
        }

        public static explicit operator Vector4i(in Vector4 vec)
        {
            return new Vector4i((int)vec.x, (int)vec.y, (int)vec.z, (int)vec.w);
        }

        /// <summary>
        /// Checks if vector is zero
        /// </summary>
        public bool isZero()
        {
            return x == 0 && y == 0 && z == 0 && w == 0;
        }

        public static Vector4i operator+(in Vector4i v1, in Vector4i v2)
        {
            return new Vector4i(v1.x + v2.x, v1.y + v2.y, v1.z + v2.z, v1.w + v2.w);
        }

        public static Vector4i operator-(in Vector4i v1, in Vector4i v2)
        {
            return new Vector4i(v1.x - v2.x, v1.y - v2.y, v1.z - v2.z, v1.w - v2.w);
        }

        public static Vector4i operator-(in Vector4i vec)
        {
            return new Vector4i(-vec.x, -vec.y, -vec.z, -vec.w);
        }

        /// <summary>
        /// Checks if vectors are equal
        /// </summary>
        public bool equals(in Vector4i vec)
        {
            return (vec - this).isZero();
        }

        public override string ToString()
        {
            return "(" + x.ToString() + ", " + y.ToString() + ", " + z.ToString() + ", " + w.ToString() + ")";
        }

        public string ToString(string format)
        {
            return "(" + x.ToString(format) + ", " + y.ToString(format) + ", " + z.ToString(format) + ", " + w.ToString(format) + ")";
        }
    }
}