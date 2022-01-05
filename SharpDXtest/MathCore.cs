using System;

namespace SharpDXtest
{
    public static class Constants
    {
        public static float FloatEpsilon = 1e-4f;
        public static float SqrFloatEpsilon = 1e-7f;

        public static double Epsilon = 1e-7;
        public static double SqrEpsilon = 1e-14;
    }
    public struct Vector2f
    {
        public static readonly Vector2f Zero = new Vector2f();
        public static readonly Vector2f UnitX = new Vector2f(1.0f, 0.0f);
        public static readonly Vector2f UnitY = new Vector2f(0.0f, 1.0f);

        public float x { get; set; }
        public float y { get; set; }
        public Vector2f(float x = 0, float y = 0)
        {
            this.x = x;
            this.y = y;
        }
        public Vector2f(float[] values)
        {
            if (values.Length != 2)
                throw new Exception("Array length must be 2.");

            x = values[0];
            y = values[1];
        }

        public static implicit operator Vector3f(Vector2f vec) => new Vector3f(vec);
        public static implicit operator Vector2(Vector2f vec) => new Vector2(vec.x, vec.y);
        public static explicit operator Vector2f(Vector2 vec) => new Vector2f((float)vec.x, (float)vec.y);

        /// <summary>
        /// Magnitude of vector. Equals to length()
        /// </summary>
        public float magnitude()
        {
            return (float)Math.Sqrt(squaredMagnitude());
        }
        /// <summary>
        /// Magnitude of vector without root. Equals to squaredLength()
        /// </summary>
        public float squaredMagnitude()
        {
            return dotMul(this);
        }
        /// <summary>
        /// Length of vector. Equals to magnitude()
        /// </summary>
        public float length()
        {
            return magnitude();
        }
        /// <summary>
        /// Length of vector without root. Equals to squaredMagnitude()
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
        public static Vector2f operator +(Vector2f v1, Vector2f v2)
        {
            return new Vector2f(v1.x + v2.x, v1.y + v2.y);
        }
        public static Vector2f operator -(Vector2f v1, Vector2f v2)
        {
            return new Vector2f(v1.x - v2.x, v1.y - v2.y);
        }
        public static Vector2f operator *(Vector2f vec, float value)
        {
            return new Vector2f(vec.x * value, vec.y * value);
        }
        public static Vector2f operator *(float value, Vector2f vec)
        {
            return new Vector2f(vec.x * value, vec.y * value);
        }
        public static Vector2f operator /(Vector2f vec, float value)
        {
            return new Vector2f(vec.x / value, vec.y / value);
        }
        public static Vector2f operator -(Vector2f vec)
        {
            return new Vector2f(-vec.x, -vec.y);
        }
        /// <summary>
        /// Scalar multiplication
        /// </summary>
        public float dotMul(Vector2f vec)
        {
            return x * vec.x + y * vec.y;
        }
        /// <summary>
        /// Scalar multiplication
        /// </summary>
        public static float operator *(Vector2f v1, Vector2f v2)
        {
            return v1.dotMul(v2);
        }
        /// <summary>
        /// Component multiplication
        /// </summary>
        /// <returns>New vector - (x1*x2, y1*y2)</returns>
        public Vector2f compMul(Vector2f vec)
        {
            return new Vector2f(x * vec.x, y * vec.y);
        }
        /// <summary>
        /// Returns normalized copy of this vector
        /// </summary>
        public Vector2f normalized()
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
        }
        /// <summary>
        /// Checks if vectors same enough to be considered equal
        /// </summary>
        public bool equals(Vector2f vec)
        {
            return (vec - this).isZero();
        }
        /// <summary>
        /// Projects vector on another vector
        /// </summary>
        public Vector2f projectOnVector(Vector2f vec)
        {
            if (vec.isZero())
                return Vector2f.Zero;
            return vec * (this * vec / vec.squaredMagnitude());
        }
        /// <summary>
        /// Vector multiplication
        /// </summary>
        public float vecMul(Vector2f vec)
        {
            return x * vec.y - y * vec.x;
        }
        /// <summary>
        /// Vector multiplication
        /// </summary>
        public float cross(Vector2f vec)
        {
            return vecMul(vec);
        }
        /// <summary>
        /// Vector multiplication
        /// </summary>
        public static float operator %(Vector2f v1, Vector2f v2)
        {
            return v1.vecMul(v2);
        }
        /// <summary>
        /// Checks if vectors are located on parallel lines
        /// </summary>
        /// <returns>True if vectors are located on parallel lines, false otherwise</returns>
        public bool isCollinearTo(Vector2f vec)
        {
            return Math.Abs(this % vec) < Constants.FloatEpsilon;
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
    public struct Vector2
    {
        public static readonly Vector2 Zero = new Vector2();
        public static readonly Vector2 UnitX = new Vector2(1.0, 0.0);
        public static readonly Vector2 UnitY = new Vector2(0.0, 1.0);

        public double x { get; set; }
        public double y { get; set; }
        public Vector2(double x = 0, double y = 0)
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

        public static implicit operator Vector3(Vector2 vec) => new Vector3(vec);

        /// <summary>
        /// Magnitude of vector. Equals to length()
        /// </summary>
        public double magnitude()
        {
            return Math.Sqrt(squaredMagnitude());
        }
        /// <summary>
        /// Magnitude of vector without root. Equals to squaredLength()
        /// </summary>
        public double squaredMagnitude()
        {
            return dotMul(this);
        }
        /// <summary>
        /// Length of vector. Equals to magnitude()
        /// </summary>
        public double length()
        {
            return magnitude();
        }
        /// <summary>
        /// Length of vector without root. Equals to squaredMagnitude()
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
        public static Vector2 operator +(Vector2 v1, Vector2 v2)
        {
            return new Vector2(v1.x + v2.x, v1.y + v2.y);
        }
        public static Vector2 operator -(Vector2 v1, Vector2 v2)
        {
            return new Vector2(v1.x - v2.x, v1.y - v2.y);
        }
        public static Vector2 operator *(Vector2 vec, double value)
        {
            return new Vector2(vec.x * value, vec.y * value);
        }
        public static Vector2 operator *(double value, Vector2 vec)
        {
            return new Vector2(vec.x * value, vec.y * value);
        }
        public static Vector2 operator /(Vector2 vec, double value)
        {
            return new Vector2(vec.x / value, vec.y / value);
        }
        public static Vector2 operator -(Vector2 vec)
        {
            return new Vector2(-vec.x, -vec.y);
        }
        /// <summary>
        /// Scalar multiplication
        /// </summary>
        public double dotMul(Vector2 vec)
        {
            return x * vec.x + y * vec.y;
        }
        /// <summary>
        /// Scalar multiplication
        /// </summary>
        public static double operator *(Vector2 v1, Vector2 v2)
        {
            return v1.dotMul(v2);
        }
        /// <summary>
        /// Component multiplication
        /// </summary>
        /// <returns>New vector - (x1*x2, y1*y2)</returns>
        public Vector2 compMul(Vector2 vec)
        {
            return new Vector2(x * vec.x, y * vec.y);
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
        /// Checks if vectors same enough to be considered equal
        /// </summary>
        public bool equals(Vector2 vec)
        {
            return (vec - this).isZero();
        }
        /// <summary>
        /// Projects vector on another vector
        /// </summary>
        public Vector2 projectOnVector(Vector2 vec)
        {
            if (vec.isZero())
                return Vector2.Zero;
            return vec * (this * vec / vec.squaredMagnitude());
        }
        /// <summary>
        /// Vector multiplication
        /// </summary>
        public double vecMul(Vector2 vec)
        {
            return x * vec.y - y * vec.x;
        }
        /// <summary>
        /// Vector multiplication
        /// </summary>
        public double cross(Vector2 vec)
        {
            return vecMul(vec);
        }
        /// <summary>
        /// Vector multiplication
        /// </summary>
        public static double operator %(Vector2 v1, Vector2 v2)
        {
            return v1.vecMul(v2);
        }
        /// <summary>
        /// Checks if vectors are located on parallel lines
        /// </summary>
        /// <returns>True if vectors are located on parallel lines, false otherwise</returns>
        public bool isCollinearTo(Vector2 vec)
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
    public struct Vector3f
    {
        public static readonly Vector3f Zero = new Vector3f();

        // x - right, y - forward, z - up
        public static readonly Vector3f Right = new Vector3f(1.0f, 0.0f, 0.0f);
        public static readonly Vector3f Forward = new Vector3f(0.0f, 1.0f, 0.0f);
        public static readonly Vector3f Up = new Vector3f(0.0f, 0.0f, 1.0f);
        public static readonly Vector3f UnitX = new Vector3f(1.0f, 0.0f, 0.0f);
        public static readonly Vector3f UnitY = new Vector3f(0.0f, 1.0f, 0.0f);
        public static readonly Vector3f UnitZ = new Vector3f(0.0f, 0.0f, 1.0f);

        public float x { get; set; }
        public float y { get; set; }
        public float z { get; set; }
        public Vector3f(float x = 0, float y = 0, float z = 0)
        {
            this.x = x;
            this.y = y;
            this.z = z;
        }
        public Vector3f(Vector2f vec, float z = 0)
        {
            x = vec.x;
            y = vec.y;
            this.z = z;
        }
        public Vector3f(params float[] values)
        {
            if (values.Length != 3)
                throw new Exception("Array length must be 3.");

            x = values[0];
            y = values[1];
            z = values[2];
        }

        public static implicit operator Vector3(Vector3f vec) => new Vector3(vec.x, vec.y, vec.z);
        public static explicit operator Vector3f(Vector3 vec) => new Vector3f((float)vec.x, (float)vec.y, (float)vec.z);

        /// <summary>
        /// Magnitude of vector. Equals to length()
        /// </summary>
        public float magnitude()
        {
            return (float)Math.Sqrt(squaredMagnitude());
        }
        /// <summary>
        /// Magnitude of vector without root. Equals to squaredLength()
        /// </summary>
        public float squaredMagnitude()
        {
            return dotMul(this);
        }
        /// <summary>
        /// Length of vector. Equals to magnitude()
        /// </summary>
        public float length()
        {
            return magnitude();
        }
        /// <summary>
        /// Length of vector without root. Equals to squaredMagnitude()
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
        public static Vector3f operator +(Vector3f v1, Vector3f v2)
        {
            return new Vector3f(v1.x + v2.x, v1.y + v2.y, v1.z + v2.z);
        }
        public static Vector3f operator -(Vector3f v1, Vector3f v2)
        {
            return new Vector3f(v1.x - v2.x, v1.y - v2.y, v1.z - v2.z);
        }
        public static Vector3f operator *(Vector3f vec, float value)
        {
            return new Vector3f(vec.x * value, vec.y * value, vec.z * value);
        }
        public static Vector3f operator *(float value, Vector3f vec)
        {
            return new Vector3f(vec.x * value, vec.y * value, vec.z * value);
        }
        public static Vector3f operator /(Vector3f vec, float value)
        {
            return new Vector3f(vec.x / value, vec.y / value, vec.z / value);
        }
        public static Vector3f operator -(Vector3f vec)
        {
            return new Vector3f(-vec.x, -vec.y, -vec.z);
        }
        /// <summary>
        /// Scalar multiplication
        /// </summary>
        public float dotMul(Vector3f vec)
        {
            return x * vec.x + y * vec.y + z * vec.z;
        }
        /// <summary>
        /// Scalar multiplication
        /// </summary>
        public static float operator *(Vector3f v1, Vector3f v2)
        {
            return v1.dotMul(v2);
        }
        /// <summary>
        /// Vector multiplication
        /// </summary>
        public Vector3f vecMul(Vector3f vec)
        {
            return new Vector3f(y * vec.z - z * vec.y, x * vec.z - z * vec.x, x * vec.y - y * vec.x);
        }
        /// <summary>
        /// Vector multiplication
        /// </summary>
        public Vector3f cross(Vector3f vec)
        {
            return vecMul(vec);
        }
        /// <summary>
        /// Vector multiplication
        /// </summary>
        public static Vector3f operator %(Vector3f v1, Vector3f v2)
        {
            return v1.vecMul(v2);
        }
        /// <summary>
        /// Component multiplication
        /// </summary>
        /// <returns>New vector - (x1*x2, y1*y2, z1*z2)</returns>
        public Vector3f compMul(Vector3f vec)
        {
            return new Vector3f(x * vec.x, y * vec.y, z * vec.z);
        }
        /// <summary>
        /// Returns normalized copy of this vector
        /// </summary>
        public Vector3f normalized()
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
        }
        /// <summary>
        /// Checks if vectors same enough to be considered equal
        /// </summary>
        public bool equals(Vector3f vec)
        {
            return (vec - this).isZero();
        }
        /// <summary>
        /// Projects vector on another vector
        /// </summary>
        public Vector3f projectOnVector(Vector3f vec)
        {
            if (vec.isZero())
                return Vector3f.Zero;
            return vec * (this * vec / vec.squaredMagnitude());
        }
        /// <summary>
        /// Projects vector on flat
        /// </summary>
        /// <param name="flatNorm">Normal vector to flat (not necessary normalized)</param>
        /// <returns></returns>
        public Vector3f projectOnFlat(Vector3f flatNorm)
        {
            return this - flatNorm * (this * flatNorm / flatNorm.squaredMagnitude());
        }
        /// <summary>
        /// Checks if vectors are located on parallel lines
        /// </summary>
        /// <returns>True if vectors are located on parallel lines, false otherwise</returns>
        public bool isCollinearTo(Vector3f vec)
        {
            return (this % vec).isZero();
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
    public struct Vector3
    {
        public static readonly Vector3 Zero = new Vector3();

        // x - right, y - forward, z - up
        public static Vector3 Right { get { return new Vector3(1.0, 0.0, 0.0); } }
        public static Vector3 Forward { get { return new Vector3(0.0, 1.0, 0.0); } }
        public static Vector3 Up { get { return new Vector3(0.0, 0.0, 1.0); } }
        public static readonly Vector3 UnitX = new Vector3(1.0, 0.0, 0.0);
        public static readonly Vector3 UnitY = new Vector3(0.0, 1.0, 0.0);
        public static readonly Vector3 UnitZ = new Vector3(0.0, 0.0, 1.0);

        public double x { get; set; }
        public double y { get; set; }
        public double z { get; set; }
        public Vector3(double x = 0, double y = 0, double z = 0)
        {
            this.x = x;
            this.y = y;
            this.z = z;
        }
        public Vector3(Vector2 vec, double z = 0)
        {
            x = vec.x;
            y = vec.y;
            this.z = z;
        }
        public Vector3(params double[] values)
        {
            if (values.Length != 3)
                throw new Exception("Array length must be 3.");

            x = values[0];
            y = values[1];
            z = values[2];
        }
        public static double divideCollinearVectors(Vector3 a, Vector3 b)
        {
            if (b.isZero())
                throw new ArgumentException("Can't divide on zero vector!");

            return b.x < Constants.Epsilon ? (b.y < Constants.Epsilon ? a.z / b.z : a.y / b.y) : a.x / b.x;
        }
        /// <summary>
        /// Magnitude of vector. Equals to length()
        /// </summary>
        public double magnitude()
        {
            return Math.Sqrt(squaredMagnitude());
        }
        /// <summary>
        /// Magnitude of vector without root. Equals to squaredLength()
        /// </summary>
        public double squaredMagnitude()
        {
            return dotMul(this);
        }
        /// <summary>
        /// Length of vector. Equals to magnitude()
        /// </summary>
        public double length()
        {
            return magnitude();
        }
        /// <summary>
        /// Length of vector without root. Equals to squaredMagnitude()
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
        public static Vector3 operator +(Vector3 v1, Vector3 v2)
        {
            return new Vector3(v1.x + v2.x, v1.y + v2.y, v1.z + v2.z);
        }
        public static Vector3 operator -(Vector3 v1, Vector3 v2)
        {
            return new Vector3(v1.x - v2.x, v1.y - v2.y, v1.z - v2.z);
        }
        public static Vector3 operator *(Vector3 vec, double value)
        {
            return new Vector3(vec.x * value, vec.y * value, vec.z * value);
        }
        public static Vector3 operator *(double value, Vector3 vec)
        {
            return new Vector3(vec.x * value, vec.y * value, vec.z * value);
        }
        public static Vector3 operator /(Vector3 vec, double value)
        {
            return new Vector3(vec.x / value, vec.y / value, vec.z / value);
        }
        public static Vector3 operator -(Vector3 vec)
        {
            return new Vector3(-vec.x, -vec.y, -vec.z);
        }
        /// <summary>
        /// Scalar multiplication
        /// </summary>
        public double dotMul(Vector3 vec)
        {
            return x * vec.x + y * vec.y + z * vec.z;
        }
        /// <summary>
        /// Scalar multiplication
        /// </summary>
        public static double operator *(Vector3 v1, Vector3 v2)
        {
            return v1.dotMul(v2);
        }
        /// <summary>
        /// Vector multiplication
        /// </summary>
        public Vector3 vecMul(Vector3 vec)
        {
            return new Vector3(y * vec.z - z * vec.y, x * vec.z - z * vec.x, x * vec.y - y * vec.x);
        }
        /// <summary>
        /// Vector multiplication
        /// </summary>
        public Vector3 cross(Vector3 vec)
        {
            return vecMul(vec);
        }
        /// <summary>
        /// Vector multiplication
        /// </summary>
        public static Vector3 operator %(Vector3 v1, Vector3 v2)
        {
            return v1.vecMul(v2);
        }
        /// <summary>
        /// Component multiplication
        /// </summary>
        /// <returns>New vector - (x1*x2, y1*y2, z1*z2)</returns>
        public Vector3 compMul(Vector3 vec)
        {
            return new Vector3(x * vec.x, y * vec.y, z * vec.z);
        }
        /// <summary>
        /// Returns normalized copy of this vector
        /// </summary>
        public Vector3 normalized()
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
            z /= magn;
        }
        /// <summary>
        /// Checks if vectors same enough to be considered equal
        /// </summary>
        public bool equals(Vector3 vec)
        {
            return (vec - this).isZero();
        }
        /// <summary>
        /// Projects vector on another vector
        /// </summary>
        public Vector3 projectOnVector(Vector3 vec)
        {
            if (vec.isZero())
                return Vector3.Zero;
            return vec * (this * vec / vec.squaredMagnitude());
        }
        /// <summary>
        /// Projects vector on flat
        /// </summary>
        /// <param name="flatNorm">Normal vector to flat (not necessary normalized)</param>
        /// <returns></returns>
        public Vector3 projectOnFlat(Vector3 flatNorm)
        {
            return this - flatNorm * (this * flatNorm / flatNorm.squaredMagnitude());
        }
        /// <summary>
        /// Checks if vectors are located on parallel lines
        /// </summary>
        /// <returns>True if vectors are located on parallel lines, false otherwise</returns>
        public bool isCollinearTo(Vector3 vec)
        {
            return (this % vec).isZero();
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
    public struct Vector4f
    {
        public static readonly Vector4f Zero = new Vector4f();
        public static readonly Vector4f UnitX = new Vector4f(1.0f, 0.0f, 0.0f, 0.0f);
        public static readonly Vector4f UnitY = new Vector4f(0.0f, 1.0f, 0.0f, 0.0f);
        public static readonly Vector4f UnitZ = new Vector4f(0.0f, 0.0f, 1.0f, 0.0f);
        public static readonly Vector4f UnitW = new Vector4f(0.0f, 0.0f, 0.0f, 1.0f);

        public float x { get; set; }
        public float y { get; set; }
        public float z { get; set; }
        public float w{ get; set; }
        public Vector3f xyz { get { return new Vector3f(x, y, z); } }
        public Vector4f(float x = 0, float y = 0, float z = 0, float w = 0)
        {
            this.x = x;
            this.y = y;
            this.z = z;
            this.w = w;
        }
        public Vector4f(Vector2f vec, float z = 0, float w = 0)
        {
            x = vec.x;
            y = vec.y;
            this.z = z;
            this.w = w;
        }
        public Vector4f(Vector3f vec, float w = 0)
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

        public static implicit operator Vector4(Vector4f vec) => new Vector4(vec.x, vec.y, vec.z);
        public static explicit operator Vector4f(Vector4 vec) => new Vector4f((float)vec.x, (float)vec.y, (float)vec.z, (float)vec.w);

        /// <summary>
        /// Magnitude of vector. Equals to length()
        /// </summary>
        public float magnitude()
        {
            return (float)Math.Sqrt(squaredMagnitude());
        }
        /// <summary>
        /// Magnitude of vector without root. Equals to squaredLength()
        /// </summary>
        public float squaredMagnitude()
        {
            return dotMul(this);
        }
        /// <summary>
        /// Length of vector. Equals to magnitude()
        /// </summary>
        public float length()
        {
            return magnitude();
        }
        /// <summary>
        /// Length of vector without root. Equals to squaredMagnitude()
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
        public static Vector4f operator +(Vector4f v1, Vector4f v2)
        {
            return new Vector4f(v1.x + v2.x, v1.y + v2.y, v1.z + v2.z, v1.w + v2.w);
        }
        public static Vector4f operator -(Vector4f v1, Vector4f v2)
        {
            return new Vector4f(v1.x - v2.x, v1.y - v2.y, v1.z - v2.z, v1.w - v2.w);
        }
        public static Vector4f operator *(Vector4f vec, float value)
        {
            return new Vector4f(vec.x * value, vec.y * value, vec.z * value, vec.w * value);
        }
        public static Vector4f operator *(float value, Vector4f vec)
        {
            return new Vector4f(vec.x * value, vec.y * value, vec.z * value, vec.w * value);
        }
        public static Vector4f operator /(Vector4f vec, float value)
        {
            return new Vector4f(vec.x / value, vec.y / value, vec.z / value, vec.w / value);
        }
        public static Vector4f operator -(Vector4f vec)
        {
            return new Vector4f(-vec.x, -vec.y, -vec.z, -vec.w);
        }
        /// <summary>
        /// Scalar multiplication
        /// </summary>
        public float dotMul(Vector4f vec)
        {
            return x * vec.x + y * vec.y + z * vec.z + w * vec.w;
        }
        /// <summary>
        /// Scalar multiplication
        /// </summary>
        public static float operator *(Vector4f v1, Vector4f v2)
        {
            return v1.dotMul(v2);
        }
        /// <summary>
        /// Component multiplication
        /// </summary>
        /// <returns>New vector - (x1*x2, y1*y2, z1*z2, w1*w2)</returns>
        public Vector4f compMul(Vector4f vec)
        {
            return new Vector4f(x * vec.x, y * vec.y, z * vec.z, w * vec.w);
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
        /// Checks if vectors same enough to be considered equal
        /// </summary>
        public bool equals(Vector4f vec)
        {
            return (vec - this).isZero();
        }
        /// <summary>
        /// Projects vector on another vector
        /// </summary>
        public Vector4f projectOnVector(Vector4f vec)
        {
            if (vec.isZero())
                return Vector4f.Zero;
            return vec * (this * vec / vec.squaredMagnitude());
        }
        /// <summary>
        /// Projects vector on flat
        /// </summary>
        /// <param name="flatNorm">Normal vector to flat (not necessary normalized)</param>
        /// <returns></returns>
        public Vector4f projectOnFlat(Vector4f flatNorm)
        {
            return this - flatNorm * (this * flatNorm / flatNorm.squaredMagnitude());
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
    public struct Vector4
    {
        public static readonly Vector4 Zero = new Vector4();
        public static readonly Vector4 UnitX = new Vector4(1.0, 0.0, 0.0, 0.0);
        public static readonly Vector4 UnitY = new Vector4(0.0, 1.0, 0.0, 0.0);
        public static readonly Vector4 UnitZ = new Vector4(0.0, 0.0, 1.0, 0.0);
        public static readonly Vector4 UnitW = new Vector4(0.0, 0.0, 0.0, 1.0);

        public double x { get; set; }
        public double y { get; set; }
        public double z { get; set; }
        public double w { get; set; }
        public Vector3 xyz { get { return new Vector3(x, y, z); } }
        public Vector4(double x = 0, double y = 0, double z = 0, double w = 0)
        {
            this.x = x;
            this.y = y;
            this.z = z;
            this.w = w;
        }
        public Vector4(Vector2 vec, double z = 0, double w = 0)
        {
            x = vec.x;
            y = vec.y;
            this.z = z;
            this.w = w;
        }
        public Vector4(Vector3 vec, double w = 0)
        {
            x = vec.x;
            y = vec.y;
            z = vec.z;
            this.w = w;
        }
        public Vector4(params double[] values)
        {
            if (values.Length != 4)
                throw new Exception("Array length must be 4.");

            x = values[0];
            y = values[1];
            z = values[2];
            w = values[3];
        }
        /// <summary>
        /// Magnitude of vector. Equals to length()
        /// </summary>
        public double magnitude()
        {
            return Math.Sqrt(squaredMagnitude());
        }
        /// <summary>
        /// Magnitude of vector without root. Equals to squaredLength()
        /// </summary>
        public double squaredMagnitude()
        {
            return dotMul(this);
        }
        /// <summary>
        /// Length of vector. Equals to magnitude()
        /// </summary>
        public double length()
        {
            return magnitude();
        }
        /// <summary>
        /// Length of vector without root. Equals to squaredMagnitude()
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
        public static Vector4 operator +(Vector4 v1, Vector4 v2)
        {
            return new Vector4(v1.x + v2.x, v1.y + v2.y, v1.z + v2.z, v1.w + v2.w);
        }
        public static Vector4 operator -(Vector4 v1, Vector4 v2)
        {
            return new Vector4(v1.x - v2.x, v1.y - v2.y, v1.z - v2.z, v1.w - v2.w);
        }
        public static Vector4 operator *(Vector4 vec, double value)
        {
            return new Vector4(vec.x * value, vec.y * value, vec.z * value, vec.w * value);
        }
        public static Vector4 operator *(double value, Vector4 vec)
        {
            return new Vector4(vec.x * value, vec.y * value, vec.z * value, vec.w * value);
        }
        public static Vector4 operator /(Vector4 vec, double value)
        {
            return new Vector4(vec.x / value, vec.y / value, vec.z / value, vec.w / value);
        }
        public static Vector4 operator -(Vector4 vec)
        {
            return new Vector4(-vec.x, -vec.y, -vec.z, -vec.w);
        }
        /// <summary>
        /// Scalar multiplication
        /// </summary>
        public double dotMul(Vector4 vec)
        {
            return x * vec.x + y * vec.y + z * vec.z + w * vec.w;
        }
        /// <summary>
        /// Scalar multiplication
        /// </summary>
        public static double operator *(Vector4 v1, Vector4 v2)
        {
            return v1.dotMul(v2);
        }
        /// <summary>
        /// Component multiplication
        /// </summary>
        /// <returns>New vector - (x1*x2, y1*y2, z1*z2, w1*w2)</returns>
        public Vector4 compMul(Vector4 vec)
        {
            return new Vector4(x * vec.x, y * vec.y, z * vec.z, w * vec.w);
        }
        /// <summary>
        /// Returns normalized copy of this vector
        /// </summary>
        public Vector4 normalized()
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
            z /= magn;
            w /= magn;
        }
        /// <summary>
        /// Checks if vectors same enough to be considered equal
        /// </summary>
        public bool equals(Vector4 vec)
        {
            return (vec - this).isZero();
        }
        /// <summary>
        /// Projects vector on another vector
        /// </summary>
        public Vector4 projectOnVector(Vector4 vec)
        {
            if (vec.isZero())
                return Vector4.Zero;
            return vec * (this * vec / vec.squaredMagnitude());
        }
        /// <summary>
        /// Projects vector on flat
        /// </summary>
        /// <param name="flatNorm">Normal vector to flat (not necessary normalized)</param>
        /// <returns></returns>
        public Vector4 projectOnFlat(Vector4 flatNorm)
        {
            return this - flatNorm * (this * flatNorm / flatNorm.squaredMagnitude());
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
    public struct Matrix4x4f
    {
        public float v00 { get; set; } // [rowIndex, columnIndex]
        public float v01 { get; set; }
        public float v02 { get; set; }
        public float v03 { get; set; }
        public float v10 { get; set; }
        public float v11 { get; set; }
        public float v12 { get; set; }
        public float v13 { get; set; }
        public float v20 { get; set; }
        public float v21 { get; set; }
        public float v22 { get; set; }
        public float v23 { get; set; }
        public float v30 { get; set; }
        public float v31 { get; set; }
        public float v32 { get; set; }
        public float v33 { get; set; }

        public static Matrix4x4f Identity { get { return new Matrix4x4f(1, 0, 0, 0, 0, 1, 0, 0, 0, 0, 1, 0, 0, 0, 0, 1); } }
        public Matrix4x4f(params float[] values)
        {
            if (values.Length != 16)
                throw new Exception("Array length must be 16.");
            v00 = values[0];
            v01 = values[1];
            v02 = values[2];
            v03 = values[3];
            v10 = values[4];
            v11 = values[5];
            v12 = values[6];
            v13 = values[7];
            v20 = values[8];
            v21 = values[9];
            v22 = values[10];
            v23 = values[11];
            v30 = values[12];
            v31 = values[13];
            v32 = values[14];
            v33 = values[15];
        }

        public static implicit operator Matrix4x4(Matrix4x4f mat) => new Matrix4x4(mat.v00, mat.v01, mat.v02, mat.v03,
                                                                                   mat.v10, mat.v11, mat.v12, mat.v13,
                                                                                   mat.v20, mat.v21, mat.v22, mat.v23,
                                                                                   mat.v30, mat.v31, mat.v32, mat.v33);
        public static explicit operator Matrix4x4f(Matrix4x4 mat) => new Matrix4x4f((float)mat.v00, (float)mat.v01, (float)mat.v02, (float)mat.v03,
                                                                                    (float)mat.v10, (float)mat.v11, (float)mat.v12, (float)mat.v13,
                                                                                    (float)mat.v20, (float)mat.v21, (float)mat.v22, (float)mat.v23,
                                                                                    (float)mat.v30, (float)mat.v31, (float)mat.v32, (float)mat.v33);
        /// <summary>
        /// Returns transposed copy of this matrix
        /// </summary>
        public Matrix4x4f transposed()
        {
            return new Matrix4x4f(v00, v10, v20, v30,
                                  v01, v11, v21, v31,
                                  v02, v12, v22, v32,
                                  v03, v13, v32, v33);
        }
        public static Matrix4x4f operator *(Matrix4x4f m1, Matrix4x4f m2)
        {
            return new Matrix4x4f(m1.v00*m2.v00 + m1.v01*m2.v10 + m1.v02*m2.v20 + m1.v03*m2.v30,
                                  m1.v00*m2.v01 + m1.v01*m2.v11 + m1.v02*m2.v21 + m1.v03*m2.v31,
                                  m1.v00*m2.v02 + m1.v01*m2.v12 + m1.v02*m2.v22 + m1.v03*m2.v32,
                                  m1.v00*m2.v03 + m1.v01*m2.v13 + m1.v02*m2.v23 + m1.v03*m2.v33,
                                  
                                  m1.v10*m2.v00 + m1.v11*m2.v10 + m1.v12*m2.v20 + m1.v13*m2.v30,
                                  m1.v10*m2.v01 + m1.v11*m2.v11 + m1.v12*m2.v21 + m1.v13*m2.v31,
                                  m1.v10*m2.v02 + m1.v11*m2.v12 + m1.v12*m2.v22 + m1.v13*m2.v32,
                                  m1.v10*m2.v03 + m1.v11*m2.v13 + m1.v12*m2.v23 + m1.v13*m2.v33,
                                  
                                  m1.v20*m2.v00 + m1.v21*m2.v10 + m1.v22*m2.v20 + m1.v23*m2.v30,
                                  m1.v20*m2.v01 + m1.v21*m2.v11 + m1.v22*m2.v21 + m1.v23*m2.v31,
                                  m1.v20*m2.v02 + m1.v21*m2.v12 + m1.v22*m2.v22 + m1.v23*m2.v32,
                                  m1.v20*m2.v03 + m1.v21*m2.v13 + m1.v22*m2.v23 + m1.v23*m2.v33,
                                  
                                  m1.v30*m2.v00 + m1.v31*m2.v10 + m1.v32*m2.v20 + m1.v33*m2.v30,
                                  m1.v30*m2.v01 + m1.v31*m2.v11 + m1.v32*m2.v21 + m1.v33*m2.v31,
                                  m1.v30*m2.v02 + m1.v31*m2.v12 + m1.v32*m2.v22 + m1.v33*m2.v32,
                                  m1.v30*m2.v03 + m1.v31*m2.v13 + m1.v32*m2.v23 + m1.v33*m2.v33);
        }
        public static Matrix4x4f operator *(Matrix4x4f mat, float value)
        {
            return new Matrix4x4f(mat.v00 * value, mat.v01 * value, mat.v02 * value, mat.v03 * value,
                                  mat.v10 * value, mat.v11 * value, mat.v12 * value, mat.v13 * value,
                                  mat.v20 * value, mat.v21 * value, mat.v22 * value, mat.v23 * value,
                                  mat.v30 * value, mat.v31 * value, mat.v32 * value, mat.v33 * value);
        }
        public static Matrix4x4f operator /(Matrix4x4f mat, float value)
        {
            if (value == 0)
                throw new DivideByZeroException();
            return new Matrix4x4f(mat.v00 / value, mat.v01 / value, mat.v02 / value, mat.v03 / value,
                                  mat.v10 / value, mat.v11 / value, mat.v12 / value, mat.v13 / value,
                                  mat.v20 / value, mat.v21 / value, mat.v22 / value, mat.v23 / value,
                                  mat.v30 / value, mat.v31 / value, mat.v32 / value, mat.v33 / value);
        }
        public static Vector4f operator *(Matrix4x4f mat, Vector4f vec)
        {
            return new Vector4f(mat.v00 * vec.x + mat.v01 * vec.y + mat.v02 * vec.z + mat.v03 * vec.w,
                                mat.v10 * vec.x + mat.v11 * vec.y + mat.v12 * vec.z + mat.v13 * vec.w,
                                mat.v20 * vec.x + mat.v21 * vec.y + mat.v22 * vec.z + mat.v23 * vec.w,
                                mat.v30 * vec.x + mat.v31 * vec.y + mat.v32 * vec.z + mat.v33 * vec.w);
        }
        public Matrix4x4f inversed()
        {
            float det00 = (v11 * (v22 * v33 - v23 * v32) - v12 * (v21 * v33 - v23 * v31) + v13 * (v21 * v32 - v22 * v31));
            float det01 = (v10 * (v22 * v33 - v23 * v32) - v12 * (v20 * v33 - v23 * v30) + v13 * (v20 * v32 - v22 * v30));
            float det02 = (v10 * (v21 * v33 - v23 * v31) - v11 * (v20 * v33 - v23 * v30) + v13 * (v20 * v31 - v21 * v30));
            float det03 = (v10 * (v21 * v32 - v22 * v31) - v11 * (v20 * v32 - v22 * v30) + v12 * (v20 * v31 - v21 * v30));

            float determinant = det00 - det01 + det02 - det03;
            if (determinant == 0)
                throw new Exception("This matrix is singular. (determinant = 0)");

            float det10 = (v01 * (v22 * v33 - v23 * v32) - v02 * (v21 * v33 - v23 * v31) + v03 * (v21 * v32 - v22 * v31));
            float det11 = (v00 * (v22 * v33 - v23 * v32) - v02 * (v20 * v33 - v23 * v30) + v03 * (v20 * v32 - v22 * v30));
            float det12 = (v00 * (v21 * v33 - v23 * v31) - v01 * (v20 * v33 - v23 * v30) + v03 * (v20 * v31 - v21 * v30));
            float det13 = (v00 * (v21 * v32 - v22 * v31) - v01 * (v20 * v32 - v22 * v30) + v02 * (v20 * v31 - v21 * v30));
            float det20 = (v01 * (v12 * v33 - v13 * v32) - v02 * (v11 * v33 - v13 * v31) + v03 * (v11 * v32 - v12 * v31));
            float det21 = (v00 * (v12 * v33 - v13 * v32) - v02 * (v10 * v33 - v13 * v30) + v03 * (v10 * v32 - v12 * v30));
            float det22 = (v00 * (v11 * v33 - v13 * v31) - v01 * (v10 * v33 - v13 * v30) + v03 * (v10 * v31 - v11 * v30));
            float det23 = (v00 * (v11 * v32 - v12 * v31) - v01 * (v10 * v32 - v12 * v30) + v02 * (v10 * v31 - v11 * v30));
            float det30 = (v01 * (v12 * v23 - v13 * v22) - v02 * (v11 * v23 - v13 * v21) + v03 * (v11 * v22 - v12 * v21));
            float det31 = (v00 * (v12 * v23 - v13 * v22) - v02 * (v10 * v23 - v13 * v20) + v03 * (v10 * v22 - v12 * v20));
            float det32 = (v00 * (v11 * v23 - v13 * v21) - v01 * (v10 * v23 - v13 * v20) + v03 * (v10 * v21 - v11 * v20));
            float det33 = (v00 * (v11 * v22 - v12 * v21) - v01 * (v10 * v22 - v12 * v20) + v02 * (v10 * v21 - v11 * v20));

            return new Matrix4x4f( det00, -det10,  det20, -det30,
                                  -det01,  det11, -det21,  det31,
                                   det02, -det12,  det22, -det32,
                                  -det03,  det13, -det23,  det33) / determinant;
        }
        public override string ToString()
        {
            return "| " + v00.ToString() + " " + v01.ToString() + " " + v02.ToString() + " " + v03.ToString() + " |\n" +
                   "| " + v10.ToString() + " " + v11.ToString() + " " + v12.ToString() + " " + v13.ToString() + " |\n" +
                   "| " + v20.ToString() + " " + v21.ToString() + " " + v22.ToString() + " " + v23.ToString() + " |\n" +
                   "| " + v30.ToString() + " " + v31.ToString() + " " + v32.ToString() + " " + v33.ToString() + " |";
        }
        public string ToString(string format)
        {
            return "| " + v00.ToString(format) + " " + v01.ToString(format) + " " + v02.ToString(format) + " " + v03.ToString(format) + " |\n" +
                   "| " + v10.ToString(format) + " " + v11.ToString(format) + " " + v12.ToString(format) + " " + v13.ToString(format) + " |\n" +
                   "| " + v20.ToString(format) + " " + v21.ToString(format) + " " + v22.ToString(format) + " " + v23.ToString(format) + " |\n" +
                   "| " + v30.ToString(format) + " " + v31.ToString(format) + " " + v32.ToString(format) + " " + v33.ToString(format) + " |";
        }
    }
    public struct Matrix4x4
    {
        public double v00 { get; set; } // [rowIndex, columnIndex]
        public double v01 { get; set; }
        public double v02 { get; set; }
        public double v03 { get; set; }
        public double v10 { get; set; }
        public double v11 { get; set; }
        public double v12 { get; set; }
        public double v13 { get; set; }
        public double v20 { get; set; }
        public double v21 { get; set; }
        public double v22 { get; set; }
        public double v23 { get; set; }
        public double v30 { get; set; }
        public double v31 { get; set; }
        public double v32 { get; set; }
        public double v33 { get; set; }

        public static Matrix4x4 Identity { get { return new Matrix4x4(1, 0, 0, 0, 0, 1, 0, 0, 0, 0, 1, 0, 0, 0, 0, 1); } }
        public Matrix4x4(params double[] values)
        {
            if (values.Length != 16)
                throw new Exception("Array length must be 16.");
            v00 = values[0];
            v01 = values[1];
            v02 = values[2];
            v03 = values[3];
            v10 = values[4];
            v11 = values[5];
            v12 = values[6];
            v13 = values[7];
            v20 = values[8];
            v21 = values[9];
            v22 = values[10];
            v23 = values[11];
            v30 = values[12];
            v31 = values[13];
            v32 = values[14];
            v33 = values[15];
        }
        /// <summary>
        /// Returns transposed copy of this matrix
        /// </summary>
        public Matrix4x4 transposed()
        {
            return new Matrix4x4(v00, v10, v20, v30,
                                 v01, v11, v21, v31,
                                 v02, v12, v22, v32,
                                 v03, v13, v32, v33 );
        }
        public static Matrix4x4 operator *(Matrix4x4 m1, Matrix4x4 m2)
        {
            return new Matrix4x4(m1.v00*m2.v00 + m1.v01*m2.v10 + m1.v02*m2.v20 + m1.v03*m2.v30,
                                 m1.v00*m2.v01 + m1.v01*m2.v11 + m1.v02*m2.v21 + m1.v03*m2.v31,
                                 m1.v00*m2.v02 + m1.v01*m2.v12 + m1.v02*m2.v22 + m1.v03*m2.v32,
                                 m1.v00*m2.v03 + m1.v01*m2.v13 + m1.v02*m2.v23 + m1.v03*m2.v33,
                                                                                 
                                 m1.v10*m2.v00 + m1.v11*m2.v10 + m1.v12*m2.v20 + m1.v13*m2.v30,
                                 m1.v10*m2.v01 + m1.v11*m2.v11 + m1.v12*m2.v21 + m1.v13*m2.v31,
                                 m1.v10*m2.v02 + m1.v11*m2.v12 + m1.v12*m2.v22 + m1.v13*m2.v32,
                                 m1.v10*m2.v03 + m1.v11*m2.v13 + m1.v12*m2.v23 + m1.v13*m2.v33,
                                                                                 
                                 m1.v20*m2.v00 + m1.v21*m2.v10 + m1.v22*m2.v20 + m1.v23*m2.v30,
                                 m1.v20*m2.v01 + m1.v21*m2.v11 + m1.v22*m2.v21 + m1.v23*m2.v31,
                                 m1.v20*m2.v02 + m1.v21*m2.v12 + m1.v22*m2.v22 + m1.v23*m2.v32,
                                 m1.v20*m2.v03 + m1.v21*m2.v13 + m1.v22*m2.v23 + m1.v23*m2.v33,
                                                                                 
                                 m1.v30*m2.v00 + m1.v31*m2.v10 + m1.v32*m2.v20 + m1.v33*m2.v30,
                                 m1.v30*m2.v01 + m1.v31*m2.v11 + m1.v32*m2.v21 + m1.v33*m2.v31,
                                 m1.v30*m2.v02 + m1.v31*m2.v12 + m1.v32*m2.v22 + m1.v33*m2.v32,
                                 m1.v30*m2.v03 + m1.v31*m2.v13 + m1.v32*m2.v23 + m1.v33*m2.v33);
        }
        public static Matrix4x4 operator *(Matrix4x4 mat, double value)
        {
            return new Matrix4x4(mat.v00 * value, mat.v01 * value, mat.v02 * value, mat.v03 * value,
                                 mat.v10 * value, mat.v11 * value, mat.v12 * value, mat.v13 * value,
                                 mat.v20 * value, mat.v21 * value, mat.v22 * value, mat.v23 * value,
                                 mat.v30 * value, mat.v31 * value, mat.v32 * value, mat.v33 * value );
        }
        public static Matrix4x4 operator /(Matrix4x4 mat, double value)
        {
            if (value == 0)
                throw new DivideByZeroException();
            return new Matrix4x4(mat.v00 / value, mat.v01 / value, mat.v02 / value, mat.v03 / value,
                                 mat.v10 / value, mat.v11 / value, mat.v12 / value, mat.v13 / value,
                                 mat.v20 / value, mat.v21 / value, mat.v22 / value, mat.v23 / value,
                                 mat.v30 / value, mat.v31 / value, mat.v32 / value, mat.v33 / value );
        }
        public static Vector4 operator *(Matrix4x4 mat, Vector4 vec)
        {
            return new Vector4(mat.v00 * vec.x + mat.v01 * vec.y + mat.v02 * vec.z + mat.v03 * vec.w,
                               mat.v10 * vec.x + mat.v11 * vec.y + mat.v12 * vec.z + mat.v13 * vec.w,
                               mat.v20 * vec.x + mat.v21 * vec.y + mat.v22 * vec.z + mat.v23 * vec.w,
                               mat.v30 * vec.x + mat.v31 * vec.y + mat.v32 * vec.z + mat.v33 * vec.w);
        }
        public Matrix4x4 inversed()
        {
            double det00 = (v11 * (v22 * v33 - v23 * v32) - v12 * (v21 * v33 - v23 * v31) + v13 * (v21 * v32 - v22 * v31));
            double det01 = (v10 * (v22 * v33 - v23 * v32) - v12 * (v20 * v33 - v23 * v30) + v13 * (v20 * v32 - v22 * v30));
            double det02 = (v10 * (v21 * v33 - v23 * v31) - v11 * (v20 * v33 - v23 * v30) + v13 * (v20 * v31 - v21 * v30));
            double det03 = (v10 * (v21 * v32 - v22 * v31) - v11 * (v20 * v32 - v22 * v30) + v12 * (v20 * v31 - v21 * v30));

            double determinant = det00 - det01 + det02 - det03;
            if (determinant == 0)
                throw new Exception("This matrix is singular. (determinant = 0)");

            double det10 = (v01 * (v22 * v33 - v23 * v32) - v02 * (v21 * v33 - v23 * v31) + v03 * (v21 * v32 - v22 * v31));
            double det11 = (v00 * (v22 * v33 - v23 * v32) - v02 * (v20 * v33 - v23 * v30) + v03 * (v20 * v32 - v22 * v30));
            double det12 = (v00 * (v21 * v33 - v23 * v31) - v01 * (v20 * v33 - v23 * v30) + v03 * (v20 * v31 - v21 * v30));
            double det13 = (v00 * (v21 * v32 - v22 * v31) - v01 * (v20 * v32 - v22 * v30) + v02 * (v20 * v31 - v21 * v30));
            double det20 = (v01 * (v12 * v33 - v13 * v32) - v02 * (v11 * v33 - v13 * v31) + v03 * (v11 * v32 - v12 * v31));
            double det21 = (v00 * (v12 * v33 - v13 * v32) - v02 * (v10 * v33 - v13 * v30) + v03 * (v10 * v32 - v12 * v30));
            double det22 = (v00 * (v11 * v33 - v13 * v31) - v01 * (v10 * v33 - v13 * v30) + v03 * (v10 * v31 - v11 * v30));
            double det23 = (v00 * (v11 * v32 - v12 * v31) - v01 * (v10 * v32 - v12 * v30) + v02 * (v10 * v31 - v11 * v30));
            double det30 = (v01 * (v12 * v23 - v13 * v22) - v02 * (v11 * v23 - v13 * v21) + v03 * (v11 * v22 - v12 * v21));
            double det31 = (v00 * (v12 * v23 - v13 * v22) - v02 * (v10 * v23 - v13 * v20) + v03 * (v10 * v22 - v12 * v20));
            double det32 = (v00 * (v11 * v23 - v13 * v21) - v01 * (v10 * v23 - v13 * v20) + v03 * (v10 * v21 - v11 * v20));
            double det33 = (v00 * (v11 * v22 - v12 * v21) - v01 * (v10 * v22 - v12 * v20) + v02 * (v10 * v21 - v11 * v20));

            return new Matrix4x4( det00, -det10,  det20, -det30,
                                 -det01,  det11, -det21,  det31,
                                  det02, -det12,  det22, -det32,
                                 -det03,  det13, -det23,  det33 ) / determinant;
        }
        public static Matrix4x4 FromQuaternion(Quaternion q)
        {
            double s2 = 2.0 / q.norm();
            return new Matrix4x4(1-s2*(q.y*q.y+q.z*q.z),   s2*(q.x*q.y-q.w*q.z),   s2*(q.x*q.z+q.w*q.y), 0,
                                   s2*(q.x*q.y+q.w*q.z), 1-s2*(q.x*q.x+q.z*q.z),   s2*(q.y*q.z-q.w*q.x), 0,
                                   s2*(q.x*q.z-q.w*q.y),   s2*(q.y*q.z+q.w*q.x), 1-s2*(q.x*q.x+q.y*q.y), 0,
                                             0,             0,             0,                            1);
        }
        public override string ToString()
        {
            return "| " + v00.ToString() + " " + v01.ToString() + " " + v02.ToString() + " " + v03.ToString() + " |\n" +
                   "| " + v10.ToString() + " " + v11.ToString() + " " + v12.ToString() + " " + v13.ToString() + " |\n" +
                   "| " + v20.ToString() + " " + v21.ToString() + " " + v22.ToString() + " " + v23.ToString() + " |\n" +
                   "| " + v30.ToString() + " " + v31.ToString() + " " + v32.ToString() + " " + v33.ToString() + " |";
        }
        public string ToString(string format)
        {
            return "| " + v00.ToString(format) + " " + v01.ToString(format) + " " + v02.ToString(format) + " " + v03.ToString(format) + " |\n" +
                   "| " + v10.ToString(format) + " " + v11.ToString(format) + " " + v12.ToString(format) + " " + v13.ToString(format) + " |\n" +
                   "| " + v20.ToString(format) + " " + v21.ToString(format) + " " + v22.ToString(format) + " " + v23.ToString(format) + " |\n" +
                   "| " + v30.ToString(format) + " " + v31.ToString(format) + " " + v32.ToString(format) + " " + v33.ToString(format) + " |";
        }
    }
    public enum EulerOrder
    {
        XYZ,
        XZY,
        YXZ,
        YZX,
        ZXY,
        ZYX
    }
    // do NOT ask me about this, i don't give a fuck how this works. (times tried to understand: 2)
    public struct Quaternion
    {
        public double w, x, y, z;

        public static Quaternion Identity { get { return new Quaternion(1); } }
        public static Quaternion Zero { get { return new Quaternion(); } }
        public Quaternion(double w = 0, double x = 0, double y = 0, double z = 0)
        {
            this.w = w;
            this.x = x;
            this.y = y;
            this.z = z;
        }
        public double dotMul(Quaternion q)
        {
            return w * q.w + x * q.x + y * q.y + z * q.z;
        }
        public double norm()
        {
            return dotMul(this);
        }
        public double length()
        {
            return Math.Sqrt(norm());
        }
        public double magnitude()
        {
            return length();
        }
        public bool isZero()
        {
            return norm() < Constants.SqrEpsilon;
        }
        public void normalize()
        {
            double l = length();
            w /= l;
            x /= l;
            y /= l;
            z /= l;
        }
        public Quaternion normalized()
        {
            return this / length();
        }
        public void conjugate()
        {
            x = -x;
            y = -y;
            z = -z;
        }
        public Quaternion conjugated()
        {
            return new Quaternion(w, -x, -y, -z);
        }
        public Quaternion cross(Quaternion q)
        {
            return new Quaternion(0.0, y * q.z - z * q.y, z * q.x - x * q.z, x * q.y - y * q.x);
        }
        public bool isCollinearTo(Quaternion q)
        {
            return cross(q).isZero();
        }
        public Quaternion inverse()
        {
            return conjugated() / norm();
        }
        public void invert()
        {
            double n = norm();
            conjugate();
            w /= n;
            x /= n;
            y /= n;
            z /= n;
        }
        public static Quaternion FromAxisAngle(Vector3 axis, double angle)
        {
            if (Math.Abs(axis.squaredLength() - 1.0) > Constants.SqrEpsilon)
                throw new ArgumentException("Axis is not normalized.");
            double sinhalf = Math.Sin(angle / 2.0);
            return new Quaternion(Math.Cos(angle / 2.0), axis.x * sinhalf, axis.y * sinhalf, axis.z * sinhalf);
        }
        public static Quaternion FromEuler(Vector3 euler, EulerOrder order = EulerOrder.ZXY)
        {
            double sinhalfx = Math.Sin(euler.x / 2.0);
            double sinhalfy = Math.Sin(euler.y / 2.0);
            double sinhalfz = Math.Sin(euler.z / 2.0);
            double coshalfx = Math.Cos(euler.x / 2.0);
            double coshalfy = Math.Cos(euler.y / 2.0);
            double coshalfz = Math.Cos(euler.z / 2.0);
            switch (order)
            {
                case EulerOrder.XYZ:
                    return new Quaternion(coshalfz, 0.0, 0.0, sinhalfz) * 
                           new Quaternion(coshalfy, 0.0, sinhalfy, 0.0) *
                           new Quaternion(coshalfx, sinhalfx, 0.0, 0.0);
                case EulerOrder.XZY:
                    return new Quaternion(coshalfy, 0.0, sinhalfy, 0.0) *
                           new Quaternion(coshalfz, 0.0, 0.0, sinhalfz) *
                           new Quaternion(coshalfx, sinhalfx, 0.0, 0.0);
                case EulerOrder.YXZ:
                    return new Quaternion(coshalfz, 0.0, 0.0, sinhalfz) *
                           new Quaternion(coshalfx, sinhalfx, 0.0, 0.0) *
                           new Quaternion(coshalfy, 0.0, sinhalfy, 0.0);
                case EulerOrder.YZX:
                    return new Quaternion(coshalfx, sinhalfx, 0.0, 0.0) *
                           new Quaternion(coshalfz, 0.0, 0.0, sinhalfz) *
                           new Quaternion(coshalfy, 0.0, sinhalfy, 0.0);
                case EulerOrder.ZXY:
                    return new Quaternion(coshalfy, 0.0, sinhalfy, 0.0) *
                           new Quaternion(coshalfx, sinhalfx, 0.0, 0.0) *
                           new Quaternion(coshalfz, 0.0, 0.0, sinhalfz);
                case EulerOrder.ZYX:
                    return new Quaternion(coshalfx, sinhalfx, 0.0, 0.0) *
                           new Quaternion(coshalfy, 0.0, sinhalfy, 0.0) *
                           new Quaternion(coshalfz, 0.0, 0.0, sinhalfz);
            }
            throw new NotImplementedException();
        }

        public static Quaternion operator /(Quaternion lhs, double rhs)
        {
            return new Quaternion(lhs.w / rhs, lhs.x / rhs, lhs.y / rhs, lhs.z / rhs);
        }
        public static Quaternion operator *(Quaternion lhs, double rhs)
        {
            return new Quaternion(lhs.w * rhs, lhs.x * rhs, lhs.y * rhs, lhs.z * rhs);
        }
        public static Quaternion operator *(double lhs, Quaternion rhs)
        {
            return new Quaternion(lhs * rhs.w, lhs * rhs.w, lhs * rhs.w, lhs * rhs.w);
        }
        public static Quaternion operator *(Quaternion lhs, Quaternion rhs)
        {
            return new Quaternion(lhs.w * rhs.w - lhs.x * rhs.x - lhs.y * rhs.y - lhs.z * rhs.z,
                                  lhs.w * rhs.x + lhs.x * rhs.w + lhs.y * rhs.z - lhs.z * rhs.y,
                                  lhs.w * rhs.y - lhs.x * rhs.z + lhs.y * rhs.w + lhs.z * rhs.x,
                                  lhs.w * rhs.z + lhs.x * rhs.y - lhs.y * rhs.x + lhs.z * rhs.w);
        }
        public static Vector3 operator *(Quaternion lhs, Vector3 rhs)
        {
            Quaternion q = lhs * new Quaternion(0.0, rhs.x, rhs.y, rhs.z) * lhs.inverse();
            return new Vector3(q.x, q.y, q.z);
        }
        public static Quaternion operator +(Quaternion lhs, Quaternion rhs)
        {
            return new Quaternion(lhs.w + rhs.w, lhs.x + rhs.x, lhs.y + rhs.y, lhs.z + rhs.z);
        }
        public static Quaternion operator -(Quaternion lhs, Quaternion rhs)
        {
            return new Quaternion(lhs.w - rhs.w, lhs.x - rhs.x, lhs.y - rhs.y, lhs.z - rhs.z);
        }

        //public static Quaternion operator *(Quaternion q, Vector3 v)
        //{
        //    return new Quaternion(-q.x * v.x - q.y - v.y - q.z * v.z,
        //                          q.w * v.x + q.y * v.z - q.z * v.y,
        //                          q.w * v.y + q.x * v.z - q.z * v.x,
        //                          q.w * v.z + q.x * v.y - q.y * v.x);
        //}
        ///// <summary>
        ///// Rotates vector v by quaternion q
        ///// </summary>
        //public Vector3 rotateVector(Vector3 vec)
        //{
        //    Quaternion result = this * vec * inversed();
        //    return new Vector3(result.x, result.y, result.z);
        //}
        ///// <summary>
        ///// Returns eulers representation of rotation in this quaternion
        ///// </summary>
        //public Vector3 toEuler()
        //{
        //    if (x * y + z * w == 0.5)
        //        return new Vector3(Math.Asin(2 * x * y + 2 * z * w), 2 * Math.Atan2(x, w), 0);
        //    else
        //    if (x * y + z * w == -0.5)
        //        return new Vector3(Math.Asin(2 * x * y + 2 * z * w), -2 * Math.Atan2(x, w), 0);
        //    else
        //        return new Vector3(Math.Asin(2 * x * y + 2 * z * w), Math.Atan2(2 * y * w - 2 * x * z, 1 - 2 * y * y - 2 * z * z), Math.Atan2(2 * x * w - 2 * y * z, 1 - 2 * x * x - 2 * z * z));
        //}
        public override string ToString()
        {
            return "(" + w.ToString() + ", " + x.ToString() + ", " + y.ToString() + ", " + z.ToString() + ")";
        }
        public string ToString(string format)
        {
            return "(" + w.ToString(format) + ", " + x.ToString(format) + ", " + y.ToString(format) + ", " + z.ToString(format) + ")";
        }
    }
}