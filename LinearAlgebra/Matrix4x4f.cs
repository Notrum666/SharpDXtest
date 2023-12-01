using System;

namespace LinearAlgebra
{
    /// <summary>
    /// 4 by 4 matrix with single precision in row-major order
    /// </summary>
    public struct Matrix4x4f
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

        /// <summary>
        /// Returns new identity matrix
        /// </summary>
        public static Matrix4x4f Identity => new Matrix4x4f(1f, 0f, 0f, 0f,
                                                            0f, 1f, 0f, 0f,
                                                            0f, 0f, 1f, 0f,
                                                            0f, 0f, 0f, 1f);

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

        public Matrix4x4f(in Vector4f vec1, in Vector4f vec2, in Vector4f vec3, in Vector4f vec4, bool rows = true)
        {
            if (rows)
            {
                v00 = vec1.x;
                v01 = vec1.y;
                v02 = vec1.z;
                v03 = vec1.w;
                v10 = vec2.x;
                v11 = vec2.y;
                v12 = vec2.z;
                v13 = vec2.w;
                v20 = vec3.x;
                v21 = vec3.y;
                v22 = vec3.z;
                v23 = vec3.w;
                v30 = vec4.x;
                v31 = vec4.y;
                v32 = vec4.z;
                v33 = vec4.w;
            }
            else
            {
                v00 = vec1.x;
                v01 = vec2.x;
                v02 = vec3.x;
                v03 = vec4.x;
                v10 = vec1.y;
                v11 = vec2.y;
                v12 = vec3.y;
                v13 = vec4.y;
                v20 = vec1.z;
                v21 = vec2.z;
                v22 = vec3.z;
                v23 = vec4.z;
                v30 = vec1.w;
                v31 = vec2.w;
                v32 = vec3.w;
                v33 = vec4.w;
            }
        }

        public static explicit operator Matrix4x4f(in Matrix4x4 mat)
        {
            return new Matrix4x4f((float)mat.v00, (float)mat.v01, (float)mat.v02, (float)mat.v03,
                                  (float)mat.v10, (float)mat.v11, (float)mat.v12, (float)mat.v13,
                                  (float)mat.v20, (float)mat.v21, (float)mat.v22, (float)mat.v23,
                                  (float)mat.v30, (float)mat.v31, (float)mat.v32, (float)mat.v33);
        }

        public static Matrix4x4f operator *(in Matrix4x4f m1, in Matrix4x4f m2)
        {
            return new Matrix4x4f(m1.v00 * m2.v00 + m1.v01 * m2.v10 + m1.v02 * m2.v20 + m1.v03 * m2.v30,
                                  m1.v00 * m2.v01 + m1.v01 * m2.v11 + m1.v02 * m2.v21 + m1.v03 * m2.v31,
                                  m1.v00 * m2.v02 + m1.v01 * m2.v12 + m1.v02 * m2.v22 + m1.v03 * m2.v32,
                                  m1.v00 * m2.v03 + m1.v01 * m2.v13 + m1.v02 * m2.v23 + m1.v03 * m2.v33,
                                  m1.v10 * m2.v00 + m1.v11 * m2.v10 + m1.v12 * m2.v20 + m1.v13 * m2.v30,
                                  m1.v10 * m2.v01 + m1.v11 * m2.v11 + m1.v12 * m2.v21 + m1.v13 * m2.v31,
                                  m1.v10 * m2.v02 + m1.v11 * m2.v12 + m1.v12 * m2.v22 + m1.v13 * m2.v32,
                                  m1.v10 * m2.v03 + m1.v11 * m2.v13 + m1.v12 * m2.v23 + m1.v13 * m2.v33,
                                  m1.v20 * m2.v00 + m1.v21 * m2.v10 + m1.v22 * m2.v20 + m1.v23 * m2.v30,
                                  m1.v20 * m2.v01 + m1.v21 * m2.v11 + m1.v22 * m2.v21 + m1.v23 * m2.v31,
                                  m1.v20 * m2.v02 + m1.v21 * m2.v12 + m1.v22 * m2.v22 + m1.v23 * m2.v32,
                                  m1.v20 * m2.v03 + m1.v21 * m2.v13 + m1.v22 * m2.v23 + m1.v23 * m2.v33,
                                  m1.v30 * m2.v00 + m1.v31 * m2.v10 + m1.v32 * m2.v20 + m1.v33 * m2.v30,
                                  m1.v30 * m2.v01 + m1.v31 * m2.v11 + m1.v32 * m2.v21 + m1.v33 * m2.v31,
                                  m1.v30 * m2.v02 + m1.v31 * m2.v12 + m1.v32 * m2.v22 + m1.v33 * m2.v32,
                                  m1.v30 * m2.v03 + m1.v31 * m2.v13 + m1.v32 * m2.v23 + m1.v33 * m2.v33);
        }

