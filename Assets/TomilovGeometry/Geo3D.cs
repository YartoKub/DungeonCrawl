using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class Geo3D
{
    public static float epsilon = 0.0001f;
    public static bool PBelongs(Vector3 p0, Vector3 p1, Vector3 p2, Vector3 p3,  Vector3 PQ) // point belongs to a sphere
    {

        Debug.Log("UNIMPLEMENTED, retirning junk");
        return false;
    }

    // determinant of a 2x2 matrix / определитель
    // 0x 0y -> Det
    // 1x 1y 
    public static float Det2x2(Vector2 p0, Vector2 p1)
    {
        return Det2x2(p0.x, p0.y, p1.x, p1.y);
    }
    public static float Det2x2(float x0, float y0, float x1, float y1)
    {
        return x0 * y1 - x1 * y0;
    }
    // returns a martix that can be used to rotate a Vector3 point that lies on a plane to match desired plane
    public static Matrix3x3 RotatePlane(Vector3 planeNormal, Vector3 desiredNormal) {
        float costheta = Vector3.Dot(planeNormal, desiredNormal) / (planeNormal.magnitude * desiredNormal.magnitude);
        Vector3 cross = Vector3.Cross(planeNormal, desiredNormal);
        Vector3 axis = cross / cross.magnitude;
        if (Mathf.Abs( cross.magnitude) < epsilon) {
            if (planeNormal == desiredNormal) return Matrix3x3.Matrix3x3One();
            else return Matrix3x3.Matrix3x3One().Multiply(-1f);
        }
        float c = costheta;
        float s = Mathf.Sqrt(1 - costheta * costheta);
        float C = 1 - c;
        float x = axis.x; float y = axis.y; float z = axis.z;
        Matrix3x3 rmat = new Matrix3x3(
            new Vector3(x*x*C+c,   x*y*C-z*s, x*z*C+y*s),
            new Vector3(y*x*C+z*s, y*y*C+c,   y*z*C-x*s),
            new Vector3(z*x*C-y*s, z*y*C+x*s, z*z*C+c  ));
        return rmat;
    }

    public static bool CutLine(Vector3 p0, Vector3 p1, Plane plane, out Vector3 intersection)
    {
        // A is vector origin, B is vector direction
        Vector3 u = p1 - p0;
        float dot = Vector3.Dot(plane.normal, u);
        if (Mathf.Abs( dot) > epsilon)
        {
            Vector3 p_co = plane.normal * (-plane.distance /  Vector3.Dot(plane.normal, plane.normal) );
            Vector3 w = p0 - p_co;
            float factor = -1 * (Vector3.Dot(plane.normal, w))  / dot;
            u = u * factor;
            intersection = p0 + u;
            return true;
        }
        Debug.Log("line is parallel to the plane");
        intersection = Vector3.zero;
        return false;
    }
    public static bool IsPointOnPlane(Vector3 p0, Plane p) // True if point lies on plane
    {
        return Mathf.Abs(p0.x * p.normal.x + p0.y * p.normal.y + p0.z * p.normal.z + p.distance) < epsilon;
    }
    public static Vector3 ScaleVector(Vector3 a, Vector3 b)
    {
        return new Vector3(a.x * b.x, a.y * b.y, a.z * b.z);
    }

    public static bool RayIntersectsTriangle(Vector3 r0, Vector3 r1, Vector3 t0, Vector3 t1, Vector3 t2, out Vector3 intersection)
    {
        // Moller Thrumbore
        intersection = new Vector3(0, 0, 0);
        Vector3 rOrigin = r0;
        Vector3 rDirection = r1 - r0;

        Vector3 edge1 = t1 - t0;
        Vector3 edge2 = t2 - t0;

        Vector3 ray_cross_e2 = Vector3.Cross(rDirection, edge2);
        float det = Vector3.Dot(edge1, ray_cross_e2);

        if (det > -epsilon && det < epsilon) return false;

        float inv_det = 1.0f / det;
        Vector3 s = rOrigin - t0;
        float u = inv_det * Vector3.Dot(s, ray_cross_e2);

        if ((u < 0 && Mathf.Abs(u) > epsilon) || (u > 1 && Mathf.Abs(u - 1) > epsilon)) return false;
        Vector3 s_cross_e1 = Vector3.Cross(s, edge1);
        float v = inv_det * Vector3.Dot(rDirection, s_cross_e1);

        if ((v < 0 && Mathf.Abs(v) > epsilon) || (u + v > 1 && Mathf.Abs(u +v - 1) > epsilon)) return false;

        float t = inv_det * Vector3.Dot(edge2, s_cross_e1);

        if (t > epsilon) {
            intersection = rOrigin + rDirection * t;
            Debug.Log(t);
            return true;
        } 
        else return false;
    }

    public static bool RayPlaneIntersectionGetT_Safe(Vector3 r0, Vector3 r1, Vector3 pointOnPlane, Vector3 planeNormal, out float t)
    {
        t = -1;
        float denom = Vector3.Dot(planeNormal, r1 - r0);
        if (Mathf.Abs(denom) > 0.0001f) // your favorite epsilon
        {
            t = Vector3.Dot((pointOnPlane - r0), planeNormal) / denom;
            if (t >= 0) return true; // you might want to allow an epsilon here too
        }
        return false;
    }
    public static float RayPlaneIntersectionGetT_Unsafe(Vector3 r0, Vector3 r1, Vector3 pointOnPlane, Vector3 planeNormal)
    {
        RayPlaneIntersectionGetT_Safe( r0,  r1,  pointOnPlane,  planeNormal, out float t);
        return t;
    }
    public static float RayPlaneIntersectionGetT(Vector3 r0, Vector3 r1, Vector3 t0, Vector3 t1, Vector3 t2)     {
        //Debug.Log(new Poly3D(t0, t1, t2).plane.distance);
        Vector3 edge1 = t1 - t0;
        Vector3 edge2 = t2 - t0;
        Vector3 ray_cross_e2 = Vector3.Cross(r1 - r0, edge2);
        float det = Vector3.Dot(edge1, ray_cross_e2);
        if (Mathf.Abs(det) < epsilon) { return -1; }
        Vector3 s = r0 - t0;
        Vector3 s_cross_e1 = Vector3.Cross(s, edge1); 
        float inv_det = 1.0f / det;
        return inv_det * Vector3.Dot(edge2, s_cross_e1);
    }
    public static Vector3 RayPlaneIntersection(Vector3 RayStart, Vector3 RayEnd, Vector3 pointonPlane, Vector3 PlaneNormal) {
        RayPlaneIntersectionGetT_Safe(RayStart, RayEnd, pointonPlane, PlaneNormal, out float t);
        return (RayEnd - RayStart) * t + RayStart;
    }

    /*
    public static bool BBoxCheck(BBox A, BBox B) {
        return  A.op0.x < B.op1.x && A.op1.x > B.op0.x &&
                A.op0.y < B.op1.y && A.op1.y > B.op0.y &&
                A.op0.z < B.op1.z && A.op1.z > B.op0.z;
    }*/

    /*
        public static float RayPlaneIntersectionGetT(Vector3 RayStart, Vector3 RayEnd, Vector3 PlaneNormal, float parameterD)
    {
        Vector3 RayDirection = (RayEnd - RayStart);
        float top = -Vector3.Dot(PlaneNormal, RayStart);
        float bottom = Vector3.Dot(PlaneNormal, RayDirection);
        if (Mathf.Abs(bottom) < epsilon) { Debug.Log("parallel"); return -1; }
        float t = (-parameterD + top) / bottom;
        Debug.Log(RayStart + " " + RayDirection + " " +  PlaneNormal + " " + -parameterD);
        Debug.Log(-parameterD + " " + top + " " + bottom);
        Debug.Log(t);
        return t;
    }

    public Vertex Mix(Vertex x, Vertex y, float weight)
    {
        float i = 1f - weight;
        Vertex v = new Vertex();
        v.position = x.position * i + y.position * weight;
        v.color = x.color * i + y.color * weight;
        v.normal = x.normal * i + y.normal * weight;
        v.tangent = x.tangent * i + y.tangent * weight;

        return v;
    }*/
}
