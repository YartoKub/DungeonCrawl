using UnityEngine;

public class TestSignedAngle : MonoBehaviour
{
    public Vector2 A;
    public Vector2 B;
    //public Vector2 C1;
    public Vector2 C2;
    // Update is called once per frame
    void Update()
    {
        DebugUtilities.DebugDrawLine(A, B, Color.red);
        //DebugUtilities.DebugDrawLine(B, C1, Color.red);
        DebugUtilities.DebugDrawLine(B, C2, Color.blue);

        //Debug.Log(Poly2DToolbox.SignedAngle(B, A, C1));
        Debug.Log(Poly2DToolbox.SignedAngle(A, B, C2));
    }
}
