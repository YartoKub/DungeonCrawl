using UnityEngine;

public class TestSignedAngle : MonoBehaviour
{
    public Vector2 A;
    public Vector2 B;
    public Vector2 C1;
    public Vector2 C2;
    public float epsilon;
    // Update is called once per frame
    void Update()
    {
        // Работоспособность SignedAngle
        /*
        DebugUtilities.DebugDrawLine(A, B, Color.red);
        //DebugUtilities.DebugDrawLine(B, C1, Color.red);
        DebugUtilities.DebugDrawLine(B, C2, Color.blue);

        //Debug.Log(Poly2DToolbox.SignedAngle(B, A, C1));
        Debug.Log(Poly2DToolbox.SignedAngle(A, B, C2));*/
        // Находится ли C2 в треугольнике ABC1
        /*
        DebugUtilities.DebugDrawLine(A, B, Color.yellow);
        DebugUtilities.DebugDrawLine(B, C1, Color.yellow);
        DebugUtilities.DebugDrawLine(C1, A, Color.yellow);
        bool IsInside = Poly2DToolbox.IsPointInside(A, B, C1, C2);
        DebugUtilities.DebugDrawCross(C2, IsInside ? Color.green : Color.red);
        */
    }


}
