using System.Collections.Generic;
using UnityEngine;

// Это объект для разбора моделек на полигоны. 
public class MeshVolume
{
    public List<Poly3D> polygons;
    Bounds BBox;
    public MeshVolume()
    {

    }

    public static MeshVolume FromMesh(Mesh mesh, Transform transform)
    {
        Vector3[] verts = mesh.vertices;
        transform.TransformPoints(verts);
        int[] triangles = mesh.triangles;
        return new MeshVolume(verts, triangles);
    }

    public MeshVolume(Vector3[] vertices, int[] triangles)
    {
        this.polygons = new List<Poly3D>();
        
        for (int i = 0; i < triangles.Length / 3; i++)
        {
            int t0 = triangles[i * 3 + 0];
            int t1 = triangles[i * 3 + 1];
            int t2 = triangles[i * 3 + 2];
            polygons.Add(new Poly3D(vertices[t0], vertices[t1], vertices[t2]));
        }
        BBox = new Bounds();
        for (int i = 0; i < vertices.Length; i++)
        {
            BBox.Encapsulate(vertices[i]);
        } 
    }

    public Mesh GetMesh()
    {
        Mesh mesh = new Mesh();
        List<Vector3> vertices = new List<Vector3>();

        for (int i = 0; i < polygons.Count; i++)
        {
            for (int j = 0; j < polygons[i].vertices.Count; j++)
            {
                vertices.Add(polygons[i].vertices[j]);
            }
        }

        int[] triangles = new int[vertices.Count];
        for (int i = 0; i < triangles.Length; i++) triangles[i] = i;

        mesh.SetVertices(vertices.ToArray());
        mesh.SetTriangles(triangles, 0);
        return mesh;
    }
    public void OptimizeMesh()
    {
        OptimizeMesh(this.polygons);
    }

    public static void OptimizeMesh(List<Poly3D> polygons)
    {   // Цель - просто объединить полигоны и все, чтобы места чуть меньше жрали
        for (int i = 0; i < polygons.Count - 1; i++)
        {
            for (int j = i + 1; j < polygons.Count; j++)
            {
                Poly3D A = polygons[i];
                Poly3D B = polygons[j];
                //if (!Poly3D.PlaneSimilarityPolyPoly(A, B)) continue;
                //Debug.Log("Consume poly " + i + " " + j);
                bool success = A.TryConsumePoly(B);
                if (!success) continue;
                polygons.RemoveAt(j);
                j -= 1;
            }
        }

    }

    public void DebugPoly(int poly_index)
    {
        if (!(poly_index >= 0 && poly_index < this.polygons.Count)) return;
        List<Vector3> p = this.polygons[poly_index].vertices;
        for (int i = 0; i < p.Count - 1; i++)
        {
            DebugUtilities.DebugUltraLine(p[i], p[i + 1], Color.green);
        }
        DebugUtilities.DebugUltraLine(p[p.Count - 1], p[0], Color.green);

        float r = 1.0f / (p.Count - 1);
        for (int i = 0; i < p.Count; i++)
        {
            DebugUtilities.DebugUltraHedgehog(p[i], new Color(1 - r * i, 0, 0 + r * i), 0.01f, 0.05f);
        }
    }

}
    

