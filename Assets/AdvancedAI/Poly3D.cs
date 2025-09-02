using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Poly3D
{
    public List<Vector3> vertices;
    public Plane plane;
    public bool convex;
    public float w; // w = Vector3.Dot(plane.normal, plane.normal * -plane.distance);


    public Poly3D(List<Vector3> _vertices)
    {
        vertices = _vertices;
        plane = new Plane(_vertices[0], _vertices[1], _vertices[2]);
        w = Vector3.Dot(plane.normal, _vertices[0]);
    }
    public Poly3D(params Vector3[] _vertices)
    {
        vertices = new List<Vector3>();
        vertices.AddRange(_vertices);
        plane = new Plane(_vertices[0], _vertices[1], _vertices[2]);

        w = Vector3.Dot(plane.normal, _vertices[0]);
    }

    enum Type
    {
        SamePlane = 0,
        Front = 1,
        Back = 2,
        Intersects = 3
    };
    public void Flip()
    {
        vertices.Reverse();
        plane.Flip();
    }

    public void CutPolygon(Plane Oplane, List<Vector3> coplanarFront, List<Vector3> coplanarBack, List<Vector3> front, List<Vector3> back)
    {
        Debug.Log("UNIMPLEMENTED GIVES WRONG DIVISIONS change Oplane.distance to calculated w or recalculate w");
        Type polyType = 0;
        List<Type> types = new List<Type>();

        for (int i = 0; i < this.vertices.Count; i++)
        {
            float t = Vector3.Dot(Oplane.normal, this.vertices[i]) - Oplane.distance;
            Type type = (t < -Geo3D.epsilon) ? Type.Back : ((t > Geo3D.epsilon) ? Type.Front : Type.SamePlane);
            polyType |= type;
            types.Add(type);
        }

        switch (polyType) {
            case Type.SamePlane:
                {
                    if (Vector3.Dot(Oplane.normal, this.plane.normal) > 0) coplanarFront.AddRange(this.vertices);
                    else coplanarBack.AddRange(this.vertices);
                }
                break;
            case Type.Front: front.AddRange(this.vertices); break;
            case Type.Back: back.AddRange(this.vertices); break;
            case Type.Intersects:
                {
                    List<Vector3> f = new List<Vector3>();
                    List<Vector3> b = new List<Vector3>();

                    for (int i = 0; i < this.vertices.Count; i++)
                    {
                        int j = (i + 1) % this.vertices.Count;
                        Type ti = types[i], tj = types[j];
                        Vector3 vi = this.vertices[i], vj = this.vertices[j];

                        if (ti != Type.Back) f.Add(vi);
                        if (ti != Type.Front) b.Add(vi);

                        if ((ti | tj) == Type.Intersects)
                        {
                            float t = (Oplane.distance - Vector3.Dot(Oplane.normal, vi)) / Vector3.Dot(Oplane.normal, vj - vi);
                            Vector3 v = Mix(vi, vj, t);
                            f.Add(v);
                            b.Add(v);
                        }
                    }
                    front.AddRange(f);
                    back.AddRange(b);
                }
                break;
        }
    }
    public Vector3 CutLine(Vector3 vertexA, Vector3 vertexB)
    {
        Vector3 normal = Vector3.Cross(vertices[1] - vertices[0], vertices[2] - vertices[0]);
        float w = Vector3.Dot(normal, vertices[0]);
        float t = (w - Vector3.Dot(normal, vertexA)) / Vector3.Dot(normal, vertexB - vertexA);
        Vector3 tureturn = vertexA * (1f - t) + vertexB * t;

        return tureturn;
    }
    public bool CutLineSafe(Vector3 vertexA, Vector3 vertexB, out Vector3 tureturn)
    {
        Vector3 normal = Vector3.Cross(vertices[1] - vertices[0], vertices[2] - vertices[0]).normalized;
        float w = Vector3.Dot(normal, vertices[0]);  // EDITED HERE 
        float t = (w - Vector3.Dot(normal, vertexA)) / Vector3.Dot(normal, vertexB - vertexA);
        tureturn = vertexA * (1f - t) + vertexB * t;

        float dot = Vector3.Dot(normal, vertexB - vertexA);
        return Mathf.Abs(dot) < Geo3D.epsilon;
    }
    public Vector3 Mix(Vector3 x, Vector3 y, float weight)
    {
        return x * (1f - weight) + y * weight;
    }

    public List<Vector2> ZRemove()
    { // вращает плоскость с полигоном, и сам полигон, так, чтобы избавиться от Z компоненты не нарушая позиций точек. 
        // Проще говоря поворачивает его в соответствии с XY осями
        Matrix3x3 rotationM = Geo3D.RotatePlane(this.plane.normal, new Vector3(0, 0, -1));
        List<Vector2> flatPoints = new List<Vector2>();
        foreach (Vector3 v in this.vertices)
        {
            Vector3 flatV = rotationM.multiply(v);
            flatPoints.Add(new Vector2(flatV.x, flatV.y));
        }
        return flatPoints;
    }


    public bool IsCounterClockwise()
    {
        Vector3 a = this.vertices[0]; Vector3 b = this.vertices[1]; Vector3 c = this.vertices[2];
        float cross = (b.x - a.x) * (c.y - a.y) - (b.y - a.y) * (c.x - a.x);
        return cross > 0;
    }






}
