using System;

namespace LinearAlgebra
{
    /// <summary>
    /// 2-dimensional integer vector
    /// </summary>
    public struct Vector2i
    {
        /// <summary>
        /// Returns new zero vector
        /// </summary>
        public static readonly Vector2i Zero = new Vector2i();
        /// <summary>
        /// Returns new unit x vector (x = 1, y = 0)
        /// </summary>
        public static readonly Vector2i UnitX = new Vector2i(1, 0);
        /// <summary>
        /// Returns new unit y vector (x = 0, y = 1)
        /// </summary>
        public static readonly Vector2i UnitY = new Vector2i(0, 1);

        public int x;
        public int y;

        public Vector2i(int x = 0, int y = 0)
        {
            this.x = x;
            this.y = y;
        }

        public Vector2i(int[] values)
        {
            if (values.Length != 2)
                throw new Exception("Array length must be 2.");

            x = values[0];
            y = values[1];
        }

        public static explicit operator Vector2i(in Vector2 vec)
        {
            return new Vector2i((int)vec.x, (int)vec.y);
        }

        /// <summary>
        /// Checks if vector small enough to be considered a zero vector
        /// </summary>
        public bool isZero()
        {
            return x == 0 && y == 0;
        }

        public static Vector2i operator+(in Vector2i v1, in Vector2i v2)
        {
            return new Vector2i(v1.x + v2.x, v1.y + v2.y);
        }

        public static Vector2i operator-(in Vector2i v1, in Vector2i v2)
        {
            return new Vector2i(v1.x - v2.x, v1.y - v2.y);
        }

        public static Vector2i operator-(in Vector2i vec)
        {
            return new Vector2i(-vec.x, -vec.y);
        }

        /// <summary>
        /// Checks if vectors are equal enough to be considered equal
        /// </summary>
        public bool equals(in Vector2i vec)
        {
            return (vec - this).isZero();
        }

        public override string ToString()
        {
            return "(" + x.ToString() + ", " + y.ToString() + ")";
        }

        public string ToString(string format)
        {
            return "(" + x.ToString(format) + ", " + y.ToString(format) + ")";
        }
    }
}