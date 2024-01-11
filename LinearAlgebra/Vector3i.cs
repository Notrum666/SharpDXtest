using System;

namespace LinearAlgebra
{
    /// <summary>
    /// 3-dimensional integer vector
    /// </summary>
    public struct Vector3i
    {
        /// <summary>
        /// Returns new zero vector
        /// </summary>
        public static readonly Vector3i Zero = new Vector3i();

        /// <summary>
        /// Returns new unit x vector (x = 1, y = 0, z = 0).
        /// </summary>
        public static readonly Vector3i UnitX = new Vector3i(1, 0, 0);
        /// <summary>
        /// Returns new unit y vector (x = 0, y = 1, z = 0).
        /// </summary>
        public static readonly Vector3i UnitY = new Vector3i(0, 1, 0);
        /// <summary>
        /// Returns new unit z vector (x = 0, y = 0, z = 1).
        /// </summary>
        public static readonly Vector3i UnitZ = new Vector3i(0, 0, 1);

        public int x;
        public int y;
        public int z;
        public Vector2i xy => new Vector2i(x, y);

        public Vector3i(int x = 0, int y = 0, int z = 0)
        {
            this.x = x;
            this.y = y;
            this.z = z;
        }

        public Vector3i(in Vector2i vec, int z = 0)
        {
            x = vec.x;
            y = vec.y;
            this.z = z;
        }

        public Vector3i(params int[] values)
        {
            if (values.Length != 3)
                throw new Exception("Array length must be 3.");

            x = values[0];
            y = values[1];
            z = values[2];
        }

        public static explicit operator Vector3i(in Vector3 vec)
        {
            return new Vector3i((int)vec.x, (int)vec.y, (int)vec.z);
        }

        /// <summary>
        /// Checks if vector is zero
        /// </summary>
        public bool isZero()
        {
            return x == 0 && y == 0 && z == 0;
        }

        public static Vector3i operator+(in Vector3i v1, in Vector3i v2)
        {
            return new Vector3i(v1.x + v2.x, v1.y + v2.y, v1.z + v2.z);
        }

        public static Vector3i operator-(in Vector3i v1, in Vector3i v2)
        {
            return new Vector3i(v1.x - v2.x, v1.y - v2.y, v1.z - v2.z);
        }

        public static Vector3i operator-(in Vector3i vec)
        {
            return new Vector3i(-vec.x, -vec.y, -vec.z);
        }

        /// <summary>
        /// Checks if vectors are equal
        /// </summary>
        public bool equals(in Vector3i vec)
        {
            return (vec - this).isZero();
        }

        public override string ToString()
        {
            return "(" + x.ToString() + ", " + y.ToString() + ", " + z.ToString() + ")";
        }

        public string ToString(string format)
        {
            return "(" + x.ToString(format) + ", " + y.ToString(format) + ", " + z.ToString(format) + ")";
        }
    }
}