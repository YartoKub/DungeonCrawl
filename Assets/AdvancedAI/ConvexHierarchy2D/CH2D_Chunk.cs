using UnityEditor;
using UnityEngine;
using System.Collections.Generic;
using System;
using System.Linq;
// Границы чанка определены Convex Hull, также чанк имеет BBox, просто чтобы был
// Внутри чанка не должно быть пересекающихся полигонов, все полигоны находятся на одном уровне, в общем графе
[Serializable]
public class CH2D_Chunk
{
    public List<CH2D_Polygon> polygons;
    public IntMatrixGraph connections;
    public const UInt16 MaxVertices = 1000; //UInt16.MaxValue; // Если число вершин выходит за пределы этого числа, чанк нужно разбить на меньшие части
    [SerializeField] public List<Vector2> vertices;
    public List<CH2D_P_Index> ConvexHull;

    public CH2D_Chunk()
    {
        polygons = new List<CH2D_Polygon>();
        vertices = new List<Vector2>();
        ConvexHull = new List<CH2D_P_Index>();
    }

    public void AddPolygon(Poly2D poly)
    {
        //Debug.Log("Not implementes");
        for (int i = 0; i < polygons.Count; i++)
        {
            
        }
    }

    public void AddPolygonTrusted(Poly2D poly)
    {   // Используя эту функцию я доверяю себе что введу внутрь чанка нормальный полигон, а не говно.
        // Подохреваю что некоторую безопасность, такую как MutualVerticeIncorporation, можно оставить лишь на конечном этапе. Но мне лень это проверять
        CH2D_Polygon int_poly = new CH2D_Polygon();
        int_poly.isHole = poly.isHole;
        int_poly.convex = poly.convex;
        int_poly.BBox = poly.BBox;
        List<CH2D_P_Index> vertices = new List<CH2D_P_Index>(poly.vertices.Count);
        // Регистрация всех вершин
        for (int i = 0; i < poly.vertices.Count; i++)
            vertices.Add(AddPointIfNew(poly.vertices[i]));
        // Встройка всех коллинеарных вершин 
        List<int> p_overlap = BBoxOverlapList(poly.BBox);
        for (int i = 0; i < p_overlap.Count; i++)
        {
            //Incorporate_Bvertice_To_PolyA(vertices, this.polygons[p_overlap[i]].vertices);
            MutualVerticeIncorporation(vertices, this.polygons[p_overlap[i]].vertices);
        }
        string n = "poly so far: "; for (int i = 0; i < vertices.Count; i++) n += vertices[i] + " "; Debug.Log(n);

        // Встройка всех пересечений (добавляет новые точки к существующим полигонам)
        for (int i = 0; i < p_overlap.Count; i++)
            PolyPolyOnlineIntersectionOnesided(vertices, this.polygons[p_overlap[i]].vertices);
        
        // Встройка новых вершин-пересечений с предыдущего шага в старые полигоны
        for (int i = 0; i < p_overlap.Count; i++)
            Incorporate_Bvertice_To_PolyA(this.polygons[p_overlap[i]].vertices, vertices);
        

        int_poly.vertices = new List<CH2D_P_Index>(vertices);
        this.polygons.Add(int_poly);
    }
    /// <summary>
    /// Проверяет, какие полигоны пересекаются с точкой.
    /// Проверяет, какая грань полигона содержит точку.
    /// Точка может быть:<br/>
    /// Одной из вершин полигона, будет возвращен index совпадающей вершины<br/>
    /// Лежать отдельно, точка будет добавлена в список, будет возвращен ее index<br/>
    /// Лежать на одной из граней, точка будет добавлена в список, будет возвращен ее index, оба полигона делящие эту грань будут обновлены чтобы включить эту точку
    /// </summary>
    /// <param name="point"></param>
    public CH2D_P_Index AddPoint(Vector2 point)
    {
        if (this.vertices.Count + 1 >= MaxVertices) throw new Exception("Больше вершин чем разрешено");
        this.vertices.Add(point);
        Debug.Log(vertices.Count);
        return new CH2D_P_Index( this.vertices.Count - 1);
    }
    public CH2D_P_Index AddPointIfNew(Vector2 point)
    {
        for (int i = 0; i < this.polygons.Count; i++)
        {
            if (!this.polygons[i].BBox.Contains(point)) continue;
            for (int j = 0; j < this.polygons[i].vertices.Count; j++)
            {
                CH2D_P_Index p = this.polygons[i].vertices[j];
                if (Poly2DToolbox.PointSimilarity(point, this.vertices[p])) return p;
            }
        }
        return AddPoint(point);
    }
    // Кажется теперь это бесполезная функция, то что она делает решается при помощи MutualVerticeIncorporation(A, B)
    public CH2D_P_Index AddPointIfNewConvoluted(Vector2 point)
    {
        List <(int poly, Nullable<CH2D_P_Index> a, Nullable<CH2D_P_Index> b)> polys = DoesChunkHavePoint(point);
        if (polys.Count == 0) { Debug.Log("Добавлнеа НЕсуществующая точка"); return AddPoint(point); } // Нет похожего, создание новой точки
        // Похожая точка уже существует
        for (int i = 0; i < polys.Count; i++) // Похожая точка содержится в полигоне, возвращаем ее индекс
        {
            if (polys[i].b == null) { Debug.Log("Добавлена существующая точка точка " + polys[i].a.Value); return polys[i].a.Value; }
        }
        // Точка сидит на границе, тут редактируются один или два полигона с общей гранью
        Debug.Log("Добавлена точка на пересечении");
        CH2D_P_Index new_p = AddPoint(point);
        for (int i = 0; i < polys.Count; i++)
        {
            polygons[polys[i].poly].InsertPointIntoPolygon(new_p, polys[i].a.Value, polys[i].b.Value);
        }
        return new_p;
    }
    // Проверяет полигоны, чьи BBox содержат точку. 
    // Проверяет все грани этих полигонов, находит грани между которыми эта точка лежит.
    // Так как это Vector2 точка, а не Ch2D_P_index, то эта точка нова для полигонов
    // Следовательно она находится либо на границе с один полигоном, либо на границе между двемя полигонами.
    private List<(int, Nullable<CH2D_P_Index>, Nullable<CH2D_P_Index>)> DoesChunkHavePoint(Vector2 point)
    {   
        List<(int, Nullable<CH2D_P_Index>, Nullable<CH2D_P_Index>)> borderers = new List<(int, Nullable<CH2D_P_Index>, Nullable<CH2D_P_Index>)>(2);
        for (int i = 0; i < polygons.Count; i++)
        {
            if (!polygons[i].BBox.Contains(point)) continue;
            (Nullable<CH2D_P_Index> a, Nullable<CH2D_P_Index> b) = PointOnBorder(i, point);
            if (a == null) continue; // Тут может быть лишь два варианта: либо null+null, либо a+b.
            if (borderers.Count == 2) break;
            borderers.Add((i, a, b));
        }
        return borderers;
    }
    public (Nullable<CH2D_P_Index>, Nullable<CH2D_P_Index>) PointOnBorder(int poly, Vector2 point)
    {
        Nullable<CH2D_P_Index> a = null; Nullable<CH2D_P_Index> b = null;
        List<Vector2> border = GetPolyVertices(poly);
        (int int_a, int int_b) = Poly2DToolbox.PointOnBorder(point, border);
        if (int_a == -1) return (a, b);
        a = polygons[poly].vertices[int_a];
        b = int_b == -1 ? null : polygons[poly].vertices[int_b];
        return (a, b);
    }
    public List<int> BBoxOverlapList(Bounds BBox)
    {   // Возвращает полигоны, чьи BBox пересекаются с этим BBox. Сложность O(N)
        List<int> result = new List<int>();
        for (int i = 0; i < polygons.Count; i++) if (polygons[i].BBox.Intersects(BBox)) result.Add(i);
        return result;
    }

