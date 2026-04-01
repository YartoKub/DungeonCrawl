using UnityEngine;
using System.Collections.Generic;
using System;

// ћногоуровневый чанк
[Serializable]
public class CH2D_LeveledChunk : CH2D_Chunk
{
    List<int> hierarchy; // указатель на Id полигона который содержит полигон под индексом. ”казатель может быть -1, тогда родител€ нет.
    public Vector2 global_position;

    public CH2D_LeveledChunk()
    {
        this.hierarchy = new List<int>();
        this.polygons = new List<CH2D_Polygon>();
        this.vertices = new List<Vector2>();
        this.ConvexHull = new List<CH2D_P_Index>();
        if (this.connections == null) this.connections = new GraphDynamicList();
    }


    public override (bool, int) DangerousAddPolygon(CH2D_Polygon poly)
    {
        CompilePolygon(poly);
        this.polygons.Add(poly);
        this.hierarchy.Add(-1);
        this.connections.AddPoint();
        return (false, - 1);
    }

    protected override bool SoftDeletePolygon(int p)
    {   // Soft-Deletes polygon without removing points, for internal use in CutIntPoly functions
        if (p < 0 | p >= this.polygons.Count) return false;
        this.polygons.RemoveAt(p);
        this.hierarchy.RemoveAt(p);
        this.connections.DeletePoint(p);
        return true;
    }

    
}
