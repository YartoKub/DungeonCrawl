using UnityEngine;
using System.Collections.Generic;
using UnityEditor;

public class PolygonTestBox : MonoBehaviour
{
    // ADD POLYGON TESTS FOR CHUNK AND GLOBAL MAP

    public enum PolygonTestCase { 
        two_boxes_apart,
        two_boxes_sharing_a_side,
        two_boxes_sharing_a_side_downward_offset,
        two_boxes_sharing_a_side_many_points,
        two_boxes_half_overlap,
        four_boxes_touching_corners_clover,
        four_boxes_touching_corners_00_10_11,
        two_croissants_sharing_point,
        ingyang_multiedge,
        collinear_sectors,
    }

    public static List<Poly2D> GetPolyList(PolygonTestCase testcase)
    {
        switch (testcase) { 
            case PolygonTestCase.two_boxes_apart: return TwoBoxesApart();
            case PolygonTestCase.two_boxes_sharing_a_side: return TwoBoxesSharingSide();
            case PolygonTestCase.two_boxes_sharing_a_side_downward_offset: return TwoBoxesSharingSideDownwardOffset();
            case PolygonTestCase.two_boxes_sharing_a_side_many_points: return TwoBoxesSharingSideManyPoints();
            case PolygonTestCase.two_boxes_half_overlap: return TwoBoxesHalfOverlap();
            case PolygonTestCase.four_boxes_touching_corners_clover: return TwoBoxesCornersTouchingClover();
            case PolygonTestCase.four_boxes_touching_corners_00_10_11: return TwoBoxesCornersTouching00_10_11();
            case PolygonTestCase.two_croissants_sharing_point: return TwoCroissantsSharingSinglePoint();
            case PolygonTestCase.ingyang_multiedge: return SolidIngYangTouchingMultiedges();
            case PolygonTestCase.collinear_sectors: return CollinearSectors120and240();
            default: return null;
        }

    }

    private static List<Poly2D> TwoBoxesApart()
    {
        List<Poly2D> polygons = new();
        polygons.Add(new Poly2D(new Vector2( 1, 0), new Vector2( 3, 0), new Vector2( 3, 2), new Vector2( 1, 2)));
        polygons.Add(new Poly2D(new Vector2(-3, 0), new Vector2(-1, 0), new Vector2(-1, 2), new Vector2(-3, 2)));
        return polygons;
    }

