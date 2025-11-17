using UnityEditor;
using UnityEngine;
using System.Collections.Generic;
using System;


public class CH2D_Polygon : I_BBoxSupporter
{
    public List<CH2D_P_Index> vertices;
    public bool isHole;
    public bool convex;
    public Bounds BBox; public Bounds i_bounds { get { return BBox; } set { BBox = value; }}

    public void InsertPointIntoPolygon(CH2D_P_Index new_point, CH2D_P_Index A, CH2D_P_Index B)
    {
        int a_pos = -1;
        for (int i = 0; i < this.vertices.Count; i++)
            if (this.vertices[i] == A)
            {
                a_pos = A;
                break;
            }
        if (a_pos == -1) throw new Exception("Не получилось добавить точку, такой точки A нет в этом полигоне");
        int prev_b = (a_pos - 1 + this.vertices.Count) % this.vertices.Count;
        int next_b = (a_pos + 1) % this.vertices.Count;
        if (this.vertices[prev_b] == B)
        {
            this.vertices.Insert(prev_b, new_point);
            return;
        }
        if (this.vertices[next_b] == B)
        {
            this.vertices.Insert(a_pos, new_point);
            return;
        }
        throw new Exception("Не было совпадения по B, полигоны соприкасаются только в одной точке");

    }

}
