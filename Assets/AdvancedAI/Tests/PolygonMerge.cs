using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

public class PolygonMerge : MonoBehaviour
{
    public List<Vector2> polygonA;
    public Vector2 polygonAoffset;
    public List<Vector2> polygonB;
    public Vector2 polygonBoffset;

    public bool Abool; public bool Bbool;

    public List<Vector2> polygonAM;
    public List<Vector2> polygonBM;

    public bool showA;
    public bool showB;
    public bool showIntersections;
    public bool showStitch;
    public bool showTriangulation;
    [Header("Debug limits")]
    [Range(0, 50)] public int iterationLimit;
    [Range(-1, 50)] public int lightUpVert;
    private void Update()
    {
        polygonAM.Clear(); polygonBM.Clear();
        for (int i = 0; i < polygonA.Count; i++) polygonAM.Add(polygonA[i] + polygonAoffset);
        for (int i = 0; i < polygonB.Count; i++) polygonBM.Add(polygonB[i] + polygonBoffset);


        SuperPoly2D combined = GHPolygonMerge.CompleteGH(polygonAM, polygonBM, Abool, Bbool, 0.01f); // List<Vector2> resultlings = 
        combined.Compile();

        if (showA) for (int i = 0; i < polygonAM.Count; i++) DebugUtilities.DebugDrawLine(polygonAM[i], polygonAM[(i + 1) % polygonAM.Count], Color.red);
        if (showB) for (int i = 0; i < polygonBM.Count; i++) DebugUtilities.DebugDrawLine(polygonBM[i], polygonBM[(i + 1) % polygonBM.Count], Color.cyan);

        //Debug.Log(combined.Count );
        if (showIntersections) combined.DebugDraw();
        combined.DebugDumpHierarchy();
        for (int i = 0; i < combined.polygons.Count; i++)
        {
            string newString = "Total Verts: " + combined.polygons[i].vertices.Count + "\n";
            for (int j = 0; j < combined.polygons[i].vertices.Count; j++)
            {
                newString += combined.polygons[i].vertices[j] + " ";
            }
            Debug.Log(newString);
        }

        Poly2D mainPoly = null;
        bool canUniteHoles = false;
        List<Poly2D> holesList = new List<Poly2D>(combined.polygons);
        for (int i = 0; i < combined.polygons.Count; i++)
        {
            if (!combined.polygons[i].isHole)
            {
                mainPoly = combined.polygons[i];
                canUniteHoles = true;
                holesList.Remove(mainPoly);
                break;
            }
        }
        Debug.Log(canUniteHoles + " " + combined.PolygonDepth);
        if (!canUniteHoles | combined.PolygonDepth != 2)
        {
            Debug.Log("Bad polygons, can not triangulate!");
            return;
        }
        
        //holesList.RemoveAt(0);
        List<Vector2> stitched = Poly2DToolbox.UniteHoles(mainPoly, holesList);

        if (showStitch) for (int i = 0; i < stitched.Count; i++) DebugUtilities.DebugDrawLine(stitched[i], stitched[(i + 1) % stitched.Count], Color.red);

        
        List<Vector3Int> triangles = Poly2DToolbox.EarClipLimited(stitched, iterationLimit);
        if (showTriangulation)
        {
            foreach (Vector3Int abc in triangles)
            {
                DebugUtilities.DebugDrawLine(stitched[abc.x], stitched[abc.y], Color.green);
                DebugUtilities.DebugDrawLine(stitched[abc.y], stitched[abc.z], Color.green);
                DebugUtilities.DebugDrawLine(stitched[abc.z], stitched[abc.x], Color.green);
                DebugUtilities.DebugDrawCross((stitched[abc.x] + stitched[abc.y] + stitched[abc.z]) / 3, Color.yellow);
            }
        }

        //ConvexPoly2D.HealScars(stitched, triangles);
        if (lightUpVert >= 0 && lightUpVert <= stitched.Count - 1)
        {
            DebugUtilities.DebugDrawSquare(stitched[lightUpVert], Color.yellow, 0.1f);
            DebugUtilities.DebugDrawSquare(stitched[lightUpVert], Color.yellow, 0.15f);
        }
    }
}
[CustomEditor(typeof(PolygonMerge))]
class PolygonMergeEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();
        PolygonMerge polyMerger = (PolygonMerge)target;

        if (GUILayout.Button("Increment iterations"))
            polyMerger.iterationLimit = Mathf.Clamp(polyMerger.iterationLimit + 1, 0, 50);
        if (GUILayout.Button("Decrement iterations"))
            polyMerger.iterationLimit = Mathf.Clamp(polyMerger.iterationLimit - 1, 0, 50);
        if (GUILayout.Button("Increment vert"))
            polyMerger.lightUpVert = Mathf.Clamp(polyMerger.lightUpVert + 1, -1, 50);
        if (GUILayout.Button("Decrement vert"))
            polyMerger.lightUpVert = Mathf.Clamp(polyMerger.lightUpVert - 1, -1, 50);
    }
}