        public static Matrix4x4f operator +(in Matrix4x4f lhs, in Matrix4x4f rhs)
        {
            return new Matrix4x4f(lhs.v00 + rhs.v00, lhs.v01 + rhs.v01, lhs.v02 + rhs.v02, lhs.v03 + rhs.v03,
                                  lhs.v10 + rhs.v10, lhs.v11 + rhs.v11, lhs.v12 + rhs.v12, lhs.v13 + rhs.v13,
                                  lhs.v20 + rhs.v20, lhs.v21 + rhs.v21, lhs.v22 + rhs.v22, lhs.v23 + rhs.v23,
                                  lhs.v30 + rhs.v30, lhs.v31 + rhs.v31, lhs.v32 + rhs.v32, lhs.v33 + rhs.v33);
        }

        public static Matrix4x4f operator -(in Matrix4x4f lhs, in Matrix4x4f rhs)
        {
            return new Matrix4x4f(lhs.v00 - rhs.v00, lhs.v01 - rhs.v01, lhs.v02 - rhs.v02, lhs.v03 - rhs.v03,
                                  lhs.v10 - rhs.v10, lhs.v11 - rhs.v11, lhs.v12 - rhs.v12, lhs.v13 - rhs.v13,
                                  lhs.v20 - rhs.v20, lhs.v21 - rhs.v21, lhs.v22 - rhs.v22, lhs.v23 - rhs.v23,
                                  lhs.v30 - rhs.v30, lhs.v31 - rhs.v31, lhs.v32 - rhs.v32, lhs.v33 - rhs.v33);
        }

        public static Matrix4x4f operator *(in Matrix4x4f mat, float value)
        {
            return new Matrix4x4f(mat.v00 * value, mat.v01 * value, mat.v02 * value, mat.v03 * value,
                                  mat.v10 * value, mat.v11 * value, mat.v12 * value, mat.v13 * value,
                                  mat.v20 * value, mat.v21 * value, mat.v22 * value, mat.v23 * value,
                                  mat.v30 * value, mat.v31 * value, mat.v32 * value, mat.v33 * value);
        }

        public static Matrix4x4f operator *(float value, in Matrix4x4f mat)
        {
            return mat * value;
        }

        public static Matrix4x4f operator /(in Matrix4x4f mat, float value)
        {
            if (value == 0)
                throw new DivideByZeroException();
            return mat * (1.0f / value);
        }

        public static Vector4f operator *(in Matrix4x4f mat, in Vector4f vec)
        {
            return new Vector4f(mat.v00 * vec.x + mat.v01 * vec.y + mat.v02 * vec.z + mat.v03 * vec.w,
                                mat.v10 * vec.x + mat.v11 * vec.y + mat.v12 * vec.z + mat.v13 * vec.w,
                                mat.v20 * vec.x + mat.v21 * vec.y + mat.v22 * vec.z + mat.v23 * vec.w,
                                mat.v30 * vec.x + mat.v31 * vec.y + mat.v32 * vec.z + mat.v33 * vec.w);
        }