    private static List<Poly2D> TwoBoxesSharingSide()
    {
        List<Poly2D> polygons = new();
        polygons.Add(new Poly2D(new Vector2(0, 0), new Vector2(2, 0), new Vector2(2, 2), new Vector2(0, 2)));
        polygons.Add(new Poly2D(new Vector2(2, 0), new Vector2(4, 0), new Vector2(4, 2), new Vector2(2, 2)));
        return polygons;
    }
    private static List<Poly2D> TwoBoxesSharingSideDownwardOffset()
    {
        List<Poly2D> polygons = new();
        polygons.Add(new Poly2D(new Vector2(0, 0), new Vector2(2, 0), new Vector2(2, 2), new Vector2(0, 2)));
        polygons.Add(new Poly2D(new Vector2(2, -1), new Vector2(4, -1), new Vector2(4, 1), new Vector2(2, 1)));
        return polygons;
    }
    private static List<Poly2D> TwoBoxesSharingSideManyPoints()
    {
        List<Poly2D> polygons = new();
        polygons.Add(new Poly2D(new Vector2(0, -3), new Vector2(2, -3), new Vector2(2, 3), new Vector2(0, 3)));
        polygons.Add(new Poly2D(new Vector2(2, -2), new Vector2(4, -2), new Vector2(4, 2), new Vector2(2, 2), new Vector2(2, 1), new Vector2(2, 0), new Vector2(2, -1)));
        return polygons;
    }
    private static List<Poly2D> TwoBoxesHalfOverlap()
    {
        List<Poly2D> polygons = new();
        polygons.Add(new Poly2D(new Vector2(0, 0), new Vector2(2, 0), new Vector2(2, 2), new Vector2(0, 2)));
        polygons.Add(new Poly2D(new Vector2(1, 0), new Vector2(3, 0), new Vector2(3, 2), new Vector2(1, 2)));
        return polygons;
    }
    private static List<Poly2D> TwoBoxesCornersTouchingClover()
    {
        List<Poly2D> polygons = new();
        polygons.Add(new Poly2D(new Vector2(-5, -2), new Vector2(-1, -2), new Vector2(-1,  2), new Vector2(-5, 2)));
        polygons.Add(new Poly2D(new Vector2( 1, -2), new Vector2( 5, -2), new Vector2( 5,  2), new Vector2( 1, 2)));
        polygons.Add(new Poly2D(new Vector2(-2,  1), new Vector2( 2,  1), new Vector2( 2,  5), new Vector2(-2, 5)));
        polygons.Add(new Poly2D(new Vector2(-2, -5), new Vector2( 2, -5), new Vector2( 2, -1), new Vector2(-2, -1)));
        return polygons;
    }
    private static List<Poly2D> TwoBoxesCornersTouching00_10_11()
    {
        List<Poly2D> polygons = new();
        polygons.Add(new Poly2D(new Vector2(-5, -2), new Vector2(-1, -2), new Vector2(-1,  2), new Vector2(-5,  2)));
        polygons.Add(new Poly2D(new Vector2( 1, -6), new Vector2( 5, -6), new Vector2( 5, -2), new Vector2( 1, -2)));
        polygons.Add(new Poly2D(new Vector2(-2,  1), new Vector2( 2,  1), new Vector2( 2,  5), new Vector2(-2,  5)));
        polygons.Add(new Poly2D(new Vector2(-2, -5), new Vector2( 2, -5), new Vector2( 2, -1), new Vector2(-2, -1)));
        return polygons;
    }
    private static List<Poly2D> TwoCroissantsSharingSinglePoint()
    {
        List<Poly2D> polygons = new();
        polygons.Add(new Poly2D(new Vector2(-3, 0), new Vector2(0, 0), new Vector2(-2, 1), new Vector2(-1, 2), new Vector2(0, 0), new Vector2(0, 3), new Vector2(-3, 3)));
        polygons.Add(new Poly2D(new Vector2(0, -3), new Vector2(3, -3), new Vector2(3, 0), new Vector2(0, 0), new Vector2(2, -1), new Vector2(1, -2), new Vector2(0, 0)));
        return polygons;
    }
    /// <summary>
    /// Ing Yang shape, A has edge with multiple points that touches B's solid edge, and the other way around
    /// </summary>
    /// <returns></returns>
    private static List<Poly2D> SolidIngYangTouchingMultiedges()
    {
        List<Poly2D> polygons = new();
        polygons.Add(new Poly2D(new Vector2(0, -4), new Vector2(2, -4), new Vector2(4, -2), new Vector2(4, 2), new Vector2(2, 4), new Vector2(0, 4), new Vector2(0, 3), new Vector2(0, 2), new Vector2(0, 1), new Vector2(1, 1), new Vector2(1, -1), new Vector2(0, -1)));
        polygons.Add(new Poly2D(new Vector2(0, -4), new Vector2(0, -3), new Vector2(0, -2), new Vector2(0, -1), new Vector2(-1, -1), new Vector2(-1, 1), new Vector2(0, 1), new Vector2(0, 4), new Vector2(-2, 4), new Vector2(-4, 2), new Vector2(-4, -2), new Vector2(-2, -4)));
        return polygons;
    }

