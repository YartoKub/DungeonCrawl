using UnityEngine;
using System.Collections.Generic;
using UnityEditor;

public class DegeneratePolygonTest : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {

    }
    public static void AddEncompassedDegenerates()
    {
        Poly2D polygon = new Poly2D(new Vector2(-0.5f, 0), new Vector2(0.5f, 0), new Vector2(0, 1));
        PolygonManager.GetManager().AddPolygon(polygon);

        Poly2D polygon2 = new Poly2D(new Vector2(0, 1), new Vector2(0.5f, 0), new Vector2(-0.5f, 0), new Vector2(0, 1), new Vector2(-1, 2), new Vector2(-3, -1), new Vector2(3, -1), new Vector2(1, 2));
        PolygonManager.GetManager().AddPolygon(polygon2);
    }
    public static void AddOverlapDegenerates()
    {
        Poly2D polygon = new Poly2D(new Vector2(-4, -1), new Vector2(4, -1), new Vector2(4, 6), new Vector2(3, 0), new Vector2(2, 6), new Vector2(1, 0), new Vector2(0, 6), new Vector2(-1, 0), new Vector2(-2, 6), new Vector2(-3, 0), new Vector2(-4, 6));
        PolygonManager.GetManager().AddPolygon(polygon);

        Poly2D polygon2 = new Poly2D(new Vector2(-6, 5), new Vector2(-6, 1), new Vector2(5, 1), new Vector2(-5, 2), new Vector2(5, 3), new Vector2(-5, 4), new Vector2(5, 5));
        PolygonManager.GetManager().AddPolygon(polygon2);
    }

    public static void AddCollinnearDegenerates()
    {
        Poly2D polygon = new Poly2D(new Vector2(0, 0), new Vector2(1, 0), new Vector2(1, 3), new Vector2(3, 3), new Vector2(3, 4), new Vector2(4, 4), new Vector2(4, 3), new Vector2(5, 3), new Vector2(5, 5), new Vector2(0, 5));
        PolygonManager.GetManager().AddPolygon(polygon);

        Poly2D polygon2 = new Poly2D(new Vector2(0, 0), new Vector2(4, 0), new Vector2(4, 4), new Vector2(3, 4), new Vector2(3, 1), new Vector2(0, 1));
        PolygonManager.GetManager().AddPolygon(polygon2);
    }
    public static void Add6Start()
    {
        Poly2D polygon = new Poly2D(new Vector2(0, 0), new Vector2(3, 0), new Vector2(1.5f, 3));
        PolygonManager.GetManager().AddPolygon(polygon);

        Poly2D polygon2 = new Poly2D(new Vector2(1.5f, -1), new Vector2(3.5f, 2f), new Vector2(-0.5f, 2f));
        PolygonManager.GetManager().AddPolygon(polygon2);
    }
}
[CustomEditor(typeof(DegeneratePolygonTest))]
public class DegeneratePolygonTestEditor : Editor 
{
    public override void OnInspectorGUI()
    {

        if (GUILayout.Button("Encompassed Degenerate")) DegeneratePolygonTest.AddEncompassedDegenerates();
        if (GUILayout.Button("Multiple Overlap Degenerate")) DegeneratePolygonTest.AddOverlapDegenerates();
        base.OnInspectorGUI();
    }
}

