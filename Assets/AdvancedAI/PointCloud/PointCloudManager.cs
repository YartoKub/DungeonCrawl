using System.Collections.Generic;
using UnityEngine;



public class PointCloudManager : MonoBehaviour
{
    public List<Vector2> points;
    public List<Vector2> pn_points;
    // Use this for initialization
    public Vector2 all_move_limits;
    public Vector2 loc_move_limits;
    public bool all_move, all_rotate, all_scale, move;

    public Vector2 offset;
    public float theta;
    void Start()
    {

    }

    // Update is called once per frame
    void Update()
    {

    }

    private void OnDrawGizmos()
    {
        if (   points != null) for (int i = 0; i <    points.Count; i++) DebugUtilities.HandlesDrawCross(   points[i], Color.yellow);
        if (pn_points != null) for (int i = 0; i < pn_points.Count; i++) DebugUtilities.HandlesDrawCross(RotateAgainstZeroZero(pn_points[i], theta) + offset, new Color(1, 0.5f, 0, 1));

    }

    public void AddPoint(Vector2 p)
    {
        points.Add(p);
        Geo3D.SortPoints(this.points);
    }

    public void RemovePoint(int i)
    {
        if (i < 0 | i >= points.Count) return;
        points.RemoveAt(i);
    }

    public (int, float) ClosestPoint(Vector2 p)
    {
        int min = -1; float min_d = float.MaxValue;
        for (int i = 0; i < points.Count; i++)
        {
            float d = (p - points[i]).magnitude;
            if (d < min_d)
            {
                min = i;
                min_d = d;
            }
        }
        return (min, min_d);
    }
    public void DebugHighLightPoint(int index) { DebugUtilities.DrawCube(points[index], Vector3.one * 0.2f, Color.white); }
    public void HandlesHighLightPoint(int index) { DebugUtilities.HandlesDrawCube(points[index], Vector3.one * 0.2f, Color.yellow); }

    public void PurgePNPoints()
    {
        this.pn_points.Clear();
    }
    public void GeneratePseudoNewPoints() => GeneratePseudoNewPoints(all_move, all_rotate, all_scale, move);
    public void GeneratePseudoNewPoints(bool all_move, bool all_rotate, bool all_scale, bool move)
    {
        pn_points = new List<Vector2>(); // Двигаю их чуть чуть для шума
        for (int i = 0; i < points.Count; i++)
        {
            Vector2 xyd = Vector2.zero;
            if (move) xyd = new Vector2(Random.Range(loc_move_limits.x, loc_move_limits.y), Random.Range(loc_move_limits.x, loc_move_limits.y));
            pn_points.Add(points[i] + xyd);
        }

        Vector2 global_movement = all_move ? new Vector2(Random.Range(all_move_limits.x, all_move_limits.y), Random.Range(all_move_limits.x, all_move_limits.y)) :  Vector2.zero;

        float rpi =  Random.Range(0.0f, Mathf.PI * 2);
        if (all_rotate)
            for (int i = 0; i < pn_points.Count; i++)
                pn_points[i] = RotateAgainstZeroZero(pn_points[i], rpi);


        for (int i = 0; i < pn_points.Count; i++) pn_points[i] += global_movement;
    }

    public void ClosestPointMatching()
    {
        if (pn_points == null | pn_points.Count == 0 | this.points.Count == 0) return;
        float[,] CM = CovarianceMatrix(points, pn_points);
        double[][] double_double_array = SingularValueDecomposition.SVDProgram.FloatMatrixToDoubleDoubleArray(CM);
        SingularValueDecomposition.SVDProgram.MatShow(double_double_array, 3, 9);
        SingularValueDecomposition.SVDProgram.SVD_Jacobi(double_double_array, out var Ud, out var Vhd, out var Sd);
        float[,] U = SingularValueDecomposition.SVDProgram.DoubleDoubleArrayToFloatMatrix(Ud);
        float[,] Vh = SingularValueDecomposition.SVDProgram.DoubleDoubleArrayToFloatMatrix(Vhd);
        Debug.Log("U component: " + Matrix.Determinant(U).ToString() + "\n"  + Matrix.DumpMatrix(U, 5));
        Debug.Log("Vh component: " + Matrix.Determinant(Vh).ToString() + "\n" + Matrix.DumpMatrix(Vh, 5));
        Debug.Log(Matrix.Determinant(U));
        SingularValueDecomposition.SVDProgram.VecShow(Sd, 3, 9);

        float[,] RotationMatrix = Matrix.Multiply(Vh, U);
        Debug.Log("Rotation determinant: " + Matrix.Determinant(RotationMatrix) + "\n" + Matrix.DumpMatrix(RotationMatrix));
        Vector2 row1 = new Vector2(RotationMatrix[0, 0], RotationMatrix[0, 1]);
        Vector2 row2 = new Vector2(RotationMatrix[1, 0], RotationMatrix[1, 1]);
        Vector2 up_mod = Matrix.VectorMatrixMultiplication2D(Vector2.up, row1, row2);
        Vector2 right_mod = Matrix.VectorMatrixMultiplication2D(Vector2.right, row1, row2);
        DebugUtilities.DebugDrawLine(Vector2.zero, Vector2.up, Color.red, 10f);
        DebugUtilities.DebugDrawLine(Vector2.zero, up_mod, Color.pink, 10f);
        DebugUtilities.DebugDrawLine(Vector2.zero, Vector2.right, Color.blue, 10f);
        DebugUtilities.DebugDrawLine(Vector2.zero, right_mod, Color.cyan, 10f);

        float angle_to_rotate = Poly2DToolbox.SignedAngle(up_mod, Vector2.zero, Vector2.up);

        Debug.Log(angle_to_rotate);
        this.theta = (-angle_to_rotate) * Mathf.Deg2Rad;
    }
    public Vector2 RotateAgainstZeroZero(Vector2 v, float teta)
    {
        float cos_a = Mathf.Cos(teta);
        float sin_a = Mathf.Sin(teta);
        return new Vector2(v.x * cos_a - v.y * sin_a, v.x * sin_a + v.y * cos_a);
    }

    public static float[,] CovarianceMatrix(List<Vector2> A, List<Vector2> B)
    {
        float[,] Am = Matrix.MatrixFromVector(A);
        float[,] Bm = Matrix.MatrixFromVector(B);
        float[,] Covariance = Matrix.MultiplyTranspose_ATxB(Am, Bm);
        Debug.Log("Covariance matrix: \n" + Matrix.DumpMatrix(Covariance));
        return Covariance;
    }

    public static List<(int new_frame, float distance, int reference_frame)> ClosestPointList(List<Vector2> new_frame, List<Vector2> reference_frame)
    {
        List<(int, float, int)> point_pairs = new List<(int, float, int)>();



        return point_pairs;
    }


    public static Vector2 GetCentroid(List<Vector2> points)
    {
        Vector2 centroid = Vector2.zero;
        for (int i = 0; i < points.Count; i++)
            centroid += points[i];
        centroid = centroid / points.Count;
        return centroid;
    }
}
