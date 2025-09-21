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

    public List<Vector2> stitched;

    public bool showA;
    public bool showB;
    public bool showC;
    public bool showCombined;

    public bool showTriangulation;
    public bool showVoronoi;

    public int ShowTriangle;
    public int ShowVertice;
    public Vector3Int debugVector;


    private void Update()
    {
        
        polygonAM.Clear(); polygonBM.Clear(); polygonCM.Clear();
        for (int i = 0; i < polygonA.Count; i++) polygonAM.Add(polygonA[i] + polygonAoffset);
        for (int i = 0; i < polygonB.Count; i++) polygonBM.Add(polygonB[i] + polygonBoffset);
        for (int i = 0; i < polygonB.Count; i++) polygonCM.Add(polygonB[i] + polyCMoffset);

        stitched = Poly2DToolbox.UniteHoles(new Poly2D(polygonAM), new List<Poly2D>() { new Poly2D(polygonBM), new Poly2D(polygonCM) });
        List<Vector3Int> triangles = Poly2DToolbox.EarClip(stitched);
        ConvexPoly2D.HealScars(stitched, triangles);

        if (showA) for (int i = 0; i < polygonAM.Count; i++) DebugUtilities.DebugDrawLine(polygonAM[i], polygonAM[(i + 1) % polygonAM.Count], Color.red);
        if (showB) for (int i = 0; i < polygonBM.Count; i++) DebugUtilities.DebugDrawLine(polygonBM[i], polygonBM[(i + 1) % polygonBM.Count], Color.cyan);
        if (showC) for (int i = 0; i < polygonCM.Count; i++) DebugUtilities.DebugDrawLine(polygonCM[i], polygonCM[(i + 1) % polygonCM.Count], Color.blue);
        if (showCombined) for (int i = 0; i < stitched.Count; i++) DebugUtilities.DebugDrawLine(stitched[i], stitched[(i + 1) % stitched.Count], Color.green);


        /*
        DebugUtilities.DebugDrawLine(stitched[PickPointA], stitched[PickPointB], Color.red);
        DebugUtilities.DebugDrawLine(stitched[PickPointB], stitched[PickPointC], Color.red);
        DebugUtilities.DebugDrawLine(stitched[PickPointC], stitched[PickPointA], Color.red);
        DebugUtilities.DebugDrawCross(stitched[PickPointP], Poly2DToolbox.DoesContainPoint(stitched[PickPointA], stitched[PickPointB], stitched[PickPointC], stitched[PickPointP]) ? Color.green : Color.red);
        */



        List<Vector3Int> connections = ConvexPoly2D.EstablishConnections(stitched, triangles);
        if (showTriangulation)
        {
            foreach (Vector3Int abc in triangles)
            {
                DebugUtilities.DebugDrawLine(stitched[abc.x], stitched[abc.y], Color.green);
                DebugUtilities.DebugDrawLine(stitched[abc.y], stitched[abc.z], Color.green);
                DebugUtilities.DebugDrawLine(stitched[abc.z], stitched[abc.x], Color.green);
                DebugUtilities.DebugDrawCross((stitched[abc.x] + stitched[abc.y] + stitched[abc.z]) / 3, Color.yellow);
            }
            ConvexPoly2D. DrawPolygonConnections(connections, triangles, stitched);
        }


        ConvexPoly2D.IterativeVoronoi(stitched, triangles, connections);

        if (showVoronoi)
        {
            foreach (Vector3Int abc in triangles)
            {
                DebugUtilities.DebugDrawLine(stitched[abc.x], stitched[abc.y], Color.green);
                DebugUtilities.DebugDrawLine(stitched[abc.y], stitched[abc.z], Color.green);
                DebugUtilities.DebugDrawLine(stitched[abc.z], stitched[abc.x], Color.green);
                DebugUtilities.DebugDrawCross((stitched[abc.x] + stitched[abc.y] + stitched[abc.z]) / 3, Color.yellow);
            }
            ConvexPoly2D.DrawPolygonConnections(connections, triangles, stitched);
        }

        if (ShowTriangle >= 0 && ShowTriangle < triangles.Count)
        {
            Vector3Int triangle = triangles[ShowTriangle];
            DebugUtilities.DebugDrawCross((stitched[triangle.x] + stitched[triangle.y] + stitched[triangle.z]) / 3, Color.purple);
        }
        if (ShowVertice >= 0 && ShowVertice < stitched.Count)
        {
            DebugUtilities.DebugDrawCross(stitched[ShowVertice], Color.purple);
        }

        ConvexPoly2D poly = new ConvexPoly2D(stitched, triangles, connections, debugVector);
        poly.DebugDrawSelf();

    }

}
