using System;

namespace SharpDXtest
{
    public static class Constants
    {
        public static double Epsilon = 1e-6;
    }
    public struct Vector2f
    {
        // TODO: fill struct
        public float x { get; set; }
        public float y { get; set; }
        public Vector2f(float x = 0, float y = 0)
        {
            this.x = x;
            this.y = y;
        }
        public static implicit operator Vector3f(Vector2f vec) => new Vector3f(vec);
    }
    public struct Vector2
    {
        // TODO: fill struct
        public double x { get; set; }
        public double y { get; set; }
        public Vector2(double x = 0, double y = 0)
        {
            this.x = x;
            this.y = y;
        }
        public static implicit operator Vector3(Vector2 vec) => new Vector3(vec);
    }
    public struct Vector3f
    {
        public static Vector3f zero { get { return new Vector3f(); } }

        // x - right, y - forward, z - up
        public static Vector3f right { get { return new Vector3f(1, 0, 0); } }
        public static Vector3f left { get { return new Vector3f(-1, 0, 0); } }
        public static Vector3f forward { get { return new Vector3f(0, 1, 0); } }
        public static Vector3f back { get { return new Vector3f(0, -1, 0); } }
        public static Vector3f up { get { return new Vector3f(0, 0, 1); } }
        public static Vector3f down { get { return new Vector3f(0, 0, -1); } }

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
            return scalMul(this);
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
            return squaredMagnitude() < Constants.Epsilon;
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
        /// <summary>
        /// Scalar multiplication
        /// </summary>
        public float scalMul(Vector3f vec)
        {
            return x * vec.x + y * vec.y + z * vec.z;
        }
        /// <summary>
        /// Scalar multiplication
        /// </summary>
        public static float operator *(Vector3f v1, Vector3f v2)
        {
            return v1.scalMul(v2);
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
                return Vector3f.zero;
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
    }
    public struct Vector3
    {
        public static Vector3 zero { get { return new Vector3(); } }

        // x - right, y - forward, z - up
        public static Vector3 right { get { return new Vector3(1, 0, 0); } }
        public static Vector3 left { get { return new Vector3(-1, 0, 0); } }
        public static Vector3 forward { get { return new Vector3(0, 1, 0); } }
        public static Vector3 back { get { return new Vector3(0, -1, 0); } }
        public static Vector3 up { get { return new Vector3(0, 0, 1); } }
        public static Vector3 down { get { return new Vector3(0, 0, -1); } }

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
            return scalMul(this);
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
            return squaredMagnitude() < Constants.Epsilon;
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
        /// <summary>
        /// Scalar multiplication
        /// </summary>
        public double scalMul(Vector3 vec)
        {
            return x * vec.x + y * vec.y + z * vec.z;
        }
        /// <summary>
        /// Scalar multiplication
        /// </summary>
        public static double operator *(Vector3 v1, Vector3 v2)
        {
            return v1.scalMul(v2);
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
                return Vector3.zero;
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
    }
    public struct Matrix4f
    {
        public float v00; // [rowIndex, columnIndex]
        public float v01;
        public float v02;
        public float v03;
        public float v10;
        public float v11;
        public float v12;
        public float v13;
        public float v20;
        public float v21;
        public float v22;
        public float v23;
        public float v30;
        public float v31;
        public float v32;
        public float v33;
        public static Matrix4f Identity { get { return new Matrix4f(new float[] { 1, 0, 0, 0, 0, 1, 0, 0, 0, 0, 1, 0, 0, 0, 0, 1 }); } }
        public Matrix4f(float[] values)
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
    }
    public struct Matrix4
    {
        public double v00; // [rowIndex, columnIndex]
        public double v01;
        public double v02;
        public double v03;
        public double v10;
        public double v11;
        public double v12;
        public double v13;
        public double v20;
        public double v21;
        public double v22;
        public double v23;
        public double v30;
        public double v31;
        public double v32;
        public double v33;