    private static List<Poly2D> CollinearSectors120and240()
    {   // Центральная точка коллинеарн
        // тестирование возможности установления корректной ориентации и порядка исходящих граней относительно центральной точки.
        // Это чисто тестовый полигон, сам по себе он имеет некорректную иерархию и может привести к неточной классиифкации наружных граней.
        // Для проверки порядка граней на пересечении
        // size = x4, y3
        List<Vector2> SmallSector = new List<Vector2>() { new Vector2(0, 0), new Vector2(0, 2), new Vector2(-2, -1), };
        List<Vector2> BigSector = new List<Vector2>() { new Vector2(0, 0), new Vector2(-2, -1), new Vector2(2, -1), new Vector2(0, 2), };
        List<Vector2> Rectangle = new List<Vector2>() { new Vector2(-3, 3), new Vector2(-3, -3), new Vector2(3, -3), new Vector2(3, 3), };
        List<Poly2D> polygons = new();
        
        polygons.Add(new Poly2D(ListPlusOffset(SmallSector, Rectangle[0])));
        polygons.Add(new Poly2D(ListPlusOffset(SmallSector, Rectangle[1])));
        SmallSector.Reverse();
        polygons.Add(new Poly2D(ListPlusOffset(SmallSector, Rectangle[2])));
        polygons.Add(new Poly2D(ListPlusOffset(SmallSector, Rectangle[3])));

        polygons.Add(new Poly2D(ListPlusOffset(BigSector, Rectangle[0])));
        polygons.Add(new Poly2D(ListPlusOffset(BigSector, Rectangle[3])));
        BigSector.Reverse();
        polygons.Add(new Poly2D(ListPlusOffset(BigSector, Rectangle[1])));
        polygons.Add(new Poly2D(ListPlusOffset(BigSector, Rectangle[2])));
        polygons.Add(new Poly2D(Rectangle));
        return polygons;
        List<Vector2> ListPlusOffset(List<Vector2> list, Vector2 offset)
        {
            List<Vector2> new_list = new List<Vector2>(list.Count);
            for (int i = 0; i < list.Count; i++) new_list.Add(list[i] + offset);
            return new_list;
        }
    }
    public static List<Poly2D> PizzaProcedural(int slices, float radius)
    {   // Питса. 
        List<Vector2> points = new List<Vector2>(slices);
        for (int i = 0; i < slices; i++)
        {
            float angle = 2 * Mathf.PI * i / slices;
            points.Add(new Vector2(Mathf.Cos(angle) * radius, Mathf.Sin(angle) * radius));
        }
        List<Poly2D> polygons = new(slices);
        for (int i = 0; i < slices; i++)
            polygons.Add(new Poly2D(points[i], points[(i + 1) % slices], Vector2.zero));
        
        return polygons;
    }


    /// <summary>
    /// A polygon with several problematic  properties: <br/>
    /// It features following issues: overlapping edges of two polygons, overlapping single points, <br/>
    /// It does not feature the following: self intersecting polygons, 0 length vertices, untriangulable polygons with 0 width doubledirectional edges, wrong CCW/CW orientations <br/>
    /// (!) CutPolyInt can handle overlapping edges, but it can not handle overlapping single points when there are more than 4 connected edges because of limited graph implementation
    /// </summary>
    private static List<Poly2D> ProblematicPolygon()
    {
        List<Poly2D> polygons = new();
        polygons.Add(new Poly2D(new Vector2(-3, 0), new Vector2(0, 0), new Vector2(-2, 1), new Vector2(-1, 2), new Vector2(0, 0), new Vector2(0, 3), new Vector2(-3, 3)));
        polygons.Add(new Poly2D(new Vector2(0, -3), new Vector2(3, -3), new Vector2(3, 0), new Vector2(0, 0), new Vector2(2, -1), new Vector2(1, -2), new Vector2(0, 0)));
        return polygons;
    }
    /// <summary>
    /// A polygon so offensively degenerate even league of legends players look normal in comparison. It features following issues:  <br/>
    /// overlapping edges of two polygons, 
    /// overlapping single points, 
    /// self intersecting polygons, 
    /// 0 length vertices, aka double vertices
    /// 0 width bidirectional edges, like extended hourglass
    /// wrong CCW/CW orientation sequences in hierarchy
    /// CCW and nested CW polygon of the same chunk sharing an edge.
    /// </summary>
    private static List<Poly2D> DegenerusMaximus()
    {
        // TODO: make the world polygon in the history of polygons
        return null;
    }

