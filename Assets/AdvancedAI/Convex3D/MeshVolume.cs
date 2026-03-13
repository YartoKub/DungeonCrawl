using System.Collections.Generic;
using UnityEngine;

// Это объект для разбора моделек на полигоны. 
public class MeshVolume
{
    public List<Poly3D> polygons;
    Bounds BBox;
    public IntMatrixGraph connections;
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
        //InitializeMesh();
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

    public void InitializeMesh() {
        this.OptimizeMesh();
        this.UpdateConnections();
    }
    public void OptimizeMesh() { OptimizeMesh(this.polygons); }
    // Функция статическая потому что может понадобиться оптиимзировать полигоны не принадлежащие к объему
    public static void OptimizeMesh(List<Poly3D> polygons)
    {   // Цель - просто объединить полигоны и все, чтобы места чуть меньше жрали
        for (int i = 0; i < polygons.Count - 1; i++)
        {
            for (int j = i + 1; j < polygons.Count; j++)
            {
                Poly3D A = polygons[i];
                Poly3D B = polygons[j];
                bool success = A.TryConsumePoly(B);
                if (!success) continue;
                polygons.RemoveAt(j); 
                j -= 1;
            }
        }
    }
    public void UpdateConnections() { UpdateConnections(this); }
    public static void UpdateConnections(MeshVolume mv)
    {
        mv.connections = new IntMatrixGraph(mv.polygons.Count);
        for (int a = 0; a < mv.polygons.Count - 1; a++)
        {
            for (int b = a + 1; b < mv.polygons.Count; b++)
            {
                (int a1, int a2, int b1, int b2) = Poly3D.ShareEdgePolyPoly(mv.polygons[a], mv.polygons[b]);
                if (a1 == -1) continue; // ВЕсли одно == -1, то все остальные тоже
                mv.connections.SetValueSafe(true, a, b);
            }
        }   // Самосвязь
        for (int i = 0; i < mv.polygons.Count; i++)
            mv.connections.SetValueSafe(true, i, i);
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
            DebugUtilities.DebugUltraHedgehog(p[i], new Color(1 - r * i, 0, 0 + r * i), 0.001f, 0.05f);
        }
        if (this.connections == null) return;

        for (int b = 0; b < this.polygons.Count; b++)
        {
            if (b == poly_index) continue;
            bool con = this.connections.GetValue(poly_index, b);
            if (!con) continue;
            Vector3 av_a = this.polygons[poly_index].AveragePoint();
            Vector3 av_b = this.polygons[b].AveragePoint();
            DebugUtilities.DebugUltraLine(av_a, av_b, Color.purple);
        }
    }
}
    