    public List<Vector2> GetPolyVertices(int PolyID)
    {
        List<Vector2> points = new List<Vector2>(polygons[PolyID].vertices.Count);
        for (int i = 0; i < polygons[PolyID].vertices.Count; i++)
            points.Add(this.vertices[polygons[PolyID].vertices[i]]);
        return points;
    }
    public Poly2D GetPoly2D(int PolyID)
    {   // Проблема централизованного хранения вершин - нужда пересобирать полигоны для операций
        Poly2D toReturn = new Poly2D();
        toReturn.vertices = GetPolyVertices(PolyID);
        toReturn.BBox = this.polygons[PolyID].BBox;
        toReturn.isHole = this.polygons[PolyID].isHole;
        toReturn.convex = this.polygons[PolyID].convex;
        return toReturn;
    }

    public int GetPolygonNeighbouringEdge(int poly, CH2D_P_Index A, CH2D_P_Index B)
    {   
        for (int i = 0; i < polygons.Count; i++)
        {
            if (poly == i) continue;
            if (!this.polygons[poly].BBox.Intersects(this.polygons[i].BBox)) continue;
            if (DoPolygonsShareEdge(poly, i, A, B)) return i;
        }
        return -1;
    }
    public bool DoPolygonsShareEdge(int polyA, int polyB, CH2D_P_Index start, CH2D_P_Index end)
    {   // Сравнивает индексы вершин. Если их значение isHole равны, то грани смотрят в противоположные стороны, если isHole разнятся - то грани смотрят в одну сторону
        (bool a_success, int a_prev_i, CH2D_P_Index a_prev_v, int a_curr_i, CH2D_P_Index a_curr_v, int a_next_i, CH2D_P_Index a_next_v) = GetSurrounds(polyA, start);
        if (!a_success) return false;
        if (a_next_v != end) return false;
        (bool b_success, int b_prev_i, CH2D_P_Index b_prev_v, int b_curr_i, CH2D_P_Index b_curr_v, int b_next_i, CH2D_P_Index b_next_v) = GetSurrounds(polyB, start);
        if (!b_success) return false;
        if (this.polygons[polyA].isHole == this.polygons[polyB].isHole)
            return b_prev_v == end;
        else
            return b_next_v == end;
    }
    /// <summary>
    /// Волшебная функция, которая находит точку Point в полигоне, и возвращает ее index, а также индексы и значения своих соседей.
    /// </summary>
    /// <param name="Poly"></param>
    /// <param name="Point"></param>
    /// <returns>success - whether Point belongs to Poly<br/>prev_i, curr_i, next_i - индексы предыдущей, этой (Point), следующей точек<br/>prev_v, curr_v, next_v - значения предыдущей, этой (Point), следующей точек</returns>
    public (bool success, int prev_i, CH2D_P_Index prev_v, int curr_i, CH2D_P_Index curr_v, int next_i, CH2D_P_Index next_v) GetSurrounds(int Poly, CH2D_P_Index Point)
    {
        int curr_i = -1; int PVCount = this.polygons[Poly].vertices.Count;
        for (int i = 0; i < PVCount; i++)
            if (this.polygons[Poly].vertices[i] == Point) { curr_i = i; break; }
        if (curr_i == -1) return (false, -1, new CH2D_P_Index(), -1, new CH2D_P_Index(), -1, new CH2D_P_Index());
        int prev_i = (curr_i - 1 + PVCount) % PVCount;
        int next_i = (curr_i + 1) % PVCount;
        return (true,
            prev_i, new CH2D_P_Index(this.polygons[Poly].vertices[prev_i]),
            curr_i, Point,
            next_i, new CH2D_P_Index(this.polygons[Poly].vertices[next_i]));
    }
    /// <summary>
    /// Предполагается что будут даны полигоны, полученные в результате проверки BBoxOverlapList пересечения или же 
    /// </summary>
    public List<int> GetPolygonsOwningPoint(CH2D_P_Index point, List<int> loc_polygons)
    {
        List<int> to_return = new List<int>();
        for (int i = 0; i < loc_polygons.Count; i++)
        {
            CH2D_Polygon p = this.polygons[loc_polygons[i]];
            if (p.vertices.Contains(point)) to_return.Add(i);
        }
        return to_return;
    }

