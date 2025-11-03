using System.Collections;
using System.Collections.Generic;
using UnityEngine;

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


}
