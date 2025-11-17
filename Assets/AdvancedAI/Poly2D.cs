using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using UnityEditor;

[System.Serializable]
public class Poly2D : I_BBoxSupporter
{
    public List<Vector2> vertices;
    public bool isHole;
    public bool convex;
    public Bounds BBox; public Bounds i_bounds { get { return BBox; } set { BBox = value; } }

    public Poly2D(List<Vector2> _vertices)
    {
        if (_vertices.Count < 3)
        {
            throw new ArgumentException("The number of vertices must be at least 3. Кастомная ошибка");
        }
        vertices = _vertices;
        CalculateBBox();
    }
    public Poly2D(params Vector2[] _vertices)
    {
        if (_vertices.Length < 3)
        {
            throw new ArgumentException("The number of vertices must be at least 3. Кастомная ошибка");
        }
        vertices = new List<Vector2>();
        vertices.AddRange(_vertices);
        CalculateBBox();
    }
    private Poly2D()
    {
        // Dummy poly
    }

    public static bool CompilePolygon(List<Vector2> points, out Poly2D out_poly)
    {
        out_poly = new Poly2D();
        if (points.Count < 3) return false;
        if (Poly2DToolbox.SelfIntersectionNaive(points)) return false;

        out_poly = new Poly2D(new List<Vector2>(points));
        out_poly.isHole = out_poly.IsCounterClockwise();
        out_poly.convex = Poly2DToolbox.IsConvex(out_poly.vertices, out_poly.isHole);
        //Debug.Log(out_poly.convex);
        return true;
    }

    public void CalculateBBox()
    {
        Bounds newBounds = new Bounds();
        newBounds.SetMinMax(vertices[0], vertices[0]);
        for (int i = 1; i < vertices.Count; i++)
        {
            newBounds.Encapsulate(vertices[i]);
        }
        this.BBox = newBounds;
    }
    /*
    public bool IsCounterClockwise()
    {
        Vector2 a = this.vertices[0]; Vector2 b = this.vertices[1]; Vector2 c = this.vertices[2];
        float cross = (b.x - a.x) * (c.y - a.y) - (b.y - a.y) * (c.x - a.x);
        return cross > 0;
    }*/

    public bool IsCounterClockwise()
    {
        return this.SignedArea() < 0;
    }

    public float BBoxArea()
    {
        return this.BBox.size.x * this.BBox.size.y; 
    }

    public void Orient(bool IsCounterClockwise)
    {
        if (this.IsCounterClockwise() == IsCounterClockwise) return;
        this.vertices.Reverse();
    }
    public float Area()
    {
        return Mathf.Abs(Poly2DToolbox.AreaShoelace(this.vertices));
    }
    public float SignedArea()
    {
        return Poly2DToolbox.AreaShoelace(this.vertices);
    }

    public bool BBoxLineIntersection(Vector2 A, Vector2 B)
    {
        return BoundsMathHelper.DoesLineIntersectBoundingBox2D(A, B, this.BBox);
    }

    public void DebugDrawSelf(Color color)
    {
        //Debug.Log(vertices.Count);
        for (int i = 0; i < vertices.Count - 1; i++)
        {
            DebugUtilities.DebugDrawLine(vertices[i], vertices[i+1], color);
        }
        DebugUtilities.DebugDrawLine(vertices[vertices.Count - 1], vertices[0], color);
    }
    public void HandlesDrawSelf(Color color)
    {
        //Debug.Log(vertices.Count);
        Color tmp = Handles.color;
        Handles.color = color;
        for (int i = 0; i < vertices.Count - 1; i++)
            Handles.DrawLine(vertices[i], vertices[i + 1]);
        
        Handles.DrawLine(vertices[vertices.Count - 1], vertices[0]);
        Handles.color = tmp;
    }

    public Vector2 AveragePoint()
    {
        Vector2 summ = Vector2.zero;
        for (int i = 0; i < vertices.Count; i++)
        {
            summ += vertices[i];
        }
        return summ / vertices.Count;
    }

    public bool IsInsidePolygon(Vector2 p)
    {
        if (this.convex) return Poly2DToolbox.IsInsidePolygonConvex(this.vertices, p, isHole);
        else             return Poly2DToolbox.IsPointInsidePolygon(p, this.vertices);
    }

    public List<Vector3> GetMesh()
    {
        Debug.Log("Not implemented");
        List<Vector3> meshList = new List<Vector3>();

        return meshList;
    }

    public static void SortListByCenters(List<Poly2D> list)
    {
        list.Sort(
            (a, b) => {
                Vector2 ac = a.BBox.center;
                Vector2 bc = b.BBox.center;
                int x_com = ac.x.CompareTo(bc.x);
                if (x_com != 0) return x_com;
                return ac.y.CompareTo(bc.y);
                }
            );
    }

}
