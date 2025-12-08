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
    public Bounds BBox; public Bounds i_bounds { get { return BBox; } set { BBox = value; }}

    public void InsertPointIntoPolygon(CH2D_P_Index new_point, CH2D_P_Index A, CH2D_P_Index B)
    {
        InsertPointIntoPolygon(this.vertices, new_point, A, B);
    }

    public static void InsertPointIntoPolygon(List<CH2D_P_Index> points, CH2D_P_Index new_point, CH2D_P_Index A, CH2D_P_Index B)
    {
        Debug.Log(new_point + " " + A + " " +  B);
        string n = "";
        for (int i = 0; i < points.Count; i++) n += points[i] + " "; Debug.Log(n);

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

    // Встраивает коллинеарные точки в структуру полигона


}
