using UnityEditor;
using UnityEngine;
using System.Collections.Generic;
using System;
// Границы чанка определены Convex Hull, также чанк имеет BBox, просто чтобы был
public class CH2D_Chunk
{
    public List<CH2D_Polygon> polygons;
    public IntMatrixGraph connections;
    public const int MaxVertices = 1000; //UInt16.MaxValue; // Если число вершин выходит за пределы этого числа, чанк нужно разбить на меньшие части
    public List<Vector2> vertices;
    public List<CH2D_P_Index> ConvexHull;


    public void AddPolygon(Poly2D poly)
    {
        for (int i = 0; i < polygons.Count; i++)
        {
            
        }
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
        return new CH2D_P_Index( this.vertices.Count - 1);
    }
    public CH2D_P_Index AddPointIfNew(Vector2 point)
    {
        (int poly, Nullable<CH2D_P_Index> a, Nullable<CH2D_P_Index> b) = DoesChunkHavePoint(point);
        if (poly == -1 | a == null) return AddPoint(point);
        if (b == null) return a.Value;

        // add and edit

        return new CH2D_P_Index(-1);
    }
    public (int, Nullable<CH2D_P_Index>, Nullable<CH2D_P_Index>) DoesChunkHavePoint(Vector2 point)
    {
        List<int> containers = new List<int>();
        for (int i = 0; i < polygons.Count; i++)
            if (polygons[i].BBox.Contains(point)) containers.Add(i);

        Nullable<CH2D_P_Index> a = null; Nullable<CH2D_P_Index> b = null; int p = -1;
        for (int i = 0; i < containers.Count; i++)
        {
            List<Vector2> border = GetPolyVertices(containers[i]);
            (int int_a, int int_b) = Poly2DToolbox.PointOnBorder(point, border);
            if (int_a == -1) continue;
            a = polygons[containers[i]].vertices[int_a];
            b = int_b == -1 ? null : polygons[containers[i]].vertices[int_b];
            p = containers[i];
            break;
        }
        return (p, a, b);
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
        int AVCount = this.polygons[polyA].vertices.Count;
        int BVCount = this.polygons[polyB].vertices.Count;

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
    /// Волшебная функция, которая находит точку Point в полигоне, и возвращает ее index, а также index-ы и значения своих соседей.
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

    public (bool found, CH2D_P_Index A , CH2D_P_Index B) GetSharedEdge(int polyA, int polyB)
    {
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



}


