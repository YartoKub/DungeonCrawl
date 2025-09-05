using System.Collections.Generic;
using UnityEngine;

public class PolygonMerge : MonoBehaviour
{
    public List<Vector2> polygonA;
    public Vector2 polygonAoffset;
    public List<Vector2> polygonB;
    public Vector2 polygonBoffset;

    public List<Vector2> polygonAM;
    public List<Vector2> polygonBM;

    public bool showA;
    public bool showB;
    public bool showIntersections;

    private void Update()
    {
        polygonAM.Clear(); polygonBM.Clear();
        for (int i = 0; i < polygonA.Count; i++) polygonAM.Add(polygonA[i] + polygonAoffset);
        for (int i = 0; i < polygonB.Count; i++) polygonBM.Add(polygonB[i] + polygonBoffset);

        if (showA) for (int i = 0; i < polygonAM.Count; i++) DebugUtilities.DebugDrawLine(polygonAM[i], polygonAM[(i + 1) % polygonAM.Count], Color.red);
        if (showB) for (int i = 0; i < polygonBM.Count; i++) DebugUtilities.DebugDrawLine(polygonBM[i], polygonBM[(i + 1) % polygonBM.Count], Color.cyan);

        //List<Vector2> result = Poly2DToolbox.CompleteGH(polygonAM, polygonBM, true, true, 0.01f); // List<Vector2> resultlings = 

        //if (showIntersections) for (int i = 0; i < result.Count; i++) DebugUtilities.DebugDrawLine(result[i], result[(i + 1) % result.Count], Color.yellow);
    }
}
