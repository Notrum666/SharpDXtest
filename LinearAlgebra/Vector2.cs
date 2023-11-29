using System;

namespace LinearAlgebra
{
    /// <summary>
    /// 2-dimensional vector with double precision
    /// </summary>
    public struct Vector2
    {
        /// <summary>
        /// Returns new zero vector
        /// </summary>
        public static readonly Vector2 Zero = new Vector2();
        /// <summary>
        /// Returns new unit x vector (x = 1, y = 0)
        /// </summary>
        public static readonly Vector2 UnitX = new Vector2(1.0, 0.0);
        /// <summary>
        /// Returns new unit y vector (x = 0, y = 1)
        /// </summary>
        public static readonly Vector2 UnitY = new Vector2(0.0, 1.0);

        public double x;
        public double y;

        public Vector2(double x = 0.0, double y = 0.0)
        {
            this.x = x;
            this.y = y;
        }

        public Vector2(double[] values)
        {
            if (values.Length != 2)
                throw new Exception("Array length must be 2.");

            x = values[0];
            y = values[1];
        }

        public static implicit operator Vector2(in Vector2f vec)
        {
            return new Vector2(vec.x, vec.y);
        }

        /// <summary>
        /// Magnitude of vector. Same as length
        /// </summary>
        public double magnitude()
        {
            return Math.Sqrt(squaredMagnitude());
        }

        /// <summary>
        /// Magnitude of vector without root. Same as squaredLength
        /// </summary>
        public double squaredMagnitude()
        {
            return dot(this);
        }

        /// <summary>
        /// Length of vector. Same as length
        /// </summary>
        public double length()
        {
            return magnitude();
        }

        /// <summary>
        /// Length of vector without root. Same as squaredMagnitude
        /// </summary>
        public double squaredLength()
        {
            return squaredMagnitude();
        }

        /// <summary>
        /// Checks if vector small enough to be considered a zero vector
        /// </summary>
        public bool isZero()
        {
            return squaredMagnitude() < Constants.SqrEpsilon;
        }

        public static Vector2 operator +(in Vector2 v1, in Vector2 v2)
        {
            return new Vector2(v1.x + v2.x, v1.y + v2.y);
        }

        public static Vector2 operator -(in Vector2 v1, in Vector2 v2)
        {
            return new Vector2(v1.x - v2.x, v1.y - v2.y);
        }

        public static Vector2 operator *(in Vector2 vec, double value)
        {
            return new Vector2(vec.x * value, vec.y * value);
        }

        public static Vector2 operator *(double value, in Vector2 vec)
        {
            return new Vector2(vec.x * value, vec.y * value);
        }

        public static Vector2 operator /(in Vector2 vec, double value)
        {
            return new Vector2(vec.x / value, vec.y / value);
        }

        public static Vector2 operator /(float value, in Vector2 vec)
        {
            return new Vector2(value / vec.x, value / vec.y);
        }

        public static Vector2 operator -(in Vector2 vec)
        {
            return new Vector2(-vec.x, -vec.y);
        }

        /// <summary>
        /// Cross product
        /// </summary>
        public static double operator %(in Vector2 v1, in Vector2 v2)
        {
            return v1.vecMul(v2);
        }

        /// <summary>
        /// Dot product
        /// </summary>
        public static double operator *(in Vector2 v1, in Vector2 v2)
        {
            return v1.dot(v2);
        }

        /// <summary>
        /// Dot product
        /// </summary>
        public double dot(in Vector2 vec)
        {
            return x * vec.x + y * vec.y;
        }

        /// <summary>
        /// Component multiplication
        /// </summary>
        /// <returns>New vector: (x1*x2, y1*y2)</returns>
        public Vector2 compMul(in Vector2 vec)
        {
            return new Vector2(x * vec.x, y * vec.y);
        }

        /// <summary>
        /// Component division
        /// </summary>
        /// <returns>New vector: (x1/x2, y1/y2)</returns>
        public Vector2 compDiv(in Vector2 vec)
        {
            return new Vector2(x / vec.x, y / vec.y);
        }

        /// <summary>
        /// Returns normalized copy of this vector
        /// </summary>
        public Vector2 normalized()
        {
            return this / magnitude();
        }

        /// <summary>
        /// Normalizes this vector
        /// </summary>
        public void normalize()
        {
            double magn = magnitude();
            x /= magn;
            y /= magn;
        }

        /// <summary>
        /// Checks if vectors are equal enough to be considered equal
        /// </summary>
        public bool equals(in Vector2 vec)
        {
            return (vec - this).isZero();
        }

        /// <summary>
        /// Projects vector on another vector
        /// </summary>
        public Vector2 projectOnVector(in Vector2 vec)
        {
            if (vec.isZero())
                return Zero;
            return vec * (this * vec / vec.squaredMagnitude());
        }

        /// <summary>
        /// Cross product. Same as cross
        /// </summary>
        public double vecMul(in Vector2 vec)
        {
            return x * vec.y - y * vec.x;
        }

        /// <summary>
        /// Cross product. Same as vecMul
        /// </summary>
        public double cross(in Vector2 vec)
        {
            return vecMul(vec);
        }

        /// <summary>
        /// Checks if vectors are parallel enough to be considered collinear
        /// </summary>
        /// <returns>True if vectors are collinear, false otherwise</returns>
        public bool isCollinearTo(in Vector2 vec)
        {
            return Math.Abs(this % vec) < Constants.Epsilon;
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