        public static Vector4f operator *(in Vector4f vec, in Matrix4x4f mat)
        {
            return new Vector4f(mat.v00 * vec.x + mat.v10 * vec.y + mat.v20 * vec.z + mat.v30 * vec.w,
                                mat.v01 * vec.x + mat.v11 * vec.y + mat.v21 * vec.z + mat.v31 * vec.w,
                                mat.v02 * vec.x + mat.v12 * vec.y + mat.v22 * vec.z + mat.v32 * vec.w,
                                mat.v03 * vec.x + mat.v13 * vec.y + mat.v23 * vec.z + mat.v33 * vec.w);
        }

        public Vector3f TransformPoint(in Vector3f point)
        {
            float invW = 1.0f / (v30 * point.x + v31 * point.y + v32 * point.z + v33);
            return new Vector3f((v00 * point.x + v01 * point.y + v02 * point.z + v03) * invW,
                                (v10 * point.x + v11 * point.y + v12 * point.z + v13) * invW,
                                (v20 * point.x + v21 * point.y + v22 * point.z + v23) * invW);
        }

        public Vector3f TransformDirection(in Vector3f dir)
        {
            return new Vector3f(v00 * dir.x + v01 * dir.y + v02 * dir.z,
                                v10 * dir.x + v11 * dir.y + v12 * dir.z,
                                v20 * dir.x + v21 * dir.y + v22 * dir.z);
        }

        /// <summary>
        /// Returns transposed copy of this matrix
        /// </summary>
        public Matrix4x4f transposed()
        {
            return new Matrix4x4f(v00, v10, v20, v30,
                                  v01, v11, v21, v31,
                                  v02, v12, v22, v32,
                                  v03, v13, v23, v33);
        }

        /// <summary>
        /// Transposes this matrix
        /// </summary>
        public void transpose()
        {
            float tmp = v10;
            v10 = v01;
            v01 = tmp;

            tmp = v20;
            v20 = v02;
            v02 = tmp;

            tmp = v21;
            v21 = v12;
            v12 = tmp;

            tmp = v30;
            v30 = v03;
            v03 = tmp;

            tmp = v31;
            v31 = v13;
            v13 = tmp;

            tmp = v32;
            v32 = v23;
            v23 = tmp;
        }

        /// <summary>
        /// Returns inverse copy of this matrix
        /// </summary>
        public Matrix4x4f inverse()
        {
            float det00 = v11 * (v22 * v33 - v23 * v32) - v12 * (v21 * v33 - v23 * v31) + v13 * (v21 * v32 - v22 * v31);
            float det01 = v10 * (v22 * v33 - v23 * v32) - v12 * (v20 * v33 - v23 * v30) + v13 * (v20 * v32 - v22 * v30);
            float det02 = v10 * (v21 * v33 - v23 * v31) - v11 * (v20 * v33 - v23 * v30) + v13 * (v20 * v31 - v21 * v30);
            float det03 = v10 * (v21 * v32 - v22 * v31) - v11 * (v20 * v32 - v22 * v30) + v12 * (v20 * v31 - v21 * v30);

            float determinant = v00 * det00 - v01 * det01 + v02 * det02 - v03 * det03;
            if (determinant == 0)
                throw new Exception("This matrix is singular. (determinant = 0)");

            float det10 = v01 * (v22 * v33 - v23 * v32) - v02 * (v21 * v33 - v23 * v31) + v03 * (v21 * v32 - v22 * v31);
            float det11 = v00 * (v22 * v33 - v23 * v32) - v02 * (v20 * v33 - v23 * v30) + v03 * (v20 * v32 - v22 * v30);
            float det12 = v00 * (v21 * v33 - v23 * v31) - v01 * (v20 * v33 - v23 * v30) + v03 * (v20 * v31 - v21 * v30);
            float det13 = v00 * (v21 * v32 - v22 * v31) - v01 * (v20 * v32 - v22 * v30) + v02 * (v20 * v31 - v21 * v30);
            float det20 = v01 * (v12 * v33 - v13 * v32) - v02 * (v11 * v33 - v13 * v31) + v03 * (v11 * v32 - v12 * v31);
            float det21 = v00 * (v12 * v33 - v13 * v32) - v02 * (v10 * v33 - v13 * v30) + v03 * (v10 * v32 - v12 * v30);
            float det22 = v00 * (v11 * v33 - v13 * v31) - v01 * (v10 * v33 - v13 * v30) + v03 * (v10 * v31 - v11 * v30);
            float det23 = v00 * (v11 * v32 - v12 * v31) - v01 * (v10 * v32 - v12 * v30) + v02 * (v10 * v31 - v11 * v30);
            float det30 = v01 * (v12 * v23 - v13 * v22) - v02 * (v11 * v23 - v13 * v21) + v03 * (v11 * v22 - v12 * v21);
            float det31 = v00 * (v12 * v23 - v13 * v22) - v02 * (v10 * v23 - v13 * v20) + v03 * (v10 * v22 - v12 * v20);
            float det32 = v00 * (v11 * v23 - v13 * v21) - v01 * (v10 * v23 - v13 * v20) + v03 * (v10 * v21 - v11 * v20);
            float det33 = v00 * (v11 * v22 - v12 * v21) - v01 * (v10 * v22 - v12 * v20) + v02 * (v10 * v21 - v11 * v20);

            return new Matrix4x4f(det00, -det10, det20, -det30,
                                  -det01, det11, -det21, det31,
                                  det02, -det12, det22, -det32,
                                  -det03, det13, -det23, det33) / determinant;
        }

