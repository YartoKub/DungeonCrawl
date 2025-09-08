using System.Collections.Generic;
using UnityEngine;

public class TestLineJumper : MonoBehaviour
{
    public List<Vector2> polygonA;
    public Vector2 polygonAoffset;
    public List<Vector2> polygonB;
    public Vector2 polygonBoffset;

    public List<Vector2> polygonAM;
    public List<Vector2> polygonBM;
    public List<Vector2> polygonCM;
    public Vector2 polyCMoffset;

    public bool showA;
    public bool showB;
    public bool showC;
    public bool showIntersections;

    private void Update()
    {
        polygonAM.Clear(); polygonBM.Clear(); polygonCM.Clear();
        for (int i = 0; i < polygonA.Count; i++) polygonAM.Add(polygonA[i] + polygonAoffset);
        for (int i = 0; i < polygonB.Count; i++) polygonBM.Add(polygonB[i] + polygonBoffset);
        for (int i = 0; i < polygonB.Count; i++) polygonCM.Add(polygonB[i] + polyCMoffset);

        //(int p1,int p2) = Poly2DToolbox.UniteHole(polygonAM, new Poly2D(polygonBM)); // List<Vector2> resultlings = 
        //Debug.Log(p1 + " " + p2);
        //List<Vector2> stitched = Poly2DToolbox.StitchHole(polygonAM, polygonBM, p1, p2);
        List<Vector2> stitched = Poly2DToolbox.UniteHoles(new Poly2D(polygonAM), new List<Poly2D>() { new Poly2D(polygonBM), new Poly2D(polygonCM) });

        if (showA) for (int i = 0; i < polygonAM.Count; i++) DebugUtilities.DebugDrawLine(polygonAM[i], polygonAM[(i + 1) % polygonAM.Count], Color.red);
        if (showB) for (int i = 0; i < polygonBM.Count; i++) DebugUtilities.DebugDrawLine(polygonBM[i], polygonBM[(i + 1) % polygonBM.Count], Color.cyan);
        if (showC) for (int i = 0; i < polygonCM.Count; i++) DebugUtilities.DebugDrawLine(polygonCM[i], polygonCM[(i + 1) % polygonCM.Count], Color.blue);

        if (showIntersections) for (int i = 0; i < stitched.Count; i++) DebugUtilities.DebugDrawLine(stitched[i], stitched[(i + 1) % stitched.Count], Color.green);
    }

}