    public (bool found, CH2D_P_Index A , CH2D_P_Index B) GetSharedEdge(int polyA, int polyB)
    {
        Debug.Log("Перепиши меня чтобы имспользовать EdgesInsideBounds()!!!");
        Bounds boundsA = this.polygons[polyA].BBox;
        Bounds boundsB = this.polygons[polyB].BBox;
        if (!boundsA.Intersects(boundsB)) return (false, new CH2D_P_Index(0), new CH2D_P_Index(0));

        List<int> Bpoint_insideA = new List<int>();
        List<int> Apoint_insideB = new List<int>();

        List<Vector2> verticesA = GetPolyVertices(polyA); List<CH2D_P_Index> verticesA_index = this.polygons[polyA].vertices;
        List<Vector2> verticesB = GetPolyVertices(polyA); List<CH2D_P_Index> verticesB_index = this.polygons[polyB].vertices;

        for (int i = 0; i < verticesA.Count; i++)
            if (boundsB.Contains(verticesA[i])) Apoint_insideB.Add(i);
        for (int i = 0; i < verticesB.Count; i++)
            if (boundsA.Contains(verticesB[i])) Bpoint_insideA.Add(i);

        if (Apoint_insideB.Count == 0 | Bpoint_insideA.Count == 0) return (false, new CH2D_P_Index(0), new CH2D_P_Index(0));

        for (int a = 0; a < Apoint_insideB.Count; a++)
        {
            for (int b = 0; b < Bpoint_insideA.Count; b++)
            {
                CH2D_P_Index A = verticesA_index[a]; CH2D_P_Index B = verticesB_index[b];
                if (A != B) continue;
                CH2D_P_Index next_A = verticesA_index[(a + 1) % verticesA_index.Count];
                CH2D_P_Index prev_A = verticesA_index[(a - 1 + verticesA_index.Count) % verticesA_index.Count];
                CH2D_P_Index next_B = verticesB_index[(b + 1) % verticesB_index.Count];
                CH2D_P_Index prev_B = verticesB_index[(b - 1 + verticesB_index.Count) % verticesB_index.Count];
                if (next_A == prev_B) return (true, A, next_A);
                if (prev_A == prev_B) return (true, prev_A, A);
                if (next_A == next_B) return (true, A, next_A);
                if (prev_A == next_B) return (true, prev_A, A);
            }
        }
        return (false, new CH2D_P_Index(0), new CH2D_P_Index(0));
    }