        /// <summary>
        /// Inverts this matrix
        /// </summary>
        public void invert()
        {
            float det00 = v11 * (v22 * v33 - v23 * v32) - v12 * (v21 * v33 - v23 * v31) + v13 * (v21 * v32 - v22 * v31);
            float det01 = v10 * (v22 * v33 - v23 * v32) - v12 * (v20 * v33 - v23 * v30) + v13 * (v20 * v32 - v22 * v30);
            float det02 = v10 * (v21 * v33 - v23 * v31) - v11 * (v20 * v33 - v23 * v30) + v13 * (v20 * v31 - v21 * v30);
            float det03 = v10 * (v21 * v32 - v22 * v31) - v11 * (v20 * v32 - v22 * v30) + v12 * (v20 * v31 - v21 * v30);

            float determinant = v00 * det00 - v01 * det01 + v02 * det02 - v03 * det03;
            if (determinant == 0)
                throw new Exception("This matrix is singular. (determinant = 0)");

            float det10 = v01 * (v22 * v33 - v23 * v32) - v02 * (v21 * v33 - v23 * v31) + v03 * (v21 * v32 - v22 * v31);
            float det11 = v00 * (v22 * v33 - v23 * v32) - v02 * (v20 * v33 - v23 * v30) + v03 * (v20 * v32 - v22 * v30);
            float det12 = v00 * (v21 * v33 - v23 * v31) - v01 * (v20 * v33 - v23 * v30) + v03 * (v20 * v31 - v21 * v30);
            float det13 = v00 * (v21 * v32 - v22 * v31) - v01 * (v20 * v32 - v22 * v30) + v02 * (v20 * v31 - v21 * v30);
            float det20 = v01 * (v12 * v33 - v13 * v32) - v02 * (v11 * v33 - v13 * v31) + v03 * (v11 * v32 - v12 * v31);
            float det21 = v00 * (v12 * v33 - v13 * v32) - v02 * (v10 * v33 - v13 * v30) + v03 * (v10 * v32 - v12 * v30);
            float det22 = v00 * (v11 * v33 - v13 * v31) - v01 * (v10 * v33 - v13 * v30) + v03 * (v10 * v31 - v11 * v30);
            float det23 = v00 * (v11 * v32 - v12 * v31) - v01 * (v10 * v32 - v12 * v30) + v02 * (v10 * v31 - v11 * v30);
            float det30 = v01 * (v12 * v23 - v13 * v22) - v02 * (v11 * v23 - v13 * v21) + v03 * (v11 * v22 - v12 * v21);
            float det31 = v00 * (v12 * v23 - v13 * v22) - v02 * (v10 * v23 - v13 * v20) + v03 * (v10 * v22 - v12 * v20);
            float det32 = v00 * (v11 * v23 - v13 * v21) - v01 * (v10 * v23 - v13 * v20) + v03 * (v10 * v21 - v11 * v20);
            float det33 = v00 * (v11 * v22 - v12 * v21) - v01 * (v10 * v22 - v12 * v20) + v02 * (v10 * v21 - v11 * v20);

            determinant = 1.0f / determinant;

            v00 = det00 * determinant;
            v01 = -det10 * determinant;
            v02 = det20 * determinant;
            v03 = -det30 * determinant;

            v10 = -det01 * determinant;
            v11 = det11 * determinant;
            v12 = -det21 * determinant;
            v13 = det31 * determinant;

            v20 = det02 * determinant;
            v21 = -det12 * determinant;
            v22 = det22 * determinant;
            v23 = -det32 * determinant;

            v30 = -det03 * determinant;
            v31 = det13 * determinant;
            v32 = -det23 * determinant;
            v33 = det33 * determinant;
        }

