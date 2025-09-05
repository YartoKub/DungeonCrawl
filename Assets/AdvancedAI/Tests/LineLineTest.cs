using UnityEngine;

public class LineLineTest : MonoBehaviour
{
    public Vector2 A1;
    public Vector2 A2;
    public Vector2 B1;
    public Vector2 B2;
    public Vector2 C1;
    public Vector2 C2;

    private void Update()
    {
        SimpleLineLine();
    }

    private void SimpleLineLine()
    {
        DebugUtilities.DebugDrawLine(A1, A2, Color.red);
        DebugUtilities.DebugDrawLine(B1, B2, Color.yellow);
        DebugUtilities.DebugDrawLine(C1, C2, Color.blue);

        Vector2 ab;
        float distance; float distance2;
        bool AB = Poly2DToolbox.LineLineIntersection(A1, A2, B1, B2, out ab, out distance);
        Vector2 ac;
        bool AC = Poly2DToolbox.LineLineIntersection(A1, A2, C1, C2, out ac, out distance2);
        Debug.Log(distance.ToString() + " " + distance2);
        if (AB) DebugUtilities.DebugDrawCross(ab, Color.orange);
        if (AC) DebugUtilities.DebugDrawCross(ac, Color.violet);
    }
}
