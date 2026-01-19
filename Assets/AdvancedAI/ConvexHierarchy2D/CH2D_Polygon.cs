using UnityEditor;
using UnityEngine;
using System.Collections.Generic;
using System;

[Serializable]
public class CH2D_Polygon : I_BBoxSupporter
{
    public List<CH2D_P_Index> vertices;
    public bool isHole;
    public bool convex;
    public bool initialized;
    public Bounds BBox; public Bounds i_bounds { get { return BBox; } set { BBox = value; }}

    public CH2D_Polygon()
    {
    }
    public CH2D_Polygon(List<CH2D_P_Index> vertices)
    {
        this.vertices = vertices;
        isHole = false;
        convex = false;
        initialized = false;
        BBox = new Bounds();
    }
    public void InsertPointIntoPolygon(CH2D_P_Index new_point, CH2D_P_Index A, CH2D_P_Index B)
    {
        InsertPointIntoPolygon(this.vertices, new_point, A, B);
    }

    public static void InsertPointIntoPolygon(List<CH2D_P_Index> points, CH2D_P_Index new_point, CH2D_P_Index A, CH2D_P_Index B)
    {
        //Debug.Log(new_point + " " + A + " " +  B);

        //string n = ""; for (int i = 0; i < points.Count; i++) n += points[i] + " "; Debug.Log(n);

        int a_pos = -1;
        for (int i = 0; i < points.Count; i++)
            if (points[i] == A)
            {
                a_pos = i;
                break;
            }
        if (a_pos == -1) throw new Exception("Не получилось добавить точку, такой точки A нет в этом полигоне");
        int prev_b = (a_pos - 1 + points.Count) % points.Count;
        int next_b = (a_pos + 1) % points.Count;
        //string pdesk = "Before: VCount (" + this.vertices.Count + ") "; for (int i = 0; i < vertices.Count; i++) pdesk += " " + vertices[i]; Debug.Log(pdesk);
        //Debug.Log( "NP: " + new_point + " A " + A + " B " + B + " Prev: " + prev_b + " (" + this.vertices[prev_b] + ") Curr: " + a_pos + " (" + this.vertices[a_pos] + ") Next: " + next_b + " (" + this.vertices[next_b] + ")");
        if (points[prev_b] == B)
        {
            points.Insert(a_pos, new_point);
            return;
        }
        if (points[next_b] == B)
        {
            points.Insert(next_b, new_point);
            return;
        }
        throw new Exception("Не было совпадения по B, полигоны соприкасаются только в одной точке");
    }

    public CH2D_Edge GetEdge(int e)
    {
        if (e < 0 | e >= this.vertices.Count) throw new Exception("Плохое ребро!!!!");
        return new CH2D_Edge(this.vertices[e], this.vertices[(e + 1) % vertices.Count]);
    }

    public void RecalculateBBox(List<Vector2> o_vertices)
    {
        for (int i = 0; i < o_vertices.Count; i++)
        {
            Debug.Log(o_vertices[i]);
        }
        Bounds b = new Bounds();
        b.SetMinMax(o_vertices[0], o_vertices[1]);
        Debug.Log(b.min + " " + b.max);
        for (int i = 0; i < o_vertices.Count; i++) b.Encapsulate(o_vertices[i]);
        this.BBox = b;
    }
    
    public float SignedArea(List<Vector2> o_vertices)
    {
        return Poly2DToolbox.AreaShoelace(o_vertices);
    }

    public bool IsCounterClockwise(List<Vector2> o_vertices)
    {
        return this.SignedArea(o_vertices) < 0;
    }
    public bool IsClockwise(List<Vector2> o_vertices)
    {
        return this.SignedArea(o_vertices) > 0;
    }
    public bool RecalculateOrientation(List<Vector2> o_vertices)
    {
        isHole = IsCounterClockwise(o_vertices);
        return isHole;
    }
    public bool RecalculateConvexity(List<Vector2> o_vertices)
    {
        this.convex = Poly2DToolbox.IsConvex(o_vertices, this.isHole);
        return this.convex;
    }

    // Встраивает коллинеарные точки в структуру полигона


}