        public static Matrix4 Identity { get { return new Matrix4(new double[] { 1, 0, 0, 0, 0, 1, 0, 0, 0, 0, 1, 0, 0, 0, 0, 1 }); } }
        public Matrix4(double[] values)
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
        public Matrix4 transposed()
        {
            return new Matrix4(new double[] { v00, v10, v20, v30,
                                              v01, v11, v21, v31,
                                              v02, v12, v22, v32,
                                              v03, v13, v32, v33 });
        }
        public static Matrix4 operator *(Matrix4 m1, Matrix4 m2)
        {
            return new Matrix4(new double[] { m1.v00*m2.v00+m1.v01*m2.v10+m1.v02*m2.v20+m1.v03*m2.v30,
                                              m1.v00*m2.v01+m1.v01*m2.v11+m1.v02*m2.v21+m1.v03*m2.v31,
                                              m1.v00*m2.v02+m1.v01*m2.v12+m1.v02*m2.v22+m1.v03*m2.v32,
                                              m1.v00*m2.v03+m1.v01*m2.v13+m1.v02*m2.v23+m1.v03*m2.v33,

                                              m1.v10*m2.v00+m1.v11*m2.v10+m1.v12*m2.v20+m1.v13*m2.v30,
                                              m1.v10*m2.v01+m1.v11*m2.v11+m1.v12*m2.v21+m1.v13*m2.v31,
                                              m1.v10*m2.v02+m1.v11*m2.v12+m1.v12*m2.v22+m1.v13*m2.v32,
                                              m1.v10*m2.v03+m1.v11*m2.v13+m1.v12*m2.v23+m1.v13*m2.v33,

                                              m1.v20*m2.v00+m1.v21*m2.v10+m1.v22*m2.v20+m1.v23*m2.v30,
                                              m1.v20*m2.v01+m1.v21*m2.v11+m1.v22*m2.v21+m1.v23*m2.v31,
                                              m1.v20*m2.v02+m1.v21*m2.v12+m1.v22*m2.v22+m1.v23*m2.v32,
                                              m1.v20*m2.v03+m1.v21*m2.v13+m1.v22*m2.v23+m1.v23*m2.v33,

                                              m1.v30*m2.v00+m1.v31*m2.v10+m1.v32*m2.v20+m1.v33*m2.v30,
                                              m1.v30*m2.v01+m1.v31*m2.v11+m1.v32*m2.v21+m1.v33*m2.v31,
                                              m1.v30*m2.v02+m1.v31*m2.v12+m1.v32*m2.v22+m1.v33*m2.v32,
                                              m1.v30*m2.v03+m1.v31*m2.v13+m1.v32*m2.v23+m1.v33*m2.v33 });
        }
        public static Matrix4 operator *(Matrix4 mat, double value)
        {
            return new Matrix4(new double[] { mat.v00 * value, mat.v01 * value, mat.v02 * value, mat.v03 * value,
                                              mat.v10 * value, mat.v11 * value, mat.v12 * value, mat.v13 * value,
                                              mat.v20 * value, mat.v21 * value, mat.v22 * value, mat.v23 * value,
                                              mat.v30 * value, mat.v31 * value, mat.v32 * value, mat.v33 * value });
        }
        public static Matrix4 operator /(Matrix4 mat, double value)
        {
            if (value == 0)
                throw new DivideByZeroException();
            return new Matrix4(new double[] { mat.v00 / value, mat.v01 / value, mat.v02 / value, mat.v03 / value,
                                              mat.v10 / value, mat.v11 / value, mat.v12 / value, mat.v13 / value,
                                              mat.v20 / value, mat.v21 / value, mat.v22 / value, mat.v23 / value,
                                              mat.v30 / value, mat.v31 / value, mat.v32 / value, mat.v33 / value });
        }
        /// <summary>
        /// Multiplies matrix by vector, where vector represents point in space (vector = (x, y, z, 1))
        /// </summary>
        public Vector3 multByPoint(Vector3 vec)
        {
            return new Vector3(v00 * vec.x + v01 * vec.y + v02 * vec.z + v03,
                               v10 * vec.x + v11 * vec.y + v12 * vec.z + v13,
                               v20 * vec.x + v21 * vec.y + v22 * vec.z + v23);
        }
        /// <summary>
        /// Multiplies matrix by vector, where vector represents direction (vector = (x, y, z, 0))
        /// </summary>
        public Vector3 multByDirection(Vector3 vec)
        {
            return new Vector3(v00 * vec.x + v01 * vec.y + v02 * vec.z,
                               v10 * vec.x + v11 * vec.y + v12 * vec.z,
                               v20 * vec.x + v21 * vec.y + v22 * vec.z);
        }
        public Matrix4 inversed()
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

