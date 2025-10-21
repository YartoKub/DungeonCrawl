using System.Collections.Generic;
using UnityEngine;

public class PolygonCutTest : MonoBehaviour
{
    public Vector3 A;
    public Vector3 B;
    public Vector3 C;

    public Vector3 A2;
    public Vector3 B2;
    public Vector3 C2;

    public bool showCut;

    private void Update()
    {
        DebugUtilities.DebugUltraLine(A, B, Color.yellow);
        DebugUtilities.DebugUltraLine(B, C, Color.yellow);
        DebugUtilities.DebugUltraLine(C, A, Color.yellow);

        DebugUtilities.DebugUltraLine(A2, B2, Color.red);
        DebugUtilities.DebugUltraLine(B2, C2, Color.red);
        DebugUtilities.DebugUltraLine(C2, A2, Color.red);

        Plane plane = new Plane(A2, B2, C2);
        Vector3 pp = (A2 + B2 + C2) / 3;
        DebugUtilities.DebugUltraLine(pp, pp + plane.normal, Color.red);

        Poly3D poly1 = new Poly3D(A, B, C);
        Poly3D poly2 = new Poly3D(A2, B2, C2);
        List<Vector3> cf = new List<Vector3>(); List<Vector3> cb = new List<Vector3>(); List<Vector3> front = new List<Vector3>(); List<Vector3> back = new List<Vector3>();
        poly1.CutPolygon(new Plane(A2, B2, C2), cf, cb, front, back);
        //Debug.Log(cf.Count + " " + cb.Count + " " + front.Count + " " + + back.Count);

        if (Poly3D.IntersectionPolyPoly(poly1, poly2))
        {
            for (int i = 0; i < front.Count; i++)
            {
                int j = (i + 1) % front.Count;
                DebugUtilities.DebugUltraLine(front[i], front[j], Color.green);
            }
            for (int i = 0; i < back.Count; i++)
            {
                int j = (i + 1) % back.Count;
                DebugUtilities.DebugUltraLine(back[i], back[j], Color.violet);
            }
        }


            
        
        
    }


}
