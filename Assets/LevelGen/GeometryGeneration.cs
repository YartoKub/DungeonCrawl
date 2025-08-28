using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class GeometryGeneration
{
    public static Mesh AxisAlignedBox(Vector3 a, Vector3 b)
    {
        Vector3[] vertices = new Vector3[8];
        vertices[0] = new Vector3(a.x, a.y, a.z);
        vertices[1] = new Vector3(a.x, a.y, b.z);
        vertices[2] = new Vector3(a.x, b.y, a.z);
        vertices[3] = new Vector3(a.x, b.y, b.z);
        vertices[4] = new Vector3(b.x, a.y, a.z);
        vertices[5] = new Vector3(b.x, a.y, b.z);
        vertices[6] = new Vector3(b.x, b.y, a.z);
        vertices[7] = new Vector3(b.x, b.y, b.z);

        int[] triangles = new int[]
        {
            0, 1, 2, // AX
            3, 2, 1,

            4, 6, 5, // BX
            5, 6, 7,

            0, 4, 1, // AY
            1, 4, 5,

            2, 3, 6, // BY
            3, 7, 6,

            1, 5, 3, // AZ 
            3, 5, 7,

            0, 2, 4, // BZ
            2, 6, 4,
        };

        Vector3[] verticesOkayNormals = new Vector3[36];
        float x = b.x - a.x; float y = b.y - a.y; float z = b.z - a.z;
        Vector2[] uvList =
        {
            new Vector2(1 * z,0 * y), new Vector2(0 * z,0 * y), new Vector2(1 * z,1 * y),  // AX
            new Vector2(0 * z,1 * y), new Vector2(1 * z,1 * y), new Vector2(0 * z,0 * y),

            new Vector2(0 * z,0 * y), new Vector2(0 * z,1 * y), new Vector2(1 * z,0 * y),  // BX
            new Vector2(1 * z,0 * y), new Vector2(0 * z,1 * y), new Vector2(1 * z,1 * y),

            new Vector2(0 * z,0 * x), new Vector2(0 * z,1 * x), new Vector2(1 * z,0 * x),  // AY
            new Vector2(1 * z,0 * x), new Vector2(0 * z,1 * x), new Vector2(1 * z,1 * x),

            new Vector2(1 * z,0 * x), new Vector2(0 * z,0 * x), new Vector2(1 * z,1 * x),  // BY
            new Vector2(0 * z,0 * x), new Vector2(0 * z,1 * x), new Vector2(1 * z,1 * x),

            new Vector2(1 * x,0 * y), new Vector2(0 * x,0 * y), new Vector2(1 * x,1 * y),  // AZ
            new Vector2(1 * x,1 * y), new Vector2(0 * x,0 * y), new Vector2(0 * x,1 * y),

            new Vector2(0 * x,0 * y), new Vector2(0 * x,1 * y), new Vector2(1 * x,0 * y),  // BZ
            new Vector2(0 * x,1 * y), new Vector2(1 * x,1 * y), new Vector2(1 * x,0 * y),
        };

        for (int i = 0; i < 36; i++)
        {
            verticesOkayNormals[i] = vertices[triangles[i]];
            triangles[i] = i;
        }

        Mesh mesh = new Mesh();
        mesh.vertices = verticesOkayNormals;
        mesh.triangles = triangles;
        mesh.SetUVs(0, uvList);





        mesh.RecalculateNormals();
        mesh.RecalculateBounds();

        return mesh;
    }

    public static Mesh AxisAlignedBox(Rect3D someRect)
    {
        return AxisAlignedBox(someRect.A, someRect.B);
    }

}
