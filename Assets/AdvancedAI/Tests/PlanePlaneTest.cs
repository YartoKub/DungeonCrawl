using UnityEngine;

public class PlanePlaneTest : MonoBehaviour
{
    public GameObject p1;
    public GameObject p2;

    private void Start()
    {
        (bool b, Vector3 ans) = Matrix3x3.CramerXYZ(new Vector3(1, 5, 3), new Vector3(2, 1, -1), new Vector3(4, 2, 1), new Vector3(31, 29, 10));
        Debug.Log(b + " " + ans);

        (bool b1, Vector3 ans1) = Matrix3x3.CramerABC(new Vector3(1, 2, 4), new Vector3(5, 1, 2), new Vector3(3, -1, 1), new Vector3(31, 29, 10));
        Debug.Log(b1 + " " + ans1);
    }

    private void Update()
    {
        Vector3 a1 = p1.transform.position;
        Vector3 a2 = a1 + p1.transform.right;
        Vector3 a3 = a1 + p1.transform.forward;

        Vector3 b1 = p2.transform.position;
        Vector3 b2 = b1 + p2.transform.right;
        Vector3 b3 = b1 + p2.transform.forward;

        DebugUtilities.DebugUltraLine(a1, a2, Color.blue);
        DebugUtilities.DebugUltraLine(a2, a3, Color.blue);
        DebugUtilities.DebugUltraLine(a3, a1, Color.blue);

        DebugUtilities.DebugUltraLine((a1 + a2 + a3) / 3, (a1 + a2 + a3) / 3 + p1.transform.up, Color.cyan);

        DebugUtilities.DebugUltraLine(b1, b2, Color.red);
        DebugUtilities.DebugUltraLine(b2, b3, Color.red);
        DebugUtilities.DebugUltraLine(b3, b1, Color.red);

        DebugUtilities.DebugUltraLine((b1 + b2 + b3) / 3, (b1 + b2 + b3) / 3 + p2.transform.up, Color.pink);


        Poly3D.PlanePlaneIntersection(new Plane(a1, a2, a3), new Plane(b1, b2, b3));

        //DebugUtilities.DebugUltraLine(s, e, Color.red);
    }
}
