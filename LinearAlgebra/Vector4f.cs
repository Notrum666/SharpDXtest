﻿using System;

namespace LinearAlgebra
{
    /// <summary>
    /// 4-dimensional vector with single precision
    /// </summary>
    public struct Vector4f : IEquatable<Vector4f>
    {
        /// <summary>
        /// Returns new zero vector
        /// </summary>
        public static readonly Vector4f Zero = new Vector4f();
        /// <summary>
        /// Returns new unit x vector (x = 1, y = 0, z = 0, w = 0)
        /// </summary>
        public static readonly Vector4f UnitX = new Vector4f(1f, 0f, 0f, 0f);
        /// <summary>
        /// Returns new unit y vector (x = 0, y = 1, z = 0, w = 0)
        /// </summary>
        public static readonly Vector4f UnitY = new Vector4f(0f, 1f, 0f, 0f);
        /// <summary>
        /// Returns new unit z vector (x = 0, y = 0, z = 1, w = 0)
        /// </summary>
        public static readonly Vector4f UnitZ = new Vector4f(0f, 0f, 1f, 0f);
        /// <summary>
        /// Returns new unit w vector (x = 0, y = 0, z = 0, w = 1)
        /// </summary>
        public static readonly Vector4f UnitW = new Vector4f(0f, 0f, 0f, 1f);

        public float x;
        public float y;
        public float z;
        public float w;
        public Vector3f xyz => new Vector3f(x, y, z);

        public Vector4f(float x = 0f, float y = 0f, float z = 0f, float w = 0f)
        {
            this.x = x;
            this.y = y;
            this.z = z;
            this.w = w;
        }

        public Vector4f(in Vector2f vec, float z = 0f, float w = 0f)
        {
            x = vec.x;
            y = vec.y;
            this.z = z;
            this.w = w;
        }

        public Vector4f(in Vector3f vec, float w = 0f)
        {
            x = vec.x;
            y = vec.y;
            z = vec.z;
            this.w = w;
        }

        public Vector4f(params float[] values)
        {
            if (values.Length != 4)
                throw new Exception("Array length must be 4.");

            x = values[0];
            y = values[1];
            z = values[2];
            w = values[3];
        }

        public static explicit operator Vector4f(in Vector4 vec)
        {
            return new Vector4f((float)vec.x, (float)vec.y, (float)vec.z, (float)vec.w);
        }

        /// <summary>
        /// Magnitude of vector. Same as length
        /// </summary>
        public float magnitude()
        {
            return (float)Math.Sqrt(squaredMagnitude());
        }

        /// <summary>
        /// Magnitude of vector without root. Same as squaredLength
        /// </summary>
        public float squaredMagnitude()
        {
            return dotMul(this);
        }

        /// <summary>
        /// Length of vector. Same as length
        /// </summary>
        public float length()
        {
            return magnitude();
        }

        /// <summary>
        /// Length of vector without root. Same as squaredMagnitude
        /// </summary>
        public float squaredLength()
        {
            return squaredMagnitude();
        }

        /// <summary>
        /// Checks if vector small enough to be considered a zero vector
        /// </summary>
        public bool isZero()
        {
            return squaredMagnitude() < Constants.SqrFloatEpsilon;
        }

        public static Vector4f operator +(in Vector4f v1, in Vector4f v2)
        {
            return new Vector4f(v1.x + v2.x, v1.y + v2.y, v1.z + v2.z, v1.w + v2.w);
        }

        public static Vector4f operator -(in Vector4f v1, in Vector4f v2)
        {
            return new Vector4f(v1.x - v2.x, v1.y - v2.y, v1.z - v2.z, v1.w - v2.w);
        }

        public static Vector4f operator *(in Vector4f vec, float value)
        {
            return new Vector4f(vec.x * value, vec.y * value, vec.z * value, vec.w * value);
        }

        public static Vector4f operator *(float value, in Vector4f vec)
        {
            return new Vector4f(vec.x * value, vec.y * value, vec.z * value, vec.w * value);
        }

        public static Vector4f operator /(in Vector4f vec, float value)
        {
            return new Vector4f(vec.x / value, vec.y / value, vec.z / value, vec.w / value);
        }

        public static Vector4f operator /(float value, in Vector4f vec)
        {
            return new Vector4f(value / vec.x, value / vec.y, value / vec.z, value / vec.w);
        }

        public static Vector4f operator -(in Vector4f vec)
        {
            return new Vector4f(-vec.x, -vec.y, -vec.z, -vec.w);
        }

        /// <summary>
        /// Dot product
        /// </summary>
        public static float operator *(in Vector4f v1, in Vector4f v2)
        {
            return v1.dotMul(v2);
        }

        /// <summary>
        /// Dot product
        /// </summary>
        public float dotMul(in Vector4f vec)
        {
            return x * vec.x + y * vec.y + z * vec.z + w * vec.w;
        }

        /// <summary>
        /// Component multiplication
        /// </summary>
        /// <returns>New vector: (x1*x2, y1*y2, z1*z2, w1*w2)</returns>
        public Vector4f compMul(in Vector4f vec)
        {
            return new Vector4f(x * vec.x, y * vec.y, z * vec.z, w * vec.w);
        }

        /// <summary>
        /// Component division
        /// </summary>
        /// <returns>New vector: (x1/x2, y1/y2, z1/z2, w1/w2)</returns>
        public Vector4f compDiv(in Vector4f vec)
        {
            return new Vector4f(x / vec.x, y / vec.y, z / vec.z, w / vec.w);
        }

        /// <summary>
        /// Returns normalized copy of this vector
        /// </summary>
        public Vector4f normalized()
        {
            return this / magnitude();
        }

        /// <summary>
        /// Normalizes this vector
        /// </summary>
        public void normalize()
        {
            float magn = magnitude();
            x /= magn;
            y /= magn;
            z /= magn;
            w /= magn;
        }

        /// <summary>
        /// Checks if vectors are equal enough to be considered equal
        /// </summary>
        public bool equals(in Vector4f vec)
        {
            return (vec - this).isZero();
        }

        /// <summary>
        /// Projects vector on another vector
        /// </summary>
        public Vector4f projectOnVector(in Vector4f vec)
        {
            if (vec.isZero())
                return Zero;
            return vec * (this * vec / vec.squaredMagnitude());
        }

        /// <summary>
        /// Projects vector on flat
        /// </summary>
        /// <param name="flatNorm">Normal vector to flat (not necessary normalized)</param>
        /// <returns></returns>
        public Vector4f projectOnFlat(in Vector4f flatNorm)
        {
            return this - flatNorm * (this * flatNorm / flatNorm.squaredMagnitude());
        }

        public override string ToString()
        {
            return "(" + x.ToString() + ", " + y.ToString() + ", " + z.ToString() + ", " + w.ToString() + ")";
        }

        public string ToString(string format)
        {
            return "(" + x.ToString(format) + ", " + y.ToString(format) + ", " + z.ToString(format) + ", " + w.ToString(format) + ")";
        }

        public static bool operator ==(Vector4f lhs, Vector4f rhs)
        {
            return lhs.Equals(rhs);
        }

        public static bool operator !=(Vector4f lhs, Vector4f rhs)
        {
            return !lhs.Equals(rhs);
        }

        public bool Equals(Vector4f other)
        {
            return x.Equals(other.x) && y.Equals(other.y) && z.Equals(other.z) && w.Equals(other.w);
        }

        public override bool Equals(object obj)
        {
            return obj is Vector4f other && Equals(other);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(x, y, z, w);
        }
    }
}