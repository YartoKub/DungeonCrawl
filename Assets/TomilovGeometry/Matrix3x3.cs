using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
public struct Matrix3x3
{
    public Vector3 p0; public Vector3 p1; public Vector3 p2;
    public float det;

    public Matrix3x3(Vector3 _p0, Vector3 _p1, Vector3 _p2) {
        p0 = _p0;
        p1 = _p1;
        p2 = _p2;
        det = Matrix3x3.Det3x3(_p0, _p1, _p2);
    }

    // determinant of a 3x3 matrix / определитель
    // 0x 0y 0z
    // 1x 1y 1z -> Det
    // 2x 2y 2z
    public float Det3x3()
    {
        float MD = (p0.x * p1.y * p2.z) + (p0.y * p1.z * p2.x) + (p0.z * p1.x * p2.y);
        float OD = (p0.z * p1.y * p2.x) + (p0.y * p1.x * p2.z) + (p0.x * p1.z * p2.y);
        float ret = MD - OD;
        return ret;
    }

    public static float Det3x3(Vector3 vx, Vector3 vy, Vector3 vz)
    {
        float MD = (vx.x * vy.y * vz.z) + (vx.y * vy.z * vz.x) + (vx.z * vy.x * vz.y);
        float OD = (vx.z * vy.y * vz.x) + (vx.y * vy.x * vz.z) + (vx.x * vy.z * vz.y);
        float ret = MD - OD;
        return ret;
    }

    public static float Det3x3(Matrix3x3 m) // хз зачем, пусть будет
    {
        return m.Det3x3();
    }
    // Adjacent of a 3x3 matrix / Союзная матрица
    // 0x 0y 0z
    // 1x 1y 1z -> 
    // 2x 2y 2z
    public Matrix3x3 Adj3x3()
    {
        Vector3 adj0 = new Vector3(Geo3D.Det2x2(p1.y, p1.z, p2.y, p2.z), -1 * Geo3D.Det2x2(p1.x, p1.z, p2.x, p2.z), Geo3D.Det2x2(p1.x, p1.y, p2.x, p2.y));
        Vector3 adj1 = new Vector3(-1 * Geo3D.Det2x2(p0.y, p0.z, p2.y, p2.z), Geo3D.Det2x2(p0.x, p0.z, p2.x, p2.z), -1 * Geo3D.Det2x2(p0.x, p0.y, p2.x, p2.y));
        Vector3 adj2 = new Vector3(Geo3D.Det2x2(p0.y, p0.z, p1.y, p1.z), -1 * Geo3D.Det2x2(p0.x, p0.z, p1.x, p1.z), Geo3D.Det2x2(p0.x, p0.y, p1.x, p1.y));
        return new Matrix3x3(adj0, adj1, adj2).T3x3();
    }
    public Matrix3x3 Inverse3x3() // Inverse of a matrix A * I = One matrix
    {
        if (this.det == 0) {  return Matrix3x3.Matrix3x3One(); }
        float invDet = 1 / det;
        return this.Adj3x3().Multiply(invDet);
    }

    public Matrix3x3 T3x3() // Transponyficated version of a matrix
    {
        return new Matrix3x3(new Vector3(p0.x, p1.x, p2.x), new Vector3(p0.y, p1.y, p2.y), new Vector3(p0.z, p1.z, p2.z));
    }

    public static Matrix3x3 Matrix3x3One() // One matrix
    {
        return new Matrix3x3(new Vector3(1, 0, 0), new Vector3(0, 1, 0), new Vector3(0, 0, 1));
    }

    public override string ToString()
    {
        return "M3x3: " + p0 + "\n" + "M3x3: " + p1 + "\n" + "M3x3: " + p2;
    }
    public Matrix3x3 Multiply(float f)
    {
        return new Matrix3x3(this.p0 * f, this.p1 * f, this.p2 * f);
    }
    
    public Matrix3x3 Multiply(Matrix3x3 B)
    {
        Vector3 _p0 = new Vector3(p0.x * B.p0.x + p0.y * B.p1.x + p0.z * B.p2.x, p0.x * B.p0.y + p0.y * B.p1.y + p0.z * B.p2.y, p0.x * B.p0.z + p0.y * B.p1.z + p0.z * B.p2.z);
        Vector3 _p1 = new Vector3(p1.x * B.p0.x + p1.y * B.p1.x + p1.z * B.p2.x, p1.x * B.p0.y + p1.y * B.p1.y + p1.z * B.p2.y, p1.x * B.p0.z + p1.y * B.p1.z + p1.z * B.p2.z);
        Vector3 _p2 = new Vector3(p2.x * B.p0.x + p2.y * B.p1.x + p2.z * B.p2.x, p2.x * B.p0.y + p2.y * B.p1.y + p2.z * B.p2.y, p2.x * B.p0.z + p2.y * B.p1.z + p2.z * B.p2.z);
        return new Matrix3x3(_p0, _p1, _p2);
    }

