using UnityEditor;
using UnityEngine;
using System.Collections.Generic;


[CustomEditor(typeof(TestMatrices))]
public class TestMatricesEditor : Editor
{
    public override void OnInspectorGUI()
    {
        if (GUILayout.Button("Test multiply")) ((TestMatrices)target).TestMultiply();
        if (GUILayout.Button("Test multiply transpose")) ((TestMatrices)target).TestMultiplyTranspose();
        base.OnInspectorGUI();
    }
}

public class TestMatrices : MonoBehaviour
{
    public List<Vector3> matrixA;
    public List<Vector3> matrixB;
    public void TestMultiply()
    {
        float[,] matA = Matrix.MatrixFromVector(matrixA);
        float[,] matB = Matrix.MatrixFromVector(matrixB);
        Debug.Log(Matrix.DumpMatrix(matA, 3));
        Debug.Log(Matrix.DumpMatrix(matB, 3));

        float[,] matCmul = Matrix.Multiply(matA, matB);
        Debug.Log("A x B: \n" + Matrix.DumpMatrix(matCmul, 3));

    }
    public void TestMultiplyTranspose()
    {
        float[,] matA = Matrix.MatrixFromVector(matrixA);
        float[,] matB = Matrix.MatrixFromVector(matrixB);
        Debug.Log(Matrix.DumpMatrix(matA, 3));
        Debug.Log(Matrix.DumpMatrix(matB, 3));

        bool can_ATxB = Matrix.TransposeCheck_ATxB(matA, matB);
        Debug.Log(can_ATxB);
        if (can_ATxB)
        {
            float[,] matCmulAt = Matrix.MultiplyTranspose_ATxB(matA, matB);
            Debug.Log("AT x B: \n" + Matrix.DumpMatrix(matCmulAt, 3));
        }
        

        bool can_AxBT = Matrix.TransposeCheck_AxBT(matA, matB);
        Debug.Log(can_AxBT);
        if (can_AxBT)
        {
            float[,] matCmulBt = Matrix.MultiplyTranspose_AxBT(matA, matB);
            Debug.Log("A x BT: \n" + Matrix.DumpMatrix(matCmulBt, 3));
        }
        
    }
}
