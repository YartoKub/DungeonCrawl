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
        List<Vector2> p1 = new List<Vector2> { new Vector2(-0.5f, 0), new Vector2(0.5f, 0), new Vector2(0, 1) };
        PolygonManager.GetManager().AddPolygon(p1);

        List<Vector2> p2 = new List<Vector2> { new Vector2(0, 1), new Vector2(0.5f, 0), new Vector2(-0.5f, 0), new Vector2(0, 1), new Vector2(-1, 2), new Vector2(-3, -1), new Vector2(3, -1), new Vector2(1, 2) };
        PolygonManager.GetManager().AddPolygon(p2);
    }
    public static void AddOverlapDegenerates()
    {
        List<Vector2> p1 = new List<Vector2> { new Vector2(-4, -1), new Vector2(4, -1), new Vector2(4, 6), new Vector2(3, 0), new Vector2(2, 6), new Vector2(1, 0), new Vector2(0, 6), new Vector2(-1, 0), new Vector2(-2, 6), new Vector2(-3, 0), new Vector2(-4, 6) };
        List<Vector2> p2 = new List<Vector2> { new Vector2(-6, 5), new Vector2(-6, 1), new Vector2(5, 1), new Vector2(-5, 2), new Vector2(5, 3), new Vector2(-5, 4), new Vector2(5, 5) };
        bool has_compiled_1 = Poly2D.CompilePolygon(p1, out Poly2D poly1, Orientation.CounterClockwise);
        bool has_compiled_2 = Poly2D.CompilePolygon(p2, out Poly2D poly2, Orientation.CounterClockwise);
        
        if (!(has_compiled_1 & has_compiled_2)) { Debug.Log("Failed to compile polygons, test is not valid"); return; }
        CH2D_Chunk.PolygonAddMode cash = PolygonManager.GetManager().polygonAddMode;
        PolygonManager.GetManager().polygonAddMode = CH2D_Chunk.PolygonAddMode.FillHoles;
        PolygonManager.GetManager().AddPolygon(poly1);
        PolygonManager.GetManager().AddPolygon(poly2);
        PolygonManager.GetManager().polygonAddMode = cash;
    }

    public static void AddCollinnearDegenerates()
    {
        List<Vector2> p1 = new List<Vector2> { new Vector2(0, 0), new Vector2(1, 0), new Vector2(1, 3), new Vector2(3, 3), new Vector2(3, 4), new Vector2(4, 4), new Vector2(4, 3), new Vector2(5, 3), new Vector2(5, 5), new Vector2(0, 5) };
        List<Vector2> p2 = new List<Vector2> { new Vector2(0, 0), new Vector2(4, 0), new Vector2(4, 4), new Vector2(3, 4), new Vector2(3, 1), new Vector2(0, 1) };
        bool has_compiled_1 = Poly2D.CompilePolygon(p1, out Poly2D poly1, Orientation.CounterClockwise);
        bool has_compiled_2 = Poly2D.CompilePolygon(p2, out Poly2D poly2, Orientation.CounterClockwise);

        if (!(has_compiled_1 & has_compiled_2)) { Debug.Log("Failed to compile polygons, test is not valid"); return; }
        CH2D_Chunk.PolygonAddMode cash = PolygonManager.GetManager().polygonAddMode;
        PolygonManager.GetManager().polygonAddMode = CH2D_Chunk.PolygonAddMode.FillHoles;
        PolygonManager.GetManager().AddPolygon(poly1);
        PolygonManager.GetManager().AddPolygon(poly2);
        PolygonManager.GetManager().polygonAddMode = cash;
    }
    public static void Add6Start()
    {
        List<Vector2> p1 = new List<Vector2> { new Vector2(0, 0), new Vector2(3, 0), new Vector2(1.5f, 3) };
        List<Vector2> p2 = new List<Vector2> { new Vector2(1.5f, -1), new Vector2(3.5f, 2f), new Vector2(-0.5f, 2f) };
        bool has_compiled_1 = Poly2D.CompilePolygon(p1, out Poly2D poly1, Orientation.CounterClockwise);
        bool has_compiled_2 = Poly2D.CompilePolygon(p2, out Poly2D poly2, Orientation.CounterClockwise);
        if (!(has_compiled_1 & has_compiled_2)) { Debug.Log("Failed to compile polygons, test is not valid"); return; }
        PolygonManager.GetManager().AddPolygon(poly1);
        PolygonManager.GetManager().AddPolygon(poly2);
    }
    public static void GemCoveredUncovered()
    {
        List<Vector2> p1 = new List<Vector2> { new Vector2(0, 0), new Vector2(2, 0), new Vector2(3, 1), new Vector2(2, 2), new Vector2(0, 2), new Vector2(-1, 1) };
        List<Vector2> p2 = new List<Vector2> { new Vector2(2, 2), new Vector2(0, 2), new Vector2(-1, 1), new Vector2(0, 0), new Vector2(2, 0) };
        bool has_compiled_1 = Poly2D.CompilePolygon(p1, out Poly2D poly1, Orientation.CounterClockwise);
        bool has_compiled_2 = Poly2D.CompilePolygon(p2, out Poly2D poly2, Orientation.CounterClockwise);

        if (!(has_compiled_1 & has_compiled_2)) { Debug.Log("Failed to compile polygons, test is not valid"); return; }
        CH2D_Chunk.PolygonAddMode cash = PolygonManager.GetManager().polygonAddMode;
        PolygonManager.GetManager().polygonAddMode = CH2D_Chunk.PolygonAddMode.FillHoles;
        PolygonManager.GetManager().AddPolygon(poly1);
        PolygonManager.GetManager().AddPolygon(poly2);
        PolygonManager.GetManager().polygonAddMode = cash;
    }
    public static void SquareHoleHalfTriangle()
    {
        List<Vector2> p1 = new List<Vector2> { new Vector2(0, 0), new Vector2(0, 3), new Vector2(3, 3), new Vector2(3, 0)};
        List<Vector2> p2 = new List<Vector2> { new Vector2(0, 0), new Vector2(3, 0), new Vector2(3, 3)};
        bool has_compiled_1 = Poly2D.CompilePolygon(p1, out Poly2D poly1, Orientation.Clockwise);
        bool has_compiled_2 = Poly2D.CompilePolygon(p2, out Poly2D poly2, Orientation.CounterClockwise);

        if (!(has_compiled_1 & has_compiled_2)) { Debug.Log("Failed to compile polygons, test is not valid"); return; }

        PolygonManager.GetManager().AddPolygon(poly1);
        PolygonManager.GetManager().AddPolygon(poly2);
    }
    public static void FullOverlap()
    {
        List<Vector2> p1 = new List<Vector2> { new Vector2(0, 0), new Vector2(2, 0), new Vector2(3, 1), new Vector2(2, 2), new Vector2(0, 2), new Vector2(-1, 1) };
        List<Vector2> p2 = new List<Vector2> { new Vector2(2, 2), new Vector2(0, 2), new Vector2(-1, 1), new Vector2(0, 0), new Vector2(2, 0), new Vector2(3, 1) };
        bool has_compiled_1 = Poly2D.CompilePolygon(p1, out Poly2D poly1, Orientation.CounterClockwise);
        bool has_compiled_2 = Poly2D.CompilePolygon(p2, out Poly2D poly2, Orientation.CounterClockwise);

        if (!(has_compiled_1 & has_compiled_2)) { Debug.Log("Failed to compile polygons, test is not valid"); return; }
        CH2D_Chunk.PolygonAddMode cash = PolygonManager.GetManager().polygonAddMode;
        PolygonManager.GetManager().polygonAddMode = CH2D_Chunk.PolygonAddMode.FillHoles;
        PolygonManager.GetManager().AddPolygon(poly1);
        PolygonManager.GetManager().AddPolygon(poly2);
        PolygonManager.GetManager().polygonAddMode = cash;
    }
    public static void BanticTopBottom()
    {
        List<Vector2> p1 = new List<Vector2>{new Vector2(0, 0), new Vector2(2, 0), new Vector2(4, 2), new Vector2(6, 0), new Vector2(8, 0), new Vector2(8, 2), new Vector2(6, 2), new Vector2(4, 2), new Vector2(2, 2), new Vector2(0, 2)};
        PolygonManager.GetManager().AddPolygon(p1);

        List<Vector2> p2 = new List<Vector2>{new Vector2(0, 2), new Vector2(2, 2), new Vector2(4, 2), new Vector2(6, 2), new Vector2(8, 2), new Vector2(8, 4), new Vector2(6, 4), new Vector2(4, 2), new Vector2(2, 4), new Vector2(0, 4)};
        PolygonManager.GetManager().AddPolygon(p2);
    }
    public static void BanticLeftRight()
    {
        List<Vector2> p1 = new List<Vector2> { new Vector2(0, 0), new Vector2(2, 0), new Vector2(4, 2), new Vector2(2, 4), new Vector2(0, 4) };
        PolygonManager.GetManager().AddPolygon(p1);

        List<Vector2> p2 = new List<Vector2> { new Vector2(4, 2), new Vector2(6, 0), new Vector2(8, 0), new Vector2(8, 4), new Vector2(6, 4) };
        PolygonManager.GetManager().AddPolygon(p2);
    }
    public static void TopBrickBottomBantic()
    {
        List<Vector2> p1 = new List<Vector2> { new Vector2(0, 0), new Vector2(2, 0), new Vector2(4, 2), new Vector2(6, 0), new Vector2(8, 0), new Vector2(8, 2), new Vector2(6, 2), new Vector2(4, 2), new Vector2(2, 2), new Vector2(0, 2) };
        PolygonManager.GetManager().AddPolygon(p1);

        List<Vector2> p2 = new List<Vector2> { new Vector2(0, 2), new Vector2(2, 2), new Vector2(4, 2), new Vector2(6, 2), new Vector2(8, 2), new Vector2(8, 4), new Vector2(6, 4), new Vector2(2, 4), new Vector2(0, 4) };
        PolygonManager.GetManager().AddPolygon(p2);
    }
    public static void DuplicateVertices()
    {
        List<Vector2> p1 = new List<Vector2> { new Vector2(-4, 0), new Vector2(-2, 0), new Vector2(-2, 2), new Vector2(-2, 2), new Vector2(-4, 2) };
        PolygonManager.GetManager().AddPolygon(p1);

        List<Vector2> p2 = new List<Vector2> { new Vector2(0, 0), new Vector2(2, 0), new Vector2(2, 2), new Vector2(2, 2), new Vector2(0, 2) };
        PolygonManager.GetManager().AddPolygon(p2);
    }

    public static void ThreeTriangleTest()
    {
        Debug.Log("This test uses AddMode setting, check it for all settings to make sure the solutions are correct for all of them.");
        List<Vector2> p1 = new List<Vector2> { new Vector2(0, 0), new Vector2(3, 0), new Vector2(3, 3)};
        List<Vector2> p2 = new List<Vector2> { new Vector2(0, 0), new Vector2(3, 3), new Vector2(0, 3) };
        List<Vector2> p3 = new List<Vector2> { new Vector2(1.5f, 0.5f), new Vector2(2.5f, 1.5f), new Vector2(1.5f, 2.5f), new Vector2(0.5f, 1.5f) };
        bool has_compiled_1 = Poly2D.CompilePolygon(p1, out Poly2D poly1, Orientation.CounterClockwise);
        bool has_compiled_2 = Poly2D.CompilePolygon(p2, out Poly2D poly2, Orientation.CounterClockwise);
        bool has_compiled_3 = Poly2D.CompilePolygon(p3, out Poly2D poly3, Orientation.CounterClockwise);
        if (!(has_compiled_1 & has_compiled_2 & has_compiled_3)) { Debug.Log("Failed to compile polygons, test is not valid"); return; }
        PolygonManager.GetManager().AddPolygon(poly1);
        PolygonManager.GetManager().AddPolygon(poly2);
        PolygonManager.GetManager().AddPolygon(poly3);
    }

    public static void ReproducibleBadMarkingsTest()
    {
        Debug.Log("PolygonManager.GetManager().polygonAddMode был на секундочку переведен в режим FillHoles для этого теста");
        List<Vector2> p1 = new List<Vector2> { new Vector2(6.386561f, 5.482759f), new Vector2(-0.3787351f, -2.440023f), new Vector2(5.987429f, 0.5534713f), new Vector2(7.424306f, 4.065838f) };
        List<Vector2> p2 = new List<Vector2> { new Vector2(1.078099f, 5.502715f), new Vector2( 5.3089040f,  3.227659f), new Vector2( 5.62821f, 3.427226f ), new Vector2(2.814325f, 5.602498f), };
        bool has_compiled_1 = Poly2D.CompilePolygon(p1, out Poly2D poly1, Orientation.CounterClockwise);
        bool has_compiled_2 = Poly2D.CompilePolygon(p2, out Poly2D poly2, Orientation.CounterClockwise);
        if (!(has_compiled_1 & has_compiled_2)) { Debug.Log("Failed to compile polygons, test is not valid"); return; }
        CH2D_Chunk.PolygonAddMode cash = PolygonManager.GetManager().polygonAddMode;
        PolygonManager.GetManager().polygonAddMode = CH2D_Chunk.PolygonAddMode.FillHoles;
        PolygonManager.GetManager().AddPolygon(poly1);
        PolygonManager.GetManager().AddPolygon(poly2);
        PolygonManager.GetManager().polygonAddMode = cash;
    }
    // This one looks like a tent. (v below v)
    public static void TriangleAndSmallTriangleTwoOutsideCollinearsTent()
    {
        Debug.Log("PolygonManager.GetManager().polygonAddMode был на секундочку переведен в режим FillHoles для этого теста");
        List<Vector2> p1 = new List<Vector2> { new Vector2(0f, 0f), new Vector2(1, 0), new Vector2(3, 4), new Vector2(5, 0), new Vector2(6, 0), new Vector2(3, 5), };
        List<Vector2> p2 = new List<Vector2> { new Vector2(5, 0), new Vector2(3, 4), new Vector2(1, 0) };
        bool has_compiled_1 = Poly2D.CompilePolygon(p1, out Poly2D poly1, Orientation.CounterClockwise);
        bool has_compiled_2 = Poly2D.CompilePolygon(p2, out Poly2D poly2, Orientation.CounterClockwise);
        if (!(has_compiled_1 & has_compiled_2)) { Debug.Log("Failed to compile polygons, test is not valid"); return; }
        CH2D_Chunk.PolygonAddMode cash = PolygonManager.GetManager().polygonAddMode;
        PolygonManager.GetManager().polygonAddMode = CH2D_Chunk.PolygonAddMode.FillHoles;
        PolygonManager.GetManager().AddPolygon(poly1);
        PolygonManager.GetManager().AddPolygon(poly2);
        PolygonManager.GetManager().polygonAddMode = cash;
    }
    // This one looks like a ice cream. (v below v)
    public static void TriangleAndSmallTriangleTwoInsideCollinearsIceCream()
    {
        Debug.Log("PolygonManager.GetManager().polygonAddMode был на секундочку переведен в режим FillHoles для этого теста");
        List<Vector2> p1 = new List<Vector2> { new Vector2(0f, 0f), new Vector2(1, 0), new Vector2(3, -4), new Vector2(5, 0), new Vector2(6, 0), new Vector2(3, 5), };
        List<Vector2> p2 = new List<Vector2> { new Vector2(5, 0), new Vector2(1, 0), new Vector2(3, -4)};
        bool has_compiled_1 = Poly2D.CompilePolygon(p1, out Poly2D poly1, Orientation.CounterClockwise);
        bool has_compiled_2 = Poly2D.CompilePolygon(p2, out Poly2D poly2, Orientation.CounterClockwise);
        if (!(has_compiled_1 & has_compiled_2)) { Debug.Log("Failed to compile polygons, test is not valid"); return; }
        CH2D_Chunk.PolygonAddMode cash = PolygonManager.GetManager().polygonAddMode;
        PolygonManager.GetManager().polygonAddMode = CH2D_Chunk.PolygonAddMode.FillHoles;
        PolygonManager.GetManager().AddPolygon(poly1);
        PolygonManager.GetManager().AddPolygon(poly2);
        PolygonManager.GetManager().polygonAddMode = cash;
    }

    public static void CheckMarkAndTriangleHoleFilling()
    {
        Debug.Log("This test uses AddMode setting, check it for all settings to make sure the solutions are correct for all of them.");
        List<Vector2> p1 = new List<Vector2> { new Vector2(0f, 0f), new Vector2(3, 4), new Vector2(6, 0), new Vector2(3, 6)};
        List<Vector2> p2 = new List<Vector2> { new Vector2(2f, 0f), new Vector2(4, 0), new Vector2(3, 3) };
        List<Vector2> p3 = new List<Vector2> { new Vector2(-1f, 1f), new Vector2(-1, -3), new Vector2(7, -3), new Vector2(7, 1) };
        bool has_compiled_1 = Poly2D.CompilePolygon(p1, out Poly2D poly1, Orientation.CounterClockwise);
        bool has_compiled_2 = Poly2D.CompilePolygon(p2, out Poly2D poly2, Orientation.CounterClockwise);
        bool has_compiled_3 = Poly2D.CompilePolygon(p3, out Poly2D poly3, Orientation.CounterClockwise);
        if (!(has_compiled_1 & has_compiled_2 & has_compiled_3)) { Debug.Log("Failed to compile polygons, test is not valid"); return; }
        PolygonManager.GetManager().AddPolygon(poly1);
        PolygonManager.GetManager().AddPolygon(poly2);
        PolygonManager.GetManager().AddPolygon(poly3);

    }
}
[CustomEditor(typeof(DegeneratePolygonTest))]
public class DegeneratePolygonTestEditor : Editor 
{
    public override void OnInspectorGUI()
    {

        if (GUILayout.Button("Encompassed Degenerate")) DegeneratePolygonTest.AddEncompassedDegenerates();
        if (GUILayout.Button("Multiple Overlap Degenerate")) DegeneratePolygonTest.AddOverlapDegenerates();
        if (GUILayout.Button("Duplicate Vertices")) DegeneratePolygonTest.DuplicateVertices();
        base.OnInspectorGUI();
    }
}