    // Multiplication:
    // x y z    x   x
    // x y z *  y = y
    // x y z    z   z

    public Vector3 multiply(Vector3 v3)
    {
        float _p0 = p0.x * v3.x + p0.y * v3.y + p0.z * v3.z;
        float _p1 = p1.x * v3.x + p1.y * v3.y + p1.z * v3.z;
        float _p2 = p2.x * v3.x + p2.y * v3.y + p2.z * v3.z;
        return new Vector3(_p0, _p1, _p2);
    }

    public Matrix3x3 add(Matrix3x3 B)
    {
        return new Matrix3x3(p0 + B.p0, p1 + B.p1, p2 + B.p2);
    }
    /// <summary>
    /// Каждый вектор содержит только значения соответствующие координатам
    /// Для системы уравнений, линейных, плоскостей, используй CramerABC
    /// </summary>

    public static (bool, Vector3) CramerXYZ(Vector3 x, Vector3 y, Vector3 z, Vector3 answers)
    {   
        float det = Det3x3(x, y, z);
        if (Mathf.Abs(det) < Geo3D.epsilon) return (false, Vector3.zero);

        float dx = Det3x3(answers, y, z);
        float dy = Det3x3(x, answers, z);
        float dz = Det3x3(x, y, answers);

        return (true, new Vector3(dx / det, dy /det, dz / det));
    }
    public static (bool, Vector3) CramerABC(Vector3 a, Vector3 b, Vector3 c, Vector3 answers)
    {
        Vector3 x = new Vector3(a.x, b.x, c.x);
        Vector3 y = new Vector3(a.y, b.y, c.y);
        Vector3 z = new Vector3(a.z, b.z, c.z);
        float det = Det3x3(x, y, z);
        if (Mathf.Abs(det) < Geo3D.epsilon) return (false, Vector3.zero);

        float dx = Det3x3(answers, y, z);
        float dy = Det3x3(x, answers, z);
        float dz = Det3x3(x, y, answers);

        return (true, new Vector3(dx / det, dy / det, dz / det));
    }


    public Vector3 VectorMatrixMultiplication(Vector3 V)
    {
        return new Vector3( V.x * this.p0.x + V.y * this.p1.x + V.z * this.p2.x,
                            V.x * this.p0.y + V.y * this.p1.y + V.z * this.p2.y,
                            V.x * this.p0.z + V.y * this.p1.z + V.z * this.p2.z);
    }

}

public static class Matrix 
{
    public static Vector2 VectorMatrixMultiplication2D(Vector2 V, Vector2 row1, Vector2 row2)
    {
        return new Vector3(V.x * row1.x + V.y * row2.x,
                           V.x * row1.y + V.y * row2.y);
    }

    public static float[,] Multiply(float[,] A, float[,] B)
    {   // Column by Row
        int A_row = A.GetLength(0); int B_row = B.GetLength(0);
        int A_col = A.GetLength(1); int B_col = B.GetLength(1);
        if (A_col != B_row) throw new System.Exception("For matrix multiplication number of A columns and B rows has to match! Your values: " + A_col + " " + B_row);
        int similar_dimension = A_col; // B_row
        float[,] result = new float[A_row, B_col];
        for (int x = 0; x < A_row; x++)
            for (int y = 0; y < B_col; y++)
                result[x, y] = matrixSingleValue(A, B, x, y, A_col);
        return result;

        float matrixSingleValue(float[,] A, float[,] B, int Arow, int Bcolumn, int n)
        {
            float to_return = 0;
            for (int i = 0; i < n; i++) to_return += A[Arow, i] * B[i, Bcolumn];
            return to_return;
        }
    }
    public static bool TransposeCheck_AxBT(float[,] A, float[,] B)
    {
        return A.GetLength(1) == B.GetLength(1);
    }
    /// <summary>
    /// Returns a matrix that is equal to unchanged A multiplied by B transposed: return = A * Bt<p>
    /// B is unchanged and will be treated like transposed
    /// </summary>
    public static float[,] MultiplyTranspose_AxBT(float[,] A, float[,] B)
    {
        int A_row = A.GetLength(0); int B_row = B.GetLength(1);
        int A_col = A.GetLength(1); int B_col = B.GetLength(0);
        if (A_col != B_row) throw new System.Exception("For matrix multiplication number of A columns and B rows has to match! Your values: " + A_col + " " + B_row);
        float[,] result = new float[A_row, B_col];
        Debug.Log(A_row + " " + A_col + " " + B_row + " " + B_col);
        for (int x = 0; x < A_row; x++)
            for (int y = 0; y < B_col; y++)
                result[x, y] = matrixSingleValue(A, B, x, y, A_col);
        return result;

        float matrixSingleValue(float[,] A, float[,] B, int Arow, int Bcolumn, int n)
        {
            float to_return = 0;
            for (int i = 0; i < n; i++) to_return += A[Arow, i] * B[Bcolumn, i];
            return to_return;
        }
    }

