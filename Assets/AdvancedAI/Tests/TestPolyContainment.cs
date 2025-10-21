using System.Collections.Generic;
using UnityEngine;

public class TestPolyContainment : MonoBehaviour
{
    public List<Vector2> polygonA;
    public Vector2 polygonAoffset;
    public List<Vector2> polygonB;
    public Vector2 polygonBoffset;
    public List<Vector2> polygonC;
    public Vector2 polygonCoffset;
    public List<Vector2> polygonD;
    public Vector2 polygonDoffset;



    public List<Vector2> polygonAM;
    public List<Vector2> polygonBM;
    public List<Vector2> polygonCM;
    public List<Vector2> polygonDM;

    public bool showA;
    public bool showB;
    public bool showC;
    public bool showD;

    public bool gigaP;


    private void Update()
    {
        polygonAM.Clear(); polygonBM.Clear(); polygonCM.Clear(); polygonDM.Clear();
        for (int i = 0; i < polygonA.Count; i++) polygonAM.Add(polygonA[i] + polygonAoffset);
        for (int i = 0; i < polygonB.Count; i++) polygonBM.Add(polygonB[i] + polygonBoffset);
        for (int i = 0; i < polygonC.Count; i++) polygonCM.Add(polygonC[i] + polygonCoffset);
        for (int i = 0; i < polygonD.Count; i++) polygonDM.Add(polygonD[i] + polygonDoffset);

        Poly2D Ap = new Poly2D(polygonAM);
        Poly2D Bp = new Poly2D(polygonBM);
        Poly2D Cp = new Poly2D(polygonCM);
        Poly2D Dp = new Poly2D(polygonDM);

        HierarchicalPoly2D gigaPoly = new HierarchicalPoly2D();
        gigaPoly.polygons.Add(Ap);
        gigaPoly.polygons.Add(Bp);
        gigaPoly.polygons.Add(Cp);
        gigaPoly.polygons.Add(Dp);

        gigaPoly.Compile();

        if (showA) for (int i = 0; i < polygonAM.Count; i++) DebugUtilities.DebugDrawLine(polygonAM[i], polygonAM[(i + 1) % polygonAM.Count], Color.red);
        if (showB) for (int i = 0; i < polygonBM.Count; i++) DebugUtilities.DebugDrawLine(polygonBM[i], polygonBM[(i + 1) % polygonBM.Count], Color.red);
        if (showC) for (int i = 0; i < polygonCM.Count; i++) DebugUtilities.DebugDrawLine(polygonCM[i], polygonCM[(i + 1) % polygonCM.Count], Color.red);
        if (showD) for (int i = 0; i < polygonDM.Count; i++) DebugUtilities.DebugDrawLine(polygonDM[i], polygonDM[(i + 1) % polygonDM.Count], Color.red);

        if (gigaP) gigaPoly.DebugDraw();
        gigaPoly.DebugDumpHierarchy();
    }
}
