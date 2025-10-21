using System.Collections.Generic;
using UnityEngine;

public class MeshTestMerge : MonoBehaviour
{
    public GameObject A;
    public GameObject B;
    public GameObject C;

    public MeshFilter combined_mesh_filter;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    MeshVolume volume_A;

    public int render_poly_index;


    void Start()
    {
        bool a_success = A.TryGetComponent(out MeshFilter filterA);
        if (!a_success) return;
        volume_A = MeshVolume.FromMesh(filterA.mesh, A.transform);

        combined_mesh_filter.mesh = volume_A.GetMesh();
        volume_A.OptimizeMesh();

        
    }

    // Update is called once per frame
    void Update()
    {
        volume_A.DebugPoly(render_poly_index);
    }
}