    public static void AddPolygons(List<Poly2D> polygons, PolygonManager.TargetDebugTestChunk target, CH2D_Chunk.PolygonAddMode mode)
    {
        for (int i = 0; i < polygons.Capacity; i++)
        {
            PolygonManager.GetManager().AddPolygon(polygons[i], target, mode);
        }
    }
    public static void AddPolygon(Poly2D polygon, PolygonManager.TargetDebugTestChunk target, CH2D_Chunk.PolygonAddMode mode)
    {
        PolygonManager.GetManager().AddPolygon(polygon, target, mode);
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

    // ADD POLYGON TESTS FOR DEBUGGING POLYGON BOOLEAN
    // Тут подразумевается что все тесты будут использовать Dangerous Add Polygon, чтобы полигоны могли накладываться друг на друга. 
    // Эти функции очищают чанк для добавления полигонов и легкости выбора индексов полигона


}
[CustomEditor(typeof(PolygonTestBox))]
public class DegeneratePolygonTestEditor : Editor 
{
    public override void OnInspectorGUI()
    {
        if (GUILayout.Button("Purge chunk fast")) PolygonManager.GetManager().PurgeChunk();
        if (GUILayout.Button("Encompassed Degenerate")) PolygonTestBox.AddEncompassedDegenerates();
        if (GUILayout.Button("Multiple Overlap Degenerate")) PolygonTestBox.AddOverlapDegenerates();
        if (GUILayout.Button("Duplicate Vertices")) PolygonTestBox.DuplicateVertices();
        if (GUILayout.Button("Boxes Apart"))
        {
            var l = PolygonTestBox.GetPolyList(PolygonTestBox.PolygonTestCase.two_boxes_apart);
            PolygonTestBox.AddPolygon(l[0], PolygonManager.TargetDebugTestChunk.first_leveled, CH2D_Chunk.PolygonAddMode.FillHoles);
            PolygonTestBox.AddPolygon(l[1], PolygonManager.TargetDebugTestChunk.second_leveled, CH2D_Chunk.PolygonAddMode.FillHoles);
        }
        if (GUILayout.Button("Boxes Side Touching"))
        {
            var l = PolygonTestBox.GetPolyList(PolygonTestBox.PolygonTestCase.two_boxes_sharing_a_side);
            PolygonTestBox.AddPolygon(l[0], PolygonManager.TargetDebugTestChunk.first_leveled, CH2D_Chunk.PolygonAddMode.FillHoles);
            PolygonTestBox.AddPolygon(l[1], PolygonManager.TargetDebugTestChunk.second_leveled, CH2D_Chunk.PolygonAddMode.FillHoles);
        }
        
        if (GUILayout.Button("Boxes Side Touching with offset"))
        {
            var l = PolygonTestBox.GetPolyList(PolygonTestBox.PolygonTestCase.two_boxes_sharing_a_side_downward_offset);
            PolygonTestBox.AddPolygon(l[0], PolygonManager.TargetDebugTestChunk.first_leveled, CH2D_Chunk.PolygonAddMode.FillHoles);
            PolygonTestBox.AddPolygon(l[1], PolygonManager.TargetDebugTestChunk.second_leveled, CH2D_Chunk.PolygonAddMode.FillHoles);
        }
        if (GUILayout.Button("Boxes Side Touching multipoint edge"))
        {
            var l = PolygonTestBox.GetPolyList(PolygonTestBox.PolygonTestCase.two_boxes_sharing_a_side_many_points);
            PolygonTestBox.AddPolygon(l[0], PolygonManager.TargetDebugTestChunk.first_leveled, CH2D_Chunk.PolygonAddMode.FillHoles);
            PolygonTestBox.AddPolygon(l[1], PolygonManager.TargetDebugTestChunk.second_leveled, CH2D_Chunk.PolygonAddMode.FillHoles);
        }
        if (GUILayout.Button("Boxes Half Overlap"))
        {
            var l = PolygonTestBox.GetPolyList(PolygonTestBox.PolygonTestCase.two_boxes_half_overlap);
            PolygonTestBox.AddPolygon(l[0], PolygonManager.TargetDebugTestChunk.first_leveled, CH2D_Chunk.PolygonAddMode.FillHoles);
            PolygonTestBox.AddPolygon(l[1], PolygonManager.TargetDebugTestChunk.second_leveled, CH2D_Chunk.PolygonAddMode.FillHoles);
        }
        if (GUILayout.Button("Boxes Corner touching clover shape"))
        {
            var l = PolygonTestBox.GetPolyList(PolygonTestBox.PolygonTestCase.four_boxes_touching_corners_clover);
            PolygonTestBox.AddPolygon(l[0], PolygonManager.TargetDebugTestChunk.first_leveled, CH2D_Chunk.PolygonAddMode.FillHoles);
            PolygonTestBox.AddPolygon(l[1], PolygonManager.TargetDebugTestChunk.first_leveled, CH2D_Chunk.PolygonAddMode.FillHoles);
            PolygonTestBox.AddPolygon(l[2], PolygonManager.TargetDebugTestChunk.second_leveled, CH2D_Chunk.PolygonAddMode.FillHoles);
            PolygonTestBox.AddPolygon(l[3], PolygonManager.TargetDebugTestChunk.second_leveled, CH2D_Chunk.PolygonAddMode.FillHoles);
        }
        if (GUILayout.Button("Boxes Corner touching chain shape"))
        {
            var l = PolygonTestBox.GetPolyList(PolygonTestBox.PolygonTestCase.four_boxes_touching_corners_00_10_11);
            PolygonTestBox.AddPolygon(l[0], PolygonManager.TargetDebugTestChunk.first_leveled, CH2D_Chunk.PolygonAddMode.FillHoles);
            PolygonTestBox.AddPolygon(l[1], PolygonManager.TargetDebugTestChunk.first_leveled, CH2D_Chunk.PolygonAddMode.FillHoles);
            PolygonTestBox.AddPolygon(l[2], PolygonManager.TargetDebugTestChunk.second_leveled, CH2D_Chunk.PolygonAddMode.FillHoles);
            PolygonTestBox.AddPolygon(l[3], PolygonManager.TargetDebugTestChunk.second_leveled, CH2D_Chunk.PolygonAddMode.FillHoles);
        }
        if (GUILayout.Button("Two croissants sharing a single point at corners"))
        {
            var l = PolygonTestBox.GetPolyList(PolygonTestBox.PolygonTestCase.two_croissants_sharing_point);
            PolygonTestBox.AddPolygon(l[0], PolygonManager.TargetDebugTestChunk.first_leveled, CH2D_Chunk.PolygonAddMode.FillHoles);
            PolygonTestBox.AddPolygon(l[1], PolygonManager.TargetDebugTestChunk.second_leveled, CH2D_Chunk.PolygonAddMode.FillHoles);
        }
        if (GUILayout.Button("Ing Yang multiedge"))
        {
            var l = PolygonTestBox.GetPolyList(PolygonTestBox.PolygonTestCase.ingyang_multiedge);
            PolygonTestBox.AddPolygon(l[0], PolygonManager.TargetDebugTestChunk.first_leveled, CH2D_Chunk.PolygonAddMode.FillHoles);
            PolygonTestBox.AddPolygon(l[1], PolygonManager.TargetDebugTestChunk.second_leveled, CH2D_Chunk.PolygonAddMode.FillHoles);
        }
        if (GUILayout.Button("Collinear Sectors"))
        {
            var l = PolygonTestBox.GetPolyList(PolygonTestBox.PolygonTestCase.collinear_sectors);
            for (int i = 0; i < 8; i++) PolygonTestBox.AddPolygon(l[i], PolygonManager.TargetDebugTestChunk.first_leveled, CH2D_Chunk.PolygonAddMode.RawAdd);
            PolygonTestBox.AddPolygon(l[8], PolygonManager.TargetDebugTestChunk.second_leveled, CH2D_Chunk.PolygonAddMode.RawAdd);
        }
        if (GUILayout.Button("Pizza Sectors"))
        {
            List<Poly2D> polygons = PolygonTestBox.PizzaProcedural(15, 2.0f);
            // Первые четыре просто добавлены
            for (int i = 0; i < 4; i++)
                PolygonTestBox.AddPolygon(polygons[i], PolygonManager.TargetDebugTestChunk.first_leveled, CH2D_Chunk.PolygonAddMode.RawAdd);
            // Тут они идут через раз.
            for (int i = 0; i < 4; i++)
                PolygonTestBox.AddPolygon(polygons[4 + i * 2], PolygonManager.TargetDebugTestChunk.first_leveled, CH2D_Chunk.PolygonAddMode.RawAdd);
            for (int i = 0; i < 3; i++)
                PolygonTestBox.AddPolygon(polygons[4 + 1 + i * 2], PolygonManager.TargetDebugTestChunk.first_leveled, CH2D_Chunk.PolygonAddMode.RawAdd);
            // Последние четыре переверныты, они - дырки
            for (int i = 0; i < 4; i++)
            {
                polygons[11 + i].Orient(true);
                PolygonTestBox.AddPolygon(polygons[11 + i], PolygonManager.TargetDebugTestChunk.first_leveled, CH2D_Chunk.PolygonAddMode.RawAdd);
            }
            PolygonTestBox.AddPolygon(new Poly2D(new Vector2(0,0), new Vector2(0, 5), new Vector2(-5, 5), new Vector2(-5, -5), new Vector2(0, -5)), PolygonManager.TargetDebugTestChunk.second_leveled, CH2D_Chunk.PolygonAddMode.RawAdd);
        }
            

        base.OnInspectorGUI();
    }
}

