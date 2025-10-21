using System;
using System.Collections.Generic;
using UnityEngine;


public class Poly3D
{
    public List<Vector3> vertices;
    public Plane plane;
    public bool isHole;
    public bool convex;
    public Bounds BBox; // ћожет убрать коробку? ќна пока не очень полезна€

    public Poly3D(List<Vector3> _vertices)
    {
        if (_vertices.Count < 3)
        {
            throw new ArgumentException("The number of vertices must be at least 3. ярик.");
        }
        vertices = _vertices;
        plane = new Plane(_vertices[0], _vertices[1], _vertices[2]);

    }
    public Poly3D(params Vector3[] _vertices)
    {
        if (_vertices.Length < 3)
        {
            throw new ArgumentException("The number of vertices must be at least 3. ярик.");
        }
        vertices = new List<Vector3>();
        vertices.AddRange(_vertices);
        plane = new Plane(_vertices[0], _vertices[1], _vertices[2]);
    }

    private enum Type : byte
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
        Type polyType = 0;
        List<Type> types = new List<Type>();

        for (int i = 0; i < this.vertices.Count; i++)
        {
            Type type = PlaneSide(Oplane, this.vertices[i]); 
            polyType |= type;
            types.Add(type);
        }

        switch (polyType) {
            case Type.SamePlane:
                if (Vector3.Dot(Oplane.normal, this.plane.normal) > 0) coplanarFront.AddRange(this.vertices);
                else coplanarBack.AddRange(this.vertices);
                break;
            case Type.Front: front.AddRange(this.vertices); break;
            case Type.Back: back.AddRange(this.vertices); break;
            case Type.Intersects:

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
                        float t = (-Oplane.distance - Vector3.Dot(Oplane.normal, vi)) / Vector3.Dot(Oplane.normal, vj - vi);
                        Vector3 v = Vector3.Lerp(vi, vj, t);
                        f.Add(v);
                        b.Add(v);
                    }
                }
                front.AddRange(f);
                back.AddRange(b);
                break;
        }
    }
    
    public Vector3 CutLine(Vector3 vertexA, Vector3 vertexB)
    {
        Vector3 normal = this.plane.normal;
        float w = plane.distance;
        float t = (w - Vector3.Dot(normal, vertexA)) / Vector3.Dot(normal, vertexB - vertexA);
        Vector3 tureturn = Vector3.Lerp(vertexA, vertexB, t);

        return tureturn;
    }
    public bool CutLineSafe(Vector3 vertexA, Vector3 vertexB, out Vector3 tureturn)
    {
        Vector3 normal = this.plane.normal;
        float w = plane.distance;
        float t = (w - Vector3.Dot(normal, vertexA)) / Vector3.Dot(normal, vertexB - vertexA);
        tureturn = Vector3.Lerp(vertexA, vertexB, t);

        float dot = Vector3.Dot(normal, vertexB - vertexA);
        return Mathf.Abs(dot) < Geo3D.epsilon;
    }
    public Vector3 Mix(Vector3 x, Vector3 y, float weight)
    {
        return x * (1f - weight) + y * weight;
    }

    public List<Vector2> ZRemove()
    { // вращает плоскость с полигоном, и сам полигон, так, чтобы избавитьс€ от Z компоненты не наруша€ позиций точек. 
        // ѕроще говор€ поворачивает его в соответствии с XY ос€ми
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

    private void ConsumePoly(int i1, int i2, int j1, int j2, Poly3D other)
    {   // i1 i2 and j1 j2 pairs are consecutive, выдает фигню когда у полигонов разна€ плоскость
        int new_v_count = other.vertices.Count - 2;
        Vector3[] stitched = new Vector3[new_v_count];

        if (!(this.vertices[i1] == other.vertices[j2]))
        {
            (j1, j2) = (j2, j1);
        }
        //Debug.Log(this.vertices[i1] == other.vertices[j2]);
        //Debug.Log(this.vertices[i2] == other.vertices[j1]);
        int bindex = (j2 + 1) % other.vertices.Count; int safety = 0;
        for (int i = 0; i < stitched.Length; i++)
        {
            stitched[safety] = other.vertices[bindex];
            bindex = (bindex + 1) % other.vertices.Count;
        }
        //for (int a = 0; a < stitched.Length; a++) Debug.Log(stitched[a]);
        this.vertices.InsertRange(i2, stitched);
        //for (int a = 0; a < this.vertices.Count; a++) Debug.Log(this.vertices[a]);
    }

    public bool TryConsumePoly(Poly3D other)
    {
        if (this.isHole != other.isHole) return false;
        if (!Poly3D.PlaneSimilarityPolyPoly(this, other)) return false;
        (int i1, int i2, int j1, int j2) = Poly3D.ShareEdgePolyPolyExhaustive(this, other);
        Debug.Log(i1 + " " + i2 + " " + j1 + " " + j2 );
        Debug.Log(this.vertices[i1] + " " + this.vertices[i2]);
        Debug.Log(other.vertices[j1] + " " + other.vertices[j2]);
        if (i1 == -1) return false;
        ConsumePoly(i1, i2, j1, j2, other);
        return true;
    }

    private static Type PlaneSide(Plane p, Vector3 v)
    {
        // Plane.GetSide() возвращает bool и кажетс€ не поддерживает проверки на копланарность
        float t = Vector3.Dot(p.normal, v) + p.distance;
        if (t < -Geo3D.epsilon) return Type.Back;
        if (t >  Geo3D.epsilon) return Type.Front;
        return Type.SamePlane;
    }

    public static bool IntersectionPolyPoly(Poly3D A, Poly3D B)
    {
        if (!A.BBox.Intersects(B.BBox)) return false;
        Type typeA = 0; Type typeB = 0;     // If both types == Intersects, then there indeed is an intersection

        for (int i = 0; i < A.vertices.Count; i++)
            typeA |= PlaneSide(B.plane, A.vertices[i]);
        if (typeA != Type.Intersects) return false;

        for (int i = 0; i < B.vertices.Count; i++)
            typeB |= PlaneSide(A.plane, B.vertices[i]);

        return typeB == Type.Intersects;
    }

    public static (int, int, int, int) ShareEdgePolyPoly(Poly3D A, Poly3D B)
    {
        if (PlaneSimilarityPolyPoly(A, B))
            return ShareEdgePolyPolyExhaustive(A, B);

        return ShareEdgePolyPolyDifferentPlanes(A, B);
    }

    public static (int, int, int, int) ShareEdgePolyPolyDifferentPlanes(Poly3D A, Poly3D B)
    {
        Plane Ap = A.plane;
        Plane Bp = B.plane;
        Vector3 direction = Vector3.Cross(Ap.normal, Bp.normal);
        if (direction.magnitude < Geo3D.epsilon) return (-1, -1, -1, -1);
        direction = direction.normalized;

        float det = Ap.normal.x * Bp.normal.y - Ap.normal.y * Bp.normal.x;
        if (Math.Abs(det) < Geo3D.epsilon) return (-1, -1, -1, -1);

        float x = (Ap.distance * Bp.normal.y - Bp.distance * Ap.normal.y) / det;
        float y = (Ap.normal.x * Bp.distance - Bp.normal.x * Ap.distance) / det;
        float z = 0.0f;
        Vector3 origin = new Vector3(x, y, z);
        // “еперь есть направление и точка отсчета, если вершана полигона не находитс€ на пересечении плоскостей, то она пропускаетс€

        List<Vector3> Av = A.vertices;
        List<Vector3> Bv = B.vertices;
        for (int i = 0; i < Av.Count; i++)
        {
            if (!PointBelongToLine(origin, direction, Av[i])) continue;
            for (int j = 0; j < Bv.Count; j++)
            {
                // –аз эти точки равны значит здесь не нужно провер€ть принадлежность к линии
                if (Av[i] != Bv[j]) continue;

                int pi = (i - 1 + Av.Count) % Av.Count;
                int ni = (i + 1) % Av.Count;
                int pj = (j - 1 + Bv.Count) % Bv.Count;
                int nj = (j + 1) % Bv.Count;
                
                if (Av[pi] == Bv[pj]) return (pi, i, pj, j);
                if (Av[pi] == Bv[nj]) return (pi, i, j, nj);
                if (Av[ni] == Bv[pj]) return (i, ni, pj, j);
                if (Av[ni] == Bv[nj]) return (i, ni, j, nj);
            }
        }
        return (-1, -1, -1, -1);
    }

    private static bool PointBelongToLine(Vector3 origin, Vector3 direction, Vector3 point)
    {   // ѕросто сравниваю направлени€ векторов, если они слишком разн€тс€ то точка не принадлежит линии
        Vector3 p_dir = (point - origin).normalized;
        if (Math.Abs(p_dir.x - direction.x) > Geo3D.epsilon) return false;
        if (Math.Abs(p_dir.y - direction.y) > Geo3D.epsilon) return false;
        if (Math.Abs(p_dir.z - direction.z) > Geo3D.epsilon) return false;
        return true;
    }

    // ¬озврат: i, next i, j, next j
    public static (int, int, int, int) ShareEdgePolyPolyExhaustive(Poly3D A, Poly3D B)
    {
        List<Vector3> Av = A.vertices;
        List<Vector3> Bv = B.vertices;
        for (int i = 0; i < Av.Count; i++)
        {
            for (int j = 0; j < Bv.Count; j++)
            {
                if (Av[i] != Bv[j]) continue;
                
                //int pi = (i - 1  + Av.Count) % Av.Count;
                int ni = (i + 1) % Av.Count;
                int pj = (j - 1  + Bv.Count) % Bv.Count;
                if (Av[ni] == Bv[pj]) return (i, ni, pj, j);
                int nj = (j + 1) % Bv.Count;
                if (Av[ni] == Bv[nj]) return (i, ni, j, nj);
                // Ќадо проверить следующий и предыдущий j, так как неизвестна совспадает ои пор€ток вершин CCW/CW
                // “акже из-за того что они наход€тс€ в пространстве 
                //if (Av[pi] == Bv[pj]) return (pi, i, pj, j); 
                //if (Av[pi] == Bv[nj]) return (pi, i, j, nj); 
            }
        }
        return (-1,-1,-1,-1);

    }

    public static bool PlaneSimilarityPolyPoly(Poly3D A, Poly3D B)
    {
        if (Math.Abs(A.plane.distance - B.plane.distance) > Geo3D.epsilon) return false;

        if (Math.Abs(A.plane.normal.x - B.plane.normal.x) > Geo3D.epsilon) return false;
        if (Math.Abs(A.plane.normal.y - B.plane.normal.y) > Geo3D.epsilon) return false;
        if (Math.Abs(A.plane.normal.z - B.plane.normal.z) > Geo3D.epsilon) return false;
        return true;
    }





}