    public void DebugDrawSelf()
    {
        foreach (var poly in polygons)
        {
            for (int i = 0; i < poly.vertices.Count - 1; i++)
                Debug.DrawLine(this.vertices[poly.vertices[i]], this.vertices[poly.vertices[i + 1]], Color.blue);
            Debug.DrawLine(this.vertices[poly.vertices[poly.vertices.Count - 1]], this.vertices[poly.vertices[0]], Color.blue);
        }
    }

    public void HandlesDrawSelf()
    {
        Color tmp = Handles.color;
        Handles.color = Color.blue;

        //Debug.Log(polygons.Count);
        for (int i = 0; i < this.polygons.Count; i++)
            HandlesDrawPolyOutline(i, Color.blue);
        
        Handles.color = tmp;
    }
    public void HandlesDrawPolyOutline(int p, Color color)
    {
        Color tmp = Handles.color;
        Handles.color = color;
        List<CH2D_P_Index> v = this.polygons[p].vertices;
        for (int i = 0; i < v.Count - 1; i++)
            Handles.DrawLine(this.vertices[v[i]], this.vertices[v[i + 1]]);
        Handles.DrawLine(this.vertices[v[v.Count - 1]], this.vertices[v[0]]);
        Handles.color = tmp;
    }
    public void HandlesDrawPolyOutlineDirected(int p, Color filled_color, Color hole_color)
    {
        Color c = this.polygons[p].isHole ? hole_color : filled_color;
        List<CH2D_P_Index> v = this.polygons[p].vertices;
        for (int i = 0; i < v.Count - 1; i++)
            DebugUtilities.HandlesDrawLine(this.vertices[v[i]], this.vertices[v[i + 1]], c, 0.2f);
        DebugUtilities.HandlesDrawLine(this.vertices[v[v.Count - 1]], this.vertices[v[0]], c, 0.2f);
    }
    public void HandlesDrawPolyBBox(int p, Color color) { DebugUtilities.HandlesDrawRectangle(this.polygons[p].BBox.min, this.polygons[p].BBox.max, color); }
    public void HandlesDrawPolyPoints(int p, Color color) { for (int i = 0; i < this.polygons[p].vertices.Count; i++) DebugUtilities.HandlesDrawCross(this.vertices[this.polygons[p].vertices[i]], color); }
    public void DebugDumpChunkData()
    {
        string ret = "DebugChunkData: PolyCount: " + this.polygons.Count + " \n";
        for (int i = 0; i < polygons.Count; i++)
        {
            ret += "(P" + i + ") ";
            if (polygons[i].vertices == null) throw new Exception("Полигон с index " + i + " не имеет списка вершин!");
            ret += " (VCount: " + polygons[i].vertices.Count + " ) ";
            ret += " { ";
            for (int v = 0; v < polygons[i].vertices.Count; v++)
            {
                ret += polygons[i].vertices[v] + " ";
            }
            ret += "} \n";
        }
        Debug.Log(ret);
    }
    // Идея в том чтобы найти все пересечения и добавить точки в список
    public void DebugGetIntersections(bool DrawIntersections, bool FindInnsAndOuts)
    {
        if (this.polygons.Count < 2) return;

        Incorporate_Bvertice_To_PolyA(this.polygons[0].vertices, this.polygons[1].vertices);
        Incorporate_Bvertice_To_PolyA(this.polygons[1].vertices, this.polygons[0].vertices);
        // Point incorporations
        List<CH2D_Intersection> intersections = GetPolyPolyIntersections(0, 1);
        Debug.Log(intersections.Count);
        if (DrawIntersections)
            for (int i = 0; i < intersections.Count; i++)
            {
                CH2D_Intersection ii = intersections[i];
                Debug.Log(ii.a_e1 + " " + ii.a_e2 + " " + ii.b_e1 + " " + ii.b_e2);
                DebugUtilities.DebugDrawCross(intersections[i].pos, Color.red, 10.0f);
            }
        if (!FindInnsAndOuts) return;

        List<Pair> pairs = new List<Pair>(intersections.Count);
        for (int i = 0; i < intersections.Count; i++)
        {
            pairs.Add(new Pair(intersections[i].a_e1, intersections[i].a_e2, false));
        }

        GHPolygonMerge.GH_IntList(vertices, polygons[0].vertices, polygons[1].vertices, GetPolyVertices(0), GetPolyVertices(1), pairs);
    }