            return new Matrix4(new double[] {  det00, -det10,  det20, -det30,
                                              -det01,  det11, -det21,  det31,
                                               det02, -det12,  det22, -det32,
                                              -det03,  det13, -det23,  det33 }) / determinant;
        }
        public override string ToString()
        {
            return "| " + v00.ToString() + " " + v01.ToString() + " " + v02.ToString() + " " + v03.ToString() + " |\n" +
                   "| " + v10.ToString() + " " + v11.ToString() + " " + v12.ToString() + " " + v13.ToString() + " |\n" +
                   "| " + v20.ToString() + " " + v21.ToString() + " " + v22.ToString() + " " + v23.ToString() + " |\n" +
                   "| " + v30.ToString() + " " + v31.ToString() + " " + v32.ToString() + " " + v33.ToString() + " |";
        }
    }
    // do NOT ask me about this, i don't give a fuck how this works.
    public struct Quaternion
    {
        private double w, x, y, z;

        public static Quaternion Identity { get { return new Quaternion(1); } }
        public Quaternion(double w = 0, double x = 0, double y = 0, double z = 0)
        {
            this.w = w;
            this.x = x;
            this.y = y;
            this.z = z;
        }
        /// <summary>
        /// Normalizes this quaternion
        /// </summary>
        public void normalize()
        {
            double length = magnitude();
            w /= length;
            x /= length;
            y /= length;
            z /= length;
        }
        /// <summary>
        /// Returns normalized copy of this quaternion
        /// </summary>
        public Quaternion normalized()
        {
            double length = magnitude();
            return new Quaternion(w / length, x / length, y / length, z / length);
        }
        /// <summary>
        /// Returns magnitude of this quaternion, equal to length()
        /// </summary>
        public double magnitude()
        {
            return Math.Sqrt(w * w + x * x + y * y + z * z);
        }
        /// <summary>
        /// Returns length of this quaternion, equal to magnitude()
        /// </summary>
        public double length()
        {
            return magnitude();
        }
        /// <summary>
        /// returns squared magnitude of this quaternion, equal to squaredLength()
        /// </summary>
        public double squaredMagnitude()
        {
            return w * w + x * x + y * y + z * z;
        }
        /// <summary>
        /// returns squared length of this quaternion, equal to squaredMagnitude()
        /// </summary>
        public double squaredLength()
        {
            return squaredMagnitude();
        }
        /// <summary>
        /// Returns inversed copy of this quaternion (conjugated/squaredMagnitude)
        /// </summary>
        public Quaternion inversed()
        {
            Quaternion q = conjugated();
            double magn = squaredMagnitude();
            q.w /= magn;
            q.x /= magn;
            q.y /= magn;
            q.z /= magn;
            return q;
        }
        /// <summary>
        /// Inverses this quaternion (conjugated/squaredMagnitude)
        /// </summary>
        public void inverse()
        {
            double magn = squaredMagnitude();
            conjugate();
            w /= magn;
            x /= magn;
            y /= magn;
            z /= magn;
        }
        /// <summary>
        /// Conjugates this vector (w, -x, -y, -z)
        /// </summary>
        public void conjugate()
        {
            x = -x;
            y = -y;
            z = -z;
        }
        /// <summary>
        /// Returns conjugated copy of this quaternion (w, -x, -y, -z)
        /// </summary>
        public Quaternion conjugated()
        {
            return new Quaternion(w, -x, -y, -z);
        }
        /// <summary>
        /// Combines 2 rotations. Important: Second rotation comes first.
        /// </summary>
        public static Quaternion operator *(Quaternion q1, Quaternion q2)
        {
            return new Quaternion(q1.w * q2.w - q1.x * q2.x - q1.y - q2.y - q1.z * q2.z,
                                  q1.w * q2.x + q1.x * q2.w + q1.y * q2.z - q1.z * q2.y,
                                  q1.w * q2.y + q1.y * q2.w + q1.x * q2.z - q1.z * q2.x,
                                  q1.w * q2.z + q1.z * q2.w + q1.x * q2.y - q1.y * q2.x);
        }
        public static Quaternion operator *(Quaternion q, Vector3 v)
        {
            return new Quaternion(-q.x * v.x - q.y - v.y - q.z * v.z,
                                  q.w * v.x + q.y * v.z - q.z * v.y,
                                  q.w * v.y + q.x * v.z - q.z * v.x,
                                  q.w * v.z + q.x * v.y - q.y * v.x);
        }
        /// <summary>
        /// Rotates vector v by quaternion q
        /// </summary>
        public Vector3 rotateVector(Vector3 vec)
        {
            Quaternion result = this * vec * inversed();
            return new Vector3(result.x, result.y, result.z);
        }
        /// <summary>
        /// Returns rotation matrix equal to this quaternion
        /// </summary>
        public Matrix4 toRotationMatrix()
        {
            return new Matrix4(new double[] { 1-2*(y*y+z*z), 2*(x*y-w*z), 2*(x*z+w*y), 0,
                                              2*(x*y+w*z), 1-2*(x*x+z*z), 2*(y*z-w*x), 0,
                                              2*(x*z-w*y), 2*(y*z+w*x), 1-2*(x*x+y*y), 0,
                                              0, 0, 0, 1 });
        }
        /// <summary>
        /// Returns quaternion represented by rotation around axis
        /// </summary>
        public static Quaternion FromAxisAngle(Vector3 axis, double angle)
        {
            double cos = Math.Cos(angle / 2.0);
            double sin = Math.Sin(angle / 2.0);
            return new Quaternion(cos, sin * axis.x, sin * axis.y, sin * axis.z);
        }
        /// <summary>
        /// Returns quaternion representation of rotation in eulers. Order: YXZ
        /// </summary>
        public static Quaternion fromEuler(Vector3 eulers)
        {
            return FromAxisAngle(Vector3.forward, eulers.z) *
                   FromAxisAngle(Vector3.right, eulers.x) *
                   FromAxisAngle(Vector3.up, eulers.y);
        }
        /// <summary>
        /// Returns eulers representation of rotation in this quaternion
        /// </summary>
        public Vector3 toEuler()
        {
            if (x * y + z * w == 0.5)
                return new Vector3(Math.Asin(2 * x * y + 2 * z * w), 2 * Math.Atan2(x, w), 0);
            else
            if (x * y + z * w == -0.5)
                return new Vector3(Math.Asin(2 * x * y + 2 * z * w), -2 * Math.Atan2(x, w), 0);
            else
                return new Vector3(Math.Asin(2 * x * y + 2 * z * w), Math.Atan2(2 * y * w - 2 * x * z, 1 - 2 * y * y - 2 * z * z), Math.Atan2(2 * x * w - 2 * y * z, 1 - 2 * x * x - 2 * z * z));
        }
        public override string ToString()
        {
            return "(" + w.ToString() + ", " + x.ToString() + ", " + y.ToString() + ", " + z.ToString() + ")";
        }
    }
}