using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;


public class Poly2D
{
    public List<Vector2> vertices;
    public bool isHole;
    public bool convex;
    public Bounds BBox;

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

    public void CalculateBBox()
    {
        Bounds newBounds = new Bounds();
        newBounds.SetMinMax(vertices[0], vertices[1]);
        for (int i = 2; i < vertices.Count; i++)
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
        //Debug.Log(this.IsCounterClockwise() + " " + IsCounterClockwise + " " + SignedArea());
        //Debug.Log(this.IsCounterClockwise() == IsCounterClockwise);
        if (this.IsCounterClockwise() == IsCounterClockwise) return;
        this.vertices.Reverse();
        //Debug.Log(this.IsCounterClockwise() + " " + IsCounterClockwise + " " + SignedArea());
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

}