    public void DebugAddTestPolygon()
    {
        Poly2D degenerate1 = new Poly2D(new List<Vector2>() { new Vector2(0, 0), new Vector2(0, 2), new Vector2(2, 2), new Vector2(2, 0) });
        Poly2D degenerate2 = new Poly2D(new List<Vector2>() { new Vector2(1, 0), new Vector2(1, 2), new Vector2(3, 2), new Vector2(3, 0) });
        this.AddPolygonTrusted(degenerate1);
        this.AddPolygonTrusted(degenerate2);
    }

    // Incorporate collinear vertices
    // Совпадающие вершины должны 
    public void Incorporate_Bvertice_To_PolyA(List<CH2D_P_Index> a_v, List<CH2D_P_Index> b_v)
    {
        for (int a = 0; a < a_v.Count; a++)
        {
            CH2D_Edge ae = new CH2D_Edge(a_v[a], a_v[(a + 1) % a_v.Count]);
            for (int b = 0; b < b_v.Count; b++)
            {
                CH2D_P_Index bv = b_v[b];
                if (bv == ae.A | bv == ae.B) continue;
                //Debug.Log(bv + " " + ae.A + " " + ae.B);
                bool success = Poly2DToolbox.PointBelongToLine2D(vertices[ae.A], vertices[ae.B], vertices[bv]);
                if (!success) continue;
                CH2D_Polygon.InsertPointIntoPolygon(a_v, bv, ae.A, ae.B);// polygons[A].InsertPointIntoPolygon(bv, ae.A, ae.B);
                //a = a - 1;
                break;
            }
        }
    }
    public void MutualVerticeIncorporation(List<CH2D_P_Index> a_v, List<CH2D_P_Index> b_v)
    {
        Incorporate_Bvertice_To_PolyA(a_v, b_v);
        Incorporate_Bvertice_To_PolyA(b_v, a_v);
    }
    public void MutualVerticeIncorporation(int a_i, int b_i)
    {
        MutualVerticeIncorporation(this.polygons[a_i].vertices, this.polygons[b_i].vertices);
    }
    public void PolyPolyIntersection(int A, int B)
    {
        List<CH2D_Intersection> intersections = GetPolyPolyIntersections(A, B);
        for (int i = 0; i < intersections.Count; i++)
        {
            CH2D_P_Index p_i = AddPointIfNew(intersections[i].pos);
            this.polygons[A].InsertPointIntoPolygon(p_i, intersections[i].a_e1, intersections[i].a_e2);
            this.polygons[B].InsertPointIntoPolygon(p_i, intersections[i].b_e1, intersections[i].b_e2);
        }
    }
    // Бля, а оба полигона должны существовать, тоесть эта штука неприменима во время добавления нового полигона, которого еще нет в спискe
    private List<CH2D_Intersection> GetPolyPolyIntersections(int a_p, int b_p)
    {
        return GetPolyPolyIntersections(this.polygons[a_p].vertices, this.polygons[b_p].vertices, this.polygons[a_p].BBox, this.polygons[b_p].BBox, a_p, b_p);
    }
    private List<CH2D_Intersection> GetPolyPolyIntersections(List<CH2D_P_Index> a_v, List<CH2D_P_Index> b_v, Bounds a_bbox, Bounds b_bbox, int a_i, int b_i)
    {
        List<CH2D_Intersection> intersections = new List<CH2D_Intersection>();

        List<CH2D_Edge> a_edges = EdgesInsideBounds(a_v, b_bbox);
        List<CH2D_Edge> b_edges = EdgesInsideBounds(b_v, a_bbox);

        for (int a = 0; a < a_edges.Count; a++)
        {
            for (int b = 0; b < b_edges.Count; b++)
            {
                CH2D_P_Index a1 = a_edges[a].A;
                CH2D_P_Index a2 = a_edges[a].B;
                CH2D_P_Index b1 = b_edges[b].A;
                CH2D_P_Index b2 = b_edges[b].B;
                //Debug.Log(a + " " + b + " " + a1 + " " + a2 + " " + b1 + " " + b2);
                if (!Poly2DToolbox.LineLineIntersection(this.vertices[a1], this.vertices[a2], this.vertices[b1], this.vertices[b2], out Vector2 inter, out float t)) continue;
                if (Poly2DToolbox.PointSimilarity(inter, this.vertices[b1]) | Poly2DToolbox.PointSimilarity(inter, this.vertices[b2])) continue;
                intersections.Add(new CH2D_Intersection(a_i, b_i, a1, a2, b1, b2, inter, t));
            }
        }
        return intersections;
    }
    // Делит полигон А об полигон В, добавляет в полигон А новые точки по мере поиска пересечений. 
    private void PolyPolyOnlineIntersectionOnesided(List<CH2D_P_Index> A, List<CH2D_P_Index> B)
    {
        string n = "New points added: ";
        for (int a = 0; a < A.Count; a++)
        {
            int a1 = A[a];
            int a2 = A[(a + 1) % A.Count];
            for (int b = 0; b < B.Count; b++)
            {
                int b1 = B[b];
                int b2 = B[(b + 1) % B.Count];
                Debug.Log(a1 + " " + a2 + " " + b1 + " " + b2);
                if (!Poly2DToolbox.LineLineIntersection(this.vertices[a1], this.vertices[a2], this.vertices[b1], this.vertices[b2], out Vector2 inter, out float t)) { Debug.Log(inter); continue; }
                if (Poly2DToolbox.PointSimilarity(inter, this.vertices[b1]) | Poly2DToolbox.PointSimilarity(inter, this.vertices[b2])) { Debug.Log("similar to B"); continue; }
                if (Poly2DToolbox.PointSimilarity(inter, this.vertices[a1]) | Poly2DToolbox.PointSimilarity(inter, this.vertices[a2])) { Debug.Log("similar to A"); continue; }
                CH2D_P_Index np = AddPoint(inter);
                Debug.Log(a2);
                A.Insert(a + 1, np);
                n += "( " + np + " " + inter + " ) ";
                a--;
                break;
            }
        }
        Debug.Log(n);
    }

