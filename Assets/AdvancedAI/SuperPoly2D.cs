using System.Collections.Generic;
using UnityEngine;

public class SuperPoly2D
{
    // Супер полигон, состоящий из нескольких полигонов или дырок. 
    // Этот класс должен содержать в себе функции для саморазбиения на выпуклые треугольнички.
    public List<Poly2D> polygons;
    public List<Poly2D> holes;
    public bool compiled; // Перед тем как использовать этого чувака надо скомпилировать

    public SuperPoly2D()
    {
        polygons = new List<Poly2D>();
        holes = new List<Poly2D>();
        compiled = false;
    }


    public void DebugDraw()
    {
        for (int i = 0; i < polygons.Count; i++)
        {
            for (int p1 = 0; p1 < polygons[i].vertices.Count; p1++)
            {
                int p2 = (p1 + 1) % polygons[i].vertices.Count;
                DebugUtilities.DebugDrawLine(polygons[i].vertices[p1], polygons[i].vertices[p2], Color.green);
            }
        }

        for (int i = 0; i < holes.Count; i++)
        {
            for (int p1 = 0; p1 < holes[i].vertices.Count; p1++)
            {
                int p2 = (p1 + 1) % holes[i].vertices.Count;
                DebugUtilities.DebugDrawLine(holes[i].vertices[p1], holes[i].vertices[p2], Color.red);
            }
        }

    }

    public void Compile()
    {
        List<Pair> pairs = new List<Pair>();
        for (int i = 0; i < polygons.Count; i++)
        {
            for (int j = i; j < polygons.Count; j++)
            {
                if (polygons[i].BBox.Intersects(polygons[j].BBox))
                {
                    pairs.Add(new Pair(i, j, false));
                }
            }
        }
        if (pairs.Count == 0) {
            this.compiled = true;
            return;
        }
    }

}