        public static Matrix4x4f FromQuaternion(in Quaternion q)
        {
            float s2 = 2.0f / (float)q.norm();
            float x = (float)q.x;
            float y = (float)q.y;
            float z = (float)q.z;
            float w = (float)q.w;
            return new Matrix4x4f(1 - s2 * (y * y + z * z), s2 * (x * y - w * z), s2 * (x * z + w * y), 0,
                                  s2 * (x * y + w * z), 1 - s2 * (x * x + z * z), s2 * (y * z - w * x), 0,
                                  s2 * (x * z - w * y), s2 * (y * z + w * x), 1 - s2 * (x * x + y * y), 0,
                                  0, 0, 0, 1);
        }

        public bool IsIdentity()
        {
            if (Math.Abs(v00 - 1.0f) > Constants.FloatEpsilon || Math.Abs(v01) > Constants.FloatEpsilon || Math.Abs(v02) > Constants.FloatEpsilon || Math.Abs(v03) > Constants.FloatEpsilon ||
                Math.Abs(v10) > Constants.FloatEpsilon || Math.Abs(v11 - 1.0f) > Constants.FloatEpsilon || Math.Abs(v12) > Constants.FloatEpsilon || Math.Abs(v13) > Constants.FloatEpsilon ||
                Math.Abs(v20) > Constants.FloatEpsilon || Math.Abs(v21) > Constants.FloatEpsilon || Math.Abs(v22 - 1.0f) > Constants.FloatEpsilon || Math.Abs(v23) > Constants.FloatEpsilon ||
                Math.Abs(v30) > Constants.FloatEpsilon || Math.Abs(v31) > Constants.FloatEpsilon || Math.Abs(v32) > Constants.FloatEpsilon || Math.Abs(v33 - 1.0f) > Constants.FloatEpsilon)
                return false;
            return true;
        }

        public bool IsZero()
        {
            if (Math.Abs(v00) > Constants.FloatEpsilon || Math.Abs(v01) > Constants.FloatEpsilon || Math.Abs(v02) > Constants.FloatEpsilon || Math.Abs(v03) > Constants.FloatEpsilon ||
                Math.Abs(v10) > Constants.FloatEpsilon || Math.Abs(v11) > Constants.FloatEpsilon || Math.Abs(v12) > Constants.FloatEpsilon || Math.Abs(v13) > Constants.FloatEpsilon ||
                Math.Abs(v20) > Constants.FloatEpsilon || Math.Abs(v21) > Constants.FloatEpsilon || Math.Abs(v22) > Constants.FloatEpsilon || Math.Abs(v23) > Constants.FloatEpsilon ||
                Math.Abs(v30) > Constants.FloatEpsilon || Math.Abs(v31) > Constants.FloatEpsilon || Math.Abs(v32) > Constants.FloatEpsilon || Math.Abs(v33) > Constants.FloatEpsilon)
                return false;
            return true;
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
}