    private List<CH2D_P_Index> PointsInsideBounds(List<CH2D_P_Index> polyA, Bounds bounds)
    {
       return polyA.FindAll(p => bounds.Contains(this.vertices[polyA[p]]));
    }
    private List<CH2D_Edge> EdgesInsideBounds(List<CH2D_P_Index> polyA, Bounds bounds)
    {
        List< CH2D_Edge > edges = new List<CH2D_Edge>();
        int c = polyA.Count;
        for (int i = 0; i < c; i++)
        {
            int j = (i + 1) % c;
            if (!BoundsMathHelper.DoesLineIntersectBoundingBox2D(this.vertices[polyA[i]], this.vertices[polyA[j]], bounds)) continue;
            edges.Add(new CH2D_Edge(polyA[i], polyA[j]));
        }
        return edges;
        //return polyA.FindAll(p => BoundsMathHelper.DoesLineIntersectBoundingBox2D(this.vertices[p], this.vertices[(p+1)%c], bounds));
    }
    private List<CH2D_Edge> EdgesInsideBounds(int p_i, Bounds bounds)
    {
        List<CH2D_Edge> edges = new List<CH2D_Edge>();
        int c = polygons[p_i].vertices.Count;
        for (int i = 0; i < c; i++)
        {
            int j = (i + 1) % c;
            if (!BoundsMathHelper.DoesLineIntersectBoundingBox2D(this.vertices[polygons[p_i].vertices[i]], this.vertices[polygons[p_i].vertices[j]], bounds)) continue;
            edges.Add(new CH2D_Edge(polygons[p_i].vertices[i], polygons[p_i].vertices[j]));
        }
        return edges;
    }

