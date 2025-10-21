using System.Collections.Generic;
using UnityEngine;

public class TestOptimizedDecomposition : MonoBehaviour
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
    public bool showCombined;

    public bool showTriangulation;
    public bool showVoronoi;
    public bool showConvexDecomposition;

    public int ShowTriangle;
    public int ShowVertice;
    public Vector3Int debugVector;

    private void Update()
    {
        polygonAM.Clear(); polygonBM.Clear(); polygonCM.Clear();
        for (int i = 0; i < polygonA.Count; i++) polygonAM.Add(polygonA[i] + polygonAoffset);
        for (int i = 0; i < polygonB.Count; i++) polygonBM.Add(polygonB[i] + polygonBoffset);
        for (int i = 0; i < polygonB.Count; i++) polygonCM.Add(polygonB[i] + polyCMoffset);

        HierarchicalPoly2D sp = new HierarchicalPoly2D();
        sp.polygons.Add(new Poly2D(polygonAM));
        sp.polygons.Add(new Poly2D(polygonBM));
        sp.polygons.Add(new Poly2D(polygonCM));
        sp.Compile();
        ComplexPolygon complex = new ComplexPolygon(sp);
        List<Triangle> triangles = complex.GetTriangulation();
        List<Vector2> combined_points = complex.GetVertices();

        //stitched = Poly2DToolbox.UniteHoles(new Poly2D(polygonAM), new List<Poly2D>() { new Poly2D(polygonBM), new Poly2D(polygonCM) });
        //List<Vector3Int> triangles = Poly2DToolbox.EarClip(stitched);
        //ConvexPoly2D.HealScars(stitched, triangles);

        if (showA) for (int i = 0; i < polygonAM.Count; i++) DebugUtilities.DebugDrawLine(polygonAM[i], polygonAM[(i + 1) % polygonAM.Count], Color.red);
        if (showB) for (int i = 0; i < polygonBM.Count; i++) DebugUtilities.DebugDrawLine(polygonBM[i], polygonBM[(i + 1) % polygonBM.Count], Color.cyan);
        if (showC) for (int i = 0; i < polygonCM.Count; i++) DebugUtilities.DebugDrawLine(polygonCM[i], polygonCM[(i + 1) % polygonCM.Count], Color.blue);

        
        List<Vector3Int> connections = ConvexPoly2D.EstablishConnections(combined_points, triangles);
        Debug.Log("Triangles: " + triangles.Count + " conn: " + connections.Count);
        if (showTriangulation)
        {
            foreach (Triangle abc in triangles)
            {
                DebugUtilities.DebugDrawLine(combined_points[abc.a], combined_points[abc.b], Color.green);
                DebugUtilities.DebugDrawLine(combined_points[abc.b], combined_points[abc.c], Color.green);
                DebugUtilities.DebugDrawLine(combined_points[abc.c], combined_points[abc.a], Color.green);
                DebugUtilities.DebugDrawCross((combined_points[abc.a] + combined_points[abc.b] + combined_points[abc.c]) / 3, Color.yellow);
            }
            ConvexPoly2D.DrawPolygonConnections(connections, triangles, combined_points);
        }


        ConvexPoly2D.IterativeVoronoi(combined_points, triangles, connections);
        if (showVoronoi)
        {
            foreach (Triangle abc in triangles)
            {
                DebugUtilities.DebugDrawLine(combined_points[abc.a], combined_points[abc.b], Color.green);
                DebugUtilities.DebugDrawLine(combined_points[abc.b], combined_points[abc.c], Color.green);
                DebugUtilities.DebugDrawLine(combined_points[abc.c], combined_points[abc.a], Color.green);
                DebugUtilities.DebugDrawCross((combined_points[abc.a] + combined_points[abc.b] + combined_points[abc.c]) / 3, Color.yellow);
            }
            ConvexPoly2D.DrawPolygonConnections(connections, triangles, combined_points);
        }
        
        

        ConvexPoly2D poly = new ConvexPoly2D(combined_points, triangles, connections, debugVector);
        if (showConvexDecomposition) poly.DebugDrawSelf();

        if (ShowTriangle >= 0 && ShowTriangle < poly.polygons.Count)
        {
            poly.DebugDrawSubject(ShowTriangle, Color.yellow);
            poly.DebugDrawSubjectCenter(ShowTriangle, Color.cyan);
        }

        if (ShowVertice >= 0 && ShowVertice < complex.vertices.Count)
        {
            DebugUtilities.DebugDrawCross(complex.vertices[ShowVertice], Color.purple);
        }

    }
}