    public static bool TransposeCheck_ATxB(float[,] A, float[,] B)
    {
        return A.GetLength(0) == B.GetLength(0);
    }

    public static float[,] MultiplyTranspose_ATxB(float[,] A, float[,] B)
    {
        int A_row = A.GetLength(0); int B_row = B.GetLength(1);
        int A_col = A.GetLength(1); int B_col = B.GetLength(0);
        if (A_col != B_row) throw new System.Exception("For matrix multiplication number of A columns and B rows has to match! Your values: " + A_col + " " + B_row);
        float[,] result = new float[A_col, B_row];
        //Debug.Log(A_row + " " + A_col + " " + B_row + " " + B_col);
        for (int x = 0; x < A_col; x++)
            for (int y = 0; y < B_row; y++)
                result[x, y] = matrixSingleValue(A, B, x, y, A_row);
        return result;

        float matrixSingleValue(float[,] A, float[,] B, int Arow, int Bcolumn, int n)
        {
            float to_return = 0;
            for (int i = 0; i < n; i++) to_return += A[i, Arow] * B[i, Bcolumn];
            return to_return;
        }
    }



    public static string DumpMatrix(float[,] m, int r = 4)
    {
        string n = "";
        for (int x = 0; x < m.GetLength(0); x++)
        {
            for (int y = 0; y < m.GetLength(1); y++)
            {
                n += Math.Round(m[x, y], r) + " ";
            }
            n += "\n";
        }
        return n;
    }

    public static float[,] MatrixFromVector(List<Vector3> v)
    {
        float[,] result = new float[v.Count, 3];
        for (int i = 0; i < v.Count; i++)
        {
            result[i, 0] = v[i].x;
            result[i, 1] = v[i].y;
            result[i, 2] = v[i].z;
        }
        return result;
    }
    public static float[,] MatrixFromVector(List<Vector2> v)
    {
        float[,] result = new float[v.Count, 2];
        for (int i = 0; i < v.Count; i++)
        {
            result[i, 0] = v[i].x;
            result[i, 1] = v[i].y;
        }
        return result;
    }
    public static Vector3[] Vector3FromMatrix(float[,] M)
    {
        if (M.GetLength(1) != 3) throw new System.Exception("Provided matrix can not be turned into a Vector3 list!");
        Vector3[] varr = new Vector3[M.GetLength(0)];
        for (int i = 0; i < varr.Length; i++) varr[i] = new Vector3(M[i, 0], M[i, 1], M[i, 2]);
        return varr;
    }
    public static Vector2[] Vector2FromMatrix(float[,] M)
    {
        if (M.GetLength(1) != 2) throw new System.Exception("Provided matrix can not be turned into a Vector2 list!");
        Vector2[] varr = new Vector2[M.GetLength(0)];
        for (int i = 0; i < varr.Length; i++) varr[i] = new Vector2(M[i, 0], M[i, 1]);
        return varr;
    }

    public static float Determinant(float[,] M)
    {
        if (M.GetLength(0) != M.GetLength(1)) throw new System.Exception("Provided matrix needs to be square matrix.");
        int n = M.GetLength(0);
        if (n == 1) return M[0, 0];
        if (n == 2) return M[0, 0] * M[1, 1] - M[0, 1] * M[1, 0];
        float det = 0;
        for (int j = 0; j < n; j++)
        {
            float cofactor = (float)Math.Pow(-1, j) * M[0, j] * Determinant(GetSubmatrix(M, 0, j));
            det += cofactor;
        }
        return det;
    }

    private static float[,] GetSubmatrix(float[,] M, int row, int column)
    {
        int n = M.GetLength(0);
        float[,] submatrix = new float[n - 1, n - 1];
        int row_index = 0;
        for (int i = 0; i < n; i++)
        {
            if (i == row) continue;
            int colIndex = 0;
            for (int j = 0; j < n; j++)
            {
                if (j == column) continue;
                submatrix[row_index, colIndex] = M[i, j];
                colIndex++;
            }
            row_index++;
        }
        return submatrix;
    }
}