    private struct CH2D_Intersection
    {
        public int polyA;
        public int polyB;
        public CH2D_P_Index a_e1;
        public CH2D_P_Index a_e2;
        public CH2D_P_Index b_e1;
        public CH2D_P_Index b_e2;
        public float distance_from_e1;
        public Vector2 pos;
        public CH2D_Intersection(int polyA, int polyB, CH2D_P_Index a_e1, CH2D_P_Index a_e2, CH2D_P_Index b_e1, CH2D_P_Index b_e2, Vector2 pos, float distance_from_e1)
        {
            //this.new_point = new_point;
            this.polyA = polyA;
            this.polyB = polyB;
            this.a_e1 = a_e1;
            this.a_e2 = a_e2;
            this.b_e1 = b_e1;
            this.b_e2 = b_e2;
            this.pos = pos;
            this.distance_from_e1 = distance_from_e1;
        }
    }

    public List<int> PolygonPointIntersection(Vector2 p)
    {
        List<int> result = new List<int>();
        for (int i = 0; i < polygons.Count; i++)
        {
            if (!polygons[i].BBox.Contains(p)) continue;
            List<Vector2> points = GetPolyVertices(i);
            if (Poly2DToolbox.IsPointInsidePolygon(p, points)) result.Add(i);
        }
        return result;
    }

    public string GetDebugData(int p)
    {
        if (p < 0 | p >= polygons.Count) return "No polygon selected\n";
        List<Vector2> points = GetPolyVertices(p);
        string to_return = "Polygon index: " + p + (this.polygons[p].isHole ? " <color=red>Hole</color>" : " <color=green>Fill</color>" + "\n");
        string p_list = "{";
        for (int i = 0; i < this.polygons[p].vertices.Count; i++) p_list += this.polygons[p].vertices[i] + " ";
        p_list += "}\n";
        string area = "Area: " + Poly2DToolbox.AreaShoelace(points) + "\n";

        return to_return + p_list + area;
    }

    // Функция для уничтожения точек что не принадлежат ни одному полигону.
    // Цель - снизу вверх уничтожать по одной точке, переименовывая точки в полигонах
    // Блин, задача удаления точки из списка точек - на удивление одна из самых дорогих задач в этьом коде
    public void PurgeUnusedPoints()
    {

    }